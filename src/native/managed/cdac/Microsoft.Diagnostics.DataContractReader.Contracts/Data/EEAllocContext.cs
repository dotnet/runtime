// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class EEAllocContext : IData<EEAllocContext>
{
    static EEAllocContext IData<EEAllocContext>.Create(Target target, TargetPointer address)
        => new EEAllocContext(target, address);

    public EEAllocContext(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.EEAllocContext);
        GCAllocationContext = target.ProcessedData.GetOrAdd<GCAllocContext>(address + (ulong)type.Fields[nameof(GCAllocationContext)].Offset);
    }

    public GCAllocContext GCAllocationContext { get; init; }
}
