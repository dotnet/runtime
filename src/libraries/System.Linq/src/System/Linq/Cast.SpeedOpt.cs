// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class CastICollectionIterator<TResult>
        {
            public override int GetCount(bool onlyIfCheap) => _source.Count;

            public override TResult[] ToArray()
            {
                TResult[] array = new TResult[_source.Count];

                int index = 0;
                foreach (TResult item in _source)
                {
                    array[index++] = item;
                }

                return array;
            }

            public override List<TResult> ToList()
            {
                List<TResult> list = new(_source.Count);

                foreach (TResult item in _source)
                {
                    list.Add(item);
                }

                return list;
            }

            public override TResult? TryGetElementAt(int index, out bool found)
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

            public override TResult? TryGetFirst(out bool found)
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

            public override TResult? TryGetLast(out bool found)
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
        }
    }
}
