// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    [ConditionalClass(typeof(SocketsHttpHandler), nameof(SocketsHttpHandler.IsSupported))]
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

        [Fact]
        public async Task RequestsCanceled_NoConnectionAttemptForCanceledRequests()
        {
            if (UseVersion == HttpVersion.Version30)
            {
                // HTTP3 does not support ConnectCallback
                return;
            }

            bool seenRequest1 = false;
            bool seenRequest2 = false;
            bool seenRequest3 = false;

            var uri = new Uri("https://example.com");
            HttpRequestMessage request1 = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);
            HttpRequestMessage request2 = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);
            HttpRequestMessage request3 = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);

            TaskCompletionSource connectCallbackEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource connectCallbackGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

            using HttpClientHandler handler = CreateHttpClientHandler();
            handler.MaxConnectionsPerServer = 1;
            GetUnderlyingSocketsHttpHandler(handler).ConnectCallback = async (context, cancellation) =>
            {
                if (context.InitialRequestMessage == request1) seenRequest1 = true;
                if (context.InitialRequestMessage == request2) seenRequest2 = true;
                if (context.InitialRequestMessage == request3) seenRequest3 = true;

                connectCallbackEntered.TrySetResult();

                await connectCallbackGate.Task.WaitAsync(TestHelper.PassingTestTimeout);

                throw new Exception("No connection");
            };
            using HttpClient client = CreateHttpClient(handler);

            Task request1Task = client.SendAsync(TestAsync, request1);
            await connectCallbackEntered.Task.WaitAsync(TestHelper.PassingTestTimeout);
            Assert.True(seenRequest1);

            using var request2Cts = new CancellationTokenSource();
            Task request2Task = client.SendAsync(TestAsync, request2, request2Cts.Token);
            Assert.False(seenRequest2);

            Task request3Task = client.SendAsync(TestAsync, request3);
            Assert.False(seenRequest2);
            Assert.False(seenRequest3);

            request2Cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(() => request2Task).WaitAsync(TestHelper.PassingTestTimeout);
            Assert.False(seenRequest2);
            Assert.False(seenRequest3);

            connectCallbackGate.SetResult();

            await Assert.ThrowsAsync<HttpRequestException>(() => request1Task).WaitAsync(TestHelper.PassingTestTimeout);
            await Assert.ThrowsAsync<HttpRequestException>(() => request3Task).WaitAsync(TestHelper.PassingTestTimeout);

            Assert.False(seenRequest2);
            Assert.True(seenRequest3);
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
