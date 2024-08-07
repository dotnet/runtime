// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

using static System.Net.Test.Common.Configuration.WebSockets;

namespace System.Net.WebSockets.Client.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "KeepAlive not supported on browser")]
    public class KeepAliveTest : ClientWebSocketTestBase
    {
        public KeepAliveTest(ITestOutputHelper output) : base(output) { }

        [ConditionalFact(nameof(WebSocketsSupported))]
        [OuterLoop("Uses Task.Delay")]
        public async Task KeepAlive_LongDelayBetweenSendReceives_Succeeds()
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(RemoteEchoServer, TimeOutMilliseconds, _output, TimeSpan.FromSeconds(1)))
            {
                await cws.SendAsync(new ArraySegment<byte>(new byte[1] { 42 }), WebSocketMessageType.Binary, true, CancellationToken.None);

                await Task.Delay(TimeSpan.FromSeconds(10));

                byte[] receiveBuffer = new byte[1];
                Assert.Equal(1, (await cws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None)).Count);
                Assert.Equal(42, receiveBuffer[0]);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "KeepAlive_LongDelayBetweenSendReceives_Succeeds", CancellationToken.None);
            }
        }

        [ConditionalTheory(nameof(WebSocketsSupported))]
        [OuterLoop("Uses Task.Delay")]
        [InlineData(1, 0)] // unsolicited pong
        [InlineData(1, 2)] // ping/pong
        public async Task KeepAlive_LongDelayBetweenReceiveSends_Succeeds(int keepAliveIntervalSec, int keepAliveTimeoutSec)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(
                RemoteEchoServer,
                TimeOutMilliseconds,
                _output,
                options =>
                {
                    options.KeepAliveInterval = TimeSpan.FromSeconds(keepAliveIntervalSec);
                    options.KeepAliveTimeout = TimeSpan.FromSeconds(keepAliveTimeoutSec);
                }))
            {
                byte[] receiveBuffer = new byte[1];
                var receiveTask = cws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None); // this will wait until we trigger the echo server by sending a message

                await Task.Delay(TimeSpan.FromSeconds(10));

                await cws.SendAsync(new ArraySegment<byte>(new byte[1] { 42 }), WebSocketMessageType.Binary, true, CancellationToken.None);

                Assert.Equal(1, (await receiveTask).Count);
                Assert.Equal(42, receiveBuffer[0]);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "KeepAlive_LongDelayBetweenSendReceives_Succeeds", CancellationToken.None);
            }
        }
    }
}
