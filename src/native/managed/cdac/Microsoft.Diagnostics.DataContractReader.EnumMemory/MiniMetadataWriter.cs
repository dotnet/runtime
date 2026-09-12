// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.EnumMemory;

internal static class MiniMetadataWriter
{
    private const uint MiniMetadataSignature = 0x6d727473;
    private const uint EENameStreamSignature = 0x614e4545;
    private const int StreamsHeaderSize = 12;
    private const int EENameStreamHeaderSize = 8;

    public static void Write(
        Target target,
        MemoryRegionEmitter emitter,
        IReadOnlyDictionary<TargetPointer, string> names)
    {
        if (names.Count == 0)
            return;

        TargetPointer bufferAddress =
            target.ReadPointer(target.ReadGlobalPointer(Constants.Globals.MiniMetaDataBuffAddress));
        uint capacity =
            target.Read<uint>(target.ReadGlobalPointer(Constants.Globals.MiniMetaDataBuffMaxSize));
        if (bufferAddress == TargetPointer.Null
            || capacity < StreamsHeaderSize + EENameStreamHeaderSize)
        {
            return;
        }

        byte[] buffer = new byte[capacity];
        int offset = StreamsHeaderSize + EENameStreamHeaderSize;
        uint count = 0;

        foreach ((TargetPointer address, string name) in names)
        {
            int nameSize = Encoding.UTF8.GetByteCount(name);
            int entrySize = checked(target.PointerSize + nameSize + 1);
            if (offset > buffer.Length - entrySize)
                break;

            WritePointer(buffer.AsSpan(offset, target.PointerSize), address.Value, target);
            offset += target.PointerSize;
            offset += Encoding.UTF8.GetBytes(name, buffer.AsSpan(offset, nameSize));
            buffer[offset++] = 0;
            count++;
        }

        WriteUInt32(buffer.AsSpan(0, sizeof(uint)), MiniMetadataSignature, target);
        WriteUInt32(buffer.AsSpan(4, sizeof(uint)), checked((uint)offset), target);
        WriteUInt32(buffer.AsSpan(8, sizeof(uint)), 1, target);
        WriteUInt32(buffer.AsSpan(StreamsHeaderSize, sizeof(uint)), EENameStreamSignature, target);
        WriteUInt32(buffer.AsSpan(StreamsHeaderSize + 4, sizeof(uint)), count, target);

        emitter.Add(bufferAddress.Value, checked((uint)offset));
        emitter.Update(bufferAddress.Value, buffer.AsSpan(0, offset));
    }

    private static void WritePointer(Span<byte> destination, ulong value, Target target)
    {
        if (target.PointerSize == sizeof(uint))
        {
            WriteUInt32(destination, checked((uint)value), target);
        }
        else if (target.IsLittleEndian)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(destination, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt64BigEndian(destination, value);
        }
    }

    private static void WriteUInt32(Span<byte> destination, uint value, Target target)
    {
        if (target.IsLittleEndian)
            BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
        else
            BinaryPrimitives.WriteUInt32BigEndian(destination, value);
    }
}
