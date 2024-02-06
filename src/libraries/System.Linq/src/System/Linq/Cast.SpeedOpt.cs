// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class CastICollectionIterator<TResult> : IPartition<TResult>
        {
            public int GetCount(bool onlyIfCheap) => Count;

            public TResult[] ToArray()
            {
                TResult[] array = new TResult[Count];

                int index = 0;
                foreach (TResult item in _source)
                {
                    array[index++] = item;
                }

                return array;
            }

            public List<TResult> ToList()
            {
                List<TResult> list = new(Count);

                foreach (TResult item in _source)
                {
                    list.Add(item);
                }

                return list;
            }

            public TResult? TryGetElementAt(int index, out bool found)
            {
                if (index >= 0)
                {
                    IEnumerator e = _source.GetEnumerator();
                    try
                    {
                        while (e.MoveNext())
                        {
                            if (index == 0)
                            {
                                found = true;
                                return (TResult)e.Current;
                            }

                            index--;
                        }
                    }
                    finally
                    {
                        (e as IDisposable)?.Dispose();
                    }
                }

                found = false;
                return default;
            }

            public TResult? TryGetFirst(out bool found)
            {
                IEnumerator e = _source.GetEnumerator();
                try
                {
                    if (e.MoveNext())
                    {
                        found = true;
                        return (TResult)e.Current;
                    }
                }
                finally
                {
                    (e as IDisposable)?.Dispose();
                }

                found = false;
                return default;
            }

            public TResult? TryGetLast(out bool found)
            {
                IEnumerator e = _source.GetEnumerator();
                try
                {
                    if (e.MoveNext())
                    {
                        TResult last = (TResult)e.Current;
                        while (e.MoveNext())
                        {
                            last = (TResult)e.Current;
                        }

                        found = true;
                        return last;
                    }

                    found = false;
                    return default;
                }
                finally
                {
                    (e as IDisposable)?.Dispose();
                }
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new SelectIPartitionIterator<TResult, TResult2>(this, selector);

            public IPartition<TResult>? Skip(int count) => new EnumerablePartition<TResult>(this, count, -1);

            public IPartition<TResult>? Take(int count) => new EnumerablePartition<TResult>(this, 0, count - 1);
        }
    }
}
