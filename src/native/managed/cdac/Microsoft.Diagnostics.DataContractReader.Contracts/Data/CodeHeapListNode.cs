// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class CodeHeapListNode : IData<CodeHeapListNode>
{
    static CodeHeapListNode IData<CodeHeapListNode>.Create(Target target, TargetPointer address)
        => new CodeHeapListNode(target, address);

    public CodeHeapListNode(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.CodeHeapListNode);
        Next = target.ReadPointerField(address, type, nameof(Next));
        StartAddress = target.ReadPointerField(address, type, nameof(StartAddress));
        EndAddress = target.ReadPointerField(address, type, nameof(EndAddress));
        MapBase = target.ReadPointerField(address, type, nameof(MapBase));
        HeaderMap = target.ReadPointerField(address, type, nameof(HeaderMap));
        Heap = target.ReadPointerField(address, type, nameof(Heap));
    }

    public TargetPointer Next { get; init; }
    public TargetPointer StartAddress { get; init; }
    public TargetPointer EndAddress { get; init; }

    public TargetPointer MapBase { get; init; }

    public TargetPointer HeaderMap { get; init; }

    public TargetPointer Heap { get; init; }
}
