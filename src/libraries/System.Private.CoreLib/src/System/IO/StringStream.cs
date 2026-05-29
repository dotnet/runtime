// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

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
    private readonly Encoder _encoder;
    private readonly Encoding _encoding;
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
        _encoder = encoding.GetEncoder();
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
        _encoder = encoding.GetEncoder();
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
    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (buffer.Length == 0 || (_charPosition >= _text.Length && _pendingCount == 0 && _encoderFlushed))
        {
            return 0;
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
            if (buffer.Length < _encoding.GetMaxByteCount(1))
            {
                _pendingBytes ??= new byte[_encoding.GetMaxByteCount(2)];
                int charsToEncode = Math.Min(2, remaining.Length);
                _encoder.Convert(remaining.Slice(0, charsToEncode), _pendingBytes, flush: false, out int charsUsed, out int bytesUsed, out _);
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
                _encoder.Convert(remaining, buffer, flush: false, out int charsUsed, out int bytesUsed, out _);
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
            _pendingBytes ??= new byte[_encoding.GetMaxByteCount(2)];
            _encoder.Convert(ReadOnlySpan<char>.Empty, _pendingBytes, flush: true, out _, out int flushBytes, out _);
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

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<int>(cancellationToken);
        }

        return Task.FromResult(Read(buffer, offset, count));
    }

    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<int>(cancellationToken);
        }

        return new ValueTask<int>(Read(buffer.Span));
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
    protected override void Dispose(bool disposing)
    {
        _disposed = true;
        base.Dispose(disposing);
    }
}
