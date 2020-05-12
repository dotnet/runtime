// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Test.Common;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

// Can't use "WinHttpHandler.Functional.Tests" in namespace as it won't compile.
// WinHttpHandler is a class and not a namespace and can't be part of namespace paths.
namespace System.Net.Http.WinHttpHandlerFunctional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    // Note:  Disposing the HttpClient object automatically disposes the handler within. So, it is not necessary
    // to separately Dispose (or have a 'using' statement) for the handler.
    public class WinHttpHandlerTest
    {
        private const string SlowServer = "http://httpbin.org/drip?numbytes=1&duration=1&delay=40&code=200";

        private readonly ITestOutputHelper _output;

        public WinHttpHandlerTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [OuterLoop]
        [Fact]
        public void SendAsync_SimpleGet_Success()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                var response = client.GetAsync(System.Net.Test.Common.Configuration.Http.RemoteEchoServer).Result;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                _output.WriteLine(responseContent);
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(CookieUsePolicy.UseInternalCookieStoreOnly, "cookieName1", "cookieValue1")]
        [InlineData(CookieUsePolicy.UseSpecifiedCookieContainer, "cookieName2", "cookieValue2")]
        public async Task GetAsync_RedirectResponseHasCookie_CookieSentToFinalUri(
            CookieUsePolicy cookieUsePolicy,
            string cookieName,
            string cookieValue)
        {
            Uri uri = System.Net.Test.Common.Configuration.Http.RemoteHttp11Server.RedirectUriForDestinationUri(302, System.Net.Test.Common.Configuration.Http.RemoteEchoServer, 1);
            var handler = new WinHttpHandler();
            handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
            handler.CookieUsePolicy = cookieUsePolicy;
            if (cookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer)
            {
                handler.CookieContainer = new CookieContainer();
            }

            using (HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add(
                    "X-SetCookie",
                    string.Format("{0}={1};Path=/", cookieName, cookieValue));
                using (HttpResponseMessage httpResponse = await client.GetAsync(uri))
                {
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    _output.WriteLine(responseText);
                    Assert.True(JsonMessageContainsKeyValue(responseText, cookieName, cookieValue));
                }
            }
        }

        [OuterLoop]
        [Fact]
        [OuterLoop]
        public async Task SendAsync_SlowServerAndCancel_ThrowsTaskCanceledException()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                var cts = new CancellationTokenSource();
                Task<HttpResponseMessage> t = client.GetAsync(SlowServer, cts.Token);

                await Task.Delay(500);
                cts.Cancel();

                AggregateException ag = Assert.Throws<AggregateException>(() => t.Wait());
                Assert.IsType<TaskCanceledException>(ag.InnerException);
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/20675")]
        [OuterLoop]
        [Fact]
        [OuterLoop]
        public void SendAsync_SlowServerRespondsAfterDefaultReceiveTimeout_ThrowsHttpRequestException()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                Task<HttpResponseMessage> t = client.GetAsync(SlowServer);

                AggregateException ag = Assert.Throws<AggregateException>(() => t.Wait());
                Assert.IsType<HttpRequestException>(ag.InnerException);
            }
        }

        [Fact]
        public async Task SendAsync_GetUsingChunkedEncoding_ThrowsHttpRequestException()
        {
            // WinHTTP doesn't support GET requests with a request body that uses
            // chunked encoding. This test pins this behavior and verifies that the
            // error handling is working correctly.
            var server = new Uri("http://www.microsoft.com"); // No network I/O actually happens.
            var request = new HttpRequestMessage(HttpMethod.Get, server);
            request.Content = new StringContent("Request body");
            request.Headers.TransferEncodingChunked = true;

            var handler = new WinHttpHandler();
            using (HttpClient client = new HttpClient(handler))
            {
                HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
                _output.WriteLine(ex.ToString());
            }
        }

        [OuterLoop]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows10Version1607OrGreater))]
        public async Task GetAsync_SetCookieContainerMultipleCookies_CookiesSent()
        {
            var cookies = new Cookie[]
            {
                new Cookie("hello", "world"),
                new Cookie("foo", "bar"),
                new Cookie("ABC", "123")
            };

            WinHttpHandler handler = new WinHttpHandler();
            var cookieContainer = new CookieContainer();

            foreach (Cookie c in cookies)
            {
                cookieContainer.Add(Configuration.Http.Http2RemoteEchoServer, c);
            }

            handler.CookieContainer = cookieContainer;
            handler.CookieUsePolicy = CookieUsePolicy.UseSpecifiedCookieContainer;
            handler.ServerCertificateValidationCallback = (m, cert, chain, err) => true;
            string payload = "Cookie Test";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, Configuration.Http.Http2RemoteEchoServer) { Version = HttpVersion20.Value };
            request.Content = new StringContent(payload);
            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(HttpVersion20.Value, response.Version);
                string responsePayload = await response.Content.ReadAsStringAsync();
                var responseContent = Newtonsoft.Json.JsonConvert
                    .DeserializeAnonymousType(responsePayload, new { Method = "_", BodyContent = "_", Cookies = new Dictionary<string, string>() });
                Assert.Equal("POST", responseContent.Method);
                Assert.Equal(payload, responseContent.BodyContent);
                Assert.Equal(cookies.ToDictionary(c => c.Name, c => c.Value), responseContent.Cookies);

            };
        }

        public static bool JsonMessageContainsKeyValue(string message, string key, string value)
        {
            string pattern = string.Format(@"""{0}"": ""{1}""", key, value);
            return message.Contains(pattern);
        }
    }
}
