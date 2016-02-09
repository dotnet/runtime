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
