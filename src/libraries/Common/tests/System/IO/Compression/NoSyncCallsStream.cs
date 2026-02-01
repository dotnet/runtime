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
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new InvalidOperationException();
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new InvalidOperationException();
    public override void CopyTo(Stream destination, int bufferSize) => throw new InvalidOperationException();
    protected override void Dispose(bool disposing) => throw new InvalidOperationException();
    public override int EndRead(IAsyncResult asyncResult) => throw new InvalidOperationException();
    public override void EndWrite(IAsyncResult asyncResult) => throw new InvalidOperationException();
    public override void Flush() => throw new InvalidOperationException();
    public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException();
    public override int Read(Span<byte> buffer) => throw new InvalidOperationException();
    public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException();
    public override void Write(ReadOnlySpan<byte> buffer) => throw new InvalidOperationException();
    public override void WriteByte(byte value) => throw new InvalidOperationException();

    // Async
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => _s.CopyToAsync(destination, bufferSize, cancellationToken);
    public override ValueTask DisposeAsync() => _s.DisposeAsync();
    public override Task FlushAsync(CancellationToken cancellationToken) => _s.FlushAsync(cancellationToken);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _s.ReadAsync(buffer, offset, count, cancellationToken);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _s.ReadAsync(buffer, cancellationToken);
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _s.WriteAsync(buffer, offset, count, cancellationToken);
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _s.WriteAsync(buffer, cancellationToken);
}
