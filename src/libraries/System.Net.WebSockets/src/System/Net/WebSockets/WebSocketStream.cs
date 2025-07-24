// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    /// <summary>Provides a <see cref="Stream"/> that delegates to a wrapped <see cref="WebSocket"/>.</summary>
    public class WebSocketStream : Stream
    {
        /// <summary>The default number of seconds before canceling CloseAsync operation issued during stream disposal.</summary>
        private const int DefaultCloseTimeoutSeconds = 16;

        /// <summary>Whether the stream has been disposed.</summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketStream"/> class using a specified <see cref="WebSocket"/> instance.
        /// </summary>
        /// <param name="webSocket">The <see cref="WebSocket"/> wrapped by this instance.</param>
        private WebSocketStream(WebSocket webSocket) => WebSocket = webSocket;

        /// <summary>Creates a <see cref="WebSocketStream"/> that delegates to a wrapped <see cref="WebSocket"/>.</summary>
        /// <param name="webSocket">The wrapped <see cref="WebSocket"/>.</param>
        /// <param name="writeMessageType">The type of messages that should be written as part of <see cref="M:Stream.WriteAsync"/> calls. Each write produces a message.</param>
        /// <param name="ownsWebSocket">
        /// <see langword="true"/> if disposing the <see cref="Stream"/> should close the underlying <see cref="WebSocket"/>; otherwise, <see langword="false"/>. Defaults to <see langword="false"/>.
        /// </param>
        /// <returns>A new instance of <see cref="WebSocketStream"/> that forwards reads and writes on the <see cref="Stream"/> to the underlying <see cref="WebSocket"/>.</returns>
        public static WebSocketStream Create(WebSocket webSocket, WebSocketMessageType writeMessageType, bool ownsWebSocket = false)
        {
            ArgumentNullException.ThrowIfNull(webSocket);
            ManagedWebSocket.ThrowIfInvalidMessageType(writeMessageType);

            return new ReadWriteStream(
                webSocket,
                writeMessageType,
                closeTimeout: ownsWebSocket ? TimeSpan.FromSeconds(DefaultCloseTimeoutSeconds) : null);
        }

        /// <summary>Creates a <see cref="WebSocketStream"/> that delegates to a wrapped <see cref="WebSocket"/>.</summary>
        /// <param name="webSocket">The wrapped <see cref="WebSocket"/>.</param>
        /// <param name="writeMessageType">The type of messages that should be written as part of <see cref="M:Stream.WriteAsync"/> calls. Each write produces a message.</param>
        /// <param name="closeTimeout">The amount of time that disposing the <see cref="WebSocketStream"/> will wait for a graceful closing of the <see cref="WebSocket"/>'s output.</param>
        /// <returns>A new instance of <see cref="WebSocketStream"/> that forwards reads and writes on the <see cref="Stream"/> to the underlying <see cref="WebSocket"/>.</returns>
        public static WebSocketStream Create(WebSocket webSocket, WebSocketMessageType writeMessageType, TimeSpan closeTimeout)
        {
            ArgumentNullException.ThrowIfNull(webSocket);
            ManagedWebSocket.ThrowIfInvalidMessageType(writeMessageType);
            if (closeTimeout < TimeSpan.Zero && closeTimeout != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(closeTimeout), SR.net_WebSockets_TimeoutOutOfRange);
            }

            return new ReadWriteStream(webSocket, writeMessageType, closeTimeout);
        }

        /// <summary>Creates a <see cref="WebSocketStream"/> that writes a single message to the underlying <see cref="WebSocket"/>.</summary>
        /// <param name="webSocket">The wrapped <see cref="WebSocket"/>.</param>
        /// <param name="writeMessageType">
        /// The type of messages that should be written as part of <see cref="M:Stream.WriteAsync"/> calls.
        /// Each write on the <see cref="Stream"/> results in writing a partial message to the underlying <see cref="WebSocket"/>.
        /// When the <see cref="Stream"/> is disposed, it will write an empty message to the underlying <see cref="WebSocket"/> to signal the end of the message.
        /// </param>
        /// <returns>A new instance of <see cref="WebSocketStream"/> that forwards writes on the <see cref="Stream"/> to the underlying <see cref="WebSocket"/>.</returns>
        public static WebSocketStream CreateWritableMessageStream(WebSocket webSocket, WebSocketMessageType writeMessageType)
        {
            ArgumentNullException.ThrowIfNull(webSocket);
            ManagedWebSocket.ThrowIfInvalidMessageType(writeMessageType);

            return new WriteMessageStream(webSocket, writeMessageType);
        }

        /// <summary>Creates a <see cref="WebSocketStream"/> that reads a single message from the underlying <see cref="WebSocket"/>.</summary>
        /// <param name="webSocket">The wrapped <see cref="WebSocket"/>.</param>
        /// <returns>A new instance of <see cref="WebSocketStream"/> that forwards reads on the <see cref="Stream"/> to the underlying <see cref="WebSocket"/>.</returns>
        /// <remarks>
        /// Reads on the <see cref="Stream"/> will read a single message from the underlying <see cref="WebSocket"/>. This means that reads will start returning
        /// 0 bytes read once all data has been consumed from the next message received in the <see cref="WebSocket"/>.
        /// </remarks>
        public static WebSocketStream CreateReadableMessageStream(WebSocket webSocket)
        {
            ArgumentNullException.ThrowIfNull(webSocket);

            return new ReadMessageStream(webSocket);
        }

        /// <summary>Gets the underlying <see cref="WebSocket"/> wrapped by this <see cref="WebSocketStream"/>.</summary>
        /// <remarks>The <see cref="WebSocket"/> used to construct this instance.</remarks>
        public WebSocket WebSocket { get; }

        /// <inheritdoc />
        public override bool CanRead => !_disposed && WebSocket.State is WebSocketState.Open or WebSocketState.CloseSent;

        /// <inheritdoc />
        public override bool CanWrite => !_disposed && WebSocket.State is WebSocketState.Open or WebSocketState.CloseReceived;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // There are no synchronous operations on WebSocket, so we're forced to do sync-over-async.
                DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        /// <inheritdoc />
        public override void Flush() { }

        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) :
            Task.CompletedTask;

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        /// <inheritdoc />
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromException<int>(ExceptionDispatchInfo.SetCurrentStackTrace(new NotSupportedException()));

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
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new NotSupportedException()));

        /// <inheritdoc />
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count), callback, state);

        /// <inheritdoc />
        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToAsyncResult.End(asyncResult);

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer, offset, count, default).GetAwaiter().GetResult();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) =>
            WriteAsync(buffer, offset, count, default).GetAwaiter().GetResult();

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>Provides stream that wraps a <see cref="WebSocket"/> and forwards reads/writes.</summary>
        private sealed class ReadWriteStream(WebSocket webSocket, WebSocketMessageType writeMessageType, TimeSpan? closeTimeout) : WebSocketStream(webSocket)
        {
            private readonly WebSocketMessageType _messageType = writeMessageType;
            private readonly TimeSpan? _closeTimeout = closeTimeout;

            /// <inheritdoc />
            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_disposed)
                {
                    return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(GetType().FullName)));
                }

                if (!CanWrite)
                {
                    return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new NotSupportedException(SR.NotWriteableStream)));
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return ValueTask.FromCanceled(cancellationToken);
                }

                return WebSocket.SendAsync(buffer, _messageType, endOfMessage: true, cancellationToken);
            }

            /// <inheritdoc />
            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (!CanRead)
                {
                    throw new NotSupportedException(SR.NotReadableStream);
                }

                cancellationToken.ThrowIfCancellationRequested();

                while (WebSocket.State < WebSocketState.CloseReceived)
                {
                    ValueWebSocketReceiveResult result = await WebSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (result.MessageType is WebSocketMessageType.Close)
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
            public override async ValueTask DisposeAsync()
            {
                if (!_disposed)
                {
                    _disposed = true;

                    if (_closeTimeout is { } timeout)
                    {
                        if (WebSocket.State is < WebSocketState.Closed)
                        {
                            CancellationTokenSource? cts = null;
                            CancellationToken ct;

                            if (timeout == default)
                            {
                                ct = new CancellationToken(canceled: true);
                            }
                            else if (timeout == Timeout.InfiniteTimeSpan)
                            {
                                ct = CancellationToken.None;
                            }
                            else
                            {
                                cts = new CancellationTokenSource(timeout);
                                ct = cts.Token;
                            }

                            try
                            {
                                await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, ct).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                            }
                            finally
                            {
                                cts?.Dispose();
                            }
                        }

                        WebSocket.Dispose();
                    }
                }
            }
        }

        /// <summary>Provides a stream that wraps a <see cref="WebSocket"/> and writes a single message.</summary>
        private sealed class WriteMessageStream(WebSocket webSocket, WebSocketMessageType writeMessageType) : WebSocketStream(webSocket)
        {
            private readonly WebSocketMessageType _messageType = writeMessageType;

            /// <inheritdoc />
            public override bool CanRead => false;

            /// <inheritdoc />
            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_disposed)
                {
                    return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(GetType().FullName)));
                }

                if (!CanWrite)
                {
                    return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new NotSupportedException(SR.NotWriteableStream)));
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return ValueTask.FromCanceled(cancellationToken);
                }

                return WebSocket.SendAsync(buffer, _messageType, endOfMessage: false, cancellationToken);
            }

            public override ValueTask DisposeAsync()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    return WebSocket.SendAsync(ReadOnlyMemory<byte>.Empty, _messageType, endOfMessage: true, CancellationToken.None);
                }

                return default;
            }
        }

        /// <summary>Provides a stream that wraps a <see cref="WebSocket"/> and reads a single message.</summary>
        private sealed class ReadMessageStream(WebSocket webSocket) : WebSocketStream(webSocket)
        {
            /// <summary>Whether we've seen and end-of-message marker.</summary>
            private bool _eof;

            /// <inheritdoc />
            public override bool CanWrite => false;

            /// <inheritdoc />
            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (!CanRead)
                {
                    throw new NotSupportedException(SR.NotReadableStream);
                }

                cancellationToken.ThrowIfCancellationRequested();

                while (!_eof && WebSocket.State < WebSocketState.CloseReceived)
                {
                    ValueWebSocketReceiveResult result = await WebSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (result.MessageType is WebSocketMessageType.Close)
                    {
                        break;
                    }

                    if (result.EndOfMessage)
                    {
                        _eof = true;
                    }

                    if (result.Count > 0 || buffer.IsEmpty)
                    {
                        return result.Count;
                    }
                }

                return 0;
            }

            /// <inheritdoc />
            public override ValueTask DisposeAsync()
            {
                _disposed = true;
                if (!_eof && WebSocket.State < WebSocketState.CloseReceived)
                {
                    WebSocket.Abort();
                }
                return default;
            }
        }
    }
}
