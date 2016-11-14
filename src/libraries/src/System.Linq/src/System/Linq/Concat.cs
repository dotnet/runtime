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

            Debug.Assert(!(first is ICollection<TSource> && first is ConcatIterator<TSource>), "Didn't expect enumerable to be both a collection and a concat iterator.");

            var firstCollection = first as ICollection<TSource>;
            if (firstCollection != null)
            {
                var secondCollection = second as ICollection<TSource>;
                if (secondCollection != null)
                {
                    return new Concat2CollectionIterator<TSource>(firstCollection, secondCollection);
                }
            }
            else
            {
                var firstConcat = first as ConcatIterator<TSource>;
                if (firstConcat != null)
                {
                    return firstConcat.Concat(second);
                }
            }

            return new Concat2EnumerableIterator<TSource>(first, second);
        }

        private sealed class Concat2EnumerableIterator<TSource> : ConcatIterator<TSource>
        {
            private readonly IEnumerable<TSource> _first;
            private readonly IEnumerable<TSource> _second;

            internal Concat2EnumerableIterator(IEnumerable<TSource> first, IEnumerable<TSource> second)
            {
                Debug.Assert(first != null && second != null);
                _first = first;
                _second = second;
            }

            public override Iterator<TSource> Clone()
            {
                return new Concat2EnumerableIterator<TSource>(_first, _second);
            }

            internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
            {
                return new ConcatNEnumerableIterator<TSource>(this, next, 2);
            }

            internal override IEnumerable<TSource> GetEnumerable(int index)
            {
                switch (index)
                {
                    case 0: return _first;
                    case 1: return _second;
                    default: return null;
                }
            }
        }

        // To handle chains of >= 3 sources, we chain the concat iterators together and allow
        // GetEnumerable to fetch enumerables from the previous sources.  This means that rather
        // than each MoveNext/Current calls having to traverse all of the previous sources, we
        // only have to traverse all of the previous sources once per chained enumerable.  An
        // alternative would be to use an array to store all of the enumerables, but this has
        // a much better memory profile and without much additional run-time cost.
        private sealed class ConcatNEnumerableIterator<TSource> : ConcatIterator<TSource>
        {
            private readonly ConcatIterator<TSource> _previousConcat;
            private readonly IEnumerable<TSource> _next;
            private readonly int _nextIndex;

            internal ConcatNEnumerableIterator(ConcatIterator<TSource> previousConcat, IEnumerable<TSource> next, int nextIndex)
            {
                Debug.Assert(previousConcat != null);
                Debug.Assert(next != null);
                Debug.Assert(nextIndex >= 2);
                _previousConcat = previousConcat;
                _next = next;
                _nextIndex = nextIndex;
            }

            public override Iterator<TSource> Clone()
            {
                return new ConcatNEnumerableIterator<TSource>(_previousConcat, _next, _nextIndex);
            }

            internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
            {
                if (_nextIndex == int.MaxValue - 2)
                {
                    // In the unlikely case of this many concatenations, if we produced a ConcatNEnumerableIterator
                    // with int.MaxValue then state would overflow before it matched its index.
                    // So we use the naïve approach of just having a left and right sequence.
                    return new Concat2EnumerableIterator<TSource>(this, next);
                }

                return new ConcatNEnumerableIterator<TSource>(this, next, _nextIndex + 1);
            }

            internal override IEnumerable<TSource> GetEnumerable(int index)
            {
                if (index > _nextIndex)
                {
                    return null;
                }

                // Walk back through the chain of ConcatNIterators looking for the one
                // that has its _nextIndex equal to index.  If we don't find one, then it
                // must be prior to any of them, so call GetEnumerable on the previous
                // Concat2Iterator.  This avoids a deep recursive call chain.
                ConcatNEnumerableIterator<TSource> current = this;
                while (true)
                {
                    if (index == current._nextIndex)
                    {
                        return current._next;
                    }

                    ConcatIterator<TSource> previous = current._previousConcat;

                    var previousEnumerables = previous as ConcatNEnumerableIterator<TSource>;
                    if (previousEnumerables != null)
                    {
                        current = previousEnumerables;
                        continue;
                    }

                    var previousCollections = previous as ConcatNCollectionIterator<TSource>;
                    if (previousCollections != null)
                    {
                        // Since ConcatNCollectionIterator.GetEnumerable does not call into this method,
                        // it is safe to call GetEnumerable on it here. It also makes things faster, since
                        // the above type-cast will only ever be run once per call of this method.
                        return previousCollections.GetEnumerable(index);
                    }

                    // We've reached the tail of the linked list, which contains the first 2 enumerables.
                    Debug.Assert(previous is Concat2EnumerableIterator<TSource> || previous is Concat2CollectionIterator<TSource>);
                    Debug.Assert(index == 0 || index == 1);
                    return previous.GetEnumerable(index);
                }
            }
        }

        private sealed class Concat2CollectionIterator<TSource> : ConcatIterator<TSource>
        {
            private readonly ICollection<TSource> _first;
            private readonly ICollection<TSource> _second;

            internal Concat2CollectionIterator(ICollection<TSource> first, ICollection<TSource> second)
            {
                Debug.Assert(first != null && second != null);
                _first = first;
                _second = second;
            }

            internal int Count => checked(_first.Count + _second.Count);

            public override Iterator<TSource> Clone()
            {
                return new Concat2CollectionIterator<TSource>(_first, _second);
            }

            internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
            {
                var nextCollection = next as ICollection<TSource>;
                if (nextCollection != null)
                {
                    return new ConcatNCollectionIterator<TSource>(this, nextCollection, 2);
                }
                return new ConcatNEnumerableIterator<TSource>(this, next, 2);
            }

            internal void CopyTo(TSource[] array, int arrayIndex)
            {
                Debug.Assert(array != null);
                Debug.Assert(arrayIndex >= 0);
                Debug.Assert(array.Length - arrayIndex >= Count);

                _first.CopyTo(array, arrayIndex);
                _second.CopyTo(array, checked(arrayIndex + _first.Count));
            }

            internal override IEnumerable<TSource> GetEnumerable(int index)
            {
                switch (index)
                {
                    case 0: return _first;
                    case 1: return _second;
                    default: return null;
                }
            }

            public override TSource[] ToArray()
            {
                int firstCount = _first.Count; // Cache an interface method call
                int totalCount = checked(firstCount + _second.Count);

                if (totalCount == 0)
                {
                    return Array.Empty<TSource>();
                }

                var result = new TSource[totalCount];

                _first.CopyTo(result, 0);
                _second.CopyTo(result, firstCount);

                return result;
            }

            public override int GetCount(bool onlyIfCheap) => Count; // Getting the count is always cheap.
        }

        private sealed class ConcatNCollectionIterator<TSource> : ConcatIterator<TSource>
        {
            private readonly ConcatIterator<TSource> _previous;
            private readonly ICollection<TSource> _next;
            private readonly int _nextIndex;

            internal ConcatNCollectionIterator(ConcatIterator<TSource> previous, ICollection<TSource> next, int nextIndex)
            {
                Debug.Assert(previous != null);
                Debug.Assert(previous is Concat2CollectionIterator<TSource> || previous is ConcatNCollectionIterator<TSource>);
                Debug.Assert(next != null);
                Debug.Assert(nextIndex >= 2);

                _previous = previous;
                _next = next;
                _nextIndex = nextIndex;
            }

            private int Count
            {
                get
                {
                    // Walk the linked list of Concat{2,N}CollectionIterators and call .Count
                    // on each of the collections.
                    // Note that we start from the last collection and make our way to the first,
                    // but the cumulative count will be the same either way.
                    
                    int totalCount = _next.Count;
                    ConcatIterator<TSource> previous = _previous;

                    ConcatNCollectionIterator<TSource> previousN;
                    while ((previousN = previous as ConcatNCollectionIterator<TSource>) != null)
                    {
                        checked
                        {
                            totalCount += previousN._next.Count;
                        }
                        previous = previousN._previous;
                    }

                    var previous2 = (Concat2CollectionIterator<TSource>)previous;
                    return checked(totalCount + previous2.Count);
                }
            }

            public override Iterator<TSource> Clone()
            {
                return new ConcatNCollectionIterator<TSource>(_previous, _next, _nextIndex);
            }

            internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
            {
                var nextCollection = next as ICollection<TSource>;
                if (nextCollection != null)
                {
                    if (_nextIndex == int.MaxValue - 2)
                    {
                        // In the unlikely case of this many concatenations, if we produced a ConcatNCollectionIterator
                        // with int.MaxValue then state would overflow before it matched its index.
                        // So we use the naïve approach of just having a left and right sequence.
                        return new Concat2EnumerableIterator<TSource>(this, next);
                    }

                    return new ConcatNCollectionIterator<TSource>(this, nextCollection, _nextIndex + 1);
                }
                
                // If we encounter a non-ICollection then getting .Count and performing .ToArray()
                // will no longer be cheap, due to enumerables' lazy nature. So, fall back to using
                // enumerable-based iterators for any further chaining.
                return new ConcatNEnumerableIterator<TSource>(this, next, _nextIndex + 1);
            }

            // Copy all of the elements in the iterator, finishing before indexAfterCopy.
            // If indexAfterCopy is array.Length, we'll finish copying at the end of the array.
            // The reason we take an ending index as opposed to a starting one is because
            // we only hold a reference to the most recently concat'd collection. So to start
            // copying at a certain index, we'd have to re-walk the linked list of iterators
            // all the way back to the least recent one, and repeat that for all of the
            // collections we hold.
            private void CopyBefore(TSource[] array, int indexAfterCopy)
            {
                Debug.Assert(array != null);
                Debug.Assert(indexAfterCopy >= 0 && indexAfterCopy <= array.Length);
                Debug.Assert(indexAfterCopy >= Count);

                // Copy the items from this collection, which is the last
                // one that was concatenated
                int copied = _next.Count;
                _next.CopyTo(array, indexAfterCopy - copied);

                ConcatIterator<TSource> previous = _previous;

                ConcatNCollectionIterator<TSource> previousN;
                while ((previousN = previous as ConcatNCollectionIterator<TSource>) != null)
                {
                    checked
                    {
                        copied += previousN._next.Count;
                    }
                    previousN._next.CopyTo(array, indexAfterCopy - copied);
                    previous = previousN._previous;
                }

                // We've reached the first 2 collections that were concatenated
                var previous2 = (Concat2CollectionIterator<TSource>)previous;
                copied += previous2.Count;
                Debug.Assert(copied == Count); // We should have copied all the elements

                previous2.CopyTo(array, indexAfterCopy - copied);
            }

            internal override IEnumerable<TSource> GetEnumerable(int index)
            {
                if (index > _nextIndex)
                {
                    return null;
                }

                ConcatNCollectionIterator<TSource> current = this;
                while (true)
                {
                    if (index == current._nextIndex)
                    {
                        return current._next;
                    }

                    var previousN = current._previous as ConcatNCollectionIterator<TSource>;
                    if (previousN != null)
                    {
                        current = previousN;
                        continue;
                    }

                    var previous2 = (Concat2CollectionIterator<TSource>)current._previous;
                    Debug.Assert(index == 0 || index == 1);
                    return previous2.GetEnumerable(index);
                }
            }

            public override TSource[] ToArray()
            {
                int totalCount = Count;
                if (totalCount == 0)
                {
                    return Array.Empty<TSource>();
                }

                var result = new TSource[totalCount];
                CopyBefore(result, result.Length);
                return result;
            }

            public override int GetCount(bool onlyIfCheap) => Count; // Getting the count is always cheap relative to manually iterating.
        }

        private abstract class ConcatIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>
        {
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

            internal abstract IEnumerable<TSource> GetEnumerable(int index);

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

            public virtual TSource[] ToArray()
            {
                var builder = new LargeArrayBuilder<TSource>(initialize: true);

                for (int i = 0; ; i++)
                {
                    IEnumerable<TSource> source = GetEnumerable(i);
                    if (source == null)
                    {
                        break;
                    }

                    builder.AddRange(source);
                }

                return builder.ToArray();
            }

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

            public virtual int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap)
                {
                    return -1;
                }

                int count = 0;
                for (int i = 0; ; i++)
                {
                    IEnumerable<TSource> source = GetEnumerable(i);
                    if (source == null)
                    {
                        break;
                    }

                    checked
                    {
                        count += source.Count();
                    }
                }

                return count;
            }
        }
    }
}
