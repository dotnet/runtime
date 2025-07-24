// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Tests;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public abstract class WebSocketStreamTests : ConnectedStreamConformanceTests
    {
        protected override bool BlocksOnZeroByteReads => true;
        protected override bool FlushRequiredToWriteData => false;
        protected override bool ReadsReadUntilSizeOrEof => false;
        protected override bool UsableAfterCanceledReads => false;
        protected override Type UnsupportedConcurrentExceptionType => null;

        protected static (WebSocket webSocket1, WebSocket webSocket2) CreateWebSockets()
        {
            (Stream stream1, Stream stream2) = ConnectedStreams.CreateBidirectional();

            WebSocket webSocket1 = WebSocket.CreateFromStream(stream1, isServer: false, null, Timeout.InfiniteTimeSpan);
            WebSocket webSocket2 = WebSocket.CreateFromStream(stream2, isServer: true, null, Timeout.InfiniteTimeSpan);

            return (webSocket1, webSocket2);
        }
    }

    public sealed class WebSocketStreamCreateTests : WebSocketStreamTests
    {
        protected override Task<StreamPair> CreateConnectedStreamsAsync()
        {
            (WebSocket webSocket1, WebSocket webSocket2) = CreateWebSockets();
            return Task.FromResult(new StreamPair(
                WebSocketStream.Create(webSocket1, WebSocketMessageType.Binary, TimeSpan.FromSeconds(120)),
                WebSocketStream.Create(webSocket2, WebSocketMessageType.Binary, TimeSpan.FromSeconds(120))));
        }

        [Fact]
        public void Create_InvalidArgs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("webSocket", () => WebSocketStream.Create(null, WebSocketMessageType.Binary));
            AssertExtensions.Throws<ArgumentNullException>("webSocket", () => WebSocketStream.Create(null, WebSocketMessageType.Text, ownsWebSocket: true));

            AssertExtensions.Throws<ArgumentNullException>("webSocket", () => WebSocketStream.Create(null, WebSocketMessageType.Text, TimeSpan.FromSeconds(30)));

            WebSocket webSocket = WebSocket.CreateFromStream(new MemoryStream(), new());

            AssertExtensions.Throws<ArgumentOutOfRangeException>("closeTimeout", () => WebSocketStream.Create(webSocket, WebSocketMessageType.Text, TimeSpan.FromSeconds(-2)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("closeTimeout", () => WebSocketStream.Create(webSocket, WebSocketMessageType.Text, TimeSpan.FromSeconds(-1)));
            AssertExtensions.Throws<ArgumentException>("writeMessageType", () => WebSocketStream.CreateWritableMessageStream(webSocket, WebSocketMessageType.Close));

            Assert.NotNull(WebSocketStream.Create(webSocket, WebSocketMessageType.Text, Timeout.InfiniteTimeSpan));
            Assert.NotNull(WebSocketStream.Create(webSocket, WebSocketMessageType.Text, TimeSpan.Zero));
            Assert.NotNull(WebSocketStream.Create(webSocket, WebSocketMessageType.Text, TimeSpan.FromSeconds(1)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(true)]
        public void Create_Roundtrips(bool? ownsWebSocket)
        {
            (WebSocket webSocket1, _) = CreateWebSockets();

            WebSocketStream stream = ownsWebSocket is not null ?
                WebSocketStream.Create(webSocket1, WebSocketMessageType.Text, ownsWebSocket.Value) :
                WebSocketStream.Create(webSocket1, WebSocketMessageType.Text);

            Assert.Same(webSocket1, stream.WebSocket);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Dispose_ClosesWebSocketIfOwned(bool? ownsWebSocket)
        {
            (WebSocket webSocket1, WebSocket webSocket2) = CreateWebSockets();

            WebSocketStream stream = ownsWebSocket is not null ?
                WebSocketStream.Create(webSocket1, WebSocketMessageType.Text, ownsWebSocket.Value) :
                WebSocketStream.Create(webSocket1, WebSocketMessageType.Text);
            Assert.Equal(WebSocketState.Open, webSocket1.State);

            if (ownsWebSocket is true)
            {
                await Task.WhenAll(
                    stream.DisposeAsync().AsTask(),
                    webSocket2.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None));

                Assert.Equal(WebSocketState.Closed, webSocket1.State);
            }
            else
            {
                stream.Dispose();
                Assert.Equal(WebSocketState.Open, webSocket1.State);
            }
        }

        [Fact]
        public async Task DisposeWebSocket_CantReadOrWrite()
        {
            (WebSocket webSocket, _) = CreateWebSockets();

            Stream stream1 = WebSocketStream.Create(webSocket, WebSocketMessageType.Text, ownsWebSocket: false);
            Stream stream2 = WebSocketStream.CreateWritableMessageStream(webSocket, WebSocketMessageType.Binary);
            Stream stream3 = WebSocketStream.CreateReadableMessageStream(webSocket);

            Assert.False(stream1.CanSeek);
            Assert.True(stream1.CanRead);
            Assert.True(stream1.CanWrite);

            Assert.False(stream1.CanSeek);
            Assert.False(stream2.CanRead);
            Assert.True(stream2.CanWrite);

            Assert.False(stream1.CanSeek);
            Assert.True(stream3.CanRead);
            Assert.False(stream3.CanWrite);

            webSocket.Dispose();

            foreach (Stream stream in new[] { stream1, stream2, stream3 })
            {
                Assert.False(stream.CanSeek);
                Assert.False(stream.CanRead);
                Assert.False(stream.CanWrite);

                Assert.Throws<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));
                Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
                Assert.Throws<NotSupportedException>(() => stream.ReadByte());
                Assert.Throws<NotSupportedException>(() => stream.WriteByte(0));
                await Assert.ThrowsAsync<NotSupportedException>(async () => await stream.ReadAsync(new byte[1], 0, 1, CancellationToken.None));
                await Assert.ThrowsAsync<NotSupportedException>(async () => await stream.WriteAsync(new byte[1], 0, 1, CancellationToken.None));
                await Assert.ThrowsAsync<NotSupportedException>(async () => await stream.ReadAsync(new Memory<byte>(new byte[1]), CancellationToken.None));
                await Assert.ThrowsAsync<NotSupportedException>(async () => await stream.WriteAsync(new ReadOnlyMemory<byte>(new byte[1]), CancellationToken.None));
            }
        }

        [Theory]
        [InlineData(WebSocketMessageType.Binary)]
        [InlineData(WebSocketMessageType.Text)]
        public async Task Write_EveryWriteProducesMessage(WebSocketMessageType messageType)
        {
            (WebSocket webSocket1, WebSocket webSocket2) = CreateWebSockets();

            WebSocketStream stream1 = WebSocketStream.Create(webSocket1, messageType);

            Memory<byte> buffer = new byte[10];
            for (int i = 0; i < 3; i++)
            {
                buffer.Span.Clear();

                stream1.Write("hello"u8);
                ValueWebSocketReceiveResult message = await webSocket2.ReceiveAsync(buffer, default);
                Assert.True(message.EndOfMessage);
                Assert.Equal(messageType, message.MessageType);
                Assert.Equal(5, message.Count);
                Assert.Equal("hello"u8, buffer.Span.Slice(0, 5));
            }
        }

        [Fact]
        public async Task ClosedSocket_Reads0()
        {
            (WebSocket webSocket1, WebSocket webSocket2) = CreateWebSockets();

            using WebSocketStream stream2 = WebSocketStream.Create(webSocket2, WebSocketMessageType.Text, ownsWebSocket: true);

            var read = stream2.ReadAsync(new byte[1], 0, 1, CancellationToken.None);

            await webSocket1.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, default);

            Assert.Equal(0, await read);
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(false, 1)]
        [InlineData(true, 0)]
        [InlineData(true, 1)]
        public async Task Dispose_TimeoutApplies(bool useAsync, int timeoutSeconds)
        {
            (WebSocket webSocket1, _) = CreateWebSockets();

            WebSocketStream stream = WebSocketStream.Create(webSocket1, WebSocketMessageType.Text, TimeSpan.FromSeconds(timeoutSeconds));

            if (useAsync)
            {
                await stream.DisposeAsync();
            }
            else
            {
                stream.Dispose();
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Dispose_InfiniteTimeout(bool useAsync)
        {
            (WebSocket webSocket1, WebSocket webSocket2) = CreateWebSockets();

            WebSocketStream stream = WebSocketStream.Create(webSocket1, WebSocketMessageType.Text, Timeout.InfiniteTimeSpan);

            Task disposeTask = Task.Run(async () =>
            {
                if (useAsync)
                {
                    await stream.DisposeAsync();
                }
                else
                {
                    stream.Dispose();
                }
            });

            await Task.Delay(100);
            Assert.False(disposeTask.IsCompleted);

            await webSocket2.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            await disposeTask;
        }
    }

    public sealed class WebSocketStreamCreateMessageTests : WebSocketStreamTests
    {
        protected override Task<StreamPair> CreateConnectedStreamsAsync()
        {
            (WebSocket webSocket1, WebSocket webSocket2) = CreateWebSockets();
            return Task.FromResult(new StreamPair(
                WebSocketStream.CreateWritableMessageStream(webSocket2, WebSocketMessageType.Binary),
                WebSocketStream.CreateReadableMessageStream(webSocket1)));
        }

        [Fact]
        public void Create_InvalidArgs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("webSocket", () => WebSocketStream.CreateWritableMessageStream(null, WebSocketMessageType.Binary));
            AssertExtensions.Throws<ArgumentNullException>("webSocket", () => WebSocketStream.CreateReadableMessageStream(null));

            WebSocket webSocket = WebSocket.CreateFromStream(new MemoryStream(), new());
            AssertExtensions.Throws<ArgumentException>("writeMessageType", () => WebSocketStream.CreateWritableMessageStream(webSocket, WebSocketMessageType.Close));
        }

        [Fact]
        public void Create_Roundtrips()
        {
            (WebSocket webSocket, _) = CreateWebSockets();
            WebSocketStream stream;

            stream = WebSocketStream.CreateWritableMessageStream(webSocket, WebSocketMessageType.Text);
            Assert.Same(webSocket, stream.WebSocket);
            stream.Dispose();
            Assert.Same(webSocket, stream.WebSocket);
            Assert.Equal(WebSocketState.Open, webSocket.State);

            stream = WebSocketStream.CreateReadableMessageStream(webSocket);
            Assert.Same(webSocket, stream.WebSocket);
            stream.Dispose(); // For read message stream, disposing is equal to cancelling a read operation
            Assert.Same(webSocket, stream.WebSocket);
            Assert.Equal(WebSocketState.Aborted, webSocket.State);
        }

        [Theory]
        [InlineData(WebSocketMessageType.Binary)]
        [InlineData(WebSocketMessageType.Text)]
        public async Task Write_EveryStreamProducesMessage(WebSocketMessageType messageType)
        {
            (WebSocket webSocket1, WebSocket webSocket2) = CreateWebSockets();

            ValueWebSocketReceiveResult message;
            Memory<byte> buffer = new byte[10];
            for (int i = 0; i < 3; i++)
            {
                buffer.Span.Clear();

                using (WebSocketStream stream1 = WebSocketStream.CreateWritableMessageStream(webSocket1, messageType))
                {
                    foreach (byte b in "hello"u8.ToArray())
                    {
                        stream1.WriteByte(b);

                        message = await webSocket2.ReceiveAsync(buffer, default);
                        Assert.False(message.EndOfMessage);
                        Assert.Equal(messageType, message.MessageType);
                        Assert.Equal(1, message.Count);
                        Assert.Equal(b, buffer.Span[0]);
                    }
                }

                message = await webSocket2.ReceiveAsync(buffer, default);
                Assert.True(message.EndOfMessage);
                Assert.Equal(messageType, message.MessageType);
                Assert.Equal(0, message.Count);
            }
        }

        [Theory]
        [InlineData(WebSocketMessageType.Binary)]
        [InlineData(WebSocketMessageType.Text)]
        public async Task Read_EveryStreamConsumesMessage(WebSocketMessageType messageType)
        {
            (WebSocket webSocket1, WebSocket webSocket2) = CreateWebSockets();

            Memory<byte> buffer = new byte[10];
            for (int i = 0; i < 3; i++)
            {
                buffer.Span.Clear();

                using (WebSocketStream stream1 = WebSocketStream.CreateReadableMessageStream(webSocket2))
                {
                    foreach (byte b in "hello"u8.ToArray())
                    {
                        await webSocket1.SendAsync(new[] { b }, messageType, endOfMessage: false, default);
                        Assert.Equal(b, stream1.ReadByte());
                    }

                    await webSocket1.SendAsync(Array.Empty<byte>(), messageType, endOfMessage: true, default);
                    Assert.Equal(-1, stream1.ReadByte());
                }
            }
        }

        [Theory]
        [InlineData(false, false, WebSocketState.Aborted)] // abortive: read canceled
        [InlineData(true,  false, WebSocketState.Open)]    // graceful: EOF consumed
        [InlineData(false, true, WebSocketState.CloseReceived)] // graceful: Close frame consumed
        [InlineData(true,  true, WebSocketState.Open)]     // graceful: EOF consumed, Close frame NOT consumed (no reads after EOF)
        public async Task Read_DisposeBeforeEofOrCloseIsAbortive(bool eof, bool close, WebSocketState expectedWebSocketState)
        {
            (WebSocket webSocket1, WebSocket webSocket2) = CreateWebSockets();

            byte[] data = "hello"u8.ToArray();
            await webSocket1.SendAsync(data, WebSocketMessageType.Binary, endOfMessage: false, default);

            if (eof)
            {
                await webSocket1.SendAsync(Array.Empty<byte>(), WebSocketMessageType.Binary, endOfMessage: true, default);
            }

            if (close)
            {
                await webSocket1.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, default);
            }

            WebSocketStream stream2 = WebSocketStream.CreateReadableMessageStream(webSocket2);
            Memory<byte> buffer = new byte[10];

            await stream2.ReadExactlyAsync(buffer[..data.Length], default);
            Assert.Equal(data, buffer[..data.Length].ToArray());
            Assert.Equal(WebSocketState.Open, webSocket2.State);

            if (eof || close)
            {
                Assert.Equal(-1, stream2.ReadByte()); // consuming EOF or Close
            }

            stream2.Dispose();
            Assert.Equal(expectedWebSocketState, webSocket2.State);
        }

        [Theory]
        [InlineData(WebSocketMessageType.Binary, 0)]
        [InlineData(WebSocketMessageType.Binary, 1)]
        [InlineData(WebSocketMessageType.Binary, 5)]
        [InlineData(WebSocketMessageType.Text, 0)]
        [InlineData(WebSocketMessageType.Text, 1)]
        [InlineData(WebSocketMessageType.Text, 5)]
        public async Task WriteRead_StreamPairPerMessage(WebSocketMessageType messageType, int length)
        {
            (WebSocket webSocket1, WebSocket webSocket2) = CreateWebSockets();
            IEnumerable<byte> source = Enumerable.Range('a', length).Select(c => (byte)c);

            for (int i = 0; i < 3; i++)
            {
                await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        using WebSocketStream stream1 = WebSocketStream.CreateWritableMessageStream(webSocket1, messageType);
                        foreach (byte b in source)
                        {
                            await stream1.WriteAsync([b], 0, 1, default);
                            await Task.Delay(1);
                        }
                    }),
                    Task.Run(async () =>
                    {
                        using WebSocketStream stream2 = WebSocketStream.CreateReadableMessageStream(webSocket2);
                        Memory<byte> buffer = new byte[length * 2 + 1];
                        int bytesRead = await stream2.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false);

                        Assert.Equal(length, bytesRead);
                        Assert.Equal(source.ToArray(), buffer.Slice(0, bytesRead).ToArray());
                    }));
            }
        }

        [Fact]
        public async Task ClosedSocket_Reads0()
        {
            (WebSocket webSocket1, WebSocket webSocket2) = CreateWebSockets();

            using WebSocketStream stream2 = WebSocketStream.CreateReadableMessageStream(webSocket2);

            var read = stream2.ReadAsync(new byte[1], 0, 1, CancellationToken.None);

            await webSocket1.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, default);

            Assert.Equal(0, await read);
        }
    }
}
