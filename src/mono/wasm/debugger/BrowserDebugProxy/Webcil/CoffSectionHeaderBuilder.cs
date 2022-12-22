// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.WebAssembly.Diagnostics.Webcil;

/// <summary>
/// This is System.Reflection.PortableExecutable.CoffHeader, but with a public constructor so that
/// we can make our own copies of it, and with fewer fields
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CoffSectionHeaderBuilder
{
    public readonly int VirtualSize;
    public readonly int VirtualAddress;
    public readonly int SizeOfRawData;
    public readonly int PointerToRawData;

    public CoffSectionHeaderBuilder(int virtualSize, int virtualAddress, int sizeOfRawData, int pointerToRawData)
    {
        VirtualSize = virtualSize;
        VirtualAddress = virtualAddress;
        SizeOfRawData = sizeOfRawData;
        PointerToRawData = pointerToRawData;
    }
}
