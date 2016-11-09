// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Security.Tests
{
    public class HttpsTestServer
    {
        private readonly X509Certificate _serverCertificate;
        private int _port;
        private readonly SslProtocols _protocols;
        private TcpListener _listener;
        private VerboseTestLogging _log;

        private const string ResponseString =
@"HTTP/1.1 200 OK
Connection: close

<html>
<head>
<title>Test Server</title>
</head>
<body>
<h1>TLS test server</h1>
</body>
</html>
";

        public HttpsTestServer(X509Certificate serverCertificate, SslProtocols acceptedProtocols = SslProtocols.Tls)
        {
            _serverCertificate = serverCertificate;
            _protocols = acceptedProtocols;
            _log = VerboseTestLogging.GetInstance();
            AuxRecordDetected = false;
        }

        public bool AuxRecordDetected
        {
            get;
            private set;
        }

        public bool IsAuxRecordDetectionInconclusive
        {
            get;
            private set;
        }

        public SslProtocols SslProtocol
        {
            get;
            private set;
        }

        public int Port
        {
            get
            {
                if (_port == 0)
                {
                    throw new NotSupportedException("Server is not bound to a port.");
                }

                return _port;
            }
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        /// <returns>The local port that the server is bound to.</returns>
        public void StartServer()
        {
            if (_listener != null)
            {
                throw new InvalidOperationException("Cannot restart server.");
            }

            IPAddress address = IPAddress.Loopback;
            _listener = new TcpListener(address, _port);

            _listener.Start(1);

            _log.WriteLine("[Server] waiting for connections ({0}:{1})", address, _port);
            _port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        }

        public async Task RunTest()
        {
            bool done = false;

            while (!done)
            {
                try
                {
                    using (TcpClient requestClient = await _listener.AcceptTcpClientAsync())
                    {
                        _log.WriteLine("[Server] Client connected.");

                        using (var tls = new SslStream(requestClient.GetStream()))
                        {
                            await tls.AuthenticateAsServerAsync(
                                _serverCertificate,
                                false,
                                _protocols,
                                false);

                            _log.WriteLine("[Server] Client authenticated.");

                            done = await HttpConversation(tls);
                        }
                    }
                }
                catch (IOException)
                {
                    // Ignore I/O issues as browsers attempt to connect only to detect crypto information.
                }
            }

            _listener.Stop();
        }

        private async Task<bool> HttpConversation(SslStream tls)
        {
            int totalBytesRead = 0;
            int chunks = 0;

            SslProtocol = tls.SslProtocol;

            while (totalBytesRead < 5)
            {
                var requestBuffer = new byte[2048];
                int bytesRead = await tls.ReadAsync(requestBuffer, 0, requestBuffer.Length);
                totalBytesRead += bytesRead;

                string requestString = Encoding.UTF8.GetString(requestBuffer, 0, bytesRead);

                _log.WriteLine("[Server] Received {0} bytes: <<<{1}>>>", bytesRead, requestString);
                if (bytesRead == 0)
                {
                    return false;
                }

                if (bytesRead == 1 && chunks == 0)
                {
                    AuxRecordDetected = true;
                }

                chunks++;
            }

            _log.WriteLine("[Server] Using cipher {0}", tls.CipherAlgorithm);

            // Test is inconclusive if any non-CBC cipher is used:
            if (tls.CipherAlgorithm == CipherAlgorithmType.None ||
                tls.CipherAlgorithm == CipherAlgorithmType.Null ||
                tls.CipherAlgorithm == CipherAlgorithmType.Rc4)
            {
                IsAuxRecordDetectionInconclusive = true;
            }

            byte[] responseBuffer = Encoding.UTF8.GetBytes(ResponseString);
            await tls.WriteAsync(responseBuffer, 0, responseBuffer.Length);
            _log.WriteLine("[Server] Replied with {0} bytes.", responseBuffer.Length);
            return true;
        }
    }
}
