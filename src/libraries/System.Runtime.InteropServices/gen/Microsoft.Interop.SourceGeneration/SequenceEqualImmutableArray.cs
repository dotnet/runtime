// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Interop
{
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
