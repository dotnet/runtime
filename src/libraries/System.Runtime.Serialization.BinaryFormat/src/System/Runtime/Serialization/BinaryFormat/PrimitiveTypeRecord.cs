// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.Serialization.BinaryFormat;

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
#if SYSTEM_RUNTIME_SERIALIZATION_BINARYFORMAT
public
#else
internal
#endif
abstract class PrimitiveTypeRecord<T> : SerializationRecord
{
    private protected PrimitiveTypeRecord(T value) => Value = value;

    public T Value { get; }

    public override bool IsTypeNameMatching(Type type) => type == typeof(T);

    internal override object? GetValue() => Value;
}
