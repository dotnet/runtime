#nullable  enable

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Buffers;
using System.Net.Quic.Implementations.MsQuic.Internal;
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
        ///     Node to the linked list of all streams needing flow control update. Should be accessed only by the <see cref="StreamCollection"/> class.
        /// </summary>
        internal readonly LinkedListNode<ManagedQuicStream> _flowControlUpdateQueueListNode;

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
            _flowControlUpdateQueueListNode = new LinkedListNode<ManagedQuicStream>(this);
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
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            ThrowIfDisposed();
            ThrowIfNotReadable();

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
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            ThrowIfDisposed();
            ThrowIfNotReadable();

            int result = await InboundBuffer!.DeliverAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (result > 0)
            {
                _connection.OnStreamDataRead(this, result);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return result;
        }

        internal override void AbortRead(long errorCode) => throw new NotImplementedException();

        internal override void AbortWrite(long errorCode) => throw new NotImplementedException();

        internal override bool CanWrite => OutboundBuffer != null;
        internal override void Write(ReadOnlySpan<byte> buffer) => Write(buffer, false);

        internal void Write(ReadOnlySpan<byte> buffer, bool endStream)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            ThrowIfDisposed();
            ThrowIfNotWritable();

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
            ThrowIfNotWritable();

            // TODO-RZ: optimize away some of the copying
            return WriteAsyncInternal(buffer, endStream, cancellationToken);
        }

        internal override async ValueTask WriteAsync(ReadOnlySequence<byte> buffers, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotWritable();

            foreach (ReadOnlyMemory<byte> buffer in buffers)
            {
                await WriteAsyncInternal(buffer, false, cancellationToken).ConfigureAwait(false);
            }
        }

        internal override async ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
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
            ThrowIfNotWritable();

            for (int i = 0; i < buffers.Span.Length; i++)
            {
                await WriteAsyncInternal(buffers.Span[i], endStream && i == buffers.Length - 1,cancellationToken).ConfigureAwait(false);
            }
        }

        internal override async ValueTask ShutdownWriteCompleted(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotWritable();

            if (CanWrite)
            {
                OutboundBuffer!.MarkEndOfData();
                await OutboundBuffer!.FlushChunkAsync(cancellationToken).ConfigureAwait(false);
                _connection.OnStreamDataWritten(this);
            }

            // TODO-RZ: cancellation
            await _shutdownCompleted.GetTask();
        }


        internal void OnConnectionClosed()
        {
            // closing connection (CONNECTION_CLOSE frame) causes all streams to become closed
            NotifyShutdownWriteCompleted();

            // TODO-RZ: handle callers blocking on async tasks
        }

        internal override void Shutdown()
        {
            ThrowIfDisposed();
            // ThrowIfNotWritable();
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            // TODO-RZ: is this really intened use for this method?
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
            ThrowIfNotWritable();

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            OutboundBuffer!.FlushChunk();
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override async Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ThrowIfNotWritable();

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            await OutboundBuffer!.FlushChunkAsync(cancellationToken).ConfigureAwait(false);
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        public override void Dispose()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            _disposed = true;
            // TODO-RZ: we might need to do more
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        public override ValueTask DisposeAsync() => throw new NotImplementedException();

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
        }

        private void ThrowIfNotReadable()
        {
            if (!CanRead)
            {
                throw new InvalidOperationException("Reading is not allowed on this stream.");
            }
        }
    }
}
