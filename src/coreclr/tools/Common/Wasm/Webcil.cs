// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.WebAssembly.Webcil;

internal static class WebcilConstants
{
    public const int WC_VERSION_MAJOR = 1;
    public const int WC_VERSION_MINOR = 0;

    /// <summary>
    /// 'WbIL' magic bytes interpreted as a little-endian uint32.
    /// </summary>
    public const uint WEBCIL_MAGIC = 0x4c496257;
}

/// <summary>
/// The header of a Webcil file.
/// </summary>
/// <remarks>
/// The header is a subset of the PE, COFF and CLI headers that are needed by the runtime to load managed assemblies.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct WebcilHeader
{
    public uint Id;
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
    public uint TableBase;
    // 32 bytes
}

/// <summary>
/// Represents a section header in a Webcil file.
/// </summary>
/// <remarks>
/// This is the Webcil analog of <see cref="System.Reflection.PortableExecutable.SectionHeader"/>, but with fewer fields.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct WebcilSectionHeader
{
    public readonly uint VirtualSize;
    public readonly uint VirtualAddress;
    public readonly uint SizeOfRawData;
    public readonly uint PointerToRawData;

    public WebcilSectionHeader(uint virtualSize, uint virtualAddress, uint sizeOfRawData, uint pointerToRawData)
    {
        VirtualSize = virtualSize;
        VirtualAddress = virtualAddress;
        SizeOfRawData = sizeOfRawData;
        PointerToRawData = pointerToRawData;
    }
}
