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
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    // Note:  Disposing the HttpClient object automatically disposes the handler within. So, it is not necessary
    // to separately Dispose (or have a 'using' statement) for the handler.
    public class HttpClientHandlerTest
    {
        readonly ITestOutputHelper _output;
        private const string Username = "testuser";
        private const string Password = "password";
        private const string DecompressedContentPart = "Accept-Encoding";
        private const string dataKey = "data";
        private const string mediaTypeJson = "application/json";

        private readonly NetworkCredential _credential = new NetworkCredential(Username, Password);

        public readonly static object[][] GetServers = HttpTestServers.GetServers;
        public readonly static object[][] PutServers = HttpTestServers.PutServers;
        public readonly static object[][] PostServers = HttpTestServers.PostServers;

        private static bool JsonMessageContainsKeyValue(string message, string key, string value)
        {
            // TODO: Align with the rest of tests w.r.t response parsing once the test server is finalized.
            // Currently not adding any new dependencies
            string pattern = string.Format(@"""{0}"": ""{1}""", key, value);
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
        public void Ctor_ExpectedDefaultPropertyValues()
        {
            using (var handler = new HttpClientHandler())
            {
                // Same as .NET Framework (Desktop).
                Assert.True(handler.AllowAutoRedirect);
                Assert.Equal(ClientCertificateOption.Manual, handler.ClientCertificateOptions);
                CookieContainer cookies = handler.CookieContainer;
                Assert.NotNull(cookies);
                Assert.Equal(0, cookies.Count);
                Assert.Null(handler.Credentials);
                Assert.Equal(50, handler.MaxAutomaticRedirections);
                Assert.False(handler.PreAuthenticate);
                Assert.Equal(null, handler.Proxy);
                Assert.True(handler.SupportsAutomaticDecompression);
                Assert.True(handler.SupportsProxy);
                Assert.True(handler.SupportsRedirectConfiguration);
                Assert.True(handler.UseCookies);
                Assert.False(handler.UseDefaultCredentials);
                Assert.True(handler.UseProxy);
                
                // Changes from .NET Framework (Desktop).
                Assert.Equal(DecompressionMethods.GZip | DecompressionMethods.Deflate, handler.AutomaticDecompression);
                Assert.Equal(0, handler.MaxRequestContentBufferSize);
            }
        }

        [Fact]
        public void MaxRequestContentBufferSize_Get_ReturnsZero()
        {
            using (var handler = new HttpClientHandler())
            {
                Assert.Equal(0, handler.MaxRequestContentBufferSize);
            }
        }

        [Fact]
        public void MaxRequestContentBufferSize_Set_ThrowsPlatformNotSupportedException()
        {
            using (var handler = new HttpClientHandler())
            {
                Assert.Throws<PlatformNotSupportedException>(() => handler.MaxRequestContentBufferSize = 1024);
            }
        }

        [Theory, MemberData("GetServers")]
        public async Task SendAsync_SimpleGet_Success(Uri remoteServer)
        {
            using (var client = new HttpClient())
            {
                // TODO: This is a placeholder until GitHub Issue #2383 gets resolved.
                HttpResponseMessage response = await client.GetAsync(remoteServer);
                await AssertSuccessfulGetResponse(response, remoteServer, _output);
            }
        }

        [Fact]
        public async Task SendAsync_MultipleRequestsReusingSameClient_Success()
        {
            using (var client = new HttpClient())
            {
                HttpResponseMessage response;
                for (int i = 0; i < 3; i++)
                {
                    response = await client.GetAsync(HttpTestServers.RemoteGetServer);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    response.Dispose();
                }
            }
        }
        
        [Fact]
        public async Task GetAsync_ResponseContentAfterClientAndHandlerDispose_Success()
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
        public async Task SendAsync_Cancel_CancellationTokenPropagates()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, HttpTestServers.RemoteGetServer);
                TaskCanceledException ex = await Assert.ThrowsAsync<TaskCanceledException>(() =>
                    client.SendAsync(request, cts.Token));
                Assert.True(cts.Token.IsCancellationRequested, "cts token IsCancellationRequested");
                Assert.True(ex.CancellationToken.IsCancellationRequested, "exception token IsCancellationRequested");
            }
        }

        [Fact]
        public async Task GetAsync_DefaultAutomaticDecompression_ContentDecompressed()
        {
            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(HttpTestServers.RemoteServerGzipUri);
                string responseContent = await response.Content.ReadAsStringAsync();
                Assert.True(responseContent.Contains(DecompressedContentPart));
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task GetAsync_ServerNeedsAuthAndSetCredential_StatusCodeOK()
        {
            var handler = new HttpClientHandler();
            handler.Credentials = _credential;
            using (var client = new HttpClient(handler))
            {
                Uri uri = HttpTestServers.BasicAuthUriForCreds(Username, Password);
                HttpResponseMessage response = await client.GetAsync(uri);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task GetAsync_ServerNeedsAuthAndNoCredential_StatusCodeUnauthorized()
        {
            using (var client = new HttpClient())
            {
                Uri uri = HttpTestServers.BasicAuthUriForCreds(Username, Password);
                HttpResponseMessage response = await client.GetAsync(uri);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task GetAsync_AllowAutoRedirectFalse_RedirectFromHttpToHttp_StatusCodeRedirect()
        {
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;
            using (var client = new HttpClient(handler))
            {
                Uri uri = HttpTestServers.RedirectUriForDestinationUri(HttpTestServers.RemoteGetServer);
                _output.WriteLine("Uri: {0}", uri);
                HttpResponseMessage response = await client.GetAsync(uri);
                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            }
        }

        [Fact]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpToHttp_StatusCodeOK()
        {
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (var client = new HttpClient(handler))
            {
                Uri uri = HttpTestServers.RedirectUriForDestinationUri(HttpTestServers.RemoteGetServer);
                _output.WriteLine("Uri: {0}", uri);
                HttpResponseMessage response = await client.GetAsync(uri);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpToHttps_StatusCodeOK()
        {
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (var client = new HttpClient(handler))
            {
                Uri uri = HttpTestServers.RedirectUriForDestinationUri(HttpTestServers.SecureRemoteGetServer);
                _output.WriteLine("Uri: {0}", uri);
                HttpResponseMessage response = await client.GetAsync(uri);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpsToHttp_StatusCodeRedirect()
        {
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (var client = new HttpClient(handler))
            {
                Uri uri = HttpTestServers.SecureRedirectUriForDestinationUri(HttpTestServers.RemoteGetServer);
                _output.WriteLine("Uri: {0}", uri);
                HttpResponseMessage response = await client.GetAsync(uri);
                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(6)]
        public async Task GetAsync_MaxAutomaticRedirectionsNServerHopsNPlus1_Throw(int hops)
        {
            var handler = new HttpClientHandler();
            handler.MaxAutomaticRedirections = hops;
            using (var client = new HttpClient(handler))
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(HttpTestServers.RedirectUriHops(hops + 1)));
            }
        }

        [Fact]
        public async Task GetAsync_CredentialIsNetworkCredentialUriRedirect_StatusCodeUnauthorized()
        {
            var handler = new HttpClientHandler();
            handler.Credentials = _credential;
            using (var client = new HttpClient(handler))
            {
                Uri redirectUri = HttpTestServers.RedirectUriForCreds(Username, Password);
                HttpResponseMessage unAuthResponse = await client.GetAsync(redirectUri);
                Assert.Equal(HttpStatusCode.Unauthorized, unAuthResponse.StatusCode);
            }
       }

        [Fact]
        public async Task GetAsync_CredentialIsCredentialCacheUriRedirect_StatusCodeOK()
        {
            Uri uri = HttpTestServers.BasicAuthUriForCreds(Username, Password);
            Uri redirectUri = HttpTestServers.RedirectUriForCreds(Username, Password);
            var credentialCache = new CredentialCache();
            credentialCache.Add(uri, "Basic", _credential);

            var handler = new HttpClientHandler();
            handler.Credentials = credentialCache;
            using (var client = new HttpClient(handler))
            {
                HttpResponseMessage response = await client.GetAsync(redirectUri);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Theory]
        [InlineData("cookiename", "cookievalue")]
        public async Task GetAsync_SetCookieContainer_CookieSent(string name, string value)
        {
            var handler = new HttpClientHandler();
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

        [Theory]
        [InlineData("X-Cust-Header","x-value")]
        public async Task GetAsync_RequestHeadersAddCustomHeaders_HeaderAndValueSent(string name, string value)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add(name, value);
                HttpResponseMessage httpResponse = await client.GetAsync(HttpTestServers.RemoteServerHeadersUri);
                httpResponse.EnsureSuccessStatusCode();
                string responseText = await httpResponse.Content.ReadAsStringAsync();
                Assert.True(JsonMessageContainsKeyValue(responseText, name, value));
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
        public async Task SendAsync_HttpRequestMsgResponseHeadersRead_StatusCodeOK()
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
        public async Task PostAsync_CallMethod_StringContent(Uri remoteServer)
        {
            using (var client = new HttpClient())
            {
                string data = "Test String";
                var stringContent = new StringContent(data, Encoding.UTF8, mediaTypeJson);
                HttpResponseMessage response =
                    await client.PostAsync(remoteServer, stringContent);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string responseContent = await response.Content.ReadAsStringAsync();
                Assert.True(JsonMessageContainsKeyValue(responseContent, dataKey, data));
                _output.WriteLine(responseContent);
            }
        }

        [Theory, MemberData("PostServers")]
        public async Task PostAsync_CallMethod_FormUrlEncodedContent(Uri remoteServer)
        {
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string> { { "thing1", "hello" }, { "thing2", "world" } };
                var content = new FormUrlEncodedContent(values);
                HttpResponseMessage response = await client.PostAsync(remoteServer, content);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string responseContent = await response.Content.ReadAsStringAsync();
                Assert.True(JsonMessageContainsKeyValue(responseContent, "thing1", "hello"));
                Assert.True(JsonMessageContainsKeyValue(responseContent, "thing2", "world"));
                _output.WriteLine(responseContent);
            }
        }

       [Theory, MemberData("PostServers")]
       public async Task PostAsync_CallMethod_UploadFile(Uri remoteServer)
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

                using (var client = new HttpClient())
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
                    HttpResponseMessage response = await client.PostAsync(remoteServer, form);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Assert.True(JsonMessageContainsKeyValue(responseContent, fileTitle, fileContent));
                    _output.WriteLine(responseContent);
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
        public async Task PostAsync_CallMethodTwice_StringContent(Uri remoteServer)
        {
            using (var client = new HttpClient())
            {
                string data = "Test String";
                var stringContent = new StringContent(data, Encoding.UTF8, mediaTypeJson);
                HttpResponseMessage response =
                    await client.PostAsync(remoteServer, stringContent);
                string responseContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseContent);

                // Repeat call.
                stringContent = new StringContent(data, Encoding.UTF8, mediaTypeJson);
                response = await client.PostAsync(remoteServer, stringContent);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                responseContent = await response.Content.ReadAsStringAsync();
                Assert.True(JsonMessageContainsKeyValue(responseContent, dataKey, data));
                _output.WriteLine(responseContent);
            }
        }

        [Theory, MemberData("PostServers")]
        public async Task PostAsync_CallMethod_UnicodeStringContent(Uri remoteServer)
        {
            using (var client = new HttpClient())
            {
                var stringContent = new StringContent(
                    "\ub4f1\uffc7\u4e82\u67ab4\uc6d4\ud1a0\uc694\uc77c\uffda3\u3155\uc218\uffdb",
                    Encoding.UTF8,
                    mediaTypeJson);
                HttpResponseMessage response = await client.PostAsync(remoteServer, stringContent);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string responseContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseContent);
            }
        }

        [Theory, MemberData("PostServersStreamsAndExpectedData")]
        public async Task PostAsync_CallMethod_StreamContent(Uri remoteServer, Stream requestContentStream, byte[] expectedData)
        {
            using (var client = new HttpClient())
            {
                HttpContent content = new StreamContent(requestContentStream);
                HttpResponseMessage response = await client.PostAsync(remoteServer, content);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string responseContent = await response.Content.ReadAsStringAsync();
                Assert.Contains(Convert.ToBase64String(expectedData), responseContent);
            }
        }

        public static IEnumerable<object[]> PostServersStreamsAndExpectedData
        {
            get
            {
                foreach (object[] postServerArr in PostServers)
                {
                    Uri postServer = (Uri)postServerArr[0];

                    byte[] data = new byte[1234];
                    new Random(42).NextBytes(data);

                    // A MemoryStream
                    {
                        var memStream = new MemoryStream(data, writable: false);
                        yield return new object[] { postServer, memStream, data };
                    }

                    // A stream that provides the data synchronously and has a known length
                    {
                        var wrappedMemStream = new MemoryStream(data, writable: false);
                        var syncKnownLengthStream = new DelegateStream(
                            canReadFunc: () => wrappedMemStream.CanRead,
                            canSeekFunc: () => wrappedMemStream.CanSeek,
                            lengthFunc: () => wrappedMemStream.Length,
                            positionGetFunc: () => wrappedMemStream.Position,
                            readAsyncFunc: (buffer, offset, count, token) => wrappedMemStream.ReadAsync(buffer, offset, count, token));
                        yield return new object[] { postServer, syncKnownLengthStream, data };
                    }

                    // A stream that provides the data synchronously and has an unknown length
                    {
                        int syncUnknownLengthStreamOffset = 0;
                        var syncUnknownLengthStream = new DelegateStream(
                            canReadFunc: () => true,
                            canSeekFunc: () => false,
                            readAsyncFunc: (buffer, offset, count, token) =>
                            {
                                int bytesRemaining = data.Length - syncUnknownLengthStreamOffset;
                                int bytesToCopy = Math.Min(bytesRemaining, count);
                                Array.Copy(data, syncUnknownLengthStreamOffset, buffer, offset, bytesToCopy);
                                syncUnknownLengthStreamOffset += bytesToCopy;
                                return Task.FromResult(bytesToCopy);
                            });
                        yield return new object[] { postServer, syncUnknownLengthStream, data };
                    }

                    // A stream that provides the data asynchronously
                    {
                        int asyncStreamOffset = 0, maxDataPerRead = 100;
                        var asyncStream = new DelegateStream(
                            canReadFunc: () => true,
                            canSeekFunc: () => false,
                            readAsyncFunc: async (buffer, offset, count, token) =>
                            {
                                await Task.Delay(1).ConfigureAwait(false);
                                int bytesRemaining = data.Length - asyncStreamOffset;
                                int bytesToCopy = Math.Min(bytesRemaining, Math.Min(maxDataPerRead, count));
                                Array.Copy(data, asyncStreamOffset, buffer, offset, bytesToCopy);
                                asyncStreamOffset += bytesToCopy;
                                return bytesToCopy;
                            });
                        yield return new object[] { postServer, asyncStream, data };
                    }
                }
            }
        }

        [Theory, MemberData("PostServers")]
        public async Task PostAsync_CallMethod_NullContent(Uri remoteServer)
        {
            using (var client = new HttpClient())
            {
                HttpContent obj = new StringContent(String.Empty);
                HttpResponseMessage response = await client.PostAsync(remoteServer, null);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string responseContent = await response.Content.ReadAsStringAsync();
                Assert.True(JsonMessageContainsKeyValue(responseContent, dataKey, String.Empty));
                _output.WriteLine(responseContent);
            }
        }

        [Fact]
        public async Task PostAsync_IncorrectUri_MethodNotAllowed()
        {
            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client.PostAsync(HttpTestServers.RemoteGetServer, null);
                Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            }
        }

        [Fact]
        [ActiveIssue(3565, PlatformID.AnyUnix)]
        public async Task PostAsync_Post_ChannelBindingHasExpectedValue()
        {
            using (var client = new HttpClient())
            {
                string expectedContent = "Test contest";
                var content = new ChannelBindingAwareContent(expectedContent);
                HttpResponseMessage response = await client.PostAsync(HttpTestServers.SecureRemotePostServer, content);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                ChannelBinding channelBinding = content.ChannelBinding;
                Assert.NotNull(channelBinding);
                _output.WriteLine("Channel Binding: {0}", channelBinding);
            }
        }

        #endregion

        #region Put Method Tests

        [Theory, MemberData("PutServers")]
        public async Task PutAsync_CallMethod_StringContent(Uri remoteServer)
        {
            using (var client = new HttpClient())
            {
                var stringContent = new StringContent("{ \"firstName\": \"John\" }", Encoding.UTF32,
                    mediaTypeJson);
                HttpResponseMessage response = await client.PutAsync(remoteServer, stringContent);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string responseContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseContent);
            }
        }

        [Theory, MemberData("PutServers")]
        public async Task PutAsync_CallMethod_NullContent(Uri remoteServer)
        {
            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client.PutAsync(remoteServer, null);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string responseContent = await response.Content.ReadAsStringAsync();
                Assert.True(JsonMessageContainsKeyValue(responseContent, dataKey, String.Empty));
                _output.WriteLine(responseContent);
            }
        }

        [Fact]
        public async Task PutAsync_IncorrectUri_MethodNotAllowed()
        {
            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client.PutAsync(HttpTestServers.RemoteGetServer, null);
                Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            }
        }

        #endregion
    }
}
