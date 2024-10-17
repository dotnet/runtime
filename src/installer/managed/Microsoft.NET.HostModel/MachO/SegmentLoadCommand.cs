// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

[StructLayout(LayoutKind.Sequential)]
internal struct SegmentLoadCommand
{
    private readonly uint _command;
    private readonly uint _commandSize;
    public NameBuffer Name;
    private readonly uint _address;
    private readonly uint _size;
    private readonly uint _fileOffset;
    private uint _fileSize;
    private readonly uint _maximumProtection;
    private readonly uint _initialProtection;
    private readonly uint _numberOfSections;
    private readonly uint _flags;

    public bool IsDefault => this.Equals(default(SegmentLoadCommand));
    public uint GetFileOffset(MachHeader header) => header.ConvertValue(_fileOffset);
    public uint GetFileSize(MachHeader header) => header.ConvertValue(_fileSize);
    public void SetFileSize(uint value, MachHeader header) => _fileSize = header.ConvertValue(value);
    public uint GetSectionsCount(MachHeader header) => header.ConvertValue(_numberOfSections);
}
