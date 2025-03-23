// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Formats.Nrbf.Utils;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace System.Formats.Nrbf;

internal sealed class RectangularArrayRecord : ArrayRecord
{
    private readonly Type _elementType;
    private readonly int[] _lengths;
    private readonly List<SerializationRecord> _records;
    private readonly AllowedRecordTypes _allowedRecordTypes;
    private readonly MemberTypeInfo _memberTypeInfo;
    private TypeName? _typeName;

    internal RectangularArrayRecord(Type elementType, ArrayInfo arrayInfo, MemberTypeInfo memberTypeInfo, int[] lengths) : base(arrayInfo)
    {
        _elementType = elementType;
        _lengths = lengths;
        _memberTypeInfo = memberTypeInfo;
        _records = new List<SerializationRecord>(Math.Min(4, arrayInfo.GetSZArrayLength()));
        _allowedRecordTypes = memberTypeInfo.GetNextAllowedRecordType(0).allowed;

        Debug.Assert(elementType == typeof(string) || elementType == typeof(SerializationRecord));
        Debug.Assert(!TypeName.GetElementType().IsArray, "Use JaggedArrayRecord instead.");
    }

    public override SerializationRecordType RecordType => SerializationRecordType.BinaryArray;

    public override ReadOnlySpan<int> Lengths => _lengths.AsSpan();

    public override TypeName TypeName
        => _typeName ??= _memberTypeInfo.GetArrayTypeName(ArrayInfo);

    [RequiresDynamicCode("May call Array.CreateInstance() and Type.MakeArrayType().")]
    private protected override Array Deserialize(Type arrayType, bool allowNulls)
    {
        bool storeStrings = _elementType == typeof(string);

        // We can not deserialize non-string types.
        // This method returns arrays of SerializationRecord for arrays of complex types.
        Array result =
#if NET9_0_OR_GREATER
            storeStrings
                ? Array.CreateInstanceFromArrayType(arrayType, _lengths)
                : Array.CreateInstance(_elementType, _lengths);
#else
            Array.CreateInstance(_elementType, _lengths);
#endif

        AllowedRecordTypes allowedRecordTypes = storeStrings ? AllowedRecordTypes.BinaryObjectString : AllowedRecordTypes.AnyObject;
        Populate(_records, result, _lengths, allowedRecordTypes, allowNulls);

        return result;
    }

    private protected override void AddValue(object value) => _records.Add((SerializationRecord)value);

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetAllowedRecordType()
        => (_allowedRecordTypes, default);
}
