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
        Hashtable words;

        public OPFDictionary(string path) {
            string line;
            StreamReader infile = new StreamReader(path);
            words = new Hashtable();
            while ((line = infile.ReadLine()) != null) {
                words.Add(line, true);
            }
            infile.Close();
        }

        public Boolean contains(string word) {
            return words.ContainsKey(word);
        }
    }
}
