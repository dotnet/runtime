// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    /// <summary>Provides a <see cref="Stream"/> that delegates to a wrapped <see cref="WebSocket"/>.</summary>
    public sealed class WebSocketStream : Stream
    {
        /// <summary>Whether disposing this instance should dispose <see cref="WebSocket"/>.</summary>
        private readonly bool _ownsWebSocket;
        /// <summary>Whether the instance has been disposed.</summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketStream"/> class using a specified <see cref="WebSocket"/> instance.
        /// </summary>
        /// <param name="webSocket">The <see cref="WebSocket"/> wrapped by this instance.</param>
        /// <param name="ownsWebSocket">
        /// <see langword="true"/> to indicate that the <see cref="WebSocketStream"/> takes ownership of the <see cref="WebSocket"/>,
        /// such that disposing of this <see cref="Stream"/> will dispose of the <see cref="WebSocket"/>; otherwise, <see langword="false"/>.
        /// When <see langword="true"/>, disposing of this instance doesn't initiate a close handshake; it merely delegates to
        /// <see cref="WebSocket.Dispose"/>.
        /// </param>
        public WebSocketStream(WebSocket webSocket, bool ownsWebSocket = false)
        {
            ArgumentNullException.ThrowIfNull(webSocket);

            WebSocket = webSocket;
            _ownsWebSocket = ownsWebSocket;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            if (disposing && _ownsWebSocket)
            {
                if (WebSocket.State is WebSocketState.Open)
                {
                    // There's no synchronous close, so we're forced to do sync-over-async.
                    WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).GetAwaiter().GetResult();
                }

                WebSocket.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        public override ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return default;
            }
            _disposed = true;

            if (_ownsWebSocket && WebSocket.State is WebSocketState.Open)
            {
                return CloseAndDisposeAsync();
            }
            else
            {
                return base.DisposeAsync();
            }

            async ValueTask CloseAndDisposeAsync()
            {
                if (WebSocket.State is WebSocketState.Open)
                {
                    await WebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).ConfigureAwait(false);
                }
                WebSocket.Dispose();
                await base.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>Gets the underlying <see cref="WebSocket"/> wrapped by this <see cref="WebSocketStream"/>.</summary>
        /// <remarks>The <see cref="WebSocket"/> used to construct this instance.</remarks>
        public WebSocket WebSocket { get; }

        /// <inheritdoc />
        public override bool CanRead => WebSocket.State is WebSocketState.Open or WebSocketState.CloseSent;

        /// <inheritdoc />
        public override bool CanWrite => WebSocket.State is WebSocketState.Open or WebSocketState.CloseReceived;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override void Flush() { }

        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            while (WebSocket.State < WebSocketState.CloseReceived)
            {
                ValueWebSocketReceiveResult result = await WebSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.Count > 0 || buffer.IsEmpty)
                {
                    return result.Count;
                }
            }

            return 0;
        }

        /// <inheritdoc />
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count), callback, state);

        /// <inheritdoc />
        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToAsyncResult.End<int>(asyncResult);

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        /// <inheritdoc />
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                return ValueTask.FromException(new ObjectDisposedException(GetType().FullName));
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            return WebSocket.SendAsync(buffer, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken);
        }

        /// <inheritdoc />
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count), callback, state);

        /// <inheritdoc />
        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToAsyncResult.End(asyncResult);

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count).GetAwaiter().GetResult();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer, offset, count).GetAwaiter().GetResult();

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
