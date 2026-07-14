// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>
    /// Provides a seekable, read-only <see cref="Stream"/> over a <see cref="ReadOnlyMemory{Byte}"/>.
    /// </summary>
    /// <remarks>
    /// <para>The stream cannot be written to. <see cref="CanWrite"/> always returns <see langword="false"/>.</para>
    /// </remarks>
    public sealed class ReadOnlyMemoryStream : Stream
    {
        private ReadOnlyMemory<byte> _memory;
        private int _position;
        private bool _isOpen;
        private CachedCompletedInt32Task _lastReadTask; // The last successful task returned from ReadAsync

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemoryStream"/> class over the specified <see cref="ReadOnlyMemory{Byte}"/>.
        /// </summary>
        /// <param name="source">The <see cref="ReadOnlyMemory{Byte}"/> to wrap.</param>
        public ReadOnlyMemoryStream(ReadOnlyMemory<byte> source)
        {
            _memory = source;
            _isOpen = true;
        }

        /// <inheritdoc/>
        public override bool CanRead => _isOpen;

        /// <inheritdoc/>
        public override bool CanSeek => _isOpen;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length
        {
            get
            {
                EnsureNotClosed();
                return _memory.Length;
            }
        }

        /// <inheritdoc/>
        public override long Position
        {
            get
            {
                EnsureNotClosed();
                return _position;
            }
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                EnsureNotClosed();

                if (value > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Format(SR.ArgumentOutOfRange_StreamPosition, int.MaxValue));
                }

                _position = (int)value;
            }
        }

        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

        /// <inheritdoc/>
        public override int ReadByte()
        {
            EnsureNotClosed();

            ReadOnlySpan<byte> span = _memory.Span;
            int position = _position;

            if ((uint)position < (uint)span.Length)
            {
                _position++;
                return span[position];
            }

            return -1;
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);

            return Read(new Span<byte>(buffer, offset, count));
        }

        /// <inheritdoc/>
        public override int Read(Span<byte> buffer)
        {
            EnsureNotClosed();

            int remaining = _memory.Length - _position;
            if (remaining <= 0 || buffer.Length == 0)
            {
                return 0;
            }

            int bytesToRead = Math.Min(remaining, buffer.Length);
            _memory.Span.Slice(_position, bytesToRead).CopyTo(buffer);
            _position += bytesToRead;

            return bytesToRead;
        }

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            EnsureNotClosed();

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            int n = Read(buffer, offset, count);
            return _lastReadTask.GetTask(n);
        }

        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            EnsureNotClosed();

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            return new ValueTask<int>(Read(buffer.Span));
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            EnsureNotClosed();

            int loc = origin switch
            {
                SeekOrigin.Begin => 0,
                SeekOrigin.Current => _position,
                SeekOrigin.End => _memory.Length,
                _ => throw new ArgumentException(SR.Argument_InvalidSeekOrigin)
            };

            if (offset > int.MaxValue - loc)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.ArgumentOutOfRange_StreamPosition, int.MaxValue));
            }

            int tempPosition = unchecked(loc + (int)offset);
            if (unchecked(loc + offset) < 0 || tempPosition < 0)
            {
                throw new IOException(SR.IO_SeekBeforeBegin);
            }

            _position = tempPosition;
            return _position;
        }

        /// <inheritdoc/>
        public override void CopyTo(Stream destination, int bufferSize)
        {
            ValidateCopyToArguments(destination, bufferSize);
            EnsureNotClosed();

            if (_memory.Length > _position)
            {
                destination.Write(_memory.Span.Slice(_position));
                _position = _memory.Length;
            }
        }

        /// <inheritdoc/>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ValidateCopyToArguments(destination, bufferSize);
            EnsureNotClosed();

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            if (_memory.Length > _position)
            {
                ReadOnlyMemory<byte> content = _memory.Slice(_position);
                _position = _memory.Length;

                return destination.WriteAsync(content, cancellationToken).AsTask();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            _isOpen = false;
            _memory = default;
            _lastReadTask = default;
            base.Dispose(disposing);
        }

        private void EnsureNotClosed()
        {
            if (!_isOpen)
                ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
        }
    }
}
