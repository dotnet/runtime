// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    switch (_source)
                    {
                        case IIListProvider<TSource> listProv:
                            return listProv.GetCount(onlyIfCheap: true);

                        case ICollection<TSource> colT:
                            return colT.Count;

                        case ICollection col:
                            return col.Count;

                        default:
                            return -1;
                    }
                }

                return _source.Count();
            }
        }
    }
}
