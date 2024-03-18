// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private abstract partial class UnionIterator<TSource>
        {
            private HashSet<TSource> FillSet()
            {
                var set = new HashSet<TSource>(_comparer);
                for (int index = 0; ; ++index)
                {
                    IEnumerable<TSource>? enumerable = GetEnumerable(index);
                    if (enumerable is null)
                    {
                        return set;
                    }

                    set.UnionWith(enumerable);
                }
            }

            public override TSource[] ToArray() => HashSetToArray(FillSet());

            public override List<TSource> ToList() => new List<TSource>(FillSet());

            public override int GetCount(bool onlyIfCheap) => onlyIfCheap ? -1 : FillSet().Count;

            public override TSource? TryGetFirst(out bool found)
            {
                IEnumerable<TSource>? source;
                for (int i = 0; (source = GetEnumerable(i)) is not null; i++)
                {
                    TSource? result = source.TryGetFirst(out found);
                    if (found)
                    {
                        return result;
                    }
                }

                found = false;
                return default;
            }
        }
    }
}
