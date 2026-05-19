// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class VirtualCallStubManager : IData<VirtualCallStubManager>
{
    static VirtualCallStubManager IData<VirtualCallStubManager>.Create(Target target, TargetPointer address)
        => new VirtualCallStubManager(target, address);

    public VirtualCallStubManager(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.VirtualCallStubManager);

        IndcellHeap = target.ReadPointerField(address, type, nameof(IndcellHeap));

        if (type.Fields.ContainsKey(nameof(CacheEntryHeap)))
            CacheEntryHeap = target.ReadPointerField(address, type, nameof(CacheEntryHeap));
    }

    public TargetPointer IndcellHeap { get; init; }
    public TargetPointer? CacheEntryHeap { get; init; }
}
