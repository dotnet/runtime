// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class ReverseIterator<TSource> : IPartition<TSource>
        {
            public TSource[] ToArray()
            {
                TSource[] array = _source.ToArray();
                Array.Reverse(array);
                return array;
            }

            public List<TSource> ToList()
            {
                List<TSource> list = _source.ToList();
                list.Reverse();
                return list;
            }

            public int GetCount(bool onlyIfCheap) =>
                !onlyIfCheap ? _source.Count() :
                TryGetNonEnumeratedCount(_source, out int count) ? count :
                -1;

            public TSource? TryGetElementAt(int index, out bool found)
            {
                if (_source is IList<TSource> list)
                {
                    int count = list.Count;
                    if ((uint)index < (uint)count)
                    {
                        found = true;
                        return list[count - index - 1];
                    }
                }
                else if (index >= 0)
                {
                    TSource[] array = _source.ToArray();
                    if (index < array.Length)
                    {
                        found = true;
                        return array[array.Length - index - 1];
                    }
                }

                found = false;
                return default;
            }

            public TSource? TryGetFirst(out bool found)
            {
                if (_source is IPartition<TSource> partition)
                {
                    return partition.TryGetLast(out found);
                }
                else if (_source is IList<TSource> list)
                {
                    int count = list.Count;
                    if (count > 0)
                    {
                        found = true;
                        return list[count - 1];
                    }
                }
                else
                {
                    using IEnumerator<TSource> e = _source.GetEnumerator();
                    if (e.MoveNext())
                    {
                        TSource result;
                        do
                        {
                            result = e.Current;
                        }
                        while (e.MoveNext());

                        found = true;
                        return result;
                    }
                }

                found = false;
                return default;
            }

            public TSource? TryGetLast(out bool found)
            {
                if (_source is IPartition<TSource> partition)
                {
                    return partition.TryGetFirst(out found);
                }
                else if (_source is IList<TSource> list)
                {
                    if (list.Count > 0)
                    {
                        found = true;
                        return list[0];
                    }
                }
                else
                {
                    using IEnumerator<TSource> e = _source.GetEnumerator();
                    if (e.MoveNext())
                    {
                        found = true;
                        return e.Current;
                    }
                }

                found = false;
                return default;
            }

            public IPartition<TSource>? Skip(int count) => new EnumerablePartition<TSource>(this, count, -1);

            public IPartition<TSource>? Take(int count) => new EnumerablePartition<TSource>(this, 0, count - 1);
        }
    }
}
