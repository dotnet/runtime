// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public sealed partial class WebSocketTests : WebSocketCreateTest
    {
        protected override WebSocket CreateFromStream(Stream stream, bool isServer, string subProtocol, TimeSpan keepAliveInterval) =>
            WebSocket.CreateFromStream(stream, isServer, subProtocol, keepAliveInterval);

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
    }
}
