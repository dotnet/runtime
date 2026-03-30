// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file defines the IReadOnlySet<T> interface for downlevel targets
// where it doesn't exist. Unlike extension()-based polyfills, this uses
// #if !NET because the entire type is missing on netstandard2.0 — there
// is nothing to extend.

#if !NET

namespace System.Collections.Generic
{
    internal interface IReadOnlySet<T> : IReadOnlyCollection<T>
    {
        bool Contains(T item);
        bool IsProperSubsetOf(IEnumerable<T> other);
        bool IsProperSupersetOf(IEnumerable<T> other);
        bool IsSubsetOf(IEnumerable<T> other);
        bool IsSupersetOf(IEnumerable<T> other);
        bool Overlaps(IEnumerable<T> other);
        bool SetEquals(IEnumerable<T> other);
    }
}

#endif
