// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ImageNTHeaders : IData<ImageNTHeaders>
{
    static ImageNTHeaders IData<ImageNTHeaders>.Create(Target target, TargetPointer address) => new ImageNTHeaders(target, address);
    public ImageNTHeaders(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ImageNTHeaders);
        OptionalHeader = target.ProcessedData.GetOrAdd<ImageOptionalHeader>(address + (ulong)type.Fields[nameof(OptionalHeader)].Offset);
        FileHeader = target.ProcessedData.GetOrAdd<ImageFileHeader>(address + (ulong)type.Fields[nameof(FileHeader)].Offset);
    }

    public ImageOptionalHeader OptionalHeader { get; init; }
    public ImageFileHeader FileHeader { get; init; }
}
