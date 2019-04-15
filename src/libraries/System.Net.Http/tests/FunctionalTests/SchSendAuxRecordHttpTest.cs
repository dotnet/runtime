// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Security.Authentication;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    [ActiveIssue(26539)]    // Flaky test
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "HttpsTestServer not compatible on UAP")]
    public abstract class SchSendAuxRecordHttpTest : HttpClientHandlerTestBase
    {
        public SchSendAuxRecordHttpTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task HttpClient_ClientUsesAuxRecord_Ok()
        {
            var options = new HttpsTestServer.Options();
            options.AllowedProtocols = SslProtocols.Tls;

            using (var server = new HttpsTestServer(options))
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
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

                string requestUriString = "https://localhost:" + server.Port.ToString();
                tasks[1] = client.GetStringAsync(requestUriString);

                await tasks.WhenAllOrAnyFailed(15 * 1000);

                if (serverAuxRecordDetectedInconclusive)
                {
                    _output.WriteLine("Test inconclusive: The Operating system preferred a non-CBC or Null cipher.");
                }
                else
                {
                    Assert.True(serverAuxRecordDetected, "Server reports: Client auxiliary record not detected.");
                }
            }
        }
    }
}
