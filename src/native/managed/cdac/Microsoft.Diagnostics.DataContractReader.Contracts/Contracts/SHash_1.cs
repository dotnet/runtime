// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly partial struct SHash_1 : ISHash
{
    internal readonly Target _target;
    public SHash_1(Target target)
    {
        _target = target;
    }

    private class SHash<TKey, TEntry> : ISHash<TKey, TEntry> where TEntry : IData<TEntry>
    {
        public TargetPointer Table { get; set; }
        public uint TableSize { get; set; }
        public uint EntrySize { get; set; }
        public List<TEntry>? Entries { get; set; }
        public ITraits<TKey, TEntry>? Traits { get; set; }
    }

    ISHash<TKey, TEntry> ISHash.CreateSHash<TKey, TEntry>(Target target, TargetPointer address, Target.TypeInfo type, ITraits<TKey, TEntry> traits)
    {
        TargetPointer table = target.ReadPointer(address + (ulong)type.Fields[nameof(SHash<TKey, TEntry>.Table)].Offset);
        uint tableSize = target.Read<uint>(address + (ulong)type.Fields[nameof(SHash<TKey, TEntry>.TableSize)].Offset);
        uint entrySize = type.Size ?? 0;
        List<TEntry> entries = [];
        for (int i = 0; i < tableSize; i++)
        {
            TargetPointer entryAddress = table + (ulong)(i * entrySize);
            TEntry entry = target.ProcessedData.GetOrAdd<TEntry>(entryAddress);
            entries.Add(entry);
        }
        return new SHash<TKey, TEntry>
        {
            Table = table,
            TableSize = tableSize,
            EntrySize = entrySize,
            Traits = traits,
            Entries = entries
        };
    }
    TEntry ISHash.LookupSHash<TKey, TEntry>(ISHash<TKey, TEntry> hashTable, TKey key)
    {
        SHash<TKey, TEntry> shashTable = (SHash<TKey, TEntry>)hashTable;
        if (shashTable.TableSize == 0)
            return shashTable.Traits!.Null();

        uint hash = shashTable.Traits!.Hash(key);
        uint index = hash % shashTable.TableSize;
        uint increment = 0;
        while (true)
        {
            TEntry current = shashTable.Entries![(int)index];
            if (shashTable.Traits.IsNull(current))
                return shashTable.Traits.Null();
            // we don't support the removal of entries
            if (shashTable.Traits.Equals(key, shashTable.Traits.GetKey(current)))
                return current;

            if (increment == 0)
                increment = (hash % (shashTable.TableSize - 1)) + 1;

            index += increment;
            if (index >= shashTable.TableSize)
                index -= shashTable.TableSize;
        }
    }
}
