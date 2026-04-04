// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace ILCompiler.Reflection.ReadyToRun.MachO;

/// <summary>
/// Represents a 64-bit symbol table entry.
/// See https://github.com/apple-oss-distributions/cctools/blob/7a5450708479bbff61527d5e0c32a3f7b7e4c1d0/include/mach-o/nlist.h#L92 for reference.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct NList64
{
    private readonly uint _stringTableIndex;
    private readonly byte _type;
    private readonly byte _section;
    private readonly ushort _description;
    private readonly ulong _value;

    public uint GetStringTableIndex(MachHeader header) => header.ConvertValue(_stringTableIndex);
    public ulong GetValue(MachHeader header) => header.ConvertValue(_value);
}
