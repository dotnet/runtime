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

        private readonly ResettableCompletionSource<int> _shutdownWriteCs = new ResettableCompletionSource<int>();

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

        private ValueTask WriteAsyncInternal(ReadOnlySpan<byte> buffer, bool endStream,
            CancellationToken cancellationToken)
        {
            // TODO-RZ: block until flow control limit is available
            OutboundBuffer!.Enqueue(buffer);
            if (endStream)
                OutboundBuffer!.MarkEndOfData();

            if (OutboundBuffer!.WrittenBytes - buffer.Length < OutboundBuffer.MaxData)
            {
                _streamCollection.MarkFlushable(this);
                _ctx.Ping();
            }

            return new ValueTask();
        }

        internal void NotifyShutdownWriteCompleted()
        {
            _shutdownWriteCs.Complete(0);
        }

        #region Public API
        internal override long StreamId { get; }
        internal override bool CanRead => InboundBuffer != null;

        internal override int Read(Span<byte> buffer)
        {
            ThrowIfDisposed();
            ThrowIfNotReadable();

            return InboundBuffer!.Deliver(buffer);
        }

        internal override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotReadable();

            return InboundBuffer!.DeliverAsync(buffer);
        }

        internal override void AbortRead(long errorCode) => throw new NotImplementedException();

        internal override void AbortWrite(long errorCode) => throw new NotImplementedException();

        internal override bool CanWrite => OutboundBuffer != null;
        internal override void Write(ReadOnlySpan<byte> buffer) => Write(buffer, false);

        internal void Write(ReadOnlySpan<byte> buffer, bool endStream)
        {
            ThrowIfDisposed();
            ThrowIfNotWritable();

            WriteAsyncInternal(buffer, endStream, CancellationToken.None).GetAwaiter().GetResult();
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
            return WriteAsyncInternal(buffer.Span, endStream, cancellationToken);
        }

        internal override async ValueTask WriteAsync(ReadOnlySequence<byte> buffers, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotWritable();

            foreach (ReadOnlyMemory<byte> buffer in buffers)
            {
                await WriteAsyncInternal(buffer.Span, false, cancellationToken);
            }
        }

        internal override async ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotWritable();

            foreach (ReadOnlyMemory<byte> buffer in buffers)
            {
                await WriteAsyncInternal(buffer.Span, false, cancellationToken);
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
                await WriteAsyncInternal(buffers.Span[i].Span, endStream && i == buffers.Length - 1,cancellationToken);
            }
        }

        internal override ValueTask ShutdownWriteCompleted(CancellationToken cancellationToken = default)
        {
            Shutdown();
            ThrowIfDisposed();
            ThrowIfNotWritable();

            return _shutdownWriteCs.GetTypelessValueTask();
        }

        internal override void Shutdown()
        {
            // TODO-RZ: is this really intened use for this method?
            if (CanWrite)
            {
                OutboundBuffer!.MarkEndOfData();
                // ensure that the stream is marked as flushable so that the fin bit is sent even if no more data was written since last time
                _streamCollection.MarkFlushable(this);
            }
        }

        internal override void Flush()
        {
            FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        internal override Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ThrowIfNotWritable();

            return OutboundBuffer!.FlushChunkAsync().AsTask();
        }

        public override void Dispose()
        {
            _disposed = true;
            // TODO-RZ: we might need to do more
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
