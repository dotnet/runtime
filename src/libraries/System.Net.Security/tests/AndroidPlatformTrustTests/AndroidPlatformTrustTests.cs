// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
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
        public async Task SslStream_CustomRootTrust_NoChainErrors()
        {
            // The server uses testservereku.contoso.com.pfx signed by the NDX Test Root CA.
            // CustomRootTrust is configured with the NDX root so managed validation accepts.
            // Explicit managed custom trust is authoritative, so platform rejection would not
            // pre-seed RemoteCertificateChainErrors.

            using X509Certificate2 rootCertificate = GetRootCertificate();
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
                    },
                    CertificateChainPolicy = new X509ChainPolicy
                    {
                        RevocationMode = X509RevocationMode.NoCheck,
                        TrustMode = X509ChainTrustMode.CustomRootTrust,
                        CustomTrustStore = { rootCertificate },
                    },
                };

                await Task.WhenAll(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions)).WaitAsync(TimeSpan.FromSeconds(30));
            }

            Assert.NotNull(reportedErrors);
            Assert.Equal(SslPolicyErrors.None, reportedErrors.Value & SslPolicyErrors.RemoteCertificateChainErrors);
        }

        [Fact]
        public async Task SslStream_PlatformTrustedCertificateRejectedByManagedChain_ReportsChainErrors()
        {
            // The platform trusts the NDX root through network_security_config.xml, but
            // managed validation does not use that Android app configuration. This verifies
            // that platform acceptance cannot mask managed chain failures.

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
                    },
                };

                await Task.WhenAll(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions)).WaitAsync(TimeSpan.FromSeconds(30));
            }

            Assert.NotNull(reportedErrors);
            Assert.True(
                (reportedErrors.Value & SslPolicyErrors.RemoteCertificateChainErrors) != 0,
                $"Expected managed chain errors but got: {reportedErrors.Value}");
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

        [Fact]
        public async Task SslStream_CustomRootTrustWithoutExtraStore_ReportsChainErrors()
        {
            // Generate a dynamic PKI (root → intermediate → leaf) that is NOT in network_security_config.xml.
            // CustomRootTrust has the dynamic root, but ExtraStore is empty — the intermediate is missing.
            // Explicit managed custom trust is authoritative, but managed validation still fails
            // because it cannot build the chain without the intermediate.
            // Result: RemoteCertificateChainErrors reported.

            (X509Certificate2 rootCert, X509Certificate2 intermediateCert, X509Certificate2 serverCert) = GenerateCertificateChain();

            SslPolicyErrors? reportedErrors = null;

            (SslStream client, SslStream server) = GetConnectedSslStreams();
            using (client)
            using (server)
            using (rootCert)
            using (intermediateCert)
            using (serverCert)
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "testservereku.contoso.com",
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        reportedErrors = sslPolicyErrors;
                        return true;
                    },
                    CertificateChainPolicy = new X509ChainPolicy
                    {
                        RevocationMode = X509RevocationMode.NoCheck,
                        TrustMode = X509ChainTrustMode.CustomRootTrust,
                        CustomTrustStore = { rootCert },
                    },
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
        public async Task SslStream_CustomRootTrustWithExtraStore_ManagedOverridesPlatform()
        {
            // Same dynamic PKI as above, but now ExtraStore contains the intermediate CA.
            // The platform would reject (dynamic root not in network_security_config.xml),
            // but explicit managed custom trust is authoritative, so the platform's verdict is ignored.
            // The managed chain builder has root (CustomTrustStore) + intermediate (ExtraStore) and succeeds.
            // Result: no RemoteCertificateChainErrors — managed validation is authoritative.

            (X509Certificate2 rootCert, X509Certificate2 intermediateCert, X509Certificate2 serverCert) = GenerateCertificateChain();

            SslPolicyErrors? reportedErrors = null;

            (SslStream client, SslStream server) = GetConnectedSslStreams();
            using (client)
            using (server)
            using (rootCert)
            using (intermediateCert)
            using (serverCert)
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "testservereku.contoso.com",
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        reportedErrors = sslPolicyErrors;
                        return true;
                    },
                    CertificateChainPolicy = new X509ChainPolicy
                    {
                        RevocationMode = X509RevocationMode.NoCheck,
                        TrustMode = X509ChainTrustMode.CustomRootTrust,
                        CustomTrustStore = { rootCert },
                        ExtraStore = { intermediateCert },
                    },
                };

                await Task.WhenAll(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions)).WaitAsync(TimeSpan.FromSeconds(30));
            }

            Assert.NotNull(reportedErrors);
            Assert.Equal(SslPolicyErrors.None, reportedErrors.Value & SslPolicyErrors.RemoteCertificateChainErrors);
        }

        [Fact]
        public async Task SslStream_CustomRootTrustDirectlySignedLeaf_ManagedTrust()
        {
            // The custom root is used by managed CustomRootTrust. The leaf is signed
            // directly by the root, so no ExtraStore is needed.

            (X509Certificate2 rootCert, X509Certificate2 serverCert) = GenerateDirectCertificateChain();

            SslPolicyErrors? reportedErrors = null;

            (SslStream client, SslStream server) = GetConnectedSslStreams();
            using (client)
            using (server)
            using (rootCert)
            using (serverCert)
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "testservereku.contoso.com",
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        reportedErrors = sslPolicyErrors;
                        return true;
                    },
                    CertificateChainPolicy = new X509ChainPolicy
                    {
                        RevocationMode = X509RevocationMode.NoCheck,
                        TrustMode = X509ChainTrustMode.CustomRootTrust,
                        CustomTrustStore = { rootCert },
                    },
                };

                await Task.WhenAll(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions)).WaitAsync(TimeSpan.FromSeconds(30));
            }

            Assert.NotNull(reportedErrors);
            Assert.Equal(SslPolicyErrors.None, reportedErrors.Value & SslPolicyErrors.RemoteCertificateChainErrors);
        }

        [Fact]
        public async Task SslStream_SslCertificateTrustList_ManagedTrust()
        {
            // Exercise the legacy SslCertificateTrust path. Because CertificateChainPolicy
            // is not set, ClientCertificateContext.Trust makes managed validation authoritative.

            using X509Certificate2 trustedServerCertificate = Configuration.Certificates.GetSelfSignedServerCertificate();
            using X509Certificate2 clientCertificate = Configuration.Certificates.GetClientCertificate();
            SslCertificateTrust trust = SslCertificateTrust.CreateForX509Collection(new X509Certificate2Collection(trustedServerCertificate));
            SslStreamCertificateContext clientCertificateContext = SslStreamCertificateContext.Create(clientCertificate, additionalCertificates: null, offline: true, trust);

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
                    TargetHost = "testselfsignedservereku.contoso.com",
                    ClientCertificateContext = clientCertificateContext,
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        reportedErrors = sslPolicyErrors;
                        return true;
                    },
                };

                await Task.WhenAll(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions)).WaitAsync(TimeSpan.FromSeconds(30));
            }

            Assert.NotNull(reportedErrors);
            Assert.Equal(SslPolicyErrors.None, reportedErrors.Value & SslPolicyErrors.RemoteCertificateChainErrors);
        }

        [Fact]
        public async Task SslStream_TargetHostIsIpLiteral_HandshakeUsesNonHostnameAwareValidation()
        {
            // When TargetHost is an IP literal, SafeDeleteSslContext deliberately skips passing
            // it to the platform's DotnetProxyTrustManager (an SNIHostName cannot be an IP).
            // The trust manager then falls back to the non-hostname-aware
            // X509TrustManager.checkServerTrusted(chain, authType) path, which means
            // network_security_config.xml's per-domain rules don't apply — the chain is
            // evaluated only against the system trust store.
            //
            // Setup: dynamic PKI not in the XML config + CustomRootTrust providing the root +
            // ExtraStore providing the intermediate. Targeting the loopback by IP literal must
            // (a) not throw because of SNI/IP handling, and (b) succeed because explicit
            // managed custom trust ignores any platform rejection.

            (X509Certificate2 rootCert, X509Certificate2 intermediateCert, X509Certificate2 serverCert) = GenerateCertificateChain();

            SslPolicyErrors? reportedErrors = null;

            (SslStream client, SslStream server) = GetConnectedSslStreams();
            using (client)
            using (server)
            using (rootCert)
            using (intermediateCert)
            using (serverCert)
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "127.0.0.1",
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        reportedErrors = sslPolicyErrors;
                        return true;
                    },
                    CertificateChainPolicy = new X509ChainPolicy
                    {
                        RevocationMode = X509RevocationMode.NoCheck,
                        TrustMode = X509ChainTrustMode.CustomRootTrust,
                        CustomTrustStore = { rootCert },
                        ExtraStore = { intermediateCert },
                        VerificationFlags = X509VerificationFlags.IgnoreInvalidName,
                    },
                };

                await Task.WhenAll(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions)).WaitAsync(TimeSpan.FromSeconds(30));
            }

            Assert.NotNull(reportedErrors);
            // No chain errors: the IP-literal path didn't crash on SNIHostName, and explicit
            // managed custom trust ignored any platform rejection.
            Assert.Equal(SslPolicyErrors.None, reportedErrors.Value & SslPolicyErrors.RemoteCertificateChainErrors);
        }

        [Fact]
        public async Task SslStream_TargetHostNotSet_NonHostnameAwarePathTrustsSystemCA()
        {
            // When the caller doesn't set TargetHost, SafeDeleteSslContext passes null to the
            // native layer; DotnetProxyTrustManager then calls X509TrustManager.checkServerTrusted
            // WITHOUT a hostname (no X509TrustManagerExtensions). This is the path callers hit
            // when they don't know the hostname (e.g. pre-resolved TCP sockets).
            //
            // We use a dynamic PKI (not in any system trust store and not in network_security_config.xml)
            // with CustomRootTrust + ExtraStore so explicit managed custom trust ignores the platform verdict.
            // Whatever the platform says, the callback must still see SslPolicyErrors.None because
            // managed validation is authoritative.

            (X509Certificate2 rootCert, X509Certificate2 intermediateCert, X509Certificate2 serverCert) = GenerateCertificateChain();

            SslPolicyErrors? reportedErrors = null;

            (SslStream client, SslStream server) = GetConnectedSslStreams();
            using (client)
            using (server)
            using (rootCert)
            using (intermediateCert)
            using (serverCert)
            {
                var serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCert,
                };

                var clientOptions = new SslClientAuthenticationOptions
                {
                    // TargetHost intentionally not set: tests the null-hostname code path.
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        reportedErrors = sslPolicyErrors;
                        return true;
                    },
                    CertificateChainPolicy = new X509ChainPolicy
                    {
                        RevocationMode = X509RevocationMode.NoCheck,
                        TrustMode = X509ChainTrustMode.CustomRootTrust,
                        CustomTrustStore = { rootCert },
                        ExtraStore = { intermediateCert },
                        VerificationFlags = X509VerificationFlags.IgnoreInvalidName,
                    },
                };

                await Task.WhenAll(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions)).WaitAsync(TimeSpan.FromSeconds(30));
            }

            Assert.NotNull(reportedErrors);
            Assert.Equal(SslPolicyErrors.None, reportedErrors.Value & SslPolicyErrors.RemoteCertificateChainErrors);
        }

        /// <summary>
        /// Generates a certificate chain: root CA → leaf cert.
        /// The root is NOT the NDX Test Root CA from network_security_config.xml,
        /// so it is trusted only when passed through custom trust options.
        /// </summary>
        private static (X509Certificate2 root, X509Certificate2 leaf) GenerateDirectCertificateChain()
        {
            DateTimeOffset notBefore = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset notAfter = DateTimeOffset.UtcNow.AddYears(1);
            byte[] serialNumber = new byte[8];

            using RSA rootKey = RSA.Create(2048);
            var rootRequest = new CertificateRequest("CN=Direct Test Root CA", rootKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: true, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
            var rootSkid = new X509SubjectKeyIdentifierExtension(rootRequest.PublicKey, critical: false);
            rootRequest.CertificateExtensions.Add(rootSkid);
            using X509Certificate2 rootCert = rootRequest.CreateSelfSigned(notBefore, notAfter);

            using RSA leafKey = RSA.Create(2048);
            var leafRequest = new CertificateRequest("CN=testservereku.contoso.com", leafKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            leafRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
            leafRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, critical: false));
            leafRequest.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(rootSkid));
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName("testservereku.contoso.com");
            leafRequest.CertificateExtensions.Add(sanBuilder.Build());
            RandomNumberGenerator.Fill(serialNumber);
            using X509Certificate2 leafCertWithoutKey = leafRequest.Create(rootCert, notBefore, notAfter, serialNumber);
            X509Certificate2 leafCert = leafCertWithoutKey.CopyWithPrivateKey(leafKey);

            return (X509CertificateLoader.LoadCertificate(rootCert.Export(X509ContentType.Cert)), leafCert);
        }

        /// <summary>
        /// Generates a certificate chain: root CA → intermediate CA → leaf cert.
        /// The root is NOT the NDX Test Root CA from network_security_config.xml,
        /// so the platform will not trust this chain.
        /// </summary>
        private static (X509Certificate2 root, X509Certificate2 intermediate, X509Certificate2 leaf) GenerateCertificateChain()
        {
            DateTimeOffset notBefore = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset notAfter = DateTimeOffset.UtcNow.AddYears(1);
            byte[] serialNumber = new byte[8];

            using RSA rootKey = RSA.Create(2048);
            var rootRequest = new CertificateRequest("CN=Test Root CA", rootKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: true, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
            var rootSkid = new X509SubjectKeyIdentifierExtension(rootRequest.PublicKey, critical: false);
            rootRequest.CertificateExtensions.Add(rootSkid);
            using X509Certificate2 rootCert = rootRequest.CreateSelfSigned(notBefore, notAfter);

            using RSA intermediateKey = RSA.Create(2048);
            var intermediateRequest = new CertificateRequest("CN=Test Intermediate CA", intermediateKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            intermediateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: true, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
            var intermediateSkid = new X509SubjectKeyIdentifierExtension(intermediateRequest.PublicKey, critical: false);
            intermediateRequest.CertificateExtensions.Add(intermediateSkid);
            intermediateRequest.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(rootSkid));
            RandomNumberGenerator.Fill(serialNumber);
            using X509Certificate2 intermediateCertWithoutKey = intermediateRequest.Create(rootCert, notBefore, notAfter, serialNumber);
            using X509Certificate2 intermediateCert = intermediateCertWithoutKey.CopyWithPrivateKey(intermediateKey);

            using RSA leafKey = RSA.Create(2048);
            var leafRequest = new CertificateRequest("CN=testservereku.contoso.com", leafKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            leafRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
            leafRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, critical: false));
            leafRequest.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(intermediateSkid));
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName("testservereku.contoso.com");
            leafRequest.CertificateExtensions.Add(sanBuilder.Build());
            RandomNumberGenerator.Fill(serialNumber);
            using X509Certificate2 leafCertWithoutKey = leafRequest.Create(intermediateCert, notBefore, notAfter, serialNumber);
            X509Certificate2 leafCert = leafCertWithoutKey.CopyWithPrivateKey(leafKey);

            return (X509CertificateLoader.LoadCertificate(rootCert.Export(X509ContentType.Cert)), X509CertificateLoader.LoadCertificate(intermediateCert.Export(X509ContentType.Cert)), leafCert);
        }

        /// <summary>
        /// Extracts the NDX Test Root CA (self-signed) from the contoso.com.p7b PKCS#7 bundle.
        /// </summary>
        private static X509Certificate2 GetRootCertificate()
        {
#pragma warning disable SYSLIB0057 // X509Certificate2Collection.Import is obsolete
            var collection = new X509Certificate2Collection();
            collection.Import(File.ReadAllBytes(Path.Combine("TestData", "contoso.com.p7b")));
#pragma warning restore SYSLIB0057
            byte[]? rootRawData = null;
            try
            {
                foreach (X509Certificate2 cert in collection)
                {
                    if (cert.Subject == cert.Issuer)
                    {
                        rootRawData = cert.Export(X509ContentType.Cert);
                        break;
                    }
                }
            }
            finally
            {
                foreach (X509Certificate2 cert in collection)
                {
                    cert.Dispose();
                }
            }

            if (rootRawData is null)
            {
                throw new InvalidOperationException("Root CA not found in contoso.com.p7b");
            }

            return X509CertificateLoader.LoadCertificate(rootRawData);
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
