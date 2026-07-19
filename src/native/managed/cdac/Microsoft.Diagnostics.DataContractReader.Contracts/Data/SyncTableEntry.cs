// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.SyncTableEntry))]
internal sealed partial class SyncTableEntry : IData<SyncTableEntry>
{
    public SyncBlock? SyncBlock { get; private set; }
    public Object? Object { get; private set; }

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SyncTableEntry);

        TargetPointer syncBlockPointer = target.ReadPointerField(address, type, nameof(SyncBlock));
        if (syncBlockPointer != TargetPointer.Null)
            SyncBlock = target.ProcessedData.GetOrAdd<SyncBlock>(syncBlockPointer);

        TargetPointer objectPointer = target.ReadPointerField(address, type, nameof(Object));
        if (objectPointer != TargetPointer.Null && (objectPointer & 1) == 0) // Defensive check: if the lowest bit is set, this is a free sync block entry and the pointer is not valid.
            Object = target.ProcessedData.GetOrAdd<Object>(objectPointer);
    }
}
