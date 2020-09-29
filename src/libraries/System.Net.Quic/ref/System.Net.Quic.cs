// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.Quic
{
    public sealed class QuicStream : System.IO.Stream
    {
        internal QuicStream() => throw null;
        public override bool CanSeek => throw null;
        public override long Length => throw null;
        public override long Seek(long offset, System.IO.SeekOrigin origin) => throw null;
        public override void SetLength(long value) => throw null;
        public override long Position { get => throw null; set => throw null; }
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw null;
        public override int EndRead(IAsyncResult asyncResult) => throw null;
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw null;
        public override void EndWrite(IAsyncResult asyncResult) => throw null;
        public override int Read(byte[] buffer, int offset, int count) => throw null;
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) => throw null;
        public override void Write(byte[] buffer, int offset, int count) => throw null;
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) => throw null;
        public long StreamId => throw null;
        public override bool CanRead => throw null;
        public override int Read(Span<byte> buffer) => throw null;
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(Memory<byte> buffer, System.Threading.CancellationToken cancellationToken = default) => throw null;
        public override bool CanWrite => throw null;
        public override void Write(ReadOnlySpan<byte> buffer) => throw null;
        public override System.Threading.Tasks.ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default) => throw null;
        public override void Flush() => throw null;
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) => throw null;
        public void AbortRead(long errorCode) => throw null;
        public void AbortWrite(long errorCode) => throw null;
        public System.Threading.Tasks.ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, System.Threading.CancellationToken cancellationToken = default) => throw null;
        public System.Threading.Tasks.ValueTask WriteAsync(System.Buffers.ReadOnlySequence<byte> buffers, System.Threading.CancellationToken cancellationToken = default) => throw null;
        public System.Threading.Tasks.ValueTask WriteAsync(System.Buffers.ReadOnlySequence<byte> buffers, bool endStream, System.Threading.CancellationToken cancellationToken = default) => throw null;
        public System.Threading.Tasks.ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, System.Threading.CancellationToken cancellationToken = default) => throw null;
        public System.Threading.Tasks.ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, bool endStream, System.Threading.CancellationToken cancellationToken = default) => throw null;
        public System.Threading.Tasks.ValueTask ShutdownWriteCompleted(System.Threading.CancellationToken cancellationToken = default) => throw null;
        public void Shutdown() => throw null;
    }
    public class QuicException : Exception
    {
        public QuicException(string? message) { throw null; }
        public QuicException(string? message, Exception? innerException) { throw null; }
    }
    public class QuicConnectionAbortedException : QuicException
    {
        public QuicConnectionAbortedException(string message, long errorCode) : base(default) { throw null; }
        public long ErrorCode { get { throw null; } }
    }
    public class QuicOperationAbortedException : QuicException
    {
        public QuicOperationAbortedException(string message) : base(default) { throw null; }
    }
    public class QuicStreamAbortedException : QuicException
    {
        public QuicStreamAbortedException(string message, long errorCode) : base(default) { throw null; }
        public long ErrorCode { get { throw null; } }
    }
}
