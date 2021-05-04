// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandler_Http11_Cancellation_Test : HttpClientHandler_Cancellation_Test
    {
        protected HttpClientHandler_Http11_Cancellation_Test(ITestOutputHelper output) : base(output) { }

        [OuterLoop]
        [Fact]
        public async Task ConnectTimeout_TimesOutSSLAuth_Throws()
        {
            var releaseServer = new TaskCompletionSource();
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var handler = new SocketsHttpHandler())
                using (var invoker = new HttpMessageInvoker(handler))
                {
                    handler.ConnectTimeout = TimeSpan.FromSeconds(1);

                    var sw = Stopwatch.StartNew();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                        invoker.SendAsync(TestAsync, new HttpRequestMessage(HttpMethod.Get,
                            new UriBuilder(uri) { Scheme = "https" }.ToString()) { Version = UseVersion }, default));
                    sw.Stop();

                    Assert.InRange(sw.ElapsedMilliseconds, 500, 85_000);
                    releaseServer.SetResult();
                }
            }, server => releaseServer.Task); // doesn't establish SSL connection
        }

        [OuterLoop("Incurs significant delay")]
        [Fact]
        public async Task Expect100Continue_WaitsExpectedPeriodOfTimeBeforeSendingContent()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var handler = new SocketsHttpHandler())
                using (var invoker = new HttpMessageInvoker(handler))
                {
                    TimeSpan delay = TimeSpan.FromSeconds(3);
                    handler.Expect100ContinueTimeout = delay;

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
                    await connection.ReadRequestHeaderAsync();
                    await connection.ReadAsync(new byte[1], 0, 1);
                    await connection.SendResponseAsync();
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
