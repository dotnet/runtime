// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// The structure for the 64-bit segment load command in a Mach-O binary.
/// See https://github.com/apple-oss-distributions/cctools/blob/7a5450708479bbff61527d5e0c32a3f7b7e4c1d0/include/mach-o/loader.h#L394 for reference.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Segment64LoadCommand
{
    private readonly MachLoadCommandType _command;
    private readonly uint _commandSize;
    public NameBuffer Name;
    private readonly ulong _address;
    private ulong _size;
    private readonly ulong _fileOffset;
    private ulong _fileSize;
    private readonly uint _maximumProtection;
    private readonly uint _initialProtection;
    private readonly uint _numberOfSections;
    private readonly uint _flags;

    public bool IsDefault => this.Equals(default(Segment64LoadCommand));

    public ulong GetFileOffset(MachHeader header) => header.ConvertValue(_fileOffset);
    public ulong GetFileSize(MachHeader header) => header.ConvertValue(_fileSize);
    public void SetFileSize(ulong value, MachHeader header) => _fileSize = header.ConvertValue(value);
    public void SetVMSize(ulong value, MachHeader header) => _size = header.ConvertValue(value);
    public ulong GetVMSize(MachHeader header) => header.ConvertValue(_size);
    public uint GetSectionsCount(MachHeader header) => header.ConvertValue(_numberOfSections);
}
