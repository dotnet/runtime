// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>
    /// <see cref="DuplexStream"/> is a <see cref="Stream"/> that has independent read and write streams.
    /// </summary>
    public abstract class DuplexStream : Stream
    {
        /// <summary>
        /// Gets a write-only <see cref="Stream"/> for the <see cref="DuplexStream"/>.
        /// When disposed, the <see cref="Stream"/> will call <see cref="CompleteWrites"/>.
        /// </summary>
        /// <returns>A new write-only instance of the <see cref="DuplexStream"/>.</returns>
        public Stream GetWriteOnlyStream() =>
            new WriteOnlyProxyStream(this);

        /// <summary>
        /// Flushes the <see cref="DuplexStream"/> and completes the write stream,
        /// allowing a peer to observe end of stream. There must be no further writes
        /// to this <see cref="DuplexStream"/>, but reads may continue.
        /// </summary>
        /// <remarks>
        /// Successful completion of <see cref="CompleteWrites"/> does not indicate
        /// that the peer has acknowledged the end of stream.
        /// </remarks>
        public abstract void CompleteWrites();

        /// <summary>
        /// Flushes the <see cref="DuplexStream"/> and completes the write stream,
        /// allowing a peer to observe end of stream. There must be no further writes
        /// to this <see cref="DuplexStream"/>, but reads may continue.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous write completion.</returns>
        /// <remarks>
        /// Successful completion of <see cref="CompleteWritesAsync"/> does not indicate
        /// that the peer has acknowledged the end of stream.
        /// </remarks>
        public abstract ValueTask CompleteWritesAsync(CancellationToken cancellationToken = default);

        private sealed class WriteOnlyProxyStream : Stream
        {
            private readonly DuplexStream _stream;
            private int _disposed;

            public override bool CanRead => false;
            public override bool CanSeek => _disposed == 0 ? _stream.CanSeek : false;
            public override bool CanWrite => _disposed == 0 ? _stream.CanWrite : false;
            public override bool CanTimeout => _disposed == 0 ? _stream.CanTimeout : false;

            public override long Length => _stream.Length;

            public override long Position
            {
                get
                {
                    ThrowIfDisposed();
                    return _stream.Position;
                }
                set
                {
                    ThrowIfDisposed();
                    _stream.Position = value;
                }
            }

            public override int ReadTimeout
            {
                get => throw CreateNotSupportedException();
                set => throw CreateNotSupportedException();
            }

            public override int WriteTimeout
            {
                get
                {
                    ThrowIfDisposed();
                    return _stream.WriteTimeout;
                }
                set
                {
                    ThrowIfDisposed();
                    _stream.WriteTimeout = value;
                }
            }

            public WriteOnlyProxyStream(DuplexStream stream)
            {
                _stream = stream;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (Interlocked.Exchange(ref _disposed, 1) == 0)
                    {
                        _stream.CompleteWrites();
                    }
                }

                base.Dispose(disposing);
            }

            public override async ValueTask DisposeAsync()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    await _stream.CompleteWritesAsync().ConfigureAwait(false);
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }

            public override int ReadByte() =>
                throw CreateNotSupportedException();

            public override int Read(byte[] buffer, int offset, int count) =>
                throw CreateNotSupportedException();

            public override int Read(Span<byte> buffer) =>
                throw CreateNotSupportedException();

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                throw CreateNotSupportedException();

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
                throw CreateNotSupportedException();

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
                TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);

            public override int EndRead(IAsyncResult asyncResult) =>
                TaskToApm.End<int>(asyncResult);

            public override void WriteByte(byte value)
            {
                ThrowIfDisposed();
                _stream.WriteByte(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                ThrowIfDisposed();
                _stream.Write(buffer, offset, count);
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                ThrowIfDisposed();
                _stream.Write(buffer);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                return _stream.WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                ThrowIfDisposed();
                return _stream.WriteAsync(buffer, cancellationToken);
            }

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
                TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);

            public override void EndWrite(IAsyncResult asyncResult) =>
                TaskToApm.End(asyncResult);

            public override void Flush()
            {
                ThrowIfDisposed();
                _stream.Flush();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                return _stream.FlushAsync(cancellationToken);
            }

            public override void CopyTo(Stream destination, int bufferSize) =>
                throw CreateNotSupportedException();

            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
                throw CreateNotSupportedException();

            public override long Seek(long offset, SeekOrigin origin)
            {
                ThrowIfDisposed();
                return _stream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                ThrowIfDisposed();
                _stream.SetLength(value);
            }

            private void ThrowIfDisposed()
            {
                if (_disposed != 0) ThrowException();
                static void ThrowException() => throw new ObjectDisposedException(nameof(WriteOnlyProxyStream));
            }

            private Exception CreateNotSupportedException() =>
                new NotSupportedException(SR.DuplexStream_InvalidReadOnWriteOnlyStream);
        }
    }
}
