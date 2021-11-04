// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class BaseConcatIterator<TSource> : ConcatIterator<TSource>
        {
            public override int GetCount(bool onlyIfCheap)
            {
                int count, totalCount;
                if (!_first.TryGetNonEnumeratedCount(out totalCount))
                {
                    if (onlyIfCheap)
                    {
                        return -1;
                    }

                    totalCount = _first.Count();
                }

                if (!_second.TryGetNonEnumeratedCount(out count))
                {
                    if (onlyIfCheap)
                    {
                        return -1;
                    }

                    count = _second.Count();
                }

                checked { totalCount += count; }

                foreach (IEnumerable<TSource> segment in _rest)
                {
                    if (!segment.TryGetNonEnumeratedCount(out count))
                    {
                        if (onlyIfCheap)
                        {
                            return -1;
                        }

                        count = _second.Count();
                    }

                    checked { totalCount += count; }
                }

                return totalCount;
            }

            public override TSource[] ToArray()
            {
                if (_rest.Length > 0)
                {
                    return LazyToArray();
                }

                var builder = new SparseArrayBuilder<TSource>(initialize: true);

                bool reservedFirst = builder.ReserveOrAdd(_first);
                bool reservedSecond = builder.ReserveOrAdd(_second);

                TSource[] array = builder.ToArray();

                if (reservedFirst)
                {
                    Marker marker = builder.Markers.First();
                    Debug.Assert(marker.Index == 0);
                    EnumerableHelpers.Copy(_first, array, 0, marker.Count);
                }

                if (reservedSecond)
                {
                    Marker marker = builder.Markers.Last();
                    EnumerableHelpers.Copy(_second, array, marker.Index, marker.Count);
                }

                return array;
            }
        }

        private sealed partial class ChainedConcatIterator<TSource> : ConcatIterator<TSource>
        {
            public override int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap && !_hasOnlyCollections)
                {
                    return -1;
                }

                int count = 0;
                ChainedConcatIterator<TSource>? node, previousN = this;

                do
                {
                    node = previousN;
                    IEnumerable<TSource> source = node._head;

                    // Enumerable.Count() handles ICollections in O(1) time, but check for them here anyway
                    // to avoid a method call because 1) they're common and 2) this code is run in a loop.
                    var collection = source as ICollection<TSource>;
                    Debug.Assert(!_hasOnlyCollections || collection != null);
                    int sourceCount = collection?.Count ?? source.Count();

                    checked
                    {
                        count += sourceCount;
                    }
                }
                while ((previousN = node.PreviousN) != null);

                Debug.Assert(node._tail is BaseConcatIterator<TSource>);
                return checked(count + node._tail.GetCount(onlyIfCheap));
            }

            public override TSource[] ToArray() => _hasOnlyCollections ? PreallocatingToArray() : LazyToArray();

            private TSource[] PreallocatingToArray()
            {
                // If there are only ICollections in this iterator, then we can just get the count, preallocate the
                // array, and copy them as we go. This has better time complexity than continuously re-walking the
                // linked list via GetEnumerable, and better memory usage than buffering the collections.

                Debug.Assert(_hasOnlyCollections);

                int count = GetCount(onlyIfCheap: true);
                Debug.Assert(count >= 0);

                if (count == 0)
                {
                    return Array.Empty<TSource>();
                }

                var array = new TSource[count];
                int arrayIndex = array.Length; // We start copying in collection-sized chunks from the end of the array.

                ChainedConcatIterator<TSource>? node, previousN = this;
                do
                {
                    node = previousN;
                    ICollection<TSource> source = (ICollection<TSource>)node._head;
                    int sourceCount = source.Count;
                    if (sourceCount > 0)
                    {
                        checked
                        {
                            arrayIndex -= sourceCount;
                        }
                        source.CopyTo(array, arrayIndex);
                    }
                }
                while ((previousN = node.PreviousN) != null);

                var previous2 = (BaseConcatIterator<TSource>)node._tail;
                var second = (ICollection<TSource>)previous2._second;
                int secondCount = second.Count;

                if (secondCount > 0)
                {
                    second.CopyTo(array, checked(arrayIndex - secondCount));
                }

                if (arrayIndex > secondCount)
                {
                    var first = (ICollection<TSource>)previous2._first;
                    first.CopyTo(array, 0);
                }

                return array;
            }
        }

        private abstract partial class ConcatIterator<TSource> : IIListProvider<TSource>
        {
            public abstract int GetCount(bool onlyIfCheap);

            public abstract TSource[] ToArray();

            public List<TSource> ToList()
            {
                int count = GetCount(onlyIfCheap: true);
                var list = count != -1 ? new List<TSource>(count) : new List<TSource>();

                for (int i = 0; ; i++)
                {
                    IEnumerable<TSource>? source = GetEnumerable(i);
                    if (source == null)
                    {
                        break;
                    }

                    list.AddRange(source);
                }

                return list;
            }

            protected TSource[] LazyToArray()
            {
                var builder = new SparseArrayBuilder<TSource>(initialize: true);
                ArrayBuilder<int> deferredCopies = default;

                for (int i = 0; ; i++)
                {
                    // Unfortunately, for the ChainedConcatIterator case we can't escape re-walking the linked list for each source,
                    // which has quadratic behavior, because we need to add the sources in order.
                    // On the bright side, the bottleneck will usually be iterating, buffering, and copying
                    // each of the enumerables, so this shouldn't be a noticeable perf hit for most scenarios.

                    IEnumerable<TSource>? source = GetEnumerable(i);
                    if (source == null)
                    {
                        break;
                    }

                    if (builder.ReserveOrAdd(source))
                    {
                        deferredCopies.Add(i);
                    }
                }

                TSource[] array = builder.ToArray();

                ArrayBuilder<Marker> markers = builder.Markers;
                for (int i = 0; i < markers.Count; i++)
                {
                    Marker marker = markers[i];
                    IEnumerable<TSource> source = GetEnumerable(deferredCopies[i])!;
                    EnumerableHelpers.Copy(source, array, marker.Index, marker.Count);
                }

                return array;
            }
        }
    }
}
