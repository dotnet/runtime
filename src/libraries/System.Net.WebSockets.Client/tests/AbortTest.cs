// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public class AbortTest : ClientWebSocketTestBase
    {
        public AbortTest(ITestOutputHelper output) : base(output) { }

        [OuterLoop]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task Abort_ConnectAndAbort_ThrowsWebSocketExceptionWithmessage(Uri server)
        {
            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(TimeOutMilliseconds);

                var ub = new UriBuilder(server);
                ub.Query = "delay10sec";

                Task t = cws.ConnectAsync(ub.Uri, cts.Token);
                cws.Abort();
                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() => t);

                Assert.Equal(ResourceHelper.GetExceptionMessage("net_webstatus_ConnectFailure"), ex.Message);

                if (PlatformDetection.IsNetCore) // bug fix in netcoreapp: https://github.com/dotnet/corefx/pull/35960
                {
                    Assert.Equal(WebSocketError.Faulted, ex.WebSocketErrorCode);
                }
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        [OuterLoop]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task Abort_SendAndAbort_Success(Uri server)
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

        [OuterLoop]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task Abort_ReceiveAndAbort_Success(Uri server)
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

        [OuterLoop]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task Abort_CloseAndAbort_Success(Uri server)
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

        [OuterLoop]
        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task ClientWebSocket_Abort_CloseOutputAsync(Uri server)
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
}
