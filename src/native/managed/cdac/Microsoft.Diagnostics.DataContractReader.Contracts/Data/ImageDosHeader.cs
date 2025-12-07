// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ImageDosHeader : IData<ImageDosHeader>
{
    static ImageDosHeader IData<ImageDosHeader>.Create(Target target, TargetPointer address)
        => new ImageDosHeader(target, address);
    private const int LfanewOffset = 60;

    public ImageDosHeader(Target target, TargetPointer address)
    {
        Lfanew = target.ReadLittleEndian<int>(address + LfanewOffset);
    }
    public int Lfanew { get; init; }
}
