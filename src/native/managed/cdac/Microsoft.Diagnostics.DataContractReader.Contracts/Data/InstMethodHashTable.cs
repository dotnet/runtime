// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class InstMethodHashTable : IData<InstMethodHashTable>
{
    private const ulong FLAG_MASK = 0x3ul;

    static InstMethodHashTable IData<InstMethodHashTable>.Create(Target target, TargetPointer address) => new InstMethodHashTable(target, address);
    public InstMethodHashTable(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InstMethodHashTable);

        DacEnumerableHash baseHashTable = new(target, address, type);

        List<Entry> entries = [];
        foreach (TargetPointer entry in baseHashTable.Entries)
        {
            TargetPointer methodDescPtr = target.ReadPointer(entry);
            entries.Add(new(methodDescPtr));
        }
        Entries = entries;
    }

    public IReadOnlyList<Entry> Entries { get; init; }

    public readonly struct Entry(TargetPointer value)
    {
        public TargetPointer MethodDesc { get; } = value & ~FLAG_MASK;
        public uint Flags { get; } = (uint)(value.Value & FLAG_MASK);
    }
}
