// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;

namespace System.Runtime.Serialization.BinaryFormat;

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

    private TypeName? _elementTypeName;

    private BinaryArrayRecord(ArrayInfo arrayInfo, MemberTypeInfo memberTypeInfo, RecordMap recordMap)
        : base(arrayInfo)
    {
        MemberTypeInfo = memberTypeInfo;
        RecordMap = recordMap;
        Values = [];
    }

    public override RecordType RecordType => RecordType.BinaryArray;

    /// <inheritdoc/>
    public override ReadOnlySpan<int> Lengths => new int[1] { Length };

    public override TypeName ElementTypeName
        => _elementTypeName ??= MemberTypeInfo.GetElementTypeName(RecordMap);

    private int Length => ArrayInfo.Length;

    private MemberTypeInfo MemberTypeInfo { get; }

    private RecordMap RecordMap { get; }

    private List<object> Values { get; }

    [RequiresDynamicCode("May call Array.CreateInstance() and Type.MakeArrayType().")]
    private protected override Array Deserialize(Type arrayType, bool allowNulls, int maxLength)
    {
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
                case RecordType.BinaryArray:
                case RecordType.ArraySinglePrimitive:
                case RecordType.ArraySingleObject:
                case RecordType.ArraySingleString:
                    ArrayRecord nestedArrayRecord = (ArrayRecord)record;
                    Array nestedArray = nestedArrayRecord.ToArray(actualElementType, allowNulls, maxLength);
                    array.SetValue(nestedArray, resultIndex++);
                    break;
                case RecordType.ObjectNull:
                case RecordType.ObjectNullMultiple256:
                case RecordType.ObjectNullMultiple:
                    if (!allowNulls)
                    {
                        ThrowHelper.ThrowArrayContainedNulls();
                    }

                    int nullCount = ((NullsRecord)item).NullCount;
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

        return array;
    }

    internal static ArrayRecord Parse(BinaryReader reader, RecordMap recordMap, PayloadOptions options)
    {
        int objectId = reader.ReadInt32();
        ArrayType arrayType = reader.ReadArrayType();
        int rank = reader.ReadInt32();

        bool isRectangular = arrayType is ArrayType.Rectangular or ArrayType.RectangularOffset;

        if (rank < 1 || rank > 32
            || (rank != 1 && !isRectangular)
            || (rank == 1 && isRectangular))
        {
            ThrowHelper.ThrowInvalidValue(rank);
        }

        int[] lengths = new int[rank]; // adversary-controlled, but acceptable since upper limit of 32
        long totalElementCount = 1;
        for (int i = 0; i < lengths.Length; i++)
        {
            lengths[i] = ArrayInfo.ParseValidArrayLength(reader);
            totalElementCount *= lengths[i];

            if (totalElementCount > uint.MaxValue)
            {
                ThrowHelper.ThrowInvalidValue(lengths[i]); // max array size exceeded
            }
        }

        int[] offsets = new int[rank]; // zero-init; adversary-controlled, but acceptable since upper limit of 32
        bool hasCustomOffset = false;
        if (arrayType is ArrayType.SingleOffset or ArrayType.JaggedOffset or ArrayType.RectangularOffset)
        {
            for (int i = 0; i < offsets.Length; i++)
            {
                int offset = reader.ReadInt32();

                if (offset < 0)
                {
                    ThrowHelper.ThrowInvalidValue(offset);
                }
                else if (offset > 0)
                {
                    hasCustomOffset = true;

                    long maxIndex = lengths[i] + offset;
                    if (maxIndex > int.MaxValue)
                    {
                        ThrowHelper.ThrowInvalidValue(maxIndex);
                    }
                }

                offsets[i] = offset;
            }
        }

        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, 1, options);
        ArrayInfo arrayInfo = new(objectId, (int)totalElementCount, arrayType, rank);

        if (isRectangular || hasCustomOffset)
        {
            return RectangularOrCustomOffsetArrayRecord.Create(arrayInfo, memberTypeInfo, lengths, offsets, recordMap);
        }

        return memberTypeInfo.ShouldBeRepresentedAsArrayOfClassRecords()
            ? new ArrayOfClassesRecord(arrayInfo, memberTypeInfo, recordMap)
            : new BinaryArrayRecord(arrayInfo, memberTypeInfo, recordMap);
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

    internal override bool IsElementType(Type typeElement)
        => MemberTypeInfo.IsElementType(typeElement, RecordMap);

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
