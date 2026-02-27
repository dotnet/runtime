// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class RealCodeHeader : IData<RealCodeHeader>
{
    static RealCodeHeader IData<RealCodeHeader>.Create(Target target, TargetPointer address)
        => new RealCodeHeader(target, address);

    public RealCodeHeader(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RealCodeHeader);
        MethodDesc = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodDesc)].Offset);
        DebugInfo = target.ReadPointer(address + (ulong)type.Fields[nameof(DebugInfo)].Offset);
        EHInfo = target.ReadPointer(address + (ulong)type.Fields[nameof(EHInfo)].Offset);
        GCInfo = target.ReadPointer(address + (ulong)type.Fields[nameof(GCInfo)].Offset);
        NumUnwindInfos = target.Read<uint>(address + (ulong)type.Fields[nameof(NumUnwindInfos)].Offset);
        UnwindInfos = address + (ulong)type.Fields[nameof(UnwindInfos)].Offset;
    }

    public TargetPointer MethodDesc { get; }
    public TargetPointer DebugInfo { get; }
    public TargetPointer EHInfo { get; }
    public TargetPointer GCInfo { get; }
    public uint NumUnwindInfos { get; }
    public TargetPointer UnwindInfos { get; }
}
