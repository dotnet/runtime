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
                    Trace("Test client started");

                    using var cws = new ClientWebSocket();
                    using var testTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    using var testTimeoutCtsRegistration = testTimeoutCts.Token.Register(() =>
                    {
                        Trace("Test timed out, canceling...");
                    });

                    await ConnectAsync(cws, uri, testTimeoutCts.Token);

                    Trace("ClientWebSocket connected");
                    TracingTestCollection.TraceUnderlyingWebSocket(this, cws);

                    Task receiveTask = cws.ReceiveAsync(new byte[1], testTimeoutCts.Token);
                    Trace("Started ReceiveAsync");

                    var cancelCloseCts = new CancellationTokenSource();
                    using var cancelCloseCtsRegistration = cancelCloseCts.Token.Register(() =>
                    {
                        Trace("Canceling CloseAsync");
                    });

                    var closeException = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                    {
                        Task t = cws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancelCloseCts.Token);
                        Trace("Started CloseAsync");
                        cancelCloseCts.Cancel();

                        Trace("Before await CloseAsync");
                        try
                        {
                            await t;
                        }
                        catch (Exception ex)
                        {
                            Trace("CloseAsync exception: " + ex.Message);
                            throw;
                        }
                        finally
                        {
                            Trace("After await CloseAsync");
                        }
                    });
                    Trace("After await Assert.ThrowsAsync");

                    Assert.Equal(cancelCloseCts.Token, closeException.CancellationToken);
                    Trace("closeOCE: " + closeException.Message);
                    Trace("cancelCloseCts.Token.IsCancellationRequested=" + cancelCloseCts.Token.IsCancellationRequested);
                    Trace("testTimeoutCts.Token.IsCancellationRequested=" + testTimeoutCts.Token.IsCancellationRequested);

                    Trace("Before await");
                    var receiveOCE = await Assert.ThrowsAsync<OperationCanceledException>(() => receiveTask);
                    Trace("After await");


                    Assert.Equal(nameof(WebSocketState.Aborted), receiveOCE.Message);
                    var ioe = Assert.IsType<System.IO.IOException>(receiveOCE.InnerException);
                    var se = Assert.IsType<System.Net.Sockets.SocketException>(ioe.InnerException);
                    Assert.Equal(System.Net.Sockets.SocketError.OperationAborted, se.SocketErrorCode);


                    Trace("receiveOCE: " + receiveOCE.Message);
                    Trace("testTimeoutCts.Token.IsCancellationRequested=" + testTimeoutCts.Token.IsCancellationRequested);

                }
                catch (Exception e)
                {
                    Trace("Client exception: " + e.Message);
                    throw;
                }
                finally
                {
                    tcs.SetResult();
                    Trace("Test client finished after " + stopwatch.Elapsed);
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Trace("Test server started");

                Dictionary<string, string> headers = await LoopbackHelper.WebSocketHandshakeAsync(connection);
                Assert.NotNull(headers);

                await tcs.Task;

                Trace("Test server finished");

            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }

        //internal void Trace(string message) => TracingTestCollection.Trace(this, message);
        internal void Trace(string _) {}
    }
}
