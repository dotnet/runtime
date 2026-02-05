// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class PEImageLayout : IData<PEImageLayout>
{
    static PEImageLayout IData<PEImageLayout>.Create(Target target, TargetPointer address) => new PEImageLayout(target, address);
    public PEImageLayout(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.PEImageLayout);

        Base = target.ReadPointer(address + (ulong)type.Fields[nameof(Base)].Offset);
        Size = target.Read<uint>(address + (ulong)type.Fields[nameof(Size)].Offset);
        Flags = target.Read<uint>(address + (ulong)type.Fields[nameof(Flags)].Offset);
    }

    public TargetPointer Base { get; init; }
    public uint Size { get; init; }
    public uint Flags { get; init; }
}
