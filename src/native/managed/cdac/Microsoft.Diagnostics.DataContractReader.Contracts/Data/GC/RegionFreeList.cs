// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class RegionFreeList : IData<RegionFreeList>
{
    static RegionFreeList IData<RegionFreeList>.Create(Target target, TargetPointer address) => new RegionFreeList(target, address);
    public RegionFreeList(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RegionFreeList);

        HeadFreeRegion = target.ReadPointer(address + (ulong)type.Fields[nameof(HeadFreeRegion)].Offset);
    }

    public TargetPointer HeadFreeRegion { get; }
}
