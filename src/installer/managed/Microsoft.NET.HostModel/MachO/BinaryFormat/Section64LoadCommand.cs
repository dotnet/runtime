// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// A load command that provides information about a section in a segment.
/// See https://github.com/apple-oss-distributions/cctools/blob/7a5450708479bbff61527d5e0c32a3f7b7e4c1d0/include/mach-o/loader.h#L468 for reference.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct Section64LoadCommand
{
    private readonly NameBuffer _sectionName;
    private readonly NameBuffer _segmentName;
    private readonly ulong _address;
    private readonly ulong _size;
    private readonly uint _fileOffset;
    private readonly uint _log2Alignment;
    private readonly uint _relocationOffset;
    private readonly uint _numberOfReloationEntries;
    private readonly uint _flags;
    private readonly uint _reserved1;
    private readonly uint _reserved2;
    private readonly uint _reserved3;

    internal uint GetFileOffset(MachHeader header) => header.ConvertValue(_fileOffset);
}
