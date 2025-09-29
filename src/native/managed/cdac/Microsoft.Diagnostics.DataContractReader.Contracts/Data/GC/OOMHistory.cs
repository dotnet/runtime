// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class OomHistory : IData<OomHistory>
{
    static OomHistory IData<OomHistory>.Create(Target target, TargetPointer address) => new OomHistory(target, address);
    public OomHistory(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.OomHistory);

        Reason = target.Read<int>(address + (ulong)type.Fields[nameof(Reason)].Offset);
        AllocSize = target.ReadNUInt(address + (ulong)type.Fields[nameof(AllocSize)].Offset);
        Reserved = target.ReadPointer(address + (ulong)type.Fields[nameof(Reserved)].Offset);
        Allocated = target.ReadPointer(address + (ulong)type.Fields[nameof(Allocated)].Offset);
        GcIndex = target.ReadNUInt(address + (ulong)type.Fields[nameof(GcIndex)].Offset);
        Fgm = target.Read<int>(address + (ulong)type.Fields[nameof(Fgm)].Offset);
        Size = target.ReadNUInt(address + (ulong)type.Fields[nameof(Size)].Offset);
        AvailablePagefileMb = target.ReadNUInt(address + (ulong)type.Fields[nameof(AvailablePagefileMb)].Offset);
        LohP = target.Read<uint>(address + (ulong)type.Fields[nameof(LohP)].Offset);
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
