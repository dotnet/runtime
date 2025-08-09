// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ImageSectionHeader : IData<ImageSectionHeader>
{
    static ImageSectionHeader IData<ImageSectionHeader>.Create(Target target, TargetPointer address) => new ImageSectionHeader(target, address);
    public ImageSectionHeader(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ImageSectionHeader);

        VirtualSize = target.Read<uint>(address + (ulong)type.Fields[nameof(VirtualSize)].Offset);
        VirtualAddress = target.Read<uint>(address + (ulong)type.Fields[nameof(VirtualAddress)].Offset);
        PointerToRawData = target.Read<uint>(address + (ulong)type.Fields[nameof(PointerToRawData)].Offset);
    }

    public uint VirtualSize { get; init; }
    public uint VirtualAddress { get; init; }
    public uint PointerToRawData { get; init; }
}
