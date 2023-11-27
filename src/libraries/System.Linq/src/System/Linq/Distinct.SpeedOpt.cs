// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class DistinctIterator<TSource> : IIListProvider<TSource>
        {
            public TSource[] ToArray() => Enumerable.HashSetToArray(new HashSet<TSource>(_source, _comparer));

            public List<TSource> ToList() => new List<TSource>(new HashSet<TSource>(_source, _comparer));

            public int GetCount(bool onlyIfCheap) => onlyIfCheap ? -1 : new HashSet<TSource>(_source, _comparer).Count;
        }
    }
}
