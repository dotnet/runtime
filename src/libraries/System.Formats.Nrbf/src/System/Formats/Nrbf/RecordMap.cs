// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        switch (record.RecordType)
        {
            case SerializationRecordType.SerializedStreamHeader:
            case SerializationRecordType.ObjectNull:
            case SerializationRecordType.MessageEnd:
            case SerializationRecordType.ObjectNullMultiple256:
            case SerializationRecordType.ObjectNullMultiple:
            case SerializationRecordType.MemberPrimitiveTyped when record.Id.IsDefault:
                // These records have no Id and don't need any verification.
                Debug.Assert(record.Id.IsDefault);
                return;
            case SerializationRecordType.BinaryLibrary:
                if (!TryAdd(record.Id, record))
                {
                    ThrowHelper.ThrowDuplicateSerializationRecordId(record.Id);
                }
                return;
            case SerializationRecordType.MemberReference:
                MemberReferenceRecord memberReferenceRecord = (MemberReferenceRecord)record;

                if (_map.TryGetValue(memberReferenceRecord.Reference, out SerializationRecord? stored))
                {
                    if (stored.RecordType != SerializationRecordType.MemberReference)
                    {
                        // When reference was stored, we have persisted the allowed record type.
                        // Now is the time to check if the provided record matches expectations.
                        memberReferenceRecord.VerifyReferencedRecordType(stored);
                    }
                }
                else
                {
                    // We store the reference now and when the record is provided we are going to perform type check.
                    _map.Add(memberReferenceRecord.Reference, record);
                    UnresolvedReferences++;
                }
                return;
            default:
                break;
        }

        Debug.Assert(!record.Id.IsDefault);

        // From https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/0a192be0-58a1-41d0-8a54-9c91db0ab7bf:
        // "If the ObjectId is not referenced by any MemberReference in the serialization stream,
        // then the ObjectId SHOULD be positive, but MAY be negative."
        if (isReferencedRecord)
        {
            if (record.Id._id < 0)
            {
                // Negative record Ids should never be referenced.
                ThrowHelper.ThrowInvalidReference();
            }
            else if (!_map.TryGetValue(record.Id, out SerializationRecord? stored) || stored is not MemberReferenceRecord memberReferenceRecord)
            {
                // The id was either unexpected or there was no reference stored for it.
                ThrowHelper.ThrowForUnexpectedRecordType((byte)record.RecordType);
            }
            else
            {
                memberReferenceRecord.VerifyReferencedRecordType(record);
            }

            _map[record.Id] = record;
            UnresolvedReferences--;
        }
        else
        {
            if (record.Id._id < 0)
            {
                // Negative ids can be exported by the writer. The root object Id can be negative.
                _map[record.Id] = record;
            }
            else if (!TryAdd(record.Id, record))
            {
                ThrowHelper.ThrowDuplicateSerializationRecordId(record.Id);
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
