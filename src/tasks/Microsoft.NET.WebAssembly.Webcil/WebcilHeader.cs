// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.NET.WebAssembly.Webcil;

/// <summary>
/// The header of a Webcil file.
/// </summary>
///
/// <remarks>
/// The header is a subset of the PE, COFF and CLI headers that are needed by the mono runtime to load managed assemblies.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct WebcilHeader
{
#if NET
    public InlineArray4<byte> Id;
#else
    public unsafe fixed byte Id[4];
#endif
    // 4 bytes
    public ushort VersionMajor;
    public ushort VersionMinor;
    // 8 bytes

    public ushort CoffSections;
    public ushort Reserved0;
    // 12 bytes
    public uint PeCliHeaderRva;
    public uint PeCliHeaderSize;
    // 20 bytes
    public uint PeDebugRva;
    public uint PeDebugSize;
    // 28 bytes
}
