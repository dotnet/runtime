// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    // Cache the request stream into a MemoryStream.  This is the
    // default behavior of Desktop HttpWebRequest.AllowWriteStreamBuffering (true).
    // Unfortunately, this property is not exposed in .NET Core, so it can't be changed
    // This will result in inefficient memory usage when sending (POST'ing) large
    // amounts of data to the server such as from a file stream.
    internal sealed class RequestStream : Stream
    {
        private readonly MemoryStream? _buffer;
        private readonly StreamBuffer _streamBuffer;
        private readonly bool _isBuffered;
        private bool _disposed;

        public RequestStream(StreamBuffer streamBuffer, bool isBuffered)
        {
            if (isBuffered)
            {
                _buffer = new MemoryStream();
            }
            _streamBuffer = streamBuffer;
            _isBuffered = isBuffered;
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        // We're already sending data as StreamContent,
        // so we buffer data in memory and flush it in three cases:
        // - When GetResponse called.
        // - When RequestStream is getting disposed.
        // - When user calls Flush.
        public override void Flush()
        {
            ThrowIfDisposed();

            if (_isBuffered && _buffer!.Length > 0)
            {
                _streamBuffer.Write(_buffer!.GetBuffer().AsSpan(0, (int) _buffer!.Length));
                _buffer!.SetLength(0);
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_isBuffered && _buffer!.Length > 0)
            {
                Task task = _streamBuffer.WriteAsync(_buffer.GetBuffer().AsMemory(0, (int) _buffer.Length), cancellationToken).AsTask();
                _buffer.SetLength(0);
                return task;
            }

            return !cancellationToken.IsCancellationRequested ? Task.CompletedTask : Task.FromCanceled(cancellationToken);
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
            if (_isBuffered)
            {
                _buffer!.Write(buffer, offset, count);
            }
            else
            {
                _streamBuffer.Write(new(buffer, offset, count));
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, count);
            return _isBuffered ? _buffer!.WriteAsync(buffer, offset, count, cancellationToken) :
                _streamBuffer.WriteAsync(new(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            return _isBuffered ? _buffer!.WriteAsync(buffer, cancellationToken) :
                _streamBuffer.WriteAsync(buffer, cancellationToken);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, count);
            return _isBuffered ? _buffer!.BeginWrite(buffer, offset, count, asyncCallback, asyncState) :
                TaskToAsyncResult.Begin(_streamBuffer.WriteAsync(new(buffer, offset, count)).AsTask(), asyncCallback, asyncState);
        }

        public void EndWriteOnStreamBuffer()
        {
            _streamBuffer.EndWrite();
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();
            if (_isBuffered)
            {
                _buffer!.EndWrite(asyncResult);
            }
            else
            {
                TaskToAsyncResult.End(asyncResult);
            }
        }

        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();
            Flush();

            if (disposing && !_disposed)
            {
                _disposed = true;
                _streamBuffer.EndWrite();
            }

            base.Dispose(disposing);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                ThrowDisposedException();

            [StackTraceHidden]
            static void ThrowDisposedException() => throw new ObjectDisposedException(nameof(RequestStream));
        }
    }
}
