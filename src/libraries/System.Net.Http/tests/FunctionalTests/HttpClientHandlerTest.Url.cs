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

        private async Task<string> RunGetAndReturnServerPathAsync(string path)
        {
            string serverPath = null;

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    client.BaseAddress = url;

                    var getTask = client.GetAsync(path);

                    var response = await server.HandleRequestAsync();
                    serverPath = response.Path;

                    await getTask;
                }
            });

            return serverPath;
        }

        [Fact]
        public async Task TrailingWhitespace_Escaped()
        {
            string path = await RunGetAndReturnServerPathAsync("/test%20");
            Assert.Equal("/test%20", path);
        }

        [Fact]
        public async Task TrailingWhitespace_NotEscaped()
        {
            string path = await RunGetAndReturnServerPathAsync("/test ");
            Assert.Equal("/test", path);
        }
    }
}
