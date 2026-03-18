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
        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
        StartAddress = target.ReadPointer(address + (ulong)type.Fields[nameof(StartAddress)].Offset);
        EndAddress = target.ReadPointer(address + (ulong)type.Fields[nameof(EndAddress)].Offset);
        MapBase = target.ReadPointer(address + (ulong)type.Fields[nameof(MapBase)].Offset);
        HeaderMap = target.ReadPointer(address + (ulong)type.Fields[nameof(HeaderMap)].Offset);
    }

    public TargetPointer Next { get; init; }
    public TargetPointer StartAddress { get; init; }
    public TargetPointer EndAddress { get; init; }

    public TargetPointer MapBase { get; init; }

    public TargetPointer HeaderMap { get; init; }
}
