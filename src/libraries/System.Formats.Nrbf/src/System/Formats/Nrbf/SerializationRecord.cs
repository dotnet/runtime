// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

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
    public abstract RecordType RecordType { get; }

    /// <summary>
    /// Gets the ID of the record.
    /// </summary>
    /// <value>The ID of the record.</value>
    public abstract SerializationRecordId Id { get; }

    /// <summary>
    /// Compares the type and assembly name read from the payload against the specified type.
    /// </summary>
    /// <remarks>
    /// <para>This method takes type forwarding into account.</para>
    /// <para>This method does NOT take into account member names or their types.</para>
    /// </remarks>
    /// <param name="type">The type to compare against.</param>
    /// <returns><see langword="true" /> if the serialized type and assembly name match provided type; otherwise, <see langword="false" />.</returns>
    public virtual bool IsTypeNameMatching(Type type) => false;

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
