// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class CallCountingManager : IData<CallCountingManager>
{
    static CallCountingManager IData<CallCountingManager>.Create(Target target, TargetPointer address)
        => new CallCountingManager(target, address);

    public CallCountingManager(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.CallCountingManager);

        CallCountingHash = address + (ulong)type.Fields[nameof(CallCountingHash)].Offset;
    }

    public TargetPointer CallCountingHash { get; init; }
}
