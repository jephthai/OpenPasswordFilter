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


namespace OPFService {
    class NetworkService {
        OPFDictionary dict;
        Socket listener;
        int restarted = 0;

        public NetworkService(OPFDictionary d) {
            dict = d;
        }

        public void main() {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            IPEndPoint local = new IPEndPoint(ip, 5999);
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(local);
                listener.Listen(64);
                while (true)
                {
                    Socket client = listener.Accept();
                    new Thread(() => handle(client)).Start();
                }
            }
            catch (Exception e)
            {
                String exception = e.ToString();
                //On a not sane restart, the Socket keeps the connection up for 30 seconds
                //try a restart by calling main() again
                if (exception.Contains("Only one usage of each socket address") && restarted < 4)
                {
                    restarted++;
                    System.Threading.Thread.Sleep(30000);
                    main();
                }
                else {
                    throw; //shut the service down
                }
            }
        }

        public void handle(Socket client) {
            try {
                NetworkStream netStream = new NetworkStream(client);
                StreamReader istream = new StreamReader(netStream);
                StreamWriter ostream = new StreamWriter(netStream);
                string command = istream.ReadLine();
                if (command == "test") {
                    string password = istream.ReadLine();
                    bool containsPassword = dict.contains(password);
                    ostream.WriteLine(containsPassword ? "false" : "true");
                    ostream.Flush();
                } else {
                    ostream.WriteLine("ERROR");
                    ostream.Flush();
                }
            } catch (Exception e) {

            }
            client.Close();
        }

        public void Close()
        {
            restarted = 0; //reset the counter
            try
            {
                listener.Close();
            }
            catch (Exception e)
            {
            }
        }
    }
}
