// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

using EchoControlMessage = System.Net.Test.Common.WebSocketEchoHelper.EchoControlMessage;
using EchoQueryKey = System.Net.Test.Common.WebSocketEchoOptions.EchoQueryKey;

namespace System.Net.WebSockets.Client.Tests
{
    //
    // Class hierarchy:
    //
    // - CancelTestBase                              → file:CancelTest.cs
    //   ├─ CancelTest_External
    //   │  ├─ [*]CancelTest_SharedHandler_External
    //   │  ├─ [*]CancelTest_Invoker_External
    //   │  └─ [*]CancelTest_HttpClient_External
    //   └─ CancelTest_Loopback                      → file:CancelTest.Loopback.cs
    //      ├─ [*]CancelTest_SharedHandler_Loopback
    //      ├─ [*]CancelTest_Invoker_Loopback
    //      ├─ [*]CancelTest_HttpClient_Loopback
    //      └─ CancelTest_Http2Loopback
    //         ├─ [*]CancelTest_Invoker_Http2Loopback
    //         └─ [*]CancelTest_HttpClient_Http2Loopback
    //
    // ---
    // `[*]` - concrete runnable test classes
    // `→ file:` - file containing the class and its concrete subclasses

    public abstract class CancelTestBase(ITestOutputHelper output) : ClientWebSocketTestBase(output)
    {
        #region Common (Echo Server) tests

        protected async Task RunClient_ConnectAsync_Cancel_ThrowsCancellationException(Uri server)
        {
            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(100);

                var ub = new UriBuilder(server)
                {
                    Query = PlatformDetection.IsBrowser ? EchoQueryKey.Delay20Sec : EchoQueryKey.Delay10Sec
                };

                var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ConnectAsync(cws, ub.Uri, cts.Token));
                Assert.True(WebSocketState.Closed == cws.State, $"Actual {cws.State} when {ex}");
            }
        }

        protected async Task RunClient_SendAsync_Cancel_Success(Uri server)
        {
            await TestCancellation((cws) =>
            {
                var cts = new CancellationTokenSource(5);
                return cws.SendAsync(
                    EchoControlMessage.Delay5Sec.ToUtf8(),
                    WebSocketMessageType.Text,
                    true,
                    cts.Token);
            }, server);
        }

        protected async Task RunClient_ReceiveAsync_Cancel_Success(Uri server)
        {
            await TestCancellation(async (cws) =>
            {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);
                var cts = new CancellationTokenSource(5);

                await cws.SendAsync(
                    EchoControlMessage.Delay5Sec.ToUtf8(),
                    WebSocketMessageType.Text,
                    true,
                    ctsDefault.Token);

                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);

                await cws.ReceiveAsync(segment, cts.Token);
            }, server);
        }

        protected async Task RunClient_CloseAsync_Cancel_Success(Uri server)
        {
            await TestCancellation(async (cws) =>
            {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                await cws.SendAsync(
                    EchoControlMessage.Delay5Sec.ToUtf8(),
                    WebSocketMessageType.Text,
                    true,
                    ctsDefault.Token);

                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "CancelClose", cts.Token);
            }, server);
        }

        protected async Task RunClient_CloseOutputAsync_Cancel_Success(Uri server)
        {
            await TestCancellation(async (cws) =>
            {

                var cts = new CancellationTokenSource(TimeOutMilliseconds);
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);

                await cws.SendAsync(
                    EchoControlMessage.Delay5Sec.ToUtf8(),
                    WebSocketMessageType.Text,
                    true,
                    ctsDefault.Token);

                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);

                await cws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "CancelShutdown", cts.Token);
            }, server);
        }

        protected async Task RunClient_ReceiveAsync_CancelThenReceive_ThrowsOperationCanceledException(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);
                var cts = new CancellationTokenSource();

                cts.Cancel();
                Task receive = cws.ReceiveAsync(segment, cts.Token);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => receive);
            }
        }

        protected async Task RunClient_ReceiveAsync_ReceiveThenCancel_ThrowsOperationCanceledException(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);
                var cts = new CancellationTokenSource();

                Task receive = cws.ReceiveAsync(segment, cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => receive);
            }
        }

        protected async Task RunClient_ReceiveAsync_AfterCancellationDoReceiveAsync_ThrowsWebSocketException(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server))
            {
                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);
                var cts = new CancellationTokenSource();

                Task receive = cws.ReceiveAsync(segment, cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => receive);

                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() =>
                    cws.ReceiveAsync(segment, CancellationToken.None));
                Assert.Equal(
                    ResourceHelper.GetExceptionMessage("net_WebSockets_InvalidState", "Aborted", "Open, CloseSent"),
                    ex.Message);
            }
        }

        #endregion
    }

    [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    public abstract class CancelTest_External(ITestOutputHelper output) : CancelTestBase(output)
    {
        #region Common (Echo Server) tests

        [ActiveIssue("https://github.com/dotnet/runtime/issues/83579", typeof(PlatformDetection), nameof(PlatformDetection.IsNodeJS))]
        [Theory, MemberData(nameof(EchoServers))]
        public Task ConnectAsync_Cancel_ThrowsCancellationException(Uri server)
            => RunClient_ConnectAsync_Cancel_ThrowsCancellationException(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task SendAsync_Cancel_Success(Uri server)
            => RunClient_SendAsync_Cancel_Success(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task ReceiveAsync_Cancel_Success(Uri server)
            => RunClient_ReceiveAsync_Cancel_Success(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task CloseAsync_Cancel_Success(Uri server)
            => RunClient_CloseAsync_Cancel_Success(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task CloseOutputAsync_Cancel_Success(Uri server)
            => RunClient_CloseOutputAsync_Cancel_Success(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task ReceiveAsync_CancelThenReceive_ThrowsOperationCanceledException(Uri server)
            => RunClient_ReceiveAsync_CancelThenReceive_ThrowsOperationCanceledException(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task ReceiveAsync_ReceiveThenCancel_ThrowsOperationCanceledException(Uri server)
            => RunClient_ReceiveAsync_ReceiveThenCancel_ThrowsOperationCanceledException(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task ReceiveAsync_AfterCancellationDoReceiveAsync_ThrowsWebSocketException(Uri server)
            => RunClient_ReceiveAsync_AfterCancellationDoReceiveAsync_ThrowsWebSocketException(server);

        #endregion
    }

    #region Runnable test classes: External/Outerloop
    public sealed class CancelTest_SharedHandler_External(ITestOutputHelper output) : CancelTest_External(output) { }

    public sealed class CancelTest_Invoker_External(ITestOutputHelper output) : CancelTest_External(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class CancelTest_HttpClient_External(ITestOutputHelper output) : CancelTest_External(output)
    {
        protected override bool UseHttpClient => true;
    }
    #endregion
}
