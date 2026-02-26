// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class RCWCleanupList : IData<RCWCleanupList>
{
    static RCWCleanupList IData<RCWCleanupList>.Create(Target target, TargetPointer address)
        => new RCWCleanupList(target, address);

    public RCWCleanupList(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.RCWCleanupList);

        FirstBucket = target.ReadPointer(address + (ulong)type.Fields[nameof(FirstBucket)].Offset);
    }

    public TargetPointer FirstBucket { get; init; }
}
