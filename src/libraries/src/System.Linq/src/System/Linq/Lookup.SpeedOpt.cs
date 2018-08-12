// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    public partial class Lookup<TKey, TElement> : IIListProvider<IGrouping<TKey, TElement>>
    {
        IGrouping<TKey, TElement>[] IIListProvider<IGrouping<TKey, TElement>>.ToArray()
        {
            IGrouping<TKey, TElement>[] array = new IGrouping<TKey, TElement>[_count];
            int index = 0;
            Grouping<TKey, TElement> g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g._next;
                    array[index] = g;
                    ++index;
                }
                while (g != _lastGrouping);
            }

            return array;
        }

        internal TResult[] ToArray<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            TResult[] array = new TResult[_count];
            int index = 0;
            Grouping<TKey, TElement> g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g._next;
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
            List<IGrouping<TKey, TElement>> list = new List<IGrouping<TKey, TElement>>(_count);
            Grouping<TKey, TElement> g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g._next;
                    list.Add(g);
                }
                while (g != _lastGrouping);
            }

            return list;
        }

        int IIListProvider<IGrouping<TKey, TElement>>.GetCount(bool onlyIfCheap) => _count;
    }
}
