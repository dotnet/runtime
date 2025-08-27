// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ImageOptionalHeader : IData<ImageOptionalHeader>
{
    static ImageOptionalHeader IData<ImageOptionalHeader>.Create(Target target, TargetPointer address) => new ImageOptionalHeader(target, address);
    private const int SectionAlignmentOffset = 32;
    public ImageOptionalHeader(Target target, TargetPointer address)
    {
        SectionAlignment = target.ReadLittleEndian<uint>(address + SectionAlignmentOffset);
    }
    public uint SectionAlignment { get; init; }
}
