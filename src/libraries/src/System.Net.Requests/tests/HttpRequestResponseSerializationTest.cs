// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization.Formatters.Tests;
using System.Net.Test.Common;
using Xunit;

namespace System.Net.HttpRequest.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public partial class HttpRequestResponseSerializationTest
    {
        [ActiveIssue(14068)]
        [OuterLoop]
        [Fact]
        public void SerializeDeserialize_Roundtrips()
        {
            HttpWebRequest request = WebRequest.CreateHttp(Configuration.Http.RemoteEchoServer);
            request.KeepAlive = true;
            request.CookieContainer = new CookieContainer(10);
           
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (HttpWebResponse responseClone = BinaryFormatterHelpers.Clone(response))
            {
                Assert.NotSame(responseClone, response);
                Assert.Equal(response.CharacterSet, responseClone.CharacterSet);
                Assert.Equal(response.ContentEncoding, responseClone.ContentEncoding);
                Assert.Equal(response.ContentLength, responseClone.ContentLength);
                Assert.Equal(response.ContentType, responseClone.ContentType);
                Assert.Equal(response.Cookies.Count, responseClone.Cookies.Count);
                Assert.Equal(response.Headers.Count, responseClone.Headers.Count);
                Assert.Equal(response.IsFromCache, responseClone.IsFromCache);
                Assert.Equal(response.IsMutuallyAuthenticated, responseClone.IsMutuallyAuthenticated);
                Assert.Equal(response.LastModified, responseClone.LastModified);
                Assert.Equal(response.Method, responseClone.Method);
                Assert.Equal(response.ProtocolVersion, responseClone.ProtocolVersion);
                Assert.Equal(response.ResponseUri, responseClone.ResponseUri);
                Assert.Equal(response.Server, responseClone.Server);
                Assert.Equal(response.StatusCode, responseClone.StatusCode);
                Assert.Equal(response.StatusDescription, responseClone.StatusDescription);
                Assert.Equal(response.SupportsHeaders, responseClone.SupportsHeaders);
            }
        }
    }
}
