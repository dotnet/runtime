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
    /// <summary>
    /// ClientWebSocket tests that do require a remote server.
    /// </summary>
    public class ClientWebSocketTest
    {
        public readonly static object[][] EchoServers = WebSocketTestServers.EchoServers;
        public readonly static object[][] EchoHeadersServers = WebSocketTestServers.EchoHeadersServers;

        private const int TimeOutMilliseconds = 10000;
        private const int CloseDescriptionMaxLength = 123;
        private readonly ITestOutputHelper _output;
        
        public ClientWebSocketTest(ITestOutputHelper output)
        {
            _output = output;
        }

        public static IEnumerable<object[]> UnavailableWebSocketServers
        {
            get
            {
                Uri server;
                
                // Unknown server.
                {
                    server = new Uri(string.Format("ws://{0}", Guid.NewGuid().ToString()));
                    yield return new object[] { server };
                }

                // Known server but not a real websocket endpoint.
                {
                    server = HttpTestServers.RemoteEchoServer;
                    var ub = new UriBuilder("ws", server.Host, server.Port, server.PathAndQuery);

                    yield return new object[] { ub.Uri };
                }
            }
        }

        private static bool WebSocketsSupported { get { return WebSocketHelper.WebSocketsSupported; } }

#region Connect
        [ConditionalTheory("WebSocketsSupported"), MemberData("UnavailableWebSocketServers")]
        public async Task ConnectAsync_NotWebSocketServer_ThrowsWebSocketExceptionWithMessage(Uri server)
        {
            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);
                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() =>
                    cws.ConnectAsync(server, cts.Token));
                    
                Assert.Equal(WebSocketError.Success, ex.WebSocketErrorCode);
                Assert.Equal(WebSocketState.Closed, cws.State);
                Assert.Equal(ResourceHelper.GetExceptionMessage("net_webstatus_ConnectFailure"), ex.Message);
            }
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task EchoBinaryMessage_Success(Uri server)
        {
            await WebSocketHelper.TestEcho(server, WebSocketMessageType.Binary, TimeOutMilliseconds, _output);
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task EchoTextMessage_Success(Uri server)
        {
            await WebSocketHelper.TestEcho(server, WebSocketMessageType.Text, TimeOutMilliseconds, _output);
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoHeadersServers")]
        public async Task ConnectAsync_AddCustomHeaders_Success(Uri server)
        {
            using (var cws = new ClientWebSocket())
            {
                cws.Options.SetRequestHeader("X-CustomHeader1", "Value1");
                cws.Options.SetRequestHeader("X-CustomHeader2", "Value2");
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    Task taskConnect = cws.ConnectAsync(server, cts.Token);
                    Assert.True(
                        (cws.State == WebSocketState.None) ||
                        (cws.State == WebSocketState.Connecting) ||
                        (cws.State == WebSocketState.Open),
                        "State immediately after ConnectAsync incorrect: " + cws.State);
                    await taskConnect;
                }

                Assert.Equal(WebSocketState.Open, cws.State);

                byte[] buffer = new byte[65536];
                var segment = new ArraySegment<byte>(buffer, 0, buffer.Length);
                WebSocketReceiveResult recvResult;
                using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                {
                    recvResult = await cws.ReceiveAsync(segment, cts.Token);
                }

                Assert.Equal(WebSocketMessageType.Text, recvResult.MessageType);
                string headers = WebSocketData.GetTextFromBuffer(segment);
                Assert.True(headers.Contains("X-CustomHeader1:Value1"));
                Assert.True(headers.Contains("X-CustomHeader2:Value2"));

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task ConnectAsync_PassNoSubProtocol_ServerRequires_ThrowsWebSocketExceptionWithMessage(Uri server)
        {
            const string AcceptedProtocol = "CustomProtocol";

            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var ub = new UriBuilder(server);
                ub.Query = "subprotocol=" + AcceptedProtocol;

                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() =>
                    cws.ConnectAsync(ub.Uri, cts.Token));

                Assert.Equal(WebSocketError.Success, ex.WebSocketErrorCode);
                Assert.Equal(WebSocketState.Closed, cws.State);
                Assert.Equal(ResourceHelper.GetExceptionMessage("net_webstatus_ConnectFailure"), ex.Message);
            }
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task ConnectAsync_PassMultipleSubProtocols_ServerRequires_ConnectionUsesAgreedSubProtocol(Uri server)
        {
            const string AcceptedProtocol = "AcceptedProtocol";
            const string OtherProtocol = "OtherProtocol";
            
            using (var cws = new ClientWebSocket())
            {
                cws.Options.AddSubProtocol(AcceptedProtocol);
                cws.Options.AddSubProtocol(OtherProtocol);
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var ub = new UriBuilder(server);
                ub.Query = "subprotocol=" + AcceptedProtocol;

                await cws.ConnectAsync(ub.Uri, cts.Token);
                Assert.Equal(WebSocketState.Open, cws.State);
                Assert.Equal(AcceptedProtocol, cws.SubProtocol);
            }
        }        
#endregion

#region SendReceive
        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
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

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
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

                Assert.Throws<ArgumentException>(() => {
                        Task t = cws.SendAsync(new ArraySegment<byte>(), WebSocketMessageType.Close, true, cts.Token); } );

                Assert.Equal(WebSocketState.Open, cws.State);
            }
        }
        
        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task SendAsync__MultipleOutstandingSendOperations_Throws(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                Task [] tasks = new Task[10];

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
                            Assert.True(false, "Unexpected exception: " + ex.Message);
                        }
                    }
                }
            }
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
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
                        else
                        {
                            Assert.True(false, "Unexpected exception: " + ex.Message);
                        }
                    }
                }
            }
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
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
#endregion

