﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;
using System.Formats.Nrbf.Utils;
using System.Diagnostics;

namespace System.Formats.Nrbf;

/// <summary>
/// Represents an array other than single dimensional array of primitive types or <see cref="object"/>.
/// </summary>
/// <remarks>
/// BinaryArray records are described in <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nrbf/9c62c928-db4e-43ca-aeba-146256ef67c2">[MS-NRBF] 2.4.3.1</see>.
/// </remarks>
internal sealed class BinaryArrayRecord : ArrayRecord
{
    private static HashSet<Type> PrimitiveTypes { get; } =
    [
        typeof(bool), typeof(char), typeof(byte), typeof(sbyte),
        typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(IntPtr), typeof(UIntPtr),
        typeof(float), typeof(double), typeof(decimal), typeof(DateTime),
        typeof(TimeSpan), typeof(string), typeof(object)
    ];

    private TypeName? _typeName;
    private long _totalElementsCount;

    private BinaryArrayRecord(ArrayInfo arrayInfo, MemberTypeInfo memberTypeInfo)
        : base(arrayInfo)
    {
        MemberTypeInfo = memberTypeInfo;
        Values = [];
        // We need to parse all elements of the jagged array to obtain total elements count.
        _totalElementsCount = -1;
    }

    public override SerializationRecordType RecordType => SerializationRecordType.BinaryArray;

    /// <inheritdoc/>
    public override ReadOnlySpan<int> Lengths => new int[1] { Length };

    /// <inheritdoc/>
    public override long FlattenedLength
    {
        get
        {
            if (_totalElementsCount < 0)
            {
                _totalElementsCount = IsJagged
                    ? GetJaggedArrayFlattenedLength(this)
                    : ArrayInfo.FlattenedLength;
            }

            return _totalElementsCount;
        }
    }

    public override TypeName TypeName
        => _typeName ??= MemberTypeInfo.GetArrayTypeName(ArrayInfo);

    private int Length => ArrayInfo.GetSZArrayLength();

    private MemberTypeInfo MemberTypeInfo { get; }

    private List<object> Values { get; }

    [RequiresDynamicCode("May call Array.CreateInstance() and Type.MakeArrayType().")]
    private protected override Array Deserialize(Type arrayType, bool allowNulls)
    {
        // We can not deserialize non-primitive types.
        // This method returns arrays of ClassRecord for arrays of complex types.
        Type elementType = MapElementType(arrayType, out bool isClassRecord);
        Type actualElementType = arrayType.GetElementType()!;
        Array array =
#if NET9_0_OR_GREATER
            isClassRecord
                ? Array.CreateInstance(elementType, Length)
                : Array.CreateInstanceFromArrayType(arrayType, Length);
#else
            Array.CreateInstance(elementType, Length);
#endif

        int resultIndex = 0;
        foreach (object value in Values)
        {
            object item = value is MemberReferenceRecord referenceRecord
                ? referenceRecord.GetReferencedRecord()
                : value;

            if (item is not SerializationRecord record)
            {
                array.SetValue(item, resultIndex++);
                continue;
            }

            switch (record.RecordType)
            {
                case SerializationRecordType.BinaryArray:
                case SerializationRecordType.ArraySinglePrimitive:
                case SerializationRecordType.ArraySingleObject:
                case SerializationRecordType.ArraySingleString:

                    // Recursion depth is bounded by the depth of arrayType, which is
                    // a trustworthy Type instance. Don't need to worry about stack overflow.

                    ArrayRecord nestedArrayRecord = (ArrayRecord)record;
                    Array nestedArray = nestedArrayRecord.GetArray(actualElementType, allowNulls);
                    array.SetValue(nestedArray, resultIndex++);
                    break;
                case SerializationRecordType.ObjectNull:
                case SerializationRecordType.ObjectNullMultiple256:
                case SerializationRecordType.ObjectNullMultiple:
                    if (!allowNulls)
                    {
                        ThrowHelper.ThrowArrayContainedNulls();
                    }

                    int nullCount = ((NullsRecord)item).NullCount;
                    Debug.Assert(nullCount > 0, "All implementations of NullsRecord are expected to return a positive value for NullCount.");
                    do
                    {
                        array.SetValue(null, resultIndex++);
                        nullCount--;
                    }
                    while (nullCount > 0);
                    break;
                default:
                    array.SetValue(record.GetValue(), resultIndex++);
                    break;
            }
        }

        Debug.Assert(resultIndex == array.Length, "We should have traversed the entirety of the newly created array.");

        return array;
    }

