// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.WebAssembly.Webcil;

/// <summary>
/// This is the Webcil analog of System.Reflection.PortableExecutable.SectionHeader, but with fewer fields
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct WebcilSectionHeader
{
    public readonly int VirtualSize;
    public readonly int VirtualAddress;
    public readonly int SizeOfRawData;
    public readonly int PointerToRawData;

    public WebcilSectionHeader(int virtualSize, int virtualAddress, int sizeOfRawData, int pointerToRawData)
    {
        VirtualSize = virtualSize;
        VirtualAddress = virtualAddress;
        SizeOfRawData = sizeOfRawData;
        PointerToRawData = pointerToRawData;
    }
}
