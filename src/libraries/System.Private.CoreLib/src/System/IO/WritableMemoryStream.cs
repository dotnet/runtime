// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

/// <summary>
/// Provides a seekable, writable <see cref="MemoryStream"/> over a <see cref="Memory{Byte}"/> with fixed capacity.
/// </summary>
/// <remarks>
/// <para>The stream cannot expand beyond the initial memory capacity.</para>
/// <para>This type is not thread-safe. Synchronize access if the stream is used concurrently.</para>
/// <para><see cref="GetBuffer"/> throws and <see cref="TryGetBuffer"/> returns <see langword="false"/>.</para>
/// </remarks>
public sealed class WritableMemoryStream : MemoryStream
{
    private Memory<byte> _buffer;
    private int _position;
    private int _length;
    private bool _isOpen;

    /// <summary>
    /// Initializes a new instance of the <see cref="WritableMemoryStream"/> class over the specified <see cref="Memory{Byte}"/>.
    /// </summary>
    /// <param name="buffer">The <see cref="Memory{Byte}"/> to wrap.</param>
    public WritableMemoryStream(Memory<byte> buffer) : base()
    {
        _buffer = buffer;
        _length = 0;
        _isOpen = true;
    }

    /// <inheritdoc/>
    public override bool CanRead => _isOpen;

    /// <inheritdoc/>
    public override bool CanSeek => _isOpen;

    /// <inheritdoc/>
    public override bool CanWrite => _isOpen;

    /// <inheritdoc/>
    public override int Capacity
    {
        get
        {
            EnsureNotClosed();
            return _buffer.Length;
        }
        set => throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);
    }

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
            EnsureNotClosed();
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, int.MaxValue);
            _position = (int)value;
        }
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        EnsureNotClosed();

        ReadOnlySpan<byte> span = _buffer.Span;
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
        ((ReadOnlyMemory<byte>)_buffer).Span.Slice(_position, bytesToRead).CopyTo(buffer);
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

        return Task.FromResult(Read(buffer, offset, count));
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

        if (_length > _position)
        {
            destination.Write(((ReadOnlyMemory<byte>)_buffer).Span.Slice(_position, _length - _position));
            _position = _length;
        }
    }

    /// <inheritdoc/>
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        ValidateCopyToArguments(destination, bufferSize);
        EnsureNotClosed();

        if (_length > _position)
        {
            ReadOnlyMemory<byte> content = ((ReadOnlyMemory<byte>)_buffer).Slice(_position, _length - _position);
            _position = _length;

            return destination.WriteAsync(content, cancellationToken).AsTask();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override void WriteByte(byte value)
    {
        EnsureNotClosed();

        if (_position >= _buffer.Length)
        {
            throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);
        }

        _buffer.Span[_position++] = value;

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

        if (_position > _buffer.Length - buffer.Length)
        {
            throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);
        }

        buffer.CopyTo(_buffer.Span.Slice(_position));
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

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        try
        {
            Write(buffer, offset, count);

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    /// <inheritdoc/>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        try
        {
            Write(buffer.Span);

            return default;
        }
        catch (Exception exception)
        {
            return ValueTask.FromException(exception);
        }
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        EnsureNotClosed();

        long newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _buffer.Length + offset,
            _ => throw new ArgumentException(SR.Argument_InvalidSeekOrigin)
        };

        if (newPosition < 0)
        {
            throw new IOException(SR.IO_SeekBeforeBegin);
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(newPosition, int.MaxValue, nameof(offset));

        _position = (int)newPosition;

        return newPosition;
    }

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);

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
    public override byte[] ToArray()
    {
        EnsureNotClosed();
        if (_length == 0)
        {
            return Array.Empty<byte>();
        }

        byte[] copy = GC.AllocateUninitializedArray<byte>(_length);
        ((ReadOnlyMemory<byte>)_buffer).Span.Slice(0, _length).CopyTo(copy);
        return copy;
    }

    /// <inheritdoc/>
    public override void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        EnsureNotClosed();

        if (_length > 0)
        {
            stream.Write(((ReadOnlyMemory<byte>)_buffer).Span.Slice(0, _length));
        }
    }

    /// <inheritdoc/>
    public override void Flush() { }

    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        _isOpen = false;
        _buffer = default;
        base.Dispose(disposing);
    }

    private void EnsureNotClosed()
    {
        ObjectDisposedException.ThrowIf(!_isOpen, this);
    }
}
