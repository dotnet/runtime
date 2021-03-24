// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Tests
{
    public class HttpClientTest
    {
        [Fact]
        public async Task GetAsync_Succeeds()
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(Configuration.Http.RemoteServer);
        }
    }
}
