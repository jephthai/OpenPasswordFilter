// This file is part of OpenPasswordFilter.
// 
// OpenPasswordFilter is free software; you can redistribute it and / or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// OpenPasswordFilter is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OpenPasswordFilter; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111 - 1307  USA
//

using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;

namespace OPFService {
    class OPFDictionary {
        Hashtable excluded;
        Hashtable exact;
        Hashtable partial;
        

        public OPFDictionary(string path) {
            string line;

            // excluded.txt - Entries from this file will be allowed even if blocked by the exact.txt or partial.txt file.
            excluded = new Hashtable();
            if (File.Exists(path + "\\excluded.txt"))
            {
                StreamReader excludedInFile = new StreamReader(path + "\\excluded.txt");
                while ((line = excludedInFile.ReadLine()) != null)
                {
                    excluded.Add(line, true);
                }
                excludedInFile.Close();
            }

            // exact.txt - Only entries that match exactly will fail the test
            exact = new Hashtable();
            
            // if exact.txt doesn't exist, check for opfdict.txt and use that instead to keep existing installations happy
            string exactFile = null;
            if (File.Exists(path + "\\exact.txt"))
            {
                exactFile = "\\exact.txt";
            }
            else if (File.Exists(path + "\\opfdict.txt"))
            {
                exactFile = "\\opfdict.txt";
            }

            if (exactFile != null)
            {

                StreamReader exactInFile = new StreamReader(path + exactFile);
                while ((line = exactInFile.ReadLine()) != null)
                {
                    exact.Add(line, true);
                }
                exactInFile.Close();
            }

            // partial.txt - If any part of the password matches an entry in this file it will fail the test (case-insensitive).
            partial = new Hashtable();
            if (File.Exists(path + "\\partial.txt"))
            {

                StreamReader partialInFile = new StreamReader(path + "\\partial.txt");
                while ((line = partialInFile.ReadLine()) != null)
                {
                    partial.Add(line, true);
                }
                partialInFile.Close();
            }
        }

        public Boolean contains(string word)
        {
            if (excluded.ContainsKey(word))
            {
                return false;
            }
            if (exact.ContainsKey(word))
            {
                return true;
            }
            foreach (string entry in partial.Keys)
            {
                if (word.ToLower().Contains(entry.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
