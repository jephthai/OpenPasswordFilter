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
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;



namespace OPFService {
  class NetworkService {
    OPFDictionary dict;
    OPFGroup group;
    private void writeLog(string message, System.Diagnostics.EventLogEntryType level) {
      using (EventLog eventLog = new EventLog("Application")) {
        eventLog.Source = "Application";
        eventLog.WriteEntry(message, level, 100, 1);
      }
    }

    public NetworkService(OPFDictionary d, OPFGroup g) {
      dict = d;
      group = g; 
    }

    public void main() {
      IPAddress ip = IPAddress.Parse("127.0.0.1");
      IPEndPoint local = new IPEndPoint(ip, 5999);
      Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

      try {
        listener.Bind(local);
        listener.Listen(64);
        writeLog("OpenPasswordFilter is now running.", EventLogEntryType.Information);
        while (true) {
          Socket client = listener.Accept();
          new Thread(() => handle(client)).Start();
        }
      } catch (Exception e) {
        // don't know what to do here
      }
    }

    /*
     * if (user is in one of the listed groups or there are no groups listed) then
     *   if username/password combo is not acceptable then return "false" 
     * return "true"
     * */
    public void handle(Socket client) {
      try {
        NetworkStream netStream = new NetworkStream(client);
        StreamReader istream = new StreamReader(netStream);
        StreamWriter ostream = new StreamWriter(netStream);
        string command = istream.ReadLine();
        if (command == "test") {
          string username = istream.ReadLine();
          string password = istream.ReadLine();
          writeLog("Validating password for user " + username, EventLogEntryType.Information);
          if (group.contains(username)) {
            writeLog("User in a restricted group", EventLogEntryType.Information);
            ostream.WriteLine(dict.contains(password) ? "false" : "true");
          } else {
            ostream.WriteLine("true");
          }
          ostream.Flush();
        } else {
          ostream.WriteLine("ERROR");
          ostream.Flush();
        }
      } catch (Exception e) {

      }
      client.Close();
    }
  }
}
