// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public sealed class WebSocketTests : WebSocketCreateTest
    {
        protected override WebSocket CreateFromStream(Stream stream, bool isServer, string subProtocol, TimeSpan keepAliveInterval) =>
            WebSocket.CreateFromStream(stream, isServer, subProtocol, keepAliveInterval);

        [Fact]
        public static void DefaultKeepAliveInterval_ValidValue()
        {
            Assert.True(WebSocket.DefaultKeepAliveInterval > TimeSpan.Zero);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public static void CreateClientBuffer_InvalidSendValues(int size)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sendBufferSize", () => WebSocket.CreateClientBuffer(256, size));
        }

        [Theory]
        [InlineData(16)]
        [InlineData(64 * 1024)]
        public static void CreateClientBuffer_ValidSendValues(int size)
        {
            ArraySegment<byte> buffer = WebSocket.CreateClientBuffer(256, size);
            Assert.InRange(buffer.Count, size, int.MaxValue);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public static void CreateClientBuffer_InvalidReceiveValues(int size)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("receiveBufferSize", () => WebSocket.CreateClientBuffer(size, 16));
        }

        [Theory]
        [InlineData(256)]
        [InlineData(64 * 1024)]
        public static void CreateClientBuffer_ValidReceiveValues(int size)
        {
            ArraySegment<byte> buffer = WebSocket.CreateClientBuffer(size, 16);
            Assert.InRange(buffer.Count, size, int.MaxValue);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public static void CreateServerBuffer_InvalidReceiveValues(int size)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("receiveBufferSize", () => WebSocket.CreateServerBuffer(size));
        }

        [Theory]
        [InlineData(256)]
        [InlineData(64 * 1024)]
        public static void CreateServerBuffer_ValidReceiveValues(int size)
        {
            ArraySegment<byte> buffer = WebSocket.CreateServerBuffer(size);
            Assert.InRange(buffer.Count, size, int.MaxValue);
        }

        [Fact]
        public static void CreateClientWebSocket_InvalidArguments_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => WebSocket.CreateClientWebSocket(
                null, "subProtocol", 16480, 9856, TimeSpan.FromSeconds(30), false, WebSocket.CreateClientBuffer(16480, 9856)));

            Assert.Throws<ArgumentException>(() => WebSocket.CreateClientWebSocket(
                new MemoryStream(), "    ", 16480, 9856, TimeSpan.FromSeconds(30), false, WebSocket.CreateClientBuffer(16480, 9856)));
            Assert.Throws<ArgumentException>(() => WebSocket.CreateClientWebSocket(
                new MemoryStream(), "\xFF", 16480, 9856, TimeSpan.FromSeconds(30), false, WebSocket.CreateClientBuffer(16480, 9856)));

            Assert.Throws<ArgumentOutOfRangeException>(() => WebSocket.CreateClientWebSocket(
                new MemoryStream(), "subProtocol", 0, 9856, TimeSpan.FromSeconds(30), false, WebSocket.CreateClientBuffer(16480, 9856)));
            Assert.Throws<ArgumentOutOfRangeException>(() => WebSocket.CreateClientWebSocket(
                new MemoryStream(), "subProtocol", 16480, 0, TimeSpan.FromSeconds(30), false, WebSocket.CreateClientBuffer(16480, 9856)));
            Assert.Throws<ArgumentOutOfRangeException>(() => WebSocket.CreateClientWebSocket(
                new MemoryStream(), "subProtocol", 16480, 9856, TimeSpan.FromSeconds(-2), false, WebSocket.CreateClientBuffer(16480, 9856)));
        }

        [Fact]
        public static void RegisterPrefixes_Unsupported()
        {
#pragma warning disable 0618 // Obsolete API
            Assert.Throws<PlatformNotSupportedException>(() => WebSocket.RegisterPrefixes());
#pragma warning restore 0618
        }

        [Fact]
        public static void IsApplicationTargeting45_AlwaysTrue()
        {
#pragma warning disable 0618 // Obsolete API
            Assert.True(WebSocket.IsApplicationTargeting45());
#pragma warning restore 0618
        }

        [Theory]
        [InlineData(WebSocketState.None)]
        [InlineData(WebSocketState.Connecting)]
        [InlineData(WebSocketState.Open)]
        [InlineData(WebSocketState.CloseSent)]
        [InlineData(WebSocketState.CloseReceived)]
        [InlineData((WebSocketState)(-1))]
        [InlineData((WebSocketState)(7))]
        public static void IsStateTerminal_NonTerminalReturnsFalse(WebSocketState state)
        {
            Assert.False(ExposeProtectedWebSocket.IsStateTerminal(state));
        }

        [Theory]
        [InlineData(WebSocketState.Closed)]
        [InlineData(WebSocketState.Aborted)]
        public static void IsStateTerminal_TerminalReturnsTrue(WebSocketState state)
        {
            Assert.True(ExposeProtectedWebSocket.IsStateTerminal(state));
        }

        [Theory]
        [InlineData(WebSocketState.Closed, new WebSocketState[] { })]
        [InlineData(WebSocketState.Closed, new WebSocketState[] { WebSocketState.Open })]
        [InlineData(WebSocketState.Open, new WebSocketState[] { WebSocketState.Aborted, WebSocketState.CloseSent })]
        public static void ThrowOnInvalidState_ThrowsIfNotInValidList(WebSocketState state, WebSocketState[] validStates)
        {
            WebSocketException wse = Assert.Throws<WebSocketException>(() => ExposeProtectedWebSocket.ThrowOnInvalidState(state, validStates));
            Assert.Equal(WebSocketError.InvalidState, wse.WebSocketErrorCode);
        }

        [Theory]
        [InlineData(WebSocketState.Open, new WebSocketState[] { WebSocketState.Open })]
        [InlineData(WebSocketState.Open, new WebSocketState[] { WebSocketState.Open, WebSocketState.Aborted, WebSocketState.Closed })]
        [InlineData(WebSocketState.Open, new WebSocketState[] { WebSocketState.Aborted, WebSocketState.Open, WebSocketState.Closed })]
        [InlineData(WebSocketState.Open, new WebSocketState[] { WebSocketState.Aborted, WebSocketState.CloseSent, WebSocketState.Open })]
        public static void ThrowOnInvalidState_SuccessIfInList(WebSocketState state, WebSocketState[] validStates)
        {
            ExposeProtectedWebSocket.ThrowOnInvalidState(state, validStates);
        }

        [Fact]
        public void ValueWebSocketReceiveResult_Ctor_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => new ValueWebSocketReceiveResult(-1, WebSocketMessageType.Text, true));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => new ValueWebSocketReceiveResult(int.MinValue, WebSocketMessageType.Text, true));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("messageType", () => new ValueWebSocketReceiveResult(0, (WebSocketMessageType)(-1), true));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("messageType", () => new ValueWebSocketReceiveResult(0, (WebSocketMessageType)(3), true));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("messageType", () => new ValueWebSocketReceiveResult(0, (WebSocketMessageType)(int.MinValue), true));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("messageType", () => new ValueWebSocketReceiveResult(0, (WebSocketMessageType)(int.MaxValue), true));
        }

        [Theory]
        [InlineData(0, WebSocketMessageType.Text, true)]
        [InlineData(0, WebSocketMessageType.Text, false)]
        [InlineData(42, WebSocketMessageType.Binary, false)]
        [InlineData(int.MaxValue, WebSocketMessageType.Close, false)]
        [InlineData(int.MaxValue, WebSocketMessageType.Close, true)]
        public void ValueWebSocketReceiveResult_Ctor_ValidArguments_Roundtrip(int count, WebSocketMessageType messageType, bool endOfMessage)
        {
            ValueWebSocketReceiveResult r = new ValueWebSocketReceiveResult(count, messageType, endOfMessage);
            Assert.Equal(count, r.Count);
            Assert.Equal(messageType, r.MessageType);
            Assert.Equal(endOfMessage, r.EndOfMessage);
        }

        [Fact]
        public async Task ThrowWhenContinuationWithDifferentCompressionFlags()
        {
            using WebSocket client = CreateFromStream(new MemoryStream(), isServer: false, null, TimeSpan.Zero);

            await client.SendAsync(Memory<byte>.Empty, WebSocketMessageType.Text, WebSocketMessageFlags.DisableCompression, default);
            Assert.Throws<ArgumentException>("messageFlags", () =>
               client.SendAsync(Memory<byte>.Empty, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage, default));
        }

        [Fact]
        public async Task ReceiveAsync_WhenDisposedInParallel_DoesNotGetStuck()
        {
            using var stream = new WebSocketTestStream();
            using var websocket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions());

            // Note: Calling ReceiveAsync() multiple times at once results in undefined behavior
            // per public API docs, but it is necessary to reliably verify that bug #97911 is fixed.
            Task r1 = websocket.ReceiveAsync(new Memory<byte>(new byte[1]), default).AsTask();
            Task r2 = websocket.ReceiveAsync(new Memory<byte>(new byte[1]), default).AsTask();
            Task r3 = websocket.ReceiveAsync(new Memory<byte>(new byte[1]), default).AsTask();

            websocket.Dispose();

            await Assert.ThrowsAsync<WebSocketException>(() => r1.WaitAsync(TimeSpan.FromSeconds(1)));
            await Assert.ThrowsAsync<WebSocketException>(() => r2.WaitAsync(TimeSpan.FromSeconds(1)));
            await Assert.ThrowsAsync<WebSocketException>(() => r3.WaitAsync(TimeSpan.FromSeconds(1)));
        }

        public abstract class ExposeProtectedWebSocket : WebSocket
        {
            public static new bool IsStateTerminal(WebSocketState state) =>
                WebSocket.IsStateTerminal(state);
            public static new void ThrowOnInvalidState(WebSocketState state, params WebSocketState[] validStates) =>
                WebSocket.ThrowOnInvalidState(state, validStates);
        }
    }
}
