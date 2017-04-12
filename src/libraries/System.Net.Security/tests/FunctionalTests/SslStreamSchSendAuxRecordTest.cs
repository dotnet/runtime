// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Security.Authentication;
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

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task SslStream_ClientAndServerUsesAuxRecord_Ok()
        {
            using (var server = new HttpsTestServer())
            {
                server.Start();

                var tasks = new Task[2];

                bool serverAuxRecordDetected = false;
                bool serverAuxRecordDetectedInconclusive = false;
                int serverTotalBytesReceived = 0;
                int serverChunks = 0;

                tasks[0] = server.AcceptHttpsClientAsync((requestString) =>
                {
                    serverTotalBytesReceived += requestString.Length;
                
                    if (serverTotalBytesReceived == 1 && serverChunks == 0)
                    {
                        serverAuxRecordDetected = true;
                    }

                    serverChunks++;
                    
                    // Test is inconclusive if any non-CBC cipher is used:
                    if (server.Stream.CipherAlgorithm == CipherAlgorithmType.None ||
                        server.Stream.CipherAlgorithm == CipherAlgorithmType.Null ||
                        server.Stream.CipherAlgorithm == CipherAlgorithmType.Rc4)
                    {
                        serverAuxRecordDetectedInconclusive = true;
                    }

                    if (serverTotalBytesReceived < 5)
                    {
                        return Task.FromResult<string>(null);
                    }
                    else
                    {
                        return Task.FromResult(HttpsTestServer.Options.DefaultResponseString);
                    }
                });


                var clientOptions = new HttpsTestClient.Options(new IPEndPoint(IPAddress.Loopback, server.Port));
                clientOptions.AllowedProtocols = SslProtocols.Tls;

                clientOptions.IgnoreSslPolicyErrors = 
                    SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors;

                var client = new HttpsTestClient(clientOptions);

                bool clientAuxRecordDetected = false;
                bool clientAuxRecordDetectedInconclusive = false;
                int clientTotalBytesReceived = 0;
                int clientChunks = 0;

                tasks[1] = client.HttpsRequestAsync((responseString) =>
                {
                    if (responseString == null)
                    {
                        string requestString = string.Format(
                            HttpsTestClient.Options.DefaultRequestStringTemplate, 
                            clientOptions.ServerName);

                        return Task.FromResult(requestString);
                    }

                    clientTotalBytesReceived += responseString.Length;

                    if (clientTotalBytesReceived == 1 && clientChunks == 0)
                    {
                        clientAuxRecordDetected = true;
                    }

                    // Test is inconclusive if any non-CBC cipher is used:
                    if (client.Stream.CipherAlgorithm == CipherAlgorithmType.None ||
                        client.Stream.CipherAlgorithm == CipherAlgorithmType.Null ||
                        client.Stream.CipherAlgorithm == CipherAlgorithmType.Rc4)
                    {
                        clientAuxRecordDetectedInconclusive = true;
                    }

                    return Task.FromResult<string>(null);
                });

                await Task.WhenAll(tasks).TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);

                if (serverAuxRecordDetectedInconclusive || clientAuxRecordDetectedInconclusive)
                {
                    _output.WriteLine("Test inconclusive: The Operating system preferred a non-CBC or Null cipher.");
                }
                else
                {
                    Assert.True(serverAuxRecordDetected, "Server reports: Client auxiliary record not detected.");
                    Assert.True(clientAuxRecordDetected, "Client reports: Server auxiliary record not detected.");
                }
            }
        }
    }
}
