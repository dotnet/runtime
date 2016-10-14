// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// SmtpServer.cs - Dummy SMTP server used to test SmtpClient
//
// Author:
//   Raja R Harinath <harinath@hurrynot.org>
//

using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace System.Net.Mail.Tests
{
    public class SmtpServer
    {
        private string _mailfrom, _mailto;

        public string MailFrom => _mailfrom;
        public string MailTo => _mailto;

        public StringBuilder data;

        TcpListener server;
        public IPEndPoint EndPoint
        {
            get { return (IPEndPoint)server.LocalEndpoint; }
        }

        public SmtpServer()
        {
            IPAddress address = Dns.GetHostEntry("localhost").AddressList[0];
            server = new TcpListener(address, 0);
            server.Start(1);
        }

        private static void WriteNS(NetworkStream ns, string s)
        {
            Trace("response", s);
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            ns.Write(bytes, 0, bytes.Length);
        }

        public void Run()
        {
            string s;
            using (TcpClient client = server.AcceptTcpClient())
            {
                Trace("connection", EndPoint.Port);
                using (NetworkStream ns = client.GetStream())
                {
                    WriteNS(ns, "220 localhost\r\n");
                    using (StreamReader r = new StreamReader(ns, Encoding.UTF8))
                    {
                        while ((s = r.ReadLine()) != null && Dispatch(ns, r, s))
                            ;
                    }
                }
            }
        }

        // return false == terminate
        public bool Dispatch(NetworkStream ns, StreamReader r, string s)
        {
            Trace("command", s);
            if (s.Length < 4)
            {
                WriteNS(ns, "502 Huh\r\n");
                return false;
            }

            bool retval = true;
            switch (s.Substring(0, 4))
            {
                case "HELO":
                    break;
                case "QUIT":
                    WriteNS(ns, "221 Quit\r\n");
                    return false;
                case "MAIL":
                    _mailfrom = s.Substring(10);
                    break;
                case "RCPT":
                    _mailto = s.Substring(8);
                    break;
                case "DATA":
                    WriteNS(ns, "354 Continue\r\n");
                    data = new StringBuilder();
                    while ((s = r.ReadLine()) != null)
                    {
                        if (s == ".")
                            break;
                        data.AppendLine(s);
                    }
                    Trace("end of data", s);
                    retval = (s != null);
                    break;
                default:
                    WriteNS(ns, "502 Huh\r\n");
                    return true;
            }

            WriteNS(ns, "250 OK\r\n");
            return retval;
        }

        [Conditional("TEST")]
        static void Trace(string key, object value)
        {
            Console.Error.WriteLine("{0}: {1}", key, value);
        }
    }
}
