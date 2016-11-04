// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.IO;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamAlertsTest
    {
        private const uint SEC_E_ILLEGAL_MESSAGE = 0x80090326;
        private const uint SEC_E_CERT_UNKNOWN = 0x80090327;

        [Fact]
        [ActiveIssue(12706, TestPlatforms.Windows)]
        [ActiveIssue(12319, TestPlatforms.AnyUnix)]
        public async Task SslStream_StreamToStream_HandshakeAlert_Ok()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new SslStream(clientStream, true, AllowAnyServerCertificate))
            using (var server = new SslStream(serverStream, true, FailClientCertificate))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                Task serverAuth = server.AuthenticateAsServerAsync(certificate);
                await client.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false));

                byte[] buffer = new byte[1024];

                // Schannel semantics require that Decrypt is called to receive an alert.
                await client.WriteAsync(buffer, 0, buffer.Length);
                var exception = await Assert.ThrowsAsync<IOException>(() => client.ReadAsync(buffer, 0, buffer.Length));

                Assert.IsType<Win32Exception>(exception.InnerException);
                var win32ex = (Win32Exception)exception.InnerException;

                // The Schannel HResults for each alert are documented here: 
                // https://msdn.microsoft.com/en-us/library/windows/desktop/dd721886(v=vs.85).aspx
                Assert.Equal(SEC_E_CERT_UNKNOWN, (uint)win32ex.NativeErrorCode);

                await Assert.ThrowsAsync<AuthenticationException>(() => serverAuth);

                await Assert.ThrowsAsync<AuthenticationException>(() => server.WriteAsync(buffer, 0, buffer.Length));
                await Assert.ThrowsAsync<AuthenticationException>(() => server.ReadAsync(buffer, 0, buffer.Length));
            }
        }

        [Fact]
        [ActiveIssue(12683, TestPlatforms.Windows)]
        [ActiveIssue(12319, TestPlatforms.AnyUnix)]
        public async Task SslStream_StreamToStream_ServerInitiatedCloseNotify_Ok()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new SslStream(clientStream, true, AllowAnyServerCertificate))
            using (var server = new SslStream(serverStream))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                var handshake = new Task[2];

                handshake[0] = server.AuthenticateAsServerAsync(certificate);
                handshake[1] = client.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false));

                await Task.WhenAll(handshake).TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);

                var readBuffer = new byte[1024];

                await server.ShutdownAsync();
                int bytesRead = await client.ReadAsync(readBuffer, 0, readBuffer.Length);
                // close_notify received by the client.
                Assert.Equal(0, bytesRead);

                await client.ShutdownAsync();
                bytesRead = await server.ReadAsync(readBuffer, 0, readBuffer.Length);
                // close_notify received by the server.
                Assert.Equal(0, bytesRead);
            }
        }

        [Fact]
        [ActiveIssue(12319, TestPlatforms.AnyUnix)]
        public async Task SslStream_StreamToStream_ClientInitiatedCloseNotify_Ok()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new SslStream(clientStream, true, AllowAnyServerCertificate))
            using (var server = new SslStream(serverStream))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                var handshake = new Task[2];

                handshake[0] = server.AuthenticateAsServerAsync(certificate);
                handshake[1] = client.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false));

                await Task.WhenAll(handshake).TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);

                var readBuffer = new byte[1024];

                await client.ShutdownAsync();
                int bytesRead = await server.ReadAsync(readBuffer, 0, readBuffer.Length);
                // close_notify received by the server.
                Assert.Equal(0, bytesRead);

                await server.ShutdownAsync();
                bytesRead = await client.ReadAsync(readBuffer, 0, readBuffer.Length);
                // close_notify received by the client.
                Assert.Equal(0, bytesRead);
            }
        }
        
        [Fact]
        [ActiveIssue(12706, TestPlatforms.Windows)]
        [ActiveIssue(12319, TestPlatforms.AnyUnix)]
        public async Task SslStream_StreamToStream_DataAfterShutdown_Fail()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new SslStream(clientStream, true, AllowAnyServerCertificate))
            using (var server = new SslStream(serverStream))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                var handshake = new Task[2];

                handshake[0] = server.AuthenticateAsServerAsync(certificate);
                handshake[1] = client.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false));

                await Task.WhenAll(handshake).TimeoutAfter(TestConfiguration.PassingTestTimeoutMilliseconds);

                var buffer = new byte[1024];

                Assert.Equal(true, client.CanWrite);

                await client.ShutdownAsync();

                Assert.Equal(false, client.CanWrite);

                await Assert.ThrowsAsync<InvalidOperationException>(() => client.ShutdownAsync());
                await Assert.ThrowsAsync<InvalidOperationException>(() => client.WriteAsync(buffer, 0, buffer.Length));
            }
        }

        private bool FailClientCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return false;
        }

        private bool AllowAnyServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
