// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable  enable

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Streams;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed
{
    internal sealed class ManagedQuicStream : QuicStreamProvider
    {
        /// <summary>
        ///     Node to the linked list of all flushable streams. Should be accessed only by the <see cref="StreamCollection"/> class.
        /// </summary>
        internal readonly LinkedListNode<ManagedQuicStream> _flushableListNode;

        /// <summary>
        ///     Node to the linked list of all streams needing some kind of update other than sending data. This
        ///     includes Flow Control limits update and aborts.
        /// </summary>
        internal readonly LinkedListNode<ManagedQuicStream> _updateQueueListNode;

        /// <summary>
        ///     Value task source for signalling that <see cref="ShutdownWriteCompleted"/> has finished.
        /// </summary>
        private readonly SingleEventValueTaskSource _shutdownCompleted = new SingleEventValueTaskSource();

        /// <summary>
        ///     True if this instance has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        ///     Connection to which this stream belongs;
        /// </summary>
        private readonly ManagedQuicConnection _connection;

        /// <summary>
        ///     If the stream can receive data, contains the receiving part of the stream. Otherwise null.
        /// </summary>
        internal ReceiveStream? ReceiveStream { get; }

        /// <summary>
        ///     If the stream can send data, contains the sending part of the stream. Otherwise null.
        /// </summary>
        internal SendStream? SendStream { get; }

        internal ManagedQuicStream(long streamId, ReceiveStream? receiveStream, SendStream? sendStream, ManagedQuicConnection connection)
        {
            // trivial check whether buffer nullable combination makes sense with respect to streamId
            Debug.Assert(receiveStream != null || sendStream != null);
            Debug.Assert(StreamHelpers.IsBidirectional(streamId) == (receiveStream != null && sendStream != null));

            StreamId = streamId;
            ReceiveStream = receiveStream;
            SendStream = sendStream;
            _connection = connection;

            _flushableListNode = new LinkedListNode<ManagedQuicStream>(this);
            _updateQueueListNode = new LinkedListNode<ManagedQuicStream>(this);
        }

        private async ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> buffer, bool endStream,
            CancellationToken cancellationToken)
        {
            await SendStream!.EnqueueAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (endStream)
            {
                SendStream.MarkEndOfData();
                await SendStream.FlushChunkAsync(cancellationToken).ConfigureAwait(false);
            }

            if (SendStream.WrittenBytes - buffer.Length < SendStream.MaxData)
            {
                _connection.OnStreamDataWritten(this);
            }
        }

        internal void NotifyShutdownWriteCompleted()
        {
            _shutdownCompleted.TryComplete();
        }

        #region Public API
        internal override long StreamId { get; }
        internal override bool CanRead => ReceiveStream != null;

        internal override int Read(Span<byte> buffer)
        {
            ThrowIfDisposed();
            ThrowIfConnectionError();
            ThrowIfNotReadable();

            int result = ReceiveStream!.Deliver(buffer);
            if (result > 0)
            {
                _connection.OnStreamDataRead(this, result);
            }

            return result;
        }

        internal override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfConnectionError();
            ThrowIfNotReadable();

            int result = await ReceiveStream!.DeliverAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (result > 0)
            {
                _connection.OnStreamDataRead(this, result);
            }

            return result;
        }

        internal override void AbortRead(long errorCode)
        {
            ThrowIfDisposed();
            ThrowIfNotReadable();

            if (ReceiveStream!.Error != null) return;

            ReceiveStream.RequestAbort(errorCode);
            _connection.OnStreamStateUpdated(this);
        }

        internal override void AbortWrite(long errorCode)
        {
            ThrowIfDisposed();
            ThrowIfNotWritable();

            if (SendStream!.Error != null) return;

            SendStream.RequestAbort(errorCode);
            _shutdownCompleted.TryCompleteException(new QuicStreamAbortedException("Stream was aborted", errorCode));
            _connection.OnStreamStateUpdated(this);
        }

        internal override bool CanWrite => SendStream != null;
        internal override void Write(ReadOnlySpan<byte> buffer) => Write(buffer, false);

        internal void Write(ReadOnlySpan<byte> buffer, bool endStream)
        {
            ThrowIfDisposed();
            ThrowIfConnectionError();
            ThrowIfNotWritable();
            SendStream!.Enqueue(buffer);

            if (endStream)
            {
                SendStream.MarkEndOfData();
                SendStream.FlushChunk();
            }

            if (SendStream.WrittenBytes - buffer.Length < SendStream.MaxData)
            {
                _connection.OnStreamDataWritten(this);
            }
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffer, false, cancellationToken);
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfConnectionError();
            ThrowIfNotWritable();

            // TODO-RZ: optimize away some of the copying
            return WriteAsyncInternal(buffer, endStream, cancellationToken);
        }

        internal override async ValueTask WriteAsync(ReadOnlySequence<byte> buffers, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfConnectionError();
            ThrowIfNotWritable();

            foreach (ReadOnlyMemory<byte> buffer in buffers)
            {
                await WriteAsyncInternal(buffer, false, cancellationToken).ConfigureAwait(false);
            }
        }

        internal override async ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfConnectionError();
            ThrowIfNotWritable();

            foreach (ReadOnlyMemory<byte> buffer in buffers)
            {
                await WriteAsyncInternal(buffer, false, cancellationToken).ConfigureAwait(false);
            }

            SendStream!.MarkEndOfData();
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffers, false, cancellationToken);
        }

        internal override async ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfConnectionError();
            ThrowIfNotWritable();

            for (int i = 0; i < buffers.Span.Length; i++)
            {
                await WriteAsyncInternal(buffers.Span[i], endStream && i == buffers.Length - 1, cancellationToken).ConfigureAwait(false);
            }
        }

        internal override async ValueTask ShutdownWriteCompleted(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfConnectionError();
            ThrowIfNotWritable();

            SendStream!.MarkEndOfData();
            await SendStream!.FlushChunkAsync(cancellationToken).ConfigureAwait(false);
            _connection.OnStreamDataWritten(this);

            await using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                _shutdownCompleted.TryCompleteException(
                    new OperationCanceledException("Shutdown was cancelled", cancellationToken));
            });

            await _shutdownCompleted.GetTask().ConfigureAwait(false);
        }

        internal void OnFatalException(Exception exception)
        {
            ReceiveStream?.OnFatalException(exception);
            SendStream?.OnFatalException(exception);
        }

        internal void OnConnectionClosed(QuicConnectionAbortedException exception)
        {
            // closing connection (CONNECTION_CLOSE frame) causes all streams to become closed
            NotifyShutdownWriteCompleted();

            OnFatalException(exception);
        }

        internal override void Shutdown()
        {
            ThrowIfDisposed();
            ThrowIfConnectionError();
            ThrowIfNotWritable();

            if (CanWrite)
            {
                SendStream!.MarkEndOfData();
                SendStream!.FlushChunk();
                _connection.OnStreamDataWritten(this);
            }
        }

        internal override void Flush()
        {
            ThrowIfDisposed();
            ThrowIfConnectionError();
            ThrowIfNotWritable();

            SendStream!.FlushChunk();
            _connection.OnStreamDataWritten(this);
        }

        internal override async Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ThrowIfConnectionError();
            ThrowIfNotWritable();

            await SendStream!.FlushChunkAsync(cancellationToken).ConfigureAwait(false);
            _connection.OnStreamDataWritten(this);
        }

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (CanWrite)
            {
                SendStream!.MarkEndOfData();
                SendStream!.FlushChunk();
                _connection.OnStreamDataWritten(this);
            }

            if (CanRead)
            {
                // TODO-RZ: should we use this error code?
                ReceiveStream!.RequestAbort(0);
                _connection.OnStreamStateUpdated(this);
            }

            _disposed = true;
        }

        public override async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (CanWrite)
            {
                SendStream!.MarkEndOfData();
                await SendStream!.FlushChunkAsync().ConfigureAwait(false);
                _connection.OnStreamDataWritten(this);
            }

            if (CanRead)
            {
                // TODO-RZ: should we use this error code?
                ReceiveStream!.RequestAbort(0);
                _connection.OnStreamStateUpdated(this);
            }
        }

        #endregion

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ManagedQuicStream));
            }
        }

        private void ThrowIfNotWritable()
        {
            if (!CanWrite)
            {
                throw new InvalidOperationException("Writing is not allowed on this stream.");
            }

            // SendStream not null is implied by CanWrite
            if (SendStream!.Error != null)
            {
                throw new QuicStreamAbortedException("Writing was aborted on the stream",  SendStream.Error.Value);
            }
        }

        private void ThrowIfNotReadable()
        {
            if (!CanRead)
            {
                throw new InvalidOperationException("Reading is not allowed on this stream.");
            }

            // ReceiveStream not null is implied by CanRead
            if (ReceiveStream!.Error != null)
            {
                throw new QuicStreamAbortedException("Reading was aborted on the stream",  ReceiveStream.Error.Value);
            }
        }

        private void ThrowIfConnectionError()
        {
            _connection.ThrowIfError();
        }
    }
}
