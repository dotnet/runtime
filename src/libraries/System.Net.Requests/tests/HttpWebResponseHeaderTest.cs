// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Http;
using System.Net.Test.Common;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;

using Xunit;
using System.Runtime.Serialization;

namespace System.Net.Tests
{
    public class HttpWebResponseHeaderTest
    {
        private static void HttpContinueMethod(int StatusCode, WebHeaderCollection httpHeaders)
        {
        }

        [Fact]
        public async Task HttpWebRequest_ContinueDelegateProperty_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = HttpMethod.Get.Method;
                HttpContinueDelegate continueDelegate = new HttpContinueDelegate(HttpContinueMethod);
                request.ContinueDelegate = continueDelegate;
                _ = request.GetResponseAsync();
                await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.OK, "Content-Type: application/json;charset=UTF-8\r\n", "12345");
                Assert.Equal(continueDelegate, request.ContinueDelegate);
            });
        }

        [Fact]
        public async Task HttpHeader_Set_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = HttpMethod.Get.Method;
                Task<WebResponse> getResponse = request.GetResponseAsync();
                await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.OK, "Content-Type: application/json;charset=UTF-8\r\n", "12345");

                using (WebResponse response = await getResponse)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;

                    Assert.Equal("UTF-8", httpResponse.CharacterSet);
                    Assert.Equal("", httpResponse.ContentEncoding);
                    Assert.Equal(5, httpResponse.ContentLength);
                    Assert.Equal(5, int.Parse(httpResponse.GetResponseHeader("Content-Length")));
                    Assert.Equal("application/json; charset=UTF-8", httpResponse.ContentType);
                    Assert.False(httpResponse.IsFromCache);
                    Assert.False(httpResponse.IsMutuallyAuthenticated);
                    Assert.Equal("GET", httpResponse.Method);
                    Assert.Equal(HttpVersion.Version11, httpResponse.ProtocolVersion);
                    Assert.Equal(url, httpResponse.ResponseUri);
                    Assert.Equal("", httpResponse.Server);
                    Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                    Assert.Equal("OK", httpResponse.StatusDescription);
                    Assert.True(httpResponse.SupportsHeaders);

                    CookieCollection cookieCollection = new CookieCollection();
                    httpResponse.Cookies = cookieCollection;
                    Assert.Equal(cookieCollection, httpResponse.Cookies);
                }
            });
        }

        [Fact]
        public async Task HttpWebResponse_Close_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = HttpMethod.Get.Method;
                Task<WebResponse> getResponse = request.GetResponseAsync();
                await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.OK, "Content-Type: application/json;charset=UTF-8\r\n", "12345");
                WebResponse response = await getResponse;
                HttpWebResponse httpResponse = (HttpWebResponse)response;
                httpResponse.Close();

                Assert.Throws<ObjectDisposedException>(() =>
                {
                    httpResponse.GetResponseStream();
                });
            });
        }

        [Fact]
        public async Task LastModified_ValidDate_Success()
        {
            DateTime expected = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(2018, 4, 10, 3, 4, 5, DateTimeKind.Utc), TimeZoneInfo.Local);
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                Task<WebResponse> getResponse = request.GetResponseAsync();
                await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.OK, "Last-Modified: Tue, 10 Apr 2018 03:04:05 GMT\r\n", "12345");

                using (HttpWebResponse response = (HttpWebResponse)(await getResponse))
                {
                    Assert.Equal(expected, response.LastModified);
                }
            });
        }

        [Fact]
        public async Task LastModified_InvalidDate_Throws()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                Task<WebResponse> getResponse = request.GetResponseAsync();
                await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.OK, "Last-Modified: invalid date\r\n", "12345");

                using (HttpWebResponse response = (HttpWebResponse)(await getResponse))
                {
                    Assert.Throws<ProtocolViolationException>(() => response.LastModified);
                }
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37087", TestPlatforms.Android)]
        public async Task HttpWebResponse_Serialize_ExpectedResult()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = HttpMethod.Get.Method;
                Task<WebResponse> getResponse = request.GetResponseAsync();
                await server.AcceptConnectionSendResponseAndCloseAsync();

                using (WebResponse response = await getResponse)
                {
                    using (MemoryStream fs = new MemoryStream())
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        HttpWebResponse hwr = (HttpWebResponse)response;

                        // HttpWebResponse is not serializable on .NET Core.
                        Assert.Throws<SerializationException>(() => formatter.Serialize(fs, hwr));
                    }
                }
            });
        }
    }
}
