// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class NativeCodeVersionNode : IData<NativeCodeVersionNode>
{
    static NativeCodeVersionNode IData<NativeCodeVersionNode>.Create(Target target, TargetPointer address) => new NativeCodeVersionNode(target, address);
    public NativeCodeVersionNode(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.NativeCodeVersionNode);

        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
        MethodDesc = target.ReadPointer(address + (ulong)type.Fields[nameof(MethodDesc)].Offset);
        NativeCode = target.ReadCodePointer(address + (ulong)type.Fields[nameof(NativeCode)].Offset);
        Flags = target.Read<uint>(address + (ulong)type.Fields[nameof(Flags)].Offset);
        ILVersionId = target.ReadNUInt(address + (ulong)type.Fields[nameof(ILVersionId)].Offset);
        OptimizationTier = target.Read<uint>(address + (ulong)type.Fields[nameof(OptimizationTier)].Offset);
        NativeId = target.Read<uint>(address + (ulong)type.Fields[nameof(NativeId)].Offset);
        if (type.Fields.ContainsKey(nameof(GCCoverageInfo)))
        {
            GCCoverageInfo = target.ReadPointer(address + (ulong)type.Fields[nameof(GCCoverageInfo)].Offset);
        }
        Address = address;
    }

    public TargetPointer Next { get; init; }
    public TargetPointer MethodDesc { get; init; }

    public TargetCodePointer NativeCode { get; init; }
    public uint Flags { get; init; }
    public TargetNUInt ILVersionId { get; init; }
    public uint OptimizationTier { get; init; }
    public uint NativeId { get; init; }

    public TargetPointer? GCCoverageInfo { get; init; }
    public TargetPointer Address { get; init; }
}
