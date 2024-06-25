// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Formats.Nrbf.Utils;

namespace System.Formats.Nrbf;

internal sealed class ArrayOfClassesRecord : SZArrayRecord<ClassRecord>
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
    public override ClassRecord?[] GetArray(bool allowNulls = true)
        => (ClassRecord?[])(allowNulls ? _arrayNullsAllowed ??= ToArray(true) : _arrayNullsNotAllowed ??= ToArray(false));

    private ClassRecord?[] ToArray(bool allowNulls)
    {
        ClassRecord?[] result = new ClassRecord?[Length];

        int resultIndex = 0;
        foreach (SerializationRecord record in Records)
        {
            SerializationRecord actual = record is MemberReferenceRecord referenceRecord
                ? referenceRecord.GetReferencedRecord()
                : record;

            if (actual is ClassRecord classRecord)
            {
                result[resultIndex++] = classRecord;
            }
            else
            {
                if (!allowNulls)
                {
                    ThrowHelper.ThrowArrayContainedNulls();
                }

                int nullCount = ((NullsRecord)actual).NullCount;
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
