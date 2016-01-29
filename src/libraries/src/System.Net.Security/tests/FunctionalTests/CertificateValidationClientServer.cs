// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    public class CertificateValidationClientServer
    {
        private readonly X509Certificate2 _clientCertificate;
        private readonly X509Certificate2Collection _clientCertificateCollection;
        private readonly X509Certificate2 _serverCertificate;
        private readonly X509Certificate2Collection _serverCertificateCollection;

        public CertificateValidationClientServer()
        {
            _serverCertificateCollection = TestConfiguration.GetServerCertificateCollection();
            _serverCertificate = TestConfiguration.GetServerCertificate();

            _clientCertificateCollection = TestConfiguration.GetClientCertificateCollection();
            _clientCertificate = TestConfiguration.GetClientCertificate();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [ActiveIssue(4467, PlatformID.Windows)]
        public async Task CertificateValidationClientServer_EndToEnd_Ok(bool useClientSelectionCallback)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.IPv6Loopback, 0);
            var server = new TcpListener(endPoint);
            server.Start();

            using (var clientConnection = new TcpClient(AddressFamily.InterNetworkV6))
            {
                IPEndPoint serverEndPoint = (IPEndPoint)server.LocalEndpoint;

                Task clientConnect = clientConnection.ConnectAsync(serverEndPoint.Address, serverEndPoint.Port);
                Task<TcpClient> serverAccept = server.AcceptTcpClientAsync();

                Assert.True(
                    Task.WaitAll(
                        new Task[] { clientConnect, serverAccept },
                        TestConfiguration.PassingTestTimeoutMilliseconds),
                    "Client/Server TCP Connect timed out.");

                LocalCertificateSelectionCallback clientCertCallback = null;

                if (useClientSelectionCallback)
                {
                    clientCertCallback = ClientCertSelectionCallback;
                }

                using (TcpClient serverConnection = await serverAccept)
                using (SslStream sslClientStream = new SslStream(
                    clientConnection.GetStream(),
                    false,
                    ClientSideRemoteServerCertificateValidation,
                    clientCertCallback))
                using (SslStream sslServerStream = new SslStream(
                    serverConnection.GetStream(),
                    false,
                    ServerSideRemoteClientCertificateValidation))

                {
                    string serverName = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false);
                    var clientCerts = new X509CertificateCollection();

                    if (!useClientSelectionCallback)
                    {
                        clientCerts.Add(_clientCertificate);
                    }

                    Task clientAuthentication = sslClientStream.AuthenticateAsClientAsync(
                        serverName,
                        clientCerts,
                        SslProtocolSupport.DefaultSslProtocols,
                        false);

                    Task serverAuthentication = sslServerStream.AuthenticateAsServerAsync(
                        _serverCertificate,
                        true,
                        SslProtocolSupport.DefaultSslProtocols,
                        false);

                    Assert.True(
                        Task.WaitAll(
                            new Task[] { clientAuthentication, serverAuthentication },
                            TestConfiguration.PassingTestTimeoutMilliseconds),
                        "Client/Server Authentication timed out.");

                    Assert.True(sslClientStream.IsMutuallyAuthenticated, "sslClientStream.IsMutuallyAuthenticated");
                    Assert.True(sslServerStream.IsMutuallyAuthenticated, "sslServerStream.IsMutuallyAuthenticated");

                    Assert.Equal(sslClientStream.RemoteCertificate.Subject, _serverCertificate.Subject);
                    Assert.Equal(sslServerStream.RemoteCertificate.Subject, _clientCertificate.Subject);
                }
            }
        }

        private X509Certificate ClientCertSelectionCallback(
            object sender,
            string targetHost,
            X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate,
            string[] acceptableIssuers)
        {
            return _clientCertificate;
        }

        private bool ServerSideRemoteClientCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            SslPolicyErrors expectedSslPolicyErrors = SslPolicyErrors.None;

            if (!Capability.IsTrustedRootCertificateInstalled())
            {
                expectedSslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;
            }
            else
            {
                // Validate only if we're able to build a trusted chain.
                CertificateChainValidation.Validate(_clientCertificateCollection, chain);
            }

            Assert.Equal(expectedSslPolicyErrors, sslPolicyErrors);
            Assert.Equal(_clientCertificate, certificate);

            return true;
        }

        private bool ClientSideRemoteServerCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            SslPolicyErrors expectedSslPolicyErrors = SslPolicyErrors.None;

            if (!Capability.IsTrustedRootCertificateInstalled())
            {
                expectedSslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;
            }
            else
            {
                // Validate only if we're able to build a trusted chain.
                CertificateChainValidation.Validate(_serverCertificateCollection, chain);
            }

            Assert.Equal(expectedSslPolicyErrors, sslPolicyErrors);
            Assert.Equal(_serverCertificate, certificate);

            return true;
        }
    }
}
