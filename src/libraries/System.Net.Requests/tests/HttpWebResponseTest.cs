// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Net.Tests;

using Xunit;

namespace System.Net.Requests.Test
{
    public class HttpWebResponseTest
    {
        public static object[][] HasContentTypeHeaderServers
        {
            get
            {
                return HttpTestServers.GetServers;
            }
        }

        public static object[][] MissingContentTypeHeaderServers
        {
            get
            {
                return null;
            }
        }

        [Theory, MemberData("HasContentTypeHeaderServers")]
        public void ContentType_ServerResponseHasContentTypeHeader_ContentTypeIsNonEmptyString(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Method = HttpMethod.Get.Method;
            WebResponse response = request.GetResponseAsync().GetAwaiter().GetResult();
            Assert.True(!string.IsNullOrEmpty(response.ContentType));
        }

        [Theory, MemberData("MissingContentTypeHeaderServers"), ActiveIssue(2385)]
        public void ContentType_ServerResponseMissingContentTypeHeader_ContentTypeIsEmptyString(Uri remoteServer)
        {
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.Method = HttpMethod.Get.Method;
            WebResponse response = request.GetResponseAsync().GetAwaiter().GetResult();
            Assert.Equal(string.Empty, response.ContentType);
        }
    }
}
