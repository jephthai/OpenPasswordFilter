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
using System.DirectoryServices.AccountManagement;

namespace OPFService {
  class OPFGroup {
    List<string> grouplist;

    public void writeLog(string message, System.Diagnostics.EventLogEntryType level) {
      using (EventLog eventLog = new EventLog("Application")) {
        eventLog.Source = "Application";
        eventLog.WriteEntry(message, level, 101, 1);
      }
    }

    public OPFGroup(string pathgroups) {
      string line;

      StreamReader infilematch = new StreamReader(pathgroups);
      grouplist = new List<string>();
      int a = 1;
      while ((line = infilematch.ReadLine()) != null) {
        try {
          grouplist.Add(line.ToLower());
          writeLog("groups are: " + line, EventLogEntryType.Information);
          a += 1;
        } catch {
          writeLog("Died trying to ingest line number " + a.ToString() + " of groups file.", EventLogEntryType.Error);
        }
      }
      infilematch.Close();
      writeLog("Succesfully read " + (a - 1).ToString() + " groups.", EventLogEntryType.Information);
    }

    public Boolean contains(string username) {

      // if the groups file is empty then we always check the passwords
      if (grouplist.Count == 0) {
        writeLog("No groups found. User's password will be validated.", EventLogEntryType.Information);
        return true;
      }

      PrincipalContext ctx = null;
      GroupPrincipal groupCtx = null;

      ctx = new System.DirectoryServices.AccountManagement.PrincipalContext(ContextType.Domain);

      foreach (String groupname in grouplist) {
        //writeLog("trying [" + groupname + "]", EventLogEntryType.Information);
        groupCtx = GroupPrincipal.FindByIdentity(ctx, groupname);
        if (groupCtx != null) {
          //writeLog("found [" + groupCtx.ToString() + "]. Finding members", EventLogEntryType.Information);
          foreach (Principal user in groupCtx.GetMembers(true)) {
            if (user.SamAccountName == username) {
              writeLog("User " + username + " is in restricted group " + groupname + " and their password will be validated.", EventLogEntryType.Information);
              ctx.Dispose();
              groupCtx.Dispose();
              return true;
            }
          }
          groupCtx.Dispose();
        }
      }
      ctx.Dispose();
      writeLog("User " + username + " is not in a restricted group", EventLogEntryType.Information);
      return false;
    }
  }
}
