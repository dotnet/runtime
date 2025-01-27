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
///   a <see cref="ClassRecord"/>, or an <see cref="ArrayRecord"/>.
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
    /// <remarks>
    /// Since the provided type name may originate from untrusted input,
    /// it should not be utilized for type loading, as it could potentially load a malicious type.
    /// </remarks>
    public abstract TypeName TypeName { get; }

    /// <summary>
    /// Compares the type name read from the payload against the specified type.
    /// </summary>
    /// <remarks>
    /// <para>This method ignores assembly names.</para>
    /// <para>This method does NOT take into account member names or their types.</para>
    /// </remarks>
    /// <param name="type">The type to compare against.</param>
    /// <returns><see langword="true" /> if the serialized type name matches the provided type; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
    public bool TypeNameMatches(Type type)
    {
#if NET
        ArgumentNullException.ThrowIfNull(type);
#else
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }
#endif

        return Matches(type, TypeName);
    }

    private static bool Matches(Type type, TypeName typeName)
    {
        // We don't need to check for pointers and references to arrays,
        // as it's impossible to serialize them with BF.
        if (type.IsPointer || type.IsByRef)
        {
            return false;
        }

        // The TypeName.FullName property getter is recursive and backed by potentially hostile
        // input. See comments in that property getter for more information, including what defenses
        // are in place to prevent attacks.
        //
        // Note that the equality comparison below is worst-case O(n) since the adversary could ensure
        // that only the last char differs. Even if the strings have equal contents, we should still
        // expect the comparison to take O(n) time since RuntimeType.FullName and TypeName.FullName
        // will never reference the same string instance with current runtime implementations.
        //
        // Since a call to Matches could take place within a loop, and since TypeName.FullName could
        // be arbitrarily long (it's attacker-controlled and the NRBF protocol allows backtracking via
        // the ClassWithId record, providing a form of compression), this presents opportunity
        // for an algorithmic complexity attack, where a (2 * l)-length payload has an l-length type
        // name and an array with l elements, resulting in O(l^2) total work factor. Protection against
        // such attack is provided by the fact that the System.Type object is fully under the app's
        // control and is assumed to be trusted and a reasonable length. This brings the cumulative loop
        // work factor back down to O(l * RuntimeType.FullName), which is acceptable.
        //
        // The above statement assumes that "(string)m == (string)n" has worst-case complexity
        // O(min(m.Length, n.Length)). This is not stated in string's public docs, but it is
        // a guaranteed behavior for all built-in Ordinal string comparisons.

        // At first, check the non-allocating properties for mismatch.
        if (type.IsArray != typeName.IsArray || type.IsConstructedGenericType != typeName.IsConstructedGenericType
            || type.IsNested != typeName.IsNested
            || (type.IsArray && type.GetArrayRank() != typeName.GetArrayRank())
#if NET
            || type.IsSZArray != typeName.IsSZArray // int[] vs int[*]
#else
            || (type.IsArray && type.Name != typeName.Name)
#endif
            )
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
    /// Gets the primitive, string, or null record value.
    /// For reference records, it returns the referenced record.
    /// For other records, it returns the records themselves.
    /// </summary>
    /// <remarks>
    /// Overrides of this method should take care not to allow
    /// the introduction of cycles, even in the face of adversarial
    /// edges in the object graph.
    /// </remarks>
    internal virtual object? GetValue() => this;

    internal virtual void HandleNextRecord(SerializationRecord nextRecord, NextInfo info)
        => throw new InvalidOperationException();

    internal virtual void HandleNextValue(object value, NextInfo info)
        => throw new InvalidOperationException();
}
