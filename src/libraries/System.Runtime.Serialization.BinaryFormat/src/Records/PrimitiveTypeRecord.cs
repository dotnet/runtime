// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
/// A record that represents the primitive value of <typeparamref name="T"/> type.
/// </summary>
/// <typeparam name="T">Primitive type.</typeparam>
/// <remarks>
/// <para>
/// <typeparamref name="T"/> can be one of the following types:
/// <seealso cref="string"/>, <seealso cref="bool"/>, <seealso cref="byte"/>, <seealso cref="sbyte"/>
/// <seealso cref="char"/>, <seealso cref="short"/>, <seealso cref="ushort"/>,
/// <seealso cref="int"/>, <seealso cref="uint"/>, <seealso cref="long"/>, <seealso cref="ulong"/>,
/// <seealso cref="float"/>, <seealso cref="double"/>, <seealso cref="decimal"/>,
/// <seealso cref="DateTime"/> or <seealso cref="TimeSpan"/>.
/// </para>
/// <para>Other serialization records are represented with <seealso cref="ClassRecord"/> or <seealso cref="ArrayRecord"/>.</para>
/// </remarks>
[DebuggerDisplay("{Value}")]
public abstract class PrimitiveTypeRecord<T> : SerializationRecord
{
    private protected PrimitiveTypeRecord(T value) => Value = value;

    public T Value { get; }

    public override bool IsTypeNameMatching(Type type) => type == typeof(T);

    internal override object? GetValue() => Value;
}
