// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.SyncTableEntry))]
internal sealed partial class SyncTableEntry : IData<SyncTableEntry>
{
    [CustomInit(nameof(InitSyncBlock))] public partial SyncBlock? SyncBlock { get; }
    [CustomInit(nameof(InitObject))] public partial Object? Object { get; }

    [DataDescriptorDependency(nameof(SyncBlock), "pointer")]
    private partial SyncBlock? InitSyncBlock(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SyncTableEntry);
        TargetPointer syncBlockPointer = target.ReadPointerField(address, type, nameof(SyncBlock));
        return syncBlockPointer != TargetPointer.Null
            ? target.ProcessedData.GetOrAdd<SyncBlock>(syncBlockPointer)
            : null;
    }

    [DataDescriptorDependency(nameof(Object), "pointer")]
    private partial Object? InitObject(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SyncTableEntry);
        TargetPointer objectPointer = target.ReadPointerField(address, type, nameof(Object));
        // Defensive check: if the lowest bit is set, this is a free sync block entry and the pointer is not valid.
        return objectPointer != TargetPointer.Null && (objectPointer & 1) == 0
            ? target.ProcessedData.GetOrAdd<Object>(objectPointer)
            : null;
    }
}
