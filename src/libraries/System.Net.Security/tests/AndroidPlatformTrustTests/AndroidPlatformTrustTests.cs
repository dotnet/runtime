// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    /// <summary>
    /// Tests for Android's network_security_config.xml integration with SslStream.
    ///
    /// This test project is bundled into an APK with a network_security_config.xml that
    /// trusts the NDX Test Root CA (from contoso.com.p7b) for the domain
    /// "testservereku.contoso.com". The root CA DER file is extracted at build time
    /// from the System.Net.TestData package and placed in res/raw/test_ca.der.
    ///
    /// Certificate hierarchy from System.Net.TestData:
    ///   - NDX Test Root CA (root, self-signed) — trusted in network_security_config.xml
    ///     └─ testservereku.contoso.com (leaf, CA-signed)
    ///   - testselfsignedservereku.contoso.com (self-signed, different CA)
    /// </summary>
    public class AndroidPlatformTrustTests
    {
        [Fact]
        public async Task SslStream_CertificateSignedByTrustedCA_NoChainErrors()
        {
            // The server uses testservereku.contoso.com.pfx which is signed by the NDX Test Root CA.
            // The network_security_config.xml trusts that root CA for "testservereku.contoso.com".
            // The platform trust manager should accept this chain, so no chain errors are reported.

            SslPolicyErrors? reportedErrors = null;

            (SslStream client, SslStream server) = GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCertificate,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "testservereku.contoso.com",
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        reportedErrors = sslPolicyErrors;
                        return true;
                    }
                };

                await Task.WhenAll(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions)).WaitAsync(TimeSpan.FromSeconds(30));
            }

            Assert.NotNull(reportedErrors);
            Assert.Equal(SslPolicyErrors.None, reportedErrors.Value & SslPolicyErrors.RemoteCertificateChainErrors);
        }

        [Fact]
        public async Task SslStream_CertificateNotSignedByTrustedCA_ReportsChainErrors()
        {
            // The server uses a self-signed certificate that is NOT signed by the NDX Test Root CA.
            // The network_security_config.xml only trusts the NDX Test Root CA, so the platform
            // trust manager rejects this certificate chain — simulating a certificate pinning mismatch.

            SslPolicyErrors? reportedErrors = null;

            (SslStream client, SslStream server) = GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetSelfSignedServerCertificate())
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCertificate,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "testservereku.contoso.com",
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        reportedErrors = sslPolicyErrors;
                        return true;
                    }
                };

                await Task.WhenAll(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions)).WaitAsync(TimeSpan.FromSeconds(30));
            }

            Assert.NotNull(reportedErrors);
            Assert.NotEqual(SslPolicyErrors.None, reportedErrors.Value & SslPolicyErrors.RemoteCertificateChainErrors);
        }

        [Fact]
        public async Task SslStream_CallbackCanOverridePlatformTrustFailure()
        {
            // Even when the platform trust manager rejects the certificate chain,
            // the RemoteCertificateValidationCallback can override the decision and
            // allow the connection to succeed.

            bool callbackInvoked = false;

            (SslStream client, SslStream server) = GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetSelfSignedServerCertificate())
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCertificate,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "testservereku.contoso.com",
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        callbackInvoked = true;
                        return true;
                    }
                };

                await Task.WhenAll(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions)).WaitAsync(TimeSpan.FromSeconds(30));
            }

            Assert.True(callbackInvoked);
        }

        [Fact]
        public async Task SslStream_CallbackRejectingUntrustedCertificate_ThrowsAuthenticationException()
        {
            // When the callback returns false for an untrusted certificate, the connection fails.

            (SslStream client, SslStream server) = GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetSelfSignedServerCertificate())
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCertificate,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "testservereku.contoso.com",
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => false
                };

                Task serverTask = server.AuthenticateAsServerAsync(serverOptions);
                await Assert.ThrowsAsync<AuthenticationException>(() =>
                    client.AuthenticateAsClientAsync(clientOptions));

                // Server side may throw too since the client rejected the connection.
                try { await serverTask.WaitAsync(TimeSpan.FromSeconds(5)); }
                catch { }
            }
        }

        [Fact]
        public async Task SslStream_DomainNotInConfig_CallbackReceivesChainErrors()
        {
            // The server uses testservereku.contoso.com.pfx signed by the NDX Test Root CA.
            // The client connects with a domain NOT listed in network_security_config.xml,
            // so the base-config applies (system CAs only). The platform trust manager rejects
            // because the NDX Test Root CA is not a system CA. This simulates a certificate
            // pinning scenario: the cert chain is valid (signed by a known CA) but the platform
            // rejects based on its trust configuration.
            //
            // The callback must receive RemoteCertificateChainErrors so the application
            // knows the platform rejected the certificate.

            SslPolicyErrors? reportedErrors = null;

            (SslStream client, SslStream server) = GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetServerCertificate())
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCertificate,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "otherdomain.contoso.com",
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        reportedErrors = sslPolicyErrors;
                        return true;
                    }
                };

                await Task.WhenAll(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions)).WaitAsync(TimeSpan.FromSeconds(30));
            }

            Assert.NotNull(reportedErrors);
            Assert.True(
                (reportedErrors.Value & SslPolicyErrors.RemoteCertificateChainErrors) != 0,
                $"Expected RemoteCertificateChainErrors but got: {reportedErrors.Value}");
        }

        [Fact]
        public async Task SslStream_UntrustedCertificateWithoutCallback_ThrowsAuthenticationException()
        {
            // When the platform trust manager rejects and no callback is provided,
            // the connection must fail. This verifies that platform trust rejection
            // (e.g. certificate pinning) is enforced even without a user callback.

            (SslStream client, SslStream server) = GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetSelfSignedServerCertificate())
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCertificate,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "testservereku.contoso.com",
                };

                Task serverTask = server.AuthenticateAsServerAsync(serverOptions);
                await Assert.ThrowsAsync<AuthenticationException>(() =>
                    client.AuthenticateAsClientAsync(clientOptions));

                try { await serverTask.WaitAsync(TimeSpan.FromSeconds(5)); }
                catch { }
            }
        }

        private static (SslStream client, SslStream server) GetConnectedSslStreams()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(listener.LocalEndPoint!);

            Socket serverSocket = listener.Accept();

            var clientStream = new SslStream(new NetworkStream(clientSocket, ownsSocket: true));
            var serverStream = new SslStream(new NetworkStream(serverSocket, ownsSocket: true));

            return (clientStream, serverStream);
        }
    }
}
