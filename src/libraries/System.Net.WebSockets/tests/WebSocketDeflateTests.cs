using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    [PlatformSpecific(~TestPlatforms.Browser)]
    public class WebSocketDeflateTests
    {
        private CancellationTokenSource? _cancellation;

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
            var stream = new WebSocketStream();

            stream.Write(0xc1, 0x07, 0xf2, 0x48, 0xcd, 0xc9, 0xc9, 0x07, 0x00);
            using var websocket = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                DeflateOptions = new()
            });

            var buffer = new byte[5];
            var result = await websocket.ReceiveAsync(buffer, CancellationToken);

            Assert.True(result.EndOfMessage);
            Assert.Equal(buffer.Length, result.Count);
            Assert.Equal(WebSocketMessageType.Text, result.MessageType);
            Assert.Equal("Hello", Encoding.UTF8.GetString(buffer));

            // Because context takeover is set by default if we try to send
            // the same message it would take fewer bytes.
            stream.Write(0xc1, 0x05, 0xf2, 0x00, 0x11, 0x00, 0x00);

            buffer.AsSpan().Clear();
            result = await websocket.ReceiveAsync(buffer, CancellationToken);

            Assert.True(result.EndOfMessage);
            Assert.Equal(buffer.Length, result.Count);
            Assert.Equal("Hello", Encoding.UTF8.GetString(buffer));
        }

        [Fact]
        public async Task HelloWithoutContextTakeover()
        {
            var stream = new WebSocketStream();

            using var websocket = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                DeflateOptions = new()
                {
                    ClientContextTakeover = false
                }
            });

            var buffer = new byte[5];

            for (var i = 0; i < 100; ++i)
            {
                // Without context takeover the message should look the same every time
                stream.Write(0xc1, 0x07, 0xf2, 0x48, 0xcd, 0xc9, 0xc9, 0x07, 0x00);
                buffer.AsSpan().Clear();

                var result = await websocket.ReceiveAsync(buffer, CancellationToken);

                Assert.True(result.EndOfMessage);
                Assert.Equal(buffer.Length, result.Count);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);
                Assert.Equal("Hello", Encoding.UTF8.GetString(buffer));
            }
        }

        [Fact]
        public async Task TwoDeflateBlocksInOneMessage()
        {
            // Two or more DEFLATE blocks may be used in one message.
            var stream = new WebSocketStream();
            using var websocket = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
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
            stream.Write(0x41, 0x08, 0xf2, 0x48, 0x05, 0x00, 0x00, 0x00, 0xff, 0xff);
            stream.Write(0x80, 0x05, 0xca, 0xc9, 0xc9, 0x07, 0x00);

            Memory<byte> buffer = new byte[5];
            var result = await websocket.ReceiveAsync(buffer, CancellationToken);

            Assert.Equal(2, result.Count);
            Assert.False(result.EndOfMessage);

            result = await websocket.ReceiveAsync(buffer.Slice(result.Count), CancellationToken);

            Assert.Equal(3, result.Count);
            Assert.True(result.EndOfMessage);
            Assert.Equal("Hello", Encoding.UTF8.GetString(buffer.Span));
        }

        [Fact]
        public async Task Duplex()
        {
            var stream = new WebSocketStream();
            using var server = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                IsServer = true,
                DeflateOptions = new()
            });
            using var client = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                DeflateOptions = new()
            });

            var buffer = new byte[1024];

            for (var i = 0; i < 10; ++i)
            {
                var message = $"Sending number {i} from server.";
                await SendTextAsync(message, server);

                var result = await client.ReceiveAsync(buffer.AsMemory(), CancellationToken);

                Assert.True(result.EndOfMessage);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                Assert.Equal(message, Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count)));
            }

            for (var i = 0; i < 10; ++i)
            {
                var message = $"Sending number {i} from client.";
                await SendTextAsync(message, client);

                var result = await server.ReceiveAsync(buffer.AsMemory(), CancellationToken);

                Assert.True(result.EndOfMessage);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                Assert.Equal(message, Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count)));
            }
        }

        [Theory]
        [MemberData(nameof(SupportedWindowBits))]
        public async Task LargeMessageSplitInMultipleFrames(int windowBits)
        {
            var stream = new WebSocketStream();
            using var server = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions
            {
                IsServer = true,
                DeflateOptions = new()
                {
                    ClientMaxWindowBits = windowBits
                }
            });
            using var client = WebSocket.CreateFromStream(stream.Remote, new WebSocketCreationOptions
            {
                DeflateOptions = new()
                {
                    ClientMaxWindowBits = windowBits
                }
            });

            Memory<byte> testData = File.ReadAllBytes(typeof(WebSocketDeflateTests).Assembly.Location).AsMemory().TrimEnd((byte)0);
            Memory<byte> receivedData = new byte[testData.Length];

            // Test it a few times with different frame sizes
            for (var i = 0; i < 10; ++i)
            {
                // Use a timeout cancellation token in case something doesn't work right
                var frameSize = RandomNumberGenerator.GetInt32(1024, 2048);
                var position = 0;

                while (position < testData.Length)
                {
                    var currentFrameSize = Math.Min(frameSize, testData.Length - position);
                    var eof = position + currentFrameSize == testData.Length;

                    await server.SendAsync(testData.Slice(position, currentFrameSize), WebSocketMessageType.Binary, eof, CancellationToken);
                    position += currentFrameSize;
                }

                Assert.Equal(testData.Length, position);
                Assert.True(testData.Length > stream.Remote.Available, "The data must be compressed.");

                // Receive the data from the client side
                receivedData.Span.Clear();
                position = 0;

                // Intentionally receive with a frame size that is less than what the sender used
                frameSize /= 3;

                while (true)
                {
                    var currentFrameSize = Math.Min(frameSize, testData.Length - position);
                    var result = await client.ReceiveAsync(receivedData.Slice(position, currentFrameSize), CancellationToken);

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
            var stream = new WebSocketStream();

            stream.Write(0xc1, 0x07, 0xf2, 0x48, 0xcd, 0xc9, 0xc9, 0x07, 0x00);
            using var websocket = WebSocket.CreateFromStream(stream.Remote, new());

            var exception = await Assert.ThrowsAsync<WebSocketException>(() =>
               websocket.ReceiveAsync(Memory<byte>.Empty, CancellationToken).AsTask());

            Assert.Equal("The WebSocket received compressed frame when compression is not enabled.", exception.Message);
        }

        private ValueTask SendTextAsync(string text, WebSocket websocket)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return websocket.SendAsync(bytes.AsMemory(), WebSocketMessageType.Text, true, CancellationToken);
        }
    }
}
