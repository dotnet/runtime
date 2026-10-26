// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Internal.Metadata.NativeFormat;

internal ref struct BlobReader(ReadOnlySpan<byte> blob)
{
    private readonly ReadOnlySpan<byte> _blob = blob;
    private int _offset;

    public readonly int Offset => _offset;
    public readonly int RemainingBytes => _blob.Length - _offset;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> ReadBytes(int count)
    {
        ReadOnlySpan<byte> result = _blob.Slice(_offset, count);
        _offset += count;
        return result;
    }

    public byte ReadByte() => ReadBytes(sizeof(byte))[0];
    public sbyte ReadSByte() => (sbyte)ReadByte();
    public bool ReadBoolean() => ReadByte() != 0;
    public char ReadChar() => (char)ReadUInt16();
    public ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(ReadBytes(sizeof(ushort)));
    public short ReadInt16() => (short)ReadUInt16();
    public uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(ReadBytes(sizeof(uint)));
    public int ReadInt32() => (int)ReadUInt32();
    public ulong ReadUInt64() => BinaryPrimitives.ReadUInt64LittleEndian(ReadBytes(sizeof(ulong)));
    public long ReadInt64() => (long)ReadUInt64();
    public float ReadSingle() => BinaryPrimitives.ReadSingleLittleEndian(ReadBytes(sizeof(float)));
    public double ReadDouble() => BinaryPrimitives.ReadDoubleLittleEndian(ReadBytes(sizeof(double)));

    private uint ReadUnsigned()
    {
        byte first = ReadByte();
        if ((first & 1) == 0)
            return (uint)(first >> 1);

        if ((first & 2) == 0)
            return (uint)(first >> 2) | ((uint)ReadByte() << 6);

        if ((first & 4) == 0)
        {
            uint value = (uint)(first >> 3) | ((uint)ReadByte() << 5);
            return value | ((uint)ReadByte() << 13);
        }

        if ((first & 8) == 0)
        {
            uint value = (uint)(first >> 4) | ((uint)ReadByte() << 4) | ((uint)ReadByte() << 12);
            return value | ((uint)ReadByte() << 20);
        }

        Debug.Assert((first & 16) == 0);
        return ReadUInt32();
    }

    public string? ReadString()
    {
        // ECMA-335 II.23.3: SerString is a compressed UTF8 byte count, or 0xFF for null.
        byte first = ReadByte();
        if (first == 0xff)
            return null;

        int length = first;
        if ((first & 0x80) != 0)
        {
            if ((first & 0x40) == 0)
            {
                length = ((first & 0x3f) << 8) | ReadByte();
            }
            else
            {
                Debug.Assert((first & 0xe0) == 0xc0);
                length = ((first & 0x1f) << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte();
            }
        }

        return Encoding.UTF8.GetString(ReadBytes(length));
    }

    public Handle ReadTypeHandle()
    {
        uint value = ReadUnsigned();
        Handle handle = new Handle((HandleType)(value & 0x7f), (int)(value >> 7));
        Debug.Assert(handle.IsNil || handle.HandleType is
            HandleType.TypeDefinition or HandleType.TypeReference or HandleType.TypeSpecification);
        return handle;
    }
}
