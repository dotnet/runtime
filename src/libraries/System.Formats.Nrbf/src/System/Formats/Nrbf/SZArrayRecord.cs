// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Formats.Nrbf.Utils;

namespace System.Formats.Nrbf;

/// <summary>
/// Defines the core behavior for NRBF single dimensional, zero-indexed array records and provides a base for derived classes.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class SZArrayRecord<T> : ArrayRecord
{
    private protected SZArrayRecord(ArrayInfo arrayInfo) : base(arrayInfo)
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
