// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// An ITypeHandle backed by a real target-process address (MethodTable* or TypeDesc*).
/// </summary>
public readonly struct TargetTypeHandle : ITypeHandle, IEquatable<TargetTypeHandle>
{
    public TargetTypeHandle(TargetPointer address)
    {
        Address = address;
    }

    public TargetPointer Address { get; }
    public bool IsNull => Address == 0;
    public bool IsSynthetic => false;

    public bool Equals(ITypeHandle? other)
        => other is TargetTypeHandle t && Address == t.Address;
    public bool Equals(TargetTypeHandle other) => Address == other.Address;
    public override bool Equals(object? obj)
        => obj is ITypeHandle th && Equals(th);
    public override int GetHashCode() => Address.GetHashCode();
}

/// <summary>
/// A reader-fabricated ITypeHandle for an unloaded constructed type
/// (Ptr, Byref, SzArray, or Array). Has no backing target memory.
/// </summary>
internal sealed class SyntheticTypeHandle : ITypeHandle, IEquatable<SyntheticTypeHandle>
{
    public SyntheticTypeHandle(CorElementType kind, ITypeHandle element, int rank = 0)
    {
        Kind = kind;
        Element = element;
        Rank = rank;
    }

    /// <summary>The outermost CorElementType (Ptr, Byref, SzArray, or Array).</summary>
    public CorElementType Kind { get; }

    /// <summary>The element (referent) ITypeHandle. May itself be synthetic.</summary>
    public ITypeHandle Element { get; }

    /// <summary>Array rank (meaningful only for Array; SzArray is always 1).</summary>
    public int Rank { get; }

    public TargetPointer Address => TargetPointer.Null;
    public bool IsNull => false;
    public bool IsSynthetic => true;

    public bool Equals(ITypeHandle? other) => other is SyntheticTypeHandle s && Equals(s);
    public bool Equals(SyntheticTypeHandle? other)
        => other is not null
           && Kind == other.Kind
           && Rank == other.Rank
           && EqualityComparer<ITypeHandle>.Default.Equals(Element, other.Element);
    public override bool Equals(object? obj) => obj is ITypeHandle th && Equals(th);
    public override int GetHashCode() => HashCode.Combine((int)Kind, Rank, Element?.GetHashCode() ?? 0);
}
