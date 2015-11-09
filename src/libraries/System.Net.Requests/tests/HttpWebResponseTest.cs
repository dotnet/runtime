// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Tests
{
    public class HttpWebResponseTest
    {
        [Fact]
        public async Task ContentType_ServerResponseHasContentTypeHeader_ContentTypeIsNonEmptyString()
        {
            HttpWebRequest request = WebRequest.CreateHttp(HttpTestServers.RemoteEchoServer);
            request.Method = HttpMethod.Get.Method;
            WebResponse response = await request.GetResponseAsync();
            Assert.True(!string.IsNullOrEmpty(response.ContentType));
        }

        [Fact]
        public async Task ContentType_ServerResponseMissingContentTypeHeader_ContentTypeIsEmptyString()
        {
            HttpWebRequest request = WebRequest.CreateHttp(HttpTestServers.RemoteEmptyContentServer);
            request.Method = HttpMethod.Get.Method;
            WebResponse response = await request.GetResponseAsync();
            Assert.Equal(string.Empty, response.ContentType);
        }
    }
}
