// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct SectionLoadCommand
{
    private readonly NameBuffer _sectionName;
    private readonly NameBuffer _segmentName;
    private readonly uint _address;
    private readonly uint _size;
    private readonly uint _fileOffset;
    private readonly uint _log2Alignment;
    private readonly uint _relocationOffset;
    private readonly uint _numberOfReloationEntries;
    private readonly uint _flags;
    private readonly uint _reserved1;
    private readonly uint _reserved2;

    public uint GetFileOffset(MachHeader header) => header.ConvertValue(_fileOffset);
}
