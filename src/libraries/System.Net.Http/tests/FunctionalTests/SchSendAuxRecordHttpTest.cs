// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security.Tests;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SchSendAuxRecordHttpTest
    {
        readonly ITestOutputHelper _output;
        
        public SchSendAuxRecordHttpTest(ITestOutputHelper output)
        {
            _output = output;
        }
        
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task HttpClient_ClientUsesAuxRecord_Ok()
        {
            X509Certificate2 serverCert = Configuration.Certificates.GetServerCertificate();

            var server = new SchSendAuxRecordTestServer(serverCert);
            int port = server.StartServer();

            string requestString = "https://localhost:" + port.ToString();
            
            using (var handler = new HttpClientHandler() { ServerCertificateCustomValidationCallback = LoopbackServer.AllowAllCertificates })
            using (var client = new HttpClient(handler))
            {
                var tasks = new Task[2];
                tasks[0] = server.RunTest();
                tasks[1] = client.GetStringAsync(requestString);
            
                await Task.WhenAll(tasks).TimeoutAfter(15 * 1000);
            
                if (server.IsInconclusive)
                {
                    _output.WriteLine("Test inconclusive: The Operating system preferred a non-CBC or Null cipher.");
                }
                else
                {
                    Assert.True(server.AuxRecordDetected, "Server reports: Client auxiliary record not detected.");
                }
            }
        }
    }
}
