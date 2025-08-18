// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class DynamicILBlobTable : IData<DynamicILBlobTable>
{
    static DynamicILBlobTable IData<DynamicILBlobTable>.Create(Target target, TargetPointer address)
        => new DynamicILBlobTable(target, address);

    public DynamicILBlobTable(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.DynamicILBlobTable);
        HashTable = new SHash<uint, DynamicILBlobEntry>(target, address, type, new DynamicILBlobTraits());
        List<DynamicILBlobEntry> entries = new List<DynamicILBlobEntry>();
        for (int i = 0; i < HashTable.TableSize; i++)
        {
            TargetPointer entryAddress = HashTable.Table + (ulong)(i * HashTable.EntrySize);
            DynamicILBlobEntry entry = new DynamicILBlobEntry(target, entryAddress);
            entries.Add(entry);
        }
        HashTable.Entries = entries;
    }

    internal sealed class DynamicILBlobTraits : ITraits<uint, DynamicILBlobEntry>
    {
        public uint GetKey(DynamicILBlobEntry entry)
        {
            return entry.EntryMethodToken;
        }
        public bool Equals(uint left, uint right)
        {
            return left == right;
        }
        public uint Hash(uint key)
        {
            return key;
        }
        public bool IsNull(DynamicILBlobEntry entry)
        {
            return entry.EntryMethodToken == 0;
        }
        public DynamicILBlobEntry Null()
        {
            return new DynamicILBlobEntry();
        }
    }
    public SHash<uint, DynamicILBlobEntry> HashTable { get; init; }
}

internal sealed class DynamicILBlobEntry
{
    public DynamicILBlobEntry(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.DynamicILBlobTable);
        EntryMethodToken = target.Read<uint>(address + (ulong)type.Fields[nameof(EntryMethodToken)].Offset);
        EntryIL = target.ReadPointer(address + (ulong)type.Fields[nameof(EntryIL)].Offset);
    }

    public DynamicILBlobEntry()
    {
        EntryMethodToken = 0;
        EntryIL = TargetPointer.Null;
    }

    public uint EntryMethodToken { get; }
    public TargetPointer EntryIL { get; }
}
