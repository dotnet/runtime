// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Buffers
{

    public sealed class ReadOnlySequenceStream : Stream
    {
        private ReadOnlySequence<byte> _sequence;
        private SequencePosition _position;
        private long _absolutePosition;
        private bool _isDisposed;
        private CachedCompletedInt32Task _lastReadTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlySequenceStream"/> class over the specified <see cref="ReadOnlySequence{Byte}"/>.
        /// </summary>
        /// <param name="source">The <see cref="ReadOnlySequence{Byte}"/> to wrap.</param>
        public ReadOnlySequenceStream(ReadOnlySequence<byte> source)
        {
            _sequence = source;
            _position = source.Start;
            _absolutePosition = 0;
            _isDisposed = false;
        }

        /// <inheritdoc />
        public override bool CanRead => !_isDisposed;

        /// <inheritdoc />
        public override bool CanSeek => !_isDisposed;

        /// <inheritdoc />
        public override bool CanWrite => false;

        private void EnsureNotDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);

        /// <inheritdoc />
        public override long Length
        {
            get
            {
                EnsureNotDisposed();
                return _sequence.Length;
            }
        }

        /// <inheritdoc />
        public override long Position
        {
            get
            {
                EnsureNotDisposed();
                return _absolutePosition;
            }
            set
            {
                EnsureNotDisposed();
                ArgumentOutOfRangeException.ThrowIfNegative(value);

                if (value >= _sequence.Length)
                {
                    _position = _sequence.End;
                }
                else if (value >= _absolutePosition)
                {
                    _position = _sequence.GetPosition(value - _absolutePosition, _position);
                }
                else
                {
                    _position = _sequence.GetPosition(value, _sequence.Start);
                }

                _absolutePosition = value;
            }
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return Read(buffer.AsSpan(offset, count));
        }

        /// <inheritdoc />
        public override int Read(Span<byte> buffer)
        {
            EnsureNotDisposed();

            if (_absolutePosition >= _sequence.Length)
            {
                return 0;
            }

            ReadOnlySequence<byte> remaining = _sequence.Slice(_position);
            int n = (int)Math.Min(remaining.Length, buffer.Length);
            if (n <= 0)
            {
                return 0;
            }

            remaining.Slice(0, n).CopyTo(buffer);
            _position = _sequence.GetPosition(n, _position);
            _absolutePosition += n;
            return n;
        }

        /// <inheritdoc />
        public override int ReadByte()
        {
            EnsureNotDisposed();

            byte b = 0;
            return Read(new Span<byte>(ref b)) > 0 ? b : -1;
        }

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            EnsureNotDisposed();

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
            EnsureNotDisposed();

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            int n = Read(buffer.Span);
            return new ValueTask<int>(n);
        }

        /// <inheritdoc />
        public override void CopyTo(Stream destination, int bufferSize)
        {
            ValidateCopyToArguments(destination, bufferSize);
            EnsureNotDisposed();

            if (_absolutePosition >= _sequence.Length)
            {
                return;
            }

            ReadOnlySequence<byte> remaining = _sequence.Slice(_position);
            foreach (ReadOnlyMemory<byte> segment in remaining)
            {
                destination.Write(segment.Span);
            }

            _position = _sequence.End;
            _absolutePosition = _sequence.Length;
        }

        /// <inheritdoc />
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ValidateCopyToArguments(destination, bufferSize);
            EnsureNotDisposed();

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            if (_absolutePosition >= _sequence.Length)
            {
                return Task.CompletedTask;
            }

            return CopyToAsyncCore(destination, cancellationToken);
        }

        private async Task CopyToAsyncCore(Stream destination, CancellationToken cancellationToken)
        {
            ReadOnlySequence<byte> remaining = _sequence.Slice(_position);
            foreach (ReadOnlyMemory<byte> segment in remaining)
            {
                await destination.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
            }

            _position = _sequence.End;
            _absolutePosition = _sequence.Length;
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
        /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            EnsureNotDisposed();

            long absolutePosition = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _absolutePosition + offset,
                SeekOrigin.End => _sequence.Length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

            if (absolutePosition < 0)
            {
                throw new IOException(SR.IO_SeekBeforeBegin);
            }

            if (absolutePosition >= _sequence.Length)
            {
                _position = _sequence.End;
            }
            else if (absolutePosition >= _absolutePosition)
            {
                _position = _sequence.GetPosition(absolutePosition - _absolutePosition, _position);
            }
            else
            {
                _position = _sequence.GetPosition(absolutePosition, _sequence.Start);
            }

            _absolutePosition = absolutePosition;
            return absolutePosition;
        }

        /// <inheritdoc />
        public override void Flush() { }

        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            _isDisposed = true;
            _sequence = default;
            base.Dispose(disposing);
        }
    }
}
