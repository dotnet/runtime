// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    public class CertificateValidationRemoteServer
    {
        [Fact]
        public async Task CertificateValidationRemoteServer_EndToEnd_Ok()
        {
            using (var client = new TcpClient(AddressFamily.InterNetwork))
            {
                await client.ConnectAsync(TestConfiguration.HttpsTestServer, 443);

                using (SslStream sslStream = new SslStream(client.GetStream(), false, RemoteHttpsCertValidation, null))
                {
                    await sslStream.AuthenticateAsClientAsync(TestConfiguration.HttpsTestServer);
                }
            }
        }

        private bool RemoteHttpsCertValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Assert.Equal(SslPolicyErrors.None, sslPolicyErrors);
            
            return true;
        }
     }
}