#region Close
        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task CloseAsync_ServerInitiatedClose_Success(Uri server)
        {
            const string closeWebSocketMetaCommand = ".close";
            
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                _output.WriteLine("SendAsync starting.");
                await cws.SendAsync(
                    WebSocketData.GetBufferFromText(closeWebSocketMetaCommand),
                    WebSocketMessageType.Text,
                    true,
                    cts.Token);
                _output.WriteLine("SendAsync done.");

                var recvBuffer = new byte[256];
                _output.WriteLine("ReceiveAsync starting.");
                WebSocketReceiveResult recvResult = await cws.ReceiveAsync(new ArraySegment<byte>(recvBuffer), cts.Token);
                _output.WriteLine("ReceiveAsync done.");
                
                // Verify received server-initiated close message.
                Assert.Equal(WebSocketCloseStatus.NormalClosure, recvResult.CloseStatus);
                Assert.Equal(closeWebSocketMetaCommand, recvResult.CloseStatusDescription);

                // Verify current websocket state as CloseReceived which indicates only partial close.
                Assert.Equal(WebSocketState.CloseReceived, cws.State);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, cws.CloseStatus);
                Assert.Equal(closeWebSocketMetaCommand, cws.CloseStatusDescription);

                // Send back close message to acknowledge server-initiated close.
                _output.WriteLine("CloseAsync starting.");
                await cws.CloseAsync(WebSocketCloseStatus.InvalidMessageType, string.Empty, cts.Token);
                _output.WriteLine("CloseAsync done.");
                Assert.Equal(WebSocketState.Closed, cws.State);
                
                // Verify that there is no follow-up echo close message back from the server by
                // making sure the close code and message are the same as from the first server close message.
                Assert.Equal(WebSocketCloseStatus.NormalClosure, cws.CloseStatus);
                Assert.Equal(closeWebSocketMetaCommand, cws.CloseStatusDescription);
            }
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task CloseAsync_ClientInitiatedClose_Success(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);
                Assert.Equal(WebSocketState.Open, cws.State);

                var closeStatus = WebSocketCloseStatus.InvalidMessageType;
                string closeDescription = "CloseAsync_InvalidMessageType";

                await cws.CloseAsync(closeStatus, closeDescription,  cts.Token);

                Assert.Equal(WebSocketState.Closed, cws.State);
                Assert.Equal(closeStatus, cws.CloseStatus);
                Assert.Equal(closeDescription, cws.CloseStatusDescription);
            }
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task CloseAsync_CloseDescriptionIsMaxLength_Success(Uri server)
        {
            string closeDescription = new string('C', CloseDescriptionMaxLength);
            
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, closeDescription, cts.Token);
            }
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task CloseAsync_CloseDescriptionIsMaxLengthPlusOne_ThrowsArgumentException(Uri server)
        {
            string closeDescription = new string('C', CloseDescriptionMaxLength + 1);

            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                string expectedInnerMessage = ResourceHelper.GetExceptionMessage(
                    "net_WebSockets_InvalidCloseStatusDescription",
                    closeDescription,
                    CloseDescriptionMaxLength);
                    
                var expectedException = new ArgumentException(expectedInnerMessage, "statusDescription");
                string expectedMessage = expectedException.Message;

                Assert.Throws<ArgumentException>(() =>
                    { Task t = cws.CloseAsync(WebSocketCloseStatus.NormalClosure, closeDescription, cts.Token); });

                Assert.Equal(WebSocketState.Open, cws.State);
            }
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task CloseAsync_CloseDescriptionHasUnicode_Success(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var closeStatus = WebSocketCloseStatus.InvalidMessageType;
                string closeDescription = "CloseAsync_Containing\u016Cnicode.";

                await cws.CloseAsync(closeStatus, closeDescription, cts.Token);

                Assert.Equal(closeStatus, cws.CloseStatus);
                Assert.Equal(closeDescription, cws.CloseStatusDescription);
            }
        }
        
        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task CloseAsync_CloseDescriptionIsNull_Success(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var closeStatus = WebSocketCloseStatus.NormalClosure;
                string closeDescription = null;
                
                await cws.CloseAsync(closeStatus, closeDescription, cts.Token);
                Assert.Equal(closeStatus, cws.CloseStatus);
                Assert.Equal(true, String.IsNullOrEmpty(cws.CloseStatusDescription));
            }
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task CloseOutputAsync_ClientInitiated_CanReceive_CanClose(Uri server)
        {
            string message = "Hello WebSockets!";

            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var closeStatus = WebSocketCloseStatus.InvalidPayloadData;
                string closeDescription = "CloseOutputAsync_Client_InvalidPayloadData";

                await cws.SendAsync(WebSocketData.GetBufferFromText(message), WebSocketMessageType.Text, true, cts.Token);
                // Need a short delay as per WebSocket rfc6455 section 5.5.1 there isn't a requirement to receive any
                // data fragments after a close has been sent. The delay allows the received data fragment to be
                // available before calling close. The WinRT MessageWebSocket implementation doesn't allow receiving
                // after a call to Close.
                await Task.Delay(100);
                await cws.CloseOutputAsync(closeStatus, closeDescription, cts.Token);

                // Should be able to receive the message echoed by the server.
                var recvBuffer = new byte[100];
                var segmentRecv = new ArraySegment<byte>(recvBuffer);
                WebSocketReceiveResult recvResult = await cws.ReceiveAsync(segmentRecv, cts.Token);
                Assert.Equal(message.Length, recvResult.Count);
                segmentRecv = new ArraySegment<byte>(segmentRecv.Array, 0, recvResult.Count);
                Assert.Equal(message, WebSocketData.GetTextFromBuffer(segmentRecv));
                Assert.Equal(null, recvResult.CloseStatus);
                Assert.Equal(null, recvResult.CloseStatusDescription);

                await cws.CloseAsync(closeStatus, closeDescription, cts.Token);

                Assert.Equal(closeStatus, cws.CloseStatus);
                Assert.Equal(closeDescription, cws.CloseStatusDescription);
            }
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task CloseOutputAsync_ServerInitiated_CanSend(Uri server)
        {
            string message = "Hello WebSockets!";
            var expectedCloseStatus = WebSocketCloseStatus.NormalClosure;
            var expectedCloseDescription = ".shutdown";

            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                await cws.SendAsync(
                    WebSocketData.GetBufferFromText(".shutdown"), 
                    WebSocketMessageType.Text, 
                    true, 
                    cts.Token);

                // Should be able to receive a shutdown message.
                var recvBuffer = new byte[100];
                var segmentRecv = new ArraySegment<byte>(recvBuffer);
                WebSocketReceiveResult recvResult = await cws.ReceiveAsync(segmentRecv, cts.Token);
                Assert.Equal(0, recvResult.Count);
                Assert.Equal(expectedCloseStatus, recvResult.CloseStatus);
                Assert.Equal(expectedCloseDescription, recvResult.CloseStatusDescription);

                // Verify WebSocket state
                Assert.Equal(expectedCloseStatus, cws.CloseStatus);
                Assert.Equal(expectedCloseDescription, cws.CloseStatusDescription);

                Assert.Equal(WebSocketState.CloseReceived, cws.State);

                // Should be able to send.
                await cws.SendAsync(WebSocketData.GetBufferFromText(message), WebSocketMessageType.Text, true, cts.Token);

                // Cannot change the close status/description with the final close.
                var closeStatus = WebSocketCloseStatus.InvalidPayloadData;
                var closeDescription = "CloseOutputAsync_Client_Description";

                await cws.CloseAsync(closeStatus, closeDescription, cts.Token);

                Assert.Equal(expectedCloseStatus, cws.CloseStatus);
                Assert.Equal(expectedCloseDescription, cws.CloseStatusDescription);
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task CloseOutputAsync_CloseDescriptionIsNull_Success(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var closeStatus = WebSocketCloseStatus.NormalClosure;
                string closeDescription = null;
                
                await cws.CloseOutputAsync(closeStatus, closeDescription, cts.Token);
            }
        }
