// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private abstract partial class UnionIterator<TSource> : IIListProvider<TSource>
        {
            private HashSet<TSource> FillSet()
            {
                var set = new HashSet<TSource>(_comparer);
                for (int index = 0; ; ++index)
                {
                    IEnumerable<TSource>? enumerable = GetEnumerable(index);
                    if (enumerable == null)
                    {
                        return set;
                    }

                    set.UnionWith(enumerable);
                }
            }

            public TSource[] ToArray() => Enumerable.HashSetToArray(FillSet());

            public List<TSource> ToList() => Enumerable.HashSetToList(FillSet());

            public int GetCount(bool onlyIfCheap) => onlyIfCheap ? -1 : FillSet().Count;
        }
    }
}
