// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public sealed class InvokerCancelTest : CancelTest
    {
        public InvokerCancelTest(ITestOutputHelper output) : base(output) { }

        protected override bool UseCustomInvoker => true;
    }

    public sealed class HttpClientCancelTest : CancelTest
    {
        public HttpClientCancelTest(ITestOutputHelper output) : base(output) { }

        protected override bool UseHttpClient => true;
    }

    public class CancelTest : ClientWebSocketTestBase
    {
        public CancelTest(ITestOutputHelper output) : base(output) { }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/83579", typeof(PlatformDetection), nameof(PlatformDetection.IsNodeJS))]
        public async Task ConnectAsync_Cancel_ThrowsCancellationException(Uri server)
        {
            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(100);

                var ub = new UriBuilder(server);
                ub.Query = PlatformDetection.IsBrowser ? "delay20sec" : "delay10sec";

                var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ConnectAsync(cws, ub.Uri, cts.Token));
                Assert.True(WebSocketState.Closed == cws.State, $"Actual {cws.State} when {ex}");
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task SendAsync_Cancel_Success(Uri server)
        {
            await TestCancellation((cws) =>
            {
                var cts = new CancellationTokenSource(5);
                return cws.SendAsync(
                    WebSocketData.GetBufferFromText(".delay5sec"),
                    WebSocketMessageType.Text,
                    true,
                    cts.Token);
            }, server);
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/19217")]
        public async Task ReceiveAsync_Cancel_Success(Uri server)
        {
            await TestCancellation(async (cws) =>
            {
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

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task CloseAsync_Cancel_Success(Uri server)
        {
            await TestCancellation(async (cws) =>
            {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

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

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task CloseOutputAsync_Cancel_Success(Uri server)
        {
            await TestCancellation(async (cws) =>
            {

                var cts = new CancellationTokenSource(TimeOutMilliseconds);
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

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task ReceiveAsync_CancelThenReceive_ThrowsOperationCanceledException(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);
                var cts = new CancellationTokenSource();

                cts.Cancel();
                Task receive = cws.ReceiveAsync(segment, cts.Token);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => receive);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task ReceiveAsync_ReceiveThenCancel_ThrowsOperationCanceledException(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);
                var cts = new CancellationTokenSource();

                Task receive = cws.ReceiveAsync(segment, cts.Token);
                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => receive);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task ReceiveAsync_AfterCancellationDoReceiveAsync_ThrowsWebSocketException(Uri server)
        {
            using (ClientWebSocket cws = await GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
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
    }
}
