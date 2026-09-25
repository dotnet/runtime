// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.SyncBlock))]
internal sealed partial class SyncBlock : IData<SyncBlock>
{
    [Field] public partial uint ThinLock { get; }
    [Field] public partial TargetPointer LinkNext { get; }
    [Field] public partial uint HashCode { get; }
    [CustomInit(nameof(InitInteropInfo))] public partial InteropSyncBlockInfo? InteropInfo { get; }
    [CustomInit(nameof(InitLock))] public partial ObjectHandle? Lock { get; }
    [CustomInit(nameof(InitEnCInfo))] public partial TargetPointer? EnCInfo { get; }

    [DataDescriptorDependency(nameof(InteropInfo), "pointer")]
    private partial InteropSyncBlockInfo? InitInteropInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SyncBlock);
        TargetPointer interopInfoPointer = target.ReadPointerField(address, type, nameof(InteropInfo));
        return interopInfoPointer != TargetPointer.Null
            ? target.ProcessedData.GetOrAdd<InteropSyncBlockInfo>(interopInfoPointer)
            : null;
    }

    [DataDescriptorDependency(nameof(Lock), "ObjectHandle")]
    private partial ObjectHandle? InitLock(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SyncBlock);
        ObjectHandle lockHandle = target.ReadDataField<ObjectHandle>(address, type, nameof(Lock));
        return lockHandle.Handle != TargetPointer.Null ? lockHandle : null;
    }

    [DataDescriptorDependency(nameof(EnCInfo), "pointer")]
    private partial TargetPointer? InitEnCInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SyncBlock);
        if (!type.Fields.ContainsKey(nameof(EnCInfo)))
            return null;

        TargetPointer encInfoPointer = target.ReadPointerField(address, type, nameof(EnCInfo));
        return encInfoPointer != TargetPointer.Null ? encInfoPointer : null;
    }
}
