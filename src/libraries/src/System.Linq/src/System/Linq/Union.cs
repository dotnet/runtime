// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using static System.Linq.Utilities;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            return Union(first, second, null);
        }

        public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            if (first == null)
            {
                throw Error.ArgumentNull(nameof(first));
            }

            if (second == null)
            {
                throw Error.ArgumentNull(nameof(second));
            }

            UnionIterator<TSource> union = first as UnionIterator<TSource>;
            return union != null && AreEqualityComparersEqual(comparer, union._comparer) ? union.Union(second) : new UnionIterator2<TSource>(first, second, comparer);
        }

        /// <summary>
        /// An iterator that yields distinct values from two or more <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private abstract class UnionIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>
        {
            internal readonly IEqualityComparer<TSource> _comparer;
            private IEnumerator<TSource> _enumerator;
            private Set<TSource> _set;

            public UnionIterator(IEqualityComparer<TSource> comparer)
            {
                _comparer = comparer;
            }

            public override sealed void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                    _set = null;
                }

                base.Dispose();
            }

            internal abstract IEnumerable<TSource> GetEnumerable(int index);

            internal abstract UnionIterator<TSource> Union(IEnumerable<TSource> next);

            protected void SetEnumerator(IEnumerator<TSource> enumerator)
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                }

                _enumerator = enumerator;
            }

            protected void StoreFirst()
            {
                Set<TSource> set = new Set<TSource>(_comparer);
                TSource element = _enumerator.Current;
                set.Add(element);
                _current = element;
                _set = set;
            }

            protected bool GetNext()
            {
                Set<TSource> set = _set;
                while (_enumerator.MoveNext())
                {
                    TSource element = _enumerator.Current;
                    if (set.Add(element))
                    {
                        _current = element;
                        return true;
                    }
                }

                return false;
            }

            public sealed override bool MoveNext()
            {
                if (_state == 1)
                {
                    for (IEnumerable<TSource> enumerable = GetEnumerable(0); enumerable != null; enumerable = GetEnumerable(_state - 1))
                    {
                        IEnumerator<TSource> enumerator = enumerable.GetEnumerator();
                        ++_state;
                        if (enumerator.MoveNext())
                        {
                            SetEnumerator(enumerator);
                            StoreFirst();
                            return true;
                        }
                    }
                }
                else if (_state > 0)
                {
                    while (true)
                    {
                        if (GetNext())
                        {
                            return true;
                        }

                        IEnumerable<TSource> enumerable = GetEnumerable(_state - 1);
                        if (enumerable == null)
                        {
                            break;
                        }

                        SetEnumerator(enumerable.GetEnumerator());
                        ++_state;
                    }
                }

                Dispose();
                return false;
            }

            private Set<TSource> FillSet()
            {
                Set<TSource> set = new Set<TSource>(_comparer);
                for (int index = 0; ; ++index)
                {
                    IEnumerable<TSource> enumerable = GetEnumerable(index);
                    if (enumerable == null)
                    {
                        return set;
                    }

                    foreach (TSource item in enumerable)
                    {
                        set.Add(item);
                    }
                }
            }

            public TSource[] ToArray()
            {
                return FillSet().ToArray();
            }

            public List<TSource> ToList()
            {
                return FillSet().ToList();
            }

            public int GetCount(bool onlyIfCheap)
            {
                return onlyIfCheap ? -1 : FillSet().Count;
            }
        }
        
        /// <summary>
        /// An iterator that yields distinct values from two <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private sealed class UnionIterator2<TSource> : UnionIterator<TSource>
        {
            private readonly IEnumerable<TSource> _first;
            private readonly IEnumerable<TSource> _second;

            public UnionIterator2(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
                : base(comparer)
            {
                Debug.Assert(first != null);
                Debug.Assert(second != null);
                _first = first;
                _second = second;
            }

            public override Iterator<TSource> Clone()
            {
                return new UnionIterator2<TSource>(_first, _second, _comparer);
            }

            internal override IEnumerable<TSource> GetEnumerable(int index)
            {
                Debug.Assert(index >= 0 && index <= 2);
                switch (index)
                {
                    case 0:
                        return _first;
                    case 1:
                        return _second;
                    default:
                        return null;
                }
            }

            internal override UnionIterator<TSource> Union(IEnumerable<TSource> next)
            {
                return new UnionIteratorN<TSource>(this, next, 2);
            }
        }

        /// <summary>
        /// An iterator that yields distinct values from three or more <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private sealed class UnionIteratorN<TSource> : UnionIterator<TSource>
        {
            private readonly UnionIterator<TSource> _previous;
            private readonly IEnumerable<TSource> _next;
            private readonly int _nextIndex;

            public UnionIteratorN(UnionIterator<TSource> previous, IEnumerable<TSource> next, int nextIndex)
                : base(previous._comparer)
            {
                Debug.Assert(next != null);
                Debug.Assert(nextIndex > 1);
                _previous = previous;
                _next = next;
                _nextIndex = nextIndex;
            }

            public override Iterator<TSource> Clone()
            {
                return new UnionIteratorN<TSource>(_previous, _next, _nextIndex);
            }

            internal override IEnumerable<TSource> GetEnumerable(int index)
            {
                if (index > _nextIndex)
                {
                    return null;
                }

                UnionIteratorN<TSource> union = this;
                while (index < union._nextIndex)
                {
                    UnionIterator<TSource> previous = union._previous;
                    union = previous as UnionIteratorN<TSource>;
                    if (union == null)
                    {
                        Debug.Assert(index == 0 || index == 1);
                        Debug.Assert(AreEqualityComparersEqual(_comparer, previous._comparer));
                        return previous.GetEnumerable(index);
                    }
                }

                return union._next;
            }

            internal override UnionIterator<TSource> Union(IEnumerable<TSource> next)
            {
                if (_nextIndex == int.MaxValue - 2)
                {
                    // In the unlikely case of this many unions, if we produced a UnionIteratorN
                    // with int.MaxValue then state would overflow before it matched it's index.
                    // So we use the naïve approach of just having a left and right sequence.
                    return new UnionIterator2<TSource>(this, next, _comparer);
                }

                return new UnionIteratorN<TSource>(this, next, _nextIndex + 1);
            }
        }
    }
}
