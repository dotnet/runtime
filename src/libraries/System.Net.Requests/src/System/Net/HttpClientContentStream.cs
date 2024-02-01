// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal sealed class HttpClientContentStream : Stream
    {
        private readonly StreamBuffer _buffer;
        private bool _disposed;

        internal HttpClientContentStream(StreamBuffer buffer)
        {
            _buffer = buffer;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                _buffer.AbortRead();
                _buffer.Dispose();
            }

            base.Dispose(disposing);
        }

        public override bool CanRead => !_disposed;
        public override bool CanWrite => false;
        public override bool CanSeek => false;

        public override void Flush() => ThrowIfDisposed();
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            return Task.CompletedTask;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            ThrowIfDisposed();

            return _buffer.Read(new Span<byte>(buffer, offset, count));
        }

        public override int ReadByte()
        {
            ThrowIfDisposed();

            byte b = 0;
            int n = _buffer.Read(MemoryMarshal.CreateSpan(ref b, 1));
            return n != 0 ? b : -1;
        }

        public override int Read(Span<byte> buffer)
        {
            ThrowIfDisposed();

            return _buffer.Read(buffer);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count), callback, state);

        public override int EndRead(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();

            return TaskToAsyncResult.End<int>(asyncResult);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            ThrowIfDisposed();

            return _buffer.ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            return _buffer.ReadAsync(buffer, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void WriteByte(byte value) => throw new NotSupportedException();

        public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new NotSupportedException();

        public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();

        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        private void ThrowIfDisposed()
        {
            if (_disposed)
                ThrowDisposedException();

            [StackTraceHidden]
            [DoesNotReturn]
            static void ThrowDisposedException() => throw new ObjectDisposedException(nameof(HttpClientContentStream));
        }
    }
}
