// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

internal sealed class NativeReader(Target target, TargetPointer baseAddress)
{
    private readonly Target _target = target;
    private readonly TargetPointer _baseAddress = baseAddress;

    public byte ReadUInt8(uint offset) => _target.Read<byte>(_baseAddress + offset);
    public ushort ReadUInt16(uint offset) => _target.Read<ushort>(_baseAddress + offset);
    public uint ReadUInt32(uint offset) => _target.Read<uint>(_baseAddress + offset);

    public uint DecodeUnsigned(uint offset, out uint value)
    {
        value = 0;
        uint val = ReadUInt8(offset);
        if ((val & 1) == 0)
        {
            value = val >> 1;
            offset += 1;
        }
        else
        if ((val & 2) == 0)
        {
            value = val >> 2;
            value |= ((uint)ReadUInt8(offset + 1)) << 6;
            offset += 2;
        }
        else
        if ((val & 4) == 0)
        {
            value = val >> 3;
            value |= ((uint)ReadUInt8(offset + 1)) << 5;
            value |= ((uint)ReadUInt8(offset + 2)) << 13;
            offset += 3;
        }
        else
        if ((val & 8) == 0)
        {
            value = val >> 4;
            value |= ((uint)ReadUInt8(offset + 1)) << 4;
            value |= ((uint)ReadUInt8(offset + 2)) << 12;
            value |= ((uint)ReadUInt8(offset + 3)) << 20;
            offset += 4;
        }
        else
        if ((val & 16) == 0)
        {
            value = ReadUInt32(offset + 1);
            offset += 5;
        }
        else
        {
            throw new InvalidOperationException("Invalid encoding in DecodeUnsigned");
        }

        return offset;
    }
}
