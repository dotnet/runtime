// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP connection management behavior is different due to WinRT")]
    public abstract class HttpClientHandler_MaxConnectionsPerServer_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_MaxConnectionsPerServer_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "MaxConnectionsPerServer either returns two or int.MaxValue depending if ctor of HttpClientHandlerTest executed first. Disabling cause of random xunit execution order.")]
        public void Default_ExpectedValue()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Equal(int.MaxValue, handler.MaxConnectionsPerServer);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "NETFX doesn't throw on invalid values")]
        public void Set_InvalidValues_Throws(int invalidValue)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => handler.MaxConnectionsPerServer = invalidValue);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MaxValue - 1)]
        public void Set_ValidValues_Success(int validValue)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                handler.MaxConnectionsPerServer = validValue;
            }
        }

        [Theory]
        [InlineData(1, 5, false)]
        [InlineData(1, 5, true)]
        [InlineData(2, 2, false)]
        [InlineData(2, 2, true)]
        [InlineData(3, 2, false)]
        [InlineData(3, 2, true)]
        [InlineData(3, 5, false)]
        [OuterLoop("Uses external servers")]
        public async Task GetAsync_MaxLimited_ConcurrentCallsStillSucceed(int maxConnections, int numRequests, bool secure)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.MaxConnectionsPerServer = maxConnections;
                await Task.WhenAll(
                    from i in Enumerable.Range(0, numRequests)
                    select client.GetAsync(secure ? Configuration.Http.RemoteEchoServer : Configuration.Http.SecureRemoteEchoServer));
            }
        }

        [OuterLoop("Relies on kicking off GC and waiting for finalizers")]
        [Fact]
        public async Task GetAsync_DontDisposeResponse_EventuallyUnblocksWaiters()
        {
            if (!UseSocketsHttpHandler)
            {
                // Issue #27067. Hang.
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server, uri) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (HttpClient client = CreateHttpClient(handler))
                {
                    handler.MaxConnectionsPerServer = 1;

                    // Let server handle two requests.
                    const string ResponseContent = "abcdefghijklmnopqrstuvwxyz";
                    Task serverTask1 = server.AcceptConnectionSendResponseAndCloseAsync(content: ResponseContent);
                    Task serverTask2 = server.AcceptConnectionSendResponseAndCloseAsync(content: ResponseContent);

                    // Make first request and drop the response, not explicitly disposing of it.
                    void MakeAndDropRequest() => client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead); // separated out to enable GC of response
                    MakeAndDropRequest();

                    // A second request should eventually succeed, once the first one is cleaned up.
                    Task<HttpResponseMessage> secondResponse = client.GetAsync(uri);
                    Assert.True(SpinWait.SpinUntil(() =>
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        return secondResponse.IsCompleted;
                    }, 30 * 1000), "Expected second response to have completed");

                    await new[] { serverTask1, serverTask2, secondResponse }.WhenAllOrAnyFailed();
                }
            });
        }
    }
}
