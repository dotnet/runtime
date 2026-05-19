// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodTableAuxiliaryData : IData<MethodTableAuxiliaryData>
{
    static MethodTableAuxiliaryData IData<MethodTableAuxiliaryData>.Create(Target target, TargetPointer address) => new MethodTableAuxiliaryData(target, address);

    private MethodTableAuxiliaryData(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.MethodTableAuxiliaryData);

        LoaderModule = target.ReadPointerField(address, type, nameof(LoaderModule));
        OffsetToNonVirtualSlots = target.ReadField<short>(address, type, nameof(OffsetToNonVirtualSlots));
        Flags = target.ReadField<uint>(address, type, nameof(Flags));

    }

    public TargetPointer LoaderModule { get; init; }
    public short OffsetToNonVirtualSlots { get; init; }
    public uint Flags { get; init; }
}
