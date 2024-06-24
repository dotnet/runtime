// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace System.Formats.Nrbf;

/// <summary>
/// Defines the core behavior for NRBF class records and provides a base for derived classes.
/// </summary>
public abstract class ClassRecord : SerializationRecord
{
    private protected ClassRecord(ClassInfo classInfo, MemberTypeInfo memberTypeInfo)
    {
        ClassInfo = classInfo;
        MemberTypeInfo = memberTypeInfo;
        MemberValues = [];
    }

    /// <inheritdoc />
    public override TypeName TypeName => ClassInfo.TypeName;

    /// <summary>
    /// Gets the names of the serialized members.
    /// </summary>
    /// <value>The names of the serialized members.</value>
    public IEnumerable<string> MemberNames => ClassInfo.MemberNames.Keys;

    /// <inheritdoc />
    public override SerializationRecordId Id => ClassInfo.Id;

    internal ClassInfo ClassInfo { get; }

    internal MemberTypeInfo MemberTypeInfo { get; }

    internal int ExpectedValuesCount => MemberTypeInfo.Infos.Count;

    internal List<object?> MemberValues { get; }

    /// <summary>
    /// Checks if member of given name was present in the payload.
    /// </summary>
    /// <param name="memberName">The name of the member.</param>
    /// <returns><see langword="true" /> if it was present, otherwise <see langword="false" />.</returns>
    /// <remarks>
    ///  <para>
    ///   It's recommended to use this method when dealing with payload that may contain
    ///   different versions of the same type.
    ///  </para>
    /// </remarks>
    public bool HasMember(string memberName) => ClassInfo.MemberNames.ContainsKey(memberName);

    /// <summary>
    /// Retrieves the value of the provided <paramref name="memberName"/>.
    /// </summary>
    /// <param name="memberName">The name of the member.</param>
    /// <returns>The value.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="memberName" /> does not refer to a known member. You can use <see cref="HasMember(string)"/> to check if given member exists.</exception>
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
    /// <para>For primitive types like <see cref="int"/>, <see langword="string"/> or <see cref="DateTime"/> returns their value.</para>
    /// <para>For nulls, returns a null.</para>
    /// <para>For other types that are not arrays, returns an instance of <see cref="ClassRecord"/>.</para>
    /// <para>For single-dimensional arrays returns <see cref="SZArrayRecord{T}"/> where the generic type is the primitive type or <see cref="ClassRecord"/>.</para>
    /// <para>For jagged and multi-dimensional arrays, returns an instance of <see cref="ArrayRecord"/>.</para>
    /// </returns>
    /// <inheritdoc cref="GetClassRecord(string)"/>
    public object? GetRawValue(string memberName) => GetMember<object>(memberName);

    /// <inheritdoc cref="GetClassRecord(string)"/>
    public ArrayRecord? GetArrayRecord(string memberName) => GetMember<ArrayRecord>(memberName);

    /// <summary>
    /// Retrieves the <see cref="SerializationRecord" /> of the provided <paramref name="memberName"/>.
    /// </summary>
    /// <param name="memberName">The name of the field.</param>
    /// <returns>The serialization record, which can be any of <see cref="PrimitiveTypeRecord{T}"/>,
    /// <see cref="ClassRecord"/>, <see cref="ArrayRecord"/> or <see langword="null" />.
    /// </returns>
    /// <exception cref="KeyNotFoundException"><paramref name="memberName" /> does not refer to a known member. You can use <see cref="HasMember(string)"/> to check if given member exists.</exception>
    /// <exception cref="InvalidOperationException">The specified member is not a <see cref="SerializationRecord"/>, but just a raw primitive value.</exception>
    public SerializationRecord? GetSerializationRecord(string memberName)
        => MemberValues[ClassInfo.MemberNames[memberName]] switch
        {
            null or NullsRecord => null,
            MemberReferenceRecord referenceRecord => referenceRecord.GetReferencedRecord(),
            SerializationRecord serializationRecord => serializationRecord,
            _ => throw new InvalidOperationException(SR.Format(SR.Serialization_MemberTypeMismatchException, memberName))
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
            : value is not T
                ? throw new InvalidOperationException(SR.Format(SR.Serialization_MemberTypeMismatchException, memberName))
                : (T)value!;
    }

    internal abstract (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType();

    internal override void HandleNextRecord(SerializationRecord nextRecord, NextInfo info)
    {
        // ObjectNullRecord is the only valid null record that can represent class record members,
        // even for multiple nulls provided in a row.
        Debug.Assert(nextRecord is not (ObjectNullMultiple256Record or ObjectNullMultipleRecord));

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
