// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class DefaultIfEmptyIterator<TSource>
        {
            public override TSource[] ToArray()
            {
                TSource[] array = _source.ToArray();
                return array.Length == 0 ? [_default] : array;
            }

            public override List<TSource> ToList()
            {
                List<TSource> list = _source.ToList();
                if (list.Count == 0)
                {
                    list.Add(_default);
                }

                return list;
            }

            public override int GetCount(bool onlyIfCheap)
            {
                int count;
                if (!onlyIfCheap || _source is ICollection<TSource> || _source is ICollection)
                {
                    count = _source.Count();
                }
                else
                {
                    count = _source is Iterator<TSource> iterator ? iterator.GetCount(onlyIfCheap: true) : -1;
                }

                return count == 0 ? 1 : count;
            }

            public override TSource? TryGetFirst(out bool found)
            {
                TSource? first = _source.TryGetFirst(out found);
                if (found)
                {
                    return first;
                }

                found = true;
                return default;
            }

            public override TSource? TryGetLast(out bool found)
            {
                TSource? last = _source.TryGetLast(out found);
                if (found)
                {
                    return last;
                }

                found = true;
                return default;
            }

            public override TSource? TryGetElementAt(int index, out bool found)
            {
                TSource? item = _source.TryGetElementAt(index, out found);
                if (found)
                {
                    return item;
                }

                if (index == 0)
                {
                    found = true;
                }

                return default;
            }
        }
    }
}
