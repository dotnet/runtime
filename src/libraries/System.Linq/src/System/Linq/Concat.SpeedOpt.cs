// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class Concat2Iterator<TSource> : ConcatIterator<TSource>
        {
            public override int GetCount(bool onlyIfCheap)
            {
                int firstCount, secondCount;
                if (!_first.TryGetNonEnumeratedCount(out firstCount))
                {
                    if (onlyIfCheap)
                    {
                        return -1;
                    }

                    firstCount = _first.Count();
                }

                if (!_second.TryGetNonEnumeratedCount(out secondCount))
                {
                    if (onlyIfCheap)
                    {
                        return -1;
                    }

                    secondCount = _second.Count();
                }

                return checked(firstCount + secondCount);
            }

            public override TSource[] ToArray()
            {
                ICollection<TSource>? firstCollection = _first as ICollection<TSource>;
                ICollection<TSource>? secondCollection = _second as ICollection<TSource>;

                if (firstCollection is not null && secondCollection is not null)
                {
                    // Both sources are ICollection<T>, so we know their sizes and can just copy them.
                    int firstCount = firstCollection.Count;
                    TSource[] result = new TSource[checked(firstCount + secondCollection.Count)];

                    firstCollection.CopyTo(result, 0);
                    secondCollection.CopyTo(result, firstCount);

                    return result;
                }
                else
                {
                    // We don't know the sizes of at least one if not both sources, so we need a builder.
                    // If we don't know the sizes of both, we'll just append each into the builder and
                    // use the builder to create the overall array. If we know the size of one, we'll
                    // only buffer the other.
                    SegmentedArrayBuilder<TSource>.ScratchBuffer scratch = default;
                    SegmentedArrayBuilder<TSource> builder = new(scratch);
                    TSource[] result;

                    if (firstCollection is not null)
                    {
                        int firstCount = firstCollection.Count;
                        builder.AddNonICollectionRange(_second);
                        result = new TSource[checked(firstCount + builder.Count)];
                        firstCollection.CopyTo(result, 0);
                        builder.ToSpan(result.AsSpan(firstCount));
                    }
                    else if (secondCollection is not null)
                    {
                        int secondCount = secondCollection.Count;
                        builder.AddNonICollectionRange(_first);
                        result = new TSource[checked(builder.Count + secondCount)];
                        builder.ToSpan(result);
                        secondCollection.CopyTo(result, result.Length - secondCount);
                    }
                    else
                    {
                        builder.AddNonICollectionRange(_first);
                        builder.AddNonICollectionRange(_second);
                        result = builder.ToArray();
                    }

                    builder.Dispose();
                    return result;
                }
            }

            public override TSource? TryGetElementAt(int index, out bool found)
            {
                if (index >= 0)
                {
                    foreach (IEnumerable<TSource> source in (ReadOnlySpan<IEnumerable<TSource>>)[_first, _second])
                    {
                        if (TryGetNonEnumeratedCount(source, out int count))
                        {
                            if (index < count)
                            {
                                found = true;
                                return source.ElementAt(index);
                            }

                            index -= count;
                        }
                        else
                        {
                            using IEnumerator<TSource> e = source.GetEnumerator();
                            while (e.MoveNext())
                            {
                                if (index == 0)
                                {
                                    found = true;
                                    return e.Current;
                                }

                                index--;
                            }
                        }
                    }
                }

                found = false;
                return default;
            }

            public override TSource? TryGetFirst(out bool found)
            {
                TSource? result = _first.TryGetFirst(out found);
                if (!found)
                {
                    result = _second.TryGetFirst(out found);
                }

                return result;
            }

            public override TSource? TryGetLast(out bool found)
            {
                TSource? result = _second.TryGetLast(out found);
                if (!found)
                {
                    result = _first.TryGetLast(out found);
                }

                return result;
            }
        }

        private sealed partial class ConcatNIterator<TSource> : ConcatIterator<TSource>
        {
            public override int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap && !_hasOnlyCollections)
                {
                    return -1;
                }

                int count = 0;
                ConcatNIterator<TSource>? node, previousN = this;

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

                Debug.Assert(node._tail is Concat2Iterator<TSource>);
                return checked(count + node._tail.GetCount(onlyIfCheap));
            }

            public override TSource[] ToArray() => _hasOnlyCollections ? PreallocatingToArray() : LazyToArray();

            private TSource[] LazyToArray()
            {
                // All of the sources being ICollection<T> is handled by PreallocatingToArray, so if we're here,
                // at least one source isn't an ICollection<T>.
                Debug.Assert(!_hasOnlyCollections);

                SegmentedArrayBuilder<TSource>.ScratchBuffer scratch = default;
                SegmentedArrayBuilder<TSource> builder = new(scratch);

                for (int i = 0; ; i++)
                {
                    // Unfortunately, we can't escape re-walking the linked list for each source, which has
                    // quadratic behavior, because we need to add the sources in order.
                    // On the bright side, the bottleneck will usually be iterating, buffering, and copying
                    // each of the enumerables, so this shouldn't be a noticeable perf hit for most scenarios.
                    IEnumerable<TSource>? source = GetEnumerable(i);
                    if (source == null)
                    {
                        break;
                    }

                    builder.AddRange(source);
                }

                TSource[] result = builder.ToArray();
                builder.Dispose();

                return result;
            }

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
                    return [];
                }

                var array = new TSource[count];
                int arrayIndex = array.Length; // We start copying in collection-sized chunks from the end of the array.

                ConcatNIterator<TSource>? node, previousN = this;
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

                var previous2 = (Concat2Iterator<TSource>)node._tail;
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

            public override TSource? TryGetElementAt(int index, out bool found)
            {
                if (index >= 0)
                {
                    IEnumerable<TSource>? source;
                    for (int i = 0; (source = GetEnumerable(i)) is not null; i++)
                    {
                        if (TryGetNonEnumeratedCount(source, out int count))
                        {
                            if (index < count)
                            {
                                found = true;
                                return source.ElementAt(index);
                            }

                            index -= count;
                        }
                        else
                        {
                            using IEnumerator<TSource> e = source.GetEnumerator();
                            while (e.MoveNext())
                            {
                                if (index == 0)
                                {
                                    found = true;
                                    return e.Current;
                                }

                                index--;
                            }
                        }
                    }
                }

                found = false;
                return default;
            }

            public override TSource? TryGetFirst(out bool found)
            {
                IEnumerable<TSource>? source;
                for (int i = 0; (source = GetEnumerable(i)) is not null; i++)
                {
                    TSource? result = source.TryGetFirst(out found);
                    if (found)
                    {
                        return result;
                    }
                }

                found = false;
                return default;
            }

            public override TSource? TryGetLast(out bool found)
            {
                ConcatNIterator<TSource>? node = this;
                do
                {
                    TSource? result = node._head.TryGetLast(out found);
                    if (found)
                    {
                        return result;
                    }
                }
                while ((node = node!.PreviousN) is not null);

                found = false;
                return default;
            }
        }

        private abstract partial class ConcatIterator<TSource> : IPartition<TSource>
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

            public abstract TSource? TryGetElementAt(int index, out bool found);

            public abstract TSource? TryGetFirst(out bool found);

            public abstract TSource? TryGetLast(out bool found);

            public IPartition<TSource>? Skip(int count) => new EnumerablePartition<TSource>(this, count, -1);

            public IPartition<TSource>? Take(int count) => new EnumerablePartition<TSource>(this, 0, count - 1);

        }
    }
}
