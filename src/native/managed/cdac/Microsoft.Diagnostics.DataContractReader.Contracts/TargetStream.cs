// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Diagnostics.DataContractReader;

internal sealed class TargetStream(Target target, ulong startPosition, long size) : Stream
{
    private readonly ulong _startPosition = startPosition;
    private long _offset;
    private readonly long _size = size;
    private readonly Target _target = target;

    private ulong GlobalPosition => _startPosition + (ulong)_offset;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => _size;

    public override long Position { get => _offset; set => _offset = value; }

    public override unsafe int Read(byte[] buffer, int offset, int count)
    {
        Span<byte> span = buffer;
        return Read(span.Slice(start: offset, length: count));
    }
    public override unsafe int Read(Span<byte> buffer)
    {
        _target.ReadBuffer(GlobalPosition, buffer);
        _offset += buffer.Length;
        return buffer.Length;
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                _offset = offset;
                break;
            case SeekOrigin.Current:
                _offset += offset;
                break;
            case SeekOrigin.End:
                throw new NotSupportedException();
        }
        return _offset;
    }

    public override void Flush() { } // No-op for read-only stream
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
