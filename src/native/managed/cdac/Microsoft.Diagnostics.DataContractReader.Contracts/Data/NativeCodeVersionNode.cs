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

        Next = target.ReadPointerField(address, type, nameof(Next));
        MethodDesc = target.ReadPointerField(address, type, nameof(MethodDesc));
        NativeCode = target.ReadCodePointerField(address, type, nameof(NativeCode));
        Flags = target.ReadField<uint>(address, type, nameof(Flags));
        ILVersionId = target.ReadNUIntField(address, type, nameof(ILVersionId));
        if (type.Fields.ContainsKey(nameof(GCCoverageInfo)))
        {
            GCCoverageInfo = target.ReadPointerField(address, type, nameof(GCCoverageInfo));
        }
        OptimizationTier = target.ReadField<uint>(address, type, nameof(OptimizationTier));
    }

    public TargetPointer Next { get; init; }
    public TargetPointer MethodDesc { get; init; }

    public TargetCodePointer NativeCode { get; init; }
    public uint Flags { get; init; }
    public TargetNUInt ILVersionId { get; init; }

    public TargetPointer? GCCoverageInfo { get; init; }
    public uint OptimizationTier { get; init; }
}
