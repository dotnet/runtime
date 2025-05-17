// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// A load command with info about the location of the symbol table and string table.
/// See https://github.com/apple-oss-distributions/cctools/blob/7a5450708479bbff61527d5e0c32a3f7b7e4c1d0/include/mach-o/loader.h#L908 for reference.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct SymbolTableLoadCommand
{
    private readonly MachLoadCommandType _command;
    private readonly uint _commandSize;
    public uint _symbolTableOffset;
    public uint _symbolsCount;
    private uint _stringTableOffset;
    private uint _stringTableSize; // in bytes

    public bool IsDefault => this.Equals(default(SymbolTableLoadCommand));

    public uint GetSymbolTableOffset(MachHeader header) => header.ConvertValue(_symbolTableOffset);
    public void SetSymbolTableOffset(uint value, MachHeader header) => _symbolTableOffset = header.ConvertValue(value);
    public uint GetSymbolsCount(MachHeader header) => header.ConvertValue(_symbolsCount);
    public void SetSymbolsCount(uint value, MachHeader header) => _symbolsCount = header.ConvertValue(value);
    public uint GetStringTableOffset(MachHeader header) => header.ConvertValue(_stringTableOffset);
    public void SetStringTableOffset(uint value, MachHeader header) => _stringTableOffset = header.ConvertValue(value);
    public uint GetStringTableSize(MachHeader header) => header.ConvertValue(_stringTableSize);
    public void SetStringTableSize(uint value, MachHeader header) => _stringTableSize = header.ConvertValue(value);
}
