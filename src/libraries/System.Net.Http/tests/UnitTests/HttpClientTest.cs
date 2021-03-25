// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class HttpClientTest
    {
        [Fact]
        public async Task GetAsync_Succeeds()
        {
            using HttpClient client = new HttpClient();
            NotImplementedException exception = await Assert.ThrowsAsync<NotImplementedException>(() => client.GetAsync(Configuration.Http.RemoteEmptyContentServer));
            Assert.Equal(HttpClientHandler.Message, exception.Message);
        }
    }
}
