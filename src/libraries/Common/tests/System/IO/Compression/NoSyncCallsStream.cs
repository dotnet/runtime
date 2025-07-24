// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression.Tests;

// A stream meant to be used for testing that an implementation's async methods do not accidentally call any sync methods.
internal sealed class NoSyncCallsStream : Stream
{
    private readonly Stream _s;

    public NoSyncCallsStream(Stream stream) => _s = stream;

    // Allows temporarily disabling the current stream's sync API usage restriction.
    public bool IsRestrictionEnabled { get; set; }

    public override bool CanRead => _s.CanRead;
    public override bool CanSeek => _s.CanSeek;
    public override bool CanTimeout => _s.CanTimeout;
    public override bool CanWrite => _s.CanWrite;
    public override long Length => _s.Length;
    public override long Position { get => _s.Position; set => _s.Position = value; }
    public override int ReadTimeout { get => _s.ReadTimeout; set => _s.ReadTimeout = value; }
    public override int WriteTimeout { get => _s.WriteTimeout; set => _s.WriteTimeout = value; }
    public override void Close() => _s.Close();
    public override bool Equals(object? obj) => _s.Equals(obj);
    public override int GetHashCode() => _s.GetHashCode();
    public override int ReadByte() => _s.ReadByte();
    public override long Seek(long offset, SeekOrigin origin) => _s.Seek(offset, origin);
    public override void SetLength(long value) => _s.SetLength(value);
    public override string? ToString() => _s.ToString();

    // Sync
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
        IsRestrictionEnabled ? throw new InvalidOperationException() : _s.BeginRead(buffer, offset, count, callback, state);
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
        IsRestrictionEnabled ? throw new InvalidOperationException() : _s.BeginRead(buffer, offset, count, callback, state);
    public override void CopyTo(Stream destination, int bufferSize)
    {
        if (IsRestrictionEnabled)
        {
            throw new InvalidOperationException();
        }
        else
        {
            _s.CopyTo(destination, bufferSize);
        }
    }
    protected override void Dispose(bool disposing)
    {
        // _disposing = true;
        if (IsRestrictionEnabled)
        {
            throw new InvalidOperationException();
        }
        else
        {
            _s.Dispose();
        }
    }
    public override int EndRead(IAsyncResult asyncResult) => IsRestrictionEnabled ? throw new InvalidOperationException() : _s.EndRead(asyncResult);
    public override void EndWrite(IAsyncResult asyncResult)
    {
        if (IsRestrictionEnabled)
        {
            throw new InvalidOperationException();
        }
        else
        {
            _s.EndWrite(asyncResult);
        }
    }
    public override void Flush()
    {
        if (IsRestrictionEnabled)
        {
            throw new InvalidOperationException();
        }
        else
        {
            _s.Flush();
        }
    }
    public override int Read(byte[] buffer, int offset, int count) =>
        IsRestrictionEnabled ? throw new InvalidOperationException() : _s.Read(buffer, offset, count);
    public override int Read(Span<byte> buffer) =>
        IsRestrictionEnabled ? throw new InvalidOperationException() : _s.Read(buffer);
    public override void Write(byte[] buffer, int offset, int count)
    {
        bool isDeflateStream = false;

        // Get the stack trace to determine the calling method
        var stackTrace = new System.Diagnostics.StackTrace();
        var callingMethod = stackTrace.GetFrame(1)?.GetMethod();

        // Check if the calling method belongs to the DeflateStream class
        if (callingMethod?.DeclaringType == typeof(System.IO.Compression.DeflateStream))
        {
            isDeflateStream = true;
        }

        if (!isDeflateStream && IsRestrictionEnabled)
        {
            throw new InvalidOperationException($"Parent class is {callingMethod.DeclaringType}");
        }
        else
        {
            _s.Write(buffer, offset, count);
        }
    }
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (IsRestrictionEnabled)
        {
            throw new InvalidOperationException();
        }
        else
        {
            _s.Write(buffer);
        }
    }
    public override void WriteByte(byte value)
    {
        if (IsRestrictionEnabled)
        {
            throw new InvalidOperationException();
        }
        else
        {
            _s.WriteByte(value);
        }
    }

    // Async
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => _s.CopyToAsync(destination, bufferSize, cancellationToken);
    public override ValueTask DisposeAsync() => _s.DisposeAsync();
    public override Task FlushAsync(CancellationToken cancellationToken) => _s.FlushAsync(cancellationToken);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _s.ReadAsync(buffer, offset, count, cancellationToken);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _s.ReadAsync(buffer, cancellationToken);
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _s.WriteAsync(buffer, offset, count, cancellationToken);
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _s.WriteAsync(buffer, cancellationToken);
}
