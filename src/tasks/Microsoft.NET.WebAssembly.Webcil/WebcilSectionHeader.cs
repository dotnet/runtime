// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.NET.WebAssembly.Webcil;

/// <summary>
/// WebCIL v1.0 section header — binary-compatible with IMAGE_SECTION_HEADER (40 bytes).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct WebcilSectionHeader
{
    public fixed byte Name[8];
    public int VirtualSize;
    public int VirtualAddress;
    public int SizeOfRawData;
    public int PointerToRawData;
    public int PointerToRelocations;
    public int PointerToLinenumbers;
    public ushort NumberOfRelocations;
    public ushort NumberOfLinenumbers;
    public int Characteristics;

    public WebcilSectionHeader(string name, int virtualSize, int virtualAddress, int sizeOfRawData, int pointerToRawData)
    {
        this = default;
        VirtualSize = virtualSize;
        VirtualAddress = virtualAddress;
        SizeOfRawData = sizeOfRawData;
        PointerToRawData = pointerToRawData;

        ReadOnlySpan<byte> nameBytes = Encoding.ASCII.GetBytes(name);
        int len = Math.Min(nameBytes.Length, 8);
        for (int i = 0; i < len; i++)
            Name[i] = nameBytes[i];
    }
}
