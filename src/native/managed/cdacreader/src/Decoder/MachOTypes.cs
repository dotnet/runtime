// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader.Decoder;

internal struct Mach64Header
{
    public const uint LE_MAGIC = 0xfeedfacf; // 64-bit Mach-O file
    public const uint BE_MAGIC = 0xcffaedfe; // 64-bit reversed Mach-O file

    public uint magic;          // mach magic number identifier
    public uint cpuType;        // cpu specifier
    public uint cpuSubType;     // machine specifier
    public uint fileType;       // type of file
    public uint nCmds;          // number of load commands
    public uint sizeOfCmds;     // the size of all the load commands
    public uint flags;          // flags
    public uint reserved;       // reserved

    public Mach64Header(BinaryReader reader)
    {
        magic = reader.ReadUInt32();
        cpuType = reader.ReadUInt32();
        cpuSubType = reader.ReadUInt32();
        fileType = reader.ReadUInt32();
        nCmds = reader.ReadUInt32();
        sizeOfCmds = reader.ReadUInt32();
        flags = reader.ReadUInt32();
        reserved = reader.ReadUInt32();
    }
}
