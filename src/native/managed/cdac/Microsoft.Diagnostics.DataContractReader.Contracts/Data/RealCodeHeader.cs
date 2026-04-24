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
        MethodDesc = target.ReadPointerField(address, type, nameof(MethodDesc));
        DebugInfo = target.ReadPointerField(address, type, nameof(DebugInfo));
        GCInfo = target.ReadPointerField(address, type, nameof(GCInfo));
        NumUnwindInfos = target.ReadField<uint>(address, type, nameof(NumUnwindInfos));
        UnwindInfos = address + (ulong)type.Fields[nameof(UnwindInfos)].Offset;
        EHInfo = target.ReadPointerField(address, type, nameof(EHInfo));
    }

    public TargetPointer MethodDesc { get; }
    public TargetPointer DebugInfo { get; }
    public TargetPointer EHInfo { get; }
    public TargetPointer GCInfo { get; }
    public uint NumUnwindInfos { get; }
    public TargetPointer UnwindInfos { get; }
}
