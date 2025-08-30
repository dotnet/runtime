// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.VisualBasic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

public interface ITraits<TKey, TEntry>
{
    abstract TKey GetKey(TEntry entry);
    abstract bool Equals(TKey left, TKey right);
    abstract uint Hash(TKey key);
    abstract bool IsNull(TEntry entry);
    abstract TEntry Null();
}
internal sealed class SHash<TKey, TEntry>
{
    public SHash(Target target, TargetPointer address, Target.TypeInfo type, ITraits<TKey, TEntry> traits)
    {
        Table = target.ReadPointer(address + (ulong)type.Fields[nameof(Table)].Offset);
        TableSize = target.Read<uint>(address + (ulong)type.Fields[nameof(TableSize)].Offset);
        EntrySize = target.Read<uint>(address + (ulong)type.Fields[nameof(EntrySize)].Offset);
        Traits = traits;
    }
    public TargetPointer Table { get; init; }
    public uint TableSize { get; init; }
    public uint EntrySize { get; init; }
    public List<TEntry>? Entries { get; set; }
    public ITraits<TKey, TEntry> Traits { get; init; }
}
