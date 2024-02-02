// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class DistinctIterator<TSource> : IIListProvider<TSource>
        {
            public TSource[] ToArray()
            {
                if (TryGetNonEnumeratedCount(_source, out int count) && count < 2)
                {
                    if (count == 1)
                    {
                        return [_source.First()];
                    }

                    return [];
                }

                return HashSetToArray(new HashSet<TSource>(_source, _comparer));
            }

            public List<TSource> ToList()
            {
                if (TryGetNonEnumeratedCount(_source, out int count) && count < 2)
                {
                    if (count == 1)
                    {
                        return new List<TSource>(1) { _source.First() };
                    }

                    return [];
                }

                return new List<TSource>(new HashSet<TSource>(_source, _comparer));
            }

            public int GetCount(bool onlyIfCheap)
            {
                if (TryGetNonEnumeratedCount(_source, out int count) && count < 2)
                {
                    return count;
                }

                if (onlyIfCheap)
                {
                    return -1;
                }

                return new HashSet<TSource>(_source, _comparer).Count;
            }
        }
    }
}
