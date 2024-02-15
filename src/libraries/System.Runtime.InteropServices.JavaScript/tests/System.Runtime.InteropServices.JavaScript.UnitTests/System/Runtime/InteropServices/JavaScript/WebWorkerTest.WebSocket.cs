// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Threading;
using Xunit;
using System.Net.WebSockets;
using System.Text;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public class WebWorkerWebSocketTest : WebWorkerTestBase
    {
        #region WebSocket

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task WebSocketClient_ContentInSameThread(Executor executor)
        {
            using var cts = CreateTestCaseTimeoutSource();

            var uri = new Uri(WebWorkerTestHelper.LocalWsEcho + "?guid=" + Guid.NewGuid());
            var message = "hello";
            var send = Encoding.UTF8.GetBytes(message);
            var receive = new byte[100];

            await executor.Execute(async () =>
            {
                using var client = new ClientWebSocket();
                await client.ConnectAsync(uri, CancellationToken.None);
                await client.SendAsync(send, WebSocketMessageType.Text, true, CancellationToken.None);

                var res = await client.ReceiveAsync(receive, CancellationToken.None);
                Assert.Equal(WebSocketMessageType.Text, res.MessageType);
                Assert.True(res.EndOfMessage);
                Assert.Equal(send.Length, res.Count);
                Assert.Equal(message, Encoding.UTF8.GetString(receive, 0, res.Count));
            }, cts.Token);
        }


        [Theory, MemberData(nameof(GetTargetThreads2x))]
        public async Task WebSocketClient_ResponseCloseInDifferentThread(Executor executor1, Executor executor2)
        {
            using var cts = CreateTestCaseTimeoutSource();

            var uri = new Uri(WebWorkerTestHelper.LocalWsEcho + "?guid=" + Guid.NewGuid());
            var message = "hello";
            var send = Encoding.UTF8.GetBytes(message);
            var receive = new byte[100];

            var e1Job = async (Task e2done, TaskCompletionSource<ClientWebSocket> e1State) =>
            {
                using var client = new ClientWebSocket();
                await client.ConnectAsync(uri, CancellationToken.None);
                await client.SendAsync(send, WebSocketMessageType.Text, true, CancellationToken.None);

                // share the state with the E2 continuation
                e1State.SetResult(client);
                await e2done;
            };

            var e2Job = async (ClientWebSocket client) =>
            {
                var res = await client.ReceiveAsync(receive, CancellationToken.None);
                Assert.Equal(WebSocketMessageType.Text, res.MessageType);
                Assert.True(res.EndOfMessage);
                Assert.Equal(send.Length, res.Count);
                Assert.Equal(message, Encoding.UTF8.GetString(receive, 0, res.Count));

                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            };

            await ActionsInDifferentThreads<ClientWebSocket>(executor1, executor2, e1Job, e2Job, cts);
        }

        [Theory, MemberData(nameof(GetTargetThreads2x))]
        public async Task WebSocketClient_CancelInDifferentThread(Executor executor1, Executor executor2)
        {
            using var cts = CreateTestCaseTimeoutSource();

            var uri = new Uri(WebWorkerTestHelper.LocalWsEcho + "?guid=" + Guid.NewGuid());
            var message = ".delay5sec"; // this will make the loopback server slower
            var send = Encoding.UTF8.GetBytes(message);
            var receive = new byte[100];

            var e1Job = async (Task e2done, TaskCompletionSource<ClientWebSocket> e1State) =>
            {
                using var client = new ClientWebSocket();
                await client.ConnectAsync(uri, CancellationToken.None);
                await client.SendAsync(send, WebSocketMessageType.Text, true, CancellationToken.None);

                // share the state with the E2 continuation
                e1State.SetResult(client);
                await e2done;
            };

            var e2Job = async (ClientWebSocket client) =>
            {
                CancellationTokenSource cts2 = new CancellationTokenSource();
                var resTask = client.ReceiveAsync(receive, cts2.Token);
                cts2.Cancel();
                var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resTask);
                Assert.Equal(cts2.Token, ex.CancellationToken);
            };

            await ActionsInDifferentThreads<ClientWebSocket>(executor1, executor2, e1Job, e2Job, cts);
        }

        #endregion
    }
}
