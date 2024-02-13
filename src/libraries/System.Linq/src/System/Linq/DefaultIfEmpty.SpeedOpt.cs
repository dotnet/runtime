// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class DefaultIfEmptyIterator<TSource> : IIListProvider<TSource>
        {
            public TSource[] ToArray()
            {
                TSource[] array = _source.ToArray();
                return array.Length == 0 ? [_default] : array;
            }

            public List<TSource> ToList()
            {
                List<TSource> list = _source.ToList();
                if (list.Count == 0)
                {
                    list.Add(_default);
                }

                return list;
            }

            public int GetCount(bool onlyIfCheap)
            {
                int count;
                if (!onlyIfCheap || _source is ICollection<TSource> || _source is ICollection)
                {
                    count = _source.Count();
                }
                else
                {
                    count = _source is IIListProvider<TSource> listProv ? listProv.GetCount(onlyIfCheap: true) : -1;
                }

                return count == 0 ? 1 : count;
            }
        }
    }
}
