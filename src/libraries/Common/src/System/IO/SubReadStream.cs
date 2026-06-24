// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    // Stream that wraps a super stream and exposes a window [startPosition, startPosition + maxLength).
    // Supports both seekable and unseekable super streams. When the super stream is seekable, this stream
    // is seekable too and will reposition the super stream before each read if needed. When the super
    // stream is unseekable, reads must be sequential and Seek/Position-set throw.
    // Does not support writing.
    internal sealed class SubReadStream : Stream
    {
        private const int MaxAdvanceBufferLength = 4096;

        private bool _hasReachedEnd;
        private readonly long _startInSuperStream;
        private long _positionInSuperStream;
        private readonly long _endInSuperStream;
        private readonly Stream _superStream;
        private bool _isDisposed;

        public SubReadStream(Stream superStream, long startPosition, long maxLength)
        {
            ArgumentNullException.ThrowIfNull(superStream);
            if (!superStream.CanRead)
            {
                throw new ArgumentException(SR.IO_NotSupported_UnreadableStream, nameof(superStream));
            }
            ArgumentOutOfRangeException.ThrowIfNegative(startPosition);
            ArgumentOutOfRangeException.ThrowIfNegative(maxLength);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(startPosition, long.MaxValue - maxLength);

            _startInSuperStream = startPosition;
            _positionInSuperStream = startPosition;
            _endInSuperStream = startPosition + maxLength;
            _superStream = superStream;
        }

        public override long Length
        {
            get
            {
                ThrowIfDisposed();
                return _endInSuperStream - _startInSuperStream;
            }
        }

        public override long Position
        {
            get
            {
                ThrowIfDisposed();
                return _positionInSuperStream - _startInSuperStream;
            }
            set
            {
                ThrowIfDisposed();
                if (!CanSeek)
                {
                    throw new NotSupportedException(SR.IO_NotSupported_UnseekableStream);
                }
                ArgumentOutOfRangeException.ThrowIfNegative(value);

                long newPositionInSuperStream = _startInSuperStream + value;
                _superStream.Position = newPositionInSuperStream;
                _positionInSuperStream = newPositionInSuperStream;
            }
        }

        public override bool CanRead => !_isDisposed && _superStream.CanRead;

        public override bool CanSeek => !_isDisposed && _superStream.CanSeek;

        public override bool CanWrite => false;

        private long Remaining => _endInSuperStream - _positionInSuperStream;

        private int LimitByRemaining(int bufferSize) => (int)Math.Max(0, Math.Min(Remaining, bufferSize));

        // Positions the super stream past the end of this stream's window. After calling this method,
        // subsequent reads on this stream throw <see cref="EndOfStreamException"/>.
        internal void AdvanceToEnd()
        {
            _hasReachedEnd = true;

            long remaining = Remaining;
            _positionInSuperStream = _endInSuperStream;
            AdvanceSuperStream(remaining);
        }

        internal ValueTask AdvanceToEndAsync(CancellationToken cancellationToken)
        {
            _hasReachedEnd = true;

            long remaining = Remaining;
            _positionInSuperStream = _endInSuperStream;
            return AdvanceSuperStreamAsync(remaining, cancellationToken);
        }

        private void AdvanceSuperStream(long bytesToDiscard)
        {
            if (_superStream.CanSeek)
            {
                _superStream.Position += bytesToDiscard;
            }
            else if (bytesToDiscard > 0)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent((int)Math.Min(MaxAdvanceBufferLength, bytesToDiscard));
                try
                {
                    while (bytesToDiscard > 0)
                    {
                        int currentLengthToRead = (int)Math.Min(MaxAdvanceBufferLength, bytesToDiscard);
                        _superStream.ReadExactly(buffer.AsSpan(0, currentLengthToRead));
                        bytesToDiscard -= currentLengthToRead;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        private async ValueTask AdvanceSuperStreamAsync(long bytesToDiscard, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_superStream.CanSeek)
            {
                _superStream.Position += bytesToDiscard;
            }
            else if (bytesToDiscard > 0)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent((int)Math.Min(MaxAdvanceBufferLength, bytesToDiscard));
                try
                {
                    while (bytesToDiscard > 0)
                    {
                        int currentLengthToRead = (int)Math.Min(MaxAdvanceBufferLength, bytesToDiscard);
                        await _superStream.ReadExactlyAsync(buffer, 0, currentLengthToRead, cancellationToken).ConfigureAwait(false);
                        bytesToDiscard -= currentLengthToRead;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
        }

        private void ThrowIfCantRead()
        {
            if (!_superStream.CanRead)
            {
                throw new NotSupportedException(SR.IO_NotSupported_UnreadableStream);
            }
        }

        private void ThrowIfBeyondEndOfStream()
        {
            if (_hasReachedEnd)
            {
                throw new EndOfStreamException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return Read(buffer.AsSpan(offset, count));
        }

        public override int Read(Span<byte> destination)
        {
            ThrowIfDisposed();
            ThrowIfCantRead();
            ThrowIfBeyondEndOfStream();

            if (_superStream.CanSeek && _superStream.Position != _positionInSuperStream)
            {
                _superStream.Seek(_positionInSuperStream, SeekOrigin.Begin);
            }

            destination = destination[..LimitByRemaining(destination.Length)];

            int ret = _superStream.Read(destination);

            _positionInSuperStream += ret;
            return ret;
        }

        public override int ReadByte()
        {
            byte b = default;
            return Read(new Span<byte>(ref b)) == 1 ? b : -1;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfCantRead();
            ThrowIfBeyondEndOfStream();
            return ReadAsyncCore(buffer, cancellationToken);
        }

        private async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(!_hasReachedEnd);

            cancellationToken.ThrowIfCancellationRequested();

            if (_superStream.CanSeek && _superStream.Position != _positionInSuperStream)
            {
                _superStream.Seek(_positionInSuperStream, SeekOrigin.Begin);
            }

            buffer = buffer[..LimitByRemaining(buffer.Length)];

            int ret = await _superStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

            _positionInSuperStream += ret;
            return ret;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();

            if (!CanSeek)
            {
                throw new NotSupportedException(SR.IO_NotSupported_UnseekableStream);
            }

            long newPositionInSuperStream = origin switch
            {
                SeekOrigin.Begin => _startInSuperStream + offset,
                SeekOrigin.Current => _positionInSuperStream + offset,
                SeekOrigin.End => _endInSuperStream + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin)),
            };

            if (newPositionInSuperStream < _startInSuperStream)
            {
                throw new IOException(SR.IO_SeekBeforeBegin);
            }

            long actualPositionInSuperStream = _superStream.Seek(newPositionInSuperStream, SeekOrigin.Begin);
            _positionInSuperStream = actualPositionInSuperStream;

            return _positionInSuperStream - _startInSuperStream;
        }

        public override void SetLength(long value) => throw new NotSupportedException(SR.IO_NotSupported_UnwritableStream);

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException(SR.IO_NotSupported_UnwritableStream);

        public override void Flush() { }

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) :
            Task.CompletedTask;

        // Close the stream for reading. Note that this does NOT close the super stream (since
        // this stream is just a 'chunk' of the super stream).
        protected override void Dispose(bool disposing)
        {
            _isDisposed = true;
            base.Dispose(disposing);
        }
    }
}
