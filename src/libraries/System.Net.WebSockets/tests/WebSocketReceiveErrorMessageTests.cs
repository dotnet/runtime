// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public class WebSocketReceiveErrorMessageTests
    {
        // The test WebSocket is created with isServer:false, so received frames must NOT be masked
        // and the synthesized frames below intentionally have MASK=0.

        [Fact]
        public async Task ReceiveAsync_InvalidUtf8TextPayload_ThrowsWithInvalidTextPayloadMessage()
        {
            // FIN=1, Text(0x1), MASK=0, payload length=2, payload=0xC3 0x28 (invalid UTF-8)
            byte[] invalidTextFrame = { 0x81, 0x02, 0xC3, 0x28 };

            WebSocketException ex = await ReceiveSyntheticFrameAsync(invalidTextFrame);

            Assert.Equal(WebSocketError.Faulted, ex.WebSocketErrorCode);
            Assert.Equal(SR.net_Websockets_InvalidTextPayload, ex.Message);
        }

        [Fact]
        public async Task ReceiveAsync_CloseFrameWithPayloadLength1_ThrowsWithProtocolViolationMessage()
        {
            // FIN=1, Close(0x8), MASK=0, payload length=1, payload=0x00
            // A close frame's payload must be either 0 or >= 2 bytes.
            byte[] invalidCloseFrame = { 0x88, 0x01, 0x00 };

            WebSocketException ex = await ReceiveSyntheticFrameAsync(invalidCloseFrame);

            Assert.Equal(WebSocketError.Faulted, ex.WebSocketErrorCode);
            Assert.Equal(SR.net_Websockets_ProtocolViolation, ex.Message);
        }

        [Fact]
        public async Task ReceiveAsync_CloseFrameWithInvalidStatusCode_ThrowsWithInvalidCloseStatusMessage()
        {
            // FIN=1, Close(0x8), MASK=0, payload length=2, status=999 (reserved/invalid)
            byte[] invalidStatusCloseFrame = { 0x88, 0x02, 0x03, 0xE7 };

            WebSocketException ex = await ReceiveSyntheticFrameAsync(invalidStatusCloseFrame);

            Assert.Equal(WebSocketError.Faulted, ex.WebSocketErrorCode);
            Assert.Equal(SR.net_Websockets_InvalidCloseStatusCodeReceived, ex.Message);
        }

        [Fact]
        public async Task ReceiveAsync_CloseFrameWithInvalidUtf8Description_ThrowsWithInvalidCloseDescriptionMessage()
        {
            // FIN=1, Close(0x8), MASK=0, payload length=4, status=1000 (NormalClosure),
            // description bytes 0xC3 0x28 (invalid UTF-8).
            byte[] invalidDescriptionCloseFrame = { 0x88, 0x04, 0x03, 0xE8, 0xC3, 0x28 };

            WebSocketException ex = await ReceiveSyntheticFrameAsync(invalidDescriptionCloseFrame);

            Assert.Equal(WebSocketError.Faulted, ex.WebSocketErrorCode);
            Assert.Equal(SR.net_Websockets_InvalidCloseDescriptionPayload, ex.Message);
            Assert.IsType<Text.DecoderFallbackException>(ex.InnerException);
        }

        private static async Task<WebSocketException> ReceiveSyntheticFrameAsync(byte[] frame)
        {
            using var stream = new WebSocketTestStream();
            using WebSocket webSocket = WebSocket.CreateFromStream(stream, isServer: false, subProtocol: null, Timeout.InfiniteTimeSpan);

            stream.Remote.Enqueue(frame);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            return await Assert.ThrowsAsync<WebSocketException>(() =>
                webSocket.ReceiveAsync(new byte[1024], cts.Token));
        }
    }
}
