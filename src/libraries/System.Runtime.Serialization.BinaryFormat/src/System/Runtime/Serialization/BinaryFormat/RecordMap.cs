// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization.BinaryFormat;

internal sealed class RecordMap : IReadOnlyDictionary<int, SerializationRecord>
{
    private readonly Dictionary<int, SerializationRecord> _map = new(CollisionResistantInt32Comparer.Instance);

    public IEnumerable<int> Keys => _map.Keys;

    public IEnumerable<SerializationRecord> Values => _map.Values;

    public int Count => _map.Count;

    public SerializationRecord this[int objectId] => _map[objectId];

    public bool ContainsKey(int key) => _map.ContainsKey(key);

    public bool TryGetValue(int key, [MaybeNullWhen(false)] out SerializationRecord value) => _map.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<int, SerializationRecord>> GetEnumerator() => _map.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _map.GetEnumerator();

    internal void Add(SerializationRecord record)
    {
        // From https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/0a192be0-58a1-41d0-8a54-9c91db0ab7bf:
        // "If the ObjectId is not referenced by any MemberReference in the serialization stream,
        // then the ObjectId SHOULD be positive, but MAY be negative."
        if (record.ObjectId != SerializationRecord.NoId)
        {
            if (record.ObjectId < 0)
            {
                // Negative record Ids should never be referenced. Duplicate negative ids can be
                // exported by the writer. The root object Id can be negative.
                _map[record.ObjectId] = record;
            }
            else
            {
#if NET
                if (_map.TryAdd(record.ObjectId, record))
                {
                    return;
                }
#else
                if (!_map.ContainsKey(record.ObjectId))
                {
                    _map.Add(record.ObjectId, record);
                    return;
                }
#endif
                throw new SerializationException(SR.Format(SR.Serialization_DuplicateSerializationRecordId, record.ObjectId));
            }
        }
    }

    internal SerializationRecord GetRootRecord(SerializedStreamHeaderRecord header)
    {
        SerializationRecord rootRecord = _map[header.RootId];
        if (rootRecord is SystemClassWithMembersAndTypesRecord systemClass)
        {
            // update the record map, so it's visible also to those who access it via Id
            _map[header.RootId] = rootRecord = systemClass.TryToMapToUserFriendly();
        }

        return rootRecord;
    }

    // keys (32-bit integer ids) are payload-provided so we need a collision-resistant comparer
    private sealed class CollisionResistantInt32Comparer : IEqualityComparer<int>
    {
        internal static CollisionResistantInt32Comparer Instance { get; } = new();

        private CollisionResistantInt32Comparer() { }

        public bool Equals(int x, int y) => x == y;

        public int GetHashCode(int obj)
        {
#if NET
            Span<int> integers = new(ref obj);
#else
            Span<int> integers = stackalloc int[1] { obj };
#endif
            return (int)XxHash32.HashToUInt32(MemoryMarshal.AsBytes(integers));
        }
    }
}
