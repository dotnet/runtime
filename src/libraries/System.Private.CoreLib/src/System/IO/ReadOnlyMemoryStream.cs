// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>
    /// Provides a seekable, read-only <see cref="MemoryStream"/> over a <see cref="ReadOnlyMemory{Byte}"/>.
    /// </summary>
    /// <remarks>
    /// <para>The stream cannot be written to. <see cref="MemoryStream.CanWrite"/> always returns <see langword="false"/>.</para>
    /// <para><see cref="MemoryStream.GetBuffer"/> throws and <see cref="MemoryStream.TryGetBuffer"/> returns <see langword="false"/>.</para>
    /// </remarks>
    public sealed class ReadOnlyMemoryStream : MemoryStream
    {
        private ReadOnlyMemory<byte> _memory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemoryStream"/> class over the specified <see cref="ReadOnlyMemory{Byte}"/>.
        /// </summary>
        /// <param name="source">The <see cref="ReadOnlyMemory{Byte}"/> to wrap.</param>
        public ReadOnlyMemoryStream(ReadOnlyMemory<byte> source) : base()
        {
            _writable = false;
            _memory = source;
            _length = source.Length;
        }

        /// <inheritdoc/>
        public override int Capacity
        {
            get
            {
                EnsureNotClosed();
                return _memory.Length;
            }
            set => throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);
        }

        /// <inheritdoc/>
        public override byte[] GetBuffer() =>
            throw new UnauthorizedAccessException(SR.UnauthorizedAccess_MemStreamBuffer);

        /// <inheritdoc/>
        public override bool TryGetBuffer(out ArraySegment<byte> buffer)
        {
            buffer = default;
            return false;
        }

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
        public override byte[] ToArray()
        {
            EnsureNotClosed();
            if (_memory.Length == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] copy = GC.AllocateUninitializedArray<byte>(_memory.Length);
            _memory.Span.CopyTo(copy);
            return copy;
        }

        /// <inheritdoc/>
        public override void WriteTo(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            EnsureNotClosed();

            stream.Write(_memory.Span);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            _memory = default;
            base.Dispose(disposing);
        }

        private void EnsureNotClosed()
        {
            if (!_isOpen)
                ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
        }
    }
}
