// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using static System.Linq.Utilities;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second) => Union(first, second, comparer: null);

        public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            if (first == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }

            if (second == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }

            return first is UnionIterator<TSource> union && AreEqualityComparersEqual(comparer, union._comparer) ? union.Union(second) : new UnionIterator2<TSource>(first, second, comparer);
        }

        /// <summary>Produces the set union of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An <see cref="IEnumerable{T}" /> whose distinct elements form the first set for the union.</param>
        /// <param name="second">An <see cref="IEnumerable{T}" /> whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The default equality comparer, <see cref="EqualityComparer{T}.Default" />, is used to compare values.</para>
        /// <para>When the object returned by this method is enumerated, <see cref="O:Enumerable.UnionBy" /> enumerates <paramref name="first" /> and <paramref name="second" /> in that order and yields each element that has not already been yielded.</para>
        /// </remarks>
        public static IEnumerable<TSource> UnionBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector) => UnionBy(first, second, keySelector, null);

        /// <summary>Produces the set union of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An <see cref="IEnumerable{T}" /> whose distinct elements form the first set for the union.</param>
        /// <param name="second">An <see cref="IEnumerable{T}" /> whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}" /> to compare values.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="EqualityComparer{T}.Default" />, is used to compare values.</para>
        /// <para>When the object returned by this method is enumerated, <see cref="O:Enumerable.UnionBy" /> enumerates <paramref name="first" /> and <paramref name="second" /> in that order and yields each element that has not already been yielded.</para>
        /// </remarks>
        public static IEnumerable<TSource> UnionBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (first is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }
            if (second is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            return UnionByIterator(first, second, keySelector, comparer);
        }

        private static IEnumerable<TSource> UnionByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            var set = new HashSet<TKey>(DefaultInternalSetCapacity, comparer);

            foreach (TSource element in first)
            {
                if (set.Add(keySelector(element)))
                {
                    yield return element;
                }
            }

            foreach (TSource element in second)
            {
                if (set.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        /// <summary>
        /// An iterator that yields distinct values from two or more <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private abstract partial class UnionIterator<TSource> : Iterator<TSource>
        {
            internal readonly IEqualityComparer<TSource>? _comparer;
            private IEnumerator<TSource>? _enumerator;
            private HashSet<TSource>? _set;

            protected UnionIterator(IEqualityComparer<TSource>? comparer)
            {
                _comparer = comparer;
            }

            public sealed override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                    _set = null;
                }

                base.Dispose();
            }

            internal abstract IEnumerable<TSource>? GetEnumerable(int index);

            internal abstract UnionIterator<TSource> Union(IEnumerable<TSource> next);

            private void SetEnumerator(IEnumerator<TSource> enumerator)
            {
                _enumerator?.Dispose();

                _enumerator = enumerator;
            }

            private void StoreFirst()
            {
                Debug.Assert(_enumerator != null);

                var set = new HashSet<TSource>(DefaultInternalSetCapacity, _comparer);
                TSource element = _enumerator.Current;
                set.Add(element);
                _current = element;
                _set = set;
            }

            private bool GetNext()
            {
                Debug.Assert(_enumerator != null);
                Debug.Assert(_set != null);

                HashSet<TSource> set = _set;

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
                    for (IEnumerable<TSource>? enumerable = GetEnumerable(0); enumerable != null; enumerable = GetEnumerable(_state - 1))
                    {
                        IEnumerator<TSource> enumerator = enumerable.GetEnumerator();
                        SetEnumerator(enumerator);

                        ++_state;
                        if (enumerator.MoveNext())
                        {
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

                        IEnumerable<TSource>? enumerable = GetEnumerable(_state - 1);
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
        }

        /// <summary>
        /// An iterator that yields distinct values from two <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private sealed class UnionIterator2<TSource> : UnionIterator<TSource>
        {
            private readonly IEnumerable<TSource> _first;
            private readonly IEnumerable<TSource> _second;

            public UnionIterator2(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
                : base(comparer)
            {
                Debug.Assert(first != null);
                Debug.Assert(second != null);
                _first = first;
                _second = second;
            }

            public override Iterator<TSource> Clone() => new UnionIterator2<TSource>(_first, _second, _comparer);

            internal override IEnumerable<TSource>? GetEnumerable(int index)
            {
                Debug.Assert(index >= 0 && index <= 2);
                return index switch
                {
                    0 => _first,
                    1 => _second,
                    _ => null,
                };
            }

            internal override UnionIterator<TSource> Union(IEnumerable<TSource> next)
            {
                var sources = new SingleLinkedNode<IEnumerable<TSource>>(_first).Add(_second).Add(next);
                return new UnionIteratorN<TSource>(sources, 2, _comparer);
            }
        }

        /// <summary>
        /// An iterator that yields distinct values from three or more <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerables.</typeparam>
        private sealed class UnionIteratorN<TSource> : UnionIterator<TSource>
        {
            private readonly SingleLinkedNode<IEnumerable<TSource>> _sources;
            private readonly int _headIndex;

            public UnionIteratorN(SingleLinkedNode<IEnumerable<TSource>> sources, int headIndex, IEqualityComparer<TSource>? comparer)
                : base(comparer)
            {
                Debug.Assert(headIndex >= 2);
                Debug.Assert(sources?.GetCount() == headIndex + 1);

                _sources = sources;
                _headIndex = headIndex;
            }

            public override Iterator<TSource> Clone() => new UnionIteratorN<TSource>(_sources, _headIndex, _comparer);

            internal override IEnumerable<TSource>? GetEnumerable(int index) => index > _headIndex ? null : _sources.GetNode(_headIndex - index).Item;

            internal override UnionIterator<TSource> Union(IEnumerable<TSource> next)
            {
                if (_headIndex == int.MaxValue - 2)
                {
                    // In the unlikely case of this many unions, if we produced a UnionIteratorN
                    // with int.MaxValue then state would overflow before it matched it's index.
                    // So we use the naive approach of just having a left and right sequence.
                    return new UnionIterator2<TSource>(this, next, _comparer);
                }

                return new UnionIteratorN<TSource>(_sources.Add(next), _headIndex + 1, _comparer);
            }
        }
    }
}
