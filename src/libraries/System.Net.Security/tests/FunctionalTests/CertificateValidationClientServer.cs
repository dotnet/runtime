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
    using Configuration = System.Net.Test.Common.Configuration;

    public class CertificateValidationClientServer : IDisposable
    {
        private readonly X509Certificate2 _clientCertificate;
        private readonly X509Certificate2Collection _clientCertificateCollection;
        private readonly X509Certificate2 _serverCertificate;
        private readonly X509Certificate2Collection _serverCertificateCollection;
        private bool _clientCertificateRemovedByFilter;

        public CertificateValidationClientServer()
        {
            _serverCertificateCollection = Configuration.Certificates.GetServerCertificateCollection();
            _serverCertificate = Configuration.Certificates.GetServerCertificate();

            _clientCertificateCollection = Configuration.Certificates.GetClientCertificateCollection();
            _clientCertificate = Configuration.Certificates.GetClientCertificate();
        }

        public void Dispose()
        {
            _serverCertificate.Dispose();
            _clientCertificate.Dispose();
            foreach (X509Certificate2 cert in _serverCertificateCollection) cert.Dispose();
            foreach (X509Certificate2 cert in _clientCertificateCollection) cert.Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CertificateValidationClientServer_EndToEnd_Ok(bool useClientSelectionCallback)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.IPv6Loopback, 0);
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

            using (var clientConnection = new TcpClient(AddressFamily.InterNetworkV6))
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
                CertificateChainValidation.Validate(_clientCertificateCollection, chain);
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
                CertificateChainValidation.Validate(_serverCertificateCollection, chain);
            }

            Assert.Equal(expectedSslPolicyErrors, sslPolicyErrors);
            Assert.Equal(_serverCertificate, certificate);

            return true;
        }
    }
}
