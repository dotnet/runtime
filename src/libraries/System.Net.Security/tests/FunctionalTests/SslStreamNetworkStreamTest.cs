// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamNetworkStreamTest : IDisposable
    {
        private readonly X509Certificate2 _serverCert;
        private readonly X509Certificate2Collection _serverChain;

        public SslStreamNetworkStreamTest()
        {
            (_serverCert, _serverChain) = TestHelper.GenerateCertificates("localhost", this.GetType().Name);
        }

        public void Dispose()
        {
            TestHelper.CleanupCertificates(this.GetType().Name);
        }

        [Fact]
        public async Task SslStream_SendReceiveOverNetworkStream_Ok()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);

            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            using (TcpClient client = new TcpClient())
            {
                listener.Start();

                Task clientConnectTask = client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
                Task<TcpClient> listenerAcceptTask = listener.AcceptTcpClientAsync();

                await Task.WhenAll(clientConnectTask, listenerAcceptTask);

                TcpClient server = listenerAcceptTask.Result;
                using (SslStream clientStream = new SslStream(
                    client.GetStream(),
                    false,
                    new RemoteCertificateValidationCallback(ValidateServerCertificate),
                    null,
                    EncryptionPolicy.RequireEncryption))
                using (SslStream serverStream = new SslStream(
                    server.GetStream(),
                    false,
                    null,
                    null,
                    EncryptionPolicy.RequireEncryption))
                {

                    Task clientAuthenticationTask = clientStream.AuthenticateAsClientAsync(
                        serverCertificate.GetNameInfo(X509NameType.SimpleName, false),
                        null,
                        SslProtocols.Tls12,
                        false);

                    Task serverAuthenticationTask = serverStream.AuthenticateAsServerAsync(
                        serverCertificate,
                        false,
                        SslProtocols.Tls12,
                        false);

                    await Task.WhenAll(clientAuthenticationTask, serverAuthenticationTask);

                    byte[] writeBuffer = new byte[256];
                    Task writeTask = clientStream.WriteAsync(writeBuffer, 0, writeBuffer.Length);

                    byte[] readBuffer = new byte[256];
                    Task<int> readTask = serverStream.ReadAsync(readBuffer, 0, readBuffer.Length);

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(writeTask, readTask);

                    Assert.InRange(readTask.Result, 1, 256);
                }
            }

            listener.Stop();
        }

        [ConditionalFact]
        [PlatformSpecific(TestPlatforms.Linux)] // This only applies where OpenSsl is used.
        public async Task SslStream_SendReceiveOverNetworkStream_AuthenticationException()
        {
            SslProtocols clientProtocol;
            SslProtocols serverProtocol;

            // Try to find protocol mismatch.
            if (PlatformDetection.SupportsTls12 && (PlatformDetection.SupportsTls10 || PlatformDetection.SupportsTls11))
            {
                // OpenSSL 1.0 where new is Tls12
                clientProtocol = SslProtocols.Tls | SslProtocols.Tls11;
                serverProtocol = SslProtocols.Tls12;
            }
            else if (PlatformDetection.SupportsTls12 && PlatformDetection.SupportsTls13)
            {
                // OpenSSl 1.1 where new is 1.3 and legacy is 1.2
                clientProtocol = SslProtocols.Tls13;
                serverProtocol = SslProtocols.Tls12;
            }
            else
            {
                throw new SkipTestException("Did not find disjoined sets");
            }

            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);

            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            using (TcpClient client = new TcpClient())
            {
                listener.Start();

                Task clientConnectTask = client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
                Task<TcpClient> listenerAcceptTask = listener.AcceptTcpClientAsync();

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(clientConnectTask, listenerAcceptTask);

                TcpClient server = listenerAcceptTask.Result;
                using (SslStream clientStream = new SslStream(
                    client.GetStream(),
                    false,
                    new RemoteCertificateValidationCallback(ValidateServerCertificate),
                    null,
                    EncryptionPolicy.RequireEncryption))
                using (SslStream serverStream = new SslStream(
                    server.GetStream(),
                    false,
                    null,
                    null,
                    EncryptionPolicy.RequireEncryption))
                {

                    Task clientAuthenticationTask = clientStream.AuthenticateAsClientAsync(
                        serverCertificate.GetNameInfo(X509NameType.SimpleName, false),
                        null,
                        clientProtocol,
                        false);

                    AuthenticationException e = await Assert.ThrowsAsync<AuthenticationException>(() => serverStream.AuthenticateAsServerAsync(
                        serverCertificate,
                        false,
                        serverProtocol,
                        false));

                    Assert.NotNull(e.InnerException);
                    Assert.Contains("SSL_ERROR_SSL", e.InnerException.Message);
                    Assert.NotNull(e.InnerException.InnerException);
                    Assert.Contains("protocol", e.InnerException.InnerException.Message);
                }
            }

            listener.Stop();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop] // Test hits external azure server.
        public async Task SslStream_NetworkStream_Renegotiation_Succeeds(bool useSync)
        {
            int validationCount = 0;

            var validationCallback = new RemoteCertificateValidationCallback((object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            {
                validationCount++;
                return true;
            });

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await s.ConnectAsync(Configuration.Security.TlsRenegotiationServer, 443);
            using (NetworkStream ns = new NetworkStream(s))
            using (SslStream ssl = new SslStream(ns, true, validationCallback))
            {
                X509CertificateCollection certBundle = new X509CertificateCollection();
                certBundle.Add(Configuration.Certificates.GetClientCertificate());

                // Perform handshake to establish secure connection.
                await ssl.AuthenticateAsClientAsync(Configuration.Security.TlsRenegotiationServer, certBundle, SslProtocols.Tls12, false);
                Assert.True(ssl.IsAuthenticated);
                Assert.True(ssl.IsEncrypted);

                // Issue request that triggers regotiation from server.
                byte[] message = Encoding.UTF8.GetBytes("GET /EchoClientCertificate.ashx HTTP/1.1\r\nHost: corefx-net-tls.azurewebsites.net\r\n\r\n");
                if (useSync)
                {
                    ssl.Write(message, 0, message.Length);
                }
                else
                {
                    await ssl.WriteAsync(message, 0, message.Length);
                }

                // Initiate Read operation, that results in starting renegotiation as per server response to the above request.
                int bytesRead = useSync ? ssl.Read(message, 0, message.Length) : await ssl.ReadAsync(message, 0, message.Length);

                // renegotiation will trigger validation callback again.
                Assert.InRange(validationCount, 2, int.MaxValue);
                Assert.InRange(bytesRead, 1, message.Length);
                Assert.Contains("HTTP/1.1 200 OK", Encoding.UTF8.GetString(message));
            }
        }

        [Fact]
        public async Task SslStream_NestedAuth_Throws()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var ssl = new SslStream(clientStream))
            {
                // Start handshake.
                Task task = ssl.AuthenticateAsClientAsync("foo.com", null, SslProtocols.Tls12, false);
                // Do it again without waiting for previous one to finish.
                await Assert.ThrowsAsync<InvalidOperationException>(() => ssl.AuthenticateAsClientAsync("foo.com", null, SslProtocols.Tls12, false));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SslStream_TargetHostName_Succeeds(bool useEmptyName)
        {
            string targetName = useEmptyName ? string.Empty : Guid.NewGuid().ToString("N");

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (var client = new SslStream(clientStream))
            using (var server = new SslStream(serverStream))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                // It should be empty before handshake.
                Assert.Equal(string.Empty, client.TargetHostName);
                Assert.Equal(string.Empty, server.TargetHostName);

                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions() { TargetHost = targetName };
                clientOptions.RemoteCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        SslStream stream = (SslStream)sender;
                        Assert.Equal(targetName, stream.TargetHostName);

                        return true;
                    };

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions();
                serverOptions.ServerCertificateSelectionCallback =
                    (sender, name) =>
                    {
                        SslStream stream = (SslStream)sender;
                        Assert.Equal(targetName, stream.TargetHostName);

                        return certificate;
                    };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions),
                                server.AuthenticateAsServerAsync(serverOptions));
                Assert.Equal(targetName, client.TargetHostName);
                Assert.Equal(targetName, server.TargetHostName);
            }
        }

        [Fact]
        public async Task SslStream_UntrustedCaWithCustomCallback_OK()
        {
            var clientOptions = new  SslClientAuthenticationOptions() { TargetHost = "localhost" };
            clientOptions.RemoteCertificateValidationCallback =
                (sender, certificate, chain, sslPolicyErrors) =>
                {
                    chain.ChainPolicy.CustomTrustStore.Add(_serverChain[_serverChain.Count -1]);
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

                    bool result = chain.Build((X509Certificate2)certificate);
                    Assert.True(result);

                    return result;
                };

            var serverOptions = new SslServerAuthenticationOptions();
            serverOptions.ServerCertificateContext = SslStreamCertificateContext.Create(_serverCert, _serverChain);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream client = new SslStream(clientStream))
            using (SslStream server = new SslStream(serverStream))
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(serverOptions, CancellationToken.None);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(t1, t2);
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SslStream_UntrustedCaWithCustomCallback_Throws(bool customCallback)
        {
            string errorMessage;
            var clientOptions = new  SslClientAuthenticationOptions() { TargetHost = "localhost" };
            if (customCallback)
            {
                clientOptions.RemoteCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        chain.ChainPolicy.CustomTrustStore.Add(_serverChain[_serverChain.Count -1]);
                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        // This should work and we should be able to trust the chain.
                        Assert.True(chain.Build((X509Certificate2)certificate));
                        // Reject it in custom callback to simulate for example pinning.
                        return false;
                    };

                errorMessage = "RemoteCertificateValidationCallback";
            }
            else
            {
                errorMessage = "UntrustedRoot";
            }

            var serverOptions = new SslServerAuthenticationOptions();
            serverOptions.ServerCertificateContext = SslStreamCertificateContext.Create(_serverCert, _serverChain);

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();
            using (clientStream)
            using (serverStream)
            using (SslStream client = new SslStream(clientStream))
            using (SslStream server = new SslStream(serverStream))
            {
                Task t1 = client.AuthenticateAsClientAsync(clientOptions, CancellationToken.None);
                Task t2 = server.AuthenticateAsServerAsync(serverOptions, CancellationToken.None);

                var e = await Assert.ThrowsAsync<AuthenticationException>(() => t1);
                Assert.Contains(errorMessage, e.Message);
                // Server side should finish since we run custom callback after handshake is done.
                await t2;
            }
        }

        private static bool ValidateServerCertificate(
            object sender,
            X509Certificate retrievedServerPublicCertificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            // Accept any certificate.
            return true;
        }
    }
}
