// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization.BinaryFormat;

namespace System.Windows.Forms.BinaryFormat.Deserializer;

internal sealed class ArrayRecordDeserializer : ObjectRecordDeserializer
{
    private readonly ArrayRecord _arrayRecord;
    private readonly Type _elementType;
    private readonly Array _arrayOfClassRecords;
    private readonly Array _arrayOfT;
    private int _index;
    private bool _hasFixups;

    [RequiresUnreferencedCode("Calls System.Windows.Forms.BinaryFormat.BinaryFormattedObject.TypeResolver.GetType(TypeName)")]
    internal ArrayRecordDeserializer(ArrayRecord arrayRecord, IDeserializer deserializer)
        : base(arrayRecord, deserializer)
    {
        // Other array types are handled directly (ArraySinglePrimitive and ArraySingleString).
        Debug.Assert(arrayRecord.RecordType is not (RecordType.ArraySingleString or RecordType.ArraySinglePrimitive));
        Debug.Assert(arrayRecord.ArrayType is (ArrayType.Single or ArrayType.Jagged or ArrayType.Rectangular));

        _arrayRecord = arrayRecord;
        _elementType = deserializer.TypeResolver.GetType(arrayRecord.ElementTypeName);
        Type expectedArrayType = arrayRecord.ArrayType switch
        {
            ArrayType.Rectangular => _elementType.MakeArrayType(arrayRecord.Rank),
            _ => _elementType.MakeArrayType()
        };
        // Tricky part: for arrays of classes/structs the following record allocates and array of class records
        // (because the payload reader can not load types, instantiate objects and rehydrate them)
        _arrayOfClassRecords = arrayRecord.ToArray(expectedArrayType, maxLength: Array.MaxLength);
        // Now we need to create an array of the same length, but of a different, exact type
        Type elementType = _arrayOfClassRecords.GetType();
        while (elementType.IsArray)
        {
            elementType = elementType.GetElementType()!;
        }

        int[] lengths = new int[arrayRecord.Rank];
        for (int dimension = 0; dimension < lengths.Length; dimension++)
        {
            lengths[dimension] = _arrayOfClassRecords.GetLength(dimension);
        }

        Object = _arrayOfT = Array.CreateInstance(_elementType, lengths);
    }

    internal override Id Continue()
    {
        while (_index < _arrayRecord.Length)
        {
            (object? memberValue, Id reference) = UnwrapMemberValue(_arrayOfClassRecords.GetArrayData<object?>()[_index]);

            if (s_missingValueSentinel == memberValue)
            {
                // Record has not been encountered yet, need to pend iteration.
                return reference;
            }

            if (memberValue is not null && DoesValueNeedUpdated(memberValue, reference))
            {
                // Need to track a fixup for this index.
                _hasFixups = true;
                Deserializer.PendValueUpdater(new ArrayUpdater(_arrayRecord.ObjectId, reference, _index));
            }

            if (memberValue is null && !_elementType.IsValueType)
            {
                // No point in setting a null. If we're a value type, let it flow to throw when setting.
                _index++;
                continue;
            }

            if (_elementType.IsValueType)
            {
                _arrayOfT.SetArrayValueByFlattenedIndex(memberValue, _index);
            }
            else
            {
                Span<object?> flatSpan = _arrayOfT.GetArrayData<object?>();
                flatSpan[_index] = memberValue;
            }

            _index++;
        }

        // No more missing member refs.

        if (!_hasFixups)
        {
            Deserializer.CompleteObject(_arrayRecord.ObjectId);
        }

        return Id.Null;
    }

    internal static Array GetArraySinglePrimitive(SerializationRecord record) => record switch
    {
        ArrayRecord<bool> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        ArrayRecord<byte> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        ArrayRecord<sbyte> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        ArrayRecord<char> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        ArrayRecord<short> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        ArrayRecord<ushort> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        ArrayRecord<int> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        ArrayRecord<uint> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        ArrayRecord<long> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        ArrayRecord<ulong> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        ArrayRecord<float> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        ArrayRecord<double> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        ArrayRecord<decimal> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        ArrayRecord<DateTime> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        ArrayRecord<TimeSpan> primitiveArray => primitiveArray.ToArray(maxLength: Array.MaxLength),
        _ => throw new NotSupportedException(),
    };

    [RequiresUnreferencedCode("Calls System.Windows.Forms.BinaryFormat.BinaryFormattedObject.TypeResolver.GetType(TypeName)")]
    internal static Array? GetSimpleBinaryArray(ArrayRecord arrayRecord, BinaryFormattedObject.ITypeResolver typeResolver)
    {
        if (arrayRecord.ArrayType is not (ArrayType.Single or ArrayType.Jagged or ArrayType.Rectangular))
        {
            throw new NotSupportedException("Only arrays with zero offsets are supported.");
        }

        Type arrayRecordElementType = typeResolver.GetType(arrayRecord.ElementTypeName);
        Type elementType = arrayRecordElementType;
        while (elementType.IsArray)
        {
            elementType = elementType.GetElementType()!;
        }

        if (!(HasBuiltInSupport(elementType)
            || (Nullable.GetUnderlyingType(elementType) is Type nullable && HasBuiltInSupport(nullable))))
        {
            return null;
        }

        Type expectedArrayType = arrayRecord.ArrayType switch
        {
            ArrayType.Rectangular => arrayRecordElementType.MakeArrayType(arrayRecord.Rank),
            _ => arrayRecordElementType.MakeArrayType()
        };

        return arrayRecord.ToArray(expectedArrayType, maxLength: Array.MaxLength);

        static bool HasBuiltInSupport(Type elementType)
            => elementType == typeof(string)
            || elementType == typeof(bool) || elementType == typeof(byte) || elementType == typeof(sbyte)
            || elementType == typeof(char) || elementType == typeof(short) || elementType == typeof(ushort)
            || elementType == typeof(int) || elementType == typeof(uint)
            || elementType == typeof(long) || elementType == typeof(ulong)
            || elementType == typeof(float) || elementType == typeof(double) || elementType == typeof(decimal)
            || elementType == typeof(DateTime) || elementType == typeof(TimeSpan);
    }
}
