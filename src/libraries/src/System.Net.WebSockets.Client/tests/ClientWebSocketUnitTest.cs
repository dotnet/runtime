// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Common.Tests;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.WebSockets.Client.Tests
{
    /// <summary>
    /// ClientWebSocket unit tests that do not require a remote server.
    /// </summary>
    public class ClientWebSocketUnitTest
    {
        private static bool WebSocketsSupported { get { return WebSocketHelper.WebSocketsSupported; } }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public void Ctor_Success()
        {
            var cws = new ClientWebSocket();
            cws.Dispose();
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public void Abort_CreateAndAbort_StateIsClosed()
        {
            using (var cws = new ClientWebSocket())
            {
                cws.Abort();

                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public void CloseAsync_CreateAndClose_ThrowsInvalidOperationException()
        {
            using (var cws = new ClientWebSocket())
            {
                Assert.Throws<InvalidOperationException>(() =>
                    { Task t = cws.CloseAsync(WebSocketCloseStatus.Empty, "", new CancellationToken()); });

                Assert.Equal(WebSocketState.None, cws.State);
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public async Task CloseAsync_CreateAndCloseOutput_ThrowsInvalidOperationExceptionWithMessage()
        {
            using (var cws = new ClientWebSocket())
            {
                InvalidOperationException exception;
                using (var tcc = new ThreadCultureChange())
                {
                    tcc.ChangeCultureInfo(CultureInfo.InvariantCulture);

                    exception = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => cws.CloseOutputAsync(WebSocketCloseStatus.Empty, "", new CancellationToken()));
                }

                string expectedMessage = ResourceHelper.GetExceptionMessage("net_WebSockets_NotConnected");
                Assert.Equal(expectedMessage, exception.Message);
                Assert.Equal(WebSocketState.None, cws.State);
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public void CloseAsync_CreateAndReceive_ThrowsInvalidOperationException()
        {
            using (var cws = new ClientWebSocket())
            {
                var buffer = new byte[100];
                var segment = new ArraySegment<byte>(buffer);
                var ct = new CancellationToken();

                Assert.Throws<InvalidOperationException>(() =>
                    { Task t = cws.ReceiveAsync(segment, ct); });

                Assert.Equal(WebSocketState.None, cws.State);
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public async Task CloseAsync_CreateAndReceive_ThrowsInvalidOperationExceptionWithMessage()
        {
            using (var cws = new ClientWebSocket())
            {
                var buffer = new byte[100];
                var segment = new ArraySegment<byte>(buffer);
                var ct = new CancellationToken();

                InvalidOperationException exception;

                using (var tcc = new ThreadCultureChange())
                {
                    tcc.ChangeCultureInfo(CultureInfo.InvariantCulture);
                    exception = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => cws.ReceiveAsync(segment, ct));
                }

                string expectedMessage = ResourceHelper.GetExceptionMessage("net_WebSockets_NotConnected");
                Assert.Equal(expectedMessage, exception.Message);
                Assert.Equal(WebSocketState.None, cws.State);
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public void CloseAsync_CreateAndSend_ThrowsInvalidOperationException()
        {
            using (var cws = new ClientWebSocket())
            {
                var buffer = new byte[100];
                var segment = new ArraySegment<byte>(buffer);
                var ct = new CancellationToken();

                Assert.Throws<InvalidOperationException>(() =>
                    { Task t = cws.SendAsync(segment, WebSocketMessageType.Text, false, ct); });

                Assert.Equal(WebSocketState.None, cws.State);
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public async Task CloseAsync_CreateAndSend_ThrowsInvalidOperationExceptionWithMessage()
        {
            using (var cws = new ClientWebSocket())
            {
                var buffer = new byte[100];
                var segment = new ArraySegment<byte>(buffer);
                var ct = new CancellationToken();

                InvalidOperationException exception;
                using (var tcc = new ThreadCultureChange())
                {
                    tcc.ChangeCultureInfo(CultureInfo.InvariantCulture);
                    exception = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => cws.SendAsync(segment, WebSocketMessageType.Text, false, ct));
                }

                string expectedMessage = ResourceHelper.GetExceptionMessage("net_WebSockets_NotConnected");
                Assert.Equal(expectedMessage, exception.Message);
                Assert.Equal(WebSocketState.None, cws.State);
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public void Ctor_ExpectedPropertyValues()
        {
            using (var cws = new ClientWebSocket())
            {
                Assert.Equal(null, cws.CloseStatus);
                Assert.Equal(null, cws.CloseStatusDescription);
                Assert.NotEqual(null, cws.Options);
                Assert.Equal(WebSocketState.None, cws.State);
                Assert.Equal(null, cws.SubProtocol);
                Assert.Equal("System.Net.WebSockets.ClientWebSocket", cws.ToString());
            }
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public void Abort_CreateAndDisposeAndAbort_StateIsClosedSuccess()
        {
            var cws = new ClientWebSocket();
            cws.Dispose();

            cws.Abort();
            Assert.Equal(WebSocketState.Closed, cws.State);
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public void CloseAsync_DisposeAndClose_ThrowsObjectDisposedException()
        {
            var cws = new ClientWebSocket();
            cws.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
                { Task t = cws.CloseAsync(WebSocketCloseStatus.Empty, "", new CancellationToken()); });

            Assert.Equal(WebSocketState.Closed, cws.State);
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public void CloseAsync_DisposeAndCloseOutput_ThrowsObjectDisposedExceptionWithMessage()
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

        [ConditionalFact(nameof(WebSocketsSupported))]
        public void ReceiveAsync_CreateAndDisposeAndReceive_ThrowsObjectDisposedExceptionWithMessage()
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

        [ConditionalFact(nameof(WebSocketsSupported))]
        public void SendAsync_CreateAndDisposeAndSend_ThrowsObjectDisposedExceptionWithMessage()
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

        [ConditionalFact(nameof(WebSocketsSupported))]
        public void Dispose_CreateAndDispose_ExpectedPropertyValues()
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