    internal static ArrayRecord Decode(BinaryReader reader, RecordMap recordMap, PayloadOptions options)
    {
        SerializationRecordId objectId = SerializationRecordId.Decode(reader);
        BinaryArrayType arrayType = reader.ReadArrayType();
        int rank = reader.ReadInt32();

        bool isRectangular = arrayType is BinaryArrayType.Rectangular;

        // It is an arbitrary limit in the current CoreCLR type loader.
        // Don't change this value without reviewing the loop a few lines below.
        const int MaxSupportedArrayRank = 32;

        if (rank < 1 || rank > MaxSupportedArrayRank
            || (rank != 1 && !isRectangular)
            || (rank == 1 && isRectangular))
        {
            ThrowHelper.ThrowInvalidValue(rank);
        }

        int[] lengths = new int[rank]; // adversary-controlled, but acceptable since upper limit of 32
        long totalElementCount = 1; // to avoid integer overflow during the multiplication below
        for (int i = 0; i < lengths.Length; i++)
        {
            lengths[i] = ArrayInfo.ParseValidArrayLength(reader);
            totalElementCount *= lengths[i];

            // n.b. This forbids "new T[Array.MaxLength, Array.MaxLength, Array.MaxLength, ..., 0]"
            // but allows "new T[0, Array.MaxLength, Array.MaxLength, Array.MaxLength, ...]". But
            // that's the same behavior that newarr and Array.CreateInstance exhibit, so at least
            // we're consistent.

            if (totalElementCount > ArrayInfo.MaxArrayLength)
            {
                ThrowHelper.ThrowInvalidValue(lengths[i]); // max array size exceeded
            }
        }

        // Per BinaryReaderExtensions.ReadArrayType, we do not support nonzero offsets, so
        // we don't need to read the NRBF stream 'LowerBounds' field here.

        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Decode(reader, 1, options, recordMap);
        ArrayInfo arrayInfo = new(objectId, totalElementCount, arrayType, rank);

        if (isRectangular)
        {
            return RectangularArrayRecord.Create(reader, arrayInfo, memberTypeInfo, lengths);
        }

        return memberTypeInfo.ShouldBeRepresentedAsArrayOfClassRecords()
            ? new ArrayOfClassesRecord(arrayInfo, memberTypeInfo)
            : new BinaryArrayRecord(arrayInfo, memberTypeInfo);
    }

    private static long GetJaggedArrayFlattenedLength(BinaryArrayRecord jaggedArrayRecord)
    {
        long result = 0;
        Queue<BinaryArrayRecord>? jaggedArrayRecords = null;

        do
        {
            if (jaggedArrayRecords is not null)
            {
                jaggedArrayRecord = jaggedArrayRecords.Dequeue();
            }

            Debug.Assert(jaggedArrayRecord.IsJagged);

            // In theory somebody could create a payload that would represent
            // a very nested array with total elements count > long.MaxValue.
            // That is why this method is using checked arithmetic.
            result = checked(result + jaggedArrayRecord.Length); // count the arrays themselves

            foreach (object value in jaggedArrayRecord.Values)
            {
                if (value is not SerializationRecord record)
                {
                    continue;
                }

                if (record.RecordType == SerializationRecordType.MemberReference)
                {
                    record = ((MemberReferenceRecord)record).GetReferencedRecord();
                }

                switch (record.RecordType)
                {
                    case SerializationRecordType.ArraySinglePrimitive:
                    case SerializationRecordType.ArraySingleObject:
                    case SerializationRecordType.ArraySingleString:
                    case SerializationRecordType.BinaryArray:
                        ArrayRecord nestedArrayRecord = (ArrayRecord)record;
                        if (nestedArrayRecord.IsJagged)
                        {
                            (jaggedArrayRecords ??= new()).Enqueue((BinaryArrayRecord)nestedArrayRecord);
                        }
                        else
                        {
                            // Don't call nestedArrayRecord.FlattenedLength to avoid any potential recursion,
                            // just call nestedArrayRecord.ArrayInfo.FlattenedLength that returns pre-computed value.
                            result = checked(result + nestedArrayRecord.ArrayInfo.FlattenedLength);
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        while (jaggedArrayRecords is not null && jaggedArrayRecords.Count > 0);

        return result;
    }

    private protected override void AddValue(object value) => Values.Add(value);

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

    /// <summary>
    /// Complex types must not be instantiated, but represented as ClassRecord.
    /// For arrays of primitive types like int, string and object this method returns the element type.
    /// For array of complex types, it returns ClassRecord.
    /// It takes arrays of arrays into account:
    /// - int[][] => int[]
    /// - MyClass[][][] => ClassRecord[][]
    /// </summary>
    [RequiresDynamicCode("May call Type.MakeArrayType().")]
    private static Type MapElementType(Type arrayType, out bool isClassRecord)
    {
        Type elementType = arrayType;
        int arrayNestingDepth = 0;

        // Loop iteration counts are bound by the nesting depth of arrayType,
        // which is a trustworthy input. No DoS concerns.

        while (elementType.IsArray)
        {
            elementType = elementType.GetElementType()!;
            arrayNestingDepth++;
        }

        if (PrimitiveTypes.Contains(elementType) || (Nullable.GetUnderlyingType(elementType) is Type nullable && PrimitiveTypes.Contains(nullable)))
        {
            isClassRecord = false;
            return arrayNestingDepth == 1 ? elementType : arrayType.GetElementType()!;
        }

        // Complex types are never instantiated, but represented as ClassRecord
        isClassRecord = true;
        Type complexType = typeof(ClassRecord);
        for (int i = 1; i < arrayNestingDepth; i++)
        {
            complexType = complexType.MakeArrayType();
        }

        return complexType;
    }
}
