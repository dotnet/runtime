// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SchSendAuxRecordTest
    {
        readonly ITestOutputHelper _output;

        public SchSendAuxRecordTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task SslStream_ClientAndServerUsesAuxRecord_Ok()
        {
            X509Certificate2 serverCert = Configuration.Certificates.GetServerCertificate();
            var server = new HttpsTestServer(serverCert);

            server.StartServer();
            int port = server.Port; 
            var client = new SchSendAuxRecordTestClient("localhost", port);

            var tasks = new Task[2];
            tasks[0] = server.RunTest();
            tasks[1] = client.RunTest();

            await Task.WhenAll(tasks).TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);
            
            if (server.IsAuxRecordDetectionInconclusive)
            {
                _output.WriteLine("Test inconclusive: The Operating system preferred a non-CBC or Null cipher.");
            }
            else
            {
                Assert.True(server.AuxRecordDetected, "Server reports: Client auxiliary record not detected.");
                Assert.True(client.AuxRecordDetected, "Client reports: Server auxiliary record not detected.");
            }
        }

        public class SchSendAuxRecordTestClient
        {
            private readonly string _server;
            private readonly int _port;
            private VerboseTestLogging _log;

            public SchSendAuxRecordTestClient(string server, int port)
            {
                _server = server;
                _port = port;
                _log = VerboseTestLogging.GetInstance();
            }

            public bool AuxRecordDetected
            {
                get;
                private set;
            }

            public async Task RunTest()
            {
                using (var tcp = new TcpClient())
                {
                    _log.WriteLine("[Client] Connecting to {0}:{1}", _server, _port);
                    await tcp.ConnectAsync(_server, _port);
                    using (var tls = new SslStream(tcp.GetStream(), false, CertificateValidation))
                    {
                        _log.WriteLine("[Client] Connected. Authenticating...");
                        await tls.AuthenticateAsClientAsync(_server, null, System.Security.Authentication.SslProtocols.Tls, false);

                        string requestString = "GET / HTTP/1.0\r\nHost: servername.test.contoso.com\r\nUser-Agent: Testing application\r\n\r\n";
                        byte[] requestBuffer = Encoding.UTF8.GetBytes(requestString);

                        _log.WriteLine("[Client] Sending request ({0} Bytes)", requestBuffer.Length);

                        await tls.WriteAsync(requestBuffer, 0, requestBuffer.Length);

                        _log.WriteLine("[Client] Waiting for reply...");

                        int bytesRead = 0;
                        int chunks = 0;
                        do
                        {
                            byte[] responseBuffer = new byte[2048];
                            bytesRead = await tls.ReadAsync(responseBuffer, 0, responseBuffer.Length);

                            if (bytesRead > 0)
                            {
                                string responseString = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
                                _log.WriteLine("[Client {0}: {2} Bytes] Response: <<<<<{1}>>>>>", chunks, responseString, bytesRead);
                            }

                            if (bytesRead == 1 && chunks == 0)
                            {
                                AuxRecordDetected = true;
                            }

                            chunks++;
                        }
                        while (bytesRead > 0);
                    }
                }
            }

            private bool CertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                // Accept all certificate errors.
                return true;
            }
        }
    }
}
