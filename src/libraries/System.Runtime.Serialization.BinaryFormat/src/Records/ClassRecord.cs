// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Base class for class records.
/// </summary>
/// <remarks>
///  <para>
///   Includes the values for the class (which trail the record)
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/c9bc3af3-5a0c-4b29-b517-1b493b51f7bb">
///    [MS-NRBF] 2.3
///   </see>.
///  </para>
/// </remarks>
public abstract class ClassRecord : SerializationRecord
{
    private const int MaxLength = ArrayRecord.DefaultMaxArrayLength;

    private TypeName? _typeName;

    private protected ClassRecord(ClassInfo classInfo)
    {
        ClassInfo = classInfo;
        MemberValues = [];
    }

    public TypeName TypeName => _typeName ??= ClassInfo.Name.WithAssemblyName(LibraryName.FullName);

    internal abstract AssemblyNameInfo LibraryName { get; }

    // Currently we don't expose raw values, so we are not preserving the order here.
    public IEnumerable<string> MemberNames => ClassInfo.MemberNames.Keys;

    public override int ObjectId => ClassInfo.ObjectId;

    internal abstract int ExpectedValuesCount { get; }

    internal ClassInfo ClassInfo { get; }

    internal List<object?> MemberValues { get; }

    /// <summary>
    /// Checks if member of given name was present in the payload.
    /// </summary>
    /// <param name="memberName">The name of the member.</param>
    /// <returns>True if it was present, otherwise false.</returns>
    /// <remarks>
    ///  <para>
    ///   It's recommended to use this method when dealing with payload that may contain
    ///   different versions of the same type.s
    ///  </para>
    /// </remarks>
    public bool HasMember(string memberName) => ClassInfo.MemberNames.ContainsKey(memberName);

    /// <summary>
    /// Retrieves the value of the provided <paramref name="memberName"/>.
    /// </summary>
    /// <param name="memberName">The name of the member.</param>
    /// <returns>The value.</returns>
    /// <exception cref="KeyNotFoundException">Member of such name does not exist. You can use <seealso cref="HasMember(string)"/> to check if given member exists.</exception>
    /// <exception cref="InvalidOperationException">Member of such name has value of a different type.</exception>
    public ClassRecord? GetClassRecord(string memberName) => GetMember<ClassRecord>(memberName);

    /// <inheritdoc cref="GetClassRecord(string)"/>
    public string? GetString(string memberName) => GetMember<string>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public bool GetBoolean(string memberName) => GetMember<bool>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public byte GetByte(string memberName) => GetMember<byte>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public sbyte GetSByte(string memberName) => GetMember<sbyte>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public short GetInt16(string memberName) => GetMember<short>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public ushort GetUInt16(string memberName) => GetMember<ushort>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public char GetChar(string memberName) => GetMember<char>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public int GetInt32(string memberName) => GetMember<int>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public uint GetUInt32(string memberName) => GetMember<uint>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public float GetSingle(string memberName) => GetMember<float>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public long GetInt64(string memberName) => GetMember<long>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public ulong GetUInt64(string memberName) => GetMember<ulong>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public double GetDouble(string memberName) => GetMember<double>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public decimal GetDecimal(string memberName) => GetMember<decimal>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public TimeSpan GetTimeSpan(string memberName) => GetMember<TimeSpan>(memberName);
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public DateTime GetDateTime(string memberName) => GetMember<DateTime>(memberName);

    /// <returns>
    /// <para>For primitive types like <seealso cref="int"/>, <seealso cref="string"/> or <seealso cref="DateTime"/> returns their value.</para>
    /// <para>For nulls, returns a null.</para>
    /// <para>For other types that are not arrays, returns an instance of <seealso cref="ClassRecord"/>.</para>
    /// <para>For single-dimensional arrays returns <seealso cref="ArrayRecord{T}"/> where the generic type is the primitive type or <seealso cref="ClassRecord"/>.</para>
    /// <para>For jagged and multi-dimensional arrays, returns an instance of <seealso cref="ArrayRecord"/>.</para>
    /// </returns>
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public object? GetRawValue(string memberName) => GetMember<object>(memberName);

    /// <summary>
    /// Retrieves an array for the provided <paramref name="memberName"/>.
    /// </summary>
    /// <param name="memberName">The name of the field.</param>
    /// <param name="allowNulls">Specifies whether null values are allowed.</param>
    /// <param name="maxLength">Specifies the max length of an array that can be allocated.</param>
    /// <returns>The array itself or null.</returns>
    /// <exception cref="KeyNotFoundException">Member of such name does not exist.</exception>
    /// <exception cref="InvalidOperationException">Member of such name has value of a different type.</exception>
    public T?[]? GetArrayOfPrimitiveType<T>(string memberName, bool allowNulls = true, int maxLength = MaxLength)
        => GetMember<ArrayRecord<T>>(memberName)?.ToArray(allowNulls, maxLength);

    /// <summary>
    /// Retrieves the <see cref="SerializationRecord" /> of the provided <paramref name="memberName"/>.
    /// </summary>
    /// <param name="memberName">The name of the field.</param>
    /// <returns>The serialization record which can be either <seealso cref="PrimitiveTypeRecord{T}"/>,
    /// a <seealso cref="ClassRecord"/>, an <seealso cref="ArrayRecord"/> or a null.
    /// </returns>
    /// <exception cref="KeyNotFoundException">Member of such name does not exist.</exception>
    /// <exception cref="InvalidOperationException">Member of such name has value of a different type or was a primitive value.</exception>
    public SerializationRecord? GetSerializationRecord(string memberName)
        => MemberValues[ClassInfo.MemberNames[memberName]] switch
        {
            null or NullsRecord => null,
            MemberReferenceRecord referenceRecord => referenceRecord.GetReferencedRecord(),
            SerializationRecord serializationRecord => serializationRecord,
            _ => throw new InvalidOperationException()
        };

    private T? GetMember<T>(string memberName)
    {
        int index = ClassInfo.MemberNames[memberName];

        object? value = MemberValues[index];
        if (value is SerializationRecord record)
        {
            value = record.GetValue();
        }

        return value is null
            ? default
            : value is not T ? throw new InvalidOperationException() : (T)value!;
    }

    internal abstract (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType();

    internal override void HandleNextRecord(SerializationRecord nextRecord, NextInfo info)
    {
        Debug.Assert(!(nextRecord is NullsRecord nullsRecord && nullsRecord.NullCount > 1));

        HandleNextValue(nextRecord, info);
    }

    internal override void HandleNextValue(object value, NextInfo info)
    {
        MemberValues.Add(value);

        if (MemberValues.Count < ExpectedValuesCount)
        {
            (AllowedRecordTypes allowed, PrimitiveType primitiveType) = GetNextAllowedRecordType();

            info.Stack.Push(info.With(allowed, primitiveType));
        }
    }
}
