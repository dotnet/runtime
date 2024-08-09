// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class RuntimeThreadLocals : IData<RuntimeThreadLocals>
{
    static RuntimeThreadLocals IData<RuntimeThreadLocals>.Create(Target target, TargetPointer address)
        => new RuntimeThreadLocals(target, address);

    public RuntimeThreadLocals(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RuntimeThreadLocals);

        // TODO: Before the GCAllocationContext, there is a pointer to the "combined limit" used to randomly sample allocations.
        //       How to get the size of a pointer here so the offset should be correct?
        //ex:
        //  AllocContext = target.ProcessedData.GetOrAdd<GCAllocContext>(
        //      address +
        //      (ulong)type.Fields[nameof(AllocContext)].Offset
        //      + sizeof(pointer)
        //      );
        AllocContext = target.ProcessedData.GetOrAdd<GCAllocContext>(address + (ulong)type.Fields[nameof(AllocContext)].Offset);
    }

    public GCAllocContext AllocContext { get; init; }
}
