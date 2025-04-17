// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class LoaderAllocator : IData<LoaderAllocator>
{
    static LoaderAllocator IData<LoaderAllocator>.Create(Target target, TargetPointer address)
        => new LoaderAllocator(target, address);

    public LoaderAllocator(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.LoaderAllocator);

        ReferenceCount = target.Read<uint>(address + (ulong)type.Fields[nameof(ReferenceCount)].Offset);

    }

    public uint ReferenceCount { get; init; }

    public bool IsAlive => ReferenceCount != 0;
}
