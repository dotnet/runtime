// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets are not supported on browser")]
    public abstract class CancelTest_Loopback(ITestOutputHelper output) : CancelTestBase(output)
    {
        #region Common (Echo Server) tests

        [Theory, MemberData(nameof(UseSsl))]
        public Task ConnectAsync_Cancel_ThrowsCancellationException(bool useSsl) => RunConnectAsync_Cancel_ThrowsCancellationException(useSsl);

        // Uses a dedicated (non-echo) loopback run instead of RunEchoAsync: the server signals once it has
        // accepted the connection and received the opening handshake request, and only then does the client
        // cancels while the server withholds the handshake response indefinitely. This avoids racing a fixed cancellation
        // delay (e.g. 100ms) against JIT-stress-induced scheduling jitter, which could previously let the
        // token fire before the client ever reached/was accepted by the server, leaving the server blocked
        // in Accept until the enclosing LoopbackWebSocketServer timeout fired a TimeoutException instead of
        // the expected OperationCanceledException.
        private async Task RunConnectAsync_Cancel_ThrowsCancellationException(bool useSsl)
        {
            var handshakeReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var timeoutCts = new CancellationTokenSource(TimeOutMilliseconds);

            var options = new LoopbackWebSocketServer.Options(HttpVersion, useSsl)
            {
                SkipServerHandshakeResponse = true,
                IgnoreServerErrors = true,
                AbortServerOnClientExit = true
            };

            await LoopbackWebSocketServer.RunAsync(
                server => RunClientAsync(server, handshakeReceived.Task, timeoutCts.Token),
                async (WebSocketRequestData requestData, CancellationToken token) =>
                {
                    handshakeReceived.TrySetResult();

                    // Never respond; wait until the client cancels (or the test times out).
                    await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
                },
                options,
                timeoutCts.Token);

            async Task RunClientAsync(Uri server, Task handshakeReceivedTask, CancellationToken timeoutToken)
            {
                using var cws = new ClientWebSocket();
                using var cts = new CancellationTokenSource();

                Task connectTask = ConnectAsync(cws, server, cts.Token);

                Task completedTask = await Task.WhenAny(connectTask, handshakeReceivedTask)
                    .WaitAsync(timeoutToken)
                    .ConfigureAwait(false);

                if (completedTask == connectTask)
                {
                    await connectTask.ConfigureAwait(false);
                    Assert.Fail("ConnectAsync completed before the server received the handshake.");
                }

                cts.Cancel();

                var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connectTask);
                Assert.True(WebSocketState.Closed == cws.State, $"Actual {cws.State} when {ex}");
            }
        }

        [Theory, MemberData(nameof(UseSsl))]
        public Task SendAsync_Cancel_Success(bool useSsl) => RunEchoAsync(
            RunClient_SendAsync_Cancel_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task ReceiveAsync_Cancel_Success(bool useSsl) => RunEchoAsync(
            RunClient_ReceiveAsync_Cancel_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseAsync_Cancel_Success(bool useSsl) => RunEchoAsync(
            RunClient_CloseAsync_Cancel_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task CloseOutputAsync_Cancel_Success(bool useSsl) => RunEchoAsync(
            RunClient_CloseOutputAsync_Cancel_Success, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task ReceiveAsync_CancelThenReceive_ThrowsOperationCanceledException(bool useSsl) => RunEchoAsync(
            RunClient_ReceiveAsync_CancelThenReceive_ThrowsOperationCanceledException, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task ReceiveAsync_ReceiveThenCancel_ThrowsOperationCanceledException(bool useSsl) => RunEchoAsync(
            RunClient_ReceiveAsync_ReceiveThenCancel_ThrowsOperationCanceledException, useSsl);

        [Theory, MemberData(nameof(UseSsl))]
        public Task ReceiveAsync_AfterCancellationDoReceiveAsync_ThrowsWebSocketException(bool useSsl) => RunEchoAsync(
            RunClient_ReceiveAsync_AfterCancellationDoReceiveAsync_ThrowsWebSocketException, useSsl);

        #endregion
    }

    public abstract class CancelTest_Http2Loopback(ITestOutputHelper output) : CancelTest_Loopback(output)
    {
        internal override Version HttpVersion => Net.HttpVersion.Version20;
    }

    #region Runnable test classes: HTTP/1.1 Loopback

    public sealed class CancelTest_SharedHandler_Loopback(ITestOutputHelper output) : CancelTest_Loopback(output) { }

    public sealed class CancelTest_Invoker_Loopback(ITestOutputHelper output) : CancelTest_Loopback(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class CancelTest_HttpClient_Loopback(ITestOutputHelper output) : CancelTest_Loopback(output)
    {
        protected override bool UseHttpClient => true;
    }

    #endregion

    #region Runnable test classes: HTTP/2 Loopback

    public sealed class CancelTest_Invoker_Http2Loopback(ITestOutputHelper output) : CancelTest_Http2Loopback(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class CancelTest_HttpClient_Http2Loopback(ITestOutputHelper output) : CancelTest_Http2Loopback(output)
    {
        protected override bool UseHttpClient => true;
    }

    #endregion
}
