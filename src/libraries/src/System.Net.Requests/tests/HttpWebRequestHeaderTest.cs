// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Tests
{
    public class HttpWebRequestHeaderTest
    {
        public readonly static object[][] EchoServers = System.Net.Test.Common.Configuration.Http.EchoServers;

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
            Assert.Equal(0,HttpWebRequest.DefaultMaximumErrorResponseLength);
            Assert.Equal(65536,HttpWebRequest.DefaultMaximumResponseHeadersLength);
            Assert.Null(request.CookieContainer);
            Assert.True(request.AllowWriteStreamBuffering);
            Assert.NotNull(request.ClientCertificates);
            Assert.Throws<NotImplementedException>(() => request.ConnectionGroupName);
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
            {
                DateTime now = DateTime.UtcNow;
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(remoteServer, new Cookie("1", "cookie1"));
                request.CookieContainer.Add(remoteServer, new Cookie("2", "cookie2"));
                Assert.True(request.SupportsCookieContainer);
                Assert.Equal(request.CookieContainer.GetCookies(remoteServer).Count, 2);
            }
        }

        [OuterLoop]
        [Theory, MemberData(nameof(EchoServers))]
        public void ServicePoint_PNS_Expect(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            {
                Assert.Throws<PlatformNotSupportedException>(() => request.ServicePoint);
            }
        }

        [OuterLoop]
        [Theory, MemberData(nameof(EchoServers))]
        public void Range_Add_Success(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            {
                request.AddRange(1, 5);
                Assert.Equal(request.Headers["Range"], "bytes=1-5");
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void HttpWebRequest_EndGetRequestStreamContext_Null(Uri remoteServer)
        {
            System.Net.TransportContext context;
            HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(remoteServer);
            httpWebRequest.Method = "POST";
            using (httpWebRequest.EndGetRequestStream(httpWebRequest.BeginGetRequestStream(null, null), out context))
            {
                Assert.Equal(null, context);
            }
        }
    }
}
