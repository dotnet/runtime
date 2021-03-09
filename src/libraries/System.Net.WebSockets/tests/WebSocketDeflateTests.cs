// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public class WebSocketDeflateTests
    {
        private readonly CancellationTokenSource? _cancellation;

        public WebSocketDeflateTests()
        {
            if (!Debugger.IsAttached)
            {
                _cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            }
        }

        public CancellationToken CancellationToken => _cancellation?.Token ?? default;

        public static IEnumerable<object[]> SupportedWindowBits
        {
            get
            {
                for (var i = 9; i <= 15; ++i)
                {
                    yield return new object[] { i };
                }
            }
        }

        [Fact]
        public async Task HelloWithContextTakeover()
        {
            WebSocketTestStream stream = new();
            stream.Enqueue(0xc1, 0x07, 0xf2, 0x48, 0xcd, 0xc9, 0xc9, 0x07, 0x00);
            using WebSocket websocket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                DeflateOptions = new()
            });

            Memory<byte> buffer = new byte[5];
            ValueWebSocketReceiveResult result = await websocket.ReceiveAsync(buffer, CancellationToken);

            Assert.True(result.EndOfMessage);
            Assert.Equal(buffer.Length, result.Count);
            Assert.Equal(WebSocketMessageType.Text, result.MessageType);
            Assert.Equal("Hello", Encoding.UTF8.GetString(buffer.Span));

            // Because context takeover is set by default if we try to send
            // the same message it would take fewer bytes.
            stream.Enqueue(0xc1, 0x05, 0xf2, 0x00, 0x11, 0x00, 0x00);

            buffer.Span.Clear();
            result = await websocket.ReceiveAsync(buffer, CancellationToken);

            Assert.True(result.EndOfMessage);
            Assert.Equal(buffer.Length, result.Count);
            Assert.Equal("Hello", Encoding.UTF8.GetString(buffer.Span));
        }

        [Fact]
        public async Task HelloWithoutContextTakeover()
        {
            WebSocketTestStream stream = new();
            using WebSocket websocket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                DeflateOptions = new()
                {
                    ClientContextTakeover = false
                }
            });

            Memory<byte> buffer = new byte[5];

            for (var i = 0; i < 100; ++i)
            {
                // Without context takeover the message should look the same every time
                stream.Enqueue(0xc1, 0x07, 0xf2, 0x48, 0xcd, 0xc9, 0xc9, 0x07, 0x00);
                buffer.Span.Clear();

                ValueWebSocketReceiveResult result = await websocket.ReceiveAsync(buffer, CancellationToken);

                Assert.True(result.EndOfMessage);
                Assert.Equal(buffer.Length, result.Count);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);
                Assert.Equal("Hello", Encoding.UTF8.GetString(buffer.Span));
            }
        }

        [Fact]
        public async Task TwoDeflateBlocksInOneMessage()
        {
            // Two or more DEFLATE blocks may be used in one message.
            WebSocketTestStream stream = new();
            using WebSocket websocket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                DeflateOptions = new()
            });
            // The first 3 octets(0xf2 0x48 0x05) and the least significant two
            // bits of the 4th octet(0x00) constitute one DEFLATE block with
            // "BFINAL" set to 0 and "BTYPE" set to 01 containing "He". The rest of
            // the 4th octet contains the header bits with "BFINAL" set to 0 and
            // "BTYPE" set to 00, and the 3 padding bits of 0. Together with the
            // following 4 octets(0x00 0x00 0xff 0xff), the header bits constitute
            // an empty DEFLATE block with no compression. A DEFLATE block
            // containing "llo" follows the empty DEFLATE block.
            stream.Enqueue(0x41, 0x08, 0xf2, 0x48, 0x05, 0x00, 0x00, 0x00, 0xff, 0xff);
            stream.Enqueue(0x80, 0x05, 0xca, 0xc9, 0xc9, 0x07, 0x00);

            Memory<byte> buffer = new byte[5];
            ValueWebSocketReceiveResult result = await websocket.ReceiveAsync(buffer, CancellationToken);

            Assert.Equal(2, result.Count);
            Assert.False(result.EndOfMessage);

            result = await websocket.ReceiveAsync(buffer.Slice(result.Count), CancellationToken);

            Assert.Equal(3, result.Count);
            Assert.True(result.EndOfMessage);
            Assert.Equal("Hello", Encoding.UTF8.GetString(buffer.Span));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public async Task Duplex(bool clientContextTakover, bool serverContextTakover)
        {
            WebSocketTestStream stream = new();
            using WebSocket server = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                IsServer = true,
                DeflateOptions = new WebSocketDeflateOptions
                {
                    ClientContextTakeover = clientContextTakover,
                    ServerContextTakeover = serverContextTakover
                }
            });
            using WebSocket client = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                DeflateOptions = new WebSocketDeflateOptions
                {
                    ClientContextTakeover = clientContextTakover,
                    ServerContextTakeover = serverContextTakover
                }
            });

            var buffer = new byte[1024];

            for (var i = 0; i < 10; ++i)
            {
                string message = $"Sending number {i} from server.";
                await SendTextAsync(message, server);

                ValueWebSocketReceiveResult result = await client.ReceiveAsync(buffer.AsMemory(), CancellationToken);

                Assert.True(result.EndOfMessage);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                Assert.Equal(message, Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count)));
            }

            for (var i = 0; i < 10; ++i)
            {
                string message = $"Sending number {i} from client.";
                await SendTextAsync(message, client);

                ValueWebSocketReceiveResult result = await server.ReceiveAsync(buffer.AsMemory(), CancellationToken);

                Assert.True(result.EndOfMessage);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                Assert.Equal(message, Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count)));
            }
        }

        [Theory]
        [MemberData(nameof(SupportedWindowBits))]
        public async Task LargeMessageSplitInMultipleFrames(int windowBits)
        {
            WebSocketTestStream stream = new();
            using WebSocket server = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                IsServer = true,
                DeflateOptions = new()
                {
                    ClientMaxWindowBits = windowBits
                }
            });
            using WebSocket client = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                DeflateOptions = new()
                {
                    ClientMaxWindowBits = windowBits
                }
            });

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

                    await server.SendAsync(testData.Slice(position, currentFrameSize), WebSocketMessageType.Binary, eof, CancellationToken);
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
                    int currentFrameSize = Math.Min(frameSize, testData.Length - position);
                    ValueWebSocketReceiveResult result = await client.ReceiveAsync(receivedData.Slice(position, currentFrameSize), CancellationToken);

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
        public async Task WebSocketWithoutDeflateShouldThrowOnCompressedMessage()
        {
            WebSocketTestStream stream = new();

            stream.Enqueue(0xc1, 0x07, 0xf2, 0x48, 0xcd, 0xc9, 0xc9, 0x07, 0x00);
            using WebSocket client = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions());

            var exception = await Assert.ThrowsAsync<WebSocketException>(() =>
               client.ReceiveAsync(Memory<byte>.Empty, CancellationToken).AsTask());

            Assert.Equal("The WebSocket received compressed frame when compression is not enabled.", exception.Message);
        }

        [Fact]
        public async Task ReceiveUncompressedMessageWhenCompressionEnabled()
        {
            // We should be able to handle the situation where even if we have
            // deflate compression enabled, uncompressed messages are OK
            WebSocketTestStream stream = new();
            WebSocket server = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                IsServer = true,
                DeflateOptions = null
            });
            WebSocket client = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                DeflateOptions = new WebSocketDeflateOptions()
            });

            // Server sends uncompressed 
            await SendTextAsync("Hello", server);

            // Although client has deflate options, it should still be able
            // to handle uncompressed messages.
            Assert.Equal("Hello", await ReceiveTextAsync(client));

            // Client sends compressed, but server compression is disabled and should throw on receive
            await SendTextAsync("Hello back", client);
            var exception = await Assert.ThrowsAsync<WebSocketException>(() => ReceiveTextAsync(server));
            Assert.Equal("The WebSocket received compressed frame when compression is not enabled.", exception.Message);
            Assert.Equal(WebSocketState.Aborted, server.State);

            // The client should close if we try to receive
            ValueWebSocketReceiveResult result = await client.ReceiveAsync(Memory<byte>.Empty, CancellationToken);
            Assert.Equal(WebSocketMessageType.Close, result.MessageType);
            Assert.Equal(WebSocketCloseStatus.ProtocolError, client.CloseStatus);
            Assert.Equal(WebSocketState.CloseReceived, client.State);
        }

        [Fact]
        public async Task ReceiveInvalidCompressedData()
        {
            WebSocketTestStream stream = new();
            WebSocket client = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                DeflateOptions = new WebSocketDeflateOptions()
            });

            stream.Enqueue(0xc1, 0x07, 0xf2, 0x48, 0xcd, 0xc9, 0xc9, 0x07, 0x00);
            Assert.Equal("Hello", await ReceiveTextAsync(client));

            stream.Enqueue(0xc1, 0x07, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00);
            var exception = await Assert.ThrowsAsync<WebSocketException>(() => ReceiveTextAsync(client));

            Assert.Equal("The message was compressed using an unsupported compression method.", exception.Message);
            Assert.Equal(WebSocketState.Aborted, client.State);
        }

        private ValueTask SendTextAsync(string text, WebSocket websocket)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return websocket.SendAsync(bytes.AsMemory(), WebSocketMessageType.Text, true, CancellationToken);
        }

        private async Task<string> ReceiveTextAsync(WebSocket websocket)
        {
            using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(1024 * 32);
            ValueWebSocketReceiveResult result = await websocket.ReceiveAsync(buffer.Memory, CancellationToken);

            Assert.True(result.EndOfMessage);
            Assert.Equal(WebSocketMessageType.Text, result.MessageType);

            return Encoding.UTF8.GetString(buffer.Memory.Span.Slice(0, result.Count));
        }
    }
}
