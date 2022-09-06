// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class CertificateValidationClientServer : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly X509Certificate2 _clientCertificate;
        private readonly X509Certificate2 _serverCertificate;
        private bool _clientCertificateRemovedByFilter;

        public CertificateValidationClientServer(ITestOutputHelper output)
        {
            _output = output;

            _serverCertificate = Configuration.Certificates.GetServerCertificate();
            _clientCertificate = Configuration.Certificates.GetClientCertificate();
        }

        public void Dispose()
        {
            _serverCertificate.Dispose();
            _clientCertificate.Dispose();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public async Task CertificateSelectionCallback_DelayedCertificate_OK(bool delayCertificate, bool sendClientCertificate)
        {
            X509Certificate? remoteCertificate = null;

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            {
                int count = 0;
                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions();
                clientOptions.TargetHost = "localhost";
                // Force Tls 1.2 to avoid issues with certain OpenSSL versions and Tls 1.3
                // https://github.com/openssl/openssl/issues/7384
                clientOptions.EnabledSslProtocols = SslProtocols.Tls12;
                clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                clientOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, certificate, acceptableIssuers) =>
                {
                    count++;
                    remoteCertificate = certificate;
                    if (delayCertificate && count == 1)
                    {
                        // wait until we get remote certificate from peer e.g. handshake started.
                        return null;
                    }

                    return sendClientCertificate ? _clientCertificate : null;
                };

                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions();
                serverOptions.ServerCertificate = _serverCertificate;
                serverOptions.ClientCertificateRequired = true;
                serverOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (sendClientCertificate)
                    {
                        Assert.NotNull(certificate);
                        // The client chain may be incomplete.
                        Assert.True(sslPolicyErrors == SslPolicyErrors.None || sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors);
                    }
                    else
                    {
                        Assert.Equal(SslPolicyErrors.RemoteCertificateNotAvailable, sslPolicyErrors);
                    }

                    return true;
                };


                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                                client.AuthenticateAsClientAsync(clientOptions),
                                server.AuthenticateAsServerAsync(serverOptions));

                // verify that the session is usable with or without client's certificate
                await TestHelper.PingPong(client, server);
                await TestHelper.PingPong(server, client);

                if (delayCertificate)
                {
                    // LocalCertificateSelectionCallback should be called with real remote certificate.
                    Assert.NotNull(remoteCertificate);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CertificateValidationClientServer_EndToEnd_Ok(bool useClientSelectionCallback)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 0);
            var server = new TcpListener(endPoint);
            server.Start();

            _clientCertificateRemovedByFilter = false;

            if (PlatformDetection.IsWindows7 &&
                !useClientSelectionCallback &&
                !Capability.IsTrustedRootCertificateInstalled())
            {
                // https://technet.microsoft.com/en-us/library/hh831771.aspx#BKMK_Changes2012R2
                // Starting with Windows 8, the "Management of trusted issuers for client authentication" has changed:
                // The behavior to send the Trusted Issuers List by default is off.
                //
                // In Windows 7 the Trusted Issuers List is sent within the Server Hello TLS record. This list is built
                // by the server using certificates from the Trusted Root Authorities certificate store.
                // The client side will use the Trusted Issuers List, if not empty, to filter proposed certificates.
                _clientCertificateRemovedByFilter = true;
            }

            using (var clientConnection = new TcpClient())
            {
                IPEndPoint serverEndPoint = (IPEndPoint)server.LocalEndpoint;

                Task clientConnect = clientConnection.ConnectAsync(serverEndPoint.Address, serverEndPoint.Port);
                Task<TcpClient> serverAccept = server.AcceptTcpClientAsync();

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(clientConnect, serverAccept);

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

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(clientAuthentication, serverAuthentication);

                    using (sslServerStream.RemoteCertificate)
                    {
                        if (!_clientCertificateRemovedByFilter)
                        {
                            Assert.True(sslClientStream.IsMutuallyAuthenticated, "sslClientStream.IsMutuallyAuthenticated");
                            Assert.True(sslServerStream.IsMutuallyAuthenticated, "sslServerStream.IsMutuallyAuthenticated");

                            Assert.Equal(sslServerStream.RemoteCertificate.Subject, _clientCertificate.Subject);
                        }
                        else
                        {
                            Assert.False(sslClientStream.IsMutuallyAuthenticated, "sslClientStream.IsMutuallyAuthenticated");
                            Assert.False(sslServerStream.IsMutuallyAuthenticated, "sslServerStream.IsMutuallyAuthenticated");

                            Assert.Null(sslServerStream.RemoteCertificate);
                        }

                        Assert.Equal(sslClientStream.RemoteCertificate.Subject, _serverCertificate.Subject);
                    }
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
                if (!_clientCertificateRemovedByFilter)
                {
                    expectedSslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;
                }
                else
                {
                    expectedSslPolicyErrors = SslPolicyErrors.RemoteCertificateNotAvailable;
                }
            }
            else
            {
                // Validate only if we're able to build a trusted chain.
                ValidateCertificateAndChain(_clientCertificate, chain);
            }

            Assert.Equal(expectedSslPolicyErrors, sslPolicyErrors);
            if (!_clientCertificateRemovedByFilter)
            {
                Assert.Equal(_clientCertificate, certificate);
            }

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
                ValidateCertificateAndChain(_serverCertificate, chain);
            }

            Assert.Equal(expectedSslPolicyErrors, sslPolicyErrors);
            Assert.Equal(_serverCertificate, certificate);

            return true;
        }

        private void ValidateCertificateAndChain(X509Certificate2 cert, X509Chain trustedChain)
        {
            _output.WriteLine("ValidateCertificateAndChain()");

            // Verify that the certificate is in the trustedChain.
            _output.WriteLine($"cert: subject={cert.Subject}, issuer={cert.Issuer}, thumbprint={cert.Thumbprint}");
            Assert.Equal(cert.Thumbprint, trustedChain.ChainElements[0].Certificate.Thumbprint);

            // Verify that the root certificate in the chain is the one that issued the received certificate.
            foreach (X509ChainElement element in trustedChain.ChainElements)
            {
                _output.WriteLine(
                    $"chain cert: subject={element.Certificate.Subject}, issuer={element.Certificate.Issuer}, thumbprint={element.Certificate.Thumbprint}");
            }
            Assert.Equal(cert.Issuer, trustedChain.ChainElements[1].Certificate.Subject);
        }
    }
}
