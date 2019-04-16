// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{

    public class PlatformHandler_HttpClientHandler : HttpClientHandlerTestBase
    {
        protected override bool UseSocketsHttpHandler => false;

        public PlatformHandler_HttpClientHandler(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [PlatformSpecific(TestPlatforms.Windows)] // Curl has issues with trailing headers https://github.com/curl/curl/issues/1354
        public async Task GetAsync_TrailingHeaders_Ignored(bool includeTrailerHeader)
        {
           await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        getResponseTask,
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            "HTTP/1.1 200 OK\r\n" +
                            "Connection: close\r\n" +
                            "Transfer-Encoding: chunked\r\n" +
                            (includeTrailerHeader ? "Trailer: MyCoolTrailerHeader\r\n" : "") +
                            "\r\n" +
                            "4\r\n" +
                            "data\r\n" +
                            "0\r\n" +
                            "MyCoolTrailerHeader: amazingtrailer\r\n" +
                            "\r\n"));

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        if (includeTrailerHeader)
                        {
                            Assert.Contains("MyCoolTrailerHeader", response.Headers.GetValues("Trailer"));
                        }
                        Assert.False(response.Headers.Contains("MyCoolTrailerHeader"), "Trailer should have been ignored");

                        string data = await response.Content.ReadAsStringAsync();
                        Assert.Contains("data", data);
                        Assert.DoesNotContain("MyCoolTrailerHeader", data);
                        Assert.DoesNotContain("amazingtrailer", data);
                    }
                }
            });
        }
    }

    public sealed class PlatformHandler_HttpClientHandler_Asynchrony_Test : HttpClientHandler_Asynchrony_Test
    {
        public PlatformHandler_HttpClientHandler_Asynchrony_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpProtocolTests : HttpProtocolTests
    {
        public PlatformHandler_HttpProtocolTests(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpProtocolTests_Dribble : HttpProtocolTests_Dribble
    {
        public PlatformHandler_HttpProtocolTests_Dribble(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_DiagnosticsTest : DiagnosticsTest
    {
        public PlatformHandler_DiagnosticsTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClient_SelectedSites_Test : HttpClient_SelectedSites_Test
    {
        public PlatformHandler_HttpClient_SelectedSites_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientEKUTest : HttpClientEKUTest
    {
        public PlatformHandler_HttpClientEKUTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

#if netcoreapp
    public sealed class PlatformHandler_HttpClientHandler_Decompression_Tests : HttpClientHandler_Decompression_Test
    {
        public PlatformHandler_HttpClientHandler_Decompression_Tests(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test : HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test
    {
        public PlatformHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }
#endif

    public sealed class PlatformHandler_HttpClientHandler_ClientCertificates_Test : HttpClientHandler_ClientCertificates_Test
    {
        public PlatformHandler_HttpClientHandler_ClientCertificates_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_DefaultProxyCredentials_Test : HttpClientHandler_DefaultProxyCredentials_Test
    {
        public PlatformHandler_HttpClientHandler_DefaultProxyCredentials_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_MaxConnectionsPerServer_Test : HttpClientHandler_MaxConnectionsPerServer_Test
    {
        public PlatformHandler_HttpClientHandler_MaxConnectionsPerServer_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_ServerCertificates_Test : HttpClientHandler_ServerCertificates_Test
    {
        public PlatformHandler_HttpClientHandler_ServerCertificates_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_PostScenarioTest : PostScenarioTest
    {
        public PlatformHandler_PostScenarioTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_ResponseStreamTest : ResponseStreamTest
    {
        public PlatformHandler_ResponseStreamTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_SslProtocols_Test : HttpClientHandler_SslProtocols_Test
    {
        public PlatformHandler_HttpClientHandler_SslProtocols_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_Proxy_Test : HttpClientHandler_Proxy_Test
    {
        public PlatformHandler_HttpClientHandler_Proxy_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_SchSendAuxRecordHttpTest : SchSendAuxRecordHttpTest
    {
        public PlatformHandler_SchSendAuxRecordHttpTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientMiniStress : HttpClientMiniStress
    {
        public PlatformHandler_HttpClientMiniStress(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandlerTest : HttpClientHandlerTest
    {
        public PlatformHandler_HttpClientHandlerTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandlerTest_AutoRedirect : HttpClientHandlerTest_AutoRedirect
    {
        public PlatformHandlerTest_AutoRedirect(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_DefaultCredentialsTest : DefaultCredentialsTest
    {
        public PlatformHandler_DefaultCredentialsTest(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_IdnaProtocolTests : IdnaProtocolTests
    {
        public PlatformHandler_IdnaProtocolTests(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
        // WinHttp on Win7 does not support IDNA
        protected override bool SupportsIdna => !PlatformDetection.IsWindows7 && !PlatformDetection.IsFullFramework;
    }

    public sealed class PlatformHandler_HttpRetryProtocolTests : HttpRetryProtocolTests
    {
        public PlatformHandler_HttpRetryProtocolTests(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandlerTest_Cookies : HttpClientHandlerTest_Cookies
    {
        public PlatformHandlerTest_Cookies(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandlerTest_Cookies_Http11 : HttpClientHandlerTest_Cookies_Http11
    {
        public PlatformHandlerTest_Cookies_Http11(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_MaxResponseHeadersLength_Test : HttpClientHandler_MaxResponseHeadersLength_Test
    {
        public PlatformHandler_HttpClientHandler_MaxResponseHeadersLength_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_Cancellation_Test : HttpClientHandler_Cancellation_Test
    {
        public PlatformHandler_HttpClientHandler_Cancellation_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    public sealed class PlatformHandler_HttpClientHandler_Authentication_Test : HttpClientHandler_Authentication_Test
    {
        public PlatformHandler_HttpClientHandler_Authentication_Test(ITestOutputHelper output) : base(output) { }
        protected override bool UseSocketsHttpHandler => false;
    }

    // Enable this to run HTTP2 tests on platform handler
#if PLATFORM_HANDLER_HTTP2_TESTS
    public sealed class PlatformHandlerTest_Http2 : HttpClientHandlerTest_Http2
    {
        protected override bool UseSocketsHttpHandler => false;
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.SupportsAlpn))]
    public sealed class PlatformHandlerTest_Cookies_Http2 : HttpClientHandlerTest_Cookies
    {
        protected override bool UseSocketsHttpHandler => false;
        protected override bool UseHttp2LoopbackServer => true;
    }
#endif
}
