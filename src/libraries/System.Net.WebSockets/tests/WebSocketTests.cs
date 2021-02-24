// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Threading;
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
            Assert.Throws<PlatformNotSupportedException>(() => WebSocket.RegisterPrefixes());
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
            if (PlatformDetection.IsNetCore) // bug fix in netcoreapp: https://github.com/dotnet/corefx/pull/35960
            {
                Assert.Equal(WebSocketError.InvalidState, wse.WebSocketErrorCode);
            }
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

        [Theory]
        [InlineData(0)]
        [InlineData(125)]
        [InlineData(ushort.MaxValue)]
        [InlineData(ushort.MaxValue * 2)]
        public async Task SendUncompressedClientMessage(int messageSize)
        {
            var stream = new WebSocketTestStream();
            using WebSocket server = CreateFromStream(stream, isServer: true, null, Timeout.InfiniteTimeSpan);
            using WebSocket client = CreateFromStream(stream.Remote, isServer: false, null, Timeout.InfiniteTimeSpan);

            var message = new byte[messageSize];
            new Random(0).NextBytes(message);

            await client.SendAsync(message, WebSocketMessageType.Binary, true, default);

            var buffer = new byte[messageSize];
            var result = await server.ReceiveAsync(buffer, default);

            Assert.Equal(messageSize, result.Count);
            Assert.True(result.EndOfMessage);
            Assert.True(message.AsSpan().SequenceEqual(buffer));
        }

        [Fact]
        public async Task WhenPingReceivedPongMessageMustBeSent()
        {
            var stream = new WebSocketTestStream();
            using WebSocket server = CreateFromStream(stream, isServer: true, null, Timeout.InfiniteTimeSpan);
            using var cancellation = new CancellationTokenSource();

            stream.Enqueue(0b1000_1001, 0x00);
            var receiveTask = server.ReceiveAsync(Memory<byte>.Empty, cancellation.Token).AsTask();

            Assert.Equal(0, stream.Available);
            Assert.Equal(2, stream.Remote.Available);
            Assert.Equal<byte>(new byte[] { 0b1000_1010, 0x00 }, stream.Remote.NextAvailableBytes.ToArray());

            cancellation.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await receiveTask.ConfigureAwait(false));
        }

        [Fact]
        public async Task WhenPongReceivedNothingShouldBeSentBack()
        {
            var stream = new WebSocketTestStream();
            using WebSocket client = CreateFromStream(stream, isServer: false, null, Timeout.InfiniteTimeSpan);

            using var cancellation = new CancellationTokenSource();

            stream.Enqueue(0b1000_1010, 0x00);
            var receiveTask = client.ReceiveAsync(Memory<byte>.Empty, cancellation.Token).AsTask();

            Assert.Equal(0, stream.Available);
            Assert.Equal(0, stream.Remote.Available);

            cancellation.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await receiveTask.ConfigureAwait(false));
        }

        [Fact]
        public async Task ClosingWebSocketsGracefully()
        {
            var stream = new WebSocketTestStream();
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            using WebSocket client = CreateFromStream(stream, isServer: false, null, Timeout.InfiniteTimeSpan);
            using WebSocket server = CreateFromStream(stream.Remote, isServer: true, null, Timeout.InfiniteTimeSpan);

            var clientClose = client.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Yeet", cancellation.Token);
            var result = await server.ReceiveAsync(Memory<byte>.Empty, cancellation.Token);

            Assert.True(result.EndOfMessage);
            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
            Assert.Equal(0, result.Count);
            Assert.Equal("Yeet", server.CloseStatusDescription);
            Assert.Equal(WebSocketCloseStatus.PolicyViolation, server.CloseStatus);

            await server.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellation.Token);
            await clientClose;

            Assert.Equal(WebSocketState.Closed, server.State);
            Assert.Equal(WebSocketState.Closed, client.State);
        }

        [Fact]
        public async Task LargeMessageSplitInMultipleFrames()
        {
            var stream = new WebSocketTestStream();
            using WebSocket server = CreateFromStream(stream, isServer: true, null, Timeout.InfiniteTimeSpan);
            using WebSocket client = CreateFromStream(stream.Remote, isServer: false, null, Timeout.InfiniteTimeSpan);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            Memory<byte> testData = new byte[ushort.MaxValue];
            Memory<byte> receivedData = new byte[testData.Length];

            // Make the data incompressible to make sure that the output is larger than the input
            var rng = new Random(0);
            rng.NextBytes(testData.Span);

            // Test it a few times with different frame sizes
            for (var i = 0; i < 10; ++i)
            {
                var frameSize = rng.Next(1024, 2048);
                var position = 0;

                while (position < testData.Length)
                {
                    var currentFrameSize = Math.Min(frameSize, testData.Length - position);
                    var eof = position + currentFrameSize == testData.Length;

                    await server.SendAsync(testData.Slice(position, currentFrameSize), WebSocketMessageType.Binary, eof, cancellation.Token);
                    position += currentFrameSize;
                }

                Assert.True(testData.Length < stream.Remote.Available, "The compressed data should be bigger.");
                Assert.Equal(testData.Length, position);

                // Receive the data from the client side
                receivedData.Span.Clear();
                position = 0;

                // Intentionally receive with a frame size that is less than what the sender used
                frameSize /= 3;

                while (true)
                {
                    var currentFrameSize = Math.Min(frameSize, testData.Length - position);
                    var result = await client.ReceiveAsync(receivedData.Slice(position, currentFrameSize), cancellation.Token);

                    Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
                    position += result.Count;

                    if (result.EndOfMessage)
                        break;
                }

                Assert.Equal(0, stream.Remote.Available);
                Assert.Equal(testData.Length, position);
                Assert.True(testData.Span.SequenceEqual(receivedData.Span));
            }
        }

        [Fact]
        public async Task Duplex()
        {
            var stream = new WebSocketTestStream();
            using WebSocket server = CreateFromStream(stream, isServer: true, null, Timeout.InfiniteTimeSpan);
            using WebSocket client = CreateFromStream(stream.Remote, isServer: false, null, Timeout.InfiniteTimeSpan);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var buffer = new byte[1024];

            for (var i = 0; i < 10; ++i)
            {
                var message = $"Sending number {i} from server.";
                if (i >= 5)
                {
                    // Because the code is optimized when tasks complete synchronously,
                    // cause the next send to complete asynchronously.
                    stream.Remote.DelayForNextSend = TimeSpan.FromMilliseconds(1);
                }
                await server.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, cancellation.Token);
                var result = await client.ReceiveAsync(buffer.AsMemory(), cancellation.Token);

                Assert.True(result.EndOfMessage);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                Assert.Equal(message, Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count)));
            }

            for (var i = 0; i < 10; ++i)
            {
                var message = $"Sending number {i} from client.";
                if (i >= 5)
                {
                    // Because the code is optimized when tasks complete synchronously,
                    // cause the next send to complete asynchronously.
                    stream.DelayForNextSend = TimeSpan.FromMilliseconds(1);
                }
                await client.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, cancellation.Token);
                var result = await server.ReceiveAsync(buffer.AsMemory(), cancellation.Token);

                Assert.True(result.EndOfMessage);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                Assert.Equal(message, Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count)));
            }
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
