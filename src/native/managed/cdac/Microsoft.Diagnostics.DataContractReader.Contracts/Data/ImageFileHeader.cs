// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType]
internal sealed partial class ImageFileHeader : IData<ImageFileHeader>
{
    private const int NumberOfSectionsOffset = 2;
    private const int SizeOfOptionalHeaderOffset = 16;

    [RawOffset(NumberOfSectionsOffset, LittleEndian = true)]
    public ushort NumberOfSections { get; }

    [RawOffset(SizeOfOptionalHeaderOffset, LittleEndian = true)]
    public ushort SizeOfOptionalHeader { get; }
}
