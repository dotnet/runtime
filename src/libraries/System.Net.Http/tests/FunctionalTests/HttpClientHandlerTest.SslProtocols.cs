// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Security;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "SslProtocols not supported on UAP")]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "dotnet/corefx #16805")]
    public partial class HttpClientHandler_SslProtocols_Test : HttpClientTestBase
    {
        [Fact]
        public void DefaultProtocols_MatchesExpected()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Equal(SslProtocols.None, handler.SslProtocols);
            }
        }

        [Theory]
        [InlineData(SslProtocols.None)]
        [InlineData(SslProtocols.Tls)]
        [InlineData(SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls11 | SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12)]
        public void SetGetProtocols_Roundtrips(SslProtocols protocols)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                handler.SslProtocols = protocols;
                Assert.Equal(protocols, handler.SslProtocols);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task SetProtocols_AfterRequest_ThrowsException()
        {
            if (!BackendSupportsSslConfiguration)
            {
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = LoopbackServer.AllowAllCertificates;
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        LoopbackServer.ReadRequestAndSendResponseAsync(server),
                        client.GetAsync(url));
                });
                Assert.Throws<InvalidOperationException>(() => handler.SslProtocols = SslProtocols.Tls12);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(~SslProtocols.None)]
#pragma warning disable 0618 // obsolete warning
        [InlineData(SslProtocols.Ssl2)]
        [InlineData(SslProtocols.Ssl3)]
        [InlineData(SslProtocols.Ssl2 | SslProtocols.Ssl3)]
        [InlineData(SslProtocols.Ssl2 | SslProtocols.Ssl3 | SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12)]
