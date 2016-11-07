// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Net.HttpRequest.Tests
{
    public partial class HttpRequestResponseSerialization
    {
        public readonly static object[][] EchoServers = System.Net.Test.Common.Configuration.Http.EchoServers;

        [OuterLoop]
        [Theory, MemberData(nameof(EchoServers))]
        public void SerializeDeserialize_Roundtrips(Uri remoteServer)
        {       
            HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
            request.KeepAlive = true;
            request.CookieContainer = new CookieContainer(10);
            HttpWebRequest clone = BinaryFormatterHelpers.Clone(request);
            Assert.NotSame(request, clone);
            
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (HttpWebResponse responseClone = BinaryFormatterHelpers.Clone(response))
                {
                    Assert.NotSame(responseClone, response);
                }
            }
        }
    }
}
 