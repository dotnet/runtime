// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

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
    private const int SLOT_NEXT = 1;
    private const int SKIP_SPECIAL_SLOTS = 3;
    private const int END_SENTINEL = 0x1;

    private readonly Target _target;
    private readonly Target.TypeInfo _type;

    public DacEnumerableHash(Target target, TargetPointer address, Target.TypeInfo type)
    {
        _target = target;
        _type = type;

        Buckets = _target.ReadPointerField(address, _type, nameof(Buckets));
        Count = _target.ReadField<uint>(address, _type, nameof(Count));
    }

    public TargetPointer Buckets { get; init; }
    public uint Count { get; init; }

    public IEnumerable<TargetPointer> Entries
    {
        get
        {
            uint length = GetLength(Buckets);
            for (uint i = 0; i < length; i++)
            {
                TargetPointer chainElement = _target.ReadPointer(Buckets + ((ulong)i + SKIP_SPECIAL_SLOTS) * (ulong)_target.PointerSize);
                foreach (TargetPointer entry in ReadChain(chainElement))
                    yield return entry;
            }
        }
    }

    public IEnumerable<TargetPointer> Lookup(uint hash)
    {
        if (Count == 0)
            yield break;

        // A stopped target can retain both bucket arrays while a resize is in progress.
        // See BaseFindFirstEntryByHashCore in src/coreclr/vm/dacenumerablehash.inl.
        for (TargetPointer buckets = Buckets; buckets != TargetPointer.Null;
            buckets = _target.ReadPointer(buckets + SLOT_NEXT * (ulong)_target.PointerSize))
        {
            uint length = GetLength(buckets);
            if (length == 0)
                continue;
            ulong bucketOffset = ((ulong)(hash % length) + SKIP_SPECIAL_SLOTS) * (ulong)_target.PointerSize;
            TargetPointer chainElement = _target.ReadPointer(buckets + bucketOffset);
            foreach (TargetPointer entry in ReadChain(chainElement))
                yield return entry;
        }
    }

    internal readonly struct VolatileEntry
    {
        public VolatileEntry(Target target, TargetPointer address, Target.TypeInfo type)
        {
            // offsets are stored on the parent type
            VolatileEntryValue = address + (ulong)type.Fields[nameof(VolatileEntryValue)].Offset;
            VolatileEntryNextEntry = target.ReadPointerField(address, type, nameof(VolatileEntryNextEntry));
        }

        public TargetPointer VolatileEntryValue { get; init; }
        public TargetPointer VolatileEntryNextEntry { get; init; }
    }

    private uint GetLength(TargetPointer buckets)
    {
        // First pointer is a size_t length
        TargetPointer length = _target.ReadPointer(buckets + (ulong)(SLOT_LENGTH * _target.PointerSize));
        return (uint)length;
    }


    private static bool IsEndSentinel(TargetPointer value)
    {
        return ((ulong)value & END_SENTINEL) == END_SENTINEL;
    }

    private IEnumerable<TargetPointer> ReadChain(TargetPointer chainElement)
    {
        while (!IsEndSentinel(chainElement))
        {
            VolatileEntry volatileEntry = new(_target, chainElement, _type);
            yield return volatileEntry.VolatileEntryValue;

            chainElement = volatileEntry.VolatileEntryNextEntry;
        }
    }
}
