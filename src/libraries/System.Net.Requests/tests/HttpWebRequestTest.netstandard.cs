// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Tests
{
    public partial class HttpWebRequestTest
    {
        [Fact]
        public void HttpWebRequest_Serialize_Fails()
        {
            using (MemoryStream fs = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                var hwr = HttpWebRequest.CreateHttp("http://localhost");

                Assert.Throws<PlatformNotSupportedException>(() => formatter.Serialize(fs, hwr));
            }
        }
    }
}
