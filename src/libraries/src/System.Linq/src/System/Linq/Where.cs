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
        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            if (source is Iterator<TSource> iterator)
            {
                return iterator.Where(predicate);
            }

            if (source is TSource[] array)
            {
                return array.Length == 0 ?
                    Empty<TSource>() :
                    new WhereArrayIterator<TSource>(array, predicate);
            }

            if (source is List<TSource> list)
            {
                return new WhereListIterator<TSource>(list, predicate);
            }

            return new WhereEnumerableIterator<TSource>(source, predicate);
        }

        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
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
        private sealed partial class WhereEnumerableIterator<TSource> : Iterator<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, bool> _predicate;
            private IEnumerator<TSource> _enumerator;

            public WhereEnumerableIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                Debug.Assert(source != null);
                Debug.Assert(predicate != null);
                _source = source;
                _predicate = predicate;
            }

            public override Iterator<TSource> Clone() => new WhereEnumerableIterator<TSource>(_source, _predicate);

            public override void Dispose()
            {
                if (_enumerator != null)
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
                new WhereSelectEnumerableIterator<TSource, TResult>(_source, _predicate, selector);

            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate) =>
                new WhereEnumerableIterator<TSource>(_source, CombinePredicates(_predicate, predicate));
        }

        /// <summary>
        /// An iterator that filters each item of a <see cref="T:TSource[]"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source array.</typeparam>
        internal sealed partial class WhereArrayIterator<TSource> : Iterator<TSource>
        {
            private readonly TSource[] _source;
            private readonly Func<TSource, bool> _predicate;

            public WhereArrayIterator(TSource[] source, Func<TSource, bool> predicate)
            {
                Debug.Assert(source != null && source.Length > 0);
                Debug.Assert(predicate != null);
                _source = source;
                _predicate = predicate;
            }

            public override Iterator<TSource> Clone() =>
                new WhereArrayIterator<TSource>(_source, _predicate);

            public override bool MoveNext()
            {
                int index = _state - 1;
                TSource[] source = _source;

                while (unchecked((uint)index < (uint)source.Length))
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
                new WhereSelectArrayIterator<TSource, TResult>(_source, _predicate, selector);

            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate) =>
                new WhereArrayIterator<TSource>(_source, CombinePredicates(_predicate, predicate));
        }

        /// <summary>
        /// An iterator that filters each item of a <see cref="List{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source list.</typeparam>
        private sealed partial class WhereListIterator<TSource> : Iterator<TSource>
        {
            private readonly List<TSource> _source;
            private readonly Func<TSource, bool> _predicate;
            private List<TSource>.Enumerator _enumerator;

            public WhereListIterator(List<TSource> source, Func<TSource, bool> predicate)
            {
                Debug.Assert(source != null);
                Debug.Assert(predicate != null);
                _source = source;
                _predicate = predicate;
            }

            public override Iterator<TSource> Clone() =>
                new WhereListIterator<TSource>(_source, _predicate);

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
                new WhereSelectListIterator<TSource, TResult>(_source, _predicate, selector);

            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate) =>
                new WhereListIterator<TSource>(_source, CombinePredicates(_predicate, predicate));
        }

        /// <summary>
        /// An iterator that filters, then maps, each item of a <see cref="T:TSource[]"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source array.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        private sealed partial class WhereSelectArrayIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly TSource[] _source;
            private readonly Func<TSource, bool> _predicate;
            private readonly Func<TSource, TResult> _selector;

            public WhereSelectArrayIterator(TSource[] source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null && source.Length > 0);
                Debug.Assert(predicate != null);
                Debug.Assert(selector != null);
                _source = source;
                _predicate = predicate;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new WhereSelectArrayIterator<TSource, TResult>(_source, _predicate, _selector);

            public override bool MoveNext()
            {
                int index = _state - 1;
                TSource[] source = _source;

                while (unchecked((uint)index < (uint)source.Length))
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
                new WhereSelectArrayIterator<TSource, TResult2>(_source, _predicate, CombineSelectors(_selector, selector));
        }

        /// <summary>
        /// An iterator that filters, then maps, each item of a <see cref="List{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source list.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        private sealed partial class WhereSelectListIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly List<TSource> _source;
            private readonly Func<TSource, bool> _predicate;
            private readonly Func<TSource, TResult> _selector;
            private List<TSource>.Enumerator _enumerator;

            public WhereSelectListIterator(List<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(predicate != null);
                Debug.Assert(selector != null);
                _source = source;
                _predicate = predicate;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new WhereSelectListIterator<TSource, TResult>(_source, _predicate, _selector);

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
                new WhereSelectListIterator<TSource, TResult2>(_source, _predicate, CombineSelectors(_selector, selector));
        }

        /// <summary>
        /// An iterator that filters, then maps, each item of an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        private sealed partial class WhereSelectEnumerableIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, bool> _predicate;
            private readonly Func<TSource, TResult> _selector;
            private IEnumerator<TSource> _enumerator;

            public WhereSelectEnumerableIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(predicate != null);
                Debug.Assert(selector != null);
                _source = source;
                _predicate = predicate;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new WhereSelectEnumerableIterator<TSource, TResult>(_source, _predicate, _selector);

            public override void Dispose()
            {
                if (_enumerator != null)
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
                new WhereSelectEnumerableIterator<TSource, TResult2>(_source, _predicate, CombineSelectors(_selector, selector));
        }
    }
}
