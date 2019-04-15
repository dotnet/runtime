// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test : HttpClientHandlerTestBase
    {
        private static bool ClientSupportsDHECipherSuites => (!PlatformDetection.IsWindows || PlatformDetection.IsWindows10Version1607OrGreater);

        public HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void SingletonReturnsTrue()
        {
            Assert.NotNull(HttpClientHandler.DangerousAcceptAnyServerCertificateValidator);
            Assert.Same(HttpClientHandler.DangerousAcceptAnyServerCertificateValidator, HttpClientHandler.DangerousAcceptAnyServerCertificateValidator);
            Assert.True(HttpClientHandler.DangerousAcceptAnyServerCertificateValidator(null, null, null, SslPolicyErrors.None));
        }

        [Theory]
        [InlineData(SslProtocols.Tls, false)] // try various protocols to ensure we correctly set versions even when accepting all certs
        [InlineData(SslProtocols.Tls, true)]
        [InlineData(SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false)]
        [InlineData(SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, true)]
        [InlineData(SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false)]
        [InlineData(SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, true)]
        [InlineData(SslProtocols.None, false)]
        [InlineData(SslProtocols.None, true)]
        public async Task SetDelegate_ConnectionSucceeds(SslProtocols acceptedProtocol, bool requestOnlyThisProtocol)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                // Refer issue: #22089
                // When the server uses SslProtocols.Tls, on MacOS, SecureTransport ends up picking a cipher suite
                // for TLS1.2, even though server said it was only using TLS1.0. LibreSsl throws error that
                // wrong cipher is used for TLs1.0.
                if (requestOnlyThisProtocol || (PlatformDetection.IsMacOsHighSierraOrHigher && acceptedProtocol == SslProtocols.Tls))
                {
                    handler.SslProtocols = acceptedProtocol;
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

        [OuterLoop] // TODO: Issue #11345
        [ConditionalTheory(nameof(ClientSupportsDHECipherSuites))]
        [MemberData(nameof(InvalidCertificateServers))]
        public async Task InvalidCertificateServers_CertificateValidationDisabled_Succeeds(string url)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                (await client.GetAsync(url)).Dispose();
            }
        }
    }
}
