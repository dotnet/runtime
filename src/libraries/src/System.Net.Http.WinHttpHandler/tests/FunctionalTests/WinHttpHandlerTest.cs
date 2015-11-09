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

// Can't use "WinHttpHandler.Functional.Tests" in namespace as it won't compile.
// WinHttpHandler is a class and not a namespace and can't be part of namespace paths.
namespace System.Net.Http.WinHttpHandlerFunctional.Tests
{
    // Note:  Disposing the HttpClient object automatically disposes the handler within. So, it is not necessary
    // to separately Dispose (or have a 'using' statement) for the handler.
    public class WinHttpHandlerTest
    {
        // TODO: This is a placeholder until GitHub Issue #2383 gets resolved.
        private const string SlowServer = "http://httpbin.org/drip?numbytes=1&duration=1&delay=40&code=200";
        
        private readonly ITestOutputHelper _output;

        public WinHttpHandlerTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void SendAsync_SimpleGet_Success()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                // TODO: This is a placeholder until GitHub Issue #2383 gets resolved.
                var response = client.GetAsync(HttpTestServers2.RemoteEchoServer).Result;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                _output.WriteLine(responseContent);
            }
        }

        [Fact]
        [OuterLoop]
        public async Task SendAsync_SlowServerAndCancel_ThrowsTaskCanceledException()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                var cts = new CancellationTokenSource();
                Task<HttpResponseMessage> t = client.GetAsync(SlowServer, cts.Token);

                await Task.Delay(500);
                cts.Cancel();
                
                AggregateException ag = Assert.Throws<AggregateException>(() => t.Wait());
                Assert.IsType<TaskCanceledException>(ag.InnerException);
            }
        }        
        
        [Fact]
        [OuterLoop]
        public void SendAsync_SlowServerRespondsAfterDefaultReceiveTimeout_ThrowsHttpRequestException()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                Task<HttpResponseMessage> t = client.GetAsync(SlowServer);
                
                AggregateException ag = Assert.Throws<AggregateException>(() => t.Wait());
                Assert.IsType<HttpRequestException>(ag.InnerException);
            }
        }
    }
}
