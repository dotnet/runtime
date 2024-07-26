// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "KeepAlive not supported on browser")]
    public class KeepAliveTest : ClientWebSocketTestBase
    {
        public KeepAliveTest(ITestOutputHelper output) : base(output) { }

        [ConditionalFact(nameof(WebSocketsSupported))]
        [OuterLoop] // involves long delay
        public async Task KeepAlive_LongDelayBetweenSendReceives_Succeeds()
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(System.Net.Test.Common.Configuration.WebSockets.RemoteEchoServer, TimeOutMilliseconds, _output, TimeSpan.FromSeconds(1)))
            {
                await cws.SendAsync(new ArraySegment<byte>(new byte[1] { 42 }), WebSocketMessageType.Binary, true, CancellationToken.None);

                await Task.Delay(TimeSpan.FromSeconds(10));

                byte[] receiveBuffer = new byte[1];
                Assert.Equal(1, (await cws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None)).Count);
                Assert.Equal(42, receiveBuffer[0]);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "KeepAlive_LongDelayBetweenSendReceives_Succeeds", CancellationToken.None);
            }
        }

        //TODO: remove
        private static IsAutobahnTestRun => false;

        [ConditionalFact(nameof(IsAutobahnTestRun))]
        public async Task Test()
        {
            var baseUri = new Uri("ws://127.0.0.1:9001");

            await RunClientAsync(baseUri, "dotnet-timeout");
        }

        static async Task RunClientAsync(Uri baseUri, string agentName)
        {
            int caseCount = await GetCaseCountAsync(baseUri);

            Memory<byte> buffer = new byte[1024 * 1024];

            for (int caseId = 1; caseId <= caseCount; ++caseId)
            {
                Console.Write($"Running test case {caseId}...");

                using var client = new ClientWebSocket
                {
                    Options =
                    {
                        KeepAliveInterval = TimeSpan.FromMilliseconds(100),
                        KeepAliveTimeout = TimeSpan.FromMilliseconds(500),
                    }
                };

                try
                {
                    await client.ConnectAsync(new Uri(baseUri, $"runCase?case={caseId}&agent={agentName}"), CancellationToken.None);

                    while (true)
                    {

                        ValueWebSocketReceiveResult result = await client.ReceiveAsync(buffer, CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }
                        await client.SendAsync(buffer.Slice(0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                    }
                }
                catch (WebSocketException)
                {
                }

                if (client.State is not (WebSocketState.Aborted or WebSocketState.Closed))
                {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, client.CloseStatusDescription, CancellationToken.None);
                }
                Console.WriteLine(" Completed.");
            }

            Console.WriteLine("Updating reports...");
            await UpdateReportsAsync(baseUri, agentName);
            Console.WriteLine("Done");
        }

        private static async Task<int> GetCaseCountAsync(Uri baseUri)
        {
            using var client = new ClientWebSocket();

            await client.ConnectAsync(new Uri(baseUri, "getCaseCount"), CancellationToken.None);
            Memory<byte> buffer = new byte[16];
            ValueWebSocketReceiveResult result = await client.ReceiveAsync(buffer, CancellationToken.None);

            return int.Parse(Encoding.UTF8.GetString(buffer.Span.Slice(0, result.Count)));
        }

        private static async Task UpdateReportsAsync(Uri baseUri, string agentName)
        {
            using var client = new ClientWebSocket();

            await client.ConnectAsync(new Uri(baseUri, "updateReports?agent=" + agentName), CancellationToken.None);
            ValueWebSocketReceiveResult result = await client.ReceiveAsync(Memory<byte>.Empty, CancellationToken.None);

            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, client.CloseStatusDescription, CancellationToken.None);
        }
    }
}
