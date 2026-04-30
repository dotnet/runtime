// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class CodeHeap : IData<CodeHeap>
{
    static CodeHeap IData<CodeHeap>.Create(Target target, TargetPointer address)
        => new CodeHeap(target, address);

    public CodeHeap(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.CodeHeap);
        HeapType = target.ReadField<byte>(address, type, nameof(HeapType));
    }

    public byte HeapType { get; init; }
}
