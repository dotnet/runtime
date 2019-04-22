// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    // This class is dedicated to SocketHttpHandler tests specific to HTTP/1.x.
    public class HttpClientHandlerTest_Http1 : HttpClientHandlerTestBase
    {
        protected override bool UseSocketsHttpHandler => true;
        protected override bool UseHttp2LoopbackServer => false;

        public HttpClientHandlerTest_Http1(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task SendAsync_HostHeader_First()
        {
            // RFC 7230  3.2.2.  Field Order
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("X-foo", "bar");

                    Task sendTask = client.SendAsync(request);

                    string[] headers = (await server.AcceptConnectionSendResponseAndCloseAsync()).ToArray();
                    await sendTask;

                    Assert.True(headers[1].StartsWith("Host"));
                }
            });
        }
    }
}

