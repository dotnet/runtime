// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// The Mach-O header is the first data in a Mach-O file.
/// See https://github.com/apple-oss-distributions/cctools/blob/7a5450708479bbff61527d5e0c32a3f7b7e4c1d0/include/mach-o/loader.h#L80 for reference.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MachHeader
{
    private readonly MachMagic _magic;
    private readonly uint _cpuType;
    private readonly uint _cpuSubType;
    private readonly MachFileType _fileType;
    private uint _numberOfCommands;
    private uint _sizeOfCommands;
    private readonly uint _flags;
    private readonly uint reserved;

    public uint NumberOfCommands { get => _magic.ConvertValue(_numberOfCommands); set => _numberOfCommands = _magic.ConvertValue(value); }
    public uint SizeOfCommands { get => _magic.ConvertValue(_sizeOfCommands); set => _sizeOfCommands = _magic.ConvertValue(value); }
    public bool Is64Bit => _magic is MachMagic.MachHeader64CurrentEndian or MachMagic.MachHeader64OppositeEndian;
    public MachFileType FileType => (MachFileType)_magic.ConvertValue((uint)_fileType);

    public uint ConvertValue(uint value) => _magic.ConvertValue(value);
    public ulong ConvertValue(ulong value) => _magic.ConvertValue(value);
}
