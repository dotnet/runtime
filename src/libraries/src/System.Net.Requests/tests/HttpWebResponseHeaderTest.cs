// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Http;
using System.Net.Test.Common;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;

using Xunit;
using Xunit.Abstractions;
using System.Runtime.Serialization;

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
                if (PlatformDetection.IsFullFramework)
                {
                    Stream stream = httpResponse.GetResponseStream();
                }
                else
                {
                    // TODO: Issue #18851. Investigate .NET Core to see if it can
                    // match .NET Framework.
                    Assert.Throws<ObjectDisposedException>(() =>
                    {
                        httpResponse.GetResponseStream();
                    });
                }
            });
        }

        [Fact]
        public async Task HttpWebResponse_Serialize_ExpectedResult()
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
                        "Content-Length: 0\r\n" +
                        "\r\n");

                using (WebResponse response = await getResponse)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    using (MemoryStream fs = new MemoryStream())
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        HttpWebResponse hwr = (HttpWebResponse)response;

                        if (PlatformDetection.IsFullFramework)
                        {
                            formatter.Serialize(fs, hwr);
                        }
                        else
                        {
                            // HttpWebResponse is not serializable on .NET Core.
                            Assert.Throws<SerializationException>(() => formatter.Serialize(fs, hwr));
                        }
                    }
                }
            });
        } 
    }
}
