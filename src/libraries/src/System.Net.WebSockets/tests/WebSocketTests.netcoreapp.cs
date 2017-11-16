// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public sealed partial class WebSocketTests
    {
        [Fact]
        public void CreateFromStream_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("stream",
                () => WebSocket.CreateFromStream(null, true, "subProtocol", TimeSpan.FromSeconds(30)));
            AssertExtensions.Throws<ArgumentException>("stream",
                () => WebSocket.CreateFromStream(new MemoryStream(new byte[100], writable:false), true, "subProtocol", TimeSpan.FromSeconds(30)));
            AssertExtensions.Throws<ArgumentException>("stream",
                () => WebSocket.CreateFromStream(new UnreadableStream(), true, "subProtocol", TimeSpan.FromSeconds(30)));

            AssertExtensions.Throws<ArgumentException>("subProtocol",
                () => WebSocket.CreateFromStream(new MemoryStream(), true, "    ", TimeSpan.FromSeconds(30)));
            AssertExtensions.Throws<ArgumentException>("subProtocol",
                () => WebSocket.CreateFromStream(new MemoryStream(), true, "\xFF", TimeSpan.FromSeconds(30)));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("keepAliveInterval", () =>
                WebSocket.CreateFromStream(new MemoryStream(), true, "subProtocol", TimeSpan.FromSeconds(-2)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(14)]
        [InlineData(4096)]
        public void CreateFromStream_ValidBufferSizes_Succeed(int bufferSize)
        {
            Assert.NotNull(WebSocket.CreateFromStream(new MemoryStream(), false, null, Timeout.InfiniteTimeSpan, new byte[bufferSize]));
            Assert.NotNull(WebSocket.CreateFromStream(new MemoryStream(), true, null, Timeout.InfiniteTimeSpan, new byte[bufferSize]));
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

        private sealed class UnreadableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => true;
            public override bool CanWrite => true;
            public override long Length => throw new NotImplementedException();
            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override void Flush() => throw new NotImplementedException();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
            public override void SetLength(long value) => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        }
    }
}
