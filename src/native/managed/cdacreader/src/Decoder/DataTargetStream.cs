// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Diagnostics.DataContractReader.Legacy;

namespace Microsoft.Diagnostics.DataContractReader.Decoder;

internal class DataTargetStream(ICLRDataTarget dataTarget, ulong startPosition) : Stream
{
    private readonly ulong _startPosition = startPosition;
    private long _offset;
    private readonly ICLRDataTarget _dataTarget = dataTarget;

    private ulong GlobalPosition => _startPosition + (ulong)_offset;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => 0x10000;

    public override long Position { get => _offset; set => _offset = value; }

    public override unsafe int Read(byte[] buffer, int offset, int count)
    {
        Span<byte> span = buffer;
        return Read(span.Slice(start: offset, length: count));
    }
    public override unsafe int Read(Span<byte> buffer)
    {
        fixed (byte* bufferPtr = buffer)
        {
            uint bytesRead;
            int hr = _dataTarget.ReadVirtual(GlobalPosition, bufferPtr, (uint)buffer.Length, &bytesRead);
            _offset += bytesRead;
            if (hr != 0)
                throw new InvalidOperationException($"ReadVirtual failed with hr={hr}");
            return (int)bytesRead;
        }
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

    public override void Flush() => throw new NotImplementedException();
    public override void SetLength(long value) => throw new NotImplementedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
}
