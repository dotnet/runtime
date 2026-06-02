// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.SyncBlock))]
internal sealed partial class SyncBlock : IData<SyncBlock>
{
    [Field] public uint ThinLock { get; }
    [Field] public TargetPointer LinkNext { get; }
    [Field] public uint HashCode { get; }

    public InteropSyncBlockInfo? InteropInfo { get; private set; }
    public ObjectHandle? Lock { get; private set; }

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SyncBlock);
        TargetPointer interopInfoPointer = target.ReadPointerField(address, type, nameof(InteropInfo));
        if (interopInfoPointer != TargetPointer.Null)
            InteropInfo = target.ProcessedData.GetOrAdd<InteropSyncBlockInfo>(interopInfoPointer);

        ObjectHandle lockHandle = target.ReadDataField<ObjectHandle>(address, type, nameof(Lock));
        if (lockHandle.Handle != TargetPointer.Null)
            Lock = lockHandle;
    }
}
