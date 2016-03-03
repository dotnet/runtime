// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Test.Common;
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
        private const string ExpectedContent = "Test contest";
        private const string Username = "testuser";
        private const string Password = "password";

        private readonly NetworkCredential _credential = new NetworkCredential(Username, Password);

        public readonly static object[][] EchoServers = HttpTestServers.EchoServers;
        public readonly static object[][] VerifyUploadServers = HttpTestServers.VerifyUploadServers;
        public readonly static object[][] CompressedServers = HttpTestServers.CompressedServers;
        public readonly static object[][] HeaderValueAndUris = {
            new object[] { "X-CustomHeader", "x-value", HttpTestServers.RemoteEchoServer },
            new object[] { "X-Cust-Header-NoValue", "" , HttpTestServers.RemoteEchoServer },
            new object[] { "X-CustomHeader", "x-value", HttpTestServers.RedirectUriForDestinationUri(
                secure:false,
                destinationUri:HttpTestServers.RemoteEchoServer,
                hops:1) },
            new object[] { "X-Cust-Header-NoValue", "" , HttpTestServers.RedirectUriForDestinationUri(
                secure:false,
                destinationUri:HttpTestServers.RemoteEchoServer,
                hops:1) },
        };
        public readonly static object[][] Http2Servers = HttpTestServers.Http2Servers;


        // Standard HTTP methods defined in RFC7231: http://tools.ietf.org/html/rfc7231#section-4.3
        //     "GET", "HEAD", "POST", "PUT", "DELETE", "OPTIONS", "TRACE"
        public readonly static IEnumerable<object[]> HttpMethods =
            GetMethods("GET", "HEAD", "POST", "PUT", "DELETE", "OPTIONS", "TRACE", "CUSTOM1");
        public readonly static IEnumerable<object[]> HttpMethodsThatAllowContent =
            GetMethods("GET", "POST", "PUT", "DELETE", "CUSTOM1");

        private static IEnumerable<object[]> GetMethods(params string[] methods)
        {
            foreach (string method in methods)
            {
                foreach (bool secure in new[] { true, false })
                {
                    yield return new object[] { method, secure };
                }
            }
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UseDefaultCredentials_SetToFalseAndServerNeedsAuth_StatusCodeUnauthorized(bool useProxy)
        {
            var handler = new HttpClientHandler();
            handler.UseProxy = useProxy;
            handler.UseDefaultCredentials = false;
            using (var client = new HttpClient(handler))
            {
                Uri uri = HttpTestServers.NegotiateAuthUriForDefaultCreds(secure:false);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task SendAsync_SimpleGet_Success(Uri remoteServer)
        {
            using (var client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(remoteServer))
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);                    
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        null);                    
                }
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
                    response = await client.GetAsync(HttpTestServers.RemoteEchoServer);
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
                response = await client.GetAsync(HttpTestServers.SecureRemoteEchoServer);
            }
            Assert.NotNull(response);
            string responseContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine(responseContent);                    
            TestHelper.VerifyResponseBody(
                responseContent,
                response.Content.Headers.ContentMD5,
                false,
                null);                    
        }

        [Fact]
        public async Task SendAsync_Cancel_CancellationTokenPropagates()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, HttpTestServers.RemoteEchoServer);
                TaskCanceledException ex = await Assert.ThrowsAsync<TaskCanceledException>(() =>
                    client.SendAsync(request, cts.Token));
                Assert.True(cts.Token.IsCancellationRequested, "cts token IsCancellationRequested");
                Assert.True(ex.CancellationToken.IsCancellationRequested, "exception token IsCancellationRequested");
            }
        }

        [Theory, MemberData(nameof(CompressedServers))]
        public async Task GetAsync_DefaultAutomaticDecompression_ContentDecompressed(Uri server)
        {
            using (var client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(server))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        null);
                }
            }
        }

        [Fact]
        public async Task GetAsync_ServerNeedsAuthAndSetCredential_StatusCodeOK()
        {
            var handler = new HttpClientHandler();
            handler.Credentials = _credential;
            using (var client = new HttpClient(handler))
            {
                Uri uri = HttpTestServers.BasicAuthUriForCreds(secure:false, userName:Username, password:Password);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetAsync_ServerNeedsAuthAndNoCredential_StatusCodeUnauthorized()
        {
            using (var client = new HttpClient())
            {
                Uri uri = HttpTestServers.BasicAuthUriForCreds(secure:false, userName:Username, password:Password);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetAsync_AllowAutoRedirectFalse_RedirectFromHttpToHttp_StatusCodeRedirect()
        {
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;
            using (var client = new HttpClient(handler))
            {
                Uri uri = HttpTestServers.RedirectUriForDestinationUri(
                    secure:false,
                    destinationUri:HttpTestServers.RemoteEchoServer,
                    hops:1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpToHttp_StatusCodeOK()
        {
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (var client = new HttpClient(handler))
            {
                Uri uri = HttpTestServers.RedirectUriForDestinationUri(
                    secure:false,
                    destinationUri:HttpTestServers.RemoteEchoServer,
                    hops:1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpToHttps_StatusCodeOK()
        {
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (var client = new HttpClient(handler))
            {
                Uri uri = HttpTestServers.RedirectUriForDestinationUri(
                    secure:false,
                    destinationUri:HttpTestServers.SecureRemoteEchoServer,
                    hops:1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpsToHttp_StatusCodeRedirect()
        {
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (var client = new HttpClient(handler))
            {
                Uri uri = HttpTestServers.RedirectUriForDestinationUri(
                    secure:true,
                    destinationUri:HttpTestServers.RemoteEchoServer,
                    hops:1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectToUriWithParams_RequestMsgUriSet()
        {
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            Uri targetUri = HttpTestServers.BasicAuthUriForCreds(secure:false, userName:Username, password:Password);
            using (var client = new HttpClient(handler))
            {
                Uri uri = HttpTestServers.RedirectUriForDestinationUri(secure:false, destinationUri:targetUri, hops:1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(targetUri, response.RequestMessage.RequestUri);
                }
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
                await Assert.ThrowsAsync<HttpRequestException>(() => 
                    client.GetAsync(HttpTestServers.RedirectUriForDestinationUri(
                        secure:false,
                        destinationUri:HttpTestServers.RemoteEchoServer,
                        hops:(hops + 1))));
            }
        }

        [Fact]
        public async Task GetAsync_CredentialIsNetworkCredentialUriRedirect_StatusCodeUnauthorized()
        {
            var handler = new HttpClientHandler();
            handler.Credentials = _credential;
            using (var client = new HttpClient(handler))
            {
                Uri redirectUri = HttpTestServers.RedirectUriForCreds(secure:false, userName:Username, password:Password);
                using (HttpResponseMessage unAuthResponse = await client.GetAsync(redirectUri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, unAuthResponse.StatusCode);
                }
            }
       }

        [Fact]
        public async Task GetAsync_CredentialIsCredentialCacheUriRedirect_StatusCodeOK()
        {
            Uri uri = HttpTestServers.BasicAuthUriForCreds(secure:false, userName:Username, password:Password);
            Uri redirectUri = HttpTestServers.RedirectUriForCreds(secure:false, userName:Username, password:Password);
            _output.WriteLine(uri.AbsoluteUri);
            _output.WriteLine(redirectUri.AbsoluteUri);
            var credentialCache = new CredentialCache();
            credentialCache.Add(uri, "Basic", _credential);

            var handler = new HttpClientHandler();
            handler.Credentials = credentialCache;
            using (var client = new HttpClient(handler))
            {
                using (HttpResponseMessage response = await client.GetAsync(redirectUri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetAsync_DefaultCoookieContainer_NoCookieSent()
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage httpResponse = await client.GetAsync(HttpTestServers.RemoteEchoServer))
                {
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    _output.WriteLine(responseText);
                    Assert.False(TestHelper.JsonMessageContainsKey(responseText, "Cookie"));
                }
            }
        }

        [Theory]
        [InlineData("cookieName1", "cookieValue1")]
        public async Task GetAsync_SetCookieContainer_CookieSent(string cookieName, string cookieValue)
        {
            var handler = new HttpClientHandler();
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(HttpTestServers.RemoteEchoServer, new Cookie(cookieName, cookieValue));
            handler.CookieContainer = cookieContainer;
            using (var client = new HttpClient(handler))
            {
                using (HttpResponseMessage httpResponse = await client.GetAsync(HttpTestServers.RemoteEchoServer))
                {
                    Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    _output.WriteLine(responseText);
                    Assert.True(TestHelper.JsonMessageContainsKeyValue(responseText, cookieName, cookieValue));
                }
            }
        }

        [Theory]
        [InlineData("cookieName1", "cookieValue1")]
        public async Task GetAsync_RedirectResponseHasCookie_CookieSentToFinalUri(string cookieName, string cookieValue)
        {
            Uri uri = HttpTestServers.RedirectUriForDestinationUri(
                secure:false,
                destinationUri:HttpTestServers.RemoteEchoServer,
                hops:1);
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add(
                    "X-SetCookie",
                    string.Format("{0}={1};Path=/", cookieName, cookieValue));
                using (HttpResponseMessage httpResponse = await client.GetAsync(uri))
                {
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    _output.WriteLine(responseText);
                    Assert.True(TestHelper.JsonMessageContainsKeyValue(responseText, cookieName, cookieValue));
                }
            }            
        }

        [Theory, MemberData(nameof(HeaderValueAndUris))]
        public async Task GetAsync_RequestHeadersAddCustomHeaders_HeaderAndValueSent(string name, string value, Uri uri)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add(name, value);
                using (HttpResponseMessage httpResponse = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    Assert.True(TestHelper.JsonMessageContainsKeyValue(responseText, name, value));
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
                                                             select client.GetAsync(HttpTestServers.RemoteEchoServer, HttpCompletionOption.ResponseHeadersRead)).ToArray();
                for (int i = responseTasks.Length - 1; i >= 0; i--) // read backwards to increase liklihood that we wait on a different task than has data available
                {
                    using (HttpResponseMessage response = await responseTasks[i])
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        _output.WriteLine(responseContent);                    
                        TestHelper.VerifyResponseBody(
                            responseContent,
                            response.Content.Headers.ContentMD5,
                            false,
                            null);                    
                   }
                }
            }
        }

        [Fact]
        public async Task SendAsync_HttpRequestMsgResponseHeadersRead_StatusCodeOK()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, HttpTestServers.SecureRemoteEchoServer);
            using (var client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);                    
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        null);                    
                }
            }
        }

        [ActiveIssue(4259, PlatformID.AnyUnix)]
        [OuterLoop]
        [Fact]
        public async Task SendAsync_ReadFromSlowStreamingServer_PartialDataReturned()
        {
            // TODO: This is a placeholder until GitHub Issue #2383 gets resolved.
            const string SlowStreamingServer = "http://httpbin.org/drip?numbytes=8192&duration=15&delay=1&code=200";
            
            int bytesRead;
            byte[] buffer = new byte[8192];
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, SlowStreamingServer);
                using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    Stream stream = await response.Content.ReadAsStreamAsync();
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                }
                _output.WriteLine("Bytes read from stream: {0}", bytesRead);
                Assert.True(bytesRead < buffer.Length, "bytesRead should be less than buffer.Length");
            }            
        }

        #region Post Methods Tests

        [Theory, MemberData(nameof(VerifyUploadServers))]
        public async Task PostAsync_CallMethodTwice_StringContent(Uri remoteServer)
        {
            using (var client = new HttpClient())
            {
                string data = "Test String";
                var content = new StringContent(data, Encoding.UTF8);
                content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(data);
                HttpResponseMessage response;
                using (response = await client.PostAsync(remoteServer, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                // Repeat call.
                content = new StringContent(data, Encoding.UTF8);
                content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(data);
                using (response = await client.PostAsync(remoteServer, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Theory, MemberData(nameof(VerifyUploadServers))]
        public async Task PostAsync_CallMethod_UnicodeStringContent(Uri remoteServer)
        {
            using (var client = new HttpClient())
            {
                string data = "\ub4f1\uffc7\u4e82\u67ab4\uc6d4\ud1a0\uc694\uc77c\uffda3\u3155\uc218\uffdb";
                var content = new StringContent(data, Encoding.UTF8);
                content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(data);
                
                using (HttpResponseMessage response = await client.PostAsync(remoteServer, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Theory, MemberData(nameof(VerifyUploadServersStreamsAndExpectedData))]
        public async Task PostAsync_CallMethod_StreamContent(Uri remoteServer, Stream requestContentStream, byte[] expectedData)
        {
            using (var client = new HttpClient())
            {
                HttpContent content = new StreamContent(requestContentStream);
                content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(expectedData);

                using (HttpResponseMessage response = await client.PostAsync(remoteServer, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        public static IEnumerable<object[]> VerifyUploadServersStreamsAndExpectedData
        {
            get
            {
                foreach (object[] serverArr in VerifyUploadServers)
                {
                    Uri server = (Uri)serverArr[0];

                    byte[] data = new byte[1234];
                    new Random(42).NextBytes(data);

                    // A MemoryStream
                    {
                        var memStream = new MemoryStream(data, writable: false);
                        yield return new object[] { server, memStream, data };
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
                        yield return new object[] { server, syncKnownLengthStream, data };
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
                        yield return new object[] { server, syncUnknownLengthStream, data };
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
                        yield return new object[] { server, asyncStream, data };
                    }
                }
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task PostAsync_CallMethod_NullContent(Uri remoteServer)
        {
            using (var client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.PostAsync(remoteServer, null))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);                    
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        string.Empty);                    
                }
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task PostAsync_CallMethod_EmptyContent(Uri remoteServer)
        {
            using (var client = new HttpClient())
            {
                var content = new StringContent(string.Empty);
                using (HttpResponseMessage response = await client.PostAsync(remoteServer, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);                    
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        string.Empty);                  
                }
            }
        }

        [Fact]
        public async Task GetAsync_CallMethod_ExpectedStatusLine()
        {
            HttpStatusCode expectedStatusCode = HttpStatusCode.MethodNotAllowed;
            string expectedReasonPhrase = "Custom descrption";
            using (var client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(HttpTestServers.StatusCodeUri(
                    false,
                    (int)expectedStatusCode,
                    expectedReasonPhrase)))
                {
                    Assert.Equal(expectedStatusCode, response.StatusCode);
                    Assert.Equal(expectedReasonPhrase, response.ReasonPhrase);
                }
            }
        }

        [Fact]
        [ActiveIssue(3565, PlatformID.OSX)]
        public async Task PostAsync_Post_ChannelBindingHasExpectedValue()
        {
            using (var client = new HttpClient())
            {
                string expectedContent = "Test contest";
                var content = new ChannelBindingAwareContent(expectedContent);
                using (HttpResponseMessage response = await client.PostAsync(HttpTestServers.SecureRemoteEchoServer, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    ChannelBinding channelBinding = content.ChannelBinding;
                    Assert.NotNull(channelBinding);
                    _output.WriteLine("Channel Binding: {0}", channelBinding);
                }
            }
        }

        #endregion

        #region Various HTTP Method Tests

        [Theory, MemberData(nameof(HttpMethods))]
        public async Task SendAsync_SendRequestUsingMethodToEchoServerWithNoContent_MethodCorrectlySent(
            string method,
            bool secureServer)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(
                    new HttpMethod(method), 
                    secureServer ? HttpTestServers.SecureRemoteEchoServer : HttpTestServers.RemoteEchoServer);
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    TestHelper.VerifyRequestMethod(response, method);
                }
            }        
        }

        [Theory, MemberData(nameof(HttpMethodsThatAllowContent))]
        public async Task SendAsync_SendRequestUsingMethodToEchoServerWithContent_Success(
            string method,
            bool secureServer)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(
                    new HttpMethod(method), 
                    secureServer ? HttpTestServers.SecureRemoteEchoServer : HttpTestServers.RemoteEchoServer);
                request.Content = new StringContent(ExpectedContent);
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    TestHelper.VerifyRequestMethod(response, method);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);                    
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        ExpectedContent);                    
                }
            }        
        }
        #endregion

        #region Version tests
        // The HTTP RFC 7230 states that servers are NOT required to respond back with the same
        // minor version if they support a higher minor version. In fact, the RFC states that
        // servers SHOULD send back the highest minor version they support. So, testing the
        // response version to see if the client sent a particular request version only works
        // for some servers. In particular the 'Http2Servers' used in these tests always seem
        // to echo the minor version of the request.
        [Theory, MemberData(nameof(Http2Servers))]
        public async Task SendAsync_RequestVersion10_ServerReceivesVersion10Request(Uri server)
        {
            Version responseVersion = await SendRequestAndGetResponseVersionAsync(new Version(1, 0), server);
            Assert.Equal(new Version(1, 0), responseVersion);
        }

        [Theory, MemberData(nameof(Http2Servers))]
        public async Task SendAsync_RequestVersion11_ServerReceivesVersion11Request(Uri server)
        {
            Version responseVersion = await SendRequestAndGetResponseVersionAsync(new Version(1, 1), server);
            Assert.Equal(new Version(1, 1), responseVersion);
        }

        [Theory, MemberData(nameof(Http2Servers))]
        public async Task SendAsync_RequestVersionNotSpecified_ServerReceivesVersion11Request(Uri server)
        {
            Version responseVersion = await SendRequestAndGetResponseVersionAsync(null, server);
            Assert.Equal(new Version(1, 1), responseVersion);
        }

        [Theory, MemberData(nameof(Http2Servers))]
        public async Task SendAsync_RequestVersion20_ResponseVersion20IfHttp2Supported(Uri server)
        {
            // We don't currently have a good way to test whether HTTP/2 is supported without
            // using the same mechanism we're trying to test, so for now we allow both 2.0 and 1.1 responses.
            Version responseVersion = await SendRequestAndGetResponseVersionAsync(new Version(2, 0), server);
            Assert.True(
                responseVersion == new Version(2, 0) || 
                responseVersion == new Version(1, 1),
                "Response version " + responseVersion);
        }

        private async Task<Version> SendRequestAndGetResponseVersionAsync(Version requestVersion, Uri server)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, server);
            if (requestVersion != null)
            {
                request.Version = requestVersion;
            }
            else
            {
                // The default value for HttpRequestMessage.Version is Version(1,1).
                // So, we need to set something different to test the "unknown" version.
                request.Version = new Version(0,0);
            }

            using (var client = new HttpClient())
            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                return response.Version;
            }
        }
        #endregion

        #region Proxy tests
        [Theory]
        [MemberData(nameof(CredentialsForProxy))]
        public void Proxy_BypassFalse_GetRequestGoesThroughCustomProxy(ICredentials creds)
        {
            int port;
            Task<LoopbackGetRequestHttpProxy.ProxyResult> proxyTask = LoopbackGetRequestHttpProxy.StartAsync(out port, requireAuth: creds != null && creds != CredentialCache.DefaultCredentials);
            Uri proxyUrl = new Uri($"http://localhost:{port}");

            using (var handler = new HttpClientHandler() { Proxy = new UseSpecifiedUriWebProxy(proxyUrl, creds) })
            using (var client = new HttpClient(handler))
            {
                Task<HttpResponseMessage> responseTask = client.GetAsync(HttpTestServers.RemoteEchoServer);
                Task<string> responseStringTask = responseTask.ContinueWith(t => t.Result.Content.ReadAsStringAsync(), TaskScheduler.Default).Unwrap();
                Task.WaitAll(proxyTask, responseTask, responseStringTask);

                TestHelper.VerifyResponseBody(responseStringTask.Result, responseTask.Result.Content.Headers.ContentMD5, false, null);
                Assert.Equal(Encoding.ASCII.GetString(proxyTask.Result.ResponseContent), responseStringTask.Result);

                NetworkCredential nc = creds != null ? creds.GetCredential(proxyUrl, "Basic") : null;
                string expectedAuth =
                    nc == null || nc == CredentialCache.DefaultCredentials ? null :
                    string.IsNullOrEmpty(nc.Domain) ? $"{nc.UserName}:{nc.Password}" :
                    $"{nc.Domain}\\{nc.UserName}:{nc.Password}";
                Assert.Equal(expectedAuth, proxyTask.Result.AuthenticationHeaderValue);
            }
        }

        [Theory]
        [MemberData(nameof(BypassedProxies))]
        public async Task Proxy_BypassTrue_GetRequestDoesntGoesThroughCustomProxy(IWebProxy proxy)
        {
            using (var client = new HttpClient(new HttpClientHandler() { Proxy = proxy }))
            using (HttpResponseMessage response = await client.GetAsync(HttpTestServers.RemoteEchoServer))
            {
                TestHelper.VerifyResponseBody(
                    await response.Content.ReadAsStringAsync(),
                    response.Content.Headers.ContentMD5,
                    false,
                    null);
            }
        }

        private sealed class UseSpecifiedUriWebProxy : IWebProxy
        {
            private readonly Uri _uri;
            private readonly bool _bypass;

            public UseSpecifiedUriWebProxy(Uri uri, ICredentials credentials = null, bool bypass = false)
            {
                _uri = uri;
                _bypass = bypass;
                Credentials = credentials;
            }

            public ICredentials Credentials { get; set; }
            public Uri GetProxy(Uri destination) => _uri;
            public bool IsBypassed(Uri host) => _bypass;
        }

        private sealed class PlatformNotSupportedWebProxy : IWebProxy
        {
            public ICredentials Credentials { get; set; }
            public Uri GetProxy(Uri destination) { throw new PlatformNotSupportedException(); }
            public bool IsBypassed(Uri host) { throw new PlatformNotSupportedException(); }
        }

        private static IEnumerable<object[]> BypassedProxies()
        {
            yield return new object[] { null };
            yield return new object[] { new PlatformNotSupportedWebProxy() };
            yield return new object[] { new UseSpecifiedUriWebProxy(new Uri($"http://{Guid.NewGuid().ToString().Substring(0, 15)}:12345"), bypass: true) };
        }

        private static IEnumerable<object[]> CredentialsForProxy()
        {
            yield return new object[] { null };
            yield return new object[] { new NetworkCredential("username", "password") };
            yield return new object[] { new NetworkCredential("username", "password", "domain") };
            yield return new object[] { CredentialCache.DefaultCredentials };
        }
        #endregion
    }
}
