// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Resources.Extensions;

/// <summary>
///  Identifier struct.
/// </summary>
internal readonly struct Id : IEquatable<Id>
{
    private readonly int _id;
    private readonly bool _isNull = true;

    // It is possible that the id may be negative with value types. See BinaryObjectWriter.InternalGetId.
    private Id(int id)
    {
        _id = id;
        _isNull = false;
    }

    private Id(bool isNull)
    {
        _id = 0;
        _isNull = isNull;
    }

    public static Id Null => new(isNull: true);
    public bool IsNull => _isNull;

    public static implicit operator int(Id value) => value._isNull ? throw new InvalidOperationException() : value._id;
    public static implicit operator Id(int value) => new(value);

    public override bool Equals([NotNullWhen(true)] object? obj)
        => (obj is Id id && Equals(id)) || (obj is int value && value == _id);

    public bool Equals(Id other) => _isNull == other._isNull && _id == other._id;

    public override readonly int GetHashCode() => _id.GetHashCode();
    public override readonly string ToString() => _isNull ? "<null>" : _id.ToString();

    public static bool operator ==(Id left, Id right) => left.Equals(right);

    public static bool operator !=(Id left, Id right) => !(left == right);
}
