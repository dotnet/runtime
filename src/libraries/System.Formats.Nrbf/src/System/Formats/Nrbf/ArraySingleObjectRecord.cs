// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Formats.Nrbf.Utils;

namespace System.Formats.Nrbf;

/// <summary>
/// Represents a single dimensional array of <see cref="object" />.
/// </summary>
/// <remarks>
/// ArraySingleObject records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/982b2f50-6367-402a-aaf2-44ee96e2a5e0">[MS-NRBF] 2.4.3.2</see>.
/// </remarks>
internal sealed class ArraySingleObjectRecord : SZArrayRecord<object?>
{
    private static TypeName? s_typeName;

    private ArraySingleObjectRecord(ArrayInfo arrayInfo) : base(arrayInfo) => Records = [];

    public override SerializationRecordType RecordType => SerializationRecordType.ArraySingleObject;

    public override TypeName TypeName
        => s_typeName ??= TypeName.Parse(("System.Object[], " + TypeNameExtensions.CoreLibAssemblyName).AsSpan());

    private List<SerializationRecord> Records { get; }

    /// <inheritdoc/>
    public override object?[] GetArray(bool allowNulls = true)
        => (object?[])(allowNulls ? _arrayNullsAllowed ??= ToArray(true) : _arrayNullsNotAllowed ??= ToArray(false));

    private object?[] ToArray(bool allowNulls)
    {
        object?[] values = new object?[Length];

        for (int recordIndex = 0, valueIndex = 0; recordIndex < Records.Count; recordIndex++)
        {
            SerializationRecord record = Records[recordIndex];

            int nullCount = record is NullsRecord nullsRecord ? nullsRecord.NullCount : 0;
            if (nullCount == 0)
            {
                values[valueIndex++] = record is MemberReferenceRecord referenceRecord && referenceRecord.Reference.Equals(Id)
                    ? values // a reference to self, and a way to get StackOverflow exception ;)
                    : record.GetValue();
                continue;
            }

            if (!allowNulls)
            {
                ThrowHelper.ThrowArrayContainedNulls();
            }

            do
            {
                values[valueIndex++] = null;
                nullCount--;
            }
            while (nullCount > 0);
        }

        return values;
    }

    internal static ArraySingleObjectRecord Decode(BinaryReader reader)
        => new ArraySingleObjectRecord(ArrayInfo.Decode(reader));

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetAllowedRecordType()
    {
        // An array of objects can contain any Object or multiple nulls.
        const AllowedRecordTypes Allowed = AllowedRecordTypes.AnyObject | AllowedRecordTypes.Nulls;

        return (Allowed, default);
    }

    private protected override void AddValue(object value) => Records.Add((SerializationRecord)value);
}
