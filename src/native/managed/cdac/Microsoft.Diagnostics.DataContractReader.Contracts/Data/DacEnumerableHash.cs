// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Data;

/// <summary>
/// Parses hash tables that are implemented by DacEnumerableHash defined in dacenumerablehash.h
/// Requires the following datadescriptor fields on the passed type:
/// Buckets - Pointer to array of VolatileEntry pointers
/// Count - Count of elements
/// VolatileEntryValue - Offset of the value in the VolatileEntry struct
/// VolatileEntryNextEntry - Offset of the next entry pointer in the VolatileEntry struct
/// </summary>
internal sealed class DacEnumerableHash
{
    private const int SLOT_LENGTH = 0;
    private const int SKIP_SPECIAL_SLOTS = 3;
    private const int END_SENTINEL = 0x1;

    private readonly Target _target;
    private readonly Target.TypeInfo _type;

    public DacEnumerableHash(Target target, TargetPointer address, Target.TypeInfo type)
    {
        // init fields
        _target = target;
        _type = type;

        Buckets = _target.ReadPointer(address + (ulong)_type.Fields[nameof(Buckets)].Offset);
        Count = _target.Read<uint>(address + (ulong)_type.Fields[nameof(Count)].Offset);

        // read items in the hash table
        uint length = GetLength();

        List<TargetPointer> entries = [];
        for (int i = 0; i < length; i++)
        {
            // indexes 0, 1, 2 have special purposes. buckets start at SKIP_SPECIAL_SLOTS
            int bucketOffset = i + SKIP_SPECIAL_SLOTS;
            TargetPointer chainElement = _target.ReadPointer(Buckets + (ulong)(bucketOffset * _target.PointerSize));
            List<TargetPointer> elements = ReadChain(chainElement);
            entries.AddRange(elements);
        }

        Debug.Assert(Count == entries.Count);

        Entries = entries;
    }

    public TargetPointer Buckets { get; init; }
    public uint Count { get; init; }

    public IReadOnlyList<TargetPointer> Entries { get; init; }

    internal sealed class VolatileEntry
    {
        public VolatileEntry(Target target, TargetPointer address, Target.TypeInfo type)
        {
            // offsets are stored on the parent type
            VolatileEntryValue = address + (ulong)type.Fields[nameof(VolatileEntryValue)].Offset;
            VolatileEntryNextEntry = target.ReadPointer(address + (ulong)type.Fields[nameof(VolatileEntryNextEntry)].Offset);
        }

        public TargetPointer VolatileEntryValue { get; init; }
        public TargetPointer VolatileEntryNextEntry { get; init; }
    }

    private uint GetLength()
    {
        // First pointer is a size_t length
        TargetPointer length = _target.ReadPointer(Buckets + (ulong)(SLOT_LENGTH * _target.PointerSize));
        return (uint)length;
    }


    private static bool IsEndSentinel(TargetPointer value)
    {
        return ((ulong)value & END_SENTINEL) == END_SENTINEL;
    }

    private List<TargetPointer> ReadChain(TargetPointer chainElement)
    {
        List<TargetPointer> elements = [];

        while (!IsEndSentinel(chainElement))
        {
            VolatileEntry volatileEntry = new(_target, chainElement, _type);
            elements.Add(volatileEntry.VolatileEntryValue);

            chainElement = volatileEntry.VolatileEntryNextEntry;
        }

        return elements;
    }
}
