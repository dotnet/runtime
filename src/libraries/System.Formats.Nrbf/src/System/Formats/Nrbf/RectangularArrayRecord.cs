// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Formats.Nrbf.Utils;

namespace System.Formats.Nrbf;

internal sealed class RectangularArrayRecord : ArrayRecord
{
    private readonly int[] _lengths;
    private readonly ICollection<object> _values;
    private TypeName? _typeName;

    private RectangularArrayRecord(Type elementType, ArrayInfo arrayInfo,
        MemberTypeInfo memberTypeInfo, int[] lengths, bool canPreAllocate) : base(arrayInfo)
    {
        ElementType = elementType;
        MemberTypeInfo = memberTypeInfo;
        _lengths = lengths;

        // A List<T> can hold as many objects as an array, so for multi-dimensional arrays
        // with more elements than Array.MaxLength we use LinkedList.
        // Testing that many elements takes a LOT of time, so to ensure that both code paths are tested,
        // we always use LinkedList code path for Debug builds.
#if DEBUG
        _values = new LinkedList<object>();
#else
        _values = arrayInfo.TotalElementsCount <= ArrayInfo.MaxArrayLength
            ? new List<object>(canPreAllocate ? arrayInfo.GetSZArrayLength() : Math.Min(4, arrayInfo.GetSZArrayLength()))
            : new LinkedList<object>();
#endif

    }

    public override SerializationRecordType RecordType => SerializationRecordType.BinaryArray;

    public override ReadOnlySpan<int> Lengths => _lengths.AsSpan();

    public override TypeName TypeName
        => _typeName ??= MemberTypeInfo.GetArrayTypeName(ArrayInfo);

    private Type ElementType { get; }

    private MemberTypeInfo MemberTypeInfo { get; }

    [RequiresDynamicCode("May call Array.CreateInstance() and Type.MakeArrayType().")]
    private protected override Array Deserialize(Type arrayType, bool allowNulls)
    {
        // We can not deserialize non-primitive types.
        // This method returns arrays of ClassRecord for arrays of complex types.
        Array result =
#if NET9_0_OR_GREATER
            ElementType == typeof(ClassRecord)
                ? Array.CreateInstance(ElementType, _lengths)
                : Array.CreateInstanceFromArrayType(arrayType, _lengths);
#else
            Array.CreateInstance(ElementType, _lengths);
#endif

#if !NET8_0_OR_GREATER
        int[] indices = new int[_lengths.Length];

        foreach (object value in _values)
        {
            result.SetValue(GetActualValue(value), indices);

            int dimension = indices.Length - 1;
            while (dimension >= 0)
            {
                indices[dimension]++;
                if (indices[dimension] < Lengths[dimension])
                {
                    break;
                }
                indices[dimension] = 0;
                dimension--;
            }

            if (dimension < 0)
            {
                break;
            }
        }

        return result;
#else
        // Idea from Array.CoreCLR that maps an array of int indices into
        // an internal flat index.
        if (ElementType.IsValueType)
        {
            if (ElementType == typeof(bool)) CopyTo<bool>(_values, result);
            else if (ElementType == typeof(byte)) CopyTo<byte>(_values, result);
            else if (ElementType == typeof(sbyte)) CopyTo<sbyte>(_values, result);
            else if (ElementType == typeof(short)) CopyTo<short>(_values, result);
            else if (ElementType == typeof(ushort)) CopyTo<ushort>(_values, result);
            else if (ElementType == typeof(char)) CopyTo<char>(_values, result);
            else if (ElementType == typeof(int)) CopyTo<int>(_values, result);
            else if (ElementType == typeof(float)) CopyTo<float>(_values, result);
            else if (ElementType == typeof(long)) CopyTo<long>(_values, result);
            else if (ElementType == typeof(ulong)) CopyTo<ulong>(_values, result);
            else if (ElementType == typeof(double)) CopyTo<double>(_values, result);
            else if (ElementType == typeof(TimeSpan)) CopyTo<TimeSpan>(_values, result);
            else if (ElementType == typeof(DateTime)) CopyTo<DateTime>(_values, result);
            else if (ElementType == typeof(decimal)) CopyTo<decimal>(_values, result);
        }
        else
        {
            CopyTo<object>(_values, result);
        }

        return result;

        static void CopyTo<T>(ICollection<object> list, Array array)
        {
            ref byte arrayDataRef = ref MemoryMarshal.GetArrayDataReference(array);
            ref T firstElementRef = ref Unsafe.As<byte, T>(ref arrayDataRef);
            nuint flattenedIndex = 0;
            foreach (object value in list)
            {
                ref T targetElement = ref Unsafe.Add(ref firstElementRef, flattenedIndex);
                targetElement = (T)GetActualValue(value)!;
                flattenedIndex++;
            }
        }
#endif
    }

