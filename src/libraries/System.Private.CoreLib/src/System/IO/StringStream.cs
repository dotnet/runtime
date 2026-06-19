// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    /// <summary>
    /// Provides a read-only, non-seekable <see cref="Stream"/> that encodes a <see cref="string"/> or
    /// <see cref="ReadOnlyMemory{Char}"/> into bytes on-the-fly using a specified <see cref="System.Text.Encoding"/>.
    /// </summary>
    /// <remarks>
    /// <para>This stream never emits a byte order mark (BOM). Callers who need a BOM can prepend it themselves.</para>
    /// <para>This type is not thread-safe. Synchronize access if the stream is used concurrently.</para>
    /// </remarks>
    public sealed class StringStream : Stream
    {
        private readonly ReadOnlyMemory<char> _text;
        // Lazily created on the encoder slow path. The single-shot fast path in Read
        // uses stateless Encoding.GetBytes and never touches this field.
        private Encoder? _encoder;
        private readonly Encoding _encoding;
        private readonly int _maxBytesPerChar;
        private int _charPosition;
        private bool _disposed;
        private bool _encoderFlushed;

        // Spillover buffer for multibyte encodings: when the caller's buffer is too small
        // to hold even one encoded scalar (e.g., ReadByte with UTF-16), we encode into
        // this buffer and serve bytes from it across subsequent Read/ReadByte calls.
        // Also used to hold final encoder flush bytes when the caller's buffer had no room.
        private byte[]? _pendingBytes;
        private int _pendingOffset;
        private int _pendingCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringStream"/> class with the specified string and encoding.
        /// </summary>
        /// <param name="text">The string to read from.</param>
        /// <param name="encoding">The encoding to use when converting the string to bytes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="encoding"/> is <see langword="null"/>.</exception>
        public StringStream(string text, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(encoding);

            _text = text.AsMemory();
            _encoding = encoding;
            _maxBytesPerChar = encoding.GetMaxByteCount(1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringStream"/> class with the specified character memory and encoding.
        /// </summary>
        /// <param name="text">The character memory to read from.</param>
        /// <param name="encoding">The encoding to use when converting the characters to bytes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="encoding"/> is <see langword="null"/>.</exception>
        public StringStream(ReadOnlyMemory<char> text, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(encoding);

            _text = text;
            _encoding = encoding;
            _maxBytesPerChar = encoding.GetMaxByteCount(1);
        }

        /// <summary>
        /// Gets the encoding used by this stream.
        /// </summary>
        public Encoding Encoding => _encoding;

        /// <inheritdoc/>
        public override bool CanRead => !_disposed;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException(SR.NotSupported_UnseekableStream);

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException(SR.NotSupported_UnseekableStream);
            set => throw new NotSupportedException(SR.NotSupported_UnseekableStream);
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);

            return Read(new Span<byte>(buffer, offset, count));
        }

        /// <inheritdoc/>
        // All Read overloads funnel here; ObjectDisposedException guards every path (TranscodingStream pattern).
        public override int Read(Span<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (buffer.Length == 0 || (_charPosition >= _text.Length && _pendingCount == 0 && _encoderFlushed))
            {
                return 0;
            }

            // Fast path: nothing emitted yet and the caller's buffer is guaranteed
            // large enough to hold the entire encoded payload in a single shot.
            // Encoding.GetBytes is stateless and emits any reset/shift sequences
            // required by stateful encodings for a complete conversion, so we can
            // mark the encoder as flushed without ever allocating an Encoder.
            if (_charPosition == 0 && _pendingCount == 0 &&
                buffer.Length >= _encoding.GetMaxByteCount(_text.Length))
            {
                int written = _encoding.GetBytes(_text.Span, buffer);
                _charPosition = _text.Length;
                _encoderFlushed = true;
                return written;
            }

            int totalBytesWritten = 0;
            int bufferBytesWritten = 0;

            // Drain any pending bytes from a previous partial read.
            if (_pendingCount > 0)
            {
                int toCopy = Math.Min(_pendingCount, buffer.Length);
                _pendingBytes.AsSpan(_pendingOffset, toCopy).CopyTo(buffer);
                _pendingOffset += toCopy;
                _pendingCount -= toCopy;
                totalBytesWritten += toCopy;

                if (totalBytesWritten == buffer.Length)
                {
                    return totalBytesWritten;
                }

                buffer = buffer.Slice(totalBytesWritten);
            }

            if (_charPosition < _text.Length)
            {
                ReadOnlySpan<char> remaining = _text.Span.Slice(_charPosition);

                // If the caller's buffer may be too small for even one encoded scalar,
                // encode into the spillover buffer first, then copy what fits.
                // Encoder.Convert throws ArgumentException when the output buffer
                // cannot hold a single complete encoded character.
                if (buffer.Length < _maxBytesPerChar)
                {
                    // Instance field — ArrayPool not appropriate. Contents are
                    // always overwritten by Encoder.Convert before being read out,
                    // so we can skip the JIT-emitted zero-init (mirrors TranscodingStream).
                    _pendingBytes ??= GC.AllocateUninitializedArray<byte>(_encoding.GetMaxByteCount(2));
                    int charsToEncode = Math.Min(2, remaining.Length);
                    GetEncoder().Convert(remaining.Slice(0, charsToEncode), _pendingBytes, flush: false, out int charsUsed, out int bytesUsed, out _);
                    _charPosition += charsUsed;

                    int toCopy = Math.Min(bytesUsed, buffer.Length);
                    _pendingBytes.AsSpan(0, toCopy).CopyTo(buffer);
                    totalBytesWritten += toCopy;
                    bufferBytesWritten += toCopy;

                    _pendingOffset = toCopy;
                    _pendingCount = bytesUsed - toCopy;
                }
                else
                {
                    // Encode directly into the caller's buffer.
                    // Only flush on the final block to preserve encoder state
                    // for stateful encodings.
                    GetEncoder().Convert(remaining, buffer, flush: false, out int charsUsed, out int bytesUsed, out _);
                    _charPosition += charsUsed;
                    totalBytesWritten += bytesUsed;
                    bufferBytesWritten += bytesUsed;
                }
            }

            // If all input chars are consumed but the encoder hasn't been flushed,
            // flush any remaining encoder state (e.g., stateful encoding reset sequences).
            // Always flush into _pendingBytes (which is guaranteed large enough) to
            // avoid ArgumentException if the caller's remaining buffer is too small.
            if (_charPosition >= _text.Length && !_encoderFlushed && _pendingCount == 0)
            {
                _pendingBytes ??= GC.AllocateUninitializedArray<byte>(_encoding.GetMaxByteCount(2));
                GetEncoder().Convert(ReadOnlySpan<char>.Empty, _pendingBytes, flush: true, out _, out int flushBytes, out _);
                _encoderFlushed = true;

                if (flushBytes > 0)
                {
                    Span<byte> flushTarget = buffer.Slice(bufferBytesWritten);
                    int toCopy = Math.Min(flushBytes, flushTarget.Length);
                    if (toCopy > 0)
                    {
                        _pendingBytes.AsSpan(0, toCopy).CopyTo(flushTarget);
                        totalBytesWritten += toCopy;
                    }

                    if (toCopy < flushBytes)
                    {
                        _pendingOffset = toCopy;
                        _pendingCount = flushBytes - toCopy;
                    }
                }
            }

            return totalBytesWritten;
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            byte b = 0;
            return Read(new Span<byte>(ref b)) > 0 ? b : -1;
        }

        private Encoder GetEncoder() => _encoder ??= _encoding.GetEncoder();

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            try
            {
                return Task.FromResult(Read(buffer, offset, count));
            }
            catch (OperationCanceledException oce)
            {
                return Task.FromCanceled<int>(oce.CancellationToken);
            }
            catch (Exception ex)
            {
                return Task.FromException<int>(ex);
            }
        }

        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            try
            {
                return new ValueTask<int>(Read(buffer.Span));
            }
            catch (OperationCanceledException oce)
            {
                return ValueTask.FromCanceled<int>(oce.CancellationToken);
            }
            catch (Exception ex)
            {
                return ValueTask.FromException<int>(ex);
            }
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException(SR.NotSupported_UnseekableStream);

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

        /// <inheritdoc/>
        public override void Flush() { }

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

        /// <inheritdoc/>
        public override void CopyTo(Stream destination, int bufferSize)
        {
            ValidateCopyToArguments(destination, bufferSize);
            ObjectDisposedException.ThrowIf(_disposed, this);

            // Size the rented buffer to the remaining encoded payload (capped by bufferSize).
            // The base Stream.CopyTo falls back to an 80 KB buffer because our Length throws;
            // sizing here avoids that waste and lets the single-shot Read fast path consume
            // the entire input in one call when the rented buffer is large enough.
            int maxBytes = _encoding.GetMaxByteCount(_text.Length - _charPosition);
            int rentSize = Math.Max(1, Math.Min(maxBytes, bufferSize));
            byte[] buffer = ArrayPool<byte>.Shared.Rent(rentSize);
            try
            {
                int n;
                while ((n = Read(buffer, 0, buffer.Length)) != 0)
                {
                    destination.Write(buffer, 0, n);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <inheritdoc/>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ValidateCopyToArguments(destination, bufferSize);
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            int maxBytes = _encoding.GetMaxByteCount(_text.Length - _charPosition);
            int rentSize = Math.Max(1, Math.Min(maxBytes, bufferSize));
            return CopyToAsyncCore(destination, rentSize, cancellationToken);
        }

        private async Task CopyToAsyncCore(Stream destination, int rentSize, CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(rentSize);
            try
            {
                int n;
                // Read is synchronous and CPU-bound; no underlying IO to await.
                while ((n = Read(buffer, 0, buffer.Length)) != 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, n), cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
