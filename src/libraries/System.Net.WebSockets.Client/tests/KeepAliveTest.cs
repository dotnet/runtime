// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public class KeepAliveTest : ClientWebSocketTestBase
    {
        public KeepAliveTest(ITestOutputHelper output) : base(output) { }

        [ActiveIssue(23204, TargetFrameworkMonikers.Uap)]
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
    }
}
