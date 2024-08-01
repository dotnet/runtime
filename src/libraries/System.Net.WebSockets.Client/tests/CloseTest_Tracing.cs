// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    [Collection(nameof(TracingTestCollection))]
    public sealed class InvokerCloseTestDbg : CloseTestDbg
    {
        public InvokerCloseTestDbg(ITestOutputHelper output) : base(output) { }

        protected override bool UseCustomInvoker => true;
    }

    [Collection(nameof(TracingTestCollection))]
    public sealed class HttpClientCloseTestDbg : CloseTestDbg
    {
        public HttpClientCloseTestDbg(ITestOutputHelper output) : base(output) { }

        protected override bool UseHttpClient => true;
    }

    [Collection(nameof(TracingTestCollection))]
    public class CloseTestDbg : ClientWebSocketTestBase
    {
        public CloseTestDbg(ITestOutputHelper output) : base(output) { }

        [ConditionalFact(nameof(WebSocketsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/54153", TestPlatforms.Browser)]
        public async Task CloseAsync_CancelableEvenWhenPendingReceive_Throws()
        {
            Console.WriteLine(Environment.NewLine + "===== " + this.GetType().FullName + " =====" + Environment.NewLine);

            using var testEventListener = TracingTestCollection.CreateTestEventListener(this);
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var stopwatch = Stopwatch.StartNew();

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                try
                {
                    TracingTestCollection.Trace(this, "Test client started");

                    using var cws = new ClientWebSocket();
                    using var testTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                    await ConnectAsync(cws, uri, testTimeoutCts.Token);

                    TracingTestCollection.Trace(this, "ClientWebSocket connected");
                    TracingTestCollection.TraceUnderlyingWebSocket(this, cws);

                    Task receiveTask = cws.ReceiveAsync(new byte[1], testTimeoutCts.Token);
                    TracingTestCollection.Trace(this, "Started ReceiveAsync");

                    var cancelCloseCts = new CancellationTokenSource();
                    var closeException = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                    {
                        Task t = cws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancelCloseCts.Token);
                        TracingTestCollection.Trace(this, "Started CloseAsync");
                        cancelCloseCts.CancelAfter(100);

                        TracingTestCollection.Trace(this, "Before await");
                        await t;
                    });
                    TracingTestCollection.Trace(this, "After await");

                    Assert.Equal(cancelCloseCts.Token, closeException.CancellationToken);
                    TracingTestCollection.Trace(this, "closeOCE: " + closeException.Message);
                    TracingTestCollection.Trace(this, "cancelCloseCts.Token.IsCancellationRequested=" + cancelCloseCts.Token.IsCancellationRequested);
                    TracingTestCollection.Trace(this, "testTimeoutCts.Token.IsCancellationRequested=" + testTimeoutCts.Token.IsCancellationRequested);

                    TracingTestCollection.Trace(this, "Before await");
                    var receiveOCE = await Assert.ThrowsAsync<OperationCanceledException>(() => receiveTask);
                    TracingTestCollection.Trace(this, "After await");


                    Assert.Equal(nameof(WebSocketState.Aborted), receiveOCE.Message);
                    var ioe = Assert.IsType<System.IO.IOException>(receiveOCE.InnerException);
                    var se = Assert.IsType<System.Net.Sockets.SocketException>(ioe.InnerException);
                    Assert.Equal(System.Net.Sockets.SocketError.OperationAborted, se.SocketErrorCode);


                    TracingTestCollection.Trace(this, "receiveOCE: " + receiveOCE.Message);
                    TracingTestCollection.Trace(this, "testTimeoutCts.Token.IsCancellationRequested=" + testTimeoutCts.Token.IsCancellationRequested);

                }
                catch (Exception e)
                {
                    TracingTestCollection.Trace(this, "Client exception: " + e.Message);
                    throw;
                }
                finally
                {
                    tcs.SetResult();
                    TracingTestCollection.Trace(this, "Test client finished after " + stopwatch.Elapsed);
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                TracingTestCollection.Trace(this, "Test server started");

                Dictionary<string, string> headers = await LoopbackHelper.WebSocketHandshakeAsync(connection);
                Assert.NotNull(headers);

                await tcs.Task;

                TracingTestCollection.Trace(this, "Test server finished");

            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }
    }
}