#pragma warning restore 0618
        public void DisabledProtocols_SetSslProtocols_ThrowsException(SslProtocols disabledProtocols)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Throws<NotSupportedException>(() => handler.SslProtocols = disabledProtocols);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(SslProtocols.Tls, false)]
        [InlineData(SslProtocols.Tls, true)]
        [InlineData(SslProtocols.Tls11, false)]
        [InlineData(SslProtocols.Tls11, true)]
        [InlineData(SslProtocols.Tls12, false)]
        [InlineData(SslProtocols.Tls12, true)]
        public async Task GetAsync_AllowedSSLVersion_Succeeds(SslProtocols acceptedProtocol, bool requestOnlyThisProtocol)
        {
            if (!BackendSupportsSslConfiguration)
            {
                return;
            }

            if (UseManagedHandler)
            {
                // TODO #26186: The managed handler is failing on some OSes.
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = LoopbackServer.AllowAllCertificates;

                if (requestOnlyThisProtocol)
                {
                    handler.SslProtocols = acceptedProtocol;
                }
                var options = new LoopbackServer.Options { UseSsl = true, SslProtocols = acceptedProtocol };
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        LoopbackServer.ReadRequestAndSendResponseAsync(server, options: options),
                        client.GetAsync(url));
                }, options);
            }
        }

        public static readonly object[][] SupportedSSLVersionServers =
        {
            new object[] {SslProtocols.Tls, Configuration.Http.TLSv10RemoteServer},
            new object[] {SslProtocols.Tls11, Configuration.Http.TLSv11RemoteServer},
            new object[] {SslProtocols.Tls12, Configuration.Http.TLSv12RemoteServer},
        };

        // This test is logically the same as the above test, albeit using remote servers
        // instead of local ones.  We're keeping it for now (as outerloop) because it helps
        // to validate against another SSL implementation that what we mean by a particular
        // TLS version matches that other implementation.
        [OuterLoop("Avoid www.ssllabs.com dependency in innerloop.")]
        [Theory]
        [MemberData(nameof(SupportedSSLVersionServers))]
        public async Task GetAsync_SupportedSSLVersion_Succeeds(SslProtocols sslProtocols, string url)
        {
            if (UseManagedHandler)
            {
                // TODO #26186: The managed handler is failing on some OSes.
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                if (PlatformDetection.IsRedHatFamily7)
                {
                    // Default protocol selection is always TLSv1 on Centos7 libcurl 7.29.0
                    // Hence, set the specific protocol on HttpClient that is required by test
                    handler.SslProtocols = sslProtocols;
                }
                using (var client = new HttpClient(handler))
                {
                    (await RemoteServerQuery.Run(() => client.GetAsync(url), remoteServerExceptionWrapper, url)).Dispose();
                }
            }
        }

        public Func<Exception, bool> remoteServerExceptionWrapper = (exception) =>
        {
            Type exceptionType = exception.GetType();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // On linux, taskcanceledexception is thrown.
                return exceptionType.Equals(typeof(TaskCanceledException));
            }
            else
            {
                // The internal exceptions return operation timed out.
                return exceptionType.Equals(typeof(HttpRequestException)) && exception.InnerException.Message.Contains("timed out");
            }
        };

        public static readonly object[][] NotSupportedSSLVersionServers =
        {
            new object[] {"SSLv2", Configuration.Http.SSLv2RemoteServer},
            new object[] {"SSLv3", Configuration.Http.SSLv3RemoteServer},
        };

        // It would be easy to remove the dependency on these remote servers if we didn't
        // explicitly disallow creating SslStream with SSLv2/3.  Since we explicitly throw
        // when trying to use such an SslStream, we can't stand up a localhost server that
        // only speaks those protocols.
        [OuterLoop("Avoid www.ssllabs.com dependency in innerloop.")]
        [Theory]
        [MemberData(nameof(NotSupportedSSLVersionServers))]
        public async Task GetAsync_UnsupportedSSLVersion_Throws(string name, string url)
        {
            if (!SSLv3DisabledByDefault)
            {
                return;
            }

            if (UseManagedHandler && !PlatformDetection.IsWindows10Version1607OrGreater)
            {
                // On Windows, https://github.com/dotnet/corefx/issues/21925#issuecomment-313408314
                // On Linux, an older version of OpenSSL may permit negotiating SSLv3.
                return;
            }

            using (HttpClient client = CreateHttpClient())
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => RemoteServerQuery.Run(() => client.GetAsync(url), remoteServerExceptionWrapper, url));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ConditionalFact(nameof(SslDefaultsToTls12))]
        public async Task GetAsync_NoSpecifiedProtocol_DefaultsToTls12()
        {
            if (!BackendSupportsSslConfiguration)
                return;
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = LoopbackServer.AllowAllCertificates;

                var options = new LoopbackServer.Options { UseSsl = true };
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        client.GetAsync(url),
                        LoopbackServer.AcceptSocketAsync(server, async (s, stream, reader, writer) =>
                        {
                            Assert.Equal(SslProtocols.Tls12, Assert.IsType<SslStream>(stream).SslProtocol);
                            await LoopbackServer.ReadWriteAcceptedAsync(s, reader, writer);
                            return null;
                        }, options));
                }, options);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(SslProtocols.Tls11, SslProtocols.Tls, typeof(IOException))]
        [InlineData(SslProtocols.Tls12, SslProtocols.Tls11, typeof(IOException))]
        [InlineData(SslProtocols.Tls, SslProtocols.Tls12, typeof(AuthenticationException))]
        public async Task GetAsync_AllowedSSLVersionDiffersFromServer_ThrowsException(
            SslProtocols allowedProtocol, SslProtocols acceptedProtocol, Type exceptedServerException)
        {
            if (!BackendSupportsSslConfiguration)
                return;
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.SslProtocols = allowedProtocol;
                handler.ServerCertificateCustomValidationCallback = LoopbackServer.AllowAllCertificates;

                var options = new LoopbackServer.Options { UseSsl = true, SslProtocols = acceptedProtocol };
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        Assert.ThrowsAsync(exceptedServerException, () => LoopbackServer.ReadRequestAndSendResponseAsync(server, options: options)),
                        Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url)));
                }, options);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ActiveIssue(8538, TestPlatforms.Windows)]
        [Fact]
        public async Task GetAsync_DisallowTls10_AllowTls11_AllowTls12()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12;
                handler.ServerCertificateCustomValidationCallback = LoopbackServer.AllowAllCertificates;

                if (BackendSupportsSslConfiguration)
                {
                    LoopbackServer.Options options = new LoopbackServer.Options { UseSsl = true };

                    options.SslProtocols = SslProtocols.Tls;
                    await LoopbackServer.CreateServerAsync(async (server, url) =>
                    {
                        await TestHelper.WhenAllCompletedOrAnyFailed(
                            Assert.ThrowsAsync<IOException>(() => LoopbackServer.ReadRequestAndSendResponseAsync(server, options: options)),
                            Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url)));
                    }, options);

                    foreach (var prot in new[] { SslProtocols.Tls11, SslProtocols.Tls12 })
                    {
                        options.SslProtocols = prot;
                        await LoopbackServer.CreateServerAsync(async (server, url) =>
                        {
                            await TestHelper.WhenAllCompletedOrAnyFailed(
                                LoopbackServer.ReadRequestAndSendResponseAsync(server, options: options),
                                client.GetAsync(url));
                        }, options);
                    }
                }
                else
                {
                    await Assert.ThrowsAnyAsync<NotSupportedException>(() => client.GetAsync($"http://{Guid.NewGuid().ToString()}/"));
                }
            }
        }

        private static bool SslDefaultsToTls12 => !PlatformDetection.IsWindows7;
        // TLS 1.2 may not be enabled on Win7
        // https://technet.microsoft.com/en-us/library/dn786418.aspx#BKMK_SchannelTR_TLS12
    }
}
