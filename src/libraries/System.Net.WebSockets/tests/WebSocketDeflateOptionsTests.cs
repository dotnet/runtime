// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.WebSockets.Tests
{
    public class WebSocketDeflateOptionsTests
    {
        [Fact]
        public void ClientMaxWindowBits()
        {
            WebSocketDeflateOptions options = new();
            Assert.Equal(15, options.ClientMaxWindowBits);

            Assert.Throws<ArgumentOutOfRangeException>(() => options.ClientMaxWindowBits = 8);
            Assert.Throws<ArgumentOutOfRangeException>(() => options.ClientMaxWindowBits = 16);

            options.ClientMaxWindowBits = 14;
            Assert.Equal(14, options.ClientMaxWindowBits);
        }

        [Fact]
        public void ServerMaxWindowBits()
        {
            WebSocketDeflateOptions options = new();
            Assert.Equal(15, options.ServerMaxWindowBits);

            Assert.Throws<ArgumentOutOfRangeException>(() => options.ServerMaxWindowBits = 8);
            Assert.Throws<ArgumentOutOfRangeException>(() => options.ServerMaxWindowBits = 16);

            options.ServerMaxWindowBits = 14;
            Assert.Equal(14, options.ServerMaxWindowBits);
        }

        [Fact]
        public void ContextTakeover()
        {
            WebSocketDeflateOptions options = new();

            Assert.True(options.ClientContextTakeover);
            Assert.True(options.ServerContextTakeover);

            options.ClientContextTakeover = false;
            Assert.False(options.ClientContextTakeover);

            options.ServerContextTakeover = false;
            Assert.False(options.ServerContextTakeover);
        }
    }
}
