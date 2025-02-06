// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Formats.Nrbf.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace System.Formats.Nrbf;

/// <summary>
/// Defines the core behavior for NRBF array records and provides a base for derived classes.
/// </summary>
public abstract class ArrayRecord : SerializationRecord
{
    private protected Array? _arrayNullsAllowed;
    private protected Array? _arrayNullsNotAllowed;

    private protected ArrayRecord(ArrayInfo arrayInfo)
    {
        ArrayInfo = arrayInfo;
        ValuesToRead = arrayInfo.FlattenedLength;
    }

    /// <summary>
    /// When overridden in a derived class, gets a buffer of integers that represent the number of elements in every dimension.
    /// </summary>
    /// <value>A buffer of integers that represent the number of elements in every dimension.</value>
    public abstract ReadOnlySpan<int> Lengths { get; }

    /// <summary>
    /// Gets the rank of the array.
    /// </summary>
    /// <value>The rank of the array.</value>
    public int Rank => ArrayInfo.Rank;

    /// <summary>
    /// Gets the type of the array.
    /// </summary>
    /// <value>The type of the array.</value>
    internal BinaryArrayType ArrayType => ArrayInfo.ArrayType;

    /// <inheritdoc />
    public override SerializationRecordId Id => ArrayInfo.Id;

    internal long ValuesToRead { get; private protected set; }

    internal ArrayInfo ArrayInfo { get; }

    internal bool IsJagged
        => ArrayInfo.ArrayType == BinaryArrayType.Jagged
        // It is possible to have binary array records have an element type of array without being marked as jagged.
        || TypeName.GetElementType().IsArray;

    /// <summary>
    /// Allocates an array and fills it with the data provided in the serialized records (in case of primitive types like <see cref="string"/> or <see cref="int"/>) or the serialized records themselves.
    /// </summary>
    /// <param name="expectedArrayType">Expected array type.</param>
    /// <param name="allowNulls">
    ///   <see langword="true" /> to permit <see langword="null" /> values within the array;
    ///   otherwise, <see langword="false" />.
    /// </param>
    /// <returns>An array filled with the data provided in the serialized records.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="expectedArrayType" /> does not match the data from the payload.</exception>
    /// <remarks>
    /// Check the total length of the array by using <see cref="Lengths"/> property before calling this method,
    /// as an attacker could have sent you a small payload that will require to allocate a very large array
    /// and potentially cause <see cref="OutOfMemoryException"/> and Denial of Service.
    /// </remarks>
    [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
    public Array GetArray(Type expectedArrayType, bool allowNulls = true)
    {
#if NET
        ArgumentNullException.ThrowIfNull(expectedArrayType);
#else
        if (expectedArrayType is null)
        {
            throw new ArgumentNullException(nameof(expectedArrayType));
        }
#endif
        if (!TypeNameMatches(expectedArrayType))
        {
            throw new InvalidOperationException(SR.Format(SR.Serialization_TypeMismatch, expectedArrayType.AssemblyQualifiedName, TypeName.AssemblyQualifiedName));
        }

        return allowNulls
            ? _arrayNullsAllowed ??= Deserialize(expectedArrayType, true)
            : _arrayNullsNotAllowed ??= Deserialize(expectedArrayType, false);
    }

    [RequiresDynamicCode("May call Array.CreateInstance() and Type.MakeArrayType().")]
    private protected abstract Array Deserialize(Type arrayType, bool allowNulls);

    internal sealed override void HandleNextValue(object value, NextInfo info)
        => HandleNext(value, info, size: 1);

    internal sealed override void HandleNextRecord(SerializationRecord nextRecord, NextInfo info)
        => HandleNext(nextRecord, info, size: nextRecord is NullsRecord nullsRecord ? nullsRecord.NullCount : 1);

    private protected abstract void AddValue(object value);

    private void HandleNext(object value, NextInfo info, int size)
    {
        ValuesToRead -= size;

        if (ValuesToRead < 0)
        {
            // The only way to get here is to read a multiple null item with Count
            // larger than the number of array items that were left to read.
            ThrowHelper.ThrowUnexpectedNullRecordCount();
        }
        else if (ValuesToRead > 0)
        {
            info.Stack.Push(info);
        }

        AddValue(value);
    }

    internal abstract (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetAllowedRecordType();

    internal static void Populate(List<SerializationRecord> source, Array destination, int[] lengths, AllowedRecordTypes allowedRecordTypes, bool allowNulls)
    {
        int[] indices = new int[lengths.Length];
        nuint numElementsWritten = 0; // only for debugging; not used in release builds

        foreach (SerializationRecord record in source)
        {
            object? value = GetActualValue(record, allowedRecordTypes, out int incrementCount);
            if (value is not null)
            {
                // null is a default element for all array of reference types, so we don't call SetValue for nulls.
                destination.SetValue(value, indices);
                Debug.Assert(incrementCount == 1, "IncrementCount other than 1 is allowed only for null records.");
            }
            else if (!allowNulls)
            {
                ThrowHelper.ThrowArrayContainedNulls();
            }

            while (incrementCount > 0)
            {
                incrementCount--;
                numElementsWritten++;
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
                    break;
                }
            }
        }

        Debug.Assert(numElementsWritten == (uint)source.Count, "We should have traversed the entirety of the source records collection.");
        Debug.Assert(numElementsWritten == (ulong)destination.LongLength, "We should have traversed the entirety of the destination array.");
    }

    private static object? GetActualValue(SerializationRecord record, AllowedRecordTypes allowedRecordTypes, out int repeatCount)
    {
        repeatCount = 1;

        if (record is NullsRecord nullsRecord)
        {
            repeatCount = nullsRecord.NullCount;
            return null;
        }
        else if (record.RecordType == SerializationRecordType.MemberReference)
        {
            record = ((MemberReferenceRecord)record).GetReferencedRecord();
        }

        if (allowedRecordTypes == AllowedRecordTypes.BinaryObjectString)
        {
            if (record is not BinaryObjectStringRecord stringRecord)
            {
                throw new SerializationException(SR.Serialization_InvalidReference);
            }

            return stringRecord.Value;
        }
        else if (allowedRecordTypes == AllowedRecordTypes.Arrays)
        {
            if (record is not ArrayRecord arrayRecord)
            {
                throw new SerializationException(SR.Serialization_InvalidReference);
            }

            return arrayRecord;
        }

        return record;
    }
}
