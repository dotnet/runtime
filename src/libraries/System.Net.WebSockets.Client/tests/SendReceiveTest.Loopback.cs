// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{

    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Sockets are not supported on browser")]
    public abstract class SendReceiveTest_LoopbackBase(ITestOutputHelper output) : SendReceiveTestBase(output)
    {
        #region Common (Echo Server) tests

        [Theory, MemberData(nameof(UseSslAndSendReceiveType))]
        public Task SendReceive_PartialMessageDueToSmallReceiveBuffer_Success(bool useSsl, SendReceiveType type) => RunEchoAsync(
            server => RunSendReceive(RunClient_SendReceive_PartialMessageDueToSmallReceiveBuffer_Success, server, type), useSsl);

        [ConditionalTheory, MemberData(nameof(UseSslAndSendReceiveType))] // Uses SkipTestException
        public Task SendReceive_PartialMessageBeforeCompleteMessageArrives_Success(bool useSsl, SendReceiveType type) => RunEchoAsync(
            server => RunSendReceive(RunClient_SendReceive_PartialMessageBeforeCompleteMessageArrives_Success, server, type), useSsl);

        [Theory, MemberData(nameof(UseSslAndSendReceiveType))]
        public Task SendAsync_SendCloseMessageType_ThrowsArgumentExceptionWithMessage(bool useSsl, SendReceiveType type) => RunEchoAsync(
            server => RunSendReceive(RunClient_SendAsync_SendCloseMessageType_ThrowsArgumentExceptionWithMessage, server, type), useSsl);

        [Theory, MemberData(nameof(UseSslAndSendReceiveType))]
        public Task SendAsync_MultipleOutstandingSendOperations_Throws(bool useSsl, SendReceiveType type) => RunEchoAsync(
            server => RunSendReceive(RunClient_SendAsync_MultipleOutstandingSendOperations_Throws, server, type), useSsl);

        [Theory, MemberData(nameof(UseSslAndSendReceiveType))]
        public Task ReceiveAsync_MultipleOutstandingReceiveOperations_Throws(bool useSsl, SendReceiveType type) => RunEchoAsync(
            server => RunSendReceive(RunClient_ReceiveAsync_MultipleOutstandingReceiveOperations_Throws, server, type), useSsl);

        [Theory, MemberData(nameof(UseSslAndSendReceiveType))]
        public Task SendAsync_SendZeroLengthPayloadAsEndOfMessage_Success(bool useSsl, SendReceiveType type) => RunEchoAsync(
            server => RunSendReceive(RunClient_SendAsync_SendZeroLengthPayloadAsEndOfMessage_Success, server, type), useSsl);

        [ConditionalTheory, MemberData(nameof(UseSslAndSendReceiveType))] // Uses SkipTestException
        public Task SendReceive_VaryingLengthBuffers_Success(bool useSsl, SendReceiveType type) => RunEchoAsync(
            server => RunSendReceive(RunClient_SendReceive_VaryingLengthBuffers_Success, server, type), useSsl);

        [Theory, MemberData(nameof(UseSslAndSendReceiveType))]
        public Task SendReceive_Concurrent_Success(bool useSsl, SendReceiveType type) => RunEchoAsync(
            server => RunSendReceive(RunClient_SendReceive_Concurrent_Success, server, type), useSsl);

        [Theory, MemberData(nameof(UseSslAndSendReceiveType))]
        public Task ZeroByteReceive_CompletesWhenDataAvailable(bool useSsl, SendReceiveType type) => RunEchoAsync(
            server => RunSendReceive(RunClient_ZeroByteReceive_CompletesWhenDataAvailable, server, type), useSsl);

        #endregion
    }

    public abstract class SendReceiveTest_Loopback(ITestOutputHelper output) : SendReceiveTest_LoopbackBase(output)
    {
        #region HTTP/1.1-only loopback tests

        [Theory, MemberData(nameof(SendReceiveTypes))]
        public Task SendReceive_ConnectionClosedPrematurely_ReceiveAsyncFailsAndWebSocketStateUpdated(SendReceiveType type) => RunSendReceive(
            RunClient_SendReceive_ConnectionClosedPrematurely_ReceiveAsyncFailsAndWebSocketStateUpdated, type);

        private async Task RunClient_SendReceive_ConnectionClosedPrematurely_ReceiveAsyncFailsAndWebSocketStateUpdated()
        {
            var options = new LoopbackServer.Options { WebSocketEndpoint = true };

            Func<ClientWebSocket, LoopbackServer, Uri, Task> connectToServerThatAbortsConnection = async (clientSocket, server, url) =>
            {
                var pendingReceiveAsyncPosted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                // Start listening for incoming connections on the server side.
                Task acceptTask = server.AcceptConnectionAsync(async connection =>
                {
                    // Complete the WebSocket upgrade. After this is done, the client-side ConnectAsync should complete.
                    Assert.NotNull(await LoopbackHelper.WebSocketHandshakeAsync(connection));

                    // Wait for client-side ConnectAsync to complete and for a pending ReceiveAsync to be posted.
                    await pendingReceiveAsyncPosted.Task.WaitAsync(TimeSpan.FromMilliseconds(TimeOutMilliseconds));

                    // Close the underlying connection prematurely (without sending a WebSocket Close frame).
                    connection.Socket.Shutdown(SocketShutdown.Both);
                    connection.Socket.Close();
                });

                // Initiate a connection attempt.
                var cts = new CancellationTokenSource(TimeOutMilliseconds);
                await ConnectAsync(clientSocket, url, cts.Token);

                // Post a pending ReceiveAsync before the TCP connection is torn down.
                var recvBuffer = new byte[100];
                var recvSegment = new ArraySegment<byte>(recvBuffer);
                Task pendingReceiveAsync = ReceiveAsync(clientSocket, recvSegment, cts.Token);
                pendingReceiveAsyncPosted.SetResult();

                // Wait for the server to close the underlying connection.
                await acceptTask.WaitAsync(cts.Token);

                WebSocketException pendingReceiveException = await Assert.ThrowsAsync<WebSocketException>(() => pendingReceiveAsync);

                Assert.Equal(WebSocketError.ConnectionClosedPrematurely, pendingReceiveException.WebSocketErrorCode);

                if (PlatformDetection.IsInAppContainer)
                {
                    const uint WININET_E_CONNECTION_ABORTED = 0x80072EFE;

                    Assert.NotNull(pendingReceiveException.InnerException);
                    Assert.Equal(WININET_E_CONNECTION_ABORTED, (uint)pendingReceiveException.InnerException.HResult);
                }

                WebSocketException newReceiveException =
                        await Assert.ThrowsAsync<WebSocketException>(() => ReceiveAsync(clientSocket, recvSegment, cts.Token));

                Assert.Equal(
                    ResourceHelper.GetExceptionMessage("net_WebSockets_InvalidState", "Aborted", "Open, CloseSent"),
                    newReceiveException.Message);

                Assert.Equal(WebSocketState.Aborted, clientSocket.State);
                Assert.Null(clientSocket.CloseStatus);
            };

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (ClientWebSocket clientSocket = new ClientWebSocket())
                {
                    await connectToServerThatAbortsConnection(clientSocket, server, url);
                }
            }, options);
        }

        #endregion
    }

    public abstract partial class SendReceiveTest_Http2Loopback(ITestOutputHelper output) : SendReceiveTest_LoopbackBase(output)
    {
        internal override Version HttpVersion => Net.HttpVersion.Version20;

        // #region HTTP/2-only loopback tests -> extracted to SendReceiveTest.Http2.cs
    }

    #region Runnable test classes: HTTP/1.1 Loopback

    public sealed class SendReceiveTest_SharedHandler_Loopback(ITestOutputHelper output) : SendReceiveTest_Loopback(output) { }

    public sealed class SendReceiveTest_Invoker_Loopback(ITestOutputHelper output) : SendReceiveTest_Loopback(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class SendReceiveTest_HttpClient_Loopback(ITestOutputHelper output) : SendReceiveTest_Loopback(output)
    {
        protected override bool UseHttpClient => true;
    }

    #endregion

    #region Runnable test classes: HTTP/2 Loopback

    public sealed class SendReceiveTest_Invoker_Http2Loopback(ITestOutputHelper output) : SendReceiveTest_Http2Loopback(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class SendReceiveTest_HttpClient_Http2Loopback(ITestOutputHelper output) : SendReceiveTest_Http2Loopback(output)
    {
        protected override bool UseHttpClient => true;
    }

    #endregion
}
