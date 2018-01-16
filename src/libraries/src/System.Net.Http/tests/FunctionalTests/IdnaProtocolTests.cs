// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public abstract class IdnaProtocolTests : HttpClientTestBase
    {
        private const string s_IdnaHost = "âbçdë.com";
        private static Uri s_IdnaUri = new Uri($"http://{s_IdnaHost}/");
        private static string s_EncodedHostName = s_IdnaUri.IdnHost;
        private static string s_EncodedUri = $"http://{s_EncodedHostName}/";

        [Fact]
        public async Task InternationalUrl_UsesIdnaEncoding_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                // We don't actually want to do DNS lookup on the IDNA host name in the URL.
                // So instead, configure the loopback server as a proxy so we will send to it.
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.UseProxy = true;
                handler.Proxy = new SimpleWebProxy(url);

                using (HttpClient client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(s_IdnaUri);
                    Task<List<string>> serverTask = LoopbackServer.ReadRequestAndSendResponseAsync(server, LoopbackServer.DefaultHttpResponse);

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    List<string> requestLines = await serverTask;

                    // Note since we're using a proxy, host name is included in the request line
                    Assert.Equal($"GET http://{s_EncodedHostName}/ HTTP/1.1", requestLines[0]);
                    Assert.Contains($"Host: {s_EncodedHostName}", requestLines);
                }
            });
        }

        [ActiveIssue(26355)] // We aren't doing IDNA encoding properly
        [Fact]
        public async Task InternationalRequestHeaderValues_UsesIdnaEncoding_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = new HttpClient(CreateHttpClientHandler()))
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Host = s_IdnaHost;
                    request.Headers.Referrer = s_IdnaUri;
                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
                    Task<List<string>> serverTask = LoopbackServer.ReadRequestAndSendResponseAsync(server, LoopbackServer.DefaultHttpResponse);

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    List<string> requestLines = await serverTask;

                    Assert.Contains($"Host: {s_EncodedHostName}", requestLines);
                    Assert.Contains($"Referer: {s_EncodedUri}", requestLines);
                }
            });
        }

        [ActiveIssue(26355)] // We aren't doing IDNA decoding properly
        [Fact]
        public async Task InternationalResponseHeaderValues_UsesIdnaDecoding_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.AllowAutoRedirect = false;

                using (HttpClient client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<List<string>> serverTask = LoopbackServer.ReadRequestAndSendResponseAsync(server, 
                        $"HTTP/1.1 302 Redirect\r\nLocation: {s_EncodedUri}\r\n\r\n");

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    HttpResponseMessage response = await getResponseTask;
                    Console.WriteLine($"Location header={response.Headers.Location}");
                    Console.WriteLine($"Location header.Host={response.Headers.Location.Host}");
                    Console.WriteLine($"Location header.IdnHost={response.Headers.Location.IdnHost}");
                }
            });
        }

        sealed class SimpleWebProxy : IWebProxy
        {
            private Uri _proxyUri;

            public SimpleWebProxy(Uri proxyUri)
            {
                _proxyUri = proxyUri;
            }

            public ICredentials Credentials { get => null; set => throw new NotImplementedException(); }
            public Uri GetProxy(Uri destination) => _proxyUri;
            public bool IsBypassed(Uri host) => false;
        }
    }

    public sealed class DefaultHandler_IdnaProtocolTests : IdnaProtocolTests
    {
    }

    public sealed class ManagedHandler_IdnaProtocolTests : IdnaProtocolTests
    {
        protected override bool UseManagedHandler => true;
    }
}
