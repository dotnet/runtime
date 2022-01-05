// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class SocketsHttpHandler_Cancellation_Test : HttpClientHandler_Cancellation_Test
    {
        protected SocketsHttpHandler_Cancellation_Test(ITestOutputHelper output) : base(output) { }

        private async Task ValidateConnectTimeout(HttpMessageInvoker invoker, Uri uri, int minElapsed, int maxElapsed)
        {
            var sw = Stopwatch.StartNew();
            var oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                invoker.SendAsync(TestAsync, new HttpRequestMessage(HttpMethod.Get, uri) { Version = UseVersion }, default));
            sw.Stop();

            Assert.IsType<TimeoutException>(oce.InnerException);
            Assert.InRange(sw.ElapsedMilliseconds, minElapsed, maxElapsed);
        }

        [OuterLoop]
        [Fact]
        public async Task ConnectTimeout_TimesOutSSLAuth_Throws()
        {
            if (UseVersion == HttpVersion.Version30)
            {
                return;
            }

            var releaseServer = new TaskCompletionSource();
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (var handler = CreateHttpClientHandler())
                using (var invoker = new HttpMessageInvoker(handler))
                {
                    var socketsHandler = GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.ConnectTimeout = TimeSpan.FromSeconds(1);

                    await ValidateConnectTimeout(invoker, new UriBuilder(uri) { Scheme = "https" }.Uri, 500, 85_000);

                    releaseServer.SetResult();
                }
            }, server => releaseServer.Task); // doesn't establish SSL connection
        }

        [OuterLoop]
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectTimeout_ConnectCallbackTimesOut_Throws(bool useSsl)
        {
            if (UseVersion == HttpVersion.Version30)
            {
                // HTTP3 does not support ConnectCallback
                return;
            }

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (var handler = CreateHttpClientHandler())
                using (var invoker = new HttpMessageInvoker(handler))
                {
                    var socketsHandler = GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.ConnectTimeout = TimeSpan.FromSeconds(1);
                    socketsHandler.ConnectCallback = async (context, token) => { await Task.Delay(-1, token); return null; };

                    await ValidateConnectTimeout(invoker, uri, 500, 85_000);
                }
            }, server => Task.CompletedTask, // doesn't actually connect to server
            options: new GenericLoopbackOptions() { UseSsl = useSsl });
        }

        [OuterLoop]
        [Fact]
        public async Task ConnectTimeout_PlaintextStreamFilterTimesOut_Throws()
        {
            if (UseVersion == HttpVersion.Version30)
            {
                // HTTP3 does not support PlaintextStreamFilter
                return;
            }

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (var handler = CreateHttpClientHandler())
                using (var invoker = new HttpMessageInvoker(handler))
                {
                    var socketsHandler = GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.ConnectTimeout = TimeSpan.FromSeconds(1);
                    socketsHandler.ConnectCallback = (context, token) => new ValueTask<Stream>(new MemoryStream());
                    socketsHandler.PlaintextStreamFilter = async (context, token) => { await Task.Delay(-1, token); return null; };

                    await ValidateConnectTimeout(invoker, uri, 500, 85_000);
                }
            }, server => Task.CompletedTask, // doesn't actually connect to server
            options: new GenericLoopbackOptions() { UseSsl = false });
        }

        [OuterLoop]
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectionFailure_AfterInitialRequestCancelled_SecondRequestSucceedsOnNewConnection(bool useSsl)
        {
            if (UseVersion == HttpVersion.Version30)
            {
                // HTTP3 does not support ConnectCallback
                return;
            }

            if (!TestAsync)
            {
                // Test relies on ordering of async operations, so we can't test the sync case
                return;
            }

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                int connectCount = 0;

                TaskCompletionSource tcsFirstConnectionInitiated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                TaskCompletionSource tcsFirstRequestCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                using (var handler = CreateHttpClientHandler())
                using (var client = CreateHttpClient(handler))
                {
                    handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
                    var socketsHandler = GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.ConnectCallback = async (context, token) =>
                    {
                        // Note we force serialization of connection creation by waiting on tcsFirstConnectionInitiated below,
                        // so we don't need to worry about concurrent access to connectCount.
                        bool isFirstConnection = connectCount == 0;
                        connectCount++;

                        Assert.True(connectCount <= 2);

                        if (isFirstConnection)
                        {
                            tcsFirstConnectionInitiated.SetResult();
                        }
                        else
                        {
                            Assert.True(tcsFirstConnectionInitiated.Task.IsCompletedSuccessfully);
                        }

                        // Wait until first request is cancelled and has completed
                        await tcsFirstRequestCanceled.Task;

                        if (isFirstConnection)
                        {
                            // Fail the first connection attempt
                            throw new Exception("Failing first connection");
                        }
                        else
                        {
                            // Succeed the second connection attempt
                            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                            await socket.ConnectAsync(context.DnsEndPoint, token);
                            return new NetworkStream(socket, ownsSocket: true);
                        }
                    };

                    using CancellationTokenSource cts = new CancellationTokenSource();
                    Task<HttpResponseMessage> t1 = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri) { Version = UseVersion, VersionPolicy = HttpVersionPolicy.RequestVersionExact }, cts.Token);

                    // Wait for the connection attempt to be initiated before we send the second request, to avoid races in connection creation
                    await tcsFirstConnectionInitiated.Task;
                    Task<HttpResponseMessage> t2 = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri) { Version = UseVersion, VersionPolicy = HttpVersionPolicy.RequestVersionExact }, default);

                    // Cancel the first message and wait for it to complete
                    cts.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t1);

                    // Signal connections to proceed
                    tcsFirstRequestCanceled.SetResult();

                    // Second request should succeed, even though the first connection failed
                    HttpResponseMessage resp2 = await t2;
                    Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
                    Assert.Equal("Hello world", await resp2.Content.ReadAsStringAsync());
                }
            }, async server =>
            {
                await server.AcceptConnectionSendResponseAndCloseAsync(content: "Hello world");
            },
            options: new GenericLoopbackOptions() { UseSsl = useSsl });
        }

        [OuterLoop("Incurs significant delay")]
        [Fact]
        public async Task Expect100Continue_WaitsExpectedPeriodOfTimeBeforeSendingContent()
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (var handler = CreateHttpClientHandler())
                using (var invoker = new HttpMessageInvoker(handler))
                {
                    var socketsHandler = GetUnderlyingSocketsHttpHandler(handler);
                    TimeSpan delay = TimeSpan.FromSeconds(3);
                    socketsHandler.Expect100ContinueTimeout = delay;

                    var tcs = new TaskCompletionSource<bool>();
                    var content = new SetTcsContent(new MemoryStream(new byte[1]), tcs);
                    var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content, Version = UseVersion };
                    request.Headers.ExpectContinue = true;

                    long start = Environment.TickCount64;
                    (await invoker.SendAsync(TestAsync, request, default)).Dispose();
                    long elapsed = content.Ticks - start;
                    Assert.True(elapsed >= delay.TotalMilliseconds);
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.HandleRequestAsync();
                });
            });
        }

        private sealed class SetTcsContent : StreamContent
        {
            private readonly TaskCompletionSource<bool> _tcs;
            public long Ticks;

            public SetTcsContent(Stream stream, TaskCompletionSource<bool> tcs) : base(stream) => _tcs = tcs;

            protected override void SerializeToStream(Stream stream, TransportContext context, CancellationToken cancellationToken) =>
                SerializeToStreamAsync(stream, context).GetAwaiter().GetResult();

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                Ticks = Environment.TickCount64;
                _tcs.SetResult(true);
                return base.SerializeToStreamAsync(stream, context);
            }
        }
    }
}
