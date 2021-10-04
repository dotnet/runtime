// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Net.Quic.Implementations;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic
{
    public sealed class QuicStream : Stream
    {
        private readonly QuicStreamProvider _provider;

        internal QuicStream(QuicStreamProvider provider)
        {
            _provider = provider;
        }

        //
        // Boilerplate implementation stuff
        //

        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count, default), callback, state);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count, default), callback, state);

        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return Read(buffer.AsSpan(offset, count));
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            Write(buffer.AsSpan(offset, count));
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        /// <summary>
        /// QUIC stream ID.
        /// </summary>
        public long StreamId => _provider.StreamId;

        public override bool CanRead => _provider.CanRead;

        public bool ReadsCompleted => _provider.ReadsCompleted;

        public override int Read(Span<byte> buffer) => _provider.Read(buffer);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _provider.ReadAsync(buffer, cancellationToken);

        public override bool CanWrite => _provider.CanWrite;

        public override void Write(ReadOnlySpan<byte> buffer) => _provider.Write(buffer);

        public override bool CanTimeout => _provider.CanTimeout;

        public override int ReadTimeout
        {
            get => _provider.ReadTimeout;
            set => _provider.ReadTimeout = value;
        }

        public override int WriteTimeout
        {
            get => _provider.WriteTimeout;
            set => _provider.WriteTimeout = value;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _provider.WriteAsync(buffer, cancellationToken);

        public override void Flush() => _provider.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => _provider.FlushAsync(cancellationToken);

        public void AbortRead(long errorCode) => _provider.AbortRead(errorCode);

        public void AbortWrite(long errorCode) => _provider.AbortWrite(errorCode);

        public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default) => _provider.WriteAsync(buffer, endStream, cancellationToken);

        public ValueTask WriteAsync(ReadOnlySequence<byte> buffers, CancellationToken cancellationToken = default) => _provider.WriteAsync(buffers, cancellationToken);

        public ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default) => _provider.WriteAsync(buffers, endStream, cancellationToken);

        public ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken = default) => _provider.WriteAsync(buffers, cancellationToken);

        public ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, bool endStream, CancellationToken cancellationToken = default) => _provider.WriteAsync(buffers, endStream, cancellationToken);

        public ValueTask ShutdownCompleted(CancellationToken cancellationToken = default) => _provider.ShutdownCompleted(cancellationToken);

        public ValueTask WaitForWriteCompletionAsync(CancellationToken cancellationToken = default) => _provider.WaitForWriteCompletionAsync(cancellationToken);

        public void Shutdown() => _provider.Shutdown();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _provider.Dispose();
            }
        }
    }
}
