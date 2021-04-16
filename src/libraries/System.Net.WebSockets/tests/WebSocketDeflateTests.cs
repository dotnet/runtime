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
        public async Task ReceiveHelloWithContextTakeover()
        {
            WebSocketTestStream stream = new();
            stream.Enqueue(0xc1, 0x07, 0xf2, 0x48, 0xcd, 0xc9, 0xc9, 0x07, 0x00);
            using WebSocket websocket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                DangerousDeflateOptions = new()
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
        public async Task SendHelloWithContextTakeover()
        {
            WebSocketTestStream stream = new();
            using WebSocket websocket = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                IsServer = true,
                DangerousDeflateOptions = new()
            });

            await websocket.SendAsync(Encoding.UTF8.GetBytes("Hello"), WebSocketMessageType.Text, true, CancellationToken);
            Assert.Equal("C107F248CDC9C90700", Convert.ToHexString(stream.NextAvailableBytes));

            stream.Clear();
            await websocket.SendAsync(Encoding.UTF8.GetBytes("Hello"), WebSocketMessageType.Text, true, CancellationToken);

            // Because context takeover is set by default if we try to send
            // the same message it should result in fewer bytes.
            Assert.Equal("C105F200110000", Convert.ToHexString(stream.NextAvailableBytes));
        }

        [Fact]
        public async Task SendHelloWithDisableCompression()
        {
            WebSocketTestStream stream = new();
            using WebSocket websocket = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                IsServer = true,
                DangerousDeflateOptions = new()
            });

            byte[] bytes = Encoding.UTF8.GetBytes("Hello");
            WebSocketMessageFlags flags = WebSocketMessageFlags.DisableCompression | WebSocketMessageFlags.EndOfMessage;
            await websocket.SendAsync(bytes, WebSocketMessageType.Text, flags, CancellationToken);

            Assert.Equal(bytes.Length + 2, stream.Available);
            Assert.True(stream.NextAvailableBytes.EndsWith(bytes));
        }

        [Fact]
        public async Task SendHelloWithEmptyFrame()
        {
            WebSocketTestStream stream = new();
            using WebSocket websocket = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                IsServer = true,
                DangerousDeflateOptions = new()
            });

            byte[] bytes = Encoding.UTF8.GetBytes("Hello");
            await websocket.SendAsync(Memory<byte>.Empty, WebSocketMessageType.Text, endOfMessage: false, CancellationToken);
            await websocket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken);

            using WebSocket client = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                IsServer = false,
                DangerousDeflateOptions = new()
            });

            ValueWebSocketReceiveResult result = await client.ReceiveAsync(bytes.AsMemory(), CancellationToken);
            Assert.False(result.EndOfMessage);
            Assert.Equal(0, result.Count);

            result = await client.ReceiveAsync(bytes.AsMemory(), CancellationToken);
            Assert.True(result.EndOfMessage);
            Assert.Equal(5, result.Count);
        }

        [Fact]
        public async Task ReceiveHelloWithoutContextTakeover()
        {
            WebSocketTestStream stream = new();
            using WebSocket websocket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                DangerousDeflateOptions = new()
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
        public async Task SendHelloWithoutContextTakeover()
        {
            WebSocketTestStream stream = new();
            using WebSocket websocket = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                IsServer = true,
                DangerousDeflateOptions = new()
                {
                    ServerContextTakeover = false
                }
            });

            Memory<byte> buffer = new byte[5];

            for (var i = 0; i < 100; ++i)
            {
                await websocket.SendAsync(Encoding.UTF8.GetBytes("Hello"), WebSocketMessageType.Text, true, CancellationToken);

                // Without context takeover the message should look the same every time
                Assert.Equal("C107F248CDC9C90700", Convert.ToHexString(stream.NextAvailableBytes));
                stream.Clear();
            }
        }

        [Fact]
        public async Task TwoDeflateBlocksInOneMessage()
        {
            // Two or more DEFLATE blocks may be used in one message.
            WebSocketTestStream stream = new();
            using WebSocket websocket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                DangerousDeflateOptions = new()
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
                DangerousDeflateOptions = new WebSocketDeflateOptions
                {
                    ClientContextTakeover = clientContextTakover,
                    ServerContextTakeover = serverContextTakover
                }
            });
            using WebSocket client = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                DangerousDeflateOptions = new WebSocketDeflateOptions
                {
                    ClientContextTakeover = clientContextTakover,
                    ServerContextTakeover = serverContextTakover
                }
            });

            var buffer = new byte[1024];

            for (var i = 0; i < 10; ++i)
            {
                string message = $"Sending number {i} from server.";
                await SendTextAsync(message, server, disableCompression: i % 2 == 0);

                ValueWebSocketReceiveResult result = await client.ReceiveAsync(buffer.AsMemory(), CancellationToken);

                Assert.True(result.EndOfMessage);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                Assert.Equal(message, Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count)));
            }

            for (var i = 0; i < 10; ++i)
            {
                string message = $"Sending number {i} from client.";
                await SendTextAsync(message, client, disableCompression: i % 2 == 0);

                ValueWebSocketReceiveResult result = await server.ReceiveAsync(buffer.AsMemory(), CancellationToken);

                Assert.True(result.EndOfMessage);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                Assert.Equal(message, Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count)));
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50235")]
        public async Task LargeMessageSplitInMultipleFramesActiveIssue()
        {
            // This test is exactly the same as LargeMessageSplitInMultipleFrames, but
            // for the data seed it uses Random(0) where the other uses Random(10). This is done
            // only because it was found that there is a bug in the deflate somewhere and it only appears
            // so far when using 10 window bits and data generated using Random(0). Once
            // the issue is resolved this test can be deleted and LargeMessageSplitInMultipleFrames should be
            // updated to use Random(0).
            WebSocketTestStream stream = new();
            using WebSocket server = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                IsServer = true,
                DangerousDeflateOptions = new()
                {
                    ClientMaxWindowBits = 10
                }
            });
            using WebSocket client = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                DangerousDeflateOptions = new()
                {
                    ClientMaxWindowBits = 10
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

        [Theory]
        [MemberData(nameof(SupportedWindowBits))]
        public async Task LargeMessageSplitInMultipleFrames(int windowBits)
        {
            WebSocketTestStream stream = new();
            using WebSocket server = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                IsServer = true,
                DangerousDeflateOptions = new()
                {
                    ClientMaxWindowBits = windowBits
                }
            });
            using WebSocket client = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                DangerousDeflateOptions = new()
                {
                    ClientMaxWindowBits = windowBits
                }
            });

            Memory<byte> testData = new byte[ushort.MaxValue];
            Memory<byte> receivedData = new byte[testData.Length];

            // Make the data incompressible to make sure that the output is larger than the input
            var rng = new Random(10);
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
                DangerousDeflateOptions = null
            });
            WebSocket client = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                DangerousDeflateOptions = new WebSocketDeflateOptions()
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
                DangerousDeflateOptions = new WebSocketDeflateOptions()
            });

            stream.Enqueue(0xc1, 0x07, 0xf2, 0x48, 0xcd, 0xc9, 0xc9, 0x07, 0x00);
            Assert.Equal("Hello", await ReceiveTextAsync(client));

            stream.Enqueue(0xc1, 0x07, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00);
            var exception = await Assert.ThrowsAsync<WebSocketException>(() => ReceiveTextAsync(client));

            Assert.Equal("The message was compressed using an unsupported compression method.", exception.Message);
            Assert.Equal(WebSocketState.Aborted, client.State);
        }

        [Fact]
        public async Task PayloadShouldHaveSimilarSizeWhenSplitIntoSegments()
        {
            WebSocketTestStream stream = new();
            WebSocket client = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                DangerousDeflateOptions = new WebSocketDeflateOptions()
            });

            // We're using a frame size that is close to the sliding window size for the deflate
            const int frameSize = 32_000;

            byte[] message = new byte[frameSize * 100];
            Random random = new(0);

            for (int i = 0; i < message.Length; ++i)
            {
                message[i] = (byte)random.Next(maxValue: 10);
            }

            await client.SendAsync(message, WebSocketMessageType.Binary, true, CancellationToken);

            int payloadLength = stream.Remote.Available;
            stream.Remote.Clear();

            for (var i = 0; i < message.Length; i += frameSize)
            {
                await client.SendAsync(message.AsMemory(i, frameSize), WebSocketMessageType.Binary, i + frameSize == message.Length, CancellationToken);
            }

            Assert.Equal(0.999, Math.Round(payloadLength * 1.0 / stream.Remote.Available, 3));
        }

        [Theory]
        [InlineData(9, 15)]
        [InlineData(15, 9)]
        public async Task SendReceiveWithDifferentWindowBits(int clientWindowBits, int serverWindowBits)
        {
            WebSocketTestStream stream = new();
            WebSocket server = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                IsServer = true,
                DangerousDeflateOptions = new()
                {
                    ClientContextTakeover = false,
                    ClientMaxWindowBits = clientWindowBits,
                    ServerContextTakeover = false,
                    ServerMaxWindowBits = serverWindowBits
                }
            });
            WebSocket client = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                DangerousDeflateOptions = new()
                {
                    ClientContextTakeover = false,
                    ClientMaxWindowBits = clientWindowBits,
                    ServerContextTakeover = false,
                    ServerMaxWindowBits = serverWindowBits
                }
            });

            Memory<byte> data = new byte[64 * 1024];
            Memory<byte> buffer = new byte[data.Length];
            new Random(0).NextBytes(data.Span.Slice(0, data.Length / 2));

            await server.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken);
            ValueWebSocketReceiveResult result = await client.ReceiveAsync(buffer, CancellationToken);

            Assert.Equal(data.Length, result.Count);
            Assert.True(result.EndOfMessage);
            Assert.True(data.Span.SequenceEqual(buffer.Span));

            buffer.Span.Clear();

            await client.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken);
            result = await server.ReceiveAsync(buffer, CancellationToken);

            Assert.Equal(data.Length, result.Count);
            Assert.True(result.EndOfMessage);
            Assert.True(data.Span.SequenceEqual(buffer.Span));
        }

        private ValueTask SendTextAsync(string text, WebSocket websocket, bool disableCompression = false)
        {
            WebSocketMessageFlags flags = WebSocketMessageFlags.EndOfMessage;
            if (disableCompression)
            {
                flags |= WebSocketMessageFlags.DisableCompression;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return websocket.SendAsync(bytes.AsMemory(), WebSocketMessageType.Text, flags, CancellationToken);
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
