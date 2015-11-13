// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    /// <summary>
    /// ClientWebSocket unit tests that do not require a remote server.
    /// </summary>
    public class ClientWebSocketUnitTest
    {
        private readonly ITestOutputHelper _output;

        public ClientWebSocketUnitTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Ctor_Success()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();
        }

        [Fact]
        public void Abort_CreateAndAbort_StateIsClosed()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();
            cws.Abort();

            Assert.Equal(WebSocketState.Closed, cws.State);
        }

        [Fact]
        public void CloseAsync_CreateAndClose_ThrowsInvalidOperationException()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();
            Assert.Throws<InvalidOperationException>(() =>
                { Task t = cws.CloseAsync(WebSocketCloseStatus.Empty, "", new CancellationToken()); });

            Assert.Equal(WebSocketState.None, cws.State);
        }

        [Fact]
        public void CloseAsync_CreateAndCloseOutput_ThrowsInvalidOperationExceptionWithCorrectMessage()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();
            AssertExtensions.Throws<InvalidOperationException>(
                () =>
                cws.CloseOutputAsync(WebSocketCloseStatus.Empty, "", new CancellationToken()).GetAwaiter().GetResult(),
                ResourceHelper.GetExceptionMessage("net_WebSockets_NotConnected"));

            Assert.Equal(WebSocketState.None, cws.State);
        }

        [Fact]
        public void CloseAsync_CreateAndReceive_ThrowsInvalidOperationException()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();

            var buffer = new byte[100];
            var segment = new ArraySegment<byte>(buffer);
            var ct = new CancellationToken();

            Assert.Throws<InvalidOperationException>(() =>
                { Task t = cws.ReceiveAsync(segment, ct); });

            Assert.Equal(WebSocketState.None, cws.State);
        }

        [Fact]
        public void CloseAsync_CreateAndReceive_ThrowsInvalidOperationExceptionWithCorrectMessage()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();

            var buffer = new byte[100];
            var segment = new ArraySegment<byte>(buffer);
            var ct = new CancellationToken();

            AssertExtensions.Throws<InvalidOperationException>(
                () => cws.ReceiveAsync(segment, ct).GetAwaiter().GetResult(),
                ResourceHelper.GetExceptionMessage("net_WebSockets_NotConnected"));

            Assert.Equal(WebSocketState.None, cws.State);
        }

        [Fact]
        public void CloseAsync_CreateAndSend_ThrowsInvalidOperationException()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();

            var buffer = new byte[100];
            var segment = new ArraySegment<byte>(buffer);
            var ct = new CancellationToken();

            Assert.Throws<InvalidOperationException>(() =>
                { Task t = cws.SendAsync(segment, WebSocketMessageType.Text, false, ct); });

            Assert.Equal(WebSocketState.None, cws.State);
        }

        [Fact]
        public void CloseAsync_CreateAndSend_ThrowsInvalidOperationExceptionWithCorrectMessage()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();

            var buffer = new byte[100];
            var segment = new ArraySegment<byte>(buffer);
            var ct = new CancellationToken();

            AssertExtensions.Throws<InvalidOperationException>(
                () => cws.SendAsync(segment, WebSocketMessageType.Text, false, ct).GetAwaiter().GetResult(),
                ResourceHelper.GetExceptionMessage("net_WebSockets_NotConnected"));

            Assert.Equal(WebSocketState.None, cws.State);
        }

        [Fact]
        public void Ctor_ExpectedPropertyValues()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();
            Assert.Equal(null, cws.CloseStatus);
            Assert.Equal(null, cws.CloseStatusDescription);
            Assert.NotEqual(null, cws.Options);
            Assert.Equal(WebSocketState.None, cws.State);
            Assert.Equal(null, cws.SubProtocol);
            Assert.Equal("System.Net.WebSockets.ClientWebSocket", cws.ToString());
        }

        [Fact]
        public void Abort_CreateAndDisposeAndAbort_StateIsClosedSuccess()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();
            cws.Dispose();

            cws.Abort();
            Assert.Equal(WebSocketState.Closed, cws.State);
        }

        [Fact]
        public void CloseAsync_DisposeAndClose_ThrowsObjectDisposedException()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();
            cws.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
                { Task t = cws.CloseAsync(WebSocketCloseStatus.Empty, "", new CancellationToken()); });

            Assert.Equal(WebSocketState.Closed, cws.State);
        }

        [Fact]
        public void CloseAsync_DisposeAndClose_ThrowsObjectDisposedExceptionWithCorrectMessage()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();
            cws.Dispose();

            var expectedException = new ObjectDisposedException(cws.GetType().FullName);

            AssertExtensions.Throws<ObjectDisposedException>(
                () =>
                cws.CloseOutputAsync(WebSocketCloseStatus.Empty, "", new CancellationToken()).GetAwaiter().GetResult(),
                expectedException.Message);

            Assert.Equal(WebSocketState.Closed, cws.State);
        }

        [Fact]
        public void ReceiveAsync_CreateAndDisposeAndReceive_ThrowsObjectDisposedExceptionWithCorrectMessage()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();
            cws.Dispose();

            var buffer = new byte[100];
            var segment = new ArraySegment<byte>(buffer);
            var ct = new CancellationToken();

            var expectedException = new ObjectDisposedException(cws.GetType().FullName);

            AssertExtensions.Throws<ObjectDisposedException>(
                () => cws.ReceiveAsync(segment, ct).GetAwaiter().GetResult(),
                expectedException.Message);

            Assert.Equal(WebSocketState.Closed, cws.State);
        }

        [Fact]
        public void SendAsync_CreateAndDisposeAndSend_ThrowsObjectDisposedExceptionWithCorrectMessage()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();
            cws.Dispose();

            var buffer = new byte[100];
            var segment = new ArraySegment<byte>(buffer);
            var ct = new CancellationToken();

            var expectedException = new ObjectDisposedException(cws.GetType().FullName);

            AssertExtensions.Throws<ObjectDisposedException>(
                () => cws.SendAsync(segment, WebSocketMessageType.Text, false, ct).GetAwaiter().GetResult(),
                expectedException.Message);

            Assert.Equal(WebSocketState.Closed, cws.State);
        }

        [Fact]
        public void Dispose_CreateAndDispose_ExpectedPropertyValues()
        {
            if (WebSocketHelper.ShouldSkipTestOSNotSupported(_output))
            {
                return;
            }

            var cws = new ClientWebSocket();
            cws.Dispose();

            Assert.Equal(null, cws.CloseStatus);
            Assert.Equal(null, cws.CloseStatusDescription);
            Assert.NotEqual(null, cws.Options);
            Assert.Equal(WebSocketState.Closed, cws.State);
            Assert.Equal(null, cws.SubProtocol);
            Assert.Equal("System.Net.WebSockets.ClientWebSocket", cws.ToString());
        }
    }
}
