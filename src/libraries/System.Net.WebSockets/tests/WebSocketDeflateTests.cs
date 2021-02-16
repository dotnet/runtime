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
            (var server, var client) = WebSocketStream.Create();
            
            server.Write(0xc1, 0x07, 0xf2, 0x48, 0xcd, 0xc9, 0xc9, 0x07, 0x00);
            using var websocket = WebSocket.CreateFromStream(client, new WebSocketCreationOptions
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
            server.Write(0xc1, 0x05, 0xf2, 0x00, 0x11, 0x00, 0x00);

            buffer.AsSpan().Clear();
            result = await websocket.ReceiveAsync(buffer, default);

            Assert.True(result.EndOfMessage);
            Assert.Equal(buffer.Length, result.Count);
            Assert.Equal("Hello", Encoding.UTF8.GetString(buffer));
        }

        [Fact]
        public async Task HelloWithoutContextTakeover()
        {
            (var server, var client) = WebSocketStream.Create();

            using var websocket = WebSocket.CreateFromStream(client, new WebSocketCreationOptions
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
                server.Write(0xc1, 0x07, 0xf2, 0x48, 0xcd, 0xc9, 0xc9, 0x07, 0x00);
                buffer.AsSpan().Clear();

                var result = await websocket.ReceiveAsync(buffer, default);

                Assert.True(result.EndOfMessage);
                Assert.Equal(buffer.Length, result.Count);
                Assert.Equal(WebSocketMessageType.Text, result.MessageType);
                Assert.Equal("Hello", Encoding.UTF8.GetString(buffer));
            }
        }
    }
}
