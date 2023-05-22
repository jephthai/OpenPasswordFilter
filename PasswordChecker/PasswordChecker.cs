using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Globalization;

namespace PasswordCheckerRay
{
    public partial class Service1 : ServiceBase
    {
        Socket listener;
        public Service1()
        {
            InitializeComponent();
        }

        internal void TestStartupAndStop(string[] args)
        {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "PasswordCheckerRay.Service1.WriteToFile(System.String)")]
        protected override void OnStart(string[] args)
        {
            PasswordCheckerRay.PasswordIndexer.IndexFile();
            WriteToFile("on start, opening socket on 127.0.0.1:5999");
            EventLog.WriteEntry("on start, opening socket on 127.0.0.1:5999", EventLogEntryType.Information);
            try
            {
                IPAddress ip = IPAddress.Parse("127.0.0.1");
                IPEndPoint local = new IPEndPoint(ip, 5999);
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(local);
                listener.Listen(64);
                new Thread(() => WaitForConnections()).Start();
            }
            catch (Exception ex)
            {
                WriteToFile(string.Format("Error {0}", ex.Message + ex.StackTrace));
                throw;
            }
            EventLog.WriteEntry("ending on start", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            listener.Close();
        }

        public void WaitForConnections()
        {
            while (listener.IsBound)
            {
                try {
                    Socket client = listener.Accept();
                    HandleConnection(client);
                    // EventLog.WriteEntry("handled connection", EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    WriteToFile(string.Format("Error {0}", ex.Message + ex.StackTrace));
                    throw;
                }
            }
        }


       
        public void HandleConnection(Socket client)
        {
            NetworkStream netStream = new NetworkStream(client);
            try
            {
                StreamReader istream = new StreamReader(netStream);
                StreamWriter ostream = new StreamWriter(netStream);
                string command = istream.ReadLine();
                if (command != "test")
                {
                    WriteToFile("invalid command on socket: " + command);
                    ostream.WriteLine("ERROR");
                    ostream.Flush();
                }
                else
                {
                    string password = istream.ReadLine();

                    bool does_exist = PwDBRay.Exists(password);
                    if (!does_exist)
                    {
                        does_exist = PwDBSha256.Exists(password);
                    }
                   
                    if (does_exist)
                    {
                        ostream.WriteLine("false");
                        ostream.Flush();
                        EventLog.WriteEntry("password change refused - common password", EventLogEntryType.FailureAudit);
                    }
                    else
                    {
                        ostream.WriteLine("true");
                        ostream.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                
                WriteToFile("Could not communicate on socket: " + e.Message);
                EventLog.WriteEntry("Could not communicate on socket: " + e.Message, EventLogEntryType.FailureAudit);
                throw;
            }
            finally {
                netStream.Close();
                client.Close();
            }

            
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object,System.Object)")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.DateTime.ToString(System.String)")]
        static private void WriteToFile(string text)
        {
            string path = "C:\\passwordchecker_log.txt";
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                    string line;
                    line = string.Format("{0}\t{1}", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"), text);
                    writer.WriteLine(line);
            }
        }
    }
}
