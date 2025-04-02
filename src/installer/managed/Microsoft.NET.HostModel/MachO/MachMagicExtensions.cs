// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.IO;

namespace Microsoft.NET.HostModel.MachO;

internal static class MachMagicExtensions
{
    public static uint ConvertValue(this MachMagic magic, uint value)
    {
        return magic switch
        {
            MachMagic.MachHeader64CurrentEndian or MachMagic.MachHeaderCurrentEndian
                => value,
            MachMagic.MachHeader64OppositeEndian or MachMagic.MachHeaderOppositeEndian
                => BinaryPrimitives.ReverseEndianness(value),
            _ => throw new InvalidDataException($"Invalid magic value 0x{magic:X}")
        };
    }

    public static ulong ConvertValue(this MachMagic magic, ulong value)
    {
        return magic switch
        {
            MachMagic.MachHeader64CurrentEndian or MachMagic.MachHeaderCurrentEndian
                => value,
            MachMagic.MachHeader64OppositeEndian or MachMagic.MachHeaderOppositeEndian
                => BinaryPrimitives.ReverseEndianness(value),
            _ => throw new InvalidDataException($"Invalid magic value 0x{magic:X}")
        };
    }
}
