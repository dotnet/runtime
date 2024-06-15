// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Formats.Nrbf;

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
        Debug.Assert(arrayRecord.RecordType is not (SerializationRecordType.ArraySingleString or SerializationRecordType.ArraySinglePrimitive));

        _arrayRecord = arrayRecord;
        _elementType = deserializer.TypeResolver.GetType(arrayRecord.TypeName.GetElementType());
        Type expectedArrayType = arrayRecord.Rank switch
        {
            1 => _elementType.MakeArrayType(),
            _ => _elementType.MakeArrayType(arrayRecord.Rank),
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

    internal override SerializationRecordId Continue()
    {
        int[] indices = _indices;
        int[] lengths = _lengths;

        while (_canIterate)
        {
            (object? memberValue, SerializationRecordId reference) = UnwrapMemberValue(_arrayOfClassRecords.GetValue(indices));

            if (s_missingValueSentinel == memberValue)
            {
                // Record has not been encountered yet, need to pend iteration.
                return reference;
            }

            if (memberValue is not null && DoesValueNeedUpdated(memberValue, reference))
            {
                // Need to track a fixup for this index.
                _hasFixups = true;
                Deserializer.PendValueUpdater(new ArrayUpdater(_arrayRecord.Id, reference, indices.ToArray()));
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
            Deserializer.CompleteObject(_arrayRecord.Id);
        }

        return default(SerializationRecordId);
    }

    internal static Array GetArraySinglePrimitive(SerializationRecord record) => record switch
    {
        SZArrayRecord<bool> primitiveArray => primitiveArray.GetArray(),
        SZArrayRecord<byte> primitiveArray => primitiveArray.GetArray(),
        SZArrayRecord<sbyte> primitiveArray => primitiveArray.GetArray(),
        SZArrayRecord<char> primitiveArray => primitiveArray.GetArray(),
        SZArrayRecord<short> primitiveArray => primitiveArray.GetArray(),
        SZArrayRecord<ushort> primitiveArray => primitiveArray.GetArray(),
        SZArrayRecord<int> primitiveArray => primitiveArray.GetArray(),
        SZArrayRecord<uint> primitiveArray => primitiveArray.GetArray(),
        SZArrayRecord<long> primitiveArray => primitiveArray.GetArray(),
        SZArrayRecord<ulong> primitiveArray => primitiveArray.GetArray(),
        SZArrayRecord<float> primitiveArray => primitiveArray.GetArray(),
        SZArrayRecord<double> primitiveArray => primitiveArray.GetArray(),
        SZArrayRecord<decimal> primitiveArray => primitiveArray.GetArray(),
        SZArrayRecord<DateTime> primitiveArray => primitiveArray.GetArray(),
        SZArrayRecord<TimeSpan> primitiveArray => primitiveArray.GetArray(),
        _ => throw new NotSupportedException(),
    };

    [RequiresUnreferencedCode("Calls System.Windows.Forms.BinaryFormat.BinaryFormattedObject.TypeResolver.GetType(TypeName)")]
    internal static Array? GetSimpleBinaryArray(ArrayRecord arrayRecord, BinaryFormattedObject.ITypeResolver typeResolver)
    {
        Type arrayRecordElementType = typeResolver.GetType(arrayRecord.TypeName.GetElementType());
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

        Type expectedArrayType = arrayRecord.Rank switch
        {
            1 => arrayRecordElementType.MakeArrayType(),
            _ => arrayRecordElementType.MakeArrayType(arrayRecord.Rank)
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
