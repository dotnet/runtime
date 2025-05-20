// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using Microsoft.NET.HostModel.MachO;

internal class SimpleBlob : Blob
{
    public override uint Size => base.Size + (uint)_data.Length;
    protected byte[] _data;

    public SimpleBlob(MemoryMappedViewAccessor accessor, long offset) : base(accessor, offset)
    {
        accessor.Read(offset + sizeof(uint), out uint size);
        size = size.ConvertFromBigEndian();
        _data = new byte[size - base.Size];
        if (size != base.Size)
        {
            accessor.ReadArray(offset + sizeof(uint) * 2, _data, 0, _data.Length);
        }
        Debug.Assert(size == Size, $"Invalid size for SimpleBlob: {size}");
    }

    public SimpleBlob(BlobMagic magic, byte[] data) : base(magic)
    {
        _data = data;
    }

    public override void Write(MemoryMappedViewAccessor accessor, long offset)
    {
        base.Write(accessor, offset);
        if (_data.Length != 0)
        {
            accessor.WriteArray(offset + base.Size, _data, 0, _data.Length);
        }
    }

    public override void Write(Span<byte> buffer)
    {
        base.Write(buffer);
        _data.CopyTo(buffer.Slice((int)base.Size));
    }
}
