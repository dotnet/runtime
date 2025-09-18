// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Generation : IData<Generation>
{
    static Generation IData<Generation>.Create(Target target, TargetPointer address) => new Generation(target, address);
    public Generation(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Generation);

        AllocationContext = target.ProcessedData.GetOrAdd<GCAllocContext>(address + (ulong)type.Fields[nameof(AllocationContext)].Offset);
        StartSegment = target.ReadPointer(address + (ulong)type.Fields[nameof(StartSegment)].Offset);

        // Fields only exist segment GC builds
        if (type.Fields.ContainsKey(nameof(AllocationStart)))
            AllocationStart = target.ReadPointer(address + (ulong)type.Fields[nameof(AllocationStart)].Offset);
    }

    public GCAllocContext AllocationContext { get; }
    public TargetPointer StartSegment { get; }
    public TargetPointer? AllocationStart { get; }
}
