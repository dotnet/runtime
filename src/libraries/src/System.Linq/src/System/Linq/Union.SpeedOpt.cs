// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private abstract partial class UnionIterator<TSource> : IIListProvider<TSource>
        {
            private Set<TSource> FillSet()
            {
                var set = new Set<TSource>(_comparer);
                for (int index = 0; ; ++index)
                {
                    IEnumerable<TSource> enumerable = GetEnumerable(index);
                    if (enumerable == null)
                    {
                        return set;
                    }

                    set.UnionWith(enumerable);
                }
            }

            public TSource[] ToArray() => FillSet().ToArray();

            public List<TSource> ToList() => FillSet().ToList();

            public int GetCount(bool onlyIfCheap) => onlyIfCheap ? -1 : FillSet().Count;
        }
    }
}
