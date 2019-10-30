// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public sealed class MemorySendReceiveTest : SendReceiveTest
    {
        public MemorySendReceiveTest(ITestOutputHelper output) : base(output) { }

        protected override async Task<WebSocketReceiveResult> ReceiveAsync(WebSocket ws, ArraySegment<byte> arraySegment, CancellationToken cancellationToken)
        {
            ValueWebSocketReceiveResult r = await ws.ReceiveAsync(
                (Memory<byte>)arraySegment,
                cancellationToken).ConfigureAwait(false);
            return new WebSocketReceiveResult(r.Count, r.MessageType, r.EndOfMessage, ws.CloseStatus, ws.CloseStatusDescription);
        }

        protected override Task SendAsync(WebSocket ws, ArraySegment<byte> arraySegment, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) =>
            ws.SendAsync(
                (ReadOnlyMemory<byte>)arraySegment,
                messageType,
                endOfMessage,
                cancellationToken).AsTask();
    }
}
