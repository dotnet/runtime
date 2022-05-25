// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.X509Certificates.Tests.Common;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class CertificateValidationRemoteServer
    {
        [OuterLoop("Uses external servers")]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls12))] // external server does not support TLS1.1 and below
        [InlineData(false)]
        [InlineData(true)]
        public async Task CertificateValidationRemoteServer_EndToEnd_Ok(bool useAsync)
        {
            using (var client = new TcpClient(AddressFamily.InterNetwork))
            {
                try
                {
                    await client.ConnectAsync(Configuration.Security.TlsServer.IdnHost, Configuration.Security.TlsServer.Port);
                }
                catch (Exception ex)
                {
                    // if we cannot connect, skip the test instead of failing.
                    // This test is not trying to test networking.
                    throw new SkipTestException($"Unable to connect to '{Configuration.Security.TlsServer.IdnHost}': {ex.Message}");
                }

                using (SslStream sslStream = new SslStream(client.GetStream(), false, RemoteHttpsCertValidation, null))
                {
                    try
                    {
                        if (useAsync)
                        {
                            await sslStream.AuthenticateAsClientAsync(Configuration.Security.TlsServer.IdnHost);
                        }
                        else
                        {
                            sslStream.AuthenticateAsClient(Configuration.Security.TlsServer.IdnHost);
                        }
                    }
                    catch (IOException ex) when (ex.InnerException is SocketException &&
                      ((SocketException)ex.InnerException).SocketErrorCode == SocketError.ConnectionReset)
                    {
                        // Since we try to verify certificate validation, ignore IO errors
                        // caused most likely by environmental failures.
                        throw new SkipTestException($"Unable to connect to '{Configuration.Security.TlsServer.IdnHost}': {ex.InnerException.Message}");
                    }
                }
            }
        }

        // MacOS has special validation rules for apple.com and icloud.com
        [ConditionalTheory]
        [OuterLoop("Uses external servers")]
        [InlineData("www.apple.com")]
        [InlineData("www.icloud.com")]
        [PlatformSpecific(TestPlatforms.OSX)]
        public Task CertificateValidationApple_EndToEnd_Ok(string host)
        {
            return EndToEndHelper(host);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.SupportsTls12))]
        [OuterLoop("Uses external servers")]
        [InlineData("api.nuget.org")]
        [InlineData("www.microsoft.com.")]
        [InlineData("")]
        public async Task DefaultConnect_EndToEnd_Ok(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                host = Configuration.Security.TlsServer.IdnHost;
            }

            await EndToEndHelper(host);
            // Second try may change the handshake because of TLS resume.
            await EndToEndHelper(host);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public Task ConnectWithRevocation_WithCallback(bool checkRevocation)
        {
            X509RevocationMode mode = checkRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
            return ConnectWithRevocation_WithCallback_Core(mode);
        }

        [PlatformSpecific(TestPlatforms.Linux)]
        [Theory]
        [OuterLoop("Subject to system load race conditions")]
        [InlineData(false)]
        [InlineData(true)]
        public Task ConnectWithRevocation_StapledOcsp(bool offlineContext)
        {
            // Offline will only work if
            // a) the revocation has been checked recently enough that it is cached, or
            // b) the server stapled the response
            //
            // At high load, the server's background fetch might not have completed before
            // this test runs.
            return ConnectWithRevocation_WithCallback_Core(X509RevocationMode.Offline, offlineContext);
        }

        private async Task ConnectWithRevocation_WithCallback_Core(X509RevocationMode revocationMode, bool offlineContext = false)
        {
            string serverName = $"{revocationMode.ToString().ToLower()}.{offlineContext.ToString().ToLower()}.server.example";

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();

            CertificateAuthority.BuildPrivatePki(
                PkiOptions.EndEntityRevocationViaOcsp | PkiOptions.CrlEverywhere,
                out RevocationResponder responder,
                out CertificateAuthority rootAuthority,
                out CertificateAuthority intermediateAuthority,
                out X509Certificate2 serverCert,
                subjectName: serverName,
                extensions: TestHelper.BuildTlsServerCertExtensions(serverName));

            SslClientAuthenticationOptions clientOpts = new SslClientAuthenticationOptions
            {
                TargetHost = serverName,
                RemoteCertificateValidationCallback = CertificateValidationCallback,
                CertificateRevocationCheckMode = revocationMode,
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                X509Certificate2 temp = new X509Certificate2(serverCert.Export(X509ContentType.Pkcs12));
                serverCert.Dispose();
                serverCert = temp;
            }

            await using (clientStream)
            await using (serverStream)
            using (responder)
            using (rootAuthority)
            using (intermediateAuthority)
            using (serverCert)
            using (X509Certificate2 issuerCert = intermediateAuthority.CloneIssuerCert())
            using (X509Certificate2 rootCert = rootAuthority.CloneIssuerCert())
            await using (SslStream tlsClient = new SslStream(clientStream))
            await using (SslStream tlsServer = new SslStream(serverStream))
            {
                intermediateAuthority.Revoke(serverCert, serverCert.NotBefore);

                SslServerAuthenticationOptions serverOpts = new SslServerAuthenticationOptions
                {
                    ServerCertificateContext = SslStreamCertificateContext.Create(
                        serverCert,
                        new X509Certificate2Collection(issuerCert),
                        trust: SslCertificateTrust.CreateForX509Collection(new X509Certificate2Collection(rootCert))),
                };

                Task serverTask = tlsServer.AuthenticateAsServerAsync(serverOpts);
                Task clientTask = tlsClient.AuthenticateAsClientAsync(clientOpts);

                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(clientTask, serverTask);
            }

            static bool CertificateValidationCallback(
                object sender,
                X509Certificate? certificate,
                X509Chain? chain,
                SslPolicyErrors sslPolicyErrors)
            {
                Assert.NotNull(certificate);
                Assert.NotNull(chain);

                sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;

                chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                chain.ChainPolicy.CustomTrustStore.Add(chain.ChainElements[^1].Certificate);

                // The offline test will not know about revocation for the intermediate,
                // so change the policy to only check the end certificate.
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;

                if (!chain.Build((X509Certificate2)certificate))
                {
                    sslPolicyErrors |= SslPolicyErrors.RemoteCertificateChainErrors;
                }

                if (chain.ChainPolicy.RevocationMode == X509RevocationMode.NoCheck)
                {
                    X509ChainStatusFlags chainFlags = 0;

                    foreach (X509ChainStatus status in chain.ChainStatus)
                    {
                        chainFlags |= status.Status;
                    }

                    Assert.Equal(X509ChainStatusFlags.NoError, chainFlags);

                    // The call didn't request revocation, so the chain should have been trusted.
                    Assert.Equal(SslPolicyErrors.None, sslPolicyErrors);
                }
                else if (certificate.Subject.Contains(".true.server.") &&
                    chain.ChainPolicy.RevocationMode == X509RevocationMode.Offline)
                {
                    // In an Offline chain with an offline context the revocation still shouldn't
                    // process, because there's no OCSP data.
                    Assert.Equal(SslPolicyErrors.RemoteCertificateChainErrors, sslPolicyErrors);

                    Assert.Contains(
                        chain.ChainElements[0].ChainElementStatus,
                        cs => cs.Status == X509ChainStatusFlags.RevocationStatusUnknown);
                }
                else
                {
                    // Revocation was requested, and the cert is revoked, so the callback should
                    // say the chain isn't happy.
                    Assert.Equal(SslPolicyErrors.RemoteCertificateChainErrors, sslPolicyErrors);

                    Assert.Contains(
                        chain.ChainElements[0].ChainElementStatus,
                        cs => cs.Status == X509ChainStatusFlags.Revoked);
                }

                return true;
            }
        }

        private async Task EndToEndHelper(string host)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(host, 443);
                }
                catch (Exception ex)
                {
                    // if we cannot connect skip the test instead of failing.
                    throw new SkipTestException($"Unable to connect to '{host}': {ex.Message}");
                }

                using (SslStream sslStream = new SslStream(client.GetStream(), false, RemoteHttpsCertValidation, null))
                {
                    await sslStream.AuthenticateAsClientAsync(host);
                }
            }
        }

        private bool RemoteHttpsCertValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Assert.Equal(SslPolicyErrors.None, sslPolicyErrors);

            return true;
        }
    }
}
