// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

[StructLayout(LayoutKind.Sequential)]
internal struct LoadCommand
{
    private readonly MachLoadCommandType _command;
    private readonly uint _commandSize;
    public MachLoadCommandType GetCommandType(MachHeader header) => (MachLoadCommandType)header.ConvertValue((uint)_command);
    public uint GetCommandSize(MachHeader header) => header.ConvertValue(_commandSize);
}
