// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Formats.Nrbf.Utils;
using System.Diagnostics;

namespace System.Formats.Nrbf;

// This library tries to minimize the number of concepts the users need to learn to use it.
// Since SZArrays are most common, it provides an SZArrayRecord<T> abstraction.
// Every other array (jagged, multi-dimensional etc) is represented using ArrayRecord.
// The goal of this class is to let the users use SZArrayRecord<SerializationRecord> abstraction.
internal sealed class SZArrayOfRecords : SZArrayRecord<SerializationRecord>
{
    private TypeName? _typeName;

    internal SZArrayOfRecords(ArrayInfo arrayInfo, MemberTypeInfo memberTypeInfo)
        : base(arrayInfo)
    {
        MemberTypeInfo = memberTypeInfo;
        Records = [];
    }

    public override SerializationRecordType RecordType => SerializationRecordType.BinaryArray;

    internal List<SerializationRecord> Records { get; }

    private MemberTypeInfo MemberTypeInfo { get; }

    public override TypeName TypeName
        => _typeName ??= MemberTypeInfo.GetArrayTypeName(ArrayInfo);

    /// <inheritdoc/>
    public override SerializationRecord?[] GetArray(bool allowNulls = true)
        => (SerializationRecord?[])(allowNulls ? _arrayNullsAllowed ??= ToArray(true) : _arrayNullsNotAllowed ??= ToArray(false));

    private SerializationRecord?[] ToArray(bool allowNulls)
    {
        SerializationRecord?[] result = new SerializationRecord?[Length];

        int resultIndex = 0;
        foreach (SerializationRecord record in Records)
        {
            SerializationRecord actual = record is MemberReferenceRecord referenceRecord
                ? referenceRecord.GetReferencedRecord()
                : record;

            if (actual is not NullsRecord nullsRecord)
            {
                result[resultIndex++] = actual;
            }
            else
            {
                if (!allowNulls)
                {
                    ThrowHelper.ThrowArrayContainedNulls();
                }

                int nullCount = nullsRecord.NullCount;
                Debug.Assert(nullCount > 0, "All implementations of NullsRecord are expected to return a positive value for NullCount.");
                do
                {
                    result[resultIndex++] = null;
                    nullCount--;
                }
                while (nullCount > 0);
            }
        }

        Debug.Assert(resultIndex == result.Length, "We should have traversed the entirety of the newly created array.");

        return result;
    }

    private protected override void AddValue(object value) => Records.Add((SerializationRecord)value);

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetAllowedRecordType()
    {
        (AllowedRecordTypes allowed, PrimitiveType primitiveType) = MemberTypeInfo.GetNextAllowedRecordType(0);

        if (allowed != AllowedRecordTypes.None)
        {
            // It's an array, it can also contain multiple nulls
            return (allowed | AllowedRecordTypes.Nulls, primitiveType);
        }

        return (allowed, primitiveType);
    }
}
