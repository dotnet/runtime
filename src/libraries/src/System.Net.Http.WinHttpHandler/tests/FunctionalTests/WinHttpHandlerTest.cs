// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Tests;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.WinHttpHandlerTests
{
    public class WinHttpHandlerTest
    {
        readonly ITestOutputHelper _output;

        public WinHttpHandlerTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void SendAsync_SimpleGet_Success()
        {
            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            
            // TODO: This is a placeholder until GitHub Issue #2383 gets resolved.
            var response = client.GetAsync(HttpTestServers.RemoteGetServer).Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            _output.WriteLine(responseContent);
        }

        [Fact]
        public async Task GetAsync_Cancel_CancellationTokenPropagates()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            try
            {
                var handler = new SendAsyncWinHttpMockHandler();
                Task <HttpResponseMessage> task = handler.SendAsyncPublic(new HttpRequestMessage(HttpMethod.Post, HttpTestServers.RemoteGetServer), cts.Token);       
                await task;

                Assert.True(false, "Expected TaskCanceledException to be thrown.");
            }
            catch (TaskCanceledException ex)
            {
                Assert.True(cts.Token.IsCancellationRequested,
                    "Expected cancellation requested on original token.");

                Assert.True(ex.CancellationToken.IsCancellationRequested,
                    "Expected cancellation requested on token attached to exception.");
            }
        }
    }
}
