// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ImageFileHeader : IData<ImageFileHeader>
{
    static ImageFileHeader IData<ImageFileHeader>.Create(Target target, TargetPointer address) => new ImageFileHeader(target, address);
    public ImageFileHeader(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ImageFileHeader);
        NumberOfSections = target.Read<ushort>(address + (ulong)type.Fields[nameof(NumberOfSections)].Offset);
        SizeOfOptionalHeader = target.Read<ushort>(address + (ulong)type.Fields[nameof(SizeOfOptionalHeader)].Offset);
    }
    public ushort NumberOfSections { get; init; }
    public ushort SizeOfOptionalHeader { get; init; }
}
