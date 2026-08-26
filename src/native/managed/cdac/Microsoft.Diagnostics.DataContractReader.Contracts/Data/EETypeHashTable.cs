// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.EETypeHashTable))]
internal sealed partial class EETypeHashTable : IData<EETypeHashTable>
{
    private const ulong FLAG_MASK = 0x1ul;
    [CustomInit(nameof(InitEntries))] public partial IReadOnlyList<Entry> Entries { get; }

    [DataDescriptorDependency("Buckets", "pointer")]
    [DataDescriptorDependency("Count", "uint32")]
    [DataDescriptorDependency("VolatileEntryValue", "pointer")]
    [DataDescriptorDependency("VolatileEntryNextEntry", "pointer")]
    private partial IReadOnlyList<Entry> InitEntries(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.EETypeHashTable);
        DacEnumerableHash baseHashTable = new(target, address, type);

        List<Entry> entries = [];
        foreach (TargetPointer entry in baseHashTable.Entries)
        {
            TargetPointer typeHandle = target.ReadPointer(entry);
            entries.Add(new(typeHandle));
        }
        return entries;
    }

    public readonly struct Entry(TargetPointer value)
    {
        public TargetPointer TypeHandle { get; } = value & ~FLAG_MASK;
        public uint Flags { get; } = (uint)(value.Value & FLAG_MASK);
    }
}
