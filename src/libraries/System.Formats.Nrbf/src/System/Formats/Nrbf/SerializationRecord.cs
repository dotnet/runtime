// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Metadata;
using System.Collections.Immutable;

namespace System.Formats.Nrbf;

/// <summary>
/// Abstract class that represents the serialization record.
/// </summary>
/// <remarks>
///  <para>
///   Every instance returned to the end user can be either <see cref="PrimitiveTypeRecord{T}"/>,
///   a <see cref="ClassRecord"/> or an <see cref="ArrayRecord"/>.
///  </para>
/// </remarks>
[DebuggerDisplay("{RecordType}, {Id}")]
public abstract class SerializationRecord
{
    internal SerializationRecord() // others can't derive from this type
    {
    }

    /// <summary>
    /// Gets the type of the record.
    /// </summary>
    /// <value>The type of the record.</value>
    public abstract SerializationRecordType RecordType { get; }

    /// <summary>
    /// Gets the ID of the record.
    /// </summary>
    /// <value>The ID of the record.</value>
    public abstract SerializationRecordId Id { get; }

    /// <summary>
    /// Gets the name of the serialized type.
    /// </summary>
    /// <value>The name of the serialized type.</value>
    public abstract TypeName TypeName { get; }

    /// <summary>
    /// Compares the type name read from the payload against the specified type.
    /// </summary>
    /// <remarks>
    /// <para>This method ignores assembly names.</para>
    /// <para>This method does NOT take into account member names or their genericTypes.</para>
    /// </remarks>
    /// <param name="type">The type to compare against.</param>
    /// <returns><see langword="true" /> if the serialized type name match provided type; otherwise, <see langword="false" />.</returns>
    public bool TypeNameMatches(Type type) => Matches(type, TypeName);

    private static bool Matches(Type type, TypeName typeName)
    {
        // We don't need to check for pointers and references to arrays,
        // as it's impossible to serialize them with BF.
        if (type.IsPointer || type.IsByRef)
        {
            return false;
        }

        // At first, check the non-allocating properties for mismatch.
        if (type.IsArray != typeName.IsArray || type.IsConstructedGenericType != typeName.IsConstructedGenericType
            || type.IsNested != typeName.IsNested
            || (type.IsArray && type.GetArrayRank() != typeName.GetArrayRank()))
        {
            return false;
        }

        if (type.FullName == typeName.FullName)
        {
            return true; // The happy path with no type forwarding
        }
        else if (typeName.IsArray)
        {
            return Matches(type.GetElementType()!, typeName.GetElementType());
        }
        else if (type.IsConstructedGenericType)
        {
            if (!Matches(type.GetGenericTypeDefinition(), typeName.GetGenericTypeDefinition()))
            {
                return false;
            }

            ImmutableArray<TypeName> genericNames = typeName.GetGenericArguments();
            Type[] genericTypes = type.GetGenericArguments();

            if (genericNames.Length != genericTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < genericTypes.Length; i++)
            {
                if (!Matches(genericTypes[i], genericNames[i]))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the primitive, string or null record value.
    /// For reference records, it returns the referenced record.
    /// For other records, it returns the records themselves.
    /// </summary>
    internal virtual object? GetValue() => this;

    internal virtual void HandleNextRecord(SerializationRecord nextRecord, NextInfo info)
        => Debug.Fail($"HandleNextRecord should not have been called for '{GetType().Name}'");

    internal virtual void HandleNextValue(object value, NextInfo info)
        => Debug.Fail($"HandleNextValue should not have been called for '{GetType().Name}'");
}
