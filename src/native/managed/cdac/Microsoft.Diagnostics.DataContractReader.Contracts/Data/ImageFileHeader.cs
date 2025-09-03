// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ImageFileHeader : IData<ImageFileHeader>
{
    static ImageFileHeader IData<ImageFileHeader>.Create(Target target, TargetPointer address) => new ImageFileHeader(target, address);
    private const int NumberOfSectionsOffset = 2;
    private const int SizeOfOptionalHeaderOffset = 16;
    public ImageFileHeader(Target target, TargetPointer address)
    {
        NumberOfSections = target.ReadLittleEndian<ushort>(address + NumberOfSectionsOffset);
        SizeOfOptionalHeader = target.ReadLittleEndian<ushort>(address + SizeOfOptionalHeaderOffset);
    }
    public ushort NumberOfSections { get; init; }
    public ushort SizeOfOptionalHeader { get; init; }
}
