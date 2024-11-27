// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;
using System.Formats.Nrbf.Utils;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace System.Formats.Nrbf;

/// <summary>
/// Represents an array of arrays.
/// </summary>
/// <remarks>
/// BinaryArray records are described in <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/9c62c928-db4e-43ca-aeba-146256ef67c2">[MS-NRBF] 2.4.3.1</see>.
/// </remarks>
internal sealed class JaggedArrayRecord : ArrayRecord
{
    private readonly MemberTypeInfo _memberTypeInfo;
    private readonly int[] _lengths;
    private readonly List<SerializationRecord> _records;
    private readonly AllowedRecordTypes _allowedRecordTypes;
    private TypeName? _typeName;

    internal JaggedArrayRecord(ArrayInfo arrayInfo, MemberTypeInfo memberTypeInfo, int[] lengths)
        : base(arrayInfo)
    {
        _memberTypeInfo = memberTypeInfo;
        _lengths = lengths;
        _records = [];
        _allowedRecordTypes = memberTypeInfo.GetNextAllowedRecordType(0).allowed;

        Debug.Assert(TypeName.GetElementType().IsArray, "Jagged arrays are required.");
    }

    public override SerializationRecordType RecordType => SerializationRecordType.BinaryArray;

    public override ReadOnlySpan<int> Lengths => _lengths;

    public override TypeName TypeName => _typeName ??= _memberTypeInfo.GetArrayTypeName(ArrayInfo);

    [RequiresDynamicCode("May call Array.CreateInstance().")]
    private protected override Array Deserialize(Type arrayType, bool allowNulls)
    {
        // This method returns arrays of ArrayRecords.
        Array array = _lengths.Length switch
        {
            1 => new ArrayRecord[_lengths[0]],
            2 => new ArrayRecord[_lengths[0], _lengths[1]],
            _ => Array.CreateInstance(typeof(ArrayRecord), _lengths)
        };

        Populate(_records, array, _lengths, AllowedRecordTypes.Arrays, allowNulls);

        return array;
    }

    private protected override void AddValue(object value) => _records.Add((SerializationRecord)value);

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetAllowedRecordType()
        => (_allowedRecordTypes, default);
}
