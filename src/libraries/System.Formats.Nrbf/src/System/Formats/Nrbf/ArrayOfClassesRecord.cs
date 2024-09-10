// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Formats.Nrbf.Utils;

namespace System.Formats.Nrbf;

internal sealed class ArrayOfClassesRecord : SZArrayRecord<SerializationRecord>
{
    private TypeName? _typeName;

    internal ArrayOfClassesRecord(ArrayInfo arrayInfo, MemberTypeInfo memberTypeInfo)
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
                do
                {
                    result[resultIndex++] = null;
                    nullCount--;
                }
                while (nullCount > 0);
            }
        }

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
