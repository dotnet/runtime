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

        /// <summary>
        /// These are public interop test servers for various QUIC and HTTP/3 implementations,
        /// taken from https://github.com/quicwg/base-drafts/wiki/Implementations
        /// </summary>
        [OuterLoop]
        [Theory]
        [InlineData("https://quic.rocks:4433/")] // Chromium
        [InlineData("https://www.litespeedtech.com/")] // LiteSpeed
        [InlineData("https://quic.tech:8443/")] // Cloudflare
        public async Task Public_Interop_Success(string uri)
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
    }
}
