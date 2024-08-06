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
    public abstract class KeepAliveTest_Loopback : ClientWebSocketTestBase
    {
        public KeepAliveTest_Loopback(ITestOutputHelper output, TracingTestCollection c) : base(output, c) { }

        protected virtual Version HttpVersion => Net.HttpVersion.Version11;

        [ActiveIssue("")] // TODO
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
                    clientOptions.KeepAliveInterval = TimeSpan.FromMilliseconds(100);
                    clientOptions.KeepAliveTimeout = TimeSpan.FromSeconds(1);
                },
                //DebugLog = Trace
            };

            return LoopbackWebSocketServer.RunAsync(
                async (clientWebSocket, token) =>
                {
                    Trace("VerifySendReceiveAsync #1 starting on client");
                    await VerifySendReceiveAsync(clientWebSocket, clientMsg, serverMsg, clientAckTcs, serverAckTcs.Task, token);
                    Trace("VerifySendReceiveAsync #1 completed on client");

                    // We need to always have a read task active to keep processing pongs
                    var outstandingReadTask = clientWebSocket.ReceiveAsync(Array.Empty<byte>(), token);

                    Trace("Client waiting for long delay by server");

                    await longDelayByServerTcs.Task.WaitAsync(token);
                    Trace("Long delay completed on client");

                    var result = await outstandingReadTask;
                    Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                    Assert.False(result.EndOfMessage);
                    Assert.Equal(0, result.Count); // we issued a zero byte read, just to wait for data to become available

                    Assert.Equal(WebSocketState.Open, clientWebSocket.State);

                    Trace("VerifySendReceiveAsync #2 starting on client");
                    await VerifySendReceiveAsync(clientWebSocket, clientMsg, serverMsg, clientAckTcs, serverAckTcs.Task, token);
                    Trace("VerifySendReceiveAsync #2 completed on client");

                    Assert.Equal(WebSocketState.Open, clientWebSocket.State);

                    Trace("Sending close frame from client");

                    await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", token);

                    Assert.Equal(WebSocketState.Closed, clientWebSocket.State);

                    Trace("Client closed");
                },
                async (serverWebSocket, token) =>
                {
                    Trace("VerifySendReceiveAsync #1 starting on server");

                    await VerifySendReceiveAsync(serverWebSocket, serverMsg, clientMsg, serverAckTcs, clientAckTcs.Task, token);

                    Trace("VerifySendReceiveAsync #1 completed on server");

                    Assert.Equal(WebSocketState.Open, serverWebSocket.State);

                    Trace("Server initiating long delay");

                    await Task.Delay(LongDelay);

                    Trace("Server long delay completed");

                    Assert.Equal(WebSocketState.Open, serverWebSocket.State);

                    // recreate already-completed TCS for another round
                    clientAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    serverAckTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                    longDelayByServerTcs.SetResult();

                    Trace("VerifySendReceiveAsync #2 starting on server");

                    await VerifySendReceiveAsync(serverWebSocket, serverMsg, clientMsg, serverAckTcs, clientAckTcs.Task, token);

                    Trace("VerifySendReceiveAsync #2 completed on server");

                    Trace("Receiving close frame on server");

                    var closeFrame = await serverWebSocket.ReceiveAsync(Array.Empty<byte>(), token);
                    Assert.Equal(WebSocketMessageType.Close, closeFrame.MessageType);
                    Assert.Equal(WebSocketState.CloseReceived, serverWebSocket.State);

                    Trace("Sending close frame response from server");

                    await serverWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", token);
                    Assert.Equal(WebSocketState.Closed, serverWebSocket.State);

                    Trace("Server closed");
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

    // --- HTTP/1.1 WebSocket loopback tests ---

    public class KeepAliveTest_Invoker_Loopback : KeepAliveTest_Loopback
    {
        public KeepAliveTest_Invoker_Loopback(ITestOutputHelper output, TracingTestCollection c) : base(output, c) { }
        protected override bool UseCustomInvoker => true;
    }

    public class KeepAliveTest_HttpClient_Loopback : KeepAliveTest_Loopback
    {
        public KeepAliveTest_HttpClient_Loopback(ITestOutputHelper output, TracingTestCollection c) : base(output, c) { }
        protected override bool UseHttpClient => true;
    }

    public class KeepAliveTest_SharedHandler_Loopback : KeepAliveTest_Loopback
    {
        public KeepAliveTest_SharedHandler_Loopback(ITestOutputHelper output, TracingTestCollection c) : base(output, c) { }
    }

    // --- HTTP/2 WebSocket loopback tests ---

    [Collection(nameof(TracingTestCollection))]
    public class KeepAliveTest_Invoker_Http2 : KeepAliveTest_Invoker_Loopback
    {
        public KeepAliveTest_Invoker_Http2(ITestOutputHelper output, TracingTestCollection c) : base(output, c) { }
        protected override Version HttpVersion => Net.HttpVersion.Version20;
    }

    [Collection(nameof(TracingTestCollection))]
    public class KeepAliveTest_HttpClient_Http2 : KeepAliveTest_HttpClient_Loopback
    {
        public KeepAliveTest_HttpClient_Http2(ITestOutputHelper output, TracingTestCollection c) : base(output, c) { }
        protected override Version HttpVersion => Net.HttpVersion.Version20;
    }
}
