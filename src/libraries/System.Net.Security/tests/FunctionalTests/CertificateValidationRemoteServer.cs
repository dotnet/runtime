// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.X509Certificates.Tests.Common;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Win32.SafeHandles;
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
        [InlineData("www.github.com.")]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/70981", TestPlatforms.OSX)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/68206", TestPlatforms.Android)]
        public Task ConnectWithRevocation_WithCallback(bool checkRevocation)
        {
            X509RevocationMode mode = checkRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
            return ConnectWithRevocation_WithCallback_Core(mode);
        }

        [PlatformSpecific(TestPlatforms.Linux)]
        [ConditionalTheory]
        [OuterLoop("Subject to system load race conditions")]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public Task ConnectWithRevocation_StapledOcsp(bool offlineContext, bool noIntermediates)
        {
            // Offline will only work if
            // a) the revocation has been checked recently enough that it is cached, or
            // b) the server stapled the response
            //
            // At high load, the server's background fetch might not have completed before
            // this test runs.
            return ConnectWithRevocation_WithCallback_Core(X509RevocationMode.Offline, offlineContext, noIntermediates);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public Task ConnectWithRevocation_ServerCertWithoutContext_NoStapledOcsp()
        {
            // When using specific certificate, OCSP is disabled e.g. when SslStreamCertificateContext is passed in explicitly.
            return ConnectWithRevocation_WithCallback_Core(X509RevocationMode.Offline, offlineContext: null);
        }

#if WINDOWS
        [ConditionalTheory]
        [OuterLoop("Uses external servers")]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(X509RevocationMode.Offline)]
        [InlineData(X509RevocationMode.Online)]
        [InlineData(X509RevocationMode.NoCheck)]
        public Task ConnectWithRevocation_RemoteServer_StapledOcsp_FromWindows(X509RevocationMode revocationMode)
        {
            // This test could ideally end at the Client Hello, because it really only wants to
            // ensure that the status_request extension was asserted.  Since the SslStream tests
            // do not currently attempt to intercept and inspect the Client Hello, this test
            // obtains the data indirectly: by talking to a host known to do OCSP Server Stapling
            // with revocation in Offline mode.
            // Unfortunately, this test will fail if the remote host stops doing server stapling,
            // but it's the best we can do right now.

            string serverName = Configuration.Http.Http2Host;

            SslClientAuthenticationOptions clientOpts = new SslClientAuthenticationOptions
            {
                TargetHost = serverName,
                RemoteCertificateValidationCallback = CertificateValidationCallback,
                CertificateRevocationCheckMode = revocationMode,
            };

            return EndToEndHelper(clientOpts);

            static bool CertificateValidationCallback(
                object sender,
                X509Certificate? certificate,
                X509Chain? chain,
                SslPolicyErrors sslPolicyErrors)
            {
                Assert.NotNull(certificate);

                using (SafeCertContextHandle ctx = new SafeCertContextHandle(certificate.Handle, ownsHandle: false))
                {
                    bool hasStapledOcsp =
                        ctx.CertHasProperty(Interop.Crypt32.CertContextPropId.CERT_OCSP_RESPONSE_PROP_ID);

                    if (((SslStream)sender).CheckCertRevocationStatus)
                    {
                        Assert.True(hasStapledOcsp, "Cert has stapled OCSP data");
                    }
                    else
                    {
                        Assert.False(hasStapledOcsp, "Cert has stapled OCSP data");
                    }
                }

                return true;
            }
        }
