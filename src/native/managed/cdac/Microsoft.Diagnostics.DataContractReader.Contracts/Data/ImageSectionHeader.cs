// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ImageSectionHeader : IData<ImageSectionHeader>
{
    static ImageSectionHeader IData<ImageSectionHeader>.Create(Target target, TargetPointer address) => new ImageSectionHeader(target, address);
    private const int VirtualSizeOffset = 8;
    private const int VirtualAddressOffset = 12;
    private const int SizeOfRawDataOffset = 16;
    private const int PointerToRawDataOffset = 20;
    public const uint Size = 40;
    public ImageSectionHeader(Target target, TargetPointer address)
    {
        VirtualSize = target.ReadLittleEndian<uint>(address + VirtualSizeOffset);
        VirtualAddress = target.ReadLittleEndian<uint>(address + VirtualAddressOffset);
        SizeOfRawData = target.ReadLittleEndian<uint>(address + SizeOfRawDataOffset);
        PointerToRawData = target.ReadLittleEndian<uint>(address + PointerToRawDataOffset);
    }

    public uint VirtualSize { get; init; }
    public uint VirtualAddress { get; init; }
    public uint SizeOfRawData { get; init; }
    public uint PointerToRawData { get; init; }
}
