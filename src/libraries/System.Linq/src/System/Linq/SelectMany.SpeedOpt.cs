// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class SelectManySingleSelectorIterator<TSource, TResult> : IIListProvider<TResult>
        {
            public int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap)
                {
                    return -1;
                }

                int count = 0;

                foreach (TSource element in _source)
                {
                    checked
                    {
                        count += _selector(element).Count();
                    }
                }

                return count;
            }

            public TResult[] ToArray()
            {
                var builder = new SparseArrayBuilder<TResult>(initialize: true);
                var deferredCopies = new ArrayBuilder<IEnumerable<TResult>>();

                foreach (TSource element in _source)
                {
                    IEnumerable<TResult> enumerable = _selector(element);

                    if (builder.ReserveOrAdd(enumerable))
                    {
                        deferredCopies.Add(enumerable);
                    }
                }

                TResult[] array = builder.ToArray();

                ArrayBuilder<Marker> markers = builder.Markers;
                for (int i = 0; i < markers.Count; i++)
                {
                    Marker marker = markers[i];
                    IEnumerable<TResult> enumerable = deferredCopies[i];
                    EnumerableHelpers.Copy(enumerable, array, marker.Index, marker.Count);
                }

                return array;
            }

            public List<TResult> ToList()
            {
                var list = new List<TResult>();

                foreach (TSource element in _source)
                {
                    list.AddRange(_selector(element));
                }

                return list;
            }
        }
    }
}
