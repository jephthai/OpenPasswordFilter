using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Globalization;

namespace PasswordCheckerRay
{
    internal static class PwDBRay
    {
        // static CultureInfo culture = new CultureInfo("en-US");
        private static int GetIndex(string candidate, string IndexFile)
        {
            string line;
            int ret = -1;
            StreamReader idxfile;
            try { idxfile  = new StreamReader(IndexFile); }
            catch { return ret; }
            try {
			    string firstchar = candidate.Substring(0, 1);
			    while ((line = idxfile.ReadLine()) != null)
			    {
				    string[] record = line.Split(":".ToCharArray(), 2);
				    int charmatch = string.Compare(record[0], firstchar, StringComparison.Ordinal);
				    if (charmatch == 0)
				    {
					    ret = Int32.Parse(record[1], CultureInfo.InvariantCulture);
					    break;
				    }
				    if (charmatch == 1)
				    {
					    ret = -1;
					    break;
				    }
			    }
            }
            finally
            {
                idxfile.Close();
            }
            return ret;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower")]
        public static bool Exists(string candidate)
        {
            string line;
            bool ret = false;
            // var datafolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "openpasswordfilter");

            string datafolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "data");
            //  string path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(MyClass)).CodeBase);
            candidate = candidate.ToLower();
            int canLength = Encoding.UTF8.GetBytes(candidate).Length;

            int offset = GetIndex(candidate, System.IO.Path.Combine(datafolder, canLength.ToString(CultureInfo.InvariantCulture) + ".idx"));

            if (offset == -1)
            {
                WriteToFile("first character not found in index");
                return false;
            }

            WriteToFile(string.Format("first character found in index {0} in file {1}.idx", offset, canLength.ToString(CultureInfo.InvariantCulture)));
            StreamReader pwfile = new StreamReader(System.IO.Path.Combine(datafolder, canLength.ToString(CultureInfo.InvariantCulture) + ".srt"));
            /*
            try
            {
                StreamWriter log = File.AppendText("c:\\temp\\pwddbray.txt");
                log.WriteLine(candidate + " " + System.IO.Path.Combine(datafolder, canLength.ToString() + ".srt") + " " + offset);
                log.Close();
            }
            catch { }
            */

            try
            {

                pwfile.BaseStream.Seek(offset, SeekOrigin.Begin);
                while ((line = pwfile.ReadLine()) != null)
                {
                    int linematch = string.Compare(line, candidate, StringComparison.OrdinalIgnoreCase);
                    if (linematch == 0)
                    {
                        ret = true;
                        break;
                    }
                    if (linematch == 1)
                    {
                        ret = false;
                        break;
                    }
                }
            }
            finally { pwfile.Close(); }
            WriteToFile("PwDBRay.exists() returning " + ret.ToString());
            return ret;
        }
        static private void WriteToFile(string text)
        {
            string path = "C:\\passwordchecker_log.txt";
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                string line;
                line = string.Format("{0}\t{1}", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture), text);
                writer.WriteLine(line);
            }
        }
    }

}
