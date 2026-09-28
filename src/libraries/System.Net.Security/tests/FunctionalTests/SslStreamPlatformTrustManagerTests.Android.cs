// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamPlatformTrustManagerTests
    {
        [Fact]
        public async Task SslStream_UntrustedCertificate_ReportsChainErrors()
        {
            // Android platform trust participation is covered by AndroidPlatformTrustTests,
            // where network_security_config.xml creates a platform-vs-managed trust
            // difference. This smoke test verifies the Android functional-test path
            // still reports chain errors for an untrusted certificate.

            SslPolicyErrors? reportedErrors = null;

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetSelfSignedServerCertificate())
            {
                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCertificate,
                };

                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "localhost",
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        reportedErrors = sslPolicyErrors;
                        return true;
                    }
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions));
            }

            Assert.NotNull(reportedErrors);
            Assert.True(
                (reportedErrors.Value & SslPolicyErrors.RemoteCertificateChainErrors) != 0,
                $"Expected RemoteCertificateChainErrors but got: {reportedErrors.Value}");
        }

        [Fact]
        public async Task SslStream_UntrustedCertificate_CallbackCanOverride()
        {
            // This test verifies that even when the platform trust manager rejects
            // the certificate chain, the RemoteCertificateValidationCallback can
            // still accept the connection.

            bool callbackInvoked = false;

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetSelfSignedServerCertificate())
            {
                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCertificate,
                };

                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "localhost",
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        callbackInvoked = true;
                        return true;
                    }
                };

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    client.AuthenticateAsClientAsync(clientOptions),
                    server.AuthenticateAsServerAsync(serverOptions));

                Assert.True(callbackInvoked);
                Assert.True(client.IsAuthenticated);
                Assert.True(server.IsAuthenticated);
            }
        }

        [Fact]
        public async Task SslStream_UntrustedCertificate_CallbackRejectingCausesFailure()
        {
            // This test verifies that when the callback returns false for an
            // untrusted certificate, the connection fails.

            (SslStream client, SslStream server) = TestHelper.GetConnectedSslStreams();
            using (client)
            using (server)
            using (X509Certificate2 serverCertificate = Configuration.Certificates.GetSelfSignedServerCertificate())
            {
                SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = serverCertificate,
                };

                SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = "localhost",
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        return false;
                    }
                };

                await Assert.ThrowsAsync<AuthenticationException>(() =>
                    TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                        client.AuthenticateAsClientAsync(clientOptions),
                        server.AuthenticateAsServerAsync(serverOptions)));
            }
        }
    }
}
