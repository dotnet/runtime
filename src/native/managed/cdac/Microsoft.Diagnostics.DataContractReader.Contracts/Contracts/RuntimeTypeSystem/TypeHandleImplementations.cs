// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// An ITypeHandle backed by a real target-process address (MethodTable* or TypeDesc*).
/// </summary>
public sealed class TargetTypeHandle : ITypeHandle, IEquatable<TargetTypeHandle>
{
    public TargetTypeHandle(TargetPointer address)
    {
        Address = address;
    }

    public TargetPointer Address { get; }
    public bool IsNull => Address == 0;

    public bool Equals(ITypeHandle? other)
        => other is not null && Address == other.Address;
    public bool Equals(TargetTypeHandle? other)
        => other is not null && Address == other.Address;
    public override bool Equals(object? obj)
        => obj is ITypeHandle th && Equals(th);
    public override int GetHashCode() => Address.GetHashCode();
}
