// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class InstMethodHashTable : IData<InstMethodHashTable>
{
    static InstMethodHashTable IData<InstMethodHashTable>.Create(Target target, TargetPointer address) => new InstMethodHashTable(target, address);
    public InstMethodHashTable(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InstMethodHashTable);

        DacEnumerableHash baseHashTable = new(target, address, type);

        List<InstMethodHashTableEntry> entries = [];
        foreach (TargetPointer entry in baseHashTable.Entries)
        {
            TargetPointer methodDescPtr = target.ReadPointer(entry);
            InstMethodHashTableEntry instMethodHashTableEntry = new()
            {
                MethodDesc = methodDescPtr.Value & ~0x3ul,
                Flags = (InstMethodHashTableFlags)(methodDescPtr.Value & 0x3ul)
            };
            entries.Add(instMethodHashTableEntry);
        }
        Entries = entries;
    }

    public IReadOnlyList<InstMethodHashTableEntry> Entries { get; init; }

    public readonly struct InstMethodHashTableEntry
    {
        public TargetPointer MethodDesc { get; init; }
        public InstMethodHashTableFlags Flags { get; init; }
    }

    [Flags]
    public enum InstMethodHashTableFlags
    {
        UnboxingStub = 0x1,
        RequiresInstArg = 0x2,
    }
}
