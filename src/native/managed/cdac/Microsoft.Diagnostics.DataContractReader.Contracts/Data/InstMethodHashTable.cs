// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.InstMethodHashTable))]
internal sealed partial class InstMethodHashTable : IData<InstMethodHashTable>
{
    private const ulong FLAG_MASK = 0x3ul;

    public IReadOnlyList<Entry> Entries { get; private set; }

    [MemberNotNull(nameof(Entries))]
    partial void OnInit(Target target, TargetPointer address)
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

    public readonly struct Entry(TargetPointer value)
    {
        public TargetPointer MethodDesc { get; } = value & ~FLAG_MASK;
        public uint Flags { get; } = (uint)(value.Value & FLAG_MASK);
    }
}
