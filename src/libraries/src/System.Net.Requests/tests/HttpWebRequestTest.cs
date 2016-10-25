// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Http;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Tests
{
    public class HttpWebRequestTest
    {
        private const string RequestBody = "This is data to POST.";
        private readonly byte[] _requestBodyBytes = Encoding.UTF8.GetBytes(RequestBody);
        private readonly NetworkCredential _explicitCredential = new NetworkCredential("user", "password", "domain");

        private HttpWebRequest _savedHttpWebRequest = null;
        private WebHeaderCollection _savedResponseHeaders = null;
        private Exception _savedRequestStreamException = null;
        private Exception _savedResponseException = null;

        private int _requestStreamCallbackCallCount = 0;
        private int _responseCallbackCallCount = 0;
        private readonly ITestOutputHelper _output;

        public readonly static object[][] EchoServers = System.Net.Test.Common.Configuration.Http.EchoServers;

        public HttpWebRequestTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Ctor_VerifyDefaults_Success(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Null(request.Accept);
            Assert.False(request.AllowReadStreamBuffering);
            Assert.Null(request.ContentType);
            Assert.Equal(350, request.ContinueTimeout);
            Assert.Null(request.CookieContainer);
            Assert.Null(request.Credentials);
            Assert.False(request.HaveResponse);
            Assert.NotNull(request.Headers);
            Assert.Equal(0, request.Headers.Count);
            Assert.Equal("GET", request.Method);
            Assert.NotNull(request.Proxy);
            Assert.Equal(remoteServer, request.RequestUri);
            Assert.True(request.SupportsCookieContainer);
            Assert.False(request.UseDefaultCredentials);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Ctor_CreateHttpWithString_ExpectNotNull(Uri remoteServer)
        {
            string remoteServerString = remoteServer.ToString();
            HttpWebRequest request = WebRequest.CreateHttp(remoteServerString);
            Assert.NotNull(request);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Ctor_CreateHttpWithUri_ExpectNotNull(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.NotNull(request);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Accept_SetThenGetValidValue_ExpectSameValue(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            string acceptType = "*/*";
            request.Accept = acceptType;
            Assert.Equal(acceptType, request.Accept);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Accept_SetThenGetEmptyValue_ExpectNull(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Accept = string.Empty;
            Assert.Null(request.Accept);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Accept_SetThenGetNullValue_ExpectNull(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Accept = null;
            Assert.Null(request.Accept);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void AllowReadStreamBuffering_SetFalseThenGet_ExpectFalse(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.AllowReadStreamBuffering = false;
            Assert.False(request.AllowReadStreamBuffering);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void AllowReadStreamBuffering_SetTrueThenGet_ExpectTrue(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.AllowReadStreamBuffering = true;
            Assert.True(request.AllowReadStreamBuffering);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task ContentLength_Get_ExpectSameAsGetResponseStream(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            WebResponse response = await request.GetResponseAsync();
            Stream myStream = response.GetResponseStream();
            String strContent;
            using (var sr = new StreamReader(myStream))
            {
                strContent = sr.ReadToEnd();
            }
            long length = response.ContentLength;
            Assert.Equal(strContent.Length, length);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ContentType_SetThenGet_ExpectSameValue(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            string myContent = "application/x-www-form-urlencoded";
            request.ContentType = myContent;
            Assert.Equal(myContent, request.ContentType);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ContentType_SetThenGetEmptyValue_ExpectNull(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.ContentType = string.Empty;
            Assert.Null(request.ContentType);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ContinueTimeout_SetThenGetZero_ExpectZero(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.ContinueTimeout = 0;
            Assert.Equal(0, request.ContinueTimeout);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ContinueTimeout_SetNegativeOne_Success(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.ContinueTimeout = -1;
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ContinueTimeout_SetNegativeTwo_ThrowsArgumentOutOfRangeException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ArgumentOutOfRangeException>(() => request.ContinueTimeout = -2);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Credentials_SetDefaultCredentialsThenGet_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Credentials = CredentialCache.DefaultCredentials;
            Assert.Equal(CredentialCache.DefaultCredentials, request.Credentials);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Credentials_SetExplicitCredentialsThenGet_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Credentials = _explicitCredential;
            Assert.Equal(_explicitCredential, request.Credentials);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void UseDefaultCredentials_SetTrue_CredentialsEqualsDefaultCredentials(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Credentials = _explicitCredential;
            request.UseDefaultCredentials = true;
            Assert.Equal(CredentialCache.DefaultCredentials, request.Credentials);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void UseDefaultCredentials_SetFalse_CredentialsNull(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Credentials = _explicitCredential;
            request.UseDefaultCredentials = false;
            Assert.Equal(null, request.Credentials);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void BeginGetRequestStream_UseGETVerb_ThrowsProtocolViolationException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ProtocolViolationException>(() =>
            {
                request.BeginGetRequestStream(null, null);
            });
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void BeginGetRequestStream_UseHEADVerb_ThrowsProtocolViolationException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Method = HttpMethod.Head.Method;
            Assert.Throws<ProtocolViolationException>(() =>
            {
                request.BeginGetRequestStream(null, null);
            });
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void BeginGetRequestStream_UseCONNECTVerb_ThrowsProtocolViolationException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Method = "CONNECT";
            Assert.Throws<ProtocolViolationException>(() =>
            {
                request.BeginGetRequestStream(null, null);
            });
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void BeginGetRequestStream_CreatePostRequestThenAbort_ThrowsWebException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Method = HttpMethod.Post.Method;
            request.Abort();
            WebException ex = Assert.Throws<WebException>(() => request.BeginGetRequestStream(null, null));
            Assert.Equal(WebExceptionStatus.RequestCanceled, ex.Status);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void BeginGetRequestStream_CreatePostRequestThenCallTwice_ThrowsInvalidOperationException(Uri remoteServer)
        {
            _savedHttpWebRequest = HttpWebRequest.CreateHttp(remoteServer);
            _savedHttpWebRequest.Method = "POST";

            IAsyncResult asyncResult = _savedHttpWebRequest.BeginGetRequestStream(null, null);
            Assert.Throws<InvalidOperationException>(() =>
            {
                _savedHttpWebRequest.BeginGetRequestStream(null, null);
            });
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void BeginGetRequestStream_CreateRequestThenBeginGetResponsePrior_ThrowsInvalidOperationException(Uri remoteServer)
        {
            _savedHttpWebRequest = HttpWebRequest.CreateHttp(remoteServer);

            IAsyncResult asyncResult = _savedHttpWebRequest.BeginGetResponse(null, null);
            Assert.Throws<InvalidOperationException>(() =>
            {
                _savedHttpWebRequest.BeginGetRequestStream(null, null);
            });
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void BeginGetResponse_CreateRequestThenCallTwice_ThrowsInvalidOperationException(Uri remoteServer)
        {
            _savedHttpWebRequest = HttpWebRequest.CreateHttp(remoteServer);

            IAsyncResult asyncResult = _savedHttpWebRequest.BeginGetResponse(null, null);
            Assert.Throws<InvalidOperationException>(() =>
            {
                _savedHttpWebRequest.BeginGetResponse(null, null);
            });
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void BeginGetResponse_CreatePostRequestThenAbort_ThrowsWebException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Method = HttpMethod.Post.Method;
            request.Abort();
            WebException ex = Assert.Throws<WebException>(() => request.BeginGetResponse(null, null));
            Assert.Equal(WebExceptionStatus.RequestCanceled, ex.Status);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task GetRequestStreamAsync_WriteAndDisposeRequestStreamThenOpenRequestStream_ThrowsArgumentException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Method = HttpMethod.Post.Method;
            Stream requestStream;
            using (requestStream = await request.GetRequestStreamAsync())
            {
                requestStream.Write(_requestBodyBytes, 0, _requestBodyBytes.Length);
            }
            Assert.Throws<ArgumentException>(() =>
            {
                var sr = new StreamReader(requestStream);
            });
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task GetRequestStreamAsync_SetPOSTThenGet_ExpectNotNull(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Method = HttpMethod.Post.Method;
            Stream requestStream = await request.GetRequestStreamAsync();
            Assert.NotNull(requestStream);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task GetResponseAsync_GetResponseStream_ExpectNotNull(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            WebResponse response = await request.GetResponseAsync();
            Assert.NotNull(response.GetResponseStream());
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task GetResponseAsync_GetResponseStream_ContainsHost(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Method = HttpMethod.Get.Method;
            WebResponse response = await request.GetResponseAsync();
            Stream myStream = response.GetResponseStream();
            Assert.NotNull(myStream);
            String strContent;
            using (var sr = new StreamReader(myStream))
            {
                strContent = sr.ReadToEnd();
            }
            Assert.True(strContent.Contains("\"Host\": \"" + System.Net.Test.Common.Configuration.Http.Host + "\""));
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task GetResponseAsync_PostRequestStream_ContainsData(Uri remoteServer)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(remoteServer);
            request.Method = HttpMethod.Post.Method;
            using (Stream requestStream = await request.GetRequestStreamAsync())
            {
                requestStream.Write(_requestBodyBytes, 0, _requestBodyBytes.Length);
            }
            WebResponse response = await request.GetResponseAsync();
            Stream myStream = response.GetResponseStream();
            String strContent;
            using (var sr = new StreamReader(myStream))
            {
                strContent = sr.ReadToEnd();
            }
            Assert.True(strContent.Contains(RequestBody));
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task GetResponseAsync_UseDefaultCredentials_ExpectSuccess(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.UseDefaultCredentials = true;
            await request.GetResponseAsync();
        }

        [OuterLoop] // fails on networks with DNS servers that provide a dummy page for invalid addresses
        [Fact]
        public void GetResponseAsync_ServerNameNotInDns_ThrowsWebException()
        {
            string serverUrl = string.Format("http://www.{0}.com/", Guid.NewGuid().ToString());
            HttpWebRequest request = WebRequest.CreateHttp(serverUrl);
            WebException ex = Assert.Throws<WebException>(() => request.GetResponseAsync().GetAwaiter().GetResult());
            Assert.Equal(WebExceptionStatus.NameResolutionFailure, ex.Status);

        }

        public static object[][] StatusCodeServers = {
            new object[] { System.Net.Test.Common.Configuration.Http.StatusCodeUri(false, 404) },
            new object[] { System.Net.Test.Common.Configuration.Http.StatusCodeUri(true, 404) },
        };

        [ActiveIssue(12236, TestPlatforms.Linux)]
        [Theory, MemberData(nameof(StatusCodeServers))]
        public async Task GetResponseAsync_ResourceNotFound_ThrowsWebException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);

            WebException ex = await Assert.ThrowsAsync<WebException>(() => request.GetResponseAsync());
            Assert.Equal(WebExceptionStatus.ProtocolError, ex.Status);

        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task HaveResponse_GetResponseAsync_ExpectTrue(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            WebResponse response = await request.GetResponseAsync();
            Assert.True(request.HaveResponse);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task Headers_GetResponseHeaders_ContainsExpectedValue(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync();
            String headersString = response.Headers.ToString();
            string headersPartialContent = "Content-Type: application/json";
            Assert.True(headersString.Contains(headersPartialContent));
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Method_SetThenGetToGET_ExpectSameValue(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Method = HttpMethod.Get.Method;
            Assert.Equal(HttpMethod.Get.Method, request.Method);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Method_SetThenGetToPOST_ExpectSameValue(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Method = HttpMethod.Post.Method;
            Assert.Equal(HttpMethod.Post.Method, request.Method);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Proxy_GetDefault_ExpectNotNull(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.NotNull(request.Proxy);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void RequestUri_CreateHttpThenGet_ExpectSameUri(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Equal(remoteServer, request.RequestUri);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task ResponseUri_GetResponseAsync_ExpectSameUri(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            WebResponse response = await request.GetResponseAsync();
            Assert.Equal(remoteServer, response.ResponseUri);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void SupportsCookieContainer_GetDefault_ExpectTrue(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.True(request.SupportsCookieContainer);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task SimpleScenario_UseGETVerb_Success(Uri remoteServer)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(remoteServer);
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            Stream responseStream = response.GetResponseStream();
            String responseBody;
            using (var sr = new StreamReader(responseStream))
            {
                responseBody = sr.ReadToEnd();
            }

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task SimpleScenario_UsePOSTVerb_Success(Uri remoteServer)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(remoteServer);
            request.Method = HttpMethod.Post.Method;

            using (Stream requestStream = await request.GetRequestStreamAsync())
            {
                requestStream.Write(_requestBodyBytes, 0, _requestBodyBytes.Length);
            }

            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

            Stream responseStream = response.GetResponseStream();
            String responseBody;
            using (var sr = new StreamReader(responseStream))
            {
                responseBody = sr.ReadToEnd();
            }

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task ContentType_AddHeaderWithNoContent_SendRequest_HeaderGetsSent(Uri remoteServer)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(remoteServer);
            request.ContentType = "application/json";

            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            Stream responseStream = response.GetResponseStream();
            String responseBody;
            using (var sr = new StreamReader(responseStream))
            {
                responseBody = sr.ReadToEnd();
            }
            _output.WriteLine(responseBody);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(responseBody.Contains("Content-Type"));
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Abort_BeginGetRequestStreamThenAbort_EndGetRequestStreamThrowsWebException(Uri remoteServer)
        {
            _savedHttpWebRequest = HttpWebRequest.CreateHttp(remoteServer);
            _savedHttpWebRequest.Method = "POST";

            _savedHttpWebRequest.BeginGetResponse(new AsyncCallback(RequestStreamCallback), null);

            _savedHttpWebRequest.Abort();
            _savedHttpWebRequest = null;
            WebException wex = _savedRequestStreamException as WebException;
            Assert.Equal(WebExceptionStatus.RequestCanceled, wex.Status);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Abort_BeginGetResponseThenAbort_ResponseCallbackCalledBeforeAbortReturns(Uri remoteServer)
        {
            _savedHttpWebRequest = HttpWebRequest.CreateHttp(remoteServer);

            _savedHttpWebRequest.BeginGetResponse(new AsyncCallback(ResponseCallback), null);

            _savedHttpWebRequest.Abort();
            Assert.Equal(1, _responseCallbackCallCount);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Abort_BeginGetResponseThenAbort_EndGetResponseThrowsWebException(Uri remoteServer)
        {
            _savedHttpWebRequest = HttpWebRequest.CreateHttp(remoteServer);

            _savedHttpWebRequest.BeginGetResponse(new AsyncCallback(ResponseCallback), null);

            _savedHttpWebRequest.Abort();
            WebException wex = _savedResponseException as WebException;
            Assert.Equal(WebExceptionStatus.RequestCanceled, wex.Status);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Abort_BeginGetResponseUsingNoCallbackThenAbort_Success(Uri remoteServer)
        {
            _savedHttpWebRequest = HttpWebRequest.CreateHttp(remoteServer);

            _savedHttpWebRequest.BeginGetResponse(null, null);

            _savedHttpWebRequest.Abort();
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Abort_CreateRequestThenAbort_Success(Uri remoteServer)
        {
            _savedHttpWebRequest = HttpWebRequest.CreateHttp(remoteServer);

            _savedHttpWebRequest.Abort();
        }

        private void RequestStreamCallback(IAsyncResult asynchronousResult)
        {
            _requestStreamCallbackCallCount++;

            try
            {
                Stream stream = (Stream)_savedHttpWebRequest.EndGetRequestStream(asynchronousResult);
                stream.Dispose();
            }
            catch (Exception ex)
            {
                _savedRequestStreamException = ex;
            }
        }

        private void ResponseCallback(IAsyncResult asynchronousResult)
        {
            _responseCallbackCallCount++;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)_savedHttpWebRequest.EndGetResponse(asynchronousResult))
                {
                    _savedResponseHeaders = response.Headers;
                }
            }
            catch (Exception ex)
            {
                _savedResponseException = ex;
            }
        }
    }
}
