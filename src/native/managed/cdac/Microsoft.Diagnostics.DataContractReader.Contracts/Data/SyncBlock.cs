// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.SyncBlock))]
internal sealed partial class SyncBlock : IData<SyncBlock>
{
    [Field] public partial uint ThinLock { get; }
    [Field] public partial TargetPointer LinkNext { get; }
    [Field] public partial uint HashCode { get; }

    [DataDescriptorDependency(nameof(InteropInfo), "pointer")]
    public InteropSyncBlockInfo? InteropInfo { get; private set; }

    [DataDescriptorDependency(nameof(Lock), "ObjectHandle")]
    public ObjectHandle? Lock { get; private set; }

    [DataDescriptorDependency(nameof(EnCInfo), "pointer")]
    public TargetPointer? EnCInfo { get; private set; }

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.SyncBlock);
        TargetPointer interopInfoPointer = target.ReadPointerField(address, type, nameof(InteropInfo));
        if (interopInfoPointer != TargetPointer.Null)
            InteropInfo = target.ProcessedData.GetOrAdd<InteropSyncBlockInfo>(interopInfoPointer);

        ObjectHandle lockHandle = target.ReadDataField<ObjectHandle>(address, type, nameof(Lock));
        if (lockHandle.Handle != TargetPointer.Null)
            Lock = lockHandle;

        if (type.Fields.ContainsKey(nameof(EnCInfo)))
        {
            TargetPointer encInfoPointer = target.ReadPointerField(address, type, nameof(EnCInfo));
            if (encInfoPointer != TargetPointer.Null)
                EnCInfo = encInfoPointer;
        }
    }
}
