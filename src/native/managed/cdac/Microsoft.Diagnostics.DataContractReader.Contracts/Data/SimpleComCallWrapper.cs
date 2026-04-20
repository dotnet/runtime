// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class SimpleComCallWrapper : IData<SimpleComCallWrapper>
{
    static SimpleComCallWrapper IData<SimpleComCallWrapper>.Create(Target target, TargetPointer address) => new SimpleComCallWrapper(target, address);
    public SimpleComCallWrapper(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SimpleComCallWrapper);

        OuterIUnknown = target.ReadPointerField(address, type, nameof(OuterIUnknown));
        RefCount = target.ReadField<long>(address, type, nameof(RefCount));
        Flags = target.ReadField<uint>(address, type, nameof(Flags));
        MainWrapper = target.ReadPointerField(address, type, nameof(MainWrapper));
        VTablePtr = address + (ulong)type.Fields[nameof(VTablePtr)].Offset;
    }

    public TargetPointer OuterIUnknown { get; init; }
    public long RefCount { get; init; }
    public uint Flags { get; init; }
    public TargetPointer MainWrapper { get; init; }
    public TargetPointer VTablePtr { get; init; }
}
