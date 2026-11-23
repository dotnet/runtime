// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.Serialization;

internal sealed class SerializationStream : Stream
{
    private readonly Stream _stream;
    private readonly CancellationToken _cancellationToken;

    internal SerializationStream(Stream stream, CancellationToken cancellationToken)
    {
        _stream = stream;
        _cancellationToken = cancellationToken;
    }

    public override bool CanRead => _stream.CanRead;
    public override bool CanWrite => _stream.CanWrite;
    public override bool CanSeek => _stream.CanSeek;
    public override bool CanTimeout => _stream.CanTimeout;
    public override long Length => _stream.Length;
    public override long Position { get => _stream.Position; set => _stream.Position = value; }
    public override int ReadTimeout { get => _stream.ReadTimeout; set => _stream.ReadTimeout = value; }
    public override int WriteTimeout { get => _stream.WriteTimeout; set => _stream.WriteTimeout = value; }

    public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
    public override void SetLength(long value) => _stream.SetLength(value);
    public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);
    public override int Read(Span<byte> buffer) => _stream.Read(buffer);
    public override int ReadByte() => _stream.ReadByte();
    public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);
    public override void Write(ReadOnlySpan<byte> buffer) => _stream.Write(buffer);
    public override void WriteByte(byte value) => _stream.WriteByte(value);
    public override void Flush() => _stream.Flush();

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        _stream.ReadAsync(buffer, offset, count, GetCancellationToken(cancellationToken));

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        _stream.ReadAsync(buffer, GetCancellationToken(cancellationToken));

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        _stream.WriteAsync(buffer, offset, count, GetCancellationToken(cancellationToken));

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
        _stream.WriteAsync(buffer, GetCancellationToken(cancellationToken));

    public override Task FlushAsync(CancellationToken cancellationToken) =>
        _stream.FlushAsync(GetCancellationToken(cancellationToken));

    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private CancellationToken GetCancellationToken(CancellationToken cancellationToken)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        cancellationToken.ThrowIfCancellationRequested();
        return _cancellationToken.CanBeCanceled ? _cancellationToken : cancellationToken;
    }
}
