// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Tests
{
    public partial class HttpWebRequestTest
    {
        private const string RequestBody = "This is data to POST.";
        private readonly byte[] _requestBodyBytes = Encoding.UTF8.GetBytes(RequestBody);
        private readonly NetworkCredential _explicitCredential = new NetworkCredential("user", "password", "domain");
        private readonly ITestOutputHelper _output;

        public static readonly object[][] EchoServers = System.Net.Test.Common.Configuration.Http.EchoServers;

        public HttpWebRequestTest(ITestOutputHelper output)
        {
            _output = output;
            // On .NET Framework, the default limit for connections/server is very low (2). 
            // On .NET Core, the default limit is higher. Since these tests run in parallel,
            // the limit needs to be increased to avoid timeouts when running the tests.
            System.Net.ServicePointManager.DefaultConnectionLimit = int.MaxValue;
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Ctor_VerifyDefaults_Success(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Null(request.Accept);
            Assert.True(request.AllowAutoRedirect);
            Assert.False(request.AllowReadStreamBuffering);
            Assert.True(request.AllowWriteStreamBuffering);
            Assert.Null(request.ContentType);
            Assert.Equal(350, request.ContinueTimeout);
            Assert.NotNull(request.ClientCertificates);
            Assert.Null(request.CookieContainer);
            Assert.Null(request.Credentials);
            Assert.False(request.HaveResponse);
            Assert.NotNull(request.Headers);
            Assert.True(request.KeepAlive);
            Assert.Equal(0, request.Headers.Count);
            Assert.Equal(HttpVersion.Version11, request.ProtocolVersion);
            Assert.Equal("GET", request.Method);
            Assert.Equal(HttpWebRequest.DefaultMaximumResponseHeadersLength, 64);
            Assert.NotNull(HttpWebRequest.DefaultCachePolicy);
            Assert.Equal(HttpWebRequest.DefaultCachePolicy.Level, RequestCacheLevel.BypassCache);
            Assert.Equal(PlatformDetection.IsFullFramework ? 64 : 0, HttpWebRequest.DefaultMaximumErrorResponseLength);
            Assert.NotNull(request.Proxy);
            Assert.Equal(remoteServer, request.RequestUri);
            Assert.True(request.SupportsCookieContainer);
            Assert.Equal(100000, request.Timeout);
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

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "not supported on .NET Framework")]
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
            using (WebResponse response = await request.GetResponseAsync())
            using (Stream myStream = response.GetResponseStream())
            using (StreamReader sr = new StreamReader(myStream))
            {
                string strContent = sr.ReadToEnd();
                long length = response.ContentLength;
                Assert.Equal(strContent.Length, length);
            }               
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ContentLength_SetAfterRequestSubmitted_ThrowsInvalidOperationException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            using (WebResponse response = request.GetResponse())
            {
                Assert.Throws<InvalidOperationException>(() => request.ContentLength = 255);
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ContentLength_SetNegativeOne_ThrowsArgumentOutOfRangeException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => request.ContentLength = -1);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ContentLength_SetThenGetOne_Success(Uri remoteServer)
        {
            const int ContentLength = 1;
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.ContentLength = ContentLength;
            Assert.Equal(ContentLength, request.ContentLength);
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
        public void MaximumResponseHeadersLength_SetAfterRequestSubmitted_ThrowsInvalidOperationException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            using (WebResponse response = request.GetResponse())
            {
                Assert.Throws<InvalidOperationException>(() => request.MaximumResponseHeadersLength = 255);
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void MaximumResponseHeadersLength_SetNegativeTwo_ThrowsArgumentOutOfRangeException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => request.MaximumResponseHeadersLength = -2);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void MaximumResponseHeadersLength_SetThenGetNegativeOne_Success(Uri remoteServer)
        {
            const int MaximumResponseHeaderLength = -1;
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.MaximumResponseHeadersLength = MaximumResponseHeaderLength;
            Assert.Equal(MaximumResponseHeaderLength, request.MaximumResponseHeadersLength);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void MaximumAutomaticRedirections_SetZeroOrNegative_ThrowsArgumentException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ArgumentException>("value", () => request.MaximumAutomaticRedirections = 0);
            Assert.Throws<ArgumentException>("value", () => request.MaximumAutomaticRedirections = -1);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void MaximumAutomaticRedirections_SetThenGetOne_Success(Uri remoteServer)
        {
            const int MaximumAutomaticRedirections = 1;
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.MaximumAutomaticRedirections = MaximumAutomaticRedirections;
            Assert.Equal(MaximumAutomaticRedirections, request.MaximumAutomaticRedirections);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ContinueTimeout_SetAfterRequestSubmitted_ThrowsInvalidOperationException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            using (WebResponse response = request.GetResponse())
            {
                Assert.Throws<InvalidOperationException>(() => request.ContinueTimeout = 255);
            }
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
            Assert.Throws<ArgumentOutOfRangeException>("value", () => request.ContinueTimeout = -2);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Timeout_SetThenGetZero_ExpectZero(Uri remoteServer)
        {
            const int Timeout = 0;
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Timeout = Timeout;
            Assert.Equal(Timeout, request.Timeout);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Timeout_SetNegativeOne_Success(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Timeout = -1;
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Timeout_SetNegativeTwo_ThrowsArgumentOutOfRangeException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => request.Timeout = -2);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void TimeOut_SetThenGet_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Timeout = 100;
            Assert.Equal(100, request.Timeout);

            request.Timeout = Threading.Timeout.Infinite;
            Assert.Equal(Threading.Timeout.Infinite, request.Timeout);

            request.Timeout = int.MaxValue;
            Assert.Equal(int.MaxValue, request.Timeout);
        }

        [Fact]
        public async Task Timeout_SetTenMillisecondsOnLoopback_ThrowsWebException()
        {
            await LoopbackServer.CreateServerAsync((server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Timeout = 10; // ms.

                var sw = Stopwatch.StartNew();
                WebException exception = Assert.Throws<WebException>(() =>
                {
                    var response = (HttpWebResponse)request.GetResponse();
                });
                
                sw.Stop();

                Assert.InRange(sw.ElapsedMilliseconds, 1, 15 * 1000);
                Assert.Equal(WebExceptionStatus.Timeout, exception.Status);
                Assert.Equal(null, exception.InnerException);
                Assert.Equal(null, exception.Response);

                return Task.FromResult<object>(null);
            });
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Address_CtorAddress_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Equal(remoteServer, request.Address);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void UserAgent_SetThenGetWindows_ValuesMatch(Uri remoteServer)
        {
            const string UserAgent = "Windows";
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.UserAgent = UserAgent;
            Assert.Equal(UserAgent, request.UserAgent);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Host_SetAfterRequestSubmitted_ThrowsInvalidOperationException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            using (WebResponse response = request.GetResponse())
            {
                Assert.Throws<InvalidOperationException>(() => request.Host = "localhost");
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Host_SetNullValue_ThrowsArgumentNullException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ArgumentNullException>("value", () => request.Host = null);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Host_SetSlash_ThrowsArgumentException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ArgumentException>("value", () => request.Host = "/localhost");
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Host_SetInvalidUri_ThrowsArgumentException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ArgumentException>("value", () => request.Host = "NoUri+-*");
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Host_SetThenGetCustomUri_ValuesMatch(Uri remoteServer)
        {
            const string Host = "localhost";
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Host = Host;
            Assert.Equal(Host, request.Host);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Host_SetThenGetCustomUriWithPort_ValuesMatch(Uri remoteServer)
        {
            const string Host = "localhost:8080";
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Host = Host;
            Assert.Equal(Host, request.Host);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Host_GetDefaultHostSameAsAddress_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Equal(remoteServer.Host, request.Host);
        }

        [Theory]
        [InlineData("https://microsoft.com:8080")]
        public void Host_GetDefaultHostWithCustomPortSameAsAddress_ValuesMatch(string endpoint)
        {
            Uri endpointUri = new Uri(endpoint);
            HttpWebRequest request = WebRequest.CreateHttp(endpointUri);
            Assert.Equal(endpointUri.Host + ":" + endpointUri.Port, request.Host);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Pipelined_SetThenGetBoolean_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Pipelined = true;
            Assert.True(request.Pipelined);
            request.Pipelined = false;
            Assert.False(request.Pipelined);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Referer_SetThenGetReferer_ValuesMatch(Uri remoteServer)
        {
            const string Referer = "Referer";
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Referer = Referer;
            Assert.Equal(Referer, request.Referer);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void TransferEncoding_NullOrWhiteSpace_ValuesMatch(Uri remoteServer)
        {
            const string TransferEncoding = "xml";
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.SendChunked = true;
            request.TransferEncoding = TransferEncoding;
            Assert.Equal(TransferEncoding, request.TransferEncoding);
            request.TransferEncoding = null;
            Assert.Equal(null, request.TransferEncoding);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void TransferEncoding_SetChunked_ThrowsArgumentException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ArgumentException>("value", () => request.TransferEncoding = "chunked");
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void TransferEncoding_SetWithSendChunkedFalse_ThrowsInvalidOperationException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<InvalidOperationException>(() => request.TransferEncoding = "xml");
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void KeepAlive_SetThenGetBoolean_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.KeepAlive = true;
            Assert.True(request.KeepAlive);
            request.KeepAlive = false;
            Assert.False(request.KeepAlive);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(true)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "dotnet/corefx #19225")]
        public void KeepAlive_CorrectConnectionHeaderSent(bool? keepAlive)
        {
            HttpWebRequest request = WebRequest.CreateHttp(System.Net.Test.Common.Configuration.Http.RemoteEchoServer);

            if (keepAlive.HasValue)
            {
                request.KeepAlive = keepAlive.Value;
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var body = new StreamReader(response.GetResponseStream()))
            {
                string content = body.ReadToEnd();
                if (!keepAlive.HasValue || keepAlive.Value)
                {
                    // Validate that the request doesn't contain Connection: "close", but we can't validate
                    // that it does contain Connection: "keep-alive", as that's optional as of HTTP 1.1.
                    Assert.DoesNotContain("\"Connection\": \"close\"", content, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    Assert.Contains("\"Connection\": \"close\"", content, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("\"Keep-Alive\"", content, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void AutomaticDecompression_SetAfterRequestSubmitted_ThrowsInvalidOperationException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            using (WebResponse response = request.GetResponse())
            {
                Assert.Throws<InvalidOperationException>(() => request.AutomaticDecompression = DecompressionMethods.Deflate);
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void AutomaticDecompression_SetAndGetDeflate_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.AutomaticDecompression = DecompressionMethods.Deflate;
            Assert.Equal(DecompressionMethods.Deflate, request.AutomaticDecompression);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void AllowWriteStreamBuffering_SetAndGetBoolean_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.AllowWriteStreamBuffering = true;
            Assert.True(request.AllowWriteStreamBuffering);
            request.AllowWriteStreamBuffering = false;
            Assert.False(request.AllowWriteStreamBuffering);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void AllowAutoRedirect_SetAndGetBoolean_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.AllowAutoRedirect = true;
            Assert.True(request.AllowAutoRedirect);
            request.AllowAutoRedirect = false;
            Assert.False(request.AllowAutoRedirect);
        }

        [Theory, MemberData(nameof(EchoServers))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Netcoreapp, "ConnectionGroupName isn't implemented in Core")]
        public void ConnectionGroupName_SetAndGetGroup_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            

            if (!PlatformDetection.IsFullFramework)
            {
                Assert.Throws<NotImplementedException>(() => request.ConnectionGroupName);
            }
            else
            {
                Assert.Null(request.ConnectionGroupName);
                request.ConnectionGroupName = "Group";
                Assert.Equal("Group", request.ConnectionGroupName);
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void PreAuthenticate_SetAndGetBoolean_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.PreAuthenticate = true;
            Assert.True(request.PreAuthenticate);
            request.PreAuthenticate = false;
            Assert.False(request.PreAuthenticate);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "dotnet/corefx #19225")]
        [Theory, MemberData(nameof(EchoServers))]
        public void PreAuthenticate_SetAndGetBooleanResponse_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.PreAuthenticate = true;
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.True(request.PreAuthenticate);
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Connection_NullOrWhiteSpace_ValuesMatch(Uri remoteServer)
        {
            const string Connection = "connect";
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Connection = Connection;
            Assert.Equal(Connection, request.Connection);
            request.Connection = null;
            Assert.Equal(null, request.Connection);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Connection_SetKeepAliveAndClose_ThrowsArgumentException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ArgumentException>("value", () => request.Connection = "keep-alive");
            Assert.Throws<ArgumentException>("value", () => request.Connection = "close");
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Expect_SetNullOrWhiteSpace_ValuesMatch(Uri remoteServer)
        {
            const string Expect = "101-go";
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Expect = Expect;
            Assert.Equal(Expect, request.Expect);
            request.Expect = null;
            Assert.Equal(null, request.Expect);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Expect_Set100Continue_ThrowsArgumentException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ArgumentException>("value", () => request.Expect = "100-continue");
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void DefaultMaximumResponseHeadersLength_SetAndGetLength_ValuesMatch(Uri remoteServer)
        {
            int defaultMaximumResponseHeadersLength = HttpWebRequest.DefaultMaximumResponseHeadersLength;
            const int NewDefaultMaximumResponseHeadersLength = 255;

            try
            {
                HttpWebRequest.DefaultMaximumResponseHeadersLength = NewDefaultMaximumResponseHeadersLength;
                Assert.Equal(NewDefaultMaximumResponseHeadersLength, HttpWebRequest.DefaultMaximumResponseHeadersLength);
            }
            finally
            {
                HttpWebRequest.DefaultMaximumResponseHeadersLength = defaultMaximumResponseHeadersLength;
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void DefaultMaximumErrorResponseLength_SetAndGetLength_ValuesMatch(Uri remoteServer)
        {
            int defaultMaximumErrorsResponseLength = HttpWebRequest.DefaultMaximumErrorResponseLength;
            const int NewDefaultMaximumErrorsResponseLength = 255;

            try
            {
                HttpWebRequest.DefaultMaximumErrorResponseLength = NewDefaultMaximumErrorsResponseLength;
                Assert.Equal(NewDefaultMaximumErrorsResponseLength, HttpWebRequest.DefaultMaximumErrorResponseLength);
            }
            finally
            {
                HttpWebRequest.DefaultMaximumErrorResponseLength = defaultMaximumErrorsResponseLength;
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void DefaultCachePolicy_SetAndGetPolicyReload_ValuesMatch(Uri remoteServer)
        {
            RequestCachePolicy requestCachePolicy = HttpWebRequest.DefaultCachePolicy;

            try
            {
                RequestCachePolicy newRequestCachePolicy = new RequestCachePolicy(RequestCacheLevel.Reload);
                HttpWebRequest.DefaultCachePolicy = newRequestCachePolicy;
                Assert.Equal(newRequestCachePolicy.Level, HttpWebRequest.DefaultCachePolicy.Level);
            }
            finally
            {
                HttpWebRequest.DefaultCachePolicy = requestCachePolicy;
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void IfModifiedSince_SetMinDateAfterValidDate_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);

            DateTime newIfModifiedSince = new DateTime(2000, 1, 1);
            request.IfModifiedSince = newIfModifiedSince;
            Assert.Equal(newIfModifiedSince, request.IfModifiedSince);

            DateTime ifModifiedSince = DateTime.MinValue;
            request.IfModifiedSince = ifModifiedSince;
            Assert.Equal(ifModifiedSince, request.IfModifiedSince);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Date_SetMinDateAfterValidDate_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);

            DateTime newDate = new DateTime(2000, 1, 1);
            request.Date = newDate;
            Assert.Equal(newDate, request.Date);

            DateTime date = DateTime.MinValue;
            request.Date = date;
            Assert.Equal(date, request.Date);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void SendChunked_SetAfterRequestSubmitted_ThrowsInvalidOperationException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            using (WebResponse response = request.GetResponse())
            {
                Assert.Throws<InvalidOperationException>(() => request.SendChunked = true);
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void SendChunked_SetAndGetBoolean_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.SendChunked = true;
            Assert.True(request.SendChunked);
            request.SendChunked = false;
            Assert.False(request.SendChunked);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ContinueDelegate_SetNullDelegate_Success(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.ContinueDelegate = null;
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ContinueDelegate_SetDelegateThenGet_ValuesSame(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            HttpContinueDelegate continueDelegate = new HttpContinueDelegate((a, b) => { });
            request.ContinueDelegate = continueDelegate;
            Assert.Same(continueDelegate, request.ContinueDelegate);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ServicePoint_GetValue_ExpectedResult(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            if (PlatformDetection.IsFullFramework)
            {
                Assert.NotNull(request.ServicePoint);
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => request.ServicePoint);
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ServerCertificateValidationCallback_SetCallbackThenGet_ValuesSame(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            var serverCertificateVallidationCallback = new Security.RemoteCertificateValidationCallback((a, b, c, d) => true);
            request.ServerCertificateValidationCallback = serverCertificateVallidationCallback;
            Assert.Same(serverCertificateVallidationCallback, request.ServerCertificateValidationCallback);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ClientCertificates_SetNullX509_ThrowsArgumentNullException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ArgumentNullException>("value", () => request.ClientCertificates = null);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ClientCertificates_SetThenGetX509_ValuesSame(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            var certificateCollection = new System.Security.Cryptography.X509Certificates.X509CertificateCollection();
            request.ClientCertificates = certificateCollection;
            Assert.Same(certificateCollection, request.ClientCertificates);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ProtocolVersion_SetInvalidHttpVersion_ThrowsArgumentException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ArgumentException>("value", () => request.ProtocolVersion = new Version());
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void ProtocolVersion_SetThenGetHttpVersions_ValuesMatch(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);

            request.ProtocolVersion = HttpVersion.Version10;
            Assert.Equal(HttpVersion.Version10, request.ProtocolVersion);

            request.ProtocolVersion = HttpVersion.Version11;
            Assert.Equal(HttpVersion.Version11, request.ProtocolVersion);
        }

        [Theory, MemberData(nameof(EchoServers))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Implemented in .NET Framework")]
        public void ReadWriteTimeout_SetThenGet_ThrowsNotImplementedException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<NotImplementedException>(() => request.ReadWriteTimeout = 5);
            Assert.Throws<NotImplementedException>(() => request.ReadWriteTimeout);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void CookieContainer_SetThenGetContainer_Success(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.CookieContainer = null;
            var cookieContainer = new CookieContainer();
            request.CookieContainer = cookieContainer;
            Assert.Same(cookieContainer, request.CookieContainer);
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

        [OuterLoop]
        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotFedoraOrRedHatOrCentos))] // #16201
        [MemberData(nameof(EchoServers))]
        public void UseDefaultCredentials_SetGetResponse_ExpectSuccess(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.UseDefaultCredentials = true;
            WebResponse response = request.GetResponse();
            response.Dispose();
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "no exception thrown on netfx")]
        public void BeginGetRequestStream_CreatePostRequestThenCallTwice_ThrowsInvalidOperationException(Uri remoteServer)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(remoteServer);
            request.Method = "POST";

            IAsyncResult asyncResult = request.BeginGetRequestStream(null, null);
            Assert.Throws<InvalidOperationException>(() =>
            {
                request.BeginGetRequestStream(null, null);
            });
        }

        [Theory, MemberData(nameof(EchoServers))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "request stream not allowed for GET on netfx")]
        public void BeginGetRequestStream_CreateRequestThenBeginGetResponsePrior_ThrowsInvalidOperationException(Uri remoteServer)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(remoteServer);

            IAsyncResult asyncResult = request.BeginGetResponse(null, null);
            Assert.Throws<InvalidOperationException>(() =>
            {
                request.BeginGetRequestStream(null, null);
            });
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void BeginGetResponse_CreateRequestThenCallTwice_ThrowsInvalidOperationException(Uri remoteServer)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(remoteServer);

            IAsyncResult asyncResult = request.BeginGetResponse(null, null);
            Assert.Throws<InvalidOperationException>(() =>
            {
                request.BeginGetResponse(null, null);
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
            using (Stream requestStream = await request.GetRequestStreamAsync())
            {
                requestStream.Write(_requestBodyBytes, 0, _requestBodyBytes.Length);
                Assert.Throws<ArgumentException>(() => new StreamReader(requestStream));
            }            
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task GetRequestStreamAsync_SetPOSTThenGet_ExpectNotNull(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Method = HttpMethod.Post.Method;
            using (Stream requestStream = await request.GetRequestStreamAsync())
            {
                Assert.NotNull(requestStream);
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task GetResponseAsync_GetResponseStream_ExpectNotNull(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            using (WebResponse response = await request.GetResponseAsync())
            {
                Assert.NotNull(response.GetResponseStream());
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task GetResponseAsync_GetResponseStream_ContainsHost(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Method = HttpMethod.Get.Method;
            using (WebResponse response = await request.GetResponseAsync())
            using (Stream myStream = response.GetResponseStream())
            {
                Assert.NotNull(myStream);
                using (var sr = new StreamReader(myStream))
                {
                    string strContent = sr.ReadToEnd();
                    Assert.True(strContent.Contains("\"Host\": \"" + System.Net.Test.Common.Configuration.Http.Host + "\""));
                }
                
            }
        }

        [OuterLoop]
        [Theory, MemberData(nameof(EchoServers))]
        public void CookieContainer_Count_Add(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            DateTime now = DateTime.UtcNow;
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(remoteServer, new Cookie("1", "cookie1"));
            request.CookieContainer.Add(remoteServer, new Cookie("2", "cookie2"));
            Assert.True(request.SupportsCookieContainer);
            Assert.Equal(request.CookieContainer.GetCookies(remoteServer).Count, 2);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Range_Add_Success(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.AddRange(1, 5);
            Assert.Equal(request.Headers["Range"], "bytes=1-5");
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

            using (WebResponse response = await request.GetResponseAsync())
            using (Stream myStream = response.GetResponseStream())
            using (var sr = new StreamReader(myStream))
            {
                string strContent = sr.ReadToEnd();
                Assert.True(strContent.Contains(RequestBody));
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotFedoraOrRedHatOrCentos))] // #16201
        [MemberData(nameof(EchoServers))]
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
            using (WebResponse response = await request.GetResponseAsync())
            {
                Assert.True(request.HaveResponse);
            }
        }

        [MemberData(nameof(EchoServers))]
        public void Headers_SetAfterRequestSubmitted_ThrowsInvalidOperationException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            using (request.GetResponse())
            {
                Assert.Throws<InvalidOperationException>(() => request.Headers = null);
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotFedoraOrRedHatOrCentos))] // #16201
        [MemberData(nameof(EchoServers))]
        public async Task Headers_GetResponseHeaders_ContainsExpectedValue(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                string headersString = response.Headers.ToString();
                string headersPartialContent = "Content-Type: application/json";
                Assert.True(headersString.Contains(headersPartialContent));
            }
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
        public void Method_SetInvalidString_ThrowsArgumentException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<ArgumentException>("value", () => request.Method = null);
            Assert.Throws<ArgumentException>("value", () => request.Method = string.Empty);
            Assert.Throws<ArgumentException>("value", () => request.Method = "Method(2");
        }

        [OuterLoop]
        [Theory, MemberData(nameof(EchoServers))]
        public void Proxy_SetAfterRequestSubmitted_ThrowsInvalidOperationException(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.Throws<InvalidOperationException>(() => request.Proxy = WebRequest.DefaultWebProxy);
            }
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
            using (WebResponse response = await request.GetResponseAsync())
            {
                Assert.Equal(remoteServer, response.ResponseUri);
            }
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
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream responseStream = response.GetResponseStream())
            using (var sr = new StreamReader(responseStream))
            {
                string responseBody = sr.ReadToEnd();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
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

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream responseStream = response.GetResponseStream())
            using (var sr = new StreamReader(responseStream))
            {
                string responseBody = sr.ReadToEnd();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public async Task ContentType_AddHeaderWithNoContent_SendRequest_HeaderGetsSent(Uri remoteServer)
        {
            const string ContentType = "text/plain; charset=utf-8";
            HttpWebRequest request = HttpWebRequest.CreateHttp(remoteServer);
            request.ContentType = ContentType;

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream responseStream = response.GetResponseStream())
            using (var sr = new StreamReader(responseStream))
            {
                string responseBody = sr.ReadToEnd();
                _output.WriteLine(responseBody);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(responseBody.Contains($"\"Content-Type\": \"{ContentType}\""));
            }            
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void MediaType_SetThenGet_ValuesMatch(Uri remoteServer)
        {
            const string MediaType = "text/plain";
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.MediaType = MediaType;
            Assert.Equal(MediaType, request.MediaType);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void HttpWebRequest_EndGetRequestStreamContext_ExpectedValue(Uri remoteServer)
        {
            System.Net.TransportContext context;
            HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(remoteServer);
            httpWebRequest.Method = "POST";

            using (httpWebRequest.EndGetRequestStream(httpWebRequest.BeginGetRequestStream(null, null), out context))
            {
                if (PlatformDetection.IsFullFramework)
                {
                    Assert.NotNull(context);
                }
                else
                {
                    Assert.Null(context);
                }
            }
        }

        [Fact]
        public async Task Abort_BeginGetRequestStreamThenAbort_EndGetRequestStreamThrowsWebException()
        {
            await LoopbackServer.CreateServerAsync((server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = "POST";
                RequestState state = new RequestState();
                state.Request = request;

                request.BeginGetResponse(new AsyncCallback(RequestStreamCallback), state);
                
                request.Abort();
                Assert.Equal(1, state.RequestStreamCallbackCallCount);
                WebException wex = state.SavedRequestStreamException as WebException;
                Assert.Equal(WebExceptionStatus.RequestCanceled, wex.Status);

                return Task.FromResult<object>(null);
            });
        }

        [Fact]
        public async Task Abort_BeginGetResponseThenAbort_ResponseCallbackCalledBeforeAbortReturns()
        {
            await LoopbackServer.CreateServerAsync((server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                RequestState state = new RequestState();
                state.Request = request;

                request.BeginGetResponse(new AsyncCallback(ResponseCallback), state);

                request.Abort();
                Assert.Equal(1, state.ResponseCallbackCallCount);

                return Task.FromResult<object>(null);
            });
        }

        [Fact]
        public async Task Abort_BeginGetResponseThenAbort_EndGetResponseThrowsWebException()
        {
            await LoopbackServer.CreateServerAsync((server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                RequestState state = new RequestState();
                state.Request = request;

                request.BeginGetResponse(new AsyncCallback(ResponseCallback), state);

                request.Abort();

                WebException wex = state.SavedResponseException as WebException;
                Assert.Equal(WebExceptionStatus.RequestCanceled, wex.Status);

                return Task.FromResult<object>(null);
            });
        }

        [Fact]
        public async Task Abort_BeginGetResponseUsingNoCallbackThenAbort_Success()
        {
            await LoopbackServer.CreateServerAsync((server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.BeginGetResponse(null, null);
                request.Abort();

                return Task.FromResult<object>(null);
            });
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void Abort_CreateRequestThenAbort_Success(Uri remoteServer)
        {
            HttpWebRequest request = HttpWebRequest.CreateHttp(remoteServer);

            request.Abort();
        }

        private void RequestStreamCallback(IAsyncResult asynchronousResult)
        {
            RequestState state = (RequestState) asynchronousResult.AsyncState;
            state.RequestStreamCallbackCallCount++;

            try
            {
                HttpWebRequest request = state.Request;
                state.Response = (HttpWebResponse) request.EndGetResponse(asynchronousResult);
      
                Stream stream = (Stream) request.EndGetRequestStream(asynchronousResult);
                stream.Dispose();
            }
            catch (Exception ex)
            {
                state.SavedRequestStreamException = ex;
            }
        }

        private void ResponseCallback(IAsyncResult asynchronousResult)
        {
            RequestState state = (RequestState) asynchronousResult.AsyncState;
            state.ResponseCallbackCallCount++;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse) state.Request.EndGetResponse(asynchronousResult))
                {
                    state.SavedResponseHeaders = response.Headers;
                }
            }
            catch (Exception ex)
            {
                state.SavedResponseException = ex;
            }
        }

        [Fact]
        public void HttpWebRequest_Serialize_Fails()
        {
            using (MemoryStream fs = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                var hwr = HttpWebRequest.CreateHttp("http://localhost");

                if (PlatformDetection.IsFullFramework)
                {
                    // .NET Framework throws a more detailed exception.
                    // System.Runtime.Serialization.SerializationException):
                    //  Type 'System.Net.WebRequest+WebProxyWrapper' in Assembly 'System, Version=4.0.0.
                    //        0, Culture=neutral, PublicKeyToken=b77a5c561934e089' is not marked as serializable.
                    Assert.Throws<System.Runtime.Serialization.SerializationException>(() => formatter.Serialize(fs, hwr));
                }
                else
                {
                    // TODO: Issue #18850. Change HttpWebRquest to throw SerializationException similar to .NET Framework.
                    Assert.Throws<PlatformNotSupportedException>(() => formatter.Serialize(fs, hwr));
                }
            }
        }
    }

    public class RequestState
    {
      public HttpWebRequest Request;
      public HttpWebResponse Response;
      public WebHeaderCollection SavedResponseHeaders;
      public int RequestStreamCallbackCallCount;
      public int ResponseCallbackCallCount;
      public Exception SavedRequestStreamException;
      public Exception SavedResponseException;
    }    
}
