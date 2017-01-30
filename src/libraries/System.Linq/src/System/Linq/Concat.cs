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

            var firstConcat = first as ConcatIterator<TSource>;
            return firstConcat != null ?
                firstConcat.Concat(second) :
                new Concat2Iterator<TSource>(first, second);
        }

        /// <summary>
        /// Represents the concatenation of two <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private sealed class Concat2Iterator<TSource> : ConcatIterator<TSource>
        {
            private readonly IEnumerable<TSource> _first;
            private readonly IEnumerable<TSource> _second;

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
                var sources = new SingleLinkedNode<IEnumerable<TSource>>(_first).Add(_second).Add(next);
                // We employ an optimization where if all of the enumerables being concatenated are ICollections,
                // we set the flag on the next iterator. This allows us to determine in O(1) time whether we can
                // preallocate for ToArray and ToList, and whether we can get the count of the iterator cheaply.
                bool hasOnlyCollections = _first is ICollection<TSource> &&
                                          _second is ICollection<TSource> &&
                                          next is ICollection<TSource>;
                return new ConcatNIterator<TSource>(sources, 2, hasOnlyCollections);
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
                ICollection<TSource> first = _first as ICollection<TSource>;
                ICollection<TSource> second = _second as ICollection<TSource>;

                if (first != null & second != null)
                {
                    return PreallocatingToArray(first, second);
                }
                
                var builder = new SparseArrayBuilder<TSource>(initialize: true);

                int? firstCount = first?.Count;
                if (firstCount > 0)
                {
                    builder.Reserve(firstCount.GetValueOrDefault());
                }
                else if (firstCount == null)
                {
                    builder.AddRange(_first);
                }

                int? secondCount = second?.Count;
                if (secondCount > 0)
                {
                    builder.Reserve(secondCount.GetValueOrDefault());
                }
                else if (secondCount == null)
                {
                    builder.AddRange(_second);
                }

                TSource[] array = builder.ToArray();

                if (first != null | second != null)
                {
                    ICollection<TSource> collection = first != null ? first : second;
                    int arrayIndex = builder.Markers.Single().Index;
                    collection.CopyTo(array, arrayIndex);
                }

                return array;
            }

            private static TSource[] PreallocatingToArray(ICollection<TSource> first, ICollection<TSource> second)
            {
                Debug.Assert(first != null);
                Debug.Assert(second != null);

                int firstCount = first.Count; // Cache one interface call.
                int count = checked(firstCount + second.Count);

                if (count == 0)
                {
                    return Array.Empty<TSource>();
                }

                var array = new TSource[count];
                first.CopyTo(array, 0);
                second.CopyTo(array, firstCount);

                return array;
            }

            public override List<TSource> ToList()
            {
                int count = GetCount(onlyIfCheap: true);
                var list = count >= 0 ? new List<TSource>(count) : new List<TSource>();

                list.AddRange(_first);
                list.AddRange(_second);
                return list;
            }
        }

        /// <summary>
        /// Represents the concatenation of three or more <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        /// <remarks>
        /// To handle chains of >= 3 sources, we store each enumerable in a singly-linked list node.
        /// Each iterator holds a reference to the head of the list, which contains the most recently
        /// concatenated enumerable. When a new one is added, we allocate a new node that points to our
        /// head and store it, then allocate a new iterator to wrap that node.
        ///
        /// This linked-list implementation means that rather than each MoveNext/Current call having to
        /// traverse all of the previous sources, we only have to traverse all of the previous sources
        /// once per chained enumerable.  An alternative would be to use an array to store all of the
        /// enumerables, but this has a much better memory profile and without much additional run-time cost.
        ///
        /// At face value, using list nodes seems worse than chaining together the iterators directly and
        /// letting the iterator itself hold a reference to the latest source. So why allocate a list node in
        /// addition to each iterator? It lets the GC reclaim intermediary iterators in the chain which
        /// have fields that are not relevant to later iterators, like <see cref="_enumerator"/> or
        /// <see cref="_headIndex"/>. Only the nodes are kept alive, and those contain only the sources.
        /// </remarks>
        private sealed class ConcatNIterator<TSource> : ConcatIterator<TSource>
        {
            private readonly SingleLinkedNode<IEnumerable<TSource>> _sources;
            private readonly int _headIndex;
            private readonly bool _hasOnlyCollections;

            internal ConcatNIterator(SingleLinkedNode<IEnumerable<TSource>> sources, int headIndex, bool hasOnlyCollections)
            {
                Debug.Assert(headIndex >= 2);
                Debug.Assert(sources?.GetCount() == headIndex + 1);

                _sources = sources;
                _headIndex = headIndex;
                _hasOnlyCollections = hasOnlyCollections;
            }
            
            public override Iterator<TSource> Clone() => new ConcatNIterator<TSource>(_sources, _headIndex, _hasOnlyCollections);

            internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
            {
                if (_headIndex == int.MaxValue - 2)
                {
                    // In the unlikely case of this many concatenations, if we produced a ConcatNIterator
                    // with int.MaxValue then state would overflow before it matched its index.
                    // So we use the naïve approach of just having a left and right sequence.
                    return new Concat2Iterator<TSource>(this, next);
                }

                // If the all of our sources are ICollections but the next enumerable isn't, update `_hasOnlyCollections` to indicate that
                // we won't be able to employ certain optimizations in the next iterator.
                bool hasOnlyCollections = _hasOnlyCollections && next is ICollection<TSource>;
                return new ConcatNIterator<TSource>(_sources.Add(next), _headIndex + 1, hasOnlyCollections);
            }

            public override int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap && !_hasOnlyCollections)
                {
                    return -1;
                }

                int count = 0;
                for (SingleLinkedNode<IEnumerable<TSource>> node = _sources; node != null; node = node.Linked)
                {
                    IEnumerable<TSource> source = node.Item;
                    Debug.Assert(!_hasOnlyCollections || source is ICollection<TSource>);
                    checked
                    {
                        count += source.Count();
                    }
                }

                return count;
            }

            internal override IEnumerable<TSource> GetEnumerable(int index) => index > _headIndex ? null : _sources.GetNode(_headIndex - index).Item;

            public override TSource[] ToArray() => _hasOnlyCollections ? PreallocatingToArray() : LazyToArray();

            private TSource[] LazyToArray()
            {
                Debug.Assert(!_hasOnlyCollections);

                var builder = new SparseArrayBuilder<TSource>(initialize: true);
                var deferredCopies = new ArrayBuilder<IEnumerable<TSource>>();

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

                    int count;
                    if (EnumerableHelpers.TryGetCount(source, out count))
                    {
                        if (count > 0)
                        {
                            builder.Reserve(count);
                            deferredCopies.Add(source);
                        }
                        continue;
                    }

                    builder.AddRange(source);
                }

                TSource[] array = builder.ToArray();

                ArrayBuilder<Marker> markers = builder.Markers;
                for (int i = 0; i < markers.Count; i++)
                {
                    Marker marker = markers[i];
                    IEnumerable<TSource> source = deferredCopies[i];
                    EnumerableHelpers.Copy(source, array, marker.Index, marker.Count);
                }

                return array;
            }

            private TSource[] PreallocatingToArray()
            {
                // If there are only ICollections in this iterator, then we can just get the count, preallocate the
                // array, and then copy them as we go. This has better time complexity than continuously re-walking
                // the linked list via GetEnumerable, and better memory usage than buffering the collections.
                Debug.Assert(_hasOnlyCollections);

                int count = GetCount(onlyIfCheap: true);
                Debug.Assert(count >= 0);

                if (count == 0)
                {
                    return Array.Empty<TSource>();
                }

                var array = new TSource[count];
                int arrayIndex = array.Length;

                for (SingleLinkedNode<IEnumerable<TSource>> node = _sources; node != null; node = node.Linked)
                {
                    ICollection<TSource> source = (ICollection<TSource>)node.Item;
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

                Debug.Assert(arrayIndex == 0);
                return array;
            }

            public override List<TSource> ToList()
            {
                int count = GetCount(onlyIfCheap: true);
                var list = count >= 0 ? new List<TSource>(count) : new List<TSource>();

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

            public abstract List<TSource> ToList();
        }
    }
}
