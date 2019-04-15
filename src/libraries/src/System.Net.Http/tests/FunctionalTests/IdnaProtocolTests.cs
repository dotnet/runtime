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
    public abstract class IdnaProtocolTests : HttpClientHandlerTestBase
    {
        protected abstract bool SupportsIdna { get; }

        public IdnaProtocolTests(ITestOutputHelper output) : base(output) { }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP does not support custom proxies.")]
        [Theory]
        [MemberData(nameof(InternationalHostNames))]
        public async Task InternationalUrl_UsesIdnaEncoding_Success(string hostname)
        {
            if (!SupportsIdna)
            {
                return;
            }

            Uri uri = new Uri($"http://{hostname}/");

            await LoopbackServer.CreateServerAsync(async (server, serverUrl) =>
            {
                // We don't actually want to do DNS lookup on the IDNA host name in the URL.
                // So instead, configure the loopback server as a proxy so we will send to it.
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(serverUrl.ToString());

                using (HttpClient client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(uri);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    List<string> requestLines = await serverTask;

                    // Note since we're using a proxy, host name is included in the request line
                    Assert.Equal($"GET http://{uri.IdnHost}/ HTTP/1.1", requestLines[0]);
                    Assert.Contains($"Host: {uri.IdnHost}", requestLines);
                }
            });
        }

        [ActiveIssue(26355)] // We aren't doing IDNA encoding properly
        [Theory]
        [MemberData(nameof(InternationalHostNames))]
        public async Task InternationalRequestHeaderValues_UsesIdnaEncoding_Success(string hostname)
        {
            if (!SupportsIdna)
            {
                return;
            }

            Uri uri = new Uri($"http://{hostname}/");

            await LoopbackServer.CreateServerAsync(async (server, serverUrl) =>
            {
                using (HttpClient client = new HttpClient(CreateHttpClientHandler()))
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, serverUrl);
                    request.Headers.Host = hostname;
                    request.Headers.Referrer = uri;
                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    List<string> requestLines = await serverTask;

                    Assert.Contains($"Host: {uri.IdnHost}", requestLines);
                    Assert.Contains($"Referer: http://{uri.IdnHost}/", requestLines);
                }
            });
        }

        [ActiveIssue(26355)] // We aren't doing IDNA decoding properly
        [Theory]
        [MemberData(nameof(InternationalHostNames))]
        public async Task InternationalResponseHeaderValues_UsesIdnaDecoding_Success(string hostname)
        {
            if (!SupportsIdna)
            {
                return;
            }

            Uri uri = new Uri($"http://{hostname}/");

            await LoopbackServer.CreateServerAsync(async (server, serverUrl) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.AllowAutoRedirect = false;

                using (HttpClient client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(serverUrl);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync(
                        HttpStatusCode.Found, "Location: http://{uri.IdnHost}/\r\n");

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    HttpResponseMessage response = await getResponseTask;

                    Assert.Equal(uri, response.Headers.Location);
                }
            });
        }

        public static IEnumerable<object[]> InternationalHostNames()
        {
            // Latin-1 supplement
            yield return new object[] { "\u00E1.com" };
            yield return new object[] { "\u00E1b\u00E7d\u00EB.com" };
            yield return new object[] { "b\u00E7.com" };
            yield return new object[] { "b\u00E7d.com" };

            // Hebrew
            yield return new object[] { "\u05E1.com" };
            yield return new object[] { "\u05D1\u05F1.com" };

            // Katakana
            yield return new object[] { "\u30A5.com" };
            yield return new object[] { "\u30B6\u30C7\u30D8.com" };
        }
    }
}
