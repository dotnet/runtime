// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType]
internal sealed partial class ImageOptionalHeader : IData<ImageOptionalHeader>
{
    private const int SectionAlignmentOffset = 32;
    private const int SizeOfImageOffset = 56;

    [RawOffset(SectionAlignmentOffset, LittleEndian = true)]
    public partial uint SectionAlignment { get; }

    [RawOffset(SizeOfImageOffset, LittleEndian = true)]
    public partial uint SizeOfImage { get; }
}
