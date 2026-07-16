// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>
    /// Provides a seekable, writable <see cref="Stream"/> over a <see cref="Memory{Byte}"/>.
    /// </summary>
    public sealed class WritableMemoryStream : Stream
    {
        private Memory<byte> _memory;
        private int _position;
        private int _length;
        private bool _isOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="WritableMemoryStream"/> class over the specified <see cref="Memory{Byte}"/>.
        /// </summary>
        /// <param name="buffer">The <see cref="Memory{Byte}"/> to wrap.</param>
        public WritableMemoryStream(Memory<byte> buffer)
        {
            _memory = buffer;
            _length = buffer.Length;
            _isOpen = true;
        }

        /// <inheritdoc/>
        public override bool CanRead => _isOpen;

        /// <inheritdoc/>
        public override bool CanSeek => _isOpen;

        /// <inheritdoc/>
        public override bool CanWrite => _isOpen;

        /// <inheritdoc/>
        public override long Length
        {
            get
            {
                EnsureNotClosed();
                return _length;
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

            if ((uint)position < (uint)_length)
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

            int remaining = _length - _position;
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
            return Task.FromResult(n);
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
                SeekOrigin.End => _length,
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

            if (_length > _position)
            {
                destination.Write(_memory.Span.Slice(_position, _length - _position));
                _position = _length;
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

            if (_length > _position)
            {
                ReadOnlyMemory<byte> content = _memory.Slice(_position, _length - _position);
                _position = _length;

                return destination.WriteAsync(content, cancellationToken).AsTask();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override void WriteByte(byte value)
        {
            EnsureNotClosed();
            EnsureCapacity(1);

            if (_position > _length)
            {
                _memory.Span.Slice(_length, _position - _length).Clear();
            }

            _memory.Span[_position++] = value;

            if (_position > _length)
            {
                _length = _position;
            }
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            Write(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            EnsureNotClosed();

            if (buffer.Length == 0)
            {
                return;
            }

            EnsureCapacity(buffer.Length);

            if (_position > _length)
            {
                _memory.Span.Slice(_length, _position - _length).Clear();
            }

            buffer.CopyTo(_memory.Span.Slice(_position));
            _position += buffer.Length;

            if (_position > _length)
            {
                _length = _position;
            }
        }

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            EnsureNotClosed();

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            Write(new ReadOnlySpan<byte>(buffer, offset, count));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            EnsureNotClosed();

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            Write(buffer.Span);
            return default;
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            EnsureNotClosed();

            if (value > _memory.Length)
            {
                throw new NotSupportedException(SR.IO_FixedCapacity);
            }

            int newLength = (int)value;
            if (newLength > _length)
            {
                // Zero out the newly exposed region so it reads back as zeros.
                _memory.Span.Slice(_length, newLength - _length).Clear();
            }

            _length = newLength;
            if (_position > newLength)
            {
                _position = newLength;
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            _isOpen = false;
            _memory = default;
            base.Dispose(disposing);
        }

        private void EnsureNotClosed()
        {
            if (!_isOpen)
                ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
        }

        private void EnsureCapacity(int count)
        {
            if (count != 0 && _position > _memory.Length - count)
            {
                throw new NotSupportedException(SR.IO_FixedCapacity);
            }
        }
    }
}
