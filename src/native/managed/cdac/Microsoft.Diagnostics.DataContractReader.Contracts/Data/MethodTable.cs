// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodTable : IData<MethodTable>
{
    static MethodTable IData<MethodTable>.Create(Target target, TargetPointer address) => new MethodTable(target, address);
    public MethodTable(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.MethodTable);

        MTFlags = target.ReadField<uint>(address, type, nameof(MTFlags));
        BaseSize = target.ReadField<uint>(address, type, nameof(BaseSize));
        MTFlags2 = target.ReadField<uint>(address, type, nameof(MTFlags2));
        EEClassOrCanonMT = target.ReadPointerField(address, type, nameof(EEClassOrCanonMT));
        Module = target.ReadPointerField(address, type, nameof(Module));
        ParentMethodTable = target.ReadPointerField(address, type, nameof(ParentMethodTable));
        NumInterfaces = target.ReadField<ushort>(address, type, nameof(NumInterfaces));
        NumVirtuals = target.ReadField<ushort>(address, type, nameof(NumVirtuals));
        PerInstInfo = target.ReadPointerField(address, type, nameof(PerInstInfo));
        AuxiliaryData = target.ReadPointerField(address, type, nameof(AuxiliaryData));
    }

    public uint MTFlags { get; init; }
    public uint BaseSize { get; init; }
    public uint MTFlags2 { get; init; }
    public TargetPointer EEClassOrCanonMT { get; init; }
    public TargetPointer Module { get; init; }
    public TargetPointer ParentMethodTable { get; init; }
    public TargetPointer PerInstInfo { get; init; }
    public ushort NumInterfaces { get; init; }
    public ushort NumVirtuals { get; init; }
    public TargetPointer AuxiliaryData { get; init; }
}
