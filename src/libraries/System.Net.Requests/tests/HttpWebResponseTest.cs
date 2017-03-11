// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Tests
{
    public class HttpWebResponseTest
    {
        [Theory]
        [InlineData("text/html")]
        [InlineData("text/html; charset=utf-8")]
        [InlineData("TypeAndNoSubType")]
        public async Task ContentType_ServerResponseHasContentTypeHeader_ContentTypeReceivedCorrectly(string expectedContentType)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = HttpMethod.Get.Method;
                Task<WebResponse> getResponse = request.GetResponseAsync();
                await LoopbackServer.ReadRequestAndSendResponseAsync(server,
                        $"HTTP/1.1 200 OK\r\n" +
                        $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                        $"Content-Type: {expectedContentType}\r\n" +
                        "Content-Length: 5\r\n" +
                        "\r\n" +
                        "12345");

                using (WebResponse response = await getResponse)
                {
                    Assert.Equal(expectedContentType, response.ContentType);
                }
            });
        }

        [Fact]
        public async Task ContentType_ServerResponseMissingContentTypeHeader_ContentTypeIsEmptyString()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = HttpMethod.Get.Method;
                Task<WebResponse> getResponse = request.GetResponseAsync();
                await LoopbackServer.ReadRequestAndSendResponseAsync(server,
                        $"HTTP/1.1 200 OK\r\n" +
                        $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                        "Content-Length: 5\r\n" +
                        "\r\n" +
                        "12345");

                using (WebResponse response = await getResponse)
                {
                    Assert.Equal(string.Empty, response.ContentType);
                }
            });
        }
    }
}
