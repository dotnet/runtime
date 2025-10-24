// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ImageDataDirectory : IData<ImageDataDirectory>
{
    static ImageDataDirectory IData<ImageDataDirectory>.Create(Target target, TargetPointer address)
        => new ImageDataDirectory(target, address);

    public ImageDataDirectory(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ImageDataDirectory);

        VirtualAddress = target.Read<uint>(address + (ulong)type.Fields[nameof(VirtualAddress)].Offset);
        Size = target.Read<uint>(address + (ulong)type.Fields[nameof(Size)].Offset);
    }

    public uint VirtualAddress { get; }
    public uint Size { get; }
}
