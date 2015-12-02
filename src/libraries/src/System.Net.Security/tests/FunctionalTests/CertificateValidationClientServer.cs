// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

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

        [Fact]
        [ActiveIssue(4467)]
        public async Task CertificateValidationClientServer_EndToEnd_Ok()
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

                using (TcpClient serverConnection = await serverAccept)
                using (SslStream sslClientStream = new SslStream(
                    clientConnection.GetStream(),
                    false,
                    ClientSideRemoteServerCertificateValidation))
                using (SslStream sslServerStream = new SslStream(
                    serverConnection.GetStream(),
                    false,
                    ServerSideRemoteClientCertificateValidation))

                {
                    string serverName = _serverCertificate.GetNameInfo(X509NameType.SimpleName, false);
                    string clientName = _clientCertificate.GetNameInfo(X509NameType.SimpleName, false);

                    var clientCerts = new X509CertificateCollection();
                    clientCerts.Add(_clientCertificate);

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
                }
            }
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
