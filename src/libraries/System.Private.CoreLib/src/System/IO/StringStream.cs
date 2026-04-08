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

        if (buffer.Length == 0 || _charPosition >= _text.Length)
        {
            return 0;
        }

        ReadOnlySpan<char> remaining = _text.Span.Slice(_charPosition);
        bool flush = true;

        _encoder.Convert(remaining, buffer, flush, out int charsUsed, out int bytesUsed, out _);
        _charPosition += charsUsed;

        return bytesUsed;
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        Span<byte> oneByte = stackalloc byte[1];
        int bytesRead = Read(oneByte);

        return bytesRead > 0 ? oneByte[0] : -1;
    }

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<int>(cancellationToken);

        return Task.FromResult(Read(buffer, offset, count));
    }

    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<int>(cancellationToken);

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
