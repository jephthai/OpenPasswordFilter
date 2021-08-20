using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Globalization;

namespace PasswordCheckerRay
{
    static class PwDBSha256
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        public static bool Exists(string candidate)
        {
            string line;
            bool ret = false;
           
            string datafile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "custom_sha256.txt");
            SHA256 mySHA256 = SHA256.Create();
            try
            {
                mySHA256.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(candidate.ToLower(CultureInfo.InvariantCulture)));
                candidate = string.Join("", mySHA256.Hash.Select(c => ((int)c).ToString("x2", CultureInfo.InvariantCulture)));
            }
            finally { mySHA256.Dispose(); }
            /*
            StreamWriter log = File.AppendText("c:\\temp\\sha256.txt");
            log.WriteLine(candidate);
            log.Close();
            */

            StreamReader pwfile;
            try
            {
                pwfile = new StreamReader(datafile);
            }
            catch
            {
                return false;
            }

            try
            {
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
                    }
                }
                // pwfile.Close();
            }
            finally { pwfile.Close(); }
            return ret;
        }
    }
}
