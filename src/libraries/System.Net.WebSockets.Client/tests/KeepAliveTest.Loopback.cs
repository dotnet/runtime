// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    //
    // Class hierarchy:
    //
    // - KeepAliveTest_Loopback                         → file:KeepAliveTest.Loopback.cs
    //   ├─ [*]KeepAliveTest_SharedHandler_Loopback
    //   ├─ [*]KeepAliveTest_Invoker_Loopback
    //   ├─ [*]KeepAliveTest_HttpClient_Loopback
    //   └─ KeepAliveTest_Http2Loopback
    //      ├─ [*]KeepAliveTest_Invoker_Http2Loopback
    //      └─ [*]KeepAliveTest_HttpClient_Http2Loopback
    //
    // ---
    // `[*]` - concrete runnable test classes
    // `→ file:` - file containing the class and its concrete subclasses

    [SkipOnPlatform(TestPlatforms.Browser, "KeepAlive not supported on browser")]
    public abstract class KeepAliveTest_Loopback(ITestOutputHelper output) : ClientWebSocketTestBase(output)
    {
        #region Loopback-only tests

        [OuterLoop("Uses Task.Delay")]
        [Theory]
        [MemberData(nameof(UseSsl))]
        public Task KeepAlive_LongDelayBetweenSendReceives_Succeeds(bool useSsl)
        {
            var clientMsg = new byte[] { 1, 2, 3, 4, 5, 6 };
            var serverMsg = new byte[] { 42 };
            var clientAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var serverAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var longDelayByServerTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TimeSpan LongDelay = TimeSpan.FromSeconds(10);

            var timeoutCts = new CancellationTokenSource(TimeOutMilliseconds);

            return LoopbackWebSocketServer.RunAsync(
                async uri =>
                {
                    var token = timeoutCts.Token;
                    ClientWebSocket rawCws = await GetConnectedWebSocket(uri,
                        o =>
                        {
                            o.KeepAliveInterval = TimeSpan.FromMilliseconds(100);
                            o.KeepAliveTimeout = TimeSpan.FromSeconds(1);
                        });
                    ReadAheadWebSocket clientWebSocket = new(rawCws);

                    await VerifySendReceiveAsync(clientWebSocket, clientMsg, serverMsg, clientAckTcs, serverAckTcs.Task, token).ConfigureAwait(false);

                    await longDelayByServerTcs.Task.WaitAsync(token).ConfigureAwait(false);
                    Assert.Equal(WebSocketState.Open, clientWebSocket.State);

                    await VerifySendReceiveAsync(clientWebSocket, clientMsg, serverMsg, clientAckTcs, serverAckTcs.Task, token).ConfigureAwait(false);
                    Assert.Equal(WebSocketState.Open, clientWebSocket.State);

                    await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", token).ConfigureAwait(false);
                    Assert.Equal(WebSocketState.Closed, clientWebSocket.State);
                },
                async (sws, token) =>
                {
                    ReadAheadWebSocket serverWebSocket = new(sws);

                    await VerifySendReceiveAsync(serverWebSocket, serverMsg, clientMsg, serverAckTcs, clientAckTcs.Task, token).ConfigureAwait(false);
                    Assert.Equal(WebSocketState.Open, serverWebSocket.State);

                    await Task.Delay(LongDelay);
                    Assert.Equal(WebSocketState.Open, serverWebSocket.State);

                    // recreate already-completed TCS for another round
                    clientAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    serverAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                    longDelayByServerTcs.SetResult();

                    await VerifySendReceiveAsync(serverWebSocket, serverMsg, clientMsg, serverAckTcs, clientAckTcs.Task, token).ConfigureAwait(false);

                    var closeFrame = await serverWebSocket.ReceiveAsync(Array.Empty<byte>(), token).ConfigureAwait(false);
                    Assert.Equal(WebSocketMessageType.Close, closeFrame.MessageType);
                    Assert.Equal(WebSocketState.CloseReceived, serverWebSocket.State);

                    await serverWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", token).ConfigureAwait(false);
                    Assert.Equal(WebSocketState.Closed, serverWebSocket.State);
                },
                new LoopbackWebSocketServer.Options(HttpVersion, useSsl),
                timeoutCts.Token);
        }

        private static async Task VerifySendReceiveAsync(WebSocket ws, byte[] localMsg, byte[] remoteMsg,
            TaskCompletionSource localAckTcs, Task remoteAck, CancellationToken cancellationToken)
        {
            var sendTask = ws.SendAsync(localMsg, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken);

            var recvBuf = new byte[remoteMsg.Length * 2];
            var recvResult = await ws.ReceiveAsync(recvBuf, cancellationToken).ConfigureAwait(false);

            Assert.Equal(WebSocketMessageType.Binary, recvResult.MessageType);
            Assert.Equal(remoteMsg.Length, recvResult.Count);
            Assert.True(recvResult.EndOfMessage);
            Assert.Equal(remoteMsg, recvBuf[..recvResult.Count]);

            localAckTcs.SetResult();

            await sendTask.ConfigureAwait(false);
            await remoteAck.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }

    public abstract class KeepAliveTest_Http2Loopback(ITestOutputHelper output) : KeepAliveTest_Loopback(output)
    {
        internal override Version HttpVersion => Net.HttpVersion.Version20;
    }

    #region Runnable test classes: HTTP/1.1 Loopback

    public sealed class KeepAliveTest_SharedHandler_Loopback(ITestOutputHelper output) : KeepAliveTest_Loopback(output) { }

    public sealed class KeepAliveTest_Invoker_Loopback(ITestOutputHelper output) : KeepAliveTest_Loopback(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class KeepAliveTest_HttpClient_Loopback(ITestOutputHelper output) : KeepAliveTest_Loopback(output)
    {
        protected override bool UseHttpClient => true;
    }

    #endregion

    #region Runnable test classes: HTTP/2 Loopback

    public sealed class KeepAliveTest_Invoker_Http2Loopback(ITestOutputHelper output) : KeepAliveTest_Http2Loopback(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class KeepAliveTest_HttpClient_Http2Loopback(ITestOutputHelper output) : KeepAliveTest_Http2Loopback(output)
    {
        protected override bool UseHttpClient => true;
    }

    #endregion
}
