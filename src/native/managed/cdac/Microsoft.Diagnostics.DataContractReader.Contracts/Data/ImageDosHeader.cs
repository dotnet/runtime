// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ImageDosHeader : IData<ImageDosHeader>
{
    static ImageDosHeader IData<ImageDosHeader>.Create(Target target, TargetPointer address)
        => new ImageDosHeader(target, address);

    public ImageDosHeader(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ImageDosHeader);
        Lfanew = target.Read<int>(address + (ulong)type.Fields[nameof(Lfanew)].Offset);
    }
    public int Lfanew { get; init; }
}
