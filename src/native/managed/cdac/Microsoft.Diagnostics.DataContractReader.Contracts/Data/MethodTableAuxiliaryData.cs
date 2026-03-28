// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodTableAuxiliaryData : IData<MethodTableAuxiliaryData>
{
    static MethodTableAuxiliaryData IData<MethodTableAuxiliaryData>.Create(Target target, TargetPointer address) => new MethodTableAuxiliaryData(target, address);

    private MethodTableAuxiliaryData(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.MethodTableAuxiliaryData);

        LoaderModule = target.ReadPointer(address + (ulong)type.Fields[nameof(LoaderModule)].Offset);
        OffsetToNonVirtualSlots = target.Read<short>(address + (ulong)type.Fields[nameof(OffsetToNonVirtualSlots)].Offset);
        Flags = target.Read<uint>(address + (ulong)type.Fields[nameof(Flags)].Offset);

    }

    public TargetPointer LoaderModule { get; init; }
    public short OffsetToNonVirtualSlots { get; init; }
    public uint Flags { get; init; }
}
