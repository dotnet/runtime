// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public class HttpClientHandlerTest_Url: HttpClientHandlerTestBase
    {
        public HttpClientHandlerTest_Url(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData("/test%20", "/test%20")]
        [InlineData("/test ", "/test")]
        [InlineData("/test%20?a=1", "/test%20?a=1")]
        [InlineData("/test ?a=1", "/test%20?a=1")]
        public async Task TrimmingTrailingWhiteSpace(string requestPath, string expectedServerPath)
        {
            string serverPath = null;

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    client.BaseAddress = url;

                    var getTask = client.GetAsync(requestPath);

                    var response = await server.HandleRequestAsync();
                    serverPath = response.Path;

                    await getTask;
                }
            });

            Assert.Equal(expectedServerPath, serverPath);
        }
    }
}
