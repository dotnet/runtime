// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// A load command that provides information about an item in the __LINKEDIT segment.
/// We only care about this when the _command is CodeSignature.
/// See https://github.com/apple-oss-distributions/cctools/blob/7a5450708479bbff61527d5e0c32a3f7b7e4c1d0/include/mach-o/loader.h#L1232 for reference.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct LinkEditLoadCommand
{
    private readonly MachLoadCommandType _command;
    private readonly uint _commandSize;
    private readonly uint _dataOffset;
    private readonly uint _dataSize;

    public LinkEditLoadCommand(MachLoadCommandType command, uint dataOffset, uint dataSize, MachHeader header)
    {
        _command = (MachLoadCommandType)header.ConvertValue((uint)command);
        uint commandSize;
        unsafe { commandSize = (uint)sizeof(LinkEditLoadCommand); }
        _commandSize = header.ConvertValue(commandSize);
        _dataOffset = header.ConvertValue(dataOffset);
        _dataSize = header.ConvertValue(dataSize);
    }

    public bool IsDefault => this.Equals(default(LinkEditLoadCommand));

    internal uint GetDataOffset(MachHeader header) => header.ConvertValue(_dataOffset);
    internal uint GetFileSize(MachHeader header) => header.ConvertValue(_dataSize);
}
