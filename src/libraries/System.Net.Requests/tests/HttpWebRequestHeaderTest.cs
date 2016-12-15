// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class HttpWebRequestHeaderTest
    {
        public static readonly object[][] EchoServers = Configuration.Http.EchoServers;

        [OuterLoop] 
        [Theory, MemberData(nameof(EchoServers))]
        public void Ctor_VerifyHttpRequestDefaults(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.True(request.AllowAutoRedirect);
            Assert.False(request.AllowReadStreamBuffering);
            Assert.False(request.HaveResponse);
            Assert.NotNull(HttpWebRequest.DefaultCachePolicy);
            Assert.Null(request.CookieContainer);
            Assert.Equal(0, HttpWebRequest.DefaultMaximumErrorResponseLength); // NetFX behavior difference (64 on NetFX).
            Assert.Equal(65536, HttpWebRequest.DefaultMaximumResponseHeadersLength); // NetFX behavior difference (64 on NetFX).
            Assert.Null(request.CookieContainer);
            Assert.True(request.AllowWriteStreamBuffering);
            Assert.NotNull(request.ClientCertificates);
            Assert.Throws<NotImplementedException>(() => request.ConnectionGroupName); //NetFX behavior difference.
        }

        [OuterLoop]
        [Theory, MemberData(nameof(EchoServers))]
        public WebResponse GetResponse_UseDefaultCredentials_ExpectSuccess(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.UseDefaultCredentials = true;
            WebResponse response = request.GetResponse();
            return response;
        }

        [OuterLoop]
        [Theory, MemberData(nameof(EchoServers))]
        public void Date_HttpHeader_NotNull(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            {
                DateTime now = DateTime.UtcNow;
                request.Date = now;
                Assert.NotNull(request.Date);
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

        [OuterLoop]
        [Theory, MemberData(nameof(EchoServers))]
        public void HttpWebRequest_ServicePoint_Throws(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.Throws<PlatformNotSupportedException>(() => request.ServicePoint); // NetFX Behavior difference.
        }

        [OuterLoop]
        [Theory, MemberData(nameof(EchoServers))]
        public void Range_Add_Success(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.AddRange(1, 5);
            Assert.Equal(request.Headers["Range"], "bytes=1-5");
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void HttpWebRequest_EndGetRequestStreamContext_Null(Uri remoteServer)
        {
            System.Net.TransportContext context;
            HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(remoteServer);
            httpWebRequest.Method = "POST";

            using (httpWebRequest.EndGetRequestStream(httpWebRequest.BeginGetRequestStream(null, null), out context))
            {
                Assert.Equal(null, context); // NetFX behavior difference.
            }
        }

        [Fact]
        public void HttpWebRequest_ConnectionTryAddCloseKeepAlive_Fails()
        {
            HttpWebRequest request = WebRequest.CreateHttp(Configuration.Http.RemoteEchoServer);
            Assert.Throws<ArgumentException>(() => { request.Connection = "Close;keep-alive"; });
        }

        [Fact]
        public void HttpWebRequest_DefaultCachePolicy_BypassCache()
        {
            Assert.Equal(Cache.RequestCacheLevel.BypassCache, HttpWebRequest.DefaultCachePolicy.Level);
        }

        [OuterLoop]
        [Theory, MemberData(nameof(EchoServers))]
        public void HttpWebRequest_ProxySetAfterGetResponse_Fails(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.Throws<InvalidOperationException>(() => { request.Proxy = WebRequest.DefaultWebProxy; });
            }
        }

        [Fact]
        public void HttpWebRequest_TimeOutSetGet_Ok()
        {
            HttpWebRequest request = WebRequest.CreateHttp(Configuration.Http.RemoteEchoServer);
            request.Timeout = 100;
            Assert.Equal(100, request.Timeout);

            request.Timeout = Threading.Timeout.Infinite;
            Assert.Equal(Threading.Timeout.Infinite, request.Timeout);

            request.Timeout = int.MaxValue;
            Assert.Equal(int.MaxValue, request.Timeout);
        }

        [Fact]
        public void HttpWebRequest_TimeOutDefaultGet_Ok()
        {
            HttpWebRequest request = WebRequest.CreateHttp(Configuration.Http.RemoteEchoServer);
            Assert.Equal(100000, request.Timeout);
        }

        [Fact]
        public void HttpWebRequest_TimeOutNegative_Throws()
        {
            HttpWebRequest request = WebRequest.CreateHttp(Configuration.Http.RemoteEchoServer);
            Assert.Throws<ArgumentOutOfRangeException>(() => { request.Timeout = -10; });
        }

        [Fact]
        public void HttpWebRequest_Timeout_Throws()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                s.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                s.Listen(0);

                IPEndPoint ep = (IPEndPoint)s.LocalEndPoint;
                Uri uri = new Uri($"http://{ep.Address}:{ep.Port}");

                HttpWebRequest request = WebRequest.CreateHttp(uri);
                request.Timeout = 10; // ms.
                
                var sw = Stopwatch.StartNew();
                WebException exception = Assert.Throws<WebException>(() =>
                {
                    var response = (HttpWebResponse)request.GetResponse();
                });

                sw.Stop();

                Assert.InRange(sw.ElapsedMilliseconds, 1, 10 * 1000); // Allow a very wide range.
                Assert.Equal(WebExceptionStatus.Timeout, exception.Status);
                Assert.Equal(null, exception.InnerException);
                Assert.Equal(null, exception.Response);
            }
        }

        [Fact]
        public void HttpWebRequest_ContentLengthSetGet_Ok()
        {
            HttpWebRequest request = WebRequest.CreateHttp(Configuration.Http.RemoteEchoServer);
            request.ContentLength = 100;
            Assert.Equal(100, request.ContentLength);
        }

        [Fact]
        public void HttpWebRequest_PreAuthenticateGetSet_Ok()
        {
            HttpWebRequest request = WebRequest.CreateHttp(Configuration.Http.RemoteEchoServer);
            request.PreAuthenticate = true;
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.True(request.PreAuthenticate);
            }
        }
    }
}
