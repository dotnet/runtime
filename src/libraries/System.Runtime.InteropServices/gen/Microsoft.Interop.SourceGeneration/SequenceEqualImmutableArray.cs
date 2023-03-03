// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Interop
{
    /// <summary>
    /// This method provides a wrapper for an <see cref="ImmutableArray{T}" /> that overrides the equality operation to provide elementwise comparison.
    /// The default equality operation for an <see cref="ImmutableArray{T}" /> is reference equality of the underlying array, which is too strict
    /// for many scenarios. This wrapper type allows us to use <see cref="ImmutableArray{T}" />s in our other record types without having to write an Equals method
    /// that we may forget to update if we add new elements to the record.
    /// </summary>
    public readonly record struct SequenceEqualImmutableArray<T>(ImmutableArray<T> Array, IEqualityComparer<T> Comparer)
    {
        public SequenceEqualImmutableArray(ImmutableArray<T> array)
            : this(array, EqualityComparer<T>.Default)
        {
        }

        public bool Equals(SequenceEqualImmutableArray<T> other)
        {
            return Array.SequenceEqual(other.Array, Comparer);
        }

        public override int GetHashCode() => throw new UnreachableException();
    }
}
