// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class LoaderAllocator : IData<LoaderAllocator>
{
    static LoaderAllocator IData<LoaderAllocator>.Create(Target target, TargetPointer address) => new LoaderAllocator(target, address);
    public LoaderAllocator(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.LoaderAllocator);

        IsCollectible = target.Read<byte>(address + (ulong)type.Fields[nameof(IsCollectible)].Offset);
    }

    public byte IsCollectible { get; init; }
}
