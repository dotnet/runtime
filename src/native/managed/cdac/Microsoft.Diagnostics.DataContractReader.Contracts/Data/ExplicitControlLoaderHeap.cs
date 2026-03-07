// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ExplicitControlLoaderHeap : IData<ExplicitControlLoaderHeap>
{
    static ExplicitControlLoaderHeap IData<ExplicitControlLoaderHeap>.Create(Target target, TargetPointer address)
        => new ExplicitControlLoaderHeap(target, address);

    public ExplicitControlLoaderHeap(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ExplicitControlLoaderHeap);

        FirstBlock = target.ReadPointer(address + (ulong)type.Fields[nameof(FirstBlock)].Offset);
    }

    public TargetPointer FirstBlock { get; init; }
}
