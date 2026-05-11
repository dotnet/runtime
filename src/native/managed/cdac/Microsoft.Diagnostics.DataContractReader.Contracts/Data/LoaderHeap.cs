// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class LoaderHeap : IData<LoaderHeap>
{
    static LoaderHeap IData<LoaderHeap>.Create(Target target, TargetPointer address)
        => new LoaderHeap(target, address);

    public LoaderHeap(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.LoaderHeap);

        FirstBlock = target.ReadPointer(address + (ulong)type.Fields[nameof(FirstBlock)].Offset);
    }

    public TargetPointer FirstBlock { get; init; }
}
