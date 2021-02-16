using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    [PlatformSpecific(~TestPlatforms.Browser)]
    public class WebSocketDeflateTests
    {
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
            var result = await websocket.ReceiveAsync(buffer, default);

            Assert.True(result.EndOfMessage);
            Assert.Equal(buffer.Length, result.Count);
            Assert.Equal(WebSocketMessageType.Text, result.MessageType);
            Assert.Equal("Hello", Encoding.UTF8.GetString(buffer));

            // Because context takeover is set by default if we try to send
            // the same message it would take fewer bytes.
            stream.Write(0xc1, 0x05, 0xf2, 0x00, 0x11, 0x00, 0x00);

            buffer.AsSpan().Clear();
            result = await websocket.ReceiveAsync(buffer, default);

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

                var result = await websocket.ReceiveAsync(buffer, default);

                Assert.True(result.EndOfMessage);
                Assert.Equal(buffer.Length, result.Count);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);
                Assert.Equal("Hello", Encoding.UTF8.GetString(buffer));
            }
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

                var result = await client.ReceiveAsync(buffer.AsMemory(), default);

                Assert.True(result.EndOfMessage);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                Assert.Equal(message, Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count)));
            }

            for (var i = 0; i < 10; ++i)
            {
                var message = $"Sending number {i} from client.";
                await SendTextAsync(message, client);

                var result = await server.ReceiveAsync(buffer.AsMemory(), default);

                Assert.True(result.EndOfMessage);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);

                Assert.Equal(message, Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count)));
            }
        }

        private static ValueTask SendTextAsync(string text, WebSocket websocket)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return websocket.SendAsync(bytes.AsMemory(), WebSocketMessageType.Text, true, default);
        }
    }
}
