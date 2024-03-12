// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    // Cache the request stream into a MemoryStream.
    internal sealed class RequestBufferingStream : Stream
    {
        private bool _disposed;
        private readonly MemoryStream _buffer = new MemoryStream();

        public RequestBufferingStream()
        {
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override void Flush() => ThrowIfDisposed(); // Nothing to do.

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            // Nothing to do.
            return cancellationToken.IsCancellationRequested ?
                Task.FromCanceled(cancellationToken) :
                Task.CompletedTask;
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, count);
            _buffer.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, count);
            return _buffer.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _buffer.WriteAsync(buffer, cancellationToken);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, count);
            return _buffer.BeginWrite(buffer, offset, count, asyncCallback, asyncState);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();
            _buffer.EndWrite(asyncResult);
        }

        public ArraySegment<byte> GetBuffer()
        {
            ArraySegment<byte> bytes;

            bool success = _buffer.TryGetBuffer(out bytes);
            Debug.Assert(success); // Buffer should always be visible since default MemoryStream constructor was used.

            return bytes;
        }

        // We need this to dispose the MemoryStream.
        public MemoryStream GetMemoryStream()
        {
            return _buffer;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
