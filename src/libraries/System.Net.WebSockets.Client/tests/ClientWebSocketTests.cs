// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

using Xunit;

namespace System.Net.WebSockets.Client.Test
{
    /// <summary>
    /// ClientWebSocket unit tests that do not require a remote server.
    /// </summary>
    public class ClientWebSocketTest
    {
        internal static class AssertExtensions
        {
            public static void Throws<T>(Action action, string message)
                where T : Exception
            {
                Assert.Equal(Assert.Throws<T>(action).Message, message);
            }
        }

        [Fact]
        public void ClientWebSocket_Ctor()
        {
            var cws = new ClientWebSocket();
        }

        [Fact]
        public void ClientWebSocket_NoConnect_AbortCalled_Ok()
        {
            var cws = new ClientWebSocket();
            cws.Abort();

            Assert.Equal(WebSocketState.Closed, cws.State);
        }

        [Fact]
        public void ClientWebSocket_NoConnect_CloseAsync_Throws()
        {
            var cws = new ClientWebSocket();
            Assert.Throws<InvalidOperationException>(
                () =>
                cws.CloseAsync(WebSocketCloseStatus.Empty, "", new CancellationToken()).GetAwaiter().GetResult());

            Assert.Equal(WebSocketState.None, cws.State);
        }

        [Fact]
        public void ClientWebSocket_NoConnect_CloseOutputAsync_Ok()
        {
            var cws = new ClientWebSocket();
            AssertExtensions.Throws<InvalidOperationException>(
                () =>
                cws.CloseOutputAsync(WebSocketCloseStatus.Empty, "", new CancellationToken()).GetAwaiter().GetResult(),
                ResourceHelper.GetExceptionMessage("net_WebSockets_NotConnected"));

            Assert.Equal(WebSocketState.None, cws.State);
        }

        [Fact]
        public void ClientWebSocket_NoConnect_ReceiveAsync_Throws()
        {
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
        public void ClientWebSocket_NoConnect_SendAsync_Throws()
        {
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
        public void ClientWebSocket_NoConnect_Properties_Ok()
        {
            var cws = new ClientWebSocket();
            Assert.Equal(null, cws.CloseStatus);
            Assert.Equal(null, cws.CloseStatusDescription);
            Assert.NotEqual(null, cws.Options);
            Assert.Equal(WebSocketState.None, cws.State);
            Assert.Equal(null, cws.SubProtocol);
            Assert.Equal("System.Net.WebSockets.ClientWebSocket", cws.ToString());
        }

        [Fact]
        public void ClientWebSocket_Dispose_AbortCalled_Ok()
        {
            var cws = new ClientWebSocket();
            cws.Dispose();

            cws.Abort();
            Assert.Equal(WebSocketState.Closed, cws.State);
        }

        [Fact]
        public void ClientWebSocket_Dispose_CloseAsync_Throws()
        {
            var cws = new ClientWebSocket();
            cws.Dispose();

            Assert.Throws<ObjectDisposedException>(
                () =>
                cws.CloseAsync(WebSocketCloseStatus.Empty, "", new CancellationToken()).GetAwaiter().GetResult());

            Assert.Equal(WebSocketState.Closed, cws.State);
        }

        [Fact]
        public void ClientWebSocket_Dispose_CloseOutputAsync_Ok()
        {
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
        public void ClientWebSocket_Dispose_ReceiveAsync_Throws()
        {
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
        public void ClientWebSocket_Dispose_SendAsync_Throws()
        {
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
        public void ClientWebSocket_Dispose_Properties_Ok()
        {
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
