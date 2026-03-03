// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class LoaderHeapBlock : IData<LoaderHeapBlock>
{
    static LoaderHeapBlock IData<LoaderHeapBlock>.Create(Target target, TargetPointer address)
        => new LoaderHeapBlock(target, address);

    public LoaderHeapBlock(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.LoaderHeapBlock);

        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
        VirtualAddress = target.ReadPointer(address + (ulong)type.Fields[nameof(VirtualAddress)].Offset);
        VirtualSize = target.ReadNUInt(address + (ulong)type.Fields[nameof(VirtualSize)].Offset);
    }

    public TargetPointer Next { get; init; }
    public TargetPointer VirtualAddress { get; init; }
    public TargetNUInt VirtualSize { get; init; }
}
