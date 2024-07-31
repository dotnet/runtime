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

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                try
                {
                    TracingTestCollection.Trace(this, "Test client started");

                    using var cws = new ClientWebSocket();
                    using var testTimeoutCts = new CancellationTokenSource(TimeOutMilliseconds);

                    await ConnectAsync(cws, uri, testTimeoutCts.Token);

                    TracingTestCollection.Trace(this, "ClientWebSocket connected");
                    TracingTestCollection.TraceUnderlyingWebSocket(this, cws);

                    Task receiveTask = cws.ReceiveAsync(new byte[1], testTimeoutCts.Token);

                    var cancelCloseCts = new CancellationTokenSource();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    {
                        Task t = cws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancelCloseCts.Token);
                        cancelCloseCts.Cancel();
                        await t;
                    });
                    Assert.False(testTimeoutCts.Token.IsCancellationRequested);

                    var receiveOCE = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => receiveTask);
                    Assert.False(testTimeoutCts.Token.IsCancellationRequested);

                }
                finally
                {
                    tcs.SetResult();
                    TracingTestCollection.Trace(this, "Test client finished");
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
