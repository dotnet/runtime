// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class ShuffleIterator<TSource>
        {
            public override TSource[] ToArray()
            {
                TSource[] array = _source.ToArray();
                Random.Shared.Shuffle(array);
                return array;
            }

            public override List<TSource> ToList()
            {
                List<TSource> list = _source.ToList();
                Random.Shared.Shuffle(CollectionsMarshal.AsSpan(list));
                return list;
            }

            public override int GetCount(bool onlyIfCheap) =>
                !onlyIfCheap ? _source.Count() :
                TryGetNonEnumeratedCount(_source, out int count) ? count :
                -1;

            public override TSource? TryGetFirst(out bool found) =>
                TryGetElementAt(0, out found);

            public override TSource? TryGetLast(out bool found) =>
                TryGetElementAt(0, out found);

            public override TSource? TryGetElementAt(int index, out bool found)
            {
                if (_source is Iterator<TSource> iterator &&
                    iterator.GetCount(onlyIfCheap: true) is int iteratorCount &&
                    iteratorCount >= 0)
                {
                    if ((uint)index < (uint)iteratorCount)
                    {
                        return iterator.TryGetElementAt(Random.Shared.Next(0, iteratorCount), out found);
                    }
                }
                else if (_source is IList<TSource> list)
                {
                    int listCount = list.Count;
                    if ((uint)index < (uint)listCount)
                    {
                        found = true;
                        return list[Random.Shared.Next(0, listCount)];
                    }
                }
                else if (index >= 0)
                {
                    TSource[] array = _source.ToArray();
                    if (index < array.Length)
                    {
                        found = true;
                        return array[Random.Shared.Next(0, array.Length)];
                    }
                }

                found = false;
                return default;
            }
        }
    }
}
