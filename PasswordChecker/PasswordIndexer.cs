using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace PasswordCheckerRay
{
    static class PasswordIndexer
    {

        public static void IndexFile()
        {
            string datafolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "data");
            var pathmatch = System.IO.Path.Combine(datafolder, "opfmatch.txt");
            var nineindex = System.IO.Path.Combine(datafolder, "9.idx");

            if (
                    File.Exists(nineindex) && File.GetLastWriteTime(pathmatch) < File.GetLastWriteTime(nineindex)
                )
            {
                return;
            }

            StreamReader infilematch = new StreamReader(pathmatch);
            int linenumber = 1;
            string line;

            IDictionary<int, StreamWriter> writers = new Dictionary<int, StreamWriter>();
            try
            {
                while ((line = infilematch.ReadLine()) != null)
                {
                    int len = Encoding.UTF8.GetBytes(line).Length;

                    if (!writers.ContainsKey(len))
                    {
                        writers[len] = File.AppendText(System.IO.Path.Combine(datafolder, len.ToString() + ".unsrt"));
                    }
                    writers[len].WriteLine(line);


                }
            }

            finally
            {
                infilematch.Close();
                foreach (KeyValuePair<int, StreamWriter> kvp in writers)
                {
                    kvp.Value.Close();
                }
                writers.Clear();
            }


            string[] fileEntries = Directory.GetFiles(datafolder);
            foreach (string fileName in fileEntries)
            {


                if (fileName.EndsWith(".unsrt"))
                {
                    SortedDictionary<string, int> index = new SortedDictionary<string, int>();
                    string last = "";
                    linenumber = 0;
                    StreamReader infile = new StreamReader(fileName);
                    List<string> matchlist;
                    matchlist = new List<string>();
                    while ((line = infile.ReadLine()) != null)
                    {
                        matchlist.Add(line.ToLower());
                    }
                    matchlist.Sort();

                    string sortedfile = fileName.Replace(".unsrt", ".srt");
                    string indexfile = fileName.Replace(".unsrt", ".idx");
                    if (File.Exists(sortedfile)) { File.Delete(sortedfile); }
                    if (File.Exists(indexfile)) { File.Delete(indexfile); }

                    int len = Int32.Parse(Path.GetFileNameWithoutExtension(fileName));

                    foreach (string pass in matchlist)
                    {
                        if (!writers.ContainsKey(len))
                        {
                            writers[len] = File.AppendText(System.IO.Path.Combine(datafolder, len.ToString() + ".srt"));
                        }
                        // Don't store duplicates (after lowercasing)
                        if (string.Compare(pass, last) != 0)
                        {
                            writers[len].WriteLine(pass);
                            string firstchar = pass.Substring(0, 1);
                            if (!index.ContainsKey(firstchar))
                            {
                                index[firstchar] = linenumber;
                            }
                            last = pass;
                            linenumber++;
                        }

                    }

                    foreach (string character in index.Keys)
                    {
                        StreamWriter sw = File.AppendText(indexfile);
                        int recordpos = ((len + 2) * index[character]);
                        sw.WriteLine(character + ":" + recordpos.ToString());
                        sw.Close();
                    }
                    infile.Close();
                    File.Delete(fileName);
                }

            }
            foreach (KeyValuePair<int, StreamWriter> kvp in writers)
            {
                kvp.Value.Close();
            }
            writers.Clear();

        }



    }
}
