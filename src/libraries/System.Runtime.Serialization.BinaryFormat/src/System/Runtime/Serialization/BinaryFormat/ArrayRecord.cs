// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Runtime.Serialization.BinaryFormat.Utils;

namespace System.Runtime.Serialization.BinaryFormat;

#if SYSTEM_RUNTIME_SERIALIZATION_BINARYFORMAT
public
#else
internal
#endif
abstract class ArrayRecord : SerializationRecord
{
    internal const int DefaultMaxArrayLength = 64_000;

    private protected ArrayRecord(ArrayInfo arrayInfo)
    {
        ArrayInfo = arrayInfo;
        ValuesToRead = arrayInfo.Length;
    }

    /// <summary>
    /// Gets a buffer of integers that represent the number of elements in every dimension.
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
    public BinaryArrayType ArrayType => ArrayInfo.ArrayType;

    public abstract TypeName ElementTypeName { get; }

    /// <inheritdoc />
    public override int ObjectId => ArrayInfo.ObjectId;

    internal long ValuesToRead { get; private protected set; }

    private protected ArrayInfo ArrayInfo { get; }

    /// <summary>
    /// Allocates an array and fills it with the data provided in the serialized records (in case of primitive types like <see cref="string"/> or <see cref="int"/>) or the serialized records themselves.
    /// </summary>
    /// <param name="expectedArrayType">Expected array type.</param>
    /// <param name="allowNulls">Specifies whether null values are allowed.</param>
    /// <param name="maxLength">The total maximum number of elements in all the dimensions of the array.</param>
    /// <exception cref="InvalidOperationException">When there is a type mismatch.</exception>
    [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
    public Array ToArray(Type expectedArrayType, bool allowNulls = true, int maxLength = DefaultMaxArrayLength)
    {
        if (!IsTypeNameMatching(expectedArrayType))
        {
            throw new InvalidOperationException(SR.Format(SR.Serialization_TypeMismatch, expectedArrayType.AssemblyQualifiedName, ElementTypeName.AssemblyQualifiedName));
        }

        ReadOnlySpan<int> lengths = Lengths;
        long totalElementCount = lengths[0];
        for (int i = 1; i < lengths.Length; i++)
        {
            totalElementCount *= lengths[i];
        }

        if (totalElementCount > maxLength)
        {
            ThrowHelper.ThrowMaxArrayLength(maxLength, totalElementCount);
        }

        return Deserialize(expectedArrayType, allowNulls, maxLength);
    }

    [RequiresDynamicCode("May call Array.CreateInstance() and Type.MakeArrayType().")]
    private protected abstract Array Deserialize(Type arrayType, bool allowNulls, int maxLength);

    public override bool IsTypeNameMatching(Type type)
        => type.IsArray
        && type.GetArrayRank() == ArrayInfo.Rank
        && IsElementType(type.GetElementType()!);

    internal sealed override void HandleNextValue(object value, NextInfo info)
        => HandleNext(value, info, size: 1);

    internal sealed override void HandleNextRecord(SerializationRecord nextRecord, NextInfo info)
        => HandleNext(nextRecord, info, size: nextRecord is NullsRecord nullsRecord ? nullsRecord.NullCount : 1);

    private protected abstract void AddValue(object value);

    internal abstract bool IsElementType(Type typeElement);

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
}

#if SYSTEM_RUNTIME_SERIALIZATION_BINARYFORMAT
public
#else
internal
#endif
abstract class ArrayRecord<T> : ArrayRecord
{
    private protected ArrayRecord(ArrayInfo arrayInfo) : base(arrayInfo)
    {
    }

    /// <summary>
    /// Gets the length of the array.
    /// </summary>
    /// <value>The length of the array.</value>
    public int Length => ArrayInfo.Length;

    /// <inheritdoc/>
    public override ReadOnlySpan<int> Lengths => new int[1] { Length };

    /// <summary>
    /// Allocates an array of <typeparamref name="T"/> and fills it with the data provided in the serialized records (in case of primitive types like <see cref="string"/> or <see cref="int"/>) or the serialized records themselves.
    /// </summary>
    /// <param name="allowNulls">Specifies whether null values are allowed.</param>
    /// <param name="maxLength">Specifies the max length of an array that can be allocated.</param>
    /// <remarks>
    /// <para>
    /// The array has <seealso cref="ArrayRecord{T}.Length"/> elements and can be used as a vector of attack.
    /// Example: an array with Array.MaxLength elements that contains only nulls
    /// takes 15 bytes to serialize and more than 2 GB to deserialize!
    /// </para>
    /// <para>
    /// A new array is allocated every time this method is called.
    /// </para>
    /// </remarks>
    public T?[] ToArray(bool allowNulls = true, int maxLength = DefaultMaxArrayLength)
    {
        if (Length > maxLength)
        {
            ThrowHelper.ThrowMaxArrayLength(maxLength, Length);
        }

        return ToArrayOfT(allowNulls);
    }

#pragma warning disable IL3051 // RequiresDynamicCode is not required in this particualar case
    private protected override Array Deserialize(Type arrayType, bool allowNulls, int maxLength)
        => ToArray(allowNulls, maxLength);
#pragma warning restore IL3051

    protected abstract T?[] ToArrayOfT(bool allowNulls);
}
