// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using static System.Linq.Utilities;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            if (source is Iterator<TSource> iterator)
            {
                return iterator.Where(predicate);
            }

            if (source is TSource[] array)
            {
                if (array.Length == 0)
                {
                    return [];
                }

                return new ArrayWhereIterator<TSource>(array, predicate);
            }

            if (source is List<TSource> list)
            {
                return new ListWhereIterator<TSource>(list, predicate);
            }

            return new IEnumerableWhereIterator<TSource>(source, predicate);
        }

        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            return WhereIterator(source, predicate);
        }

        private static IEnumerable<TSource> WhereIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked
                {
                    index++;
                }

                if (predicate(element, index))
                {
                    yield return element;
                }
            }
        }

        /// <summary>
        /// An iterator that filters each item of an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        private sealed partial class IEnumerableWhereIterator<TSource> : Iterator<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, bool> _predicate;
            private IEnumerator<TSource>? _enumerator;

            public IEnumerableWhereIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                Debug.Assert(source is not null);
                Debug.Assert(predicate is not null);
                _source = source;
                _predicate = predicate;
            }

            public override Iterator<TSource> Clone() => new IEnumerableWhereIterator<TSource>(_source, _predicate);

            public override void Dispose()
            {
                if (_enumerator is not null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        Debug.Assert(_enumerator is not null);
                        while (_enumerator.MoveNext())
                        {
                            TSource item = _enumerator.Current;
                            if (_predicate(item))
                            {
                                _current = item;
                                return true;
                            }
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector) =>
                new IEnumerableWhereSelectIterator<TSource, TResult>(_source, _predicate, selector);

            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate) =>
                new IEnumerableWhereIterator<TSource>(_source, CombinePredicates(_predicate, predicate));
        }

        /// <summary>
        /// An iterator that filters each item of an array.
        /// </summary>
        /// <typeparam name="TSource">The type of the source array.</typeparam>
        internal sealed partial class ArrayWhereIterator<TSource> : Iterator<TSource>
        {
            private readonly TSource[] _source;
            private readonly Func<TSource, bool> _predicate;

            public ArrayWhereIterator(TSource[] source, Func<TSource, bool> predicate)
            {
                Debug.Assert(source is not null && source.Length > 0);
                Debug.Assert(predicate is not null);
                _source = source;
                _predicate = predicate;
            }

            public override Iterator<TSource> Clone() =>
                new ArrayWhereIterator<TSource>(_source, _predicate);

            public override bool MoveNext()
            {
                int index = _state - 1;
                TSource[] source = _source;

                while ((uint)index < (uint)source.Length)
                {
                    TSource item = source[index];
                    index = _state++;
                    if (_predicate(item))
                    {
                        _current = item;
                        return true;
                    }
                }

                Dispose();
                return false;
            }

            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector) =>
                new ArrayWhereSelectIterator<TSource, TResult>(_source, _predicate, selector);

            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate) =>
                new ArrayWhereIterator<TSource>(_source, CombinePredicates(_predicate, predicate));
        }

        /// <summary>
        /// An iterator that filters each item of a <see cref="List{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source list.</typeparam>
        private sealed partial class ListWhereIterator<TSource> : Iterator<TSource>
        {
            private readonly List<TSource> _source;
            private readonly Func<TSource, bool> _predicate;
            private List<TSource>.Enumerator _enumerator;

            public ListWhereIterator(List<TSource> source, Func<TSource, bool> predicate)
            {
                Debug.Assert(source is not null);
                Debug.Assert(predicate is not null);
                _source = source;
                _predicate = predicate;
            }

            public override Iterator<TSource> Clone() =>
                new ListWhereIterator<TSource>(_source, _predicate);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        while (_enumerator.MoveNext())
                        {
                            TSource item = _enumerator.Current;
                            if (_predicate(item))
                            {
                                _current = item;
                                return true;
                            }
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector) =>
                new ListWhereSelectIterator<TSource, TResult>(_source, _predicate, selector);

            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate) =>
                new ListWhereIterator<TSource>(_source, CombinePredicates(_predicate, predicate));
        }

        /// <summary>
        /// An iterator that filters, then maps, each item of an array.
        /// </summary>
        /// <typeparam name="TSource">The type of the source array.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        private sealed partial class ArrayWhereSelectIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly TSource[] _source;
            private readonly Func<TSource, bool> _predicate;
            private readonly Func<TSource, TResult> _selector;

            public ArrayWhereSelectIterator(TSource[] source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                Debug.Assert(source is not null && source.Length > 0);
                Debug.Assert(predicate is not null);
                Debug.Assert(selector is not null);
                _source = source;
                _predicate = predicate;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new ArrayWhereSelectIterator<TSource, TResult>(_source, _predicate, _selector);

            public override bool MoveNext()
            {
                int index = _state - 1;
                TSource[] source = _source;

                while ((uint)index < (uint)source.Length)
                {
                    TSource item = source[index];
                    index = _state++;
                    if (_predicate(item))
                    {
                        _current = _selector(item);
                        return true;
                    }
                }

                Dispose();
                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new ArrayWhereSelectIterator<TSource, TResult2>(_source, _predicate, CombineSelectors(_selector, selector));
        }

        /// <summary>
        /// An iterator that filters, then maps, each item of a <see cref="List{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source list.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        private sealed partial class ListWhereSelectIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly List<TSource> _source;
            private readonly Func<TSource, bool> _predicate;
            private readonly Func<TSource, TResult> _selector;
            private List<TSource>.Enumerator _enumerator;

            public ListWhereSelectIterator(List<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                Debug.Assert(source is not null);
                Debug.Assert(predicate is not null);
                Debug.Assert(selector is not null);
                _source = source;
                _predicate = predicate;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new ListWhereSelectIterator<TSource, TResult>(_source, _predicate, _selector);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        while (_enumerator.MoveNext())
                        {
                            TSource item = _enumerator.Current;
                            if (_predicate(item))
                            {
                                _current = _selector(item);
                                return true;
                            }
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new ListWhereSelectIterator<TSource, TResult2>(_source, _predicate, CombineSelectors(_selector, selector));
        }

        /// <summary>
        /// An iterator that filters, then maps, each item of an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        private sealed partial class IEnumerableWhereSelectIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, bool> _predicate;
            private readonly Func<TSource, TResult> _selector;
            private IEnumerator<TSource>? _enumerator;

            public IEnumerableWhereSelectIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                Debug.Assert(source is not null);
                Debug.Assert(predicate is not null);
                Debug.Assert(selector is not null);
                _source = source;
                _predicate = predicate;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new IEnumerableWhereSelectIterator<TSource, TResult>(_source, _predicate, _selector);

            public override void Dispose()
            {
                if (_enumerator is not null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        Debug.Assert(_enumerator is not null);
                        while (_enumerator.MoveNext())
                        {
                            TSource item = _enumerator.Current;
                            if (_predicate(item))
                            {
                                _current = _selector(item);
                                return true;
                            }
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new IEnumerableWhereSelectIterator<TSource, TResult2>(_source, _predicate, CombineSelectors(_selector, selector));
        }
    }
}
