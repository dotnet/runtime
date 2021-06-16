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
    public abstract class SocketsHttpHandler_Cancellation_Test : HttpClientHandler_Cancellation_Test
    {
        protected SocketsHttpHandler_Cancellation_Test(ITestOutputHelper output) : base(output) { }

        private async Task ValidateConnectTimeout(HttpMessageInvoker invoker, Uri uri, int minElapsed, int maxElapsed)
        {
            var sw = Stopwatch.StartNew();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                invoker.SendAsync(TestAsync, new HttpRequestMessage(HttpMethod.Get, uri) { Version = UseVersion }, default));
            sw.Stop();

            Assert.InRange(sw.ElapsedMilliseconds, minElapsed, maxElapsed);
        }

        [OuterLoop]
        [Fact]
        public async Task ConnectTimeout_TimesOutSSLAuth_Throws()
        {
            var releaseServer = new TaskCompletionSource();
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (var handler = new SocketsHttpHandler())
                using (var invoker = new HttpMessageInvoker(handler))
                {
                    handler.ConnectTimeout = TimeSpan.FromSeconds(1);

                    await ValidateConnectTimeout(invoker, new UriBuilder(uri) { Scheme = "https" }.Uri, 500, 85_000);

                    releaseServer.SetResult();
                }
            }, server => releaseServer.Task); // doesn't establish SSL connection
        }

        [OuterLoop]
        [Fact]
        public async Task ConnectTimeout_ConnectCallbackTimesOut_Throws()
        {
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
            }, server => Task.CompletedTask); // doesn't actually connect to server
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
