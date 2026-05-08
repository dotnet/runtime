// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class OomHistory : IData<OomHistory>
{
    static OomHistory IData<OomHistory>.Create(Target target, TargetPointer address) => new OomHistory(target, address);
    public OomHistory(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.OomHistory);

        Reason = target.ReadField<int>(address, type, nameof(Reason));
        AllocSize = target.ReadNUIntField(address, type, nameof(AllocSize));
        Reserved = target.ReadPointerField(address, type, nameof(Reserved));
        Allocated = target.ReadPointerField(address, type, nameof(Allocated));
        GcIndex = target.ReadNUIntField(address, type, nameof(GcIndex));
        Fgm = target.ReadField<int>(address, type, nameof(Fgm));
        Size = target.ReadNUIntField(address, type, nameof(Size));
        AvailablePagefileMb = target.ReadNUIntField(address, type, nameof(AvailablePagefileMb));
        LohP = target.ReadField<uint>(address, type, nameof(LohP));
    }

    public int Reason { get; }
    public TargetNUInt AllocSize { get; }
    public TargetPointer Reserved { get; }
    public TargetPointer Allocated { get; }
    public TargetNUInt GcIndex { get; }
    public int Fgm { get; }
    public TargetNUInt Size { get; }
    public TargetNUInt AvailablePagefileMb { get; }
    public uint LohP { get; }
}
