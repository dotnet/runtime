// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// The base structure for all load commands in a Mach-O binary.
/// Load commands are used to describe the structure of the binary.
/// See https://github.com/apple-oss-distributions/cctools/blob/7a5450708479bbff61527d5e0c32a3f7b7e4c1d0/include/mach-o/loader.h#L265 for reference;
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct LoadCommand
{
    private readonly MachLoadCommandType _command;
    private readonly uint _commandSize;
    public MachLoadCommandType GetCommandType(MachHeader header) => (MachLoadCommandType)header.ConvertValue((uint)_command);
    public uint GetCommandSize(MachHeader header) => header.ConvertValue(_commandSize);
}
