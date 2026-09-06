// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Buffers
{
    /// <summary>
    /// Provides a read-only, non-seekable <see cref="Stream"/> for reading from a <see cref="ReadOnlySequence{Byte}"/>.
    /// </summary>
    /// <remarks>
    /// <para>The underlying sequence is not copied; reads are served directly from its segments.</para>
    /// <para>The stream cannot be written to. <see cref="CanWrite"/> always returns <see langword="false"/>.</para>
    /// </remarks>
    public sealed class ReadOnlySequenceStream : Stream
    {
        private ReadOnlySequence<byte> _sequence;
        private SequencePosition _position;
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
        }

        /// <inheritdoc />
        public override bool CanRead => !_isDisposed;

        /// <inheritdoc />
        /// <summary>Gets a value indicating whether the <see cref="ReadOnlySequenceStream"/> supports seeking.</summary>
        // Keep this intentionally non-seekable: backward positioning requires traversing segments
        // again from the beginning, making repeated seeks worst-case O(N). ReadOnlySequence<T>
        // segment boundaries may be indirectly controlled by an untrusted network client through
        // packet framing, so even correct stitching logic can produce adversarial fragmentation.
        // Consumers must remain resilient against the worst technically compliant implementation
        // rather than assuming ASP.NET-like segmentation.
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        private void EnsureNotDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);

        /// <inheritdoc />
        /// <summary>Gets the length of the stream. This property is not supported and always throws a <see cref="NotSupportedException"/>.</summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        // Keep Length and Position unsupported to match the standard contract encoded by the
        // stream conformance tests for streams where CanSeek is false, even though the underlying
        // sequence can provide its length cheaply.
        public override long Length => throw new NotSupportedException(SR.NotSupported_UnseekableStream);

        /// <inheritdoc />
        /// <summary>Gets or sets the position within the current stream. This property is not supported and always throws a <see cref="NotSupportedException"/>.</summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override long Position
        {
            get => throw new NotSupportedException(SR.NotSupported_UnseekableStream);
            set => throw new NotSupportedException(SR.NotSupported_UnseekableStream);
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

            ReadOnlySequence<byte> remaining = _sequence.Slice(_position);
            if (remaining.IsEmpty)
            {
                return;
            }

            foreach (ReadOnlyMemory<byte> segment in remaining)
            {
                destination.Write(segment.Span);
            }

            _position = _sequence.End;
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

            ReadOnlySequence<byte> remaining = _sequence.Slice(_position);
            if (remaining.IsEmpty)
            {
                return Task.CompletedTask;
            }

            return CopyToAsyncCore(remaining, destination, cancellationToken);
        }

        private async Task CopyToAsyncCore(ReadOnlySequence<byte> remaining, Stream destination, CancellationToken cancellationToken)
        {
            foreach (ReadOnlyMemory<byte> segment in remaining)
            {
                await destination.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
            }

            _position = _sequence.End;
        }

        /// <inheritdoc />
        /// <summary>Writes a sequence of bytes to the stream. This method is not supported and always throws a <see cref="NotSupportedException"/>.</summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        /// <summary>Writes a sequence of bytes to the stream. This method is not supported and always throws a <see cref="NotSupportedException"/>.</summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        /// <summary>Asynchronously writes a sequence of bytes to the stream. This method is not supported and always throws a <see cref="NotSupportedException"/>.</summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        /// <summary>Asynchronously writes a sequence of bytes to the stream. This method is not supported and always throws a <see cref="NotSupportedException"/>.</summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        /// <summary>Sets the current position of the stream. This method is not supported and always throws a <see cref="NotSupportedException"/>.</summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException(SR.NotSupported_UnseekableStream);

        /// <inheritdoc />
        public override void Flush() { }

        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

        /// <inheritdoc />
        /// <summary>Sets the length of the stream. This method is not supported and always throws a <see cref="NotSupportedException"/>.</summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
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
