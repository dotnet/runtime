// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandlerTest_Http3 : HttpClientHandlerTestBase
    {
        protected override Version UseVersion => HttpVersion30;

        public HttpClientHandlerTest_Http3(ITestOutputHelper output) : base(output)
        {
        }

        [OuterLoop]
        [Theory]
        [MemberData(nameof(InteropUris))]
        public async Task Public_Interop_ExactVersion_Success(string uri)
        {
            using HttpClient client = CreateHttpClient();
            using HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri, UriKind.Absolute),
                Version = HttpVersion30,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };
            using HttpResponseMessage response = await client.SendAsync(request).TimeoutAfter(20_000);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(3, response.Version.Major);
        }

        [OuterLoop]
        [Theory]
        [MemberData(nameof(InteropUris))]
        public async Task Public_Interop_Upgrade_Success(string uri)
        {
            using HttpClient client = CreateHttpClient();

            // First request uses HTTP/1 or HTTP/2 and receives an Alt-Svc either by header or (with HTTP/2) by frame.

            using (HttpRequestMessage requestA = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri, UriKind.Absolute),
                Version = HttpVersion30,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            })
            {
                using HttpResponseMessage responseA = await client.SendAsync(requestA).TimeoutAfter(20_000);
                Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
                Assert.NotEqual(3, responseA.Version.Major);
            }

            // Second request uses HTTP/3.

            using (HttpRequestMessage requestB = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri, UriKind.Absolute),
                Version = HttpVersion30,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            })
            {
                using HttpResponseMessage responseB = await client.SendAsync(requestB).TimeoutAfter(20_000);

                Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
                Assert.NotEqual(3, responseB.Version.Major);
            }
        }

        /// <summary>
        /// These are public interop test servers for various QUIC and HTTP/3 implementations,
        /// taken from https://github.com/quicwg/base-drafts/wiki/Implementations
        /// </summary>
        public static TheoryData<string> InteropUris() =>
            new TheoryData<string>
            {
                { "https://quic.rocks:4433/" }, // Chromium
                { "https://http3-test.litespeedtech.com:4433/" }, // LiteSpeed
                { "https://quic.tech:8443/" } // Cloudflare
            };
    }
}
