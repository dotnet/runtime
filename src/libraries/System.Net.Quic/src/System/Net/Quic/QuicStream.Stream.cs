// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic;

// Boilerplate implementation of Stream methods.
public partial class QuicStream : Stream
{
    // Seek and length.
    /// <inheritdoc />
    /// <summary>Gets a value indicating whether the <see cref="QuicStream" /> supports seeking.</summary>
    public override bool CanSeek => false;

    /// <inheritdoc />
    /// <summary>Gets the length of the data available on the stream. This property is not currently supported and always throws a <see cref="NotSupportedException" />.</summary>
    /// <exception cref="NotSupportedException">In all cases.</exception>
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    /// <summary>Gets or sets the position within the current stream. This property is not currently supported and always throws a <see cref="NotSupportedException" />.</summary>
    /// <exception cref="NotSupportedException">In all cases.</exception>
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    /// <inheritdoc />
    /// <summary>Sets the current position of the stream to the given value. This method is not currently supported and always throws a <see cref="NotSupportedException" />.</summary>
    /// <exception cref="NotSupportedException">In all cases.</exception>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    /// <summary>Sets the length of the stream. This method is not currently supported and always throws a <see cref="NotSupportedException" />.</summary>
    /// <exception cref="NotSupportedException">In all cases.</exception>
    public override void SetLength(long value) => throw new NotSupportedException();

    // Read and Write timeouts.
    /// <inheritdoc />
    /// <summary>Gets a value that indicates whether the <see cref="QuicStream" /> can timeout.</summary>
    public override bool CanTimeout => true;

    private TimeSpan _readTimeout = Timeout.InfiniteTimeSpan;
    private TimeSpan _writeTimeout = Timeout.InfiniteTimeSpan;

    /// <inheritdoc />
    public override int ReadTimeout
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return (int)_readTimeout.TotalMilliseconds;
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (value <= 0 && value != Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException(nameof(value), SR.net_quic_timeout_use_gt_zero);
            }
            _readTimeout = TimeSpan.FromMilliseconds(value);
        }
    }

    /// <inheritdoc />
    public override int WriteTimeout
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return (int)_writeTimeout.TotalMilliseconds;
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (value <= 0 && value != Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException(nameof(value), SR.net_quic_timeout_use_gt_zero);
            }
            _writeTimeout = TimeSpan.FromMilliseconds(value);
        }
    }

    // Read boilerplate.
    /// <inheritdoc />
    /// <summary>Gets a value indicating whether the <see cref="QuicStream" /> supports reading.</summary>
    public override bool CanRead => !Volatile.Read(ref _disposed) && _canRead;

    /// <inheritdoc />
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count, default), callback, state);

    /// <inheritdoc />
    public override int EndRead(IAsyncResult asyncResult)
        => TaskToAsyncResult.End<int>(asyncResult);

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        return Read(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc />
    public override int ReadByte()
    {
        byte b = 0;
        return Read(new Span<byte>(ref b)) != 0 ? b : -1;
    }

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
        CancellationTokenSource? cts = null;
        try
        {
            if (_readTimeout > TimeSpan.Zero)
            {
                cts = new CancellationTokenSource(_readTimeout);
            }
            int readLength = ReadAsync(new Memory<byte>(rentedBuffer, 0, buffer.Length), cts?.Token ?? default).AsTask().GetAwaiter().GetResult();
            rentedBuffer.AsSpan(0, readLength).CopyTo(buffer);
            return readLength;
        }
        catch (OperationCanceledException) when (cts?.IsCancellationRequested == true)
        {
            // sync operations do not have Cancellation
            throw new IOException(SR.net_quic_timeout);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
            cts?.Dispose();
        }
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ValidateBufferArguments(buffer, offset, count);
        return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
    }

    // Write boilerplate.
    /// <inheritdoc />
    /// <summary>Gets a value indicating whether the <see cref="QuicStream" /> supports writing.</summary>
    public override bool CanWrite => !Volatile.Read(ref _disposed) && _canWrite;

    /// <inheritdoc />
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count, default), callback, state);

    /// <inheritdoc />
    public override void EndWrite(IAsyncResult asyncResult)
        => TaskToAsyncResult.End(asyncResult);

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        Write(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc />
    public override void WriteByte(byte value)
    {
        Write(new ReadOnlySpan<byte>(in value));
    }

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        CancellationTokenSource? cts = null;
        if (_writeTimeout > TimeSpan.Zero)
        {
            cts = new CancellationTokenSource(_writeTimeout);
        }
        try
        {
            WriteAsync(buffer.ToArray(), cts?.Token ?? default).AsTask().GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (cts?.IsCancellationRequested == true)
        {
            // sync operations do not have Cancellation
            throw new IOException(SR.net_quic_timeout);
        }
        finally
        {
            cts?.Dispose();
        }
    }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ValidateBufferArguments(buffer, offset, count);
        return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
    }

    // Flush.

    /// <inheritdoc />
    public override void Flush()
        => FlushAsync().GetAwaiter().GetResult();

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken = default)
        // NOP for now
        => Task.CompletedTask;

    // Dispose.
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.Dispose(disposing);
    }
}
