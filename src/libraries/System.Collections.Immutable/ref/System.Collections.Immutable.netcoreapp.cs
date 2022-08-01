// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Collections.Immutable
{
    public readonly partial struct ImmutableArray<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Collections.Immutable.IImmutableList<T>, System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IEquatable<System.Collections.Immutable.ImmutableArray<T>>
    {
        public System.ReadOnlySpan<T> AsSpan(System.Range range) { throw null; }
    }
}
