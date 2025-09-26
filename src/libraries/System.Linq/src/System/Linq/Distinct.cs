// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source) => Distinct(source, null);

        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource>? comparer)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            if (source is PureOrderedIterator<TSource> pureOrderedIterator && (comparer is null || comparer == EqualityComparer<TSource>.Default))
            {
                return new PureOrderedDistinctIterator<TSource>(pureOrderedIterator);
            }

            return new DistinctIterator<TSource>(source, comparer);
        }

        /// <summary>Returns distinct elements from a sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The <see cref="DistinctBy{TSource, TKey}(IEnumerable{TSource}, Func{TSource, TKey})" /> method returns an unordered sequence that contains no duplicate values. The default equality comparer, <see cref="EqualityComparer{T}.Default" />, is used to compare values.</para>
        /// </remarks>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) => DistinctBy(source, keySelector, null);

        /// <summary>Returns distinct elements from a sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to distinguish elements by.</typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{TKey}" /> to compare keys.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The <see cref="DistinctBy{TSource, TKey}(IEnumerable{TSource}, Func{TSource, TKey}, IEqualityComparer{TKey}?)" /> method returns an unordered sequence that contains no duplicate values. If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="EqualityComparer{T}.Default" />, is used to compare values.</para>
        /// </remarks>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            return DistinctByIterator(source, keySelector, comparer);
        }

        private static IEnumerable<TSource> DistinctByIterator<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            using IEnumerator<TSource> enumerator = source.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var set = new HashSet<TKey>(DefaultInternalSetCapacity, comparer);
                do
                {
                    TSource element = enumerator.Current;
                    if (set.Add(keySelector(element)))
                    {
                        yield return element;
                    }
                }
                while (enumerator.MoveNext());
            }
        }

        /// <summary>
        /// An iterator that yields the distinct values in an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        private sealed partial class DistinctIterator<TSource> : Iterator<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly IEqualityComparer<TSource>? _comparer;
            private HashSet<TSource>? _set;
            private IEnumerator<TSource>? _enumerator;

            public DistinctIterator(IEnumerable<TSource> source, IEqualityComparer<TSource>? comparer)
            {
                Debug.Assert(source is not null);
                _source = source;
                _comparer = comparer;
            }

            private protected override Iterator<TSource> Clone() => new DistinctIterator<TSource>(_source, _comparer);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        if (!_enumerator.MoveNext())
                        {
                            Dispose();
                            return false;
                        }

                        TSource element = _enumerator.Current;
                        _set = new HashSet<TSource>(DefaultInternalSetCapacity, _comparer);
                        _set.Add(element);
                        _current = element;
                        _state = 2;
                        return true;
                    case 2:
                        Debug.Assert(_enumerator is not null);
                        Debug.Assert(_set is not null);
                        while (_enumerator.MoveNext())
                        {
                            element = _enumerator.Current;
                            if (_set.Add(element))
                            {
                                _current = element;
                                return true;
                            }
                        }

                        break;
                }

                Dispose();
                return false;
            }

            public override void Dispose()
            {
                if (_enumerator is not null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                    _set = null;
                }

                base.Dispose();
            }
        }

        /// <summary>
        /// An iterator that yields the distinct values in an <see cref="PureOrderedIterator{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source PureOrderedDistinctIterator.</typeparam>
        /// 
        private sealed partial class PureOrderedDistinctIterator<TSource> : Iterator<TSource>
        {
            private readonly PureOrderedIterator<TSource> _source;
            private EqualityComparer<TSource>? _comparer;
            private Iterator<TSource>? _enumerator;

            private const int _valueTypeState = 2;
            private const int _referenceTypeState = 3;
            public PureOrderedDistinctIterator(PureOrderedIterator<TSource> source)
            {
                Debug.Assert(source is not null);
                _source = source;
            }

            private protected override Iterator<TSource> Clone() => new PureOrderedDistinctIterator<TSource>(_source);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        if (!_enumerator.MoveNext())
                        {
                            Dispose();
                            return false;
                        }

                        TSource element = _enumerator.Current;
                        _current = element;

                        if (typeof(TSource).IsValueType)
                        {
                            _state = _valueTypeState;
                        }
                        else
                        {
                            _comparer = EqualityComparer<TSource>.Default;
                            _state = _referenceTypeState;
                        }

                        return true;
                    case _valueTypeState:
                        // Value types
                        Debug.Assert(_enumerator is not null);
                        while (_enumerator.MoveNext())
                        {
                            element = _enumerator.Current;
                            if (!EqualityComparer<TSource>.Default.Equals(_current, element))
                            {
                                _current = element;
                                return true;
                            }
                        }

                        break;

                    case _referenceTypeState:
                        // Reference types
                        Debug.Assert(_enumerator is not null);
                        Debug.Assert(_comparer is not null);
                        EqualityComparer<TSource> comparer = _comparer;
                        while (_enumerator.MoveNext())
                        {
                            element = _enumerator.Current;
                            if (!comparer.Equals(_current, element))
                            {
                                _current = element;
                                return true;
                            }
                        }

                        break;
                }

                Dispose();
                return false;
            }

            public override void Dispose()
            {
                if (_enumerator is not null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                _comparer = null;

                base.Dispose();
            }
        }
    }
}
