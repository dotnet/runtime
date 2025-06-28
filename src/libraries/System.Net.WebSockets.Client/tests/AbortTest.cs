// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public abstract class AbortTestBase(ITestOutputHelper output) : ClientWebSocketTestBase(output)
    {
        protected async Task RunClient_Abort_ConnectAndAbort_ThrowsWebSocketExceptionWithMessage(Uri server)
        {
            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var ub = new UriBuilder(server);
                ub.Query = "delay10sec";

                Task t = ConnectAsync(cws, ub.Uri, cts.Token);

                cws.Abort();
                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() => t);

                Assert.Equal(ResourceHelper.GetExceptionMessage("net_webstatus_ConnectFailure"), ex.Message);

                Assert.Equal(WebSocketError.Faulted, ex.WebSocketErrorCode);
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        protected async Task RunClient_Abort_SendAndAbort_Success(Uri server)
        {
            await TestCancellation(async (cws) =>
            {
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

        protected async Task RunClient_Abort_ReceiveAndAbort_Success(Uri server)
        {
            await TestCancellation(async (cws) =>
            {
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

        protected async Task RunClient_Abort_CloseAndAbort_Success(Uri server)
        {
            await TestCancellation(async (cws) =>
            {
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

        protected async Task RunClient_ClientWebSocket_Abort_CloseOutputAsync(Uri server)
        {
            await TestCancellation(async (cws) =>
            {
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
    }

    [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
    [ConditionalClass(typeof(ClientWebSocketTestBase), nameof(WebSocketsSupported))]
    public abstract class AbortTest_External(ITestOutputHelper output) : AbortTestBase(output)
    {
        [Theory, MemberData(nameof(EchoServers))]
        public Task Abort_ConnectAndAbort_ThrowsWebSocketExceptionWithMessage(Uri server)
            => RunClient_Abort_ConnectAndAbort_ThrowsWebSocketExceptionWithMessage(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task Abort_SendAndAbort_Success(Uri server)
            => RunClient_Abort_SendAndAbort_Success(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task Abort_ReceiveAndAbort_Success(Uri server)
            => RunClient_Abort_ReceiveAndAbort_Success(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task Abort_CloseAndAbort_Success(Uri server)
            => RunClient_Abort_CloseAndAbort_Success(server);

        [Theory, MemberData(nameof(EchoServers))]
        public Task ClientWebSocket_Abort_CloseOutputAsync(Uri server)
            => RunClient_ClientWebSocket_Abort_CloseOutputAsync(server);
    }

    public sealed class AbortTest_SharedHandler_External(ITestOutputHelper output) : AbortTest_External(output) { }

    public sealed class AbortTest_Invoker_External(ITestOutputHelper output) : AbortTest_External(output)
    {
        protected override bool UseCustomInvoker => true;
    }

    public sealed class AbortTest_HttpClient_External(ITestOutputHelper output) : AbortTest_External(output)
    {
        protected override bool UseHttpClient => true;
    }
}