#endregion

#region Abort
        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public void Abort_ConnectAndAbort_ThrowsWebSocketExceptionWithmessage(Uri server)
        {
            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var ub = new UriBuilder(server);
                ub.Query = "delay10sec";

                Task t = cws.ConnectAsync(ub.Uri, cts.Token);
                cws.Abort();
                WebSocketException ex = Assert.Throws<WebSocketException>(() => t.GetAwaiter().GetResult());

                Assert.Equal(ResourceHelper.GetExceptionMessage("net_webstatus_ConnectFailure"), ex.Message);

                Assert.Equal(WebSocketError.Success, ex.WebSocketErrorCode);
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task Abort_SendAndAbort_Success(Uri server)
        {
            await TestCancellation(async (cws) => {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                Task t = cws.SendAsync(
                    WebSocketData.GetBufferFromText(".delay5sec"),
                    WebSocketMessageType.Text,
                    true,
                    cts.Token);

                cws.Abort();

                await t;
            }, server);
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task Abort_ReceiveAndAbort_Success(Uri server)
        {
            await TestCancellation(async (cws) => {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);

                await cws.SendAsync(
                    WebSocketData.GetBufferFromText(".delay5sec"),
                    WebSocketMessageType.Text,
                    true,
                    ctsDefault.Token);

                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);

                Task t = cws.ReceiveAsync(segment, ctsDefault.Token);
                cws.Abort();

                await t;
            }, server);
        }
        
        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task Abort_CloseAndAbort_Success(Uri server)
        {
            await TestCancellation(async (cws) => {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);

                await cws.SendAsync(
                    WebSocketData.GetBufferFromText(".delay5sec"),
                    WebSocketMessageType.Text,
                    true,
                    ctsDefault.Token);

                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);

                Task t = cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "AbortClose", ctsDefault.Token);
                cws.Abort();

                await t;
            }, server);
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task ClientWebSocket_Abort_CloseOutputAsync(Uri server)
        {
            await TestCancellation(async (cws) => {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);

                await cws.SendAsync(
                    WebSocketData.GetBufferFromText(".delay5sec"),
                    WebSocketMessageType.Text,
                    true,
                    ctsDefault.Token);

                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);

                Task t = cws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "AbortShutdown", ctsDefault.Token);
                cws.Abort();

                await t;
            }, server);
        }
