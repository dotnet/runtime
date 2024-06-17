// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Nrbf.Utils;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Formats.Nrbf;

internal sealed class RecordMap : IReadOnlyDictionary<SerializationRecordId, SerializationRecord>
{
    private readonly Dictionary<SerializationRecordId, SerializationRecord> _map = new();

    public IEnumerable<SerializationRecordId> Keys => _map.Keys;

    public IEnumerable<SerializationRecord> Values => _map.Values;

    public int Count => _map.Count;

    public SerializationRecord this[SerializationRecordId objectId] => _map[objectId];

    internal int UnresolvedReferences { get; private set; }

    public bool ContainsKey(SerializationRecordId key) => _map.ContainsKey(key);

    public bool TryGetValue(SerializationRecordId key, [MaybeNullWhen(false)] out SerializationRecord value) => _map.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<SerializationRecordId, SerializationRecord>> GetEnumerator() => _map.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _map.GetEnumerator();

    internal void Add(SerializationRecord record, bool isReferencedRecord)
    {
        if (isReferencedRecord)
        {
            if (record.Id._id <= 0)
            {
                // Negative record Ids should never be referenced.
                // 0 is simply illegal Id for such records.
                ThrowHelper.ThrowInvalidReference();
            }
            else if (!_map.TryGetValue(record.Id, out SerializationRecord? stored) || stored is not MemberReferenceRecord memberReferenceRecord)
            {
                // The id was either unexpected or there was no reference stored for it.
                ThrowHelper.ThrowForUnexpectedRecordType((byte)record.RecordType);
            }
            else if (((uint)memberReferenceRecord.ReferencedRecordType & (1u << (byte)record.RecordType)) == 0)
            {
                // We expected a reference to a record of a different type.
                ThrowHelper.ThrowInvalidReference();
            }

            _map[record.Id] = record;
            UnresolvedReferences--;
            return;
        }

        // From https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/0a192be0-58a1-41d0-8a54-9c91db0ab7bf:
        // "If the ObjectId is not referenced by any MemberReference in the serialization stream,
        // then the ObjectId SHOULD be positive, but MAY be negative."
        if (!record.Id.Equals(SerializationRecordId.NoId))
        {
            if (record.Id._id < 0)
            {
                // Negative ids can be exported by the writer. The root object Id can be negative.
                _map[record.Id] = record;
            }
            else if (!TryAdd(record.Id, record))
            {
                throw new SerializationException(SR.Format(SR.Serialization_DuplicateSerializationRecordId, record.Id._id));
            }
        }
        else if (record.RecordType == SerializationRecordType.MemberReference)
        {
            MemberReferenceRecord memberReferenceRecord = (MemberReferenceRecord)record;

            if (TryAdd(memberReferenceRecord.Reference, memberReferenceRecord))
            {
                UnresolvedReferences++;
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

    private bool TryAdd(SerializationRecordId id, SerializationRecord record)
    {
#if NET
        return _map.TryAdd(id, record);
#else
        if (!_map.ContainsKey(id))
        {
            _map.Add(id, record);
            return true;
        }
        return false;
#endif
    }
}
