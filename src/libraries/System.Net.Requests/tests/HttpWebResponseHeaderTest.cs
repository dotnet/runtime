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
    public class HttpWebResponseHeaderTest
    {
        public void HttpContinueMethod(int StatusCode, WebHeaderCollection httpHeaders)
        {
        }

        [OuterLoop]
        [Fact]
        public async Task HttpWebRequest_ContinueDelegateProperty_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = HttpMethod.Get.Method;
                HttpContinueDelegate continueDelegate = new HttpContinueDelegate(HttpContinueMethod);
                request.ContinueDelegate = continueDelegate;
                Task<WebResponse> getResponse = request.GetResponseAsync();
                DateTimeOffset utcNow = DateTimeOffset.UtcNow;
                await LoopbackServer.ReadRequestAndSendResponseAsync(server,
                        $"HTTP/1.1 200 OK\r\n" +
                        $"Date: {utcNow:R}\r\n" +
                        "Content-Type: application/json;charset=UTF-8\r\n" +
                        "Content-Length: 5\r\n" +
                        "\r\n" +
                        "12345");
                Assert.Equal(continueDelegate, request.ContinueDelegate);
            });
        }

        [OuterLoop]
        [Fact]
        public async Task HttpHeader_Set_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = HttpMethod.Get.Method;
                Task<WebResponse> getResponse = request.GetResponseAsync();
                DateTimeOffset utcNow = DateTimeOffset.UtcNow;
                await LoopbackServer.ReadRequestAndSendResponseAsync(server,
                        $"HTTP/1.1 200 OK\r\n" +
                        $"Date: {utcNow:R}\r\n" +
                        "Content-Type: application/json;charset=UTF-8\r\n" +
                        "Content-Length: 5\r\n" +
                        "\r\n" +
                        "12345");

                using (WebResponse response = await getResponse)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    Assert.Equal("UTF-8", httpResponse.CharacterSet);
                    Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                    Assert.Equal("OK", httpResponse.StatusDescription);
                    CookieCollection cookieCollection = new CookieCollection();
                    httpResponse.Cookies = cookieCollection;
                    Assert.Equal(cookieCollection, httpResponse.Cookies);
                    Assert.Equal(5,httpResponse.ContentLength);
                    Assert.Equal(5, int.Parse(httpResponse.GetResponseHeader("Content-Length")));
                }
            });
        }

        [OuterLoop]
        [Fact]
        public async Task HttpWebResponse_Close_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = HttpMethod.Get.Method;
                Task<WebResponse> getResponse = request.GetResponseAsync();
                DateTimeOffset utcNow = DateTimeOffset.UtcNow;
                await LoopbackServer.ReadRequestAndSendResponseAsync(server,
                        $"HTTP/1.1 200 OK\r\n" +
                        $"Date: {utcNow:R}\r\n" +
                        "Content-Type: application/json;charset=UTF-8\r\n" +
                        "Content-Length: 5\r\n" +
                        "\r\n" +
                        "12345");
                WebResponse response = await getResponse;
                HttpWebResponse httpResponse = (HttpWebResponse)response;
                httpResponse.Close();
                Assert.Throws<ObjectDisposedException>(() =>
                {
                    httpResponse.GetResponseStream();
                });
            });
        }

    }
}
