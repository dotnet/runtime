// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Formats.Nrbf.Utils;

namespace System.Formats.Nrbf;

/// <summary>
/// Represents a single dimensional array of <see langword="string" />.
/// </summary>
/// <remarks>
/// ArraySingleString records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/3d98fd60-d2b4-448a-ac0b-3cd8dea41f9d">[MS-NRBF] 2.4.3.4</see>.
/// </remarks>
internal sealed class ArraySingleStringRecord : SZArrayRecord<string?>
{
    private static TypeName? s_typeName;

    private ArraySingleStringRecord(ArrayInfo arrayInfo) : base(arrayInfo) => Records = [];

    public override SerializationRecordType RecordType => SerializationRecordType.ArraySingleString;

    /// <inheritdoc />
    public override TypeName TypeName
        => s_typeName ??= TypeName.Parse(("System.String[], " + TypeNameExtensions.CoreLibAssemblyName).AsSpan());

    private List<SerializationRecord> Records { get; }

    internal static ArraySingleStringRecord Decode(BinaryReader reader)
        => new(ArrayInfo.Decode(reader));

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetAllowedRecordType()
    {
        // An array of string can consist of string(s), null(s) and reference(s) to string(s).
        const AllowedRecordTypes AllowedTypes = AllowedRecordTypes.BinaryObjectString
            | AllowedRecordTypes.Nulls | AllowedRecordTypes.MemberReference;

        return (AllowedTypes, default);
    }

    private protected override void AddValue(object value) => Records.Add((SerializationRecord)value);

    /// <inheritdoc/>
    public override string?[] GetArray(bool allowNulls = true)
        => (string?[])(allowNulls ? _arrayNullsAllowed ??= ToArray(true) : _arrayNullsNotAllowed ??= ToArray(false));

    private string?[] ToArray(bool allowNulls)
    {
        string?[] values = new string?[Length];

        for (int recordIndex = 0, valueIndex = 0; recordIndex < Records.Count; recordIndex++)
        {
            SerializationRecord record = Records[recordIndex];

            if (record is MemberReferenceRecord memberReference)
            {
                record = memberReference.GetReferencedRecord();

                if (record is not BinaryObjectStringRecord)
                {
                    ThrowHelper.ThrowInvalidReference();
                }
            }

            if (record is BinaryObjectStringRecord stringRecord)
            {
                values[valueIndex++] = stringRecord.Value;
                continue;
            }

            if (!allowNulls)
            {
                ThrowHelper.ThrowArrayContainedNulls();
            }

            int nullCount = ((NullsRecord)record).NullCount;
            do
            {
                values[valueIndex++] = null;
                nullCount--;
            }
            while (nullCount > 0);
        }

        return values;
    }
}
