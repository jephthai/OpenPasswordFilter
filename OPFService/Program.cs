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
using System.Threading;
using System.ServiceProcess;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;

namespace OPFService
{
  class OPFService : ServiceBase
  {
    Thread worker;
    Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public OPFService()
    {
    }
    private void writeLog(string message, System.Diagnostics.EventLogEntryType level)
    {
      using (EventLog eventLog = new EventLog("Application"))
      {
        eventLog.Source = "Application";
        eventLog.WriteEntry(message, level, 100, 1);
      }
    }


    static void Main(string[] args)
    {
      ServiceBase.Run(new OPFService());
    }

    protected override void OnStart(string[] args)
    {
      base.OnStart(args);
      string OPFSysVolPath = "\\\\127.0.0.1\\SysVol\\" + System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName + "\\OPF\\";
      OPFDictionary d = new OPFDictionary(
          OPFSysVolPath + "opfmatch.txt",
          OPFSysVolPath + "opfcont.txt",
          OPFSysVolPath + "opfregex.txt");
      OPFGroup g = new OPFGroup(OPFSysVolPath + "opfgroups.txt");  // restrict password filter to users in these groups.
      NetworkService svc = new NetworkService(d, g);
      worker = new Thread(() => svc.main(listener));
      worker.Start();
    }

    protected override void OnShutdown()
    {
      base.OnShutdown();
      //listener.Shutdown(SocketShutdown.Both);
      worker.Abort();
    }
    //listener.accept blocks in the worker thread, so service restart doesn't kill the process in a timely manner
    //this causes the new instance to be unable to bind to the local port
    //move socket construction out here so we can override OnStop to forcibly close the socket
    protected override void OnStop()
    {
      base.OnStop();
      writeLog("Stopping OpenPasswordFilter Service...", EventLogEntryType.Information);
      listener.Close();
      worker.Abort();
    }

    private void InitializeComponent()
    {
      // 
      // OPFService
      // 
      this.ServiceName = "OPF";

    }
  }
}
