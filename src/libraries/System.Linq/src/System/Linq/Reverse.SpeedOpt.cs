// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class ReverseIterator<TSource> : IIListProvider<TSource>
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

            public int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap)
                {
                    return _source switch
                    {
                        IIListProvider<TSource> listProv => listProv.GetCount(onlyIfCheap: true),
                        ICollection<TSource> colT => colT.Count,
                        ICollection col => col.Count,
                        _ => -1,
                    };
                }

                return _source.Count();
            }
        }
    }
}
