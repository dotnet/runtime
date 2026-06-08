// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType]
internal sealed partial class ImageSectionHeader : IData<ImageSectionHeader>
{
    private const int VirtualSizeOffset = 8;
    private const int VirtualAddressOffset = 12;
    private const int SizeOfRawDataOffset = 16;
    private const int PointerToRawDataOffset = 20;
    public const uint Size = 40;

    [RawOffset(VirtualSizeOffset, LittleEndian = true)]
    public uint VirtualSize { get; }

    [RawOffset(VirtualAddressOffset, LittleEndian = true)]
    public uint VirtualAddress { get; }

    [RawOffset(SizeOfRawDataOffset, LittleEndian = true)]
    public uint SizeOfRawData { get; }

    [RawOffset(PointerToRawDataOffset, LittleEndian = true)]
    public uint PointerToRawData { get; }
}
