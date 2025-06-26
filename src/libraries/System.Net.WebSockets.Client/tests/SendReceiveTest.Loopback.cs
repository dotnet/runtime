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
    public abstract class SendReceiveTest_Loopback : SendReceiveTestBase
    {
        public SendReceiveTest(ITestOutputHelper output) : base(output) { }

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task SendReceive_PartialMessageDueToSmallReceiveBuffer_Success(Uri server)
            => RunClient_SendReceive_PartialMessageDueToSmallReceiveBuffer_Success(server);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task SendReceive_PartialMessageBeforeCompleteMessageArrives_Success(Uri server)
            => RunClient_SendReceive_PartialMessageBeforeCompleteMessageArrives_Success(server);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task SendAsync_SendCloseMessageType_ThrowsArgumentExceptionWithMessage(Uri server)
            => RunClient_SendAsync_SendCloseMessageType_ThrowsArgumentExceptionWithMessage(server);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task SendAsync_MultipleOutstandingSendOperations_Throws(Uri server)
            => RunClient_SendAsync_MultipleOutstandingSendOperations_Throws(server);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ReceiveAsync_MultipleOutstandingReceiveOperations_Throws(Uri server)
            => RunClient_ReceiveAsync_MultipleOutstandingReceiveOperations_Throws(server);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task SendAsync_SendZeroLengthPayloadAsEndOfMessage_Success(Uri server)
            => RunClient_SendAsync_SendZeroLengthPayloadAsEndOfMessage_Success(server);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task SendReceive_VaryingLengthBuffers_Success(Uri server)
            => RunClient_SendReceive_VaryingLengthBuffers_Success(server);

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task SendReceive_Concurrent_Success(Uri server)
            => RunClient_SendReceive_Concurrent_Success(server);

        [Fact]
        public Task SendReceive_ConnectionClosedPrematurely_ReceiveAsyncFailsAndWebSocketStateUpdated()
            => RunClient_SendReceive_ConnectionClosedPrematurely_ReceiveAsyncFailsAndWebSocketStateUpdated();

        [Theory, MemberData(nameof(UseSsl_MemberData))]
        public Task ZeroByteReceive_CompletesWhenDataAvailable(Uri server)
            => RunClient_ZeroByteReceive_CompletesWhenDataAvailable(server);
    }

        public class MemorySendReceiveTest : SendReceiveTest
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

    public class ArraySegmentSendReceiveTest : SendReceiveTest
    {
        public ArraySegmentSendReceiveTest(ITestOutputHelper output) : base(output) { }

        protected override Task<WebSocketReceiveResult> ReceiveAsync(WebSocket ws, ArraySegment<byte> arraySegment, CancellationToken cancellationToken) =>
            ws.ReceiveAsync(arraySegment, cancellationToken);

        protected override Task SendAsync(WebSocket ws, ArraySegment<byte> arraySegment, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) =>
            ws.SendAsync(arraySegment, messageType, endOfMessage, cancellationToken);
    }
}