#endif

        private async Task ConnectWithRevocation_WithCallback_Core(
            X509RevocationMode revocationMode,
            bool? offlineContext = false,
            bool noIntermediates = false)
        {
            string offlinePart = offlineContext.HasValue ? offlineContext.GetValueOrDefault().ToString().ToLower() : "null";
            string serverName = $"{revocationMode.ToString().ToLower()}.{offlinePart}.server.example";

            (Stream clientStream, Stream serverStream) = TestHelper.GetConnectedStreams();

            CertificateAuthority.BuildPrivatePki(
                PkiOptions.EndEntityRevocationViaOcsp | PkiOptions.CrlEverywhere,
                out RevocationResponder responder,
                out CertificateAuthority rootAuthority,
                out CertificateAuthority[] intermediateAuthorities,
                out X509Certificate2 serverCert,
                intermediateAuthorityCount: noIntermediates ? 0 : 1,
                subjectName: serverName,
                keySize: 2048,
                extensions: Configuration.Certificates.BuildTlsServerCertExtensions(serverName));

            CertificateAuthority issuingAuthority = noIntermediates ? rootAuthority : intermediateAuthorities[0];
            X509Certificate2 issuerCert = issuingAuthority.CloneIssuerCert();
            X509Certificate2 rootCert = rootAuthority.CloneIssuerCert();

            SslClientAuthenticationOptions clientOpts = new SslClientAuthenticationOptions
            {
                TargetHost = serverName,
                RemoteCertificateValidationCallback = CertificateValidationCallback,
                CertificateChainPolicy = new X509ChainPolicy
                {
                    RevocationMode = revocationMode,
                    TrustMode = X509ChainTrustMode.CustomRootTrust,

                    // The offline test will not know about revocation for the intermediate,
                    // so change the policy to only check the end certificate.
                    RevocationFlag = X509RevocationFlag.EndCertificateOnly,

                    ExtraStore =
                    {
                        issuerCert,
                    },
                    CustomTrustStore =
                    {
                        rootCert,
                    },
                },
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                X509Certificate2 temp = new X509Certificate2(serverCert.Export(X509ContentType.Pkcs12));
                serverCert.Dispose();
                serverCert = temp;
            }

            try
            {
                await using (clientStream)
                await using (serverStream)
                using (responder)
                using (rootAuthority)
                using (serverCert)
                using (issuerCert)
                using (rootCert)
                await using (SslStream tlsClient = new SslStream(clientStream))
                await using (SslStream tlsServer = new SslStream(serverStream))
                {
                    issuingAuthority.Revoke(serverCert, serverCert.NotBefore);

                    SslServerAuthenticationOptions serverOpts = new SslServerAuthenticationOptions();

                    if (offlineContext.HasValue)
                    {
                        serverOpts.ServerCertificateContext = SslStreamCertificateContext.Create(
                            serverCert,
                            new X509Certificate2Collection(issuerCert),
                            offlineContext.GetValueOrDefault());

                        if (revocationMode == X509RevocationMode.Offline)
                        {
                            if (offlineContext.GetValueOrDefault(false))
                            {
                                // Add a delay just to show we're not winning because of race conditions.
                                await Task.Delay(200);
                            }
                            else
                            {
                                if (!OperatingSystem.IsLinux())
                                {
                                    throw new InvalidOperationException(
                                        "This test configuration uses reflection and is only defined for Linux.");
                                }

                                FieldInfo pendingDownloadTaskField = typeof(SslStreamCertificateContext).GetField(
                                    "_pendingDownload",
                                    BindingFlags.Instance | BindingFlags.NonPublic);

                                if (pendingDownloadTaskField is null)
                                {
                                    throw new InvalidOperationException("Cannot find the pending download field.");
                                }

                                Task download = (Task)pendingDownloadTaskField.GetValue(serverOpts.ServerCertificateContext);

                                // If it's null, it should mean it has already finished. If not, it might not have.
                                if (download is not null)
                                {
                                    await download;
                                }
                            }
                        }
                    }
                    else
                    {
                        serverOpts.ServerCertificate = serverCert;
                    }

                    Task serverTask = tlsServer.AuthenticateAsServerAsync(serverOpts);
                    Task clientTask = tlsClient.AuthenticateAsClientAsync(clientOpts);

                    await TestConfiguration.WhenAllOrAnyFailedWithTimeout(clientTask, serverTask);
                }
            }
            finally
            {
                foreach (CertificateAuthority intermediateAuthority in intermediateAuthorities)
                {
                    intermediateAuthority.Dispose();
                }
            }

            static bool CertificateValidationCallback(
                object sender,
                X509Certificate? certificate,
                X509Chain? chain,
                SslPolicyErrors sslPolicyErrors)
            {
                Assert.NotNull(certificate);
                Assert.NotNull(chain);

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
                else if ((certificate.Subject.Contains(".true.server.") || certificate.Subject.Contains(".null.server.")) &&
                    chain.ChainPolicy.RevocationMode == X509RevocationMode.Offline)
                {
                    // In an Offline chain with an offline context the revocation still shouldn't
                    // process, because there's no OCSP data.
                    Assert.Equal(SslPolicyErrors.RemoteCertificateChainErrors, sslPolicyErrors);

                    X509ChainStatusFlags[] flags = chain.ChainElements[0].ChainElementStatus.Select(cs => cs.Status).ToArray();
                    Assert.Contains(X509ChainStatusFlags.RevocationStatusUnknown, flags);
                }
                else
                {
                    // Revocation was requested, and the cert is revoked, so the callback should
                    // say the chain isn't happy.
                    Assert.Equal(SslPolicyErrors.RemoteCertificateChainErrors, sslPolicyErrors);

                    X509ChainStatusFlags[] flags = chain.ChainElements[0].ChainElementStatus.Select(cs => cs.Status).ToArray();
                    Assert.Contains(X509ChainStatusFlags.Revoked, flags);
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

        private async Task EndToEndHelper(SslClientAuthenticationOptions clientOptions)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(clientOptions.TargetHost, 443);
                }
                catch (Exception ex)
                {
                    // if we cannot connect skip the test instead of failing.
                    throw new SkipTestException($"Unable to connect to '{clientOptions.TargetHost}': {ex.Message}");
                }

                using (SslStream sslStream = new SslStream(client.GetStream()))
                {
                    await sslStream.AuthenticateAsClientAsync(clientOptions);
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
