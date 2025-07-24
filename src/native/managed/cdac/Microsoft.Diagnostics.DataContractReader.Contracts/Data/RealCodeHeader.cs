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
        GCInfo = target.ReadPointer(address + (ulong)type.Fields[nameof(GCInfo)].Offset);
        NumUnwindInfos = target.Read<uint>(address + (ulong)type.Fields[nameof(NumUnwindInfos)].Offset);
        UnwindInfos = address + (ulong)type.Fields[nameof(UnwindInfos)].Offset;
    }

    public TargetPointer MethodDesc { get; init; }
    public TargetPointer GCInfo { get; init; }
    public uint NumUnwindInfos { get; init; }
    public TargetPointer UnwindInfos { get; init; }
}
