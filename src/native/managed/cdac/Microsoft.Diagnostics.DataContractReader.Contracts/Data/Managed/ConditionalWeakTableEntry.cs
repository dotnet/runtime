// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Data.Managed;

/// <summary>
/// Wraps a single <c>ConditionalWeakTable&lt;,&gt;+Entry</c> value-type slot embedded
/// inline in the <c>_entries</c> array. The slot has no object header.
/// </summary>
internal sealed class ConditionalWeakTableEntry : IData<ConditionalWeakTableEntry>
{
    private const string FullyQualifiedName = "System.Runtime.CompilerServices.ConditionalWeakTable`2+Entry";

    public static TypeHandle TypeHandle(Target target)
        => target.Contracts.ManagedTypeSource.GetTypeHandle(FullyQualifiedName);

    static ConditionalWeakTableEntry IData<ConditionalWeakTableEntry>.Create(Target target, TargetPointer address)
        => new ConditionalWeakTableEntry(target, address);

    public ConditionalWeakTableEntry(Target target, TargetPointer address)
    {
        // Value-type slot — no object header; the address IS the data address.
        Target.TypeInfo typeInfo = target.Contracts.ManagedTypeSource.GetTypeInfo(FullyQualifiedName);

        HashCode = target.ReadField<int>(address, typeInfo, "HashCode");
        Next = target.ReadField<int>(address, typeInfo, "Next");
        DepHndAddress = address + (uint)typeInfo.Fields["depHnd"].Offset;
    }

    public int HashCode { get; init; }
    public int Next { get; init; }
    public TargetPointer DepHndAddress { get; init; }
}
