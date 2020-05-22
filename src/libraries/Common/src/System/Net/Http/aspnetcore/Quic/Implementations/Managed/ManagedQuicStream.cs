#nullable  enable

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Buffers;
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
        ///     If the stream can receive data, contains buffer representing receiving part of the stream. Otherwise null.
        /// </summary>
        internal InboundBuffer? InboundBuffer { get; }

        /// <summary>
        ///     If the stream can send data, contains buffer representing sending part of the stream.
        /// </summary>
        internal OutboundBuffer? OutboundBuffer { get; }

        internal ManagedQuicStream(long streamId, InboundBuffer? inboundBuffer, OutboundBuffer? outboundBuffer, ManagedQuicConnection connection)
        {
            // trivial check whether buffer nullable combination makes sense with respect to streamId
            Debug.Assert(inboundBuffer != null || outboundBuffer != null);
            Debug.Assert(StreamHelpers.IsBidirectional(streamId) == (inboundBuffer != null && outboundBuffer != null));

            StreamId = streamId;
            InboundBuffer = inboundBuffer;
            OutboundBuffer = outboundBuffer;
            _connection = connection;

            _flushableListNode = new LinkedListNode<ManagedQuicStream>(this);
            _updateQueueListNode = new LinkedListNode<ManagedQuicStream>(this);
        }

        private async ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> buffer, bool endStream,
            CancellationToken cancellationToken)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            await OutboundBuffer!.EnqueueAsync(buffer, cancellationToken).ConfigureAwait(false);

            if (endStream)
                OutboundBuffer!.MarkEndOfData();

            if (OutboundBuffer!.WrittenBytes - buffer.Length < OutboundBuffer.MaxData)
            {
                _connection.OnStreamDataWritten(this);
            }
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal void NotifyShutdownWriteCompleted()
        {
            _shutdownCompleted.TryComplete();
        }

        #region Public API
        internal override long StreamId { get; }
        internal override bool CanRead => InboundBuffer != null;

        internal override int Read(Span<byte> buffer)
        {
            ThrowIfDisposed();
            ThrowIfError();
            ThrowIfNotReadable();

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            int result = InboundBuffer!.Deliver(buffer);
            if (result > 0)
            {
                _connection.OnStreamDataRead(this, result);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return result;
        }

        internal override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfError();
            ThrowIfNotReadable();

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            int result = await InboundBuffer!.DeliverAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (result > 0)
            {
                _connection.OnStreamDataRead(this, result);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return result;
        }

        internal override void AbortRead(long errorCode)
        {
            ThrowIfDisposed();
            ThrowIfNotReadable();

            if (InboundBuffer!.Error != null) return;

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            InboundBuffer.RequestAbort(errorCode);
            _connection.OnStreamStateUpdated(this);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override void AbortWrite(long errorCode)
        {
            ThrowIfDisposed();
            ThrowIfNotWritable();

            if (OutboundBuffer!.Error != null) return;

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            OutboundBuffer.Abort(errorCode);
            _shutdownCompleted.TryCompleteException(new QuicStreamAbortedException("Stream was aborted", errorCode));
            _connection.OnStreamStateUpdated(this);
            // TODO-RZ: abort current writes

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override bool CanWrite => OutboundBuffer != null;
        internal override void Write(ReadOnlySpan<byte> buffer) => Write(buffer, false);

        internal void Write(ReadOnlySpan<byte> buffer, bool endStream)
        {
            ThrowIfDisposed();
            ThrowIfError();
            ThrowIfNotWritable();

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            OutboundBuffer!.Enqueue(buffer);

            if (endStream)
                OutboundBuffer!.MarkEndOfData();

            if (OutboundBuffer!.WrittenBytes - buffer.Length < OutboundBuffer.MaxData)
            {
                _connection.OnStreamDataWritten(this);
            }
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffer, false, cancellationToken);
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfError();
            ThrowIfNotWritable();

            // TODO-RZ: optimize away some of the copying
            return WriteAsyncInternal(buffer, endStream, cancellationToken);
        }

        internal override async ValueTask WriteAsync(ReadOnlySequence<byte> buffers, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfError();
            ThrowIfNotWritable();

            foreach (ReadOnlyMemory<byte> buffer in buffers)
            {
                await WriteAsyncInternal(buffer, false, cancellationToken).ConfigureAwait(false);
            }
        }

        internal override async ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfError();
            ThrowIfNotWritable();

            foreach (ReadOnlyMemory<byte> buffer in buffers)
            {
                await WriteAsyncInternal(buffer, false, cancellationToken).ConfigureAwait(false);
            }

            OutboundBuffer!.MarkEndOfData();
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffers, false, cancellationToken);
        }

        internal override async ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfError();
            ThrowIfNotWritable();

            for (int i = 0; i < buffers.Span.Length; i++)
            {
                await WriteAsyncInternal(buffers.Span[i], endStream && i == buffers.Length - 1, cancellationToken).ConfigureAwait(false);
            }
        }

        internal override async ValueTask ShutdownWriteCompleted(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfError();
            ThrowIfNotWritable();

            OutboundBuffer!.MarkEndOfData();
            await OutboundBuffer!.FlushChunkAsync(cancellationToken).ConfigureAwait(false);
            _connection.OnStreamDataWritten(this);

            await using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                _shutdownCompleted.TryCompleteException(
                    new OperationCanceledException("Shutdown was cancelled", cancellationToken));
            });

            await _shutdownCompleted.GetTask().ConfigureAwait(false);
        }


        internal void OnConnectionClosed(QuicConnectionAbortedException exception)
        {
            // closing connection (CONNECTION_CLOSE frame) causes all streams to become closed
            NotifyShutdownWriteCompleted();

            // TODO-RZ: handle callers blocking on other async tasks
            InboundBuffer?.OnConnectionError(exception);
        }

        internal override void Shutdown()
        {
            ThrowIfDisposed();
            ThrowIfError();
            ThrowIfNotWritable();

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            // TODO-RZ: is this really intended use for this method?
            if (CanWrite)
            {
                OutboundBuffer!.MarkEndOfData();
                OutboundBuffer!.FlushChunk();
                _connection.OnStreamDataWritten(this);
            }
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override void Flush()
        {
            ThrowIfDisposed();
            ThrowIfError();
            ThrowIfNotWritable();

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            OutboundBuffer!.FlushChunk();
            _connection.OnStreamDataWritten(this);
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override async Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ThrowIfError();
            ThrowIfNotWritable();

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            await OutboundBuffer!.FlushChunkAsync(cancellationToken).ConfigureAwait(false);
            _connection.OnStreamDataWritten(this);
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            if (CanWrite)
            {
                OutboundBuffer!.MarkEndOfData();
                OutboundBuffer!.FlushChunk();
                _connection.OnStreamDataWritten(this);
            }

            if (CanRead)
            {
                // TODO-RZ: should we use this error code?
                InboundBuffer!.RequestAbort(0);
                _connection.OnStreamStateUpdated(this);
            }

            _disposed = true;
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        public override async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            if (CanWrite)
            {
                OutboundBuffer!.MarkEndOfData();
                await OutboundBuffer!.FlushChunkAsync().ConfigureAwait(false);
                _connection.OnStreamDataWritten(this);
            }

            if (CanRead)
            {
                // TODO-RZ: should we use this error code?
                InboundBuffer!.RequestAbort(0);
                _connection.OnStreamStateUpdated(this);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
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

            // OutboundBuffer not null is implied by CanWrite
            if (OutboundBuffer!.Error != null)
            {
                throw new QuicStreamAbortedException("Writing was aborted on the stream",  OutboundBuffer.Error.Value);
            }
        }

        private void ThrowIfNotReadable()
        {
            if (!CanRead)
            {
                throw new InvalidOperationException("Reading is not allowed on this stream.");
            }

            // InboundBuffer not null is implied by CanRead
            if (InboundBuffer!.Error != null)
            {
                throw new QuicStreamAbortedException("Reading was aborted on the stream",  InboundBuffer.Error.Value);
            }
        }

        private void ThrowIfError()
        {
            _connection.ThrowIfError();
        }
    }
}
