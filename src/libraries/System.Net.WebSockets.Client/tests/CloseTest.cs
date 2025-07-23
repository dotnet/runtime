// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

using EchoControlMessage = System.Net.Test.Common.WebSocketEchoHelper.EchoControlMessage;

namespace System.Net.WebSockets.Client.Tests
{
    //
    // Class hierarchy:
    //
    // - CloseTestBase                                → file:CloseTest.cs
    //   ├─ CloseTest_External
    //   │  ├─ [*]CloseTest_SharedHandler_External
    //   │  ├─ [*]CloseTest_Invoker_External
    //   │  └─ [*]CloseTest_HttpClient_External
    //   └─ CloseTest_Loopback                        → file:CloseTest.Loopback.cs
    //      ├─ [*]CloseTest_SharedHandler_Loopback
    //      ├─ [*]CloseTest_Invoker_Loopback
    //      ├─ [*]CloseTest_HttpClient_Loopback
    //      └─ CloseTest_Http2Loopback
    //         ├─ [*]CloseTest_Invoker_Http2Loopback
    //         └─ [*]CloseTest_HttpClient_Http2Loopback
    //
    // ---
    // `[*]` - concrete runnable test classes
    // `→ file:` - file containing the class and its concrete subclasses

    public abstract class CloseTestBase(ITestOutputHelper output) : ClientWebSocketTestBase(output)
    {
        #region Common (Echo Server) tests

        protected async Task RunClient_CloseAsync_ServerInitiatedClose_Success(Uri server, bool useCloseOutputAsync)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                await cws.SendAsync(
                    EchoControlMessage.Shutdown.ToUtf8(),
                    WebSocketMessageType.Text,
                    true,
                    cts.Token);

                var recvBuffer = new byte[256];
                WebSocketReceiveResult recvResult = await cws.ReceiveAsync(new ArraySegment<byte>(recvBuffer), cts.Token);

                // Verify received server-initiated close message.
                Assert.Equal(WebSocketCloseStatus.NormalClosure, recvResult.CloseStatus);
                Assert.Equal(EchoControlMessage.Shutdown, recvResult.CloseStatusDescription);
                Assert.Equal(WebSocketMessageType.Close, recvResult.MessageType);

                // Verify current websocket state as CloseReceived which indicates only partial close.
                Assert.Equal(WebSocketState.CloseReceived, cws.State);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, cws.CloseStatus);
                Assert.Equal(EchoControlMessage.Shutdown, cws.CloseStatusDescription);

                // Send back close message to acknowledge server-initiated close.
                var closeStatus = PlatformDetection.IsNotBrowser ? WebSocketCloseStatus.InvalidMessageType : (WebSocketCloseStatus)3210;
                await (useCloseOutputAsync ?
                    cws.CloseOutputAsync(closeStatus, string.Empty, cts.Token) :
                    cws.CloseAsync(closeStatus, string.Empty, cts.Token));
                Assert.Equal(WebSocketState.Closed, cws.State);

