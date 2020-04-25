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
        internal readonly LinkedListNode<ManagedQuicStream> _flushableListNode;

        private readonly SingleEventValueTaskSource _shutdownCompleted = new SingleEventValueTaskSource();

        private bool _disposed;

        /// <summary>
        ///     Stream collection to which this stream belongs.
        /// </summary>
        private readonly StreamCollection _streamCollection;

        private readonly QuicSocketContext _ctx;

        // TODO-RZ: think about thread-safety of the buffers, and who can access which parts of them
        internal InboundBuffer? InboundBuffer { get; }

        internal OutboundBuffer? OutboundBuffer { get; }

        internal ManagedQuicStream(long streamId, InboundBuffer? inboundBuffer, OutboundBuffer? outboundBuffer, StreamCollection streamCollection, QuicSocketContext ctx)
        {
            // trivial check whether buffer nullable combination makes sense with respect to streamId
            Debug.Assert(inboundBuffer != null || outboundBuffer != null);
            Debug.Assert(StreamHelpers.IsBidirectional(streamId) == (inboundBuffer != null && outboundBuffer != null));

            StreamId = streamId;
            InboundBuffer = inboundBuffer;
            OutboundBuffer = outboundBuffer;
            _streamCollection = streamCollection;
            _ctx = ctx;
            _flushableListNode = new LinkedListNode<ManagedQuicStream>(this);
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
                RequestUpdate();
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
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return result;
        }

        internal override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            ThrowIfDisposed();
            ThrowIfNotReadable();

            int result = await InboundBuffer!.DeliverAsync(buffer, cancellationToken).ConfigureAwait(false);
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
                RequestUpdate();
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
                RequestUpdate();
            }

            // TODO-RZ: cancellation
            await _shutdownCompleted.GetTask();
        }

        private void RequestUpdate()
        {
            _streamCollection.MarkFlushable(this);
            _ctx.Ping();
        }

        internal void OnConnectionClosed()
        {
            // closing connection (CONNECTION_CLOSE frame) causes all streams to become closed
            NotifyShutdownWriteCompleted();
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
                RequestUpdate();
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
