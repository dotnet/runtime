// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class GCAllocContext : IData<GCAllocContext>
{
    static GCAllocContext IData<GCAllocContext>.Create(Target target, TargetPointer address)
        => new GCAllocContext(target, address);

    public GCAllocContext(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.GCAllocContext);
        Pointer = target.ReadPointer(address + (ulong)type.Fields[nameof(Pointer)].Offset);
        Limit = target.ReadPointer(address + (ulong)type.Fields[nameof(Limit)].Offset);
    }

    public TargetPointer Pointer { get; init; }
    public TargetPointer Limit { get; init; }
}
