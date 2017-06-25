// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

// Can't use "WinHttpHandler.Functional.Tests" in namespace as it won't compile.
// WinHttpHandler is a class and not a namespace and can't be part of namespace paths.
namespace System.Net.Http.WinHttpHandlerFunctional.Tests
{
    // Note:  Disposing the HttpClient object automatically disposes the handler within. So, it is not necessary
    // to separately Dispose (or have a 'using' statement) for the handler.
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "WinHttpHandler not supported on UAP")]
    public class WinHttpHandlerTest
    {
        // TODO: This is a placeholder until GitHub Issue #2383 gets resolved.
        private const string SlowServer = "http://httpbin.org/drip?numbytes=1&duration=1&delay=40&code=200";

        private readonly ITestOutputHelper _output;

        public WinHttpHandlerTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SendAsync_SimpleGet_Success()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                // TODO: This is a placeholder until GitHub Issue #2383 gets resolved.
                var response = client.GetAsync(System.Net.Test.Common.Configuration.Http.RemoteEchoServer).Result;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                _output.WriteLine(responseContent);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(CookieUsePolicy.UseInternalCookieStoreOnly, "cookieName1", "cookieValue1")]
        [InlineData(CookieUsePolicy.UseSpecifiedCookieContainer, "cookieName2", "cookieValue2")]
        public async Task GetAsync_RedirectResponseHasCookie_CookieSentToFinalUri(
            CookieUsePolicy cookieUsePolicy,
            string cookieName,
            string cookieValue)
        {
            Uri uri = System.Net.Test.Common.Configuration.Http.RedirectUriForDestinationUri(false, 302, System.Net.Test.Common.Configuration.Http.RemoteEchoServer, 1);
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

        [OuterLoop] // TODO: Issue #11345
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
        
        [ActiveIssue(17234)]
        [OuterLoop] // TODO: Issue #11345
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

        public static bool JsonMessageContainsKeyValue(string message, string key, string value)
        {
            // TODO: Merge with System.Net.Http TestHelper class as part of GitHub Issue #4989.
            string pattern = string.Format(@"""{0}"": ""{1}""", key, value);
            return message.Contains(pattern);
        }      
    }
}
