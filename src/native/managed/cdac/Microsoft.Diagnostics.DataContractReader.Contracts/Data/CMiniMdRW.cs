// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.CMiniMdRW))]
internal sealed partial class CMiniMdRW : IData<CMiniMdRW>
{
    [FieldAddress] public TargetPointer Schema { get; }
    [Field] public uint TableCount { get; }
    [Field(UnderlyingBoolType = typeof(uint))] public bool All4ByteColumns { get; }
    [FieldAddress] public TargetPointer Tables { get; }
    [FieldAddress] public TargetPointer StringHeap { get; }
    [FieldAddress] public TargetPointer BlobHeap { get; }
    [FieldAddress] public TargetPointer UserStringHeap { get; }
    [FieldAddress] public TargetPointer GuidHeap { get; }
    public ImmutableArray<TargetPointer> TableSegments { get; private set; }

    [MemberNotNull(nameof(TableSegments))]
    partial void OnInit(Target target, TargetPointer address)
    {
        int tableCount = checked((int)TableCount);
        uint tableStride = target.GetTypeInfo(DataType.TableRW).Size
            ?? throw new InvalidOperationException("TableRW size is required to index the tables array.");

        var tableSegments = new TargetPointer[tableCount];
        for (int i = 0; i < tableCount; i++)
        {
            tableSegments[i] = Tables + (ulong)i * tableStride;
        }
        TableSegments = ImmutableArray.Create(tableSegments);
    }
}
