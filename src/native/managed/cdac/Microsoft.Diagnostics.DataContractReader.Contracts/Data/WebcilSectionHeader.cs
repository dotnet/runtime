// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class WebcilSectionHeader : IData<WebcilSectionHeader>
{
    static WebcilSectionHeader IData<WebcilSectionHeader>.Create(Target target, TargetPointer address) => new WebcilSectionHeader(target, address);
    public WebcilSectionHeader(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.WebcilSectionHeader);

        VirtualSize = target.Read<uint>(address + (ulong)type.Fields[nameof(VirtualSize)].Offset);
        VirtualAddress = target.Read<uint>(address + (ulong)type.Fields[nameof(VirtualAddress)].Offset);
        SizeOfRawData = target.Read<uint>(address + (ulong)type.Fields[nameof(SizeOfRawData)].Offset);
        PointerToRawData = target.Read<uint>(address + (ulong)type.Fields[nameof(PointerToRawData)].Offset);
    }

    public uint VirtualSize { get; init; }
    public uint VirtualAddress { get; init; }
    public uint SizeOfRawData { get; init; }
    public uint PointerToRawData { get; init; }
}
