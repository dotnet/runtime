// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "KeepAlive not supported on browser")]
    public class KeepAliveTest_Loopback : ClientWebSocketTestBase
    {
        public KeepAliveTest_Loopback(ITestOutputHelper output) : base(output) { }

        protected virtual Version HttpVersion => Net.HttpVersion.Version11;

        public static readonly object[][] UseSsl_MemberData = PlatformDetection.SupportsAlpn
            ? new[] { new object[] { false }, new object[] { true } }
            : new[] { new object[] { false } };

        [Theory]
        [MemberData(nameof(UseSsl_MemberData))]
        public Task KeepAlive_LongDelayBetweenSendReceives_Succeeds(bool useSsl)
        {
            var clientMsg = new byte[] { 1, 2, 3, 4, 5, 6 };
            var serverMsg = new byte[] { 42 };
            var clientAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var serverAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var longDelayByServerTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TimeSpan LongDelay = TimeSpan.FromSeconds(10);

            var timeoutCts = new CancellationTokenSource(TimeOutMilliseconds);

            var options = new LoopbackWebSocketServer.Options(HttpVersion, useSsl, GetInvoker())
            {
                DisposeServerWebSocket = true,
                DisposeClientWebSocket = true,
                ConfigureClientOptions = clientOptions =>
                {
                    //clientOptions.KeepAliveInterval = TimeSpan.FromSeconds(100);
                    //clientOptions.KeepAliveTimeout = TimeSpan.FromSeconds(500);
                },
            };

            return LoopbackWebSocketServer.RunAsync(
                async (clientWebSocket, token) =>
                {
                    await VerifySendReceiveAsync(clientWebSocket, clientMsg, serverMsg, clientAckTcs, serverAckTcs.Task, token);

                    // We need to always have a read task active to keep processing pongs
                    var outstandingReadTask = clientWebSocket.ReceiveAsync(Array.Empty<byte>(), token);

                    await longDelayByServerTcs.Task.WaitAsync(token);

                    var result = await outstandingReadTask;
                    Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                    Assert.False(result.EndOfMessage);
                    Assert.Equal(0, result.Count); // we issued a zero byte read, just to wait for data to become available

                    Assert.Equal(WebSocketState.Open, clientWebSocket.State);

                    await VerifySendReceiveAsync(clientWebSocket, clientMsg, serverMsg, clientAckTcs, serverAckTcs.Task, token);

                    Assert.Equal(WebSocketState.Open, clientWebSocket.State);

                    await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", token);

                    Assert.Equal(WebSocketState.Closed, clientWebSocket.State);
                },
                async (serverWebSocket, token) =>
                {
                    await VerifySendReceiveAsync(serverWebSocket, serverMsg, clientMsg, serverAckTcs, clientAckTcs.Task, token);

                    Assert.Equal(WebSocketState.Open, serverWebSocket.State);

                    await Task.Delay(LongDelay);

                    Assert.Equal(WebSocketState.Open, serverWebSocket.State);

                    // recreate already-completed TCS for another round
                    clientAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    serverAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                    longDelayByServerTcs.SetResult();

                    await VerifySendReceiveAsync(serverWebSocket, serverMsg, clientMsg, serverAckTcs, clientAckTcs.Task, token);

                    var closeFrame = await serverWebSocket.ReceiveAsync(Array.Empty<byte>(), token);
                    Assert.Equal(WebSocketMessageType.Close, closeFrame.MessageType);
                    Assert.Equal(WebSocketState.CloseReceived, serverWebSocket.State);

                    await serverWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", token);
                    Assert.Equal(WebSocketState.Closed, serverWebSocket.State);
                },
                options,
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

    }
}
