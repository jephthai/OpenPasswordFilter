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
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace OPFService {
  class OPFDictionary {
    HashSet<string> matchlist;
    List<string> contlist;
    List<Regex> regexlist;

    public void writeLog(string message, System.Diagnostics.EventLogEntryType level) {
      using (EventLog eventLog = new EventLog("Application")) {
        eventLog.Source = "Application";
        eventLog.WriteEntry(message, level, 101, 1);
      }
    }
    public OPFDictionary(string pathmatch, string pathcont, string pathregex) {

      writeLog("Opening Match Configuration File", EventLogEntryType.Information);
      string line;

      StreamReader infilematch = new StreamReader(pathmatch);
      matchlist = new HashSet<string>();
      int a = 1;
      while ((line = infilematch.ReadLine()) != null) {
        try {
          matchlist.Add(line.ToLower());
          a += 1;
        } catch {
          using (EventLog eventLog = new EventLog("Application")) {
            eventLog.Source = "Application";
            eventLog.WriteEntry("Died trying to ingest line number " + a.ToString() + " of opfmatch.txt.", EventLogEntryType.Error, 101, 1);
          }
        }
      }
      infilematch.Close();
      writeLog("Opening Contains Configuration File", EventLogEntryType.Information);
      StreamReader infilecont = new StreamReader(pathcont);
      contlist = new List<string>();
      a = 1;
      while ((line = infilecont.ReadLine()) != null) {
        try {
          contlist.Add(line.ToLower());
          a += 1;
        } catch {
          using (EventLog eventLog = new EventLog("Application")) {
            eventLog.Source = "Application";
            eventLog.WriteEntry("Died trying to ingest line number " + a.ToString() + " of opfcont.txt.", EventLogEntryType.Error, 101, 1);
          }
        }
      }
      infilematch.Close();
      writeLog("Opening Regular Expression Configuration File", EventLogEntryType.Information);
      StreamReader infileregex = new StreamReader(pathregex);
      regexlist = new List<Regex>();
      a = 1;
      while ((line = infileregex.ReadLine()) != null) {
        try {
          regexlist.Add(new Regex(line, RegexOptions.IgnoreCase));
          a += 1;
        } catch {
          using (EventLog eventLog = new EventLog("Application")) {
            eventLog.Source = "Application";
            eventLog.WriteEntry("Died trying to ingest line number " + a.ToString() + " of opfregex.txt.", EventLogEntryType.Error, 101, 1);
          }
        }
      }
      infileregex.Close();

      writeLog("Successfully parsed all configuration files", EventLogEntryType.Information);

    }

    public Boolean contains(string word) {
      foreach (string badstr in contlist) {
        if (word.ToLower().Contains(badstr)) {
          using (EventLog eventLog = new EventLog("Application")) {
            eventLog.Source = "Application";
            eventLog.WriteEntry("Password attempt contains poison string " + badstr + ", case insensitive.", EventLogEntryType.Information, 101, 1);
          }
          return true;
        }
      }

      if (matchlist.Contains(word)) {
        using (EventLog eventLog = new EventLog("Application")) {
          eventLog.Source = "Application";
          eventLog.WriteEntry("Password attempt matched a string in the bad password list", EventLogEntryType.Information, 101, 1);
        }
        return true;
      }

      foreach (Regex r in regexlist) {

        Match m = r.Match(word);
        if (m.Success) {
          using (EventLog eventLog = new EventLog("Application")) {
            eventLog.Source = "Application";
            eventLog.WriteEntry("Password attempt matched this regular express: " + r.ToString(), EventLogEntryType.Information, 101, 1);
          }
          return true;
        }
      }

      using (EventLog eventLog = new EventLog("Application")) {
        eventLog.Source = "Application";
        eventLog.WriteEntry("Password passed custom filter.", EventLogEntryType.Information, 101, 1);
      }
      return false;
    }
  }
}
