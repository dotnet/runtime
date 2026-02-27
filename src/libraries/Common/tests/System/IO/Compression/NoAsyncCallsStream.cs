// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression.Tests;

// A stream meant to be used for testing that an implementation's sync methods do not accidentally call any async methods.
internal sealed class NoAsyncCallsStream : Stream
{
    private readonly Stream _s;

    public NoAsyncCallsStream(Stream stream) => _s = stream;

    public override bool CanRead => _s.CanRead;
    public override bool CanSeek => _s.CanSeek;
    public override bool CanTimeout => _s.CanTimeout;
    public override bool CanWrite => _s.CanWrite;
    public override long Length => _s.Length;
    public override long Position { get => _s.Position; set => _s.Position = value; }
    public override int ReadTimeout { get => _s.ReadTimeout; set => _s.ReadTimeout = value; }
    public override int WriteTimeout { get => _s.WriteTimeout; set => _s.WriteTimeout = value; }
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => _s.BeginRead(buffer, offset, count, callback, state);
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => _s.BeginWrite(buffer, offset, count, callback, state);
    public override void Close() => _s.Close();
    public override int EndRead(IAsyncResult asyncResult) => _s.EndRead(asyncResult);
    public override void EndWrite(IAsyncResult asyncResult) => _s.EndWrite(asyncResult);
    public override bool Equals(object? obj) => _s.Equals(obj);
    public override int GetHashCode() => _s.GetHashCode();
    public override int ReadByte() => _s.ReadByte();
    public override long Seek(long offset, SeekOrigin origin) => _s.Seek(offset, origin);
    public override void SetLength(long value) => _s.SetLength(value);
    public override string? ToString() => _s.ToString();

    // Sync
    public override void CopyTo(Stream destination, int bufferSize) => _s.CopyTo(destination, bufferSize);
    protected override void Dispose(bool disposing) => _s.Dispose();
    public override void Flush() => _s.Flush();
    public override int Read(byte[] buffer, int offset, int count) => _s.Read(buffer, offset, count);
    public override int Read(Span<byte> buffer) => _s.Read(buffer);
    public override void Write(byte[] buffer, int offset, int count) => _s.Write(buffer, offset, count);
    public override void Write(ReadOnlySpan<byte> buffer) => _s.Write(buffer);
    public override void WriteByte(byte value) => _s.WriteByte(value);

    // Async
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => throw new InvalidOperationException();
    public override ValueTask DisposeAsync() => throw new InvalidOperationException();
    public override Task FlushAsync(CancellationToken cancellationToken) => throw new InvalidOperationException();
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new InvalidOperationException();
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new InvalidOperationException();
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
}
