#nullable  enable

using System.Buffers;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed
{
    internal class ManagedQuicStream : QuicStreamProvider
    {
        private bool _disposed;

        /// <summary>
        ///     Stream collection to which this stream belongs.
        /// </summary>
        private readonly StreamCollection _streamCollection;

        // TODO-RZ: think about thread-safety of the buffers, and who can access which parts of them
        internal InboundBuffer? InboundBuffer { get; }

        internal OutboundBuffer? OutboundBuffer { get; }

        internal ManagedQuicStream(long streamId, InboundBuffer? inboundBuffer, OutboundBuffer? outboundBuffer, StreamCollection streamCollection)
        {
            // trivial check whether buffer nullable combination makes sense with respect to streamId
            Debug.Assert(inboundBuffer != null || outboundBuffer != null);
            Debug.Assert(StreamHelpers.IsBidirectional(streamId) == (inboundBuffer != null && outboundBuffer != null));

            StreamId = streamId;
            InboundBuffer = inboundBuffer;
            OutboundBuffer = outboundBuffer;
            _streamCollection = streamCollection;
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

        internal override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override void AbortRead(long errorCode) => throw new NotImplementedException();

        internal override void AbortWrite(long errorCode) => throw new NotImplementedException();

        internal override bool CanWrite => OutboundBuffer != null;
        internal override void Write(ReadOnlySpan<byte> buffer) => Write(buffer, false);

        internal void Write(ReadOnlySpan<byte> buffer, bool endStream)
        {
            ThrowIfDisposed();
            ThrowIfNotWritable();

            OutboundBuffer!.Enqueue(buffer);
            if (OutboundBuffer.HasPendingData)
                _streamCollection.MarkFlushable(this, true);
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override ValueTask WriteAsync(ReadOnlySequence<byte> buffers, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, bool endStream, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override ValueTask ShutdownWriteCompleted(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override void Shutdown()
        {
            // TODO-RZ: is this really intened use for this method?
            if (CanWrite)
            {
                OutboundBuffer!.MarkEndOfData();
                // ensure that the stream is marked as flushable so that the fin bit is sent even if no more data was written since last time
                _streamCollection.MarkFlushable(this, true);
            }
        }

        internal override void Flush() => throw new NotImplementedException();

        internal override Task FlushAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        public override void Dispose()
        {
            _disposed = true;
            throw new NotImplementedException();
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
