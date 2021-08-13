// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public class CloseTest : ClientWebSocketTestBase
    {
        public CloseTest(ITestOutputHelper output) : base(output) { }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/28957")]
        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServersAndBoolean))]
        public async Task CloseAsync_ServerInitiatedClose_Success(Uri server, bool useCloseOutputAsync)
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
                _output.WriteLine("Close starting.");
                await (useCloseOutputAsync ?
                    cws.CloseOutputAsync(WebSocketCloseStatus.InvalidMessageType, string.Empty, cts.Token) :
                    cws.CloseAsync(WebSocketCloseStatus.InvalidMessageType, string.Empty, cts.Token));
                _output.WriteLine("Close done.");
                Assert.Equal(WebSocketState.Closed, cws.State);

                // Verify that there is no follow-up echo close message back from the server by
                // making sure the close code and message are the same as from the first server close message.
                Assert.Equal(WebSocketCloseStatus.NormalClosure, cws.CloseStatus);
                Assert.Equal(closeWebSocketMetaCommand, cws.CloseStatusDescription);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task CloseAsync_ClientInitiatedClose_Success(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);
                Assert.Equal(WebSocketState.Open, cws.State);

                // See issue for Browser websocket differences https://github.com/dotnet/runtime/issues/45538
                var closeStatus = PlatformDetection.IsBrowser ? WebSocketCloseStatus.NormalClosure : WebSocketCloseStatus.InvalidMessageType;

                string closeDescription = "CloseAsync_InvalidMessageType";

                await cws.CloseAsync(closeStatus, closeDescription, cts.Token);

                Assert.Equal(WebSocketState.Closed, cws.State);
                Assert.Equal(closeStatus, cws.CloseStatus);
                Assert.Equal(closeDescription, cws.CloseStatusDescription);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task CloseAsync_CloseDescriptionIsMaxLength_Success(Uri server)
        {
            string closeDescription = new string('C', CloseDescriptionMaxLength);

            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, closeDescription, cts.Token);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
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

                AssertExtensions.Throws<ArgumentException>("statusDescription", () =>
                    { Task t = cws.CloseAsync(WebSocketCloseStatus.NormalClosure, closeDescription, cts.Token); });

                Assert.Equal(WebSocketState.Open, cws.State);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task CloseAsync_CloseDescriptionHasUnicode_Success(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                // See issue for Browser websocket differences https://github.com/dotnet/runtime/issues/45538
                var closeStatus = PlatformDetection.IsBrowser ? WebSocketCloseStatus.NormalClosure : WebSocketCloseStatus.InvalidMessageType;
                string closeDescription = "CloseAsync_Containing\u016Cnicode.";

                await cws.CloseAsync(closeStatus, closeDescription, cts.Token);

                Assert.Equal(closeStatus, cws.CloseStatus);
                Assert.Equal(closeDescription, cws.CloseStatusDescription);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task CloseAsync_CloseDescriptionIsNull_Success(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var closeStatus = WebSocketCloseStatus.NormalClosure;
                string closeDescription = null;

                await cws.CloseAsync(closeStatus, closeDescription, cts.Token);
                Assert.Equal(closeStatus, cws.CloseStatus);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task CloseOutputAsync_ExpectedStates(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var closeStatus = WebSocketCloseStatus.NormalClosure;
                string closeDescription = null;

                await cws.CloseOutputAsync(closeStatus, closeDescription, cts.Token);
                Assert.True(
                    cws.State == WebSocketState.CloseSent || cws.State == WebSocketState.Closed,
                    $"Expected CloseSent or Closed, got {cws.State}");
                Assert.True(string.IsNullOrEmpty(cws.CloseStatusDescription));
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task CloseAsync_CloseOutputAsync_Throws(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var closeStatus = WebSocketCloseStatus.NormalClosure;
                string closeDescription = null;

                await cws.CloseAsync(closeStatus, closeDescription, cts.Token);
                Assert.True(
                    cws.State == WebSocketState.CloseSent || cws.State == WebSocketState.Closed,
                    $"Expected CloseSent or Closed, got {cws.State}");
                Assert.True(string.IsNullOrEmpty(cws.CloseStatusDescription));
                await Assert.ThrowsAnyAsync<WebSocketException>(async () =>
                    { await cws.CloseOutputAsync(closeStatus, closeDescription, cts.Token); });
                Assert.True(
                    cws.State == WebSocketState.CloseSent || cws.State == WebSocketState.Closed,
                    $"Expected CloseSent or Closed, got {cws.State}");
                Assert.True(string.IsNullOrEmpty(cws.CloseStatusDescription));
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task CloseOutputAsync_ClientInitiated_CanReceive_CanClose(Uri server)
        {
            string message = "Hello WebSockets!";

            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                // See issue for Browser websocket differences https://github.com/dotnet/runtime/issues/45538
                var closeStatus = PlatformDetection.IsBrowser ? WebSocketCloseStatus.NormalClosure : WebSocketCloseStatus.InvalidPayloadData;
                string closeDescription = "CloseOutputAsync_Client_InvalidPayloadData";

                await cws.SendAsync(WebSocketData.GetBufferFromText(message), WebSocketMessageType.Text, true, cts.Token);
                // Need a short delay as per WebSocket rfc6455 section 5.5.1 there isn't a requirement to receive any
                // data fragments after a close has been sent. The delay allows the received data fragment to be
                // available before calling close. The WinRT MessageWebSocket implementation doesn't allow receiving
                // after a call to Close.
                await Task.Delay(500);
                await cws.CloseOutputAsync(closeStatus, closeDescription, cts.Token);

                // Should be able to receive the message echoed by the server.
                var recvBuffer = new byte[100];
                var segmentRecv = new ArraySegment<byte>(recvBuffer);
                WebSocketReceiveResult recvResult = await cws.ReceiveAsync(segmentRecv, cts.Token);
                Assert.Equal(message.Length, recvResult.Count);
                segmentRecv = new ArraySegment<byte>(segmentRecv.Array, 0, recvResult.Count);
                Assert.Equal(message, WebSocketData.GetTextFromBuffer(segmentRecv));
                Assert.Null(recvResult.CloseStatus);
                Assert.Null(recvResult.CloseStatusDescription);

                await cws.CloseAsync(closeStatus, closeDescription, cts.Token);

                Assert.Equal(closeStatus, cws.CloseStatus);
                Assert.Equal(closeDescription, cws.CloseStatusDescription);
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/28957")]
        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
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

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22000", TargetFrameworkMonikers.Netcoreapp)]
        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task CloseOutputAsync_DuringConcurrentReceiveAsync_ExpectedStates(Uri server)
        {
            var receiveBuffer = new byte[1024];
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                // Issue a receive but don't wait for it.
                var t = cws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                Assert.False(t.IsCompleted);
                Assert.Equal(WebSocketState.Open, cws.State);

                // Send a close frame. After this completes, the state could be CloseSent if we haven't
                // yet received the server's response close frame, or it could be Closed if we have.
                await cws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                Assert.True(
                    cws.State == WebSocketState.CloseSent || cws.State == WebSocketState.Closed,
                    $"Expected CloseSent or Closed, got {cws.State}");

                // Now wait for the receive. It will complete once the server's close frame arrives,
                // at which point the ClientWebSocket's state should automatically transition to Closed.
                WebSocketReceiveResult r = await t;
                Assert.Equal(WebSocketMessageType.Close, r.MessageType);
                Assert.Equal(WebSocketState.Closed, cws.State);

                // Closing an already-closed ClientWebSocket should be a no-op. Any other behavior (e.g., throwing exception)
                // would give way to race conditions between (1) CloseAsync being called and (2) the server's response close
                // frame being received after CloseOutputAsync.
                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                Assert.Equal(WebSocketState.Closed, cws.State);

                // Call CloseAsync one more time on the already-closed ClientWebSocket for good measure. Again, this should be a no-op.
                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task CloseAsync_DuringConcurrentReceiveAsync_ExpectedStates(Uri server)
        {
            var receiveBuffer = new byte[1024];
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var t = cws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                Assert.False(t.IsCompleted);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

                // There is a race condition in the above.  If the ReceiveAsync receives the sent close message from the server,
                // then it will complete successfully and the socket will close successfully.  If the CloseAsync receive the sent
                // close message from the server, then the receive async will end up getting aborted along with the socket.
                try
                {
                    await t;
                    Assert.Equal(WebSocketState.Closed, cws.State);
                }
                catch (OperationCanceledException)
                {
                    Assert.Equal(WebSocketState.Aborted, cws.State);
                }
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34690", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/54153", TestPlatforms.Browser)]
        public async Task CloseAsync_CancelableEvenWhenPendingReceive_Throws()
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                try
                {
                    using (var cws = new ClientWebSocket())
                    using (var cts = new CancellationTokenSource(TimeOutMilliseconds))
                    {
                        await cws.ConnectAsync(uri, cts.Token);

                        Task receiveTask = cws.ReceiveAsync(new byte[1], CancellationToken.None);

                        var cancelCloseCts = new CancellationTokenSource();
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                        {
                            Task t = cws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancelCloseCts.Token);
                            cancelCloseCts.Cancel();
                            await t;
                        });

                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => receiveTask);
                    }
                }
                finally
                {
                    tcs.SetResult();
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                Dictionary<string, string> headers = await LoopbackHelper.WebSocketHandshakeAsync(connection);
                Assert.NotNull(headers);

                await tcs.Task;

            }), new LoopbackServer.Options { WebSocketEndpoint = true });
        }
    }
}
