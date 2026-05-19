// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Generated;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType]
internal sealed partial class ImageFileHeader : IData<ImageFileHeader>
{
    private const int NumberOfSectionsOffset = 2;
    private const int SizeOfOptionalHeaderOffset = 16;

    [FieldOffset(NumberOfSectionsOffset, LittleEndian = true)]
    public ushort NumberOfSections { get; }

    [FieldOffset(SizeOfOptionalHeaderOffset, LittleEndian = true)]
    public ushort SizeOfOptionalHeader { get; }
}
