// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Tests;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public sealed class WebSocketStreamTests : ConnectedStreamConformanceTests
    {
        protected override bool BlocksOnZeroByteReads => true;
        protected override bool FlushRequiredToWriteData => false;
        protected override bool ReadsReadUntilSizeOrEof => false;
        protected override bool UsableAfterCanceledReads => false;
        protected override Type UnsupportedConcurrentExceptionType => null;

        protected override Task<StreamPair> CreateConnectedStreamsAsync()
        {
            (Stream stream1, Stream stream2) = ConnectedStreams.CreateBidirectional();

            WebSocket webSocket1 = WebSocket.CreateFromStream(stream1, isServer: false, null, Timeout.InfiniteTimeSpan);
            WebSocket webSocket2 = WebSocket.CreateFromStream(stream2, isServer: true, null, Timeout.InfiniteTimeSpan);

            return Task.FromResult(new StreamPair(
                new WebSocketStream(webSocket1, ownsWebSocket: true),
                new WebSocketStream(webSocket2, ownsWebSocket: true)));
        }

        [Fact]
        public void Ctor_InvalidArgs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("webSocket", () => new WebSocketStream(null));
            AssertExtensions.Throws<ArgumentNullException>("webSocket", () => new WebSocketStream(null, ownsWebSocket: true));
        }

        [Fact]
        public void Ctor_Roundtrips()
        {
            (Stream stream1, Stream stream2) = ConnectedStreams.CreateBidirectional();

            WebSocket webSocket = WebSocket.CreateFromStream(stream1, isServer: false, null, Timeout.InfiniteTimeSpan);

            WebSocketStream stream = new WebSocketStream(webSocket);
            Assert.Same(webSocket, stream.WebSocket);
            stream.Dispose();
            Assert.Same(webSocket, stream.WebSocket);

            stream2.Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Dispose_ClosesWebSocketIfOwned(bool ownsWebSocket)
        {
            (Stream stream1, Stream stream2) = ConnectedStreams.CreateBidirectional();

            WebSocket webSocket = WebSocket.CreateFromStream(stream1, isServer: false, null, Timeout.InfiniteTimeSpan);

            WebSocketStream stream = new WebSocketStream(webSocket, ownsWebSocket);
            Assert.Equal(WebSocketState.Open, webSocket.State);

            stream.Dispose();
            Assert.Equal(ownsWebSocket ? WebSocketState.Closed : WebSocketState.Open, webSocket.State);

            stream2.Dispose();
        }
    }
}
