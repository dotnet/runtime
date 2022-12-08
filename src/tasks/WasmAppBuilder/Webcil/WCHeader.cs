// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.WebAssembly.Build.Tasks.WebCil;

/// <summary>
/// The header of a WebCIL file.
/// </summary>
///
/// <remarks>
/// The header is a subset of the PE, COFF and CLI headers that are needed by the mono runtime to load managed assemblies.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct WCHeader
{
    public fixed byte id[2]; // 'W' 'C'
    public byte version;
    public byte reserved0; // 0
    // 4 bytes

    public ushort coff_sections;
    public ushort reserved1; // 0
    // 8 bytes
    public uint pe_cli_header_rva;
    public uint pe_cli_header_size;
    // 16 bytes
}
