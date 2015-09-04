// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Tests;
using System.Text;
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
        private const string dataKey = "data";
        private const string mediaTypeJson = "application/json";

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

        public static object[][] PutServers
        {
            get
            {
                return HttpTestServers.PutServers;
            }
        }

        public static object[][] PostServers
        {
            get
            {
                return HttpTestServers.PostServers;
            }
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
        public async Task GetAsync_ResponseHeadersRead_ReadFromEachIterativelyDoesntDeadlock()
        {
            using (var client = new HttpClient())
            {
                const int NumGets = 5;
                Task<HttpResponseMessage>[] responseTasks = (from _ in Enumerable.Range(0, NumGets)
                                                             select client.GetAsync(HttpTestServers.RemoteGetServer, HttpCompletionOption.ResponseHeadersRead)).ToArray();
                for (int i = responseTasks.Length - 1; i >= 0; i--) // read backwards to increase liklihood that we wait on a different task than has data available
                {
                    using (HttpResponseMessage response = await responseTasks[i])
                    {
                        await AssertSuccessfulGetResponse(response, HttpTestServers.RemoteGetServer, _output);
                    }
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

        #region Post Methods Tests

        [Theory, MemberData("PostServers")]
        public async Task PostAsync_CallMethod_StringContent(Uri RemoteServer)
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    var data = "Test String";
                    var stringContent = new StringContent(data, UnicodeEncoding.UTF8, mediaTypeJson);
                    HttpResponseMessage response =
                        await client.PostAsync(RemoteServer, stringContent);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Assert.True(JsonMessageContainsKeyValue(responseContent, dataKey, data));
                    _output.WriteLine(responseContent);
                }
            }
        }

        [Theory, MemberData("PostServers")]
        public async Task PostAsync_CallMethod_FormUrlEncodedContent(Uri RemoteServer)
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    var values = new Dictionary<string, string> { { "thing1", "hello" }, { "thing2", "world" } };
                    var content = new FormUrlEncodedContent(values);
                    HttpResponseMessage response = await client.PostAsync(RemoteServer, content);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Assert.True(JsonMessageContainsKeyValue(responseContent, "thing1", "hello"));
                    Assert.True(JsonMessageContainsKeyValue(responseContent, "thing2", "world"));
                    _output.WriteLine(responseContent);
                }
            }
        }

       [Theory, MemberData("PostServers")]
       public async Task PostAsync_CallMethod_UploadFile(Uri RemoteServer)
        {
            string fileName = Path.GetTempFileName();
            string fileTitle = "fileToUpload";
            string fileContent = "This file to test POST Scenario";

            try
            {
                // open file to edit
                using (FileStream fs = File.Open(fileName, FileMode.OpenOrCreate))
                {
                    // Add some text to file
                    byte[] author = new UTF8Encoding(true).GetBytes(fileContent);
                    fs.Write(author, 0, author.Length);
                }

                using (var handler = new HttpClientHandler())
                {
                    using (var client = new HttpClient(handler))
                    {
                        var form = new MultipartFormDataContent();
                        var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                        var content = new StreamContent(stream);
                        content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                        {
                            Name = fileTitle,
                            FileName = fileName
                        };
                        form.Add(content);
                        HttpResponseMessage response = await client.PostAsync(RemoteServer, form);
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        var responseContent = await response.Content.ReadAsStringAsync();
                        Assert.True(JsonMessageContainsKeyValue(responseContent, fileTitle, fileContent));
                        _output.WriteLine(responseContent);
                    }
                }
            }
            finally
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }

        [Theory, MemberData("PostServers")]
        public async Task PostAsync_CallMethodTwice_StringContent(Uri RemoteServer)
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    var data = "Test String";
                    var stringContent = new StringContent(data, UnicodeEncoding.UTF8, mediaTypeJson);
                    HttpResponseMessage response =
                        await client.PostAsync(RemoteServer, stringContent);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);

                    //Repeat call
                    stringContent = new StringContent(data, UnicodeEncoding.UTF8, mediaTypeJson);
                    response = await client.PostAsync(RemoteServer, stringContent);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    responseContent = await response.Content.ReadAsStringAsync();
                    Assert.True(JsonMessageContainsKeyValue(responseContent, dataKey, data));
                    _output.WriteLine(responseContent);
                }
            }
        }

        [Theory, MemberData("PostServers")]
        public async Task PostAsync_CallMethod_UnicodeStringContent(Uri RemoteServer)
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {

                    var stringContent = new StringContent("\ub4f1\uffc7\u4e82\u67ab4\uc6d4\ud1a0\uc694\uc77c\uffda3\u3155\uc218\uffdb", UnicodeEncoding.UTF8,
                        mediaTypeJson);
                    HttpResponseMessage response =
                        await client.PostAsync(RemoteServer, stringContent);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);
                }
            }
        }

        [Theory, MemberData("PostServers")]
        public async Task PostAsync_CallMethod_NullContent(Uri RemoteServer)
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    HttpContent obj = new StringContent(String.Empty);
                    HttpResponseMessage response = await client.PostAsync(RemoteServer, null);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Assert.True(JsonMessageContainsKeyValue(responseContent, dataKey, String.Empty));
                    _output.WriteLine(responseContent);
                }
            }
        }

        [Fact]
        public async Task PostAsync_IncorrectUri_MethodNotAllowed()
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    HttpResponseMessage response = await client.PostAsync(HttpTestServers.RemoteGetServer, null);
                    Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
                }
            }
        }

        #endregion

        #region Put Method Tests

        [Theory, MemberData("PutServers")]
        public async Task PutAsync_CallMethod_StringContent(Uri RemoteServer)
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    var stringContent = new StringContent("{ \"firstName\": \"John\" }", UnicodeEncoding.UTF32,
                        mediaTypeJson);
                    HttpResponseMessage response = await client.PutAsync(RemoteServer, stringContent);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);
                }
            }
        }

        [Theory, MemberData("PutServers")]
        public async Task PutAsync_CallMethod_NullContent(Uri RemoteServer)
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    HttpResponseMessage response = await client.PutAsync(RemoteServer, null);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Assert.True(JsonMessageContainsKeyValue(responseContent, dataKey, String.Empty));
                    _output.WriteLine(responseContent);
                }
            }
        }

        [Fact]
        public async Task PutAsync_IncorrectUri_MethodNotAllowed()
        {
            using (var handler = new HttpClientHandler())
            {
                using (var client = new HttpClient(handler))
                {
                    HttpResponseMessage response = await client.PutAsync(HttpTestServers.RemoteGetServer, null);
                    Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
                }
            }
        }

        #endregion

    }
}
