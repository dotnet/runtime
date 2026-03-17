// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ConditionalWeakTableObject : IData<ConditionalWeakTableObject>
{
    static ConditionalWeakTableObject IData<ConditionalWeakTableObject>.Create(Target target, TargetPointer address)
        => new ConditionalWeakTableObject(target, address);

    public ConditionalWeakTableObject(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ConditionalWeakTableObject);
        Container = target.ReadPointer(address + (ulong)type.Fields[nameof(Container)].Offset);
    }

    public TargetPointer Container { get; init; }
}
