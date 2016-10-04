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
    public class HttpWebRequestTest17
    {
        public readonly static object[][] EchoServers = System.Net.Test.Common.Configuration.Http.EchoServers;

        [Theory, MemberData(nameof(EchoServers))]
        public void Ctor_VerifyHttpRequestDefaults(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            Assert.True(request.AllowAutoRedirect);            
            Assert.False(request.AllowReadStreamBuffering);
            Assert.False(request.HaveResponse);         
            Assert.NotNull(HttpWebRequest.DefaultCachePolicy);
            Assert.Null(request.CookieContainer);
            Assert.Equal(HttpWebRequest.DefaultMaximumErrorResponseLength,0);
            Assert.Equal(HttpWebRequest.DefaultMaximumResponseHeadersLength,65536);
            Assert.Null(request.CookieContainer);             
            Assert.True(request.AllowWriteStreamBuffering);                        
            Assert.NotNull(request.ClientCertificates);
            Assert.Throws<NotImplementedException>(() => request.ConnectionGroupName);
        }

        [Theory, MemberData(nameof(EchoServers))]
        public WebResponse GetResponse_UseDefaultCredentials_ExpectSuccess(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.UseDefaultCredentials = true;
            WebResponse response = request.GetResponse();
            return response;
        }


        [Theory, MemberData(nameof(EchoServers))]
        public void TryHttpHeaderValueDate(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            {
                DateTime now = DateTime.UtcNow;
                request.Date = now;
                Assert.NotNull(request.Date);
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void TryCookieContainer(Uri remoteServer)
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

        [Theory, MemberData(nameof(EchoServers))]
        public void TrySerivePointGetter(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            {
                Assert.Throws<PlatformNotSupportedException>(() => request.ServicePoint);
            }
        }

        [Theory, MemberData(nameof(EchoServers))]
        public void TryAddRange(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            {
                request.AddRange(1, 5);
                Assert.Equal(request.Headers["Range"], "bytes=1-5");
            }
        }

    }
}
