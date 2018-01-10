// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamAllowRenegotiationTests
    { 
        [Fact]
        [OuterLoop] // Test hits external azure server.
        public async Task SslStream_AllowRenegotiation_True_Succeeds()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await s.ConnectAsync(Configuration.Security.TlsRenegotiationServer, 443);
            using (NetworkStream ns = new NetworkStream(s))
            using (SslStream ssl = new SslStream(ns, true))
            {
                X509CertificateCollection certBundle = new X509CertificateCollection();
                certBundle.Add(Configuration.Certificates.GetClientCertificate());

                SslClientAuthenticationOptions options = new SslClientAuthenticationOptions
                {
                    TargetHost = Configuration.Security.TlsRenegotiationServer,
                    ClientCertificates = certBundle,
                    EnabledSslProtocols = SslProtocols.Tls12,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    AllowRenegotiation = true
                };

                // Perform handshake to establish secure connection.
                await ssl.AuthenticateAsClientAsync(options, CancellationToken.None);
                Assert.True(ssl.IsAuthenticated);
                Assert.True(ssl.IsEncrypted);

                // Issue request that triggers regotiation from server.
                byte[] message = Encoding.UTF8.GetBytes("GET /EchoClientCertificate.ashx HTTP/1.1\r\nHost: corefx-net-tls.azurewebsites.net\r\n\r\n");
                await ssl.WriteAsync(message, 0, message.Length);

                // Initiate Read operation, that results in starting renegotiation as per server response to the above request.
                int bytesRead = await ssl.ReadAsync(message, 0, message.Length);

                // SslStream.ReadAsync should return -1 bytes, indicating renegotiation operation is in progress.
                Assert.Equal(-1, bytesRead);

                // Do another Read, to get the HTTP response from the server, after successful renegotiation.
                bytesRead = await ssl.ReadAsync(message, 0, message.Length);
                Assert.InRange(bytesRead, 1, message.Length);
                Assert.Contains("HTTP/1.1 200 OK", Encoding.UTF8.GetString(message));
            }
        }

        [Fact]
        [OuterLoop] // Test hits external azure server.
        public async Task SslStream_AllowRenegotiation_False_Throws()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await s.ConnectAsync(Configuration.Security.TlsRenegotiationServer, 443);
            using (NetworkStream ns = new NetworkStream(s))
            using (SslStream ssl = new SslStream(ns, true))
            {
                X509CertificateCollection certBundle = new X509CertificateCollection();
                certBundle.Add(Configuration.Certificates.GetClientCertificate());

                SslClientAuthenticationOptions options = new SslClientAuthenticationOptions
                {
                    TargetHost = Configuration.Security.TlsRenegotiationServer,
                    ClientCertificates = certBundle,
                    EnabledSslProtocols = SslProtocols.Tls12,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    AllowRenegotiation = false
                };

                // Perform handshake to establish secure connection.
                await ssl.AuthenticateAsClientAsync(options, CancellationToken.None);
                Assert.True(ssl.IsAuthenticated);
                Assert.True(ssl.IsEncrypted);

                // Issue request that triggers regotiation from server.
                byte[] message = Encoding.UTF8.GetBytes("GET /EchoClientCertificate.ashx HTTP/1.1\r\nHost: corefx-net-tls.azurewebsites.net\r\n\r\n");
                await ssl.WriteAsync(message, 0, message.Length);

                // Initiate Read operation, that results in starting renegotiation as per server response to the above request.
                // This will throw IOException, since renegotiation is disabled on client side.
                await Assert.ThrowsAsync<IOException>(() => ssl.ReadAsync(message, 0, message.Length));
            }
        }
    }
}