#endregion

#region Cancellation
        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task ConnectAsync_Cancel_ThrowsWebSocketExceptionWithMessage(Uri server)
        {
            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(500);

                var ub = new UriBuilder(server);
                ub.Query = "delay10sec";

                WebSocketException ex =
                    await Assert.ThrowsAsync<WebSocketException>(() => cws.ConnectAsync(ub.Uri, cts.Token));
                Assert.Equal(
                    ResourceHelper.GetExceptionMessage("net_webstatus_ConnectFailure"),
                    ex.Message);
                Assert.Equal(WebSocketError.Success, ex.WebSocketErrorCode);
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task SendAsync_Cancel_Success(Uri server)
        {
            await TestCancellation((cws) => {
                var cts = new CancellationTokenSource(5);
                return cws.SendAsync(
                    WebSocketData.GetBufferFromText(".delay5sec"), 
                    WebSocketMessageType.Text, 
                    true, 
                    cts.Token);
            }, server);
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task ReceiveAsync_Cancel_Success(Uri server)
        {
            await TestCancellation(async (cws) => {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);
                var cts = new CancellationTokenSource(5);

                await cws.SendAsync(
                    WebSocketData.GetBufferFromText(".delay5sec"), 
                    WebSocketMessageType.Text, 
                    true, 
                    ctsDefault.Token);

                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);

                await cws.ReceiveAsync(segment, cts.Token);
            }, server);
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task CloseAsync_Cancel_Success(Uri server)
        {
            await TestCancellation(async (cws) => {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);
                var cts = new CancellationTokenSource(5);

                await cws.SendAsync(
                    WebSocketData.GetBufferFromText(".delay5sec"),
                    WebSocketMessageType.Text,
                    true,
                    ctsDefault.Token);

                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "CancelClose", cts.Token);
            }, server);
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task CloseOutputAsync_Cancel_Success(Uri server)
        {
            await TestCancellation(async (cws) => {

                var cts = new CancellationTokenSource(5);
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);

                await cws.SendAsync(
                    WebSocketData.GetBufferFromText(".delay5sec"),
                    WebSocketMessageType.Text,
                    true,
                    ctsDefault.Token);

                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);

                await cws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "CancelShutdown", cts.Token);
            }, server);
        }

        [ConditionalTheory("WebSocketsSupported"), MemberData("EchoServers")]
        public async Task ReceiveAsync_CancelAndReceive_ThrowsWebSocketExceptionWithMessage(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(500);

                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);

                try
                {
                    await cws.ReceiveAsync(segment, cts.Token);
                    Assert.True(false, "Receive should not complete.");
                }
                catch (OperationCanceledException) { }
                catch (ObjectDisposedException) { }
                catch (WebSocketException) { }

                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() =>
                    cws.ReceiveAsync(segment, CancellationToken.None));
                Assert.Equal(
                    ResourceHelper.GetExceptionMessage("net_WebSockets_InvalidState", "Aborted", "Open, CloseSent"),
                    ex.Message);
            }
        }

        private async Task TestCancellation(Func<ClientWebSocket, Task> action, Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                try
                {
                    await action(cws);
                    // Operation finished before CTS expired.
                }
                catch (OperationCanceledException)
                {
                    // Expected exception
                    Assert.Equal(WebSocketState.Aborted, cws.State);
                }
                catch (ObjectDisposedException)
                {
                    // Expected exception
                    Assert.Equal(WebSocketState.Aborted, cws.State);
                }
                catch (WebSocketException exception)
                {
                    Assert.Equal(ResourceHelper.GetExceptionMessage(
                        "net_WebSockets_InvalidState_ClosedOrAborted",
                        "System.Net.WebSockets.InternalClientWebSocket",
                        "Aborted"),
                        exception.Message);

                    Assert.Equal(WebSocketError.InvalidState, exception.WebSocketErrorCode);
                    Assert.Equal(WebSocketState.Aborted, cws.State);
                }
            }
        }
#endregion
    }
}
