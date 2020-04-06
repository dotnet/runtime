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
        // TODO-RZ: think about thread-safety of the buffers, and who can access which parts of them
        internal InboundBuffer? InboundBuffer { get; }

        internal OutboundBuffer? OutboundBuffer { get; }

        internal ManagedQuicStream(long streamId, InboundBuffer? inboundBuffer, OutboundBuffer? outboundBuffer)
        {
            // trivial check whether buffer nullable combination makes sense with respect to streamId
            Debug.Assert(inboundBuffer != null || outboundBuffer != null);
            Debug.Assert(StreamHelpers.IsBidirectional(streamId) == (inboundBuffer != null && outboundBuffer != null));

            StreamId = streamId;
            InboundBuffer = inboundBuffer;
            OutboundBuffer = outboundBuffer;
        }

        #region Public API
        internal override long StreamId { get; }
        internal override bool CanRead => InboundBuffer != null;
        internal override int Read(Span<byte> buffer) => throw new NotImplementedException();

        internal override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override void AbortRead(long errorCode) => throw new NotImplementedException();

        internal override void AbortWrite(long errorCode) => throw new NotImplementedException();

        internal override bool CanWrite => OutboundBuffer != null;
        internal override void Write(ReadOnlySpan<byte> buffer) => throw new NotImplementedException();

        internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override ValueTask WriteAsync(ReadOnlySequence<byte> buffers, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, bool endStream, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override ValueTask ShutdownWriteCompleted(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override void Shutdown() => throw new NotImplementedException();

        internal override void Flush() => throw new NotImplementedException();

        internal override Task FlushAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        public override void Dispose() => throw new NotImplementedException();

        public override ValueTask DisposeAsync() => throw new NotImplementedException();

        #endregion
    }
}
