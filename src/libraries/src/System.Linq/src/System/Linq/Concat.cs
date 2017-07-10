// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null)
            {
                throw Error.ArgumentNull(nameof(first));
            }

            if (second == null)
            {
                throw Error.ArgumentNull(nameof(second));
            }

            return first is ConcatIterator<TSource> firstConcat
                ? firstConcat.Concat(second)
                : new Concat2Iterator<TSource>(first, second);
        }

        /// <summary>
        /// Represents the concatenation of two <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private sealed class Concat2Iterator<TSource> : ConcatIterator<TSource>
        {
            /// <summary>
            /// The first source to concatenate.
            /// </summary>
            internal readonly IEnumerable<TSource> _first;

            /// <summary>
            /// The second source to concatenate.
            /// </summary>
            internal readonly IEnumerable<TSource> _second;

            /// <summary>
            /// Initializes a new instance of the <see cref="Concat2Iterator{TSource}"/> class.
            /// </summary>
            /// <param name="first">The first source to concatenate.</param>
            /// <param name="second">The second source to concatenate.</param>
            internal Concat2Iterator(IEnumerable<TSource> first, IEnumerable<TSource> second)
            {
                Debug.Assert(first != null);
                Debug.Assert(second != null);

                _first = first;
                _second = second;
            }

            public override Iterator<TSource> Clone() => new Concat2Iterator<TSource>(_first, _second);

            internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
            {
                bool hasOnlyCollections = next is ICollection<TSource> &&
                                          _first is ICollection<TSource> &&
                                          _second is ICollection<TSource>;
                return new ConcatNIterator<TSource>(this, next, 2, hasOnlyCollections);
            }

            public override int GetCount(bool onlyIfCheap)
            {
                int firstCount, secondCount;
                if (!EnumerableHelpers.TryGetCount(_first, out firstCount))
                {
                    if (onlyIfCheap)
                    {
                        return -1;
                    }

                    firstCount = _first.Count();
                }

                if (!EnumerableHelpers.TryGetCount(_second, out secondCount))
                {
                    if (onlyIfCheap)
                    {
                        return -1;
                    }

                    secondCount = _second.Count();
                }

                return checked(firstCount + secondCount);
            }

            internal override IEnumerable<TSource> GetEnumerable(int index)
            {
                Debug.Assert(index >= 0 && index <= 2);

                switch (index)
                {
                    case 0: return _first;
                    case 1: return _second;
                    default: return null;
                }
            }

            public override TSource[] ToArray()
            {
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

        /// <summary>
        /// Represents the concatenation of three or more <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        /// <remarks>
        /// To handle chains of >= 3 sources, we chain the <see cref="Concat"/> iterators together and allow
        /// <see cref="GetEnumerable"/> to fetch enumerables from the previous sources.  This means that rather
        /// than each <see cref="IEnumerator{T}.MoveNext"/> and <see cref="IEnumerator{T}.Current"/> calls having to traverse all of the previous
        /// sources, we only have to traverse all of the previous sources once per chained enumerable.  An alternative
        /// would be to use an array to store all of the enumerables, but this has a much better memory profile and
        /// without much additional run-time cost.
        /// </remarks>
        private sealed class ConcatNIterator<TSource> : ConcatIterator<TSource>
        {
            /// <summary>
            /// The linked list of previous sources.
            /// </summary>
            private readonly ConcatIterator<TSource> _tail;
            
            /// <summary>
            /// The source associated with this iterator.
            /// </summary>
            private readonly IEnumerable<TSource> _head;

            /// <summary>
            /// The logical index associated with this iterator.
            /// </summary>
            private readonly int _headIndex;

            /// <summary>
            /// <c>true</c> if all sources this iterator concatenates implement <see cref="ICollection{TSource}"/>;
            /// otherwise, <c>false</c>.
            /// </summary>
            /// <remarks>
            /// This flag allows us to determine in O(1) time whether we can preallocate for <see cref="ToArray"/>
            /// and <see cref="ConcatIterator{TSource}.ToList"/>, and whether we can get the count of the iterator cheaply.
            /// </remarks>
            private readonly bool _hasOnlyCollections;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConcatNIterator{TSource}"/> class.
            /// </summary>
            /// <param name="tail">The linked list of previous sources.</param>
            /// <param name="head">The source associated with this iterator.</param>
            /// <param name="headIndex">The logical index associated with this iterator.</param>
            /// <param name="hasOnlyCollections">
            /// <c>true</c> if all sources this iterator concatenates implement <see cref="ICollection{TSource}"/>;
            /// otherwise, <c>false</c>.
            /// </param>
            internal ConcatNIterator(ConcatIterator<TSource> tail, IEnumerable<TSource> head, int headIndex, bool hasOnlyCollections)
            {
                Debug.Assert(tail != null);
                Debug.Assert(head != null);
                Debug.Assert(headIndex >= 2);

                _tail = tail;
                _head = head;
                _headIndex = headIndex;
                _hasOnlyCollections = hasOnlyCollections;
            }

            private ConcatNIterator<TSource> PreviousN => _tail as ConcatNIterator<TSource>;
            
            public override Iterator<TSource> Clone() => new ConcatNIterator<TSource>(_tail, _head, _headIndex, _hasOnlyCollections);

            internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
            {
                if (_headIndex == int.MaxValue - 2)
                {
                    // In the unlikely case of this many concatenations, if we produced a ConcatNIterator
                    // with int.MaxValue then state would overflow before it matched its index.
                    // So we use the naïve approach of just having a left and right sequence.
                    return new Concat2Iterator<TSource>(this, next);
                }
                
                bool hasOnlyCollections = _hasOnlyCollections && next is ICollection<TSource>;
                return new ConcatNIterator<TSource>(this, next, _headIndex + 1, hasOnlyCollections);
            }

            public override int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap && !_hasOnlyCollections)
                {
                    return -1;
                }

                int count = 0;
                ConcatNIterator<TSource> node, previousN = this;

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

            internal override IEnumerable<TSource> GetEnumerable(int index)
            {
                Debug.Assert(index >= 0);

                if (index > _headIndex)
                {
                    return null;
                }

                ConcatNIterator<TSource> node, previousN = this;
                do
                {
                    node = previousN;
                    if (index == node._headIndex)
                    {
                        return node._head;
                    }
                }
                while ((previousN = node.PreviousN) != null);

                Debug.Assert(index == 0 || index == 1);
                Debug.Assert(node._tail is Concat2Iterator<TSource>);
                return node._tail.GetEnumerable(index);
            }

            public override TSource[] ToArray() => _hasOnlyCollections ? PreallocatingToArray() : LazyToArray();

            private TSource[] LazyToArray()
            {
                Debug.Assert(!_hasOnlyCollections);

                var builder = new SparseArrayBuilder<TSource>(initialize: true);
                var deferredCopies = new ArrayBuilder<int>();

                for (int i = 0; ; i++)
                {
                    // Unfortunately, we can't escape re-walking the linked list for each source, which has
                    // quadratic behavior, because we need to add the sources in order.
                    // On the bright side, the bottleneck will usually be iterating, buffering, and copying
                    // each of the enumerables, so this shouldn't be a noticeable perf hit for most scenarios.

                    IEnumerable<TSource> source = GetEnumerable(i);
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
                    IEnumerable<TSource> source = GetEnumerable(deferredCopies[i]);
                    EnumerableHelpers.Copy(source, array, marker.Index, marker.Count);
                }

                return array;
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
                    return Array.Empty<TSource>();
                }

                var array = new TSource[count];
                int arrayIndex = array.Length; // We start copying in collection-sized chunks from the end of the array.

                ConcatNIterator<TSource> node, previousN = this;
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
        }

        /// <summary>
        /// Represents the concatenation of two or more <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private abstract class ConcatIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>
        {
            /// <summary>
            /// The enumerator of the current source, if <see cref="MoveNext"/> has been called.
            /// </summary>
            private IEnumerator<TSource> _enumerator;

            public override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            /// <summary>
            /// Gets the enumerable at a logical index in this iterator.
            /// If the index is equal to the number of enumerables this iterator holds, <c>null</c> is returned.
            /// </summary>
            /// <param name="index">The logical index.</param>
            internal abstract IEnumerable<TSource> GetEnumerable(int index);

            /// <summary>
            /// Creates a new iterator that concatenates this iterator with an enumerable.
            /// </summary>
            /// <param name="next">The next enumerable.</param>
            internal abstract ConcatIterator<TSource> Concat(IEnumerable<TSource> next);

            public override bool MoveNext()
            {
                if (_state == 1)
                {
                    _enumerator = GetEnumerable(0).GetEnumerator();
                    _state = 2;
                }

                if (_state > 1)
                {
                    while (true)
                    {
                        if (_enumerator.MoveNext())
                        {
                            _current = _enumerator.Current;
                            return true;
                        }

                        IEnumerable<TSource> next = GetEnumerable(_state++ - 1);
                        if (next != null)
                        {
                            _enumerator.Dispose();
                            _enumerator = next.GetEnumerator();
                            continue;
                        }

                        Dispose();
                        break;
                    }
                }

                return false;
            }

            public abstract int GetCount(bool onlyIfCheap);

            public abstract TSource[] ToArray();

            public List<TSource> ToList()
            {
                int count = GetCount(onlyIfCheap: true);
                var list = count != -1 ? new List<TSource>(count) : new List<TSource>();

                for (int i = 0; ; i++)
                {
                    IEnumerable<TSource> source = GetEnumerable(i);
                    if (source == null)
                    {
                        break;
                    }

                    list.AddRange(source);
                }

                return list;
            }
        }
    }
}
