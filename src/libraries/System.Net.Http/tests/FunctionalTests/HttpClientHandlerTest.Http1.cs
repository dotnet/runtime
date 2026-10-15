// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    // This class is dedicated to SocketHttpHandler tests specific to HTTP/1.x.
    public class HttpClientHandlerTest_Http1 : HttpClientHandlerTestBase
    {
        public HttpClientHandlerTest_Http1(ITestOutputHelper output) : base(output) { }

[ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/129223", typeof(PlatformDetection), nameof(PlatformDetection.IsWasi))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task SendAsync_GetWithContent_ServerReadsBody(bool chunked, bool readBody)
        {
            const string Content = "test content";
            await Http11LoopbackServerFactory.Singleton.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, uri)
                {
                    Content = new StringContent(Content),
                    Version = HttpVersion.Version11
                };
                request.Headers.TransferEncodingChunked = chunked;
                using HttpResponseMessage response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }, async server =>
            {
                await using GenericLoopbackConnection connection = await server.EstablishGenericConnectionAsync();
                HttpRequestData request = await connection.ReadRequestDataAsync(readBody);
                byte[] body = readBody ? request.Body : await connection.ReadRequestBodyAsync();
                Assert.Equal(Content, Encoding.UTF8.GetString(body));
                await connection.SendResponseAsync();
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNodeJS))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/101115", typeof(PlatformDetection), nameof(PlatformDetection.IsFirefox))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/129223", typeof(PlatformDetection), nameof(PlatformDetection.IsWasi))]
        public async Task SendAsync_HostHeader_First()
        {
            // RFC 7230  3.2.2.  Field Order
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url) { Version = HttpVersion.Version11 };
                    request.Headers.Add("X-foo", "bar");

                    Task sendTask = client.SendAsync(request);

                    string[] headers = (await server.AcceptConnectionSendResponseAndCloseAsync()).ToArray();
                    await sendTask;

                    Assert.StartsWith("Host", headers[1]);
                }
            });
        }
    }
}
