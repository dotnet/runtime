// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ImageOptionalHeader : IData<ImageOptionalHeader>
{
    static ImageOptionalHeader IData<ImageOptionalHeader>.Create(Target target, TargetPointer address) => new ImageOptionalHeader(target, address);
    public ImageOptionalHeader(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ImageOptionalHeader);
        SectionAlignment = target.Read<uint>(address + (ulong)type.Fields[nameof(SectionAlignment)].Offset);
    }
    public uint SectionAlignment { get; init; }
}
