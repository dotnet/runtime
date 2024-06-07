// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Runtime.Serialization.BinaryFormat.Utils;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
/// Defines the core behavior for NRBF array records and provides a base for derived classes.
/// </summary>
#if SYSTEM_RUNTIME_SERIALIZATION_BINARYFORMAT
public
#else
internal
#endif
abstract class ArrayRecord : SerializationRecord
{
    private protected Array? _arrayNullsAllowed;
    private protected Array? _arrayNullsNotAllowed;

    private protected ArrayRecord(ArrayInfo arrayInfo)
    {
        ArrayInfo = arrayInfo;
        ValuesToRead = arrayInfo.TotalElementsCount;
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
    public BinaryArrayType ArrayType => ArrayInfo.ArrayType;

    /// <summary>
    /// Gets the name of the array element type.
    /// </summary>
    /// <value>The name of the array element type.</value>
    public abstract TypeName ElementTypeName { get; }

    /// <inheritdoc />
    public override int ObjectId => ArrayInfo.ObjectId;

    internal long ValuesToRead { get; private protected set; }

    private protected ArrayInfo ArrayInfo { get; }

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
        if (!IsTypeNameMatching(expectedArrayType))
        {
            throw new InvalidOperationException(SR.Format(SR.Serialization_TypeMismatch, expectedArrayType.AssemblyQualifiedName, ElementTypeName.AssemblyQualifiedName));
        }

        return allowNulls
            ? _arrayNullsAllowed ??= Deserialize(expectedArrayType, true)
            : _arrayNullsNotAllowed ??= Deserialize(expectedArrayType, false);
    }

    [RequiresDynamicCode("May call Array.CreateInstance() and Type.MakeArrayType().")]
    private protected abstract Array Deserialize(Type arrayType, bool allowNulls);

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

/// <summary>
/// Defines the core behavior for NRBF single dimensional, zero-indexed array records and provides a base for derived classes.
/// </summary>
/// <typeparam name="T"></typeparam>
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
    public int Length => ArrayInfo.GetSZArrayLength();

    /// <inheritdoc/>
    public override ReadOnlySpan<int> Lengths => new int[1] { Length };

    /// <summary>
    /// When overridden in a derived class, allocates an array of <typeparamref name="T"/> and fills it with the data provided in the serialized records (in case of primitive types like <see cref="string"/> or <see cref="int"/>) or the serialized records themselves.
    /// </summary>
    /// <param name="allowNulls">
    ///   <see langword="true" /> to permit <see langword="null" /> values within the array;
    ///   otherwise, <see langword="false" />.
    /// </param>
    /// <returns>An array filled with the data provided in the serialized records.</returns>
    public abstract T?[] GetArray(bool allowNulls = true);

#pragma warning disable IL3051 // RequiresDynamicCode is not required in this particualar case
    private protected override Array Deserialize(Type arrayType, bool allowNulls) => GetArray(allowNulls);
#pragma warning restore IL3051
}
