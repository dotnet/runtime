// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Threading;
using System.IO;
using System.Threading.Tasks;

namespace System.Buffers
{
    /// <summary>
    /// Provides a seekable, read-only <see cref="Stream"/> implementation over a <see cref="ReadOnlySequence{T}"/> of bytes.
    /// </summary>
    /// <remarks>
    /// This type is not thread-safe. Synchronize access if the stream is used concurrently.
    /// The underlying sequence should not be modified while the stream is in use.
    /// Seeking beyond the end of the stream is supported; subsequent reads will return zero bytes.
    /// </remarks>
    // Seekable Stream from ReadOnlySequence<byte>
    public sealed class ReadOnlySequenceStream : Stream
    {
        private ReadOnlySequence<byte> _sequence;
        private SequencePosition _position;
        private long _positionPastEnd; // -1 if within bounds, or the actual position if past end
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlySequenceStream"/> class over the specified <see cref="ReadOnlySequence{Byte}"/>.
        /// </summary>
        /// <param name="sequence">The <see cref="ReadOnlySequence{Byte}"/> to wrap.</param>
        public ReadOnlySequenceStream(ReadOnlySequence<byte> sequence)
        {
            _sequence = sequence;
            _position = sequence.Start;
            _positionPastEnd = -1;
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
                return _positionPastEnd >= 0 ? _positionPastEnd : _sequence.Slice(_sequence.Start, _position).Length;
            }
            set
            {
                EnsureNotDisposed();
                ArgumentOutOfRangeException.ThrowIfNegative(value);

                // Allow seeking past the end
                if (value >= Length)
                {
                    _position = _sequence.End;
                    _positionPastEnd = value;
                }
                else
                {
                    _position = _sequence.GetPosition(value, _sequence.Start);
                    _positionPastEnd = -1;
                }
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

            if (_positionPastEnd >= 0)
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
            return n;
        }

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            // If cancellation was requested, bail early
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<int>(cancellationToken);

            int n = Read(buffer, offset, count);
            return Task.FromResult(n);
        }

        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            int bytesRead = Read(buffer.Span);
            return new ValueTask<int>(bytesRead);
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
                SeekOrigin.Current => (_positionPastEnd >= 0 ? _positionPastEnd : _sequence.Slice(_sequence.Start, _position).Length) + offset,
                SeekOrigin.End => Length + offset,
                _ => throw new ArgumentException(SR.Argument_InvalidSeekOrigin, nameof(origin))
            };

            // Negative positions are invalid
            if (absolutePosition < 0)
            {
                throw new IOException(SR.IO_SeekBeforeBegin);
            }

            // Update position - seeking past end is allowed
            if (absolutePosition >= Length)
            {
                _position = _sequence.End;
                _positionPastEnd = absolutePosition;
            }
            else
            {
                _position = _sequence.GetPosition(absolutePosition, _sequence.Start);
                _positionPastEnd = -1;
            }

            return absolutePosition;
        }

        /// <inheritdoc />
        public override void Flush() { }

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            _isDisposed = true;
            base.Dispose(disposing);
        }
    }
}
