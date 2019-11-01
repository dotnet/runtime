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
using System.Net.Sockets;
using System.Text;

namespace System.Net.Mail.Tests
{
    public class SmtpServer
    {
        private string _mailfrom, _mailto, _subject, _body, _clientdomain;

        public string MailFrom => _mailfrom;
        public string MailTo => _mailto;
        public string Subject => _subject;
        public string Body => _body;
        public string ClientDomain => _clientdomain;

        private readonly TcpListener _server;

        public IPEndPoint EndPoint
        {
            get { return (IPEndPoint)_server.LocalEndpoint; }
        }

        public bool SupportSmtpUTF8 { get; set; }

        public SmtpServer()
        {
            IPAddress address = IPAddress.Loopback;
            _server = new TcpListener(address, 0);
            _server.Start(1);
        }

        private static void WriteNS(NetworkStream ns, string s)
        {
            Trace("response", s);
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            ns.Write(bytes, 0, bytes.Length);
        }

        public void Stop()
        {
            _server.Stop();
        }

        public void Run()
        {
            try
            {
                string s;
                using (TcpClient client = _server.AcceptTcpClient())
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
            catch (SocketException e)
            {
                // The _server might have been stopped.
                if (e.SocketErrorCode != SocketError.Interrupted)
                    throw;
            }
        }

        // return false == terminate
        private bool Dispatch(NetworkStream ns, StreamReader r, string s)
        {
            Trace("command", s);
            if (s.Length < 4)
            {
                WriteNS(ns, "502 Unrecognized\r\n");
                return false;
            }

            bool retval = true;
            switch (s.Substring(0, 4))
            {
                case "HELO":
                    _clientdomain = s.Substring(5).Trim().ToLower();
                    break;
                case "EHLO":
                    _clientdomain = s.Substring(5).Trim().ToLower();
                    WriteNS(ns, "250-localhost Hello" + s.Substring(5, s.Length - 5) + "\r\n");
                    WriteNS(ns, "250-AUTH PLAIN\r\n");
                    if (SupportSmtpUTF8)
                    {
                        WriteNS(ns, "250-SMTPUTF8\r\n");
                    }
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
                    while ((s = r.ReadLine()) != null)
                    {
                        if (s == ".")
                            break;

                        if (s.StartsWith("Subject:"))
                        {
                            _subject = s.Substring(9, s.Length - 9);
                        }
                        else if (s == "" && _body == null)
                        {
                            _body = r.ReadLine();
                        }
                    }
                    Trace("end of data", s);
                    retval = (s != null);
                    break;
                default:
                    WriteNS(ns, "502 Unrecognized\r\n");
                    return true;
            }

            WriteNS(ns, "250 OK\r\n");
            return retval;
        }

        [Conditional("TEST")]
        private static void Trace(string key, object value)
        {
            Console.Error.WriteLine("{0}: {1}", key, value);
        }
    }
}
