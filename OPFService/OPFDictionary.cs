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
using System.DirectoryServices.AccountManagement;
using System.Security.Cryptography;

namespace OPFService
{
  class OPFDictionary
  {
    HashSet<string> matchlist;
    List<string> contlist;
    List<Regex> regexlist;
    string pathmatch;
    string pathcont;
    string pathregex;
    DateTime matchmtime;
    DateTime contmtime;
    DateTime regexmtime;

    public void writeLog(string message, System.Diagnostics.EventLogEntryType level)
    {
      using (EventLog eventLog = new EventLog("Application"))
      {
        eventLog.Source = "Application";
        eventLog.WriteEntry(message, level, 101, 1);
      }
    }
    public OPFDictionary(string m, string c, string r)
    {

      pathmatch = m;
      pathcont = c;
      pathregex = r;

      writeLog("Opening Match Configuration File " + pathmatch, EventLogEntryType.Information);
      ReadInMatchFile();
      writeLog("Opening Contains Configuration File", EventLogEntryType.Information);
      ReadInContFile();
      writeLog("Opening Regular Expression Configuration File", EventLogEntryType.Information);
      ReadInRegexFile();

      writeLog("Successfully parsed all configuration files", EventLogEntryType.Information);

    }

    private void ReadInRegexFile()
    {
      string line;
      regexmtime = File.GetLastWriteTimeUtc(pathregex);
      StreamReader infileregex = new StreamReader(pathregex);
      regexlist = new List<Regex>();
      int a = 1;
      while ((line = infileregex.ReadLine()) != null)
      {
        try
        {
          regexlist.Add(new Regex(line, RegexOptions.IgnoreCase));
          a += 1;
        }
        catch
        {
          writeLog("Died trying to ingest line number " + a.ToString() + " of opfregex.txt.", EventLogEntryType.Error);
        }
      }
      infileregex.Close();
    }

    private void ReadInContFile()
    {
      string line;
      contmtime = File.GetLastWriteTimeUtc(pathcont);
      StreamReader infilecont = new StreamReader(pathcont);
      contlist = new List<string>();
      int a = 1;
      while ((line = infilecont.ReadLine()) != null)
      {
        try
        {
          contlist.Add(line.ToLower());
          a += 1;
        }
        catch
        {
          writeLog("Died trying to ingest line number " + a.ToString() + " of opfcont.txt.", EventLogEntryType.Error);
        }
      }
      infilecont.Close();
    }

    private void ReadInMatchFile()
    {
      string line;
      matchmtime = File.GetLastWriteTimeUtc(pathmatch);
      StreamReader infilematch = new StreamReader(pathmatch);
      matchlist = new HashSet<string>();
      int a = 1;
      while ((line = infilematch.ReadLine()) != null)
      {
        try
        {
          matchlist.Add(line.ToLower());
          a += 1;
        }
        catch
        {
          writeLog("Died trying to ingest line number " + a.ToString() + " of opfmatch.txt", EventLogEntryType.Error);
        }
      }
      infilematch.Close();
    }

    private void CheckFileFreshness()
    {
      if (matchmtime != File.GetLastWriteTimeUtc(pathmatch))
      {
        writeLog("OPFMatch.txt has changed. Rereading now...", EventLogEntryType.Information);
        ReadInMatchFile();
      }
      if (contmtime != File.GetLastWriteTimeUtc(pathcont))
      {
        writeLog("OPFCont.txt has changed. Rereading now...", EventLogEntryType.Information);
        ReadInContFile();
      }
      if (regexmtime != File.GetLastWriteTimeUtc(pathregex))
      {
        writeLog("OPFRegex.txt has changed. Rereading now...", EventLogEntryType.Information);
        ReadInRegexFile();
      }
    }

    public Boolean contains(string word, string username)
    {
      CheckFileFreshness();
      foreach (string badstr in contlist)
      {
        if (word.ToLower().Contains(badstr))
        {
          writeLog("Password attempt contains poison string " + badstr + ", case insensitive.", EventLogEntryType.Information);
          return true;
        }
      }

      if (matchlist.Contains(word.ToLower()))
      {
        writeLog("Password attempt matched a string in the bad password list", EventLogEntryType.Information);
        return true;
      }

      foreach (Regex r in regexlist)
      {
        Match m = r.Match(word);
        if (m.Success)
        {
          writeLog("Password attempt matched regular expression " + r.ToString(), EventLogEntryType.Information);
          return true;
        }
      }

      Dictionary<string, string> namedict = new Dictionary<string, string>();
      using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
      {
        using (UserPrincipal user = UserPrincipal.FindByIdentity(context, username))
        {
          if (user != null)
          {
            namedict.Add("full name", user.DisplayName);
            namedict.Add("given name", user.GivenName);
            namedict.Add("surname", user.Surname);
            namedict.Add("SAMAccountName", user.SamAccountName);
          }
        }
      }
      foreach (string key in namedict.Keys)
      {
        if (namedict[key] != null)
        {
          if (word.ToLower().Contains(namedict[key].ToLower()))
          {
            writeLog("Password attempt contained the user's " + key + ",", EventLogEntryType.Information);
            return true;
          }
        }
      }
      writeLog("Password passed custom filter.", EventLogEntryType.Information);
      return false;
    }
  }
}
