using Xunit;

namespace System.Net.WebSockets.Tests
{
    public class WebSocketDeflateOptionsTests
    {
        [Fact]
        public void ClientMaxWindowBits()
        {
            var options = new WebSocketDeflateOptions();
            Assert.Equal(15, options.ClientMaxWindowBits);

            Assert.Throws<ArgumentOutOfRangeException>(() => options.ClientMaxWindowBits = 8);
            Assert.Throws<ArgumentOutOfRangeException>(() => options.ClientMaxWindowBits = 16);

            options.ClientMaxWindowBits = 14;
            Assert.Equal(14, options.ClientMaxWindowBits);
        }

        [Fact]
        public void ServerMaxWindowBits()
        {
            var options = new WebSocketDeflateOptions();
            Assert.Equal(15, options.ServerMaxWindowBits);

            Assert.Throws<ArgumentOutOfRangeException>(() => options.ServerMaxWindowBits = 8);
            Assert.Throws<ArgumentOutOfRangeException>(() => options.ServerMaxWindowBits = 16);

            options.ServerMaxWindowBits = 14;
            Assert.Equal(14, options.ServerMaxWindowBits);
        }

        [Fact]
        public void ContextTakeover()
        {
            var options = new WebSocketDeflateOptions();

            Assert.True(options.ClientContextTakeover);
            Assert.True(options.ServerContextTakeover);

            options.ClientContextTakeover = false;
            Assert.False(options.ClientContextTakeover);

            options.ServerContextTakeover = false;
            Assert.False(options.ServerContextTakeover);
        }
    }
}
