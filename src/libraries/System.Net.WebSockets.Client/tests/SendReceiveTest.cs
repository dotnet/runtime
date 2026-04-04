// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

using EchoControlMessage = System.Net.Test.Common.WebSocketEchoHelper.EchoControlMessage;
using EchoQueryKey = System.Net.Test.Common.WebSocketEchoOptions.EchoQueryKey;

namespace System.Net.WebSockets.Client.Tests
{
    //
    // Class hierarchy:
    //
    // - SendReceiveTestBase                                 → file:SendReceiveTest.cs
    //   ├─ SendReceiveTest_External
    //   │  ├─ [*]SendReceiveTest_SharedHandler_External
    //   │  ├─ [*]SendReceiveTest_Invoker_External
    //   │  └─ [*]SendReceiveTest_HttpClient_External
    //   └─ SendReceiveTest_LoopbackBase                      → file:SendReceiveTest.Loopback.cs
    //      ├─ SendReceiveTest_Loopback
    //      │  ├─ [*]SendReceiveTest_SharedHandler_Loopback
    //      │  ├─ [*]SendReceiveTest_Invoker_Loopback
    //      │  └─ [*]SendReceiveTest_HttpClient_Loopback
    //      └─ SendReceiveTest_Http2Loopback                  → file:SendReceiveTest.Loopback.cs, SendReceiveTest.Http2.cs
    //         ├─ [*]SendReceiveTest_Invoker_Http2Loopback
    //         └─ [*]SendReceiveTest_HttpClient_Http2Loopback
    // ---
    // `[*]` - concrete runnable test classes
    // `→ file:` - file containing the class and its concrete subclasses

    public abstract class SendReceiveTestBase(ITestOutputHelper output) : ClientWebSocketTestBase(output)
    {
        #region Send-receive type setup

        public static readonly object[][] EchoServersAndSendReceiveType = ToMemberData(EchoServers_Values, Enum.GetValues<SendReceiveType>());
        public static readonly object[][] UseSslAndSendReceiveType = ToMemberData(UseSsl_Values, Enum.GetValues<SendReceiveType>());
        public static readonly object[][] SendReceiveTypes = ToMemberData(Enum.GetValues<SendReceiveType>());

        public enum SendReceiveType
        {
            ArraySegment = 1,
            Memory = 2
        }

        protected SendReceiveType TestType { get; private set; }

        protected Task SendAsync(WebSocket webSocket, ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return TestType switch
            {
                SendReceiveType.ArraySegment => webSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken),
                SendReceiveType.Memory => SendAsMemoryAsync(webSocket, buffer, messageType, endOfMessage, cancellationToken),
                _ => throw new ArgumentException(nameof(TestType))
            };

            static Task SendAsMemoryAsync(WebSocket ws, ArraySegment<byte> buf, WebSocketMessageType mt, bool eom, CancellationToken ct)
                => ws.SendAsync((ReadOnlyMemory<byte>)buf, mt, eom, ct).AsTask();
        }

        protected Task<WebSocketReceiveResult> ReceiveAsync(WebSocket webSocket, ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            return TestType switch
            {
                SendReceiveType.ArraySegment => webSocket.ReceiveAsync(buffer, cancellationToken),
                SendReceiveType.Memory => ReceiveAsMemoryAsync(webSocket, buffer, cancellationToken),
                _ => throw new ArgumentException(nameof(TestType))
            };

            static async Task<WebSocketReceiveResult> ReceiveAsMemoryAsync(WebSocket ws, ArraySegment<byte> buf, CancellationToken ct)
            {
                ValueWebSocketReceiveResult result = await ws.ReceiveAsync((Memory<byte>)buf, ct);
                return new WebSocketReceiveResult(result.Count, result.MessageType, result.EndOfMessage, ws.CloseStatus, ws.CloseStatusDescription);
            }
        }

        protected Task RunSendReceive(Func<Task> sendReceiveFunc, SendReceiveType sendReceiveTestType)
        {
            TestType = sendReceiveTestType;
            return sendReceiveFunc();
        }

        protected Task RunSendReceive(Func<Uri, Task> sendReceiveFunc, Uri uri, SendReceiveType sendReceiveTestType)
            => RunSendReceive(() => sendReceiveFunc(uri), sendReceiveTestType);

        #endregion

        #region Common (Echo Server) tests

