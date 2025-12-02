// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class DistinctIterator<TSource>
        {
            public override TSource[] ToArray() => ICollectionToArray(new HashSet<TSource>(_source, _comparer));

            public override List<TSource> ToList() => new List<TSource>(new HashSet<TSource>(_source, _comparer));

            public override int GetCount(bool onlyIfCheap) => onlyIfCheap ? -1 : new HashSet<TSource>(_source, _comparer).Count;

            public override TSource? TryGetFirst(out bool found) => _source.TryGetFirst(out found);

            public override bool Contains(TSource value) =>
                // If we're using the default comparer, then source.Distinct().Contains(value) is no different from
                // source.Contains(value), as the Distinct() won't remove anything that could have caused
                // Contains to return true. If, however, there is a custom comparer, Distinct might remove
                // the elements that would have matched, and thus we can't skip it.
                _comparer is null ? _source.Contains(value) :
                base.Contains(value);
        }
    }
}
