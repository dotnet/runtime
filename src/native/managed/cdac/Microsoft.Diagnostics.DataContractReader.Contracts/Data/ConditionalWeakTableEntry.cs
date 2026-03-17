// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ConditionalWeakTableEntry : IData<ConditionalWeakTableEntry>
{
    static ConditionalWeakTableEntry IData<ConditionalWeakTableEntry>.Create(Target target, TargetPointer address)
        => new ConditionalWeakTableEntry(target, address);

    public ConditionalWeakTableEntry(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ConditionalWeakTableEntry);
        HashCode = target.Read<int>(address + (ulong)type.Fields[nameof(HashCode)].Offset);
        Next = target.Read<int>(address + (ulong)type.Fields[nameof(Next)].Offset);
        DepHnd = target.ReadPointer(address + (ulong)type.Fields[nameof(DepHnd)].Offset);
    }

    public int HashCode { get; init; }
    public int Next { get; init; }
    public TargetPointer DepHnd { get; init; }
}
