// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ImageNTHeaders : IData<ImageNTHeaders>
{
    static ImageNTHeaders IData<ImageNTHeaders>.Create(Target target, TargetPointer address) => new ImageNTHeaders(target, address);
    public const int FileHeaderOffset = 4;
    public const int OptionalHeaderOffset = 24;
    public ImageNTHeaders(Target target, TargetPointer address)
    {
        FileHeader = target.ProcessedData.GetOrAdd<ImageFileHeader>(address + FileHeaderOffset);
        OptionalHeader = target.ProcessedData.GetOrAdd<ImageOptionalHeader>(address + OptionalHeaderOffset);
    }
    public ImageFileHeader FileHeader { get; init; }
    public ImageOptionalHeader OptionalHeader { get; init; }
}
