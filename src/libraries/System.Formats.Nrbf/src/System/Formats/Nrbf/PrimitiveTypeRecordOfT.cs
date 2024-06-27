// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Nrbf.Utils;
using System.Reflection.Metadata;

namespace System.Formats.Nrbf;

/// <summary>
/// Represents a record that itself represents the primitive value of <typeparamref name="T"/> type.
/// </summary>
/// <typeparam name="T">The type of the primitive value.</typeparam>
/// <remarks>
/// <para>
/// The NRBF specification considers the following types to be primitive:
/// <see cref="string"/>, <see cref="bool"/>, <see cref="byte"/>, <see cref="sbyte"/>
/// <see cref="char"/>, <see cref="short"/>, <see cref="ushort"/>,
/// <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>,
/// <see cref="float"/>, <see cref="double"/>, <see cref="decimal"/>,
/// <see cref="DateTime"/> and <see cref="TimeSpan"/>.
/// </para>
/// <para>Other serialization records are represented with <see cref="ClassRecord"/> or <see cref="ArrayRecord"/>.</para>
/// </remarks>
[DebuggerDisplay("{Value}")]
public abstract class PrimitiveTypeRecord<T> : PrimitiveTypeRecord
{
    private static TypeName? s_typeName;

    private protected PrimitiveTypeRecord(T value) => Value = value;

    /// <summary>
    /// Gets the serialized primitive value.
    /// </summary>
    /// <value>The primitive value.</value>
    public new T Value { get; }

    /// <inheritdoc />
    public override TypeName TypeName
        => s_typeName ??= TypeName.Parse(typeof(T).FullName.AsSpan()).WithCoreLibAssemblyName();

    internal override object? GetValue() => Value;
}
