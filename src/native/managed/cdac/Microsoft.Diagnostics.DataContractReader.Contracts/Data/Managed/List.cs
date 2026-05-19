// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Data.Managed;

/// <summary>Wraps a <c>System.Collections.Generic.List&lt;T&gt;</c> instance.</summary>
internal sealed class List : IData<List>
{
    private const string FullyQualifiedName = "System.Collections.Generic.List`1";

    public static TypeHandle TypeHandle(Target target)
        => target.Contracts.ManagedTypeSource.GetTypeHandle(FullyQualifiedName);

    static List IData<List>.Create(Target target, TargetPointer address) => new List(target, address);

    public List(Target target, TargetPointer address)
    {
        Target.TypeInfo typeInfo = target.Contracts.ManagedTypeSource.GetTypeInfo(FullyQualifiedName);
        TargetPointer dataAddress = address + target.GetTypeInfo(DataType.Object).Size!.Value;

        Items = target.ReadPointerField(dataAddress, typeInfo, "_items");
        Size = target.ReadField<int>(dataAddress, typeInfo, "_size");
    }

    /// <summary>Pointer to the backing <c>T[]</c> array.</summary>
    public TargetPointer Items { get; init; }

    /// <summary>Logical element count.</summary>
    public int Size { get; init; }
}
