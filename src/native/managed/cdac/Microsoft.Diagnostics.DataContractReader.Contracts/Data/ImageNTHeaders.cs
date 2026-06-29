// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType]
internal sealed partial class ImageNTHeaders : IData<ImageNTHeaders>
{
    public const int FileHeaderOffset = 4;
    public const int OptionalHeaderOffset = 24;

    [RawOffset(FileHeaderOffset)]
    public ImageFileHeader FileHeader { get; }

    [RawOffset(OptionalHeaderOffset)]
    public ImageOptionalHeader OptionalHeader { get; }
}
