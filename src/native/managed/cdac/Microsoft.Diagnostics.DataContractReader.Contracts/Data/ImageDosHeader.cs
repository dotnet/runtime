// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType]
internal sealed partial class ImageDosHeader : IData<ImageDosHeader>
{
    private const int LfanewOffset = 60;

    [RawOffset(LfanewOffset, LittleEndian = true)]
    public int Lfanew { get; }
}
