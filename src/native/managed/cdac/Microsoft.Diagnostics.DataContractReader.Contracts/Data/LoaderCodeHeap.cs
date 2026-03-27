// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class LoaderCodeHeap : IData<LoaderCodeHeap>
{
    static LoaderCodeHeap IData<LoaderCodeHeap>.Create(Target target, TargetPointer address)
        => new LoaderCodeHeap(target, address);

    public LoaderCodeHeap(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.LoaderCodeHeap);
        LoaderHeap = address + (ulong)type.Fields[nameof(LoaderHeap)].Offset;
    }

    // Address of the embedded ExplicitControlLoaderHeap within this LoaderCodeHeap object.
    public TargetPointer LoaderHeap { get; init; }
}
