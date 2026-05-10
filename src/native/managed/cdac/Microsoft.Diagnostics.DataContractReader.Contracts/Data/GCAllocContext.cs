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
        Pointer = target.ReadPointerField(address, type, nameof(Pointer));
        Limit = target.ReadPointerField(address, type, nameof(Limit));
        AllocBytes = target.ReadField<long>(address, type, nameof(AllocBytes));
        AllocBytesLoh = target.ReadField<long>(address, type, nameof(AllocBytesLoh));
    }

    public TargetPointer Pointer { get; init; }
    public TargetPointer Limit { get; init; }
    public long AllocBytes { get; init; }
    public long AllocBytesLoh { get; init; }
}
