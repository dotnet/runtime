// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    public class ParameterValidationTest
    {
        // The following method is invoked by the RemoteCertificateValidationDelegate.
        public bool AllowAnyServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            return true;  // allow everything
        }

        [Fact]
        public async Task SslStreamConstructor_BadEncryptionPolicy_ThrowException()
        {
            using (var _remoteServer = new DummyTcpServer(
                new IPEndPoint(IPAddress.Loopback, 0), EncryptionPolicy.RequireEncryption))
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(_remoteServer.RemoteEndPoint.Address, _remoteServer.RemoteEndPoint.Port);

                AssertExtensions.Throws<ArgumentException>("encryptionPolicy", () =>
                {
                    SslStream sslStream = new SslStream(client.GetStream(), false, AllowAnyServerCertificate, null, (EncryptionPolicy)100);
                });
            }
        }
    }
}

