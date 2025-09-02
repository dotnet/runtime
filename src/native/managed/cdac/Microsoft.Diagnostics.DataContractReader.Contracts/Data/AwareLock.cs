// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class AwareLock : IData<AwareLock>
{
    static AwareLock IData<AwareLock>.Create(Target target, TargetPointer address)
        => new AwareLock(target, address);

    public AwareLock(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.AwareLock);

        LockState = target.Read<uint>(address + (ulong)type.Fields[nameof(LockState)].Offset);
        RecursionLevel = target.Read<uint>(address + (ulong)type.Fields[nameof(RecursionLevel)].Offset);
        HoldingThreadId = target.Read<uint>(address + (ulong)type.Fields[nameof(HoldingThreadId)].Offset);
    }

    public uint LockState { get; }
    public uint RecursionLevel { get; }
    public uint HoldingThreadId { get; }
}
