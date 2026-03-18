// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class TLSIndex : IData<TLSIndex>
{
    static TLSIndex IData<TLSIndex>.Create(Target target, TargetPointer address) => new TLSIndex(target, address);
    public TLSIndex(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TLSIndex);
        TLSIndexRawIndex = target.Read<uint>(address + (ulong)type.Fields[nameof(TLSIndexRawIndex)].Offset);
        IndexOffset = (int)(TLSIndexRawIndex & 0xFFFFFF);
        IndexType = (int)(TLSIndexRawIndex >> 24);
        IsAllocated = (TLSIndexRawIndex != 0xFFFFFFFF);
    }
    public uint TLSIndexRawIndex { get; init; }
    public int IndexOffset { get; init; }
    public int IndexType { get; init; }
    public bool IsAllocated { get; init; }
}