        protected async Task RunClient_SendReceive_PartialMessageDueToSmallReceiveBuffer_Success(Uri server)
        {
            const int SendBufferSize = 10;
            var sendBuffer = new byte[SendBufferSize];
            var sendSegment = new ArraySegment<byte>(sendBuffer);

            var receiveBuffer = new byte[SendBufferSize / 2];
            var receiveSegment = new ArraySegment<byte>(receiveBuffer);

            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);

                // The server will read buffers and aggregate it before echoing back a complete message.
                // But since this test uses a receive buffer that is smaller than the complete message, we will get
                // back partial message fragments as we read them until we read the complete message payload.
                for (int i = 0; i < SendBufferSize * 5; i++)
                {
                    await SendAsync(cws, sendSegment, WebSocketMessageType.Binary, false, ctsDefault.Token);
                }

                await SendAsync(cws, sendSegment, WebSocketMessageType.Binary, true, ctsDefault.Token);

                WebSocketReceiveResult recvResult = await ReceiveAsync(cws, receiveSegment, ctsDefault.Token);
                Assert.False(recvResult.EndOfMessage);

                while (recvResult.EndOfMessage == false)
                {
                    recvResult = await ReceiveAsync(cws, receiveSegment, ctsDefault.Token);
                }

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "PartialMessageDueToSmallReceiveBufferTest", ctsDefault.Token);
            }
        }

        protected async Task RunClient_SendReceive_PartialMessageBeforeCompleteMessageArrives_Success(Uri server)
        {
            if (HttpVersion == Net.HttpVersion.Version20)
            {
                throw new SkipTestException("[ActiveIssue] -- temporarily skipping on HTTP/2");
            }

            var sendBuffer = new byte[ushort.MaxValue + 1];
            Random.Shared.NextBytes(sendBuffer);
            var sendSegment = new ArraySegment<byte>(sendBuffer);

            // Ask the remote server to echo back received messages without ever signaling "end of message".
            var ub = new UriBuilder(server) { Query = EchoQueryKey.ReplyWithPartialMessages };

            using (ClientWebSocket cws = await GetConnectedWebSocket(ub.Uri))
            {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);

                // Send data to the server; the server will reply back with one or more partial messages. We should be
                // able to consume that data as it arrives, without having to wait for "end of message" to be signaled.
                await SendAsync(cws, sendSegment, WebSocketMessageType.Binary, true, ctsDefault.Token);

                int totalBytesReceived = 0;
                var receiveBuffer = new byte[sendBuffer.Length];
                while (totalBytesReceived < receiveBuffer.Length)
                {
                    WebSocketReceiveResult recvResult = await ReceiveAsync(
                        cws,
                        new ArraySegment<byte>(receiveBuffer, totalBytesReceived, receiveBuffer.Length - totalBytesReceived),
                        ctsDefault.Token);

                    Assert.False(recvResult.EndOfMessage);
                    Assert.InRange(recvResult.Count, 0, receiveBuffer.Length - totalBytesReceived);
                    totalBytesReceived += recvResult.Count;
                }

                Assert.Equal(receiveBuffer.Length, totalBytesReceived);
                Assert.Equal<byte>(sendBuffer, receiveBuffer);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "PartialMessageBeforeCompleteMessageArrives", ctsDefault.Token);
            }
        }

        protected async Task RunClient_SendAsync_SendCloseMessageType_ThrowsArgumentExceptionWithMessage(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                string expectedInnerMessage = ResourceHelper.GetExceptionMessage(
                        "net_WebSockets_Argument_InvalidMessageType",
                        "Close",
                        "SendAsync",
                        "Binary",
                        "Text",
                        "CloseOutputAsync");

                var expectedException = new ArgumentException(expectedInnerMessage, "messageType");
                string expectedMessage = expectedException.Message;

                AssertExtensions.Throws<ArgumentException>("messageType", () =>
                {
                    Task t = SendAsync(cws, new ArraySegment<byte>(), WebSocketMessageType.Close, true, cts.Token);
                });

                Assert.Equal(WebSocketState.Open, cws.State);
            }
        }

        protected async Task RunClient_SendAsync_MultipleOutstandingSendOperations_Throws(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                Task[] tasks = new Task[10];

                try
                {
                    for (int i = 0; i < tasks.Length; i++)
                    {
                        tasks[i] = SendAsync(
                            cws,
                            "hello".ToUtf8(),
                            WebSocketMessageType.Text,
                            true,
                            cts.Token);
                    }

                    await Task.WhenAll(tasks);

                    Assert.Equal(WebSocketState.Open, cws.State);
                }
                catch (AggregateException ag)
                {
                    foreach (var ex in ag.InnerExceptions)
                    {
                        if (ex is InvalidOperationException)
                        {
                            Assert.Equal(
                                ResourceHelper.GetExceptionMessage(
                                    "net_Websockets_AlreadyOneOutstandingOperation",
                                    "SendAsync"),
                                ex.Message);

                            Assert.Equal(WebSocketState.Aborted, cws.State);
                        }
                        else if (ex is WebSocketException)
                        {
                            // Multiple cases.
                            Assert.Equal(WebSocketState.Aborted, cws.State);

                            WebSocketError errCode = (ex as WebSocketException).WebSocketErrorCode;
                            Assert.True(
                                (errCode == WebSocketError.InvalidState) || (errCode == WebSocketError.Success),
                                "WebSocketErrorCode");
                        }
                        else
                        {
                            Assert.IsAssignableFrom<OperationCanceledException>(ex);
                        }
                    }
                }
            }
        }

        protected const int SmallTimeoutMs = 200;
        protected virtual int MultipleOutstandingReceiveOperations_TimeoutMs => SmallTimeoutMs;

        protected async Task RunClient_ReceiveAsync_MultipleOutstandingReceiveOperations_Throws(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var cts = new CancellationTokenSource(MultipleOutstandingReceiveOperations_TimeoutMs);

                Task[] tasks = new Task[2];

                await SendAsync(
                    cws,
                    EchoControlMessage.Delay5Sec.ToUtf8(),
                    WebSocketMessageType.Text,
                    true,
                    cts.Token);

                var recvBuffer = new byte[100];
                var recvSegment = new ArraySegment<byte>(recvBuffer);

                try
                {
                    for (int i = 0; i < tasks.Length; i++)
                    {
                        tasks[i] = ReceiveAsync(cws, recvSegment, cts.Token);
                    }

                    await Task.WhenAll(tasks);
                    Assert.Equal(WebSocketState.Open, cws.State);
                }
                catch (Exception ex)
                {
                    if (ex is InvalidOperationException)
                    {
                        Assert.Equal(
                            ResourceHelper.GetExceptionMessage(
                                "net_Websockets_AlreadyOneOutstandingOperation",
                                "ReceiveAsync"),
                            ex.Message);

                        Assert.True(WebSocketState.Aborted == cws.State, cws.State + " state when InvalidOperationException");
                    }
                    else if (ex is WebSocketException)
                    {
                        // Multiple cases.
                        Assert.True(WebSocketState.Aborted == cws.State, cws.State + " state when WebSocketException");

                        WebSocketError errCode = (ex as WebSocketException).WebSocketErrorCode;
                        Assert.True(
                            (errCode == WebSocketError.InvalidState) || (errCode == WebSocketError.Success),
                            "WebSocketErrorCode");
                    }
                    else if (ex is OperationCanceledException)
                    {
                        Assert.True(WebSocketState.Aborted == cws.State, cws.State + " state when OperationCanceledException");
                    }
                    else
                    {
                        Assert.Fail("Unexpected exception: " + ex.Message);
                    }
                }
            }
        }

        protected async Task RunClient_SendAsync_SendZeroLengthPayloadAsEndOfMessage_Success(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);
                string message = "hello";
                await SendAsync(
                    cws,
                    message.ToUtf8(),
                    WebSocketMessageType.Text,
                    false,
                    cts.Token);
                Assert.Equal(WebSocketState.Open, cws.State);
                await SendAsync(
                    cws,
                    new ArraySegment<byte>(new byte[0]),
                    WebSocketMessageType.Text,
                    true,
                    cts.Token);
                Assert.Equal(WebSocketState.Open, cws.State);

                var recvBuffer = new byte[100];
                var receiveSegment = new ArraySegment<byte>(recvBuffer);
                WebSocketReceiveResult recvRet = await ReceiveAsync(cws, receiveSegment, cts.Token);

                Assert.Equal(WebSocketState.Open, cws.State);
                Assert.Equal(message.Length, recvRet.Count);
                Assert.Equal(WebSocketMessageType.Text, recvRet.MessageType);
                Assert.True(recvRet.EndOfMessage);
                Assert.Null(recvRet.CloseStatus);
                Assert.Null(recvRet.CloseStatusDescription);

                var recvSegment = new ArraySegment<byte>(receiveSegment.Array, receiveSegment.Offset, recvRet.Count);
                Assert.Equal(message, recvSegment.Utf8ToString());
            }
        }

        protected async Task RunClient_SendReceive_VaryingLengthBuffers_Success(Uri server)
        {
            if (HttpVersion == Net.HttpVersion.Version20)
            {
                throw new SkipTestException("[ActiveIssue] -- temporarily skipping on HTTP/2");
            }

            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var rand = new Random();
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);

                // Values chosen close to boundaries in websockets message length handling as well
                // as in vectors used in mask application.
                foreach (int bufferSize in new int[] { 1, 3, 4, 5, 31, 32, 33, 125, 126, 127, 128, ushort.MaxValue - 1, ushort.MaxValue, ushort.MaxValue + 1, ushort.MaxValue * 2 })
                {
                    byte[] sendBuffer = new byte[bufferSize];
                    rand.NextBytes(sendBuffer);
                    await SendAsync(cws, new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Binary, true, ctsDefault.Token);

                    byte[] receiveBuffer = new byte[bufferSize];
                    int totalReceived = 0;
                    while (true)
                    {
                        WebSocketReceiveResult recvResult = await ReceiveAsync(
                            cws,
                            new ArraySegment<byte>(receiveBuffer, totalReceived, receiveBuffer.Length - totalReceived),
                            ctsDefault.Token);

                        Assert.InRange(recvResult.Count, 0, receiveBuffer.Length - totalReceived);
                        totalReceived += recvResult.Count;

                        if (recvResult.EndOfMessage) break;
                    }

                    Assert.Equal(receiveBuffer.Length, totalReceived);
                    Assert.Equal<byte>(sendBuffer, receiveBuffer);
                }

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "SendReceive_VaryingLengthBuffers_Success", ctsDefault.Token);
            }
        }

        protected async Task RunClient_SendReceive_Concurrent_Success(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                CancellationTokenSource ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);

                byte[] receiveBuffer = new byte[10];
                byte[] sendBuffer = new byte[10];
                for (int i = 0; i < sendBuffer.Length; i++)
                {
                    sendBuffer[i] = (byte)i;
                }

                for (int i = 0; i < sendBuffer.Length; i++)
                {
                    Task<WebSocketReceiveResult> receive = ReceiveAsync(cws, new ArraySegment<byte>(receiveBuffer, receiveBuffer.Length - i - 1, 1), ctsDefault.Token);
                    Task send = SendAsync(cws, new ArraySegment<byte>(sendBuffer, i, 1), WebSocketMessageType.Binary, true, ctsDefault.Token);
                    await Task.WhenAll(receive, send);
                    Assert.Equal(1, receive.Result.Count);
                }
                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "SendReceive_Concurrent_Success", ctsDefault.Token);

                Array.Reverse(receiveBuffer);
                Assert.Equal<byte>(sendBuffer, receiveBuffer);
            }
        }

        protected async Task RunClient_ZeroByteReceive_CompletesWhenDataAvailable(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);

                // Do a 0-byte receive.  It shouldn't complete yet.
                Task<WebSocketReceiveResult> t = ReceiveAsync(cws, new ArraySegment<byte>(Array.Empty<byte>()), ctsDefault.Token);
                Assert.False(t.IsCompleted);

                // Send a packet to the echo server.
                await SendAsync(cws, new ArraySegment<byte>(new byte[1] { 42 }), WebSocketMessageType.Binary, true, ctsDefault.Token);

                // Now the 0-byte receive should complete, but without reading any data.
                WebSocketReceiveResult r = await t;
                Assert.Equal(WebSocketMessageType.Binary, r.MessageType);
                Assert.Equal(0, r.Count);
                Assert.False(r.EndOfMessage);

                // Now do a receive to get the payload.
                var receiveBuffer = new byte[1];
                t = ReceiveAsync(cws, new ArraySegment<byte>(receiveBuffer), ctsDefault.Token);
                // this is not synchronously possible when the WS client is on another WebWorker
                if (!PlatformDetection.IsWasmThreadingSupported)
                {
                    Assert.Equal(TaskStatus.RanToCompletion, t.Status);
                }

                r = await t;
                Assert.Equal(WebSocketMessageType.Binary, r.MessageType);
                Assert.Equal(1, r.Count);
                Assert.True(r.EndOfMessage);
                Assert.Equal(42, receiveBuffer[0]);

                // Clean up.
                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, nameof(RunClient_ZeroByteReceive_CompletesWhenDataAvailable), ctsDefault.Token);
            }
        }

        #endregion
    }

    [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    public abstract class SendReceiveTest_External(ITestOutputHelper output) : SendReceiveTestBase(output)
    {
        #region Common (Echo Server) tests

        [Theory, MemberData(nameof(EchoServersAndSendReceiveType))]
        public Task SendReceive_PartialMessageDueToSmallReceiveBuffer_Success(Uri server, SendReceiveType type) => RunSendReceive(
            RunClient_SendReceive_PartialMessageDueToSmallReceiveBuffer_Success, server, type);

        [SkipOnPlatform(TestPlatforms.Browser, "JS Websocket does not support see issue https://github.com/dotnet/runtime/issues/46983")]
        [Theory, MemberData(nameof(EchoServersAndSendReceiveType))]
        public Task SendReceive_PartialMessageBeforeCompleteMessageArrives_Success(Uri server, SendReceiveType type) => RunSendReceive(
            RunClient_SendReceive_PartialMessageBeforeCompleteMessageArrives_Success, server, type);

        [Theory, MemberData(nameof(EchoServersAndSendReceiveType))]
        public Task SendAsync_SendCloseMessageType_ThrowsArgumentExceptionWithMessage(Uri server, SendReceiveType type) => RunSendReceive(
            RunClient_SendAsync_SendCloseMessageType_ThrowsArgumentExceptionWithMessage, server, type);

        [Theory, MemberData(nameof(EchoServersAndSendReceiveType))]
        public Task SendAsync_MultipleOutstandingSendOperations_Throws(Uri server, SendReceiveType type) => RunSendReceive(
            RunClient_SendAsync_MultipleOutstandingSendOperations_Throws, server, type);

        protected override int MultipleOutstandingReceiveOperations_TimeoutMs => PlatformDetection.LocalEchoServerIsNotAvailable ? TimeOutMilliseconds : SmallTimeoutMs;

        [ActiveIssue("https://github.com/dotnet/runtime/issues/83517", typeof(PlatformDetection), nameof(PlatformDetection.IsNodeJS))]
        [Theory, MemberData(nameof(EchoServersAndSendReceiveType))]
        public Task ReceiveAsync_MultipleOutstandingReceiveOperations_Throws(Uri server, SendReceiveType type) => RunSendReceive(
            RunClient_ReceiveAsync_MultipleOutstandingReceiveOperations_Throws, server, type);

        [Theory, MemberData(nameof(EchoServersAndSendReceiveType))]
        public Task SendAsync_SendZeroLengthPayloadAsEndOfMessage_Success(Uri server, SendReceiveType type) => RunSendReceive(
            RunClient_SendAsync_SendZeroLengthPayloadAsEndOfMessage_Success, server, type);

        [Theory, MemberData(nameof(EchoServersAndSendReceiveType))]
        public Task SendReceive_VaryingLengthBuffers_Success(Uri server, SendReceiveType type) => RunSendReceive(
            RunClient_SendReceive_VaryingLengthBuffers_Success, server, type);

        [Theory, MemberData(nameof(EchoServersAndSendReceiveType))]
        public Task SendReceive_Concurrent_Success(Uri server, SendReceiveType type) => RunSendReceive(
            RunClient_SendReceive_Concurrent_Success, server, type);

        [Theory, MemberData(nameof(EchoServersAndSendReceiveType))]
        public Task ZeroByteReceive_CompletesWhenDataAvailable(Uri server, SendReceiveType type) => RunSendReceive(
            RunClient_ZeroByteReceive_CompletesWhenDataAvailable, server, type);

        #endregion
    }

    #region Runnable test classes: External/Outerloop

    public sealed class SendReceiveTest_SharedHandler_External(ITestOutputHelper output) : SendReceiveTest_External(output) { }

    public sealed class SendReceiveTest_Invoker_External(ITestOutputHelper output) : SendReceiveTest_External(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class SendReceiveTest_HttpClient_External(ITestOutputHelper output) : SendReceiveTest_External(output)
    {
        protected override bool UseHttpClient => true;
    }

    #endregion
}