    private protected override void AddValue(object value) => _values.Add(value);

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

    internal static RectangularArrayRecord Create(BinaryReader reader, ArrayInfo arrayInfo,
        MemberTypeInfo memberTypeInfo, int[] lengths)
    {
        BinaryType binaryType = memberTypeInfo.Infos[0].BinaryType;
        Type elementType = binaryType switch
        {
            BinaryType.Primitive => MapPrimitive((PrimitiveType)memberTypeInfo.Infos[0].AdditionalInfo!),
            BinaryType.PrimitiveArray => MapPrimitiveArray((PrimitiveType)memberTypeInfo.Infos[0].AdditionalInfo!),
            BinaryType.String => typeof(string),
            BinaryType.Object => typeof(object),
            _ => typeof(ClassRecord)
        };

        bool canPreAllocate = false;
        if (binaryType == BinaryType.Primitive)
        {
            int sizeOfSingleValue = (PrimitiveType)memberTypeInfo.Infos[0].AdditionalInfo! switch
            {
                PrimitiveType.Boolean => sizeof(bool),
                PrimitiveType.Byte => sizeof(byte),
                PrimitiveType.SByte => sizeof(sbyte),
                PrimitiveType.Char => sizeof(byte), // it's UTF8
                PrimitiveType.Int16 => sizeof(short),
                PrimitiveType.UInt16 => sizeof(ushort),
                PrimitiveType.Int32 => sizeof(int),
                PrimitiveType.UInt32 => sizeof(uint),
                PrimitiveType.Single => sizeof(float),
                PrimitiveType.Int64 => sizeof(long),
                PrimitiveType.UInt64 => sizeof(ulong),
                PrimitiveType.Double => sizeof(double),
                _ => -1
            };

            if (sizeOfSingleValue > 0)
            {
                long size = arrayInfo.TotalElementsCount * sizeOfSingleValue;
                bool? isDataAvailable = reader.IsDataAvailable(size);
                if (isDataAvailable.HasValue)
                {
                    if (!isDataAvailable.Value)
                    {
                        ThrowHelper.ThrowEndOfStreamException();
                    }

                    canPreAllocate = true;
                }
            }
        }

        return new RectangularArrayRecord(elementType, arrayInfo, memberTypeInfo, lengths, canPreAllocate);
    }

    private static Type MapPrimitive(PrimitiveType primitiveType)
        => primitiveType switch
        {
            PrimitiveType.Boolean => typeof(bool),
            PrimitiveType.Byte => typeof(byte),
            PrimitiveType.Char => typeof(char),
            PrimitiveType.Decimal => typeof(decimal),
            PrimitiveType.Double => typeof(double),
            PrimitiveType.Int16 => typeof(short),
            PrimitiveType.Int32 => typeof(int),
            PrimitiveType.Int64 => typeof(long),
            PrimitiveType.SByte => typeof(sbyte),
            PrimitiveType.Single => typeof(float),
            PrimitiveType.TimeSpan => typeof(TimeSpan),
            PrimitiveType.DateTime => typeof(DateTime),
            PrimitiveType.UInt16 => typeof(ushort),
            PrimitiveType.UInt32 => typeof(uint),
            _ => typeof(ulong)
        };

    private static Type MapPrimitiveArray(PrimitiveType primitiveType)
        => primitiveType switch
        {
            PrimitiveType.Boolean => typeof(bool[]),
            PrimitiveType.Byte => typeof(byte[]),
            PrimitiveType.Char => typeof(char[]),
            PrimitiveType.Decimal => typeof(decimal[]),
            PrimitiveType.Double => typeof(double[]),
            PrimitiveType.Int16 => typeof(short[]),
            PrimitiveType.Int32 => typeof(int[]),
            PrimitiveType.Int64 => typeof(long[]),
            PrimitiveType.SByte => typeof(sbyte[]),
            PrimitiveType.Single => typeof(float[]),
            PrimitiveType.TimeSpan => typeof(TimeSpan[]),
            PrimitiveType.DateTime => typeof(DateTime[]),
            PrimitiveType.UInt16 => typeof(ushort[]),
            PrimitiveType.UInt32 => typeof(uint[]),
            _ => typeof(ulong[]),
        };

    private static object? GetActualValue(object value)
        => value is SerializationRecord serializationRecord
            ? serializationRecord.GetValue()
            : value; // it must be a primitive type
}
