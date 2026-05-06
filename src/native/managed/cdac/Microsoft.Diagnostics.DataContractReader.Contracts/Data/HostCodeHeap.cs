// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class HostCodeHeap : IData<HostCodeHeap>
{
    static HostCodeHeap IData<HostCodeHeap>.Create(Target target, TargetPointer address)
        => new HostCodeHeap(target, address);

    public HostCodeHeap(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.HostCodeHeap);
        BaseAddress = target.ReadPointerField(address, type, nameof(BaseAddress));
        CurrentAddress = target.ReadPointerField(address, type, nameof(CurrentAddress));
    }

    public TargetPointer BaseAddress { get; init; }
    public TargetPointer CurrentAddress { get; init; }
}
