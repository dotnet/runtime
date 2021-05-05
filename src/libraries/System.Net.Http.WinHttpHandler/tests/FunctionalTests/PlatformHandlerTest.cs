// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    [ActiveIssue("https://github.com/mono/mono/issues/15005", TestRuntimes.Mono)]
    public class PlatformHandler_HttpClientHandler : HttpClientHandlerTestBase
    {
        public PlatformHandler_HttpClientHandler(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetAsync_TrailingHeaders_Ignored(bool includeTrailerHeader)
        {
           await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient(new WinHttpHandler()))
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
    }

    public sealed class PlatformHandler_HttpProtocolTests : HttpProtocolTests
    {
        public PlatformHandler_HttpProtocolTests(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpProtocolTests_Dribble : HttpProtocolTests_Dribble
    {
        public PlatformHandler_HttpProtocolTests_Dribble(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClient_SelectedSites_Test : HttpClient_SelectedSites_Test
    {
        public PlatformHandler_HttpClient_SelectedSites_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientEKUTest : HttpClientEKUTest
    {
        public PlatformHandler_HttpClientEKUTest(ITestOutputHelper output) : base(output) { }
    }

#if NETCOREAPP
    public sealed class PlatformHandler_HttpClientHandler_Decompression_Tests : HttpClientHandler_Decompression_Test
    {
        public PlatformHandler_HttpClientHandler_Decompression_Tests(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test : HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test
    {
        public PlatformHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test(ITestOutputHelper output) : base(output) { }
    }
#endif

    public sealed class PlatformHandler_HttpClientHandler_ClientCertificates_Test : HttpClientHandler_ClientCertificates_Test
    {
        public PlatformHandler_HttpClientHandler_ClientCertificates_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_DefaultProxyCredentials_Test : HttpClientHandler_DefaultProxyCredentials_Test
    {
        public PlatformHandler_HttpClientHandler_DefaultProxyCredentials_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_MaxConnectionsPerServer_Test : HttpClientHandler_MaxConnectionsPerServer_Test
    {
        public PlatformHandler_HttpClientHandler_MaxConnectionsPerServer_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_ServerCertificates_Test : HttpClientHandler_ServerCertificates_Test
    {
        public PlatformHandler_HttpClientHandler_ServerCertificates_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_PostScenarioTest : PostScenarioTest
    {
        public PlatformHandler_PostScenarioTest(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_ResponseStreamTest : ResponseStreamTest
    {
        public PlatformHandler_ResponseStreamTest(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_SslProtocols_Test : HttpClientHandler_SslProtocols_Test
    {
        public PlatformHandler_HttpClientHandler_SslProtocols_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_Proxy_Test : HttpClientHandler_Proxy_Test
    {
        public PlatformHandler_HttpClientHandler_Proxy_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_SchSendAuxRecordHttpTest : SchSendAuxRecordHttpTest
    {
        public PlatformHandler_SchSendAuxRecordHttpTest(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandlerTest : HttpClientHandlerTest
    {
        public PlatformHandler_HttpClientHandlerTest(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandlerTest_AutoRedirect : HttpClientHandlerTest_AutoRedirect
    {
        public PlatformHandlerTest_AutoRedirect(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_DefaultCredentialsTest : DefaultCredentialsTest
    {
        public PlatformHandler_DefaultCredentialsTest(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_IdnaProtocolTests : IdnaProtocolTests
    {
        public PlatformHandler_IdnaProtocolTests(ITestOutputHelper output) : base(output) { }
        // WinHttp on Win7 does not support IDNA
        protected override bool SupportsIdna => !PlatformDetection.IsWindows7;
    }

    public sealed class PlatformHandlerTest_Cookies : HttpClientHandlerTest_Cookies
    {
        public PlatformHandlerTest_Cookies(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandlerTest_Cookies_Http11 : HttpClientHandlerTest_Cookies_Http11
    {
        public PlatformHandlerTest_Cookies_Http11(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_MaxResponseHeadersLength_Test : HttpClientHandler_MaxResponseHeadersLength_Test
    {
        public PlatformHandler_HttpClientHandler_MaxResponseHeadersLength_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_Cancellation_Test : HttpClientHandler_Cancellation_Test
    {
        public PlatformHandler_HttpClientHandler_Cancellation_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_Authentication_Test : HttpClientHandler_Authentication_Test
    {
        public PlatformHandler_HttpClientHandler_Authentication_Test(ITestOutputHelper output) : base(output) { }
    }

#if NETCOREAPP
#if !WINHTTPHANDLER_TEST // [ActiveIssue("https://github.com/dotnet/runtime/issues/33930")]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows10Version1607OrGreater))]
    public sealed class PlatformHandlerTest_Cookies_Http2 : HttpClientHandlerTest_Cookies
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandlerTest_Cookies_Http2(ITestOutputHelper output) : base(output) { }
    }
#endif

    public sealed class PlatformHandler_HttpClientHandler_Asynchrony_Http2_Test : HttpClientHandler_Asynchrony_Test
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClientHandler_Asynchrony_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpProtocol_Http2_Tests : HttpProtocolTests
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpProtocol_Http2_Tests(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpProtocolTests_Http2_Dribble : HttpProtocolTests_Dribble
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpProtocolTests_Http2_Dribble(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClient_SelectedSites_Http2_Test : HttpClient_SelectedSites_Test
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClient_SelectedSites_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientEKU_Http2_Test : HttpClientEKUTest
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClientEKU_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_Decompression_Http2_Tests : HttpClientHandler_Decompression_Test
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClientHandler_Decompression_Http2_Tests(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Http2_Test : HttpClientHandler_DangerousAcceptAllCertificatesValidator_Test
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClientHandler_DangerousAcceptAllCertificatesValidator_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_ClientCertificates_Http2_Test : HttpClientHandler_ClientCertificates_Test
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClientHandler_ClientCertificates_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_DefaultProxyCredentials_Http2_Test : HttpClientHandler_DefaultProxyCredentials_Test
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClientHandler_DefaultProxyCredentials_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_MaxConnectionsPerServer_Http2_Test : HttpClientHandler_MaxConnectionsPerServer_Test
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClientHandler_MaxConnectionsPerServer_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_ServerCertificates_Http2_Test : HttpClientHandler_ServerCertificates_Test
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClientHandler_ServerCertificates_Http2_Test(ITestOutputHelper output) : base(output) {
            AllowAllHttp2Certificates = false;
        }
    }

    public sealed class PlatformHandler_PostScenario_Http2_Test : PostScenarioTest
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_PostScenario_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_SslProtocols_Http2_Test : HttpClientHandler_SslProtocols_Test
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClientHandler_SslProtocols_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_Proxy_Http2_Test : HttpClientHandler_Proxy_Test
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClientHandler_Proxy_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_SchSendAuxRecordHttp_Http2_Test : SchSendAuxRecordHttpTest
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_SchSendAuxRecordHttp_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows10Version1607OrGreater))]
    public sealed class PlatformHandler_HttpClientHandler_Http2_Test : HttpClientHandlerTest
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClientHandler_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandlerTest_AutoRedirect_Http2 : HttpClientHandlerTest_AutoRedirect
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandlerTest_AutoRedirect_Http2(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_DefaultCredentials_Http2_Test : DefaultCredentialsTest
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_DefaultCredentials_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_IdnaProtocol_Http2_Tests : IdnaProtocolTests
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_IdnaProtocol_Http2_Tests(ITestOutputHelper output) : base(output) { }
        // WinHttp on Win7 does not support IDNA
        protected override bool SupportsIdna => !PlatformDetection.IsWindows7;
    }

    public sealed class PlatformHandlerTest_Cookies_Http11_Http2 : HttpClientHandlerTest_Cookies_Http11
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandlerTest_Cookies_Http11_Http2(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_MaxResponseHeadersLength_Http2_Test : HttpClientHandler_MaxResponseHeadersLength_Test
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClientHandler_MaxResponseHeadersLength_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_Cancellation_Http2_Test : HttpClientHandler_Cancellation_Test
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClientHandler_Cancellation_Http2_Test(ITestOutputHelper output) : base(output) { }
    }

    public sealed class PlatformHandler_HttpClientHandler_Authentication_Http2_Test : HttpClientHandler_Authentication_Test
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_HttpClientHandler_Authentication_Http2_Test(ITestOutputHelper output) : base(output) { }
    }
#endif
    public sealed class PlatformHandler_ResponseStream_Http2_Test : ResponseStreamTest
    {
        protected override Version UseVersion => HttpVersion20.Value;

        public PlatformHandler_ResponseStream_Http2_Test(ITestOutputHelper output) : base(output) { }
    }
}