                // Verify that there is no follow-up echo close message back from the server by
                // making sure the close code and message are the same as from the first server close message.
                Assert.Equal(WebSocketCloseStatus.NormalClosure, cws.CloseStatus);
                Assert.Equal(EchoControlMessage.Shutdown, cws.CloseStatusDescription);
            }
        }

        protected async Task RunClient_CloseAsync_ClientInitiatedClose_Success(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
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

        protected async Task RunClient_CloseAsync_CloseDescriptionIsMaxLength_Success(Uri server)
        {
            string closeDescription = new string('C', CloseDescriptionMaxLength);

            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, closeDescription, cts.Token);
            }
        }

        protected async Task RunClient_CloseAsync_CloseDescriptionIsMaxLengthPlusOne_ThrowsArgumentException(Uri server)
        {
            string closeDescription = new string('C', CloseDescriptionMaxLength + 1);

            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
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

        protected async Task RunClient_CloseAsync_CloseDescriptionHasUnicode_Success(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
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

        protected async Task RunClient_CloseAsync_CloseDescriptionIsNull_Success(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var closeStatus = WebSocketCloseStatus.NormalClosure;
                string closeDescription = null;

                await cws.CloseAsync(closeStatus, closeDescription, cts.Token);
                Assert.Equal(closeStatus, cws.CloseStatus);
            }
        }

        protected async Task RunClient_CloseOutputAsync_ExpectedStates(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
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

        protected async Task RunClient_CloseAsync_CloseOutputAsync_Throws(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
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

        protected async Task RunClient_CloseOutputAsync_ClientInitiated_CanReceive_CanClose(Uri server)
        {
            string message = "Hello WebSockets!";

            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var closeStatus = PlatformDetection.IsNotBrowser ? WebSocketCloseStatus.InvalidPayloadData : (WebSocketCloseStatus)3210;
                string closeDescription = "CloseOutputAsync_Client_InvalidPayloadData";

                await cws.SendAsync(message.ToUtf8(), WebSocketMessageType.Text, true, cts.Token);
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
                Assert.Equal(message, segmentRecv.Utf8ToString());
                Assert.Null(recvResult.CloseStatus);
                Assert.Null(recvResult.CloseStatusDescription);

                await cws.CloseAsync(closeStatus, closeDescription, cts.Token);

                Assert.Equal(closeStatus, cws.CloseStatus);
                Assert.Equal(closeDescription, cws.CloseStatusDescription);
            }
        }

        protected async Task RunClient_CloseOutputAsync_ServerInitiated_CanReceive(Uri server, bool delayReceiving)
        {
            var expectedCloseStatus = WebSocketCloseStatus.NormalClosure;
            var expectedCloseDescription = EchoControlMessage.ShutdownAfter;

            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                await cws.SendAsync(
                    expectedCloseDescription.ToUtf8(),
                    WebSocketMessageType.Text,
                    true,
                    cts.Token);

                // let server close the output before we request receiving
                if (delayReceiving)
                    await Task.Delay(1000);

                // Should be able to receive the message echoed by the server.
                var recvBuffer = new byte[100];
                var segmentRecv = new ArraySegment<byte>(recvBuffer);
                WebSocketReceiveResult recvResult = await cws.ReceiveAsync(segmentRecv, cts.Token);
                Assert.Equal(expectedCloseDescription.Length, recvResult.Count);
                segmentRecv = new ArraySegment<byte>(segmentRecv.Array, 0, recvResult.Count);
                Assert.Equal(expectedCloseDescription, segmentRecv.Utf8ToString());
                Assert.Null(recvResult.CloseStatus);
                Assert.Null(recvResult.CloseStatusDescription);

                // Should be able to receive a shutdown message.
                segmentRecv = new ArraySegment<byte>(recvBuffer);
                recvResult = await cws.ReceiveAsync(segmentRecv, cts.Token);
                Assert.Equal(0, recvResult.Count);
                Assert.Equal(expectedCloseStatus, recvResult.CloseStatus);
                Assert.Equal(expectedCloseDescription, recvResult.CloseStatusDescription);

                // Verify WebSocket state
                Assert.Equal(expectedCloseStatus, cws.CloseStatus);
                Assert.Equal(expectedCloseDescription, cws.CloseStatusDescription);

                Assert.Equal(WebSocketState.CloseReceived, cws.State);

                // Cannot change the close status/description with the final close.
                var closeStatus = PlatformDetection.IsNotBrowser ? WebSocketCloseStatus.InvalidPayloadData : (WebSocketCloseStatus)3210;
                var closeDescription = "CloseOutputAsync_Client_Description";

                await cws.CloseAsync(closeStatus, closeDescription, cts.Token);

                Assert.Equal(expectedCloseStatus, cws.CloseStatus);
                Assert.Equal(expectedCloseDescription, cws.CloseStatusDescription);
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        protected async Task RunClient_CloseOutputAsync_ServerInitiated_CanSend(Uri server)
        {
            string message = "Hello WebSockets!";
            var expectedCloseStatus = WebSocketCloseStatus.NormalClosure;
            var expectedCloseDescription = EchoControlMessage.Shutdown;

            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                await cws.SendAsync(
                    EchoControlMessage.Shutdown.ToUtf8(),
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
                await cws.SendAsync(message.ToUtf8(), WebSocketMessageType.Text, true, cts.Token);

                // Cannot change the close status/description with the final close.
                var closeStatus = PlatformDetection.IsNotBrowser ? WebSocketCloseStatus.InvalidPayloadData : (WebSocketCloseStatus)3210;
                var closeDescription = "CloseOutputAsync_Client_Description";

                await cws.CloseAsync(closeStatus, closeDescription, cts.Token);

                Assert.Equal(expectedCloseStatus, cws.CloseStatus);
                Assert.Equal(expectedCloseDescription, cws.CloseStatusDescription);
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        protected async Task RunClient_CloseOutputAsync_ServerInitiated_CanReceiveAfterClose(Uri server, bool syncState)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);
                await cws.SendAsync(
                    EchoControlMessage.ReceiveMessageAfterClose.ToUtf8(),
                    WebSocketMessageType.Text,
                    true,
                    cts.Token);

                await Task.Delay(2000);

                if (syncState)
                {
                    var state = cws.State;
                    Assert.Equal(WebSocketState.Open, state);
                    // should be able to receive after this sync
                }

                var recvBuffer = new ArraySegment<byte>(new byte[1024]);
                WebSocketReceiveResult recvResult = await cws.ReceiveAsync(recvBuffer, cts.Token);
                var recvSegment = new ArraySegment<byte>(recvBuffer.ToArray(), 0, recvResult.Count);
                var message = recvSegment.Utf8ToString();

                Assert.Contains(EchoControlMessage.ReceiveMessageAfterClose, message);
            }
        }

        protected async Task RunClient_CloseOutputAsync_CloseDescriptionIsNull_Success(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var closeStatus = WebSocketCloseStatus.NormalClosure;
                string closeDescription = null;

                await cws.CloseOutputAsync(closeStatus, closeDescription, cts.Token);
            }
        }

        protected async Task RunClient_CloseOutputAsync_DuringConcurrentReceiveAsync_ExpectedStates(Uri server)
        {
            var receiveBuffer = new byte[1024];
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
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

        protected async Task RunClient_CloseAsync_DuringConcurrentReceiveAsync_ExpectedStates(Uri server)
        {
            var receiveBuffer = new byte[1024];
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
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

        #endregion
    }

    [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    public abstract class CloseTest_External(ITestOutputHelper output) : CloseTestBase(output)
    {
        #region Common (Echo Server) tests

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser))] // See https://github.com/dotnet/runtime/issues/28957
        [MemberData(nameof(EchoServersAndBoolean))]
        public Task CloseAsync_ServerInitiatedClose_Success(Uri server, bool useCloseOutputAsync)
            => RunClient_CloseAsync_ServerInitiatedClose_Success(server, useCloseOutputAsync);

        [Theory, MemberData(nameof(EchoServers))]
        public Task CloseAsync_ClientInitiatedClose_Success(Uri server)
            => RunClient_CloseAsync_ClientInitiatedClose_Success(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task CloseAsync_CloseDescriptionIsMaxLength_Success(Uri server)
            => RunClient_CloseAsync_CloseDescriptionIsMaxLength_Success(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task CloseAsync_CloseDescriptionIsMaxLengthPlusOne_ThrowsArgumentException(Uri server)
            => RunClient_CloseAsync_CloseDescriptionIsMaxLengthPlusOne_ThrowsArgumentException(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task CloseAsync_CloseDescriptionHasUnicode_Success(Uri server)
            => RunClient_CloseAsync_CloseDescriptionHasUnicode_Success(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task CloseAsync_CloseDescriptionIsNull_Success(Uri server)
            => RunClient_CloseAsync_CloseDescriptionIsNull_Success(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task CloseOutputAsync_ExpectedStates(Uri server)
            => RunClient_CloseOutputAsync_ExpectedStates(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task CloseAsync_CloseOutputAsync_Throws(Uri server)
            => RunClient_CloseAsync_CloseOutputAsync_Throws(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task CloseOutputAsync_ClientInitiated_CanReceive_CanClose(Uri server)
            => RunClient_CloseOutputAsync_ClientInitiated_CanReceive_CanClose(server);

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser))] // See https://github.com/dotnet/runtime/issues/28957
        [MemberData(nameof(EchoServersAndBoolean))]
        public Task CloseOutputAsync_ServerInitiated_CanReceive(Uri server, bool delayReceiving)
            => RunClient_CloseOutputAsync_ServerInitiated_CanReceive(server, delayReceiving);

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser))] // See https://github.com/dotnet/runtime/issues/28957
        [MemberData(nameof(EchoServers))]
        public Task CloseOutputAsync_ServerInitiated_CanSend(Uri server)
            => RunClient_CloseOutputAsync_ServerInitiated_CanSend(server);

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser))] // See https://github.com/dotnet/runtime/issues/28957
        [MemberData(nameof(EchoServersAndBoolean))]
        public Task CloseOutputAsync_ServerInitiated_CanReceiveAfterClose(Uri server, bool syncState)
            => RunClient_CloseOutputAsync_ServerInitiated_CanReceiveAfterClose(server, syncState);

        [Theory, MemberData(nameof(EchoServers))]
        public Task CloseOutputAsync_CloseDescriptionIsNull_Success(Uri server)
            => RunClient_CloseOutputAsync_CloseDescriptionIsNull_Success(server);

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22000", TargetFrameworkMonikers.Netcoreapp)]
        [Theory, MemberData(nameof(EchoServers))]
        public Task CloseOutputAsync_DuringConcurrentReceiveAsync_ExpectedStates(Uri server)
            => RunClient_CloseOutputAsync_DuringConcurrentReceiveAsync_ExpectedStates(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task CloseAsync_DuringConcurrentReceiveAsync_ExpectedStates(Uri server)
            => RunClient_CloseAsync_DuringConcurrentReceiveAsync_ExpectedStates(server);

        #endregion
    }

    #region Runnable test classes: External/Outerloop

    public sealed class CloseTest_SharedHandler_External(ITestOutputHelper output) : CloseTest_External(output) { }

    public sealed class CloseTest_Invoker_External(ITestOutputHelper output) : CloseTest_External(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class CloseTest_HttpClient_External(ITestOutputHelper output) : CloseTest_External(output)
    {
        protected override bool UseHttpClient => true;
    }

    #endregion
}
