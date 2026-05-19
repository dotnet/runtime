// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Data.Managed;

/// <summary>Wraps a <c>ConditionalWeakTable&lt;,&gt;+Container</c> instance.</summary>
internal sealed class ConditionalWeakTableContainer : IData<ConditionalWeakTableContainer>
{
    private const string FullyQualifiedName = "System.Runtime.CompilerServices.ConditionalWeakTable`2+Container";

    public static TypeHandle TypeHandle(Target target)
        => target.Contracts.ManagedTypeSource.GetTypeHandle(FullyQualifiedName);

    static ConditionalWeakTableContainer IData<ConditionalWeakTableContainer>.Create(Target target, TargetPointer address)
        => new ConditionalWeakTableContainer(target, address);

    public ConditionalWeakTableContainer(Target target, TargetPointer address)
    {
        Target.TypeInfo typeInfo = target.Contracts.ManagedTypeSource.GetTypeInfo(FullyQualifiedName);
        TargetPointer dataAddress = address + target.GetTypeInfo(DataType.Object).Size!.Value;

        Buckets = target.ReadPointerField(dataAddress, typeInfo, "_buckets");
        Entries = target.ReadPointerField(dataAddress, typeInfo, "_entries");
    }

    /// <summary>Pointer to the <c>int[]</c> hash buckets array.</summary>
    public TargetPointer Buckets { get; init; }

    /// <summary>Pointer to the <c>Entry[]</c> entries array.</summary>
    public TargetPointer Entries { get; init; }
}
