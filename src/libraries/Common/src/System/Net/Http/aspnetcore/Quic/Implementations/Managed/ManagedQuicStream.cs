using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Managed
{
    internal class ManagedQuicStream : QuicStreamProvider
    {
        internal override long StreamId { get; }
        internal override bool CanRead { get; }
        internal override int Read(Span<byte> buffer) => throw new NotImplementedException();

        internal override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        internal override void AbortRead(long errorCode) => throw new NotImplementedException();

        internal override void AbortWrite(long errorCode) => throw new NotImplementedException();

        internal override bool CanWrite { get; }
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
    }
}
