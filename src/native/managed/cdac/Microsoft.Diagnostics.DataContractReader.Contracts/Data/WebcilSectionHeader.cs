// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class WebcilSectionHeader : IData<WebcilSectionHeader>
{
    static WebcilSectionHeader IData<WebcilSectionHeader>.Create(Target target, TargetPointer address) => new WebcilSectionHeader(target, address);
    public WebcilSectionHeader(Target target, TargetPointer address)
    {
        VirtualSize = target.Read<uint>(address + 0);
        VirtualAddress = target.Read<uint>(address + 4);
        SizeOfRawData = target.Read<uint>(address + 8);
        PointerToRawData = target.Read<uint>(address + 12);
    }

    public uint VirtualSize { get; init; }
    public uint VirtualAddress { get; init; }
    public uint SizeOfRawData { get; init; }
    public uint PointerToRawData { get; init; }
}
