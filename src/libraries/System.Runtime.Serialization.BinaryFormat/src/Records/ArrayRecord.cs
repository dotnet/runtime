// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace System.Runtime.Serialization.BinaryFormat;

public abstract class ArrayRecord : SerializationRecord
{
    internal const int DefaultMaxArrayLength = 64_000;

    private protected ArrayRecord(ArrayInfo arrayInfo)
    {
        ArrayInfo = arrayInfo;
        ValuesToRead = arrayInfo.Length;
    }

    /// <summary>
    /// Length of the array.
    /// </summary>
    public uint Length => ArrayInfo.Length;

    /// <summary>
    /// Rank of the array.
    /// </summary>
    public int Rank => ArrayInfo.Rank;

    /// <summary>
    /// Type of the array.
    /// </summary>
    public ArrayType ArrayType => ArrayInfo.ArrayType;

    public abstract TypeName ElementTypeName { get; }

    public override int ObjectId => ArrayInfo.ObjectId;

    internal long ValuesToRead { get; private protected set; }

    private protected ArrayInfo ArrayInfo { get; }

    public Array ToArray(Type expectedArrayType, bool allowNulls = true, int maxLength = DefaultMaxArrayLength)
    {
        if (!IsTypeNameMatching(expectedArrayType))
        {
            throw new InvalidOperationException();
        }

        return Deserialize(expectedArrayType, allowNulls, maxLength);
    }

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

public abstract class ArrayRecord<T> : ArrayRecord
{
    private protected ArrayRecord(ArrayInfo arrayInfo) : base(arrayInfo)
    {
    }

    /// <summary>
    /// Allocates an array of <typeparamref name="T"/> and fills it with the data provided in the serialized records (in case of primitive types like <see cref="string"/> or <see cref="int"/>) or the serialized records themselves.
    /// </summary>
    /// <param name="allowNulls">Specifies whether null values are allowed.</param>
    /// <param name="maxLength">Specifies the max length of an array that can be allocated.</param>
    /// <remarks>
    /// <para>
    /// The array has <seealso cref="ArrayRecord.Length"/> elements and can be used as a vector of attack.
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

    // PERF: if allocating new arrays is not acceptable, then we could introduce CopyTo method

    private protected override Array Deserialize(Type arrayType, bool allowNulls, int maxLength)
        => ToArray(allowNulls, maxLength);

    protected abstract T?[] ToArrayOfT(bool allowNulls);
}
