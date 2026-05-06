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

            public override TSource[] ToArray() => ICollectionToArray(FillSet());

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

            public override bool Contains(TSource value)
            {
                // If there's no comparer, then source1.Union(source2).Contains(value) is no different from
                // source1.Contains(value) || source2.Contains(value), as Union's set semantics won't remove
                // anything from either that could have matched. However, if there is a comparer, it's possible
                // the Union could end up removing items that would have matched, and thus we can't skip it.
                if (_comparer is null)
                {
                    IEnumerable<TSource>? source;
                    for (int i = 0; (source = GetEnumerable(i)) is not null; i++)
                    {
                        if (source.Contains(value))
                        {
                            return true;
                        }
                    }

                    return false;
                }


                return base.Contains(value);
            }
        }
    }
}
