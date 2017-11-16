// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public sealed class ArraySegmentSendReceiveTest : SendReceiveTest
    {
        public ArraySegmentSendReceiveTest(ITestOutputHelper output) : base(output) { }

        protected override Task<WebSocketReceiveResult> ReceiveAsync(WebSocket ws, ArraySegment<byte> arraySegment, CancellationToken cancellationToken) =>
            ws.ReceiveAsync(arraySegment, cancellationToken);

        protected override Task SendAsync(WebSocket ws, ArraySegment<byte> arraySegment, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) =>
            ws.SendAsync(arraySegment, messageType, endOfMessage, cancellationToken);
    }

    public abstract class SendReceiveTest : ClientWebSocketTestBase
    {
        protected abstract Task SendAsync(WebSocket ws, ArraySegment<byte> arraySegment, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);
        protected abstract Task<WebSocketReceiveResult> ReceiveAsync(WebSocket ws, ArraySegment<byte> arraySegment, CancellationToken cancellationToken);

        public static bool PartialMessagesSupported => PlatformDetection.ClientWebSocketPartialMessagesSupported;

        public SendReceiveTest(ITestOutputHelper output) : base(output) { }

        [OuterLoop] // TODO: Issue #11345
        [ActiveIssue(9296)]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task SendReceive_PartialMessageDueToSmallReceiveBuffer_Success(Uri server)
        {
            var sendBuffer = new byte[1024];
            var sendSegment = new ArraySegment<byte>(sendBuffer);

            var receiveBuffer = new byte[1024];
            var receiveSegment = new ArraySegment<byte>(receiveBuffer);

            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);

                // The server will read buffers and aggregate it up to 64KB before echoing back a complete message.
                // But since this test uses a receive buffer that is small, we will get back partial message fragments
                // as we read them until we read the complete message payload.
                for (int i = 0; i < 63; i++)
                {
                    await SendAsync(cws, sendSegment, WebSocketMessageType.Binary, false, ctsDefault.Token);
                }
                await SendAsync(cws, sendSegment, WebSocketMessageType.Binary, true, ctsDefault.Token);

                WebSocketReceiveResult recvResult = await ReceiveAsync(cws, receiveSegment, ctsDefault.Token);
                Assert.Equal(false, recvResult.EndOfMessage);

                while (recvResult.EndOfMessage == false)
                {
                    recvResult = await ReceiveAsync(cws, receiveSegment, ctsDefault.Token);
                }

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "PartialMessageDueToSmallReceiveBufferTest", ctsDefault.Token);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ConditionalTheory(nameof(WebSocketsSupported), nameof(PartialMessagesSupported)), MemberData(nameof(EchoServers))]
        public async Task SendReceive_PartialMessageBeforeCompleteMessageArrives_Success(Uri server)
        {
            var rand = new Random();
            var sendBuffer = new byte[ushort.MaxValue + 1];
            rand.NextBytes(sendBuffer);
            var sendSegment = new ArraySegment<byte>(sendBuffer);

            // Ask the remote server to echo back received messages without ever signaling "end of message".
            var ub = new UriBuilder(server);
            ub.Query = "replyWithPartialMessages";

            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(ub.Uri, TimeOutMilliseconds, _output))
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

                    Assert.Equal(false, recvResult.EndOfMessage);
                    Assert.InRange(recvResult.Count, 0, receiveBuffer.Length - totalBytesReceived);
                    totalBytesReceived += recvResult.Count;
                }

                Assert.Equal(receiveBuffer.Length, totalBytesReceived);
                Assert.Equal<byte>(sendBuffer, receiveBuffer);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "PartialMessageBeforeCompleteMessageArrives", ctsDefault.Token);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task SendAsync_SendCloseMessageType_ThrowsArgumentExceptionWithMessage(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
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

        [OuterLoop] // TODO: Issue #11345
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task SendAsync_MultipleOutstandingSendOperations_Throws(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                Task[] tasks = new Task[10];

                try
                {
                    for (int i = 0; i < tasks.Length; i++)
                    {
                        tasks[i] = SendAsync(
                            cws,
                            WebSocketData.GetBufferFromText("hello"),
                            WebSocketMessageType.Text,
                            true,
                            cts.Token);
                    }

                    Task.WaitAll(tasks);

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

        [OuterLoop] // TODO: Issue #11345
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task ReceiveAsync_MultipleOutstandingReceiveOperations_Throws(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                Task[] tasks = new Task[2];

                await SendAsync(
                    cws,
                    WebSocketData.GetBufferFromText(".delay5sec"),
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

                    Task.WaitAll(tasks);
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
                                    "ReceiveAsync"),
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
                        else if (ex is OperationCanceledException)
                        {
                            Assert.Equal(WebSocketState.Aborted, cws.State);
                        }
                        else
                        {
                            Assert.True(false, "Unexpected exception: " + ex.Message);
                        }
                    }
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task SendAsync_SendZeroLengthPayloadAsEndOfMessage_Success(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);
                string message = "hello";
                await SendAsync(
                    cws,
                    WebSocketData.GetBufferFromText(message),
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
                Assert.Equal(true, recvRet.EndOfMessage);
                Assert.Equal(null, recvRet.CloseStatus);
                Assert.Equal(null, recvRet.CloseStatusDescription);

                var recvSegment = new ArraySegment<byte>(receiveSegment.Array, receiveSegment.Offset, recvRet.Count);
                Assert.Equal(message, WebSocketData.GetTextFromBuffer(recvSegment));
            }
        }

        [ActiveIssue(23765)]
        [OuterLoop] // TODO: Issue #11345
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task SendReceive_VaryingLengthBuffers_Success(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
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

        [OuterLoop] // TODO: Issue #11345
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task SendReceive_Concurrent_Success(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);

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
                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "SendReceive_VaryingLengthBuffers_Success", ctsDefault.Token);

                Array.Reverse(receiveBuffer);
                Assert.Equal<byte>(sendBuffer, receiveBuffer);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ConditionalFact(nameof(WebSocketsSupported))]
        public async Task SendReceive_ConnectionClosedPrematurely_ReceiveAsyncFailsAndWebSocketStateUpdated()
        {
            var options = new LoopbackServer.Options { WebSocketEndpoint = true };

            Func<ClientWebSocket, Socket, Uri, Task> connectToServerThatAbortsConnection = async (clientSocket, server, url) =>
            {
                AutoResetEvent pendingReceiveAsyncPosted =  new AutoResetEvent(false);

                // Start listening for incoming connections on the server side.
                Task<List<string>> acceptTask = LoopbackServer.AcceptSocketAsync(server, async (socket, stream, reader, writer) =>
                {
                    // Complete the WebSocket upgrade. After this is done, the client-side ConnectAsync should complete.
                    Assert.True(await LoopbackServer.WebSocketHandshakeAsync(socket, reader, writer));

                    // Wait for client-side ConnectAsync to complete and for a pending ReceiveAsync to be posted.
                    pendingReceiveAsyncPosted.WaitOne(TimeOutMilliseconds);

                    // Close the underlying connection prematurely (without sending a WebSocket Close frame).
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();

                    return null;
                }, options);

                // Initiate a connection attempt.
                var cts = new CancellationTokenSource(TimeOutMilliseconds);
                await clientSocket.ConnectAsync(url, cts.Token);

                // Post a pending ReceiveAsync before the TCP connection is torn down.
                var recvBuffer = new byte[100];
                var recvSegment = new ArraySegment<byte>(recvBuffer);
                Task pendingReceiveAsync = ReceiveAsync(clientSocket, recvSegment, cts.Token);
                pendingReceiveAsyncPosted.Set();

                // Wait for the server to close the underlying connection.
                acceptTask.Wait(cts.Token);

                // Validate I/O errors and socket state.
                if (!PlatformDetection.IsWindows)
                {
                    _output.WriteLine("[Non-Windows] ManagedWebSocket-based implementation.");

                    WebSocketException pendingReceiveException = await Assert.ThrowsAsync<WebSocketException>(() => pendingReceiveAsync);
                    Assert.Equal(WebSocketError.ConnectionClosedPrematurely, pendingReceiveException.WebSocketErrorCode);

                    WebSocketException newReceiveException =
                        await Assert.ThrowsAsync<WebSocketException>(() => ReceiveAsync(clientSocket, recvSegment, cts.Token));
                    Assert.Equal(WebSocketError.ConnectionClosedPrematurely, newReceiveException.WebSocketErrorCode);

                    Assert.Equal(WebSocketState.Open, clientSocket.State);
                    Assert.Null(clientSocket.CloseStatus);
                }
                else if (PlatformDetection.IsFullFramework)
                {
                    _output.WriteLine("[Windows] ManagedWebSocket-based implementation.");

                    WebSocketException pendingReceiveException = await Assert.ThrowsAsync<WebSocketException>(() => pendingReceiveAsync);
                    Assert.Equal(WebSocketError.ConnectionClosedPrematurely, pendingReceiveException.WebSocketErrorCode);

                    WebSocketException newReceiveException =
                        await Assert.ThrowsAsync<WebSocketException>(() => ReceiveAsync(clientSocket, recvSegment, cts.Token));
                    Assert.Equal(WebSocketError.Success, newReceiveException.WebSocketErrorCode);
                    Assert.Equal(
                        ResourceHelper.GetExceptionMessage("net_WebSockets_InvalidState", "Aborted", "Open, CloseSent"),
                        newReceiveException.Message);

                    Assert.Equal(WebSocketState.Aborted, clientSocket.State);
                    Assert.Null(clientSocket.CloseStatus);
                }
                else if (PlatformDetection.IsUap)
                {
                    _output.WriteLine("WinRTWebSocket-based implementation.");

                    const uint WININET_E_CONNECTION_ABORTED = 0x80072EFE;

                    WebSocketException pendingReceiveException = await Assert.ThrowsAsync<WebSocketException>(() => pendingReceiveAsync);
                    Assert.Equal(WebSocketError.ConnectionClosedPrematurely, pendingReceiveException.WebSocketErrorCode);
                    Assert.NotNull(pendingReceiveException.InnerException);
                    Assert.Equal(WININET_E_CONNECTION_ABORTED, (uint)pendingReceiveException.InnerException.HResult);

                    WebSocketException newReceiveException =
                        await Assert.ThrowsAsync<WebSocketException>(() => ReceiveAsync(clientSocket, recvSegment, cts.Token));
                    Assert.Equal(WebSocketError.Success, newReceiveException.WebSocketErrorCode);
                    Assert.Equal(
                        ResourceHelper.GetExceptionMessage("net_WebSockets_InvalidState", "Aborted", "Open, CloseSent"),
                        newReceiveException.Message);

                    Assert.Equal(WebSocketState.Aborted, clientSocket.State);
                    Assert.Null(clientSocket.CloseStatus);
                }
                else
                {
                    _output.WriteLine("WinHttpWebSocket-based implementation.");

                    const uint WININET_E_CONNECTION_RESET = 0x80072eff;

                    Win32Exception pendingReceiveException = await Assert.ThrowsAnyAsync<Win32Exception>(() => pendingReceiveAsync);
                    Assert.Equal(WININET_E_CONNECTION_RESET, (uint)pendingReceiveException.HResult);

                    Win32Exception newReceiveException =
                        await Assert.ThrowsAnyAsync<Win32Exception>(() => ReceiveAsync(clientSocket, recvSegment, cts.Token));
                    Assert.Equal(WININET_E_CONNECTION_RESET, (uint)newReceiveException.HResult);

                    Assert.Equal(WebSocketState.Open, clientSocket.State);
                    Assert.Null(clientSocket.CloseStatus);
                }
            };

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (ClientWebSocket clientSocket = new ClientWebSocket())
                {
                    await connectToServerThatAbortsConnection(clientSocket, server, url);
                }
            }, options);
        }
    }
}
