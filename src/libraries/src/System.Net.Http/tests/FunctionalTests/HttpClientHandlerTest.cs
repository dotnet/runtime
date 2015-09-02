// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Tests;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Tests
{
    public class HttpClientHandlerTest
    {
        readonly ITestOutputHelper _output;
        private const string Username = "testuser";
        private const string Password = "password";
        private const string DecompressedContentPart = "Accept-Encoding";
        private readonly NetworkCredential _credential = new NetworkCredential(Username, Password);

        private static bool JsonMessageContainsKeyValue(string message, string key, string value)
        {
            // TODO: Align with the rest of tests w.r.t response parsing once the test server is finalized.
            // Currently not adding any new dependencies
            var pattern = string.Format(@"""{0}"": ""{1}""", key, value);
            return message.Contains(pattern);
        }

        private static async Task AssertSuccessfulGetResponse(HttpResponseMessage response, Uri uri, ITestOutputHelper output)
        {
            Assert.Equal<HttpStatusCode>(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal<string>("OK", response.ReasonPhrase);
            string responseContent = await response.Content.ReadAsStringAsync();
            Assert.True(JsonMessageContainsKeyValue(responseContent, "url", uri.AbsoluteUri));
            output.WriteLine(responseContent);
        }

        public HttpClientHandlerTest(ITestOutputHelper output)
        {
            _output = output;
        }
        
        [Fact]
        public async void SendAsync_SimpleGet_Success()
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    // TODO: This is a placeholder until GitHub Issue #2383 gets resolved.
                    HttpResponseMessage response = await client.GetAsync(HttpTestServers.RemoteGetServer);
                    await AssertSuccessfulGetResponse(response, HttpTestServers.RemoteGetServer, _output);
                }
            }
        }

        [Fact]
        public async void SendAsync_SimpleHttpsGet_Success()
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    HttpResponseMessage response = await client.GetAsync(HttpTestServers.SecureRemoteGetServer);
                    await AssertSuccessfulGetResponse(response, HttpTestServers.SecureRemoteGetServer, _output);
                }
            }
        }

        [Fact]
        public async void GetAsync_ResponseContentAfterClientAndHandlerDispose_Success()
        {
            HttpResponseMessage response = null;
            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                response = await client.GetAsync(HttpTestServers.SecureRemoteGetServer);
            }
            Assert.NotNull(response);
            await AssertSuccessfulGetResponse(response, HttpTestServers.SecureRemoteGetServer, _output);
        }

        [Fact]
        public async Task GetAsync_Cancel_CancellationTokenPropagates()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            try
            {
                var client = new HttpClient();
                Task <HttpResponseMessage> task = client.GetAsync(HttpTestServers.RemoteGetServer, cts.Token);
                await task;

                Assert.True(false, "Expected TaskCanceledException to be thrown.");
            }
            catch (TaskCanceledException ex)
            {
                Assert.True(cts.Token.IsCancellationRequested,
                    "Expected cancellation requested on original token.");

                Assert.True(ex.CancellationToken.IsCancellationRequested,
                    "Expected cancellation requested on token attached to exception.");
            }
        }

        [Fact]
        public async void GetAsync_DefaultAutomaticDecompression_ContentDecompressed()
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    HttpResponseMessage response = await client.GetAsync(HttpTestServers.RemoteServerGzipUri);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Assert.True(responseContent.Contains(DecompressedContentPart));
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async void GetAsync_SetCredential_StatusCodeOK()
        {
            Uri uri = HttpTestServers.BasicAuthUriForCreds(Username, Password);
            using (var handler = new HttpClientHandler())
            {
                handler.Credentials = _credential;
                using (var client = new HttpClient(handler))
                {
                    HttpResponseMessage response = await client.GetAsync(uri);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async void GetAsync_AllowRedirectFalse_StatusCodeRedirect()
        {
            using (var handler = new HttpClientHandler())
            {
                handler.AllowAutoRedirect = false;
                using (var client = new HttpClient(handler))
                {
                    HttpResponseMessage response = await client.GetAsync(HttpTestServers.RemoteServerRedirectUri);
                    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
                }
            }
        }

        [Fact]
        public async void GetAsync_AllowRedirectDefault_StatusCodeOK()
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    HttpResponseMessage response = await client.GetAsync(HttpTestServers.RemoteServerRedirectUri);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(6)]
        public async void GetAsync_MaxAutomaticRedirectionsNServerHopsNPlus1_Throw(int hops)
        {
            using (var handler = new HttpClientHandler())
            {
                handler.MaxAutomaticRedirections = hops;
                var client = new HttpClient(handler);
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(HttpTestServers.RedirectUriHops(hops + 1)));
            }
        }

        [Fact]
        public async void GetAsync_CredentialIsNetworkCredentialUriRedirect_StatusCodeUnauthorized()
        {
            Uri redirectUri = HttpTestServers.RedirectUriForCreds(Username, Password);
            using (var handler = new HttpClientHandler())
            {
                handler.Credentials = _credential;
                var client = new HttpClient(handler);
                HttpResponseMessage unAuthResponse = await client.GetAsync(redirectUri);
                Assert.Equal(HttpStatusCode.Unauthorized, unAuthResponse.StatusCode);
            }
       }

        [Fact]
        public void GetAsync_CredentialIsCredentialCacheUriRedirect_HttpStatusCodeOK()
        {
            Uri uri = HttpTestServers.BasicAuthUriForCreds(Username, Password);
            Uri redirectUri = HttpTestServers.RedirectUriForCreds(Username, Password);
            var credentialCache = new CredentialCache();
            credentialCache.Add(uri, "Basic", _credential);

            using (var handler = new HttpClientHandler())
            {
                handler.Credentials = credentialCache;
                var client = new HttpClient(handler);
                var response = client.GetAsync(uri).Result;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Theory]
        [InlineData("cookiename", "cookievalue")]
        public async void GetAsync_SetCookieContainer_CookieSent(string name, string value)
        {
            using (var handler = new HttpClientHandler())
            {
                var cookieContainer = new CookieContainer();
                cookieContainer.Add(HttpTestServers.RemoteServerCookieUri, new Cookie(name, value));
                handler.CookieContainer = cookieContainer;
                using (var client = new HttpClient(handler))
                {
                    HttpResponseMessage httpResponse = await client.GetAsync(HttpTestServers.RemoteServerCookieUri);
                    Assert.Equal(httpResponse.StatusCode, HttpStatusCode.OK);
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    Assert.True(JsonMessageContainsKeyValue(responseText, name, value));
                }
            }
        }

        [Theory]
        [InlineData("X-Cust-Header","x-value")]
        public async void GetAsync_RequestHeadersAddCustomHeaders_HeaderAndValueSent(string name, string value)
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add(name, value);
                    HttpResponseMessage httpResponse = await client.GetAsync(HttpTestServers.RemoteServerHeadersUri);
                    httpResponse.EnsureSuccessStatusCode();
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    Assert.True(JsonMessageContainsKeyValue(responseText, name, value));
                }
            }
        }

        [Fact]
        public async void SendAsync_HttpRequestMsgResponseHeadersRead_StatusCodeOK()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, HttpTestServers.SecureRemoteGetServer);
            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                await AssertSuccessfulGetResponse(response, HttpTestServers.SecureRemoteGetServer, _output);
            }
        }
    }
}
