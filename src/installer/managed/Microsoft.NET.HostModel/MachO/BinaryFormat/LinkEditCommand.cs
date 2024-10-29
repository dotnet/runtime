// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

[StructLayout(LayoutKind.Sequential)]
internal struct LinkEditCommand
{
    private readonly MachLoadCommandType _command;
    private readonly uint _commandSize;
    private readonly uint _dataOffset;
    private readonly uint _dataSize;

    public LinkEditCommand(MachLoadCommandType command, uint dataOffset, uint dataSize, MachHeader header)
    {
        _command = (MachLoadCommandType)header.ConvertValue((uint)command);
        uint commandSize;
        unsafe { commandSize = (uint) sizeof(LinkEditCommand); }
        _commandSize = header.ConvertValue(commandSize);
        _dataOffset = header.ConvertValue(dataOffset);
        _dataSize = header.ConvertValue(dataSize);
    }

    public bool IsDefault => this.Equals(default(LinkEditCommand));

    internal uint GetDataOffset(MachHeader header) => header.ConvertValue(_dataOffset);
    internal uint GetFileSize(MachHeader header) => header.ConvertValue(_dataSize);
}
