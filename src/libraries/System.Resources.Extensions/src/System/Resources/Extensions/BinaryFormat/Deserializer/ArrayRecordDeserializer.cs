// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization.BinaryFormat;

namespace System.Resources.Extensions.BinaryFormat.Deserializer;

internal sealed class ArrayRecordDeserializer : ObjectRecordDeserializer
{
    private readonly ArrayRecord _arrayRecord;
    private readonly Type _elementType;
    private readonly Array _arrayOfClassRecords;
    private readonly Array _arrayOfT;
    private readonly int[] _lengths, _indices;
    private bool _hasFixups, _canIterate;

    [RequiresUnreferencedCode("Calls System.Windows.Forms.BinaryFormat.BinaryFormattedObject.TypeResolver.GetType(TypeName)")]
    internal ArrayRecordDeserializer(ArrayRecord arrayRecord, IDeserializer deserializer)
        : base(arrayRecord, deserializer)
    {
        // Other array types are handled directly (ArraySinglePrimitive and ArraySingleString).
        Debug.Assert(arrayRecord.RecordType is not (RecordType.ArraySingleString or RecordType.ArraySinglePrimitive));
        Debug.Assert(arrayRecord.ArrayType is (BinaryArrayType.Single or BinaryArrayType.Jagged or BinaryArrayType.Rectangular));

        _arrayRecord = arrayRecord;
        _elementType = deserializer.TypeResolver.GetType(arrayRecord.ElementTypeName);
        Type expectedArrayType = arrayRecord.ArrayType switch
        {
            BinaryArrayType.Rectangular => _elementType.MakeArrayType(arrayRecord.Rank),
            _ => _elementType.MakeArrayType()
        };
        // Tricky part: for arrays of classes/structs the following record allocates and array of class records
        // (because the payload reader can not load types, instantiate objects and rehydrate them)
        _arrayOfClassRecords = arrayRecord.GetArray(expectedArrayType);
        // Now we need to create an array of the same length, but of a different, exact type
        Type elementType = _arrayOfClassRecords.GetType();
        while (elementType.IsArray)
        {
            elementType = elementType.GetElementType()!;
        }

        _lengths = arrayRecord.Lengths.ToArray();
        Object = _arrayOfT = Array.CreateInstance(_elementType, _lengths);
        _indices = new int[_lengths.Length];
        _canIterate = _arrayOfT.Length > 0;
    }

    internal override Id Continue()
    {
        int[] indices = _indices;
        int[] lengths = _lengths;

        while (_canIterate)
        {
            (object? memberValue, Id reference) = UnwrapMemberValue(_arrayOfClassRecords.GetValue(indices));

            if (s_missingValueSentinel == memberValue)
            {
                // Record has not been encountered yet, need to pend iteration.
                return reference;
            }

            if (memberValue is not null && DoesValueNeedUpdated(memberValue, reference))
            {
                // Need to track a fixup for this index.
                _hasFixups = true;
                Deserializer.PendValueUpdater(new ArrayUpdater(_arrayRecord.ObjectId, reference, indices.ToArray()));
            }

            _arrayOfT.SetValue(memberValue, indices);

            int dimension = indices.Length - 1;
            while (dimension >= 0)
            {
                indices[dimension]++;
                if (indices[dimension] < lengths[dimension])
                {
                    break;
                }

                indices[dimension] = 0;
                dimension--;
            }

            if (dimension < 0)
            {
                _canIterate = false;
            }
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
        ArrayRecord<bool> primitiveArray => primitiveArray.GetArray(),
        ArrayRecord<byte> primitiveArray => primitiveArray.GetArray(),
        ArrayRecord<sbyte> primitiveArray => primitiveArray.GetArray(),
        ArrayRecord<char> primitiveArray => primitiveArray.GetArray(),
        ArrayRecord<short> primitiveArray => primitiveArray.GetArray(),
        ArrayRecord<ushort> primitiveArray => primitiveArray.GetArray(),
        ArrayRecord<int> primitiveArray => primitiveArray.GetArray(),
        ArrayRecord<uint> primitiveArray => primitiveArray.GetArray(),
        ArrayRecord<long> primitiveArray => primitiveArray.GetArray(),
        ArrayRecord<ulong> primitiveArray => primitiveArray.GetArray(),
        ArrayRecord<float> primitiveArray => primitiveArray.GetArray(),
        ArrayRecord<double> primitiveArray => primitiveArray.GetArray(),
        ArrayRecord<decimal> primitiveArray => primitiveArray.GetArray(),
        ArrayRecord<DateTime> primitiveArray => primitiveArray.GetArray(),
        ArrayRecord<TimeSpan> primitiveArray => primitiveArray.GetArray(),
        _ => throw new NotSupportedException(),
    };

    [RequiresUnreferencedCode("Calls System.Windows.Forms.BinaryFormat.BinaryFormattedObject.TypeResolver.GetType(TypeName)")]
    internal static Array? GetSimpleBinaryArray(ArrayRecord arrayRecord, BinaryFormattedObject.ITypeResolver typeResolver)
    {
        if (arrayRecord.ArrayType is not (BinaryArrayType.Single or BinaryArrayType.Jagged or BinaryArrayType.Rectangular))
        {
            throw new NotSupportedException(SR.NotSupported_NonZeroOffsets);
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
            BinaryArrayType.Rectangular => arrayRecordElementType.MakeArrayType(arrayRecord.Rank),
            _ => arrayRecordElementType.MakeArrayType()
        };

        return arrayRecord.GetArray(expectedArrayType);

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
