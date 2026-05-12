// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Data.Managed;

internal sealed class Lock : IData<Lock>
{
    private const string FullyQualifiedName = "System.Threading.Lock";

    public static TypeHandle TypeHandle(Target target)
        => target.Contracts.ManagedTypeSource.GetTypeHandle(FullyQualifiedName);

    static Lock IData<Lock>.Create(Target target, TargetPointer address) => new Lock(target, address);

    public Lock(Target target, TargetPointer address)
    {
        Target.TypeInfo typeInfo = target.Contracts.ManagedTypeSource.GetTypeInfo(FullyQualifiedName);
        TargetPointer dataAddress = address + target.GetTypeInfo(DataType.Object).Size!.Value;

        State = target.ReadField<uint>(dataAddress, typeInfo, "_state");
        OwningThreadId = target.ReadField<int>(dataAddress, typeInfo, "_owningThreadId");
        RecursionCount = target.ReadField<uint>(dataAddress, typeInfo, "_recursionCount");
    }

    public uint State { get; init; }
    public int OwningThreadId { get; init; }
    public uint RecursionCount { get; init; }
}
