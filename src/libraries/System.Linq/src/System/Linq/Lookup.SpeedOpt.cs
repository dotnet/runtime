// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public partial class Lookup<TKey, TElement> : IIListProvider<IGrouping<TKey, TElement>>
    {
        IGrouping<TKey, TElement>[] IIListProvider<IGrouping<TKey, TElement>>.ToArray()
        {
            IGrouping<TKey, TElement>[] array;
            if (_count > 0)
            {
                array = new IGrouping<TKey, TElement>[_count];
                Fill(_lastGrouping, array);
            }
            else
            {
                array = [];
            }
            return array;
        }

        internal TResult[] ToArray<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            TResult[] array = new TResult[_count];
            int index = 0;
            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g._next;
                    Debug.Assert(g != null);

                    g.Trim();
                    array[index] = resultSelector(g._key, g._elements);
                    ++index;
                }
                while (g != _lastGrouping);
            }

            return array;
        }

        List<IGrouping<TKey, TElement>> IIListProvider<IGrouping<TKey, TElement>>.ToList()
        {
            var list = new List<IGrouping<TKey, TElement>>(_count);
            if (_count > 0)
            {
                Fill(_lastGrouping, Enumerable.SetCountAndGetSpan(list, _count));
            }

            return list;
        }

        private static void Fill(Grouping<TKey, TElement>? lastGrouping, Span<IGrouping<TKey, TElement>> results)
        {
            int index = 0;
            Grouping<TKey, TElement>? g = lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g._next;
                    Debug.Assert(g != null);

                    results[index] = g;
                    ++index;
                }
                while (g != lastGrouping);
            }

            Debug.Assert(index == results.Length, "All list elements were not initialized.");
        }

        int IIListProvider<IGrouping<TKey, TElement>>.GetCount(bool onlyIfCheap) => _count;
    }
}
