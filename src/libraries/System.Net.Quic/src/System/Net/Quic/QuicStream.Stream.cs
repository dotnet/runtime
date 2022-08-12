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
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    // Read and Write timeouts.
    public override bool CanTimeout => true;
    private TimeSpan _readTimeout = Timeout.InfiniteTimeSpan;
    private TimeSpan _writeTimeout = Timeout.InfiniteTimeSpan;
    public override int ReadTimeout
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed == 1, this);
            return (int)_readTimeout.TotalMilliseconds;
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed == 1, this);
            if (value <= 0 && value != Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException(nameof(value), SR.net_quic_timeout_use_gt_zero);
            }
            _readTimeout = TimeSpan.FromMilliseconds(value);
        }
    }
    public override int WriteTimeout
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed == 1, this);
            return (int)_writeTimeout.TotalMilliseconds;
        }
        set
        {
            ObjectDisposedException.ThrowIf(_disposed == 1, this);
            if (value <= 0 && value != Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException(nameof(value), SR.net_quic_timeout_use_gt_zero);
            }
            _writeTimeout = TimeSpan.FromMilliseconds(value);
        }
    }

    // Read boilerplate.
    public override bool CanRead => Volatile.Read(ref _disposed) == 0 && _canRead;
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => TaskToApm.Begin(ReadAsync(buffer, offset, count, default), callback, state);
    public override int EndRead(IAsyncResult asyncResult)
        => TaskToApm.End<int>(asyncResult);
    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        return Read(buffer.AsSpan(offset, count));
    }
    public override int ReadByte()
    {
        byte b = 0;
        return Read(MemoryMarshal.CreateSpan(ref b, 1)) != 0 ? b : -1;
    }
    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

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
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ValidateBufferArguments(buffer, offset, count);
        return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
    }

    // Write boilerplate.
    public override bool CanWrite => Volatile.Read(ref _disposed) == 0 && _canWrite;
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => TaskToApm.Begin(WriteAsync(buffer, offset, count, default), callback, state);
    public override void EndWrite(IAsyncResult asyncResult)
        => TaskToApm.End(asyncResult);
    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        Write(buffer.AsSpan(offset, count));
    }
    public override void WriteByte(byte value)
    {
        Write(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
    }
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

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
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        ValidateBufferArguments(buffer, offset, count);
        return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
    }

    // Flush.
    public override void Flush()
        => FlushAsync().GetAwaiter().GetResult();
    public override Task FlushAsync(CancellationToken cancellationToken = default)
        // NOP for now
        => Task.CompletedTask;

    // Dispose.
    protected override void Dispose(bool disposing)
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.Dispose(disposing);
    }
}
