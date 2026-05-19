// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Data.Managed;

/// <summary>Wraps a <c>System.Runtime.CompilerServices.ConditionalWeakTable&lt;TKey, TValue&gt;</c> instance.</summary>
internal sealed class ConditionalWeakTable : IData<ConditionalWeakTable>
{
    private const string FullyQualifiedName = "System.Runtime.CompilerServices.ConditionalWeakTable`2";

    public static TypeHandle TypeHandle(Target target)
        => target.Contracts.ManagedTypeSource.GetTypeHandle(FullyQualifiedName);

    static ConditionalWeakTable IData<ConditionalWeakTable>.Create(Target target, TargetPointer address)
        => new ConditionalWeakTable(target, address);

    public ConditionalWeakTable(Target target, TargetPointer address)
    {
        Target.TypeInfo typeInfo = target.Contracts.ManagedTypeSource.GetTypeInfo(FullyQualifiedName);
        TargetPointer dataAddress = address + target.GetTypeInfo(DataType.Object).Size!.Value;

        Container = target.ReadPointerField(dataAddress, typeInfo, "_container");
    }

    /// <summary>Pointer to the active <c>Container</c> object.</summary>
    public TargetPointer Container { get; init; }
}
