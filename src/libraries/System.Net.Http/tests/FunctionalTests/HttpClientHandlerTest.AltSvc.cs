// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Net.Test.Common;
using System.Net.Quic;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandler_AltSvc_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_AltSvc_Test(ITestOutputHelper output) : base(output)
        {
        }

        /// <summary>
        /// HTTP/3 tests by default use prenegotiated HTTP/3. To test Alt-Svc upgrades, that must be disabled.
        /// </summary>
        private HttpClient CreateHttpClient(Version version)
        {
            var client = CreateHttpClient();
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            client.DefaultRequestVersion = version;
            return client;
        }

        [Theory]
        [MemberData(nameof(AltSvcHeaderUpgradeVersions))]
        public async Task AltSvc_Header_Upgrade_Success(Version fromVersion, bool overrideHost)
        {
            // The test makes a request to a HTTP/1 or HTTP/2 server first, which supplies an Alt-Svc header pointing to the second server.
            using GenericLoopbackServer firstServer =
                fromVersion.Major switch
                {
                    1 => Http11LoopbackServerFactory.Singleton.CreateServer(new LoopbackServer.Options { UseSsl = true }),
                    2 => Http2LoopbackServer.CreateServer(),
                    _ => throw new Exception("Unknown HTTP version.")
                };

            // The second request is expected to come in on this HTTP/3 server.
            using Http3LoopbackServer secondServer = CreateHttp3LoopbackServer();

            if (!overrideHost)
                Assert.Equal(firstServer.Address.IdnHost, secondServer.Address.IdnHost);

            using HttpClient client = CreateHttpClient(fromVersion);

            Task<HttpResponseMessage> firstResponseTask = client.GetAsync(firstServer.Address);
            Task serverTask = firstServer.AcceptConnectionSendResponseAndCloseAsync(additionalHeaders: new[]
            {
                new HttpHeaderData("Alt-Svc", $"h3=\"{(overrideHost ? secondServer.Address.IdnHost : null)}:{secondServer.Address.Port}\"")
            });

            await new[] { firstResponseTask, serverTask }.WhenAllOrAnyFailed(30_000);

            using HttpResponseMessage firstResponse = firstResponseTask.Result;
            Assert.True(firstResponse.IsSuccessStatusCode);

            await AltSvc_Upgrade_Success(firstServer, secondServer, client);
        }

        public static TheoryData<Version, bool> AltSvcHeaderUpgradeVersions =>
            new TheoryData<Version, bool>
            {
                { HttpVersion.Version11, true },
                { HttpVersion.Version11, false },
                { HttpVersion.Version20, true },
                { HttpVersion.Version20, false }
            };

        [Fact]
        public async Task AltSvc_ConnectionFrame_UpgradeFrom20_Success()
        {
            using Http2LoopbackServer firstServer = Http2LoopbackServer.CreateServer();
            using Http3LoopbackServer secondServer = CreateHttp3LoopbackServer();
            using HttpClient client = CreateHttpClient(HttpVersion.Version20);

            Task<HttpResponseMessage> firstResponseTask = client.GetAsync(firstServer.Address);
            Task serverTask = Task.Run(async () =>
            {
                await using Http2LoopbackConnection connection = await firstServer.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();
                await connection.WriteFrameAsync(new AltSvcFrame($"https://{firstServer.Address.IdnHost}:{firstServer.Address.Port}", $"h3=\"{secondServer.Address.IdnHost}:{secondServer.Address.Port}\"", streamId: 0));
                await connection.SendDefaultResponseAsync(streamId);
            });

            await new[] { firstResponseTask, serverTask }.WhenAllOrAnyFailed(30_000);

            HttpResponseMessage firstResponse = firstResponseTask.Result;
            Assert.True(firstResponse.IsSuccessStatusCode);

            await AltSvc_Upgrade_Success(firstServer, secondServer, client);
        }

        [Fact]
        public async Task AltSvc_UpgradeThenClear_Success()
        {
            using Http2LoopbackServer firstServer = Http2LoopbackServer.CreateServer();
            using Http3LoopbackServer secondServer = CreateHttp3LoopbackServer();
            using HttpClient client = CreateHttpClient(HttpVersion.Version20);

            Task<HttpResponseMessage> responseTask = client.GetAsync(firstServer.Address);
            Task<HttpRequestData> serverTask = firstServer.HandleRequestAsync(headers: new[]
            {
                new HttpHeaderData("Alt-Svc", $"h3=\"{secondServer.Address.IdnHost}:{secondServer.Address.Port}\"")
            });
            await new Task[] { responseTask, serverTask }.WhenAllOrAnyFailed(30_000);
            using HttpResponseMessage response1 = responseTask.Result;
            Assert.True(response1.IsSuccessStatusCode);

            responseTask = client.GetAsync(firstServer.Address);
            serverTask = secondServer.HandleRequestAsync(headers: new[]
            {
                new HttpHeaderData("Alt-Svc", $"clear")
            });
            await new Task[] { responseTask, serverTask }.WhenAllOrAnyFailed(30_000);
            using HttpResponseMessage response2 = responseTask.Result;
            string altUsed = serverTask.Result.GetSingleHeaderValue("Alt-Used");
            Assert.Equal($"{secondServer.Address.IdnHost}:{secondServer.Address.Port}", altUsed);
            Assert.True(response2.IsSuccessStatusCode);

            responseTask = client.GetAsync(firstServer.Address);
            serverTask = firstServer.HandleRequestAsync();
            await new Task[] { responseTask, serverTask }.WhenAllOrAnyFailed(30_000);
            using HttpResponseMessage response3 = responseTask.Result;
            Assert.True(response3.IsSuccessStatusCode);
        }

        [Fact]
        public async Task AltSvc_ResponseFrame_UpgradeFrom20_Success()
        {
            using Http2LoopbackServer firstServer = Http2LoopbackServer.CreateServer();
            using Http3LoopbackServer secondServer = CreateHttp3LoopbackServer();
            using HttpClient client = CreateHttpClient(HttpVersion.Version20);

            Task<HttpResponseMessage> firstResponseTask = client.GetAsync(firstServer.Address);
            Task serverTask = Task.Run(async () =>
            {
                await using Http2LoopbackConnection connection = await firstServer.EstablishConnectionAsync();

                int streamId = await connection.ReadRequestHeaderAsync();
                await connection.SendDefaultResponseHeadersAsync(streamId);
                await connection.WriteFrameAsync(new AltSvcFrame("", $"h3=\"{secondServer.Address.IdnHost}:{secondServer.Address.Port}\"", streamId));
                await connection.SendResponseDataAsync(streamId, Array.Empty<byte>(), true);
            });

            await new[] { firstResponseTask, serverTask }.WhenAllOrAnyFailed(30_000);

            HttpResponseMessage firstResponse = firstResponseTask.Result;
            Assert.True(firstResponse.IsSuccessStatusCode);

            await AltSvc_Upgrade_Success(firstServer, secondServer, client);
        }

        [OuterLoop("Waits for the connection pool maintenance timer to fire.")]
        [Fact]
        public async Task ShouldEvictConnection_Http3AltSvcConnection_ContextReportsAltAuthority()
        {
            using Http2LoopbackServer firstServer = Http2LoopbackServer.CreateServer();
            using Http3LoopbackServer secondServer = CreateHttp3LoopbackServer();

            SocketsHttpConnectionEvictionContext capturedContext = null;
            var callbackInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            using HttpClientHandler handler = CreateHttpClientHandler();
            SocketsHttpHandler socketsHandler = GetUnderlyingSocketsHttpHandler(handler);
            socketsHandler.PooledConnectionLifetime = Timeout.InfiniteTimeSpan;
            socketsHandler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(4);
            socketsHandler.ShouldEvictConnection = (context, _) =>
            {
                // The origin HTTP/2 connection is also evaluated; capture only the HTTP/3 (Alt-Svc) connection.
                if (context.HttpVersion == HttpVersion.Version30)
                {
                    capturedContext ??= context;
                    callbackInvoked.TrySetResult();
                }
                return Task.FromResult(false); // Never evict; we only want to observe the context.
            };

            using HttpClient client = CreateHttpClient(handler);
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            client.DefaultRequestVersion = HttpVersion.Version20;

            // First request over HTTP/2 advertises an HTTP/3 alternative on secondServer, whose authority (port) differs
            // from the origin.
            Task<HttpResponseMessage> firstResponseTask = client.GetAsync(firstServer.Address);
            Task<HttpRequestData> firstServerTask = firstServer.HandleRequestAsync(headers: new[]
            {
                new HttpHeaderData("Alt-Svc", $"h3=\"{secondServer.Address.IdnHost}:{secondServer.Address.Port}\"")
            });
            await new Task[] { firstResponseTask, firstServerTask }.WhenAllOrAnyFailed(TestHelper.PassingTestTimeoutMilliseconds);
            using (HttpResponseMessage firstResponse = firstResponseTask.Result)
            {
                Assert.True(firstResponse.IsSuccessStatusCode);
            }

            // Second request upgrades to HTTP/3 on the alt authority. Handle it at the stream level (no GOAWAY) so the
            // connection stays open, pooled and idle, making it eligible for the eviction maintenance pass.
            Task<HttpResponseMessage> secondResponseTask = client.GetAsync(firstServer.Address);
            await using (GenericLoopbackConnection genericConnection = await secondServer.EstablishGenericConnectionAsync())
            {
                var h3Connection = (Http3LoopbackConnection)genericConnection;
                Http3LoopbackStream stream = await h3Connection.AcceptRequestStreamAsync();
                await stream.HandleRequestAsync();

                using (HttpResponseMessage secondResponse = await secondResponseTask.WaitAsync(TestHelper.PassingTestTimeout))
                {
                    Assert.True(secondResponse.IsSuccessStatusCode);
                }

                await callbackInvoked.Task.WaitAsync(TestHelper.PassingTestTimeout);

                Assert.NotNull(capturedContext);
                // The connection targets the Alt-Svc authority, so the eviction context must report that authority
                // (consistent with RemoteEndPoint), not the pool's origin authority. The alt authority uses a different port.
                Assert.Equal(secondServer.Address.IdnHost, capturedContext.DnsEndPoint.Host);
                Assert.Equal(secondServer.Address.Port, capturedContext.DnsEndPoint.Port);
                Assert.NotEqual(firstServer.Address.Port, capturedContext.DnsEndPoint.Port);

                // HTTP/3 always reports the remote endpoint (from the QUIC connection). It targets the alt authority, so
                // its port matches the reported DnsEndPoint.
                IPEndPoint remoteEndPoint = Assert.IsType<IPEndPoint>(capturedContext.RemoteEndPoint);
                Assert.Equal(secondServer.Address.Port, remoteEndPoint.Port);
            }
        }

        [OuterLoop("Waits for the connection pool maintenance timer to fire.")]
        [Fact]
        public async Task ShouldEvictConnection_Http3AltSvcChangesAfterConnect_ContextReportsOriginalEndpoint()
        {
            using Http2LoopbackServer firstServer = Http2LoopbackServer.CreateServer();
            using Http3LoopbackServer secondServer = CreateHttp3LoopbackServer();
            // The Alt-Svc will later be changed to point at this authority; we never actually connect to it.
            using Http3LoopbackServer thirdServer = CreateHttp3LoopbackServer();

            SocketsHttpConnectionEvictionContext capturedContext = null;
            var callbackInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            using HttpClientHandler handler = CreateHttpClientHandler();
            SocketsHttpHandler socketsHandler = GetUnderlyingSocketsHttpHandler(handler);
            socketsHandler.PooledConnectionLifetime = Timeout.InfiniteTimeSpan;
            socketsHandler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(4);
            socketsHandler.ShouldEvictConnection = (context, _) =>
            {
                if (context.HttpVersion == HttpVersion.Version30)
                {
                    capturedContext ??= context;
                    callbackInvoked.TrySetResult();
                }
                return Task.FromResult(false);
            };

            using HttpClient client = CreateHttpClient(handler);
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            client.DefaultRequestVersion = HttpVersion.Version20;

            // First request over HTTP/2 advertises the HTTP/3 alternative on secondServer.
            Task<HttpResponseMessage> firstResponseTask = client.GetAsync(firstServer.Address);
            Task<HttpRequestData> firstServerTask = firstServer.HandleRequestAsync(headers: new[]
            {
                new HttpHeaderData("Alt-Svc", $"h3=\"{secondServer.Address.IdnHost}:{secondServer.Address.Port}\"")
            });
            await new Task[] { firstResponseTask, firstServerTask }.WhenAllOrAnyFailed(TestHelper.PassingTestTimeoutMilliseconds);
            using (HttpResponseMessage firstResponse = firstResponseTask.Result)
            {
                Assert.True(firstResponse.IsSuccessStatusCode);
            }

            // Second request upgrades to HTTP/3 on secondServer. Its response changes the advertised Alt-Svc to point at
            // thirdServer (a different authority/port). The connection stays open, pooled and idle.
            Task<HttpResponseMessage> secondResponseTask = client.GetAsync(firstServer.Address);
            await using (GenericLoopbackConnection genericConnection = await secondServer.EstablishGenericConnectionAsync())
            {
                var h3Connection = (Http3LoopbackConnection)genericConnection;
                Http3LoopbackStream stream = await h3Connection.AcceptRequestStreamAsync();
                await stream.HandleRequestAsync(headers: new[]
                {
                    new HttpHeaderData("Alt-Svc", $"h3=\"{thirdServer.Address.IdnHost}:{thirdServer.Address.Port}\"")
                });

                using (HttpResponseMessage secondResponse = await secondResponseTask.WaitAsync(TestHelper.PassingTestTimeout))
                {
                    Assert.True(secondResponse.IsSuccessStatusCode);
                }

                await callbackInvoked.Task.WaitAsync(TestHelper.PassingTestTimeout);

                Assert.NotNull(capturedContext);
                // Even though Alt-Svc now points at thirdServer, the existing connection still reports the endpoint it
                // was actually established to (secondServer) - captured at connection setup, not the pool's current alt
                // authority.
                Assert.Equal(secondServer.Address.IdnHost, capturedContext.DnsEndPoint.Host);
                Assert.Equal(secondServer.Address.Port, capturedContext.DnsEndPoint.Port);
                Assert.NotEqual(thirdServer.Address.Port, capturedContext.DnsEndPoint.Port);
            }
        }

        private async Task AltSvc_Upgrade_Success(GenericLoopbackServer firstServer, Http3LoopbackServer secondServer, HttpClient client)
        {
            Task<HttpResponseMessage> secondResponseTask = client.GetAsync(firstServer.Address);
            Task<HttpRequestData> secondRequestTask = secondServer.AcceptConnectionSendResponseAndCloseAsync();

            await new[] { (Task)secondResponseTask, secondRequestTask }.WhenAllOrAnyFailed(30_000);

            HttpRequestData secondRequest = secondRequestTask.Result;
            using HttpResponseMessage secondResponse = secondResponseTask.Result;

            string altUsed = secondRequest.GetSingleHeaderValue("Alt-Used");
            Assert.Equal($"{secondServer.Address.IdnHost}:{secondServer.Address.Port}", altUsed);
            Assert.True(secondResponse.IsSuccessStatusCode);
        }
    }
}
