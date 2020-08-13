// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpClientHandler;
#endif

    public abstract class HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test : HttpClientHandlerTestBase
    {
        private static bool ClientSupportsDHECipherSuites => (!PlatformDetection.IsWindows || PlatformDetection.IsWindows10Version1607OrGreater);

        public HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test(ITestOutputHelper output) : base(output) { }

#if !WINHTTPHANDLER_TEST
        [Fact]
        public void SingletonReturnsTrue()
        {
            Assert.NotNull(HttpClientHandler.DangerousAcceptAnyServerCertificateValidator);
            Assert.Same(HttpClientHandler.DangerousAcceptAnyServerCertificateValidator, HttpClientHandler.DangerousAcceptAnyServerCertificateValidator);
            Assert.True(HttpClientHandler.DangerousAcceptAnyServerCertificateValidator(null, null, null, SslPolicyErrors.None));
        }
#endif

        [Theory]
        [InlineData(SslProtocols.Tls12, false)] // try various protocols to ensure we correctly set versions even when accepting all certs
        [InlineData(SslProtocols.Tls12, true)]
        [InlineData(SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false)]
        [InlineData(SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, true)]
#if !NETFRAMEWORK
        [InlineData(SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false)]
        [InlineData(SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, true)]
#endif
        [InlineData(SslProtocols.None, false)]
        [InlineData(SslProtocols.None, true)]
        public async Task SetDelegate_ConnectionSucceeds(SslProtocols acceptedProtocol, bool requestOnlyThisProtocol)
        {
            // Overriding flag for the same reason we skip tests on Catalina
            // On OSX 10.13-10.14 we can override this flag to enable the scenario
            requestOnlyThisProtocol |= PlatformDetection.IsOSX && acceptedProtocol == SslProtocols.Tls;

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                if (requestOnlyThisProtocol)
                {
                    handler.SslProtocols = acceptedProtocol;
                }
                else
                {
                    // Explicitly setting protocols clears implementation default
                    // restrictions on minimum TLS/SSL version
                    // We currently know that some platforms like Debian 10 OpenSSL
                    // will by default block < TLS 1.2
#if !NETFRAMEWORK
                    handler.SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
#else
                    handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
#endif
                }

                var options = new LoopbackServer.Options { UseSsl = true, SslProtocols = acceptedProtocol };
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        server.AcceptConnectionSendResponseAndCloseAsync(),
                        client.GetAsync(url));
                }, options);
            }
        }

        public static readonly object[][] InvalidCertificateServers =
        {
            new object[] { Configuration.Http.ExpiredCertRemoteServer },
            new object[] { Configuration.Http.SelfSignedCertRemoteServer },
            new object[] { Configuration.Http.WrongHostNameCertRemoteServer },
        };

        [OuterLoop]
        [ConditionalTheory(nameof(ClientSupportsDHECipherSuites))]
        [MemberData(nameof(InvalidCertificateServers))]
        public async Task InvalidCertificateServers_CertificateValidationDisabled_Succeeds(string url)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                (await client.GetAsync(url)).Dispose();
            }
        }
    }
}
