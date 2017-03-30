// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public class SendReceiveTest : ClientWebSocketTestBase
    {
        public SendReceiveTest(ITestOutputHelper output) : base(output) { }

        [OuterLoop] // TODO: Issue #11345
        [ActiveIssue(9296)]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task SendReceive_PartialMessage_Success(Uri server)
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
                    await cws.SendAsync(sendSegment, WebSocketMessageType.Binary, false, ctsDefault.Token);
                }
                await cws.SendAsync(sendSegment, WebSocketMessageType.Binary, true, ctsDefault.Token);

                WebSocketReceiveResult recvResult = await cws.ReceiveAsync(receiveSegment, ctsDefault.Token);
                Assert.Equal(false, recvResult.EndOfMessage);

                while (recvResult.EndOfMessage == false)
                {
                    recvResult = await cws.ReceiveAsync(receiveSegment, ctsDefault.Token);
                }

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "PartialMessageTest", ctsDefault.Token);
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

                Assert.Throws<ArgumentException>(() =>
                {
                    Task t = cws.SendAsync(new ArraySegment<byte>(), WebSocketMessageType.Close, true, cts.Token);
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
                        tasks[i] = cws.SendAsync(
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

                await cws.SendAsync(
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
                        tasks[i] = cws.ReceiveAsync(recvSegment, cts.Token);
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
                await cws.SendAsync(
                            WebSocketData.GetBufferFromText(message),
                            WebSocketMessageType.Text,
                            false,
                            cts.Token);
                Assert.Equal(WebSocketState.Open, cws.State);
                await cws.SendAsync(new ArraySegment<byte>(new byte[0]),
                            WebSocketMessageType.Text,
                            true,
                            cts.Token);
                Assert.Equal(WebSocketState.Open, cws.State);

                var recvBuffer = new byte[100];
                var receiveSegment = new ArraySegment<byte>(recvBuffer);
                WebSocketReceiveResult recvRet = await cws.ReceiveAsync(receiveSegment, cts.Token);

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
                    await cws.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Binary, true, ctsDefault.Token);

                    byte[] receiveBuffer = new byte[bufferSize];
                    int totalReceived = 0;
                    while (true)
                    {
                        WebSocketReceiveResult recvResult = await cws.ReceiveAsync(
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
                    Task<WebSocketReceiveResult> receive = cws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer, receiveBuffer.Length - i - 1, 1), ctsDefault.Token);
                    Task send = cws.SendAsync(new ArraySegment<byte>(sendBuffer, i, 1), WebSocketMessageType.Binary, true, ctsDefault.Token);
                    await Task.WhenAll(receive, send);
                    Assert.Equal(1, receive.Result.Count);
                }
                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "SendReceive_VaryingLengthBuffers_Success", ctsDefault.Token);

                Array.Reverse(receiveBuffer);
                Assert.Equal<byte>(sendBuffer, receiveBuffer);
            }
        }
    }
}
