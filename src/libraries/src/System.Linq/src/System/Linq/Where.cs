// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (predicate == null)
            {
                throw Error.ArgumentNull(nameof(predicate));
            }

            Iterator<TSource> iterator = source as Iterator<TSource>;
            if (iterator != null)
            {
                return iterator.Where(predicate);
            }

            TSource[] array = source as TSource[];
            if (array != null)
            {
                return new WhereArrayIterator<TSource>(array, predicate);
            }

            List<TSource> list = source as List<TSource>;
            if (list != null)
            {
                return new WhereListIterator<TSource>(list, predicate);
            }

            return new WhereEnumerableIterator<TSource>(source, predicate);
        }

        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (predicate == null)
            {
                throw Error.ArgumentNull(nameof(predicate));
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

        private static Func<TSource, bool> CombinePredicates<TSource>(Func<TSource, bool> predicate1, Func<TSource, bool> predicate2)
        {
            return x => predicate1(x) && predicate2(x);
        }

        internal class WhereEnumerableIterator<TSource> : Iterator<TSource>
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

            public override Iterator<TSource> Clone()
            {
                return new WhereEnumerableIterator<TSource>(_source, _predicate);
            }

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

            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
            {
                return new WhereSelectEnumerableIterator<TSource, TResult>(_source, _predicate, selector);
            }

            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate)
            {
                return new WhereEnumerableIterator<TSource>(_source, CombinePredicates(_predicate, predicate));
            }
        }

        internal class WhereArrayIterator<TSource> : Iterator<TSource>
        {
            private readonly TSource[] _source;
            private readonly Func<TSource, bool> _predicate;
            private int _index;

            public WhereArrayIterator(TSource[] source, Func<TSource, bool> predicate)
            {
                Debug.Assert(source != null);
                Debug.Assert(predicate != null);
                _source = source;
                _predicate = predicate;
            }

            public override Iterator<TSource> Clone()
            {
                return new WhereArrayIterator<TSource>(_source, _predicate);
            }

            public override bool MoveNext()
            {
                if (_state == 1)
                {
                    while (_index < _source.Length)
                    {
                        TSource item = _source[_index];
                        _index++;
                        if (_predicate(item))
                        {
                            _current = item;
                            return true;
                        }
                    }

                    Dispose();
                }

                return false;
            }

            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
            {
                return new WhereSelectArrayIterator<TSource, TResult>(_source, _predicate, selector);
            }

            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate)
            {
                return new WhereArrayIterator<TSource>(_source, CombinePredicates(_predicate, predicate));
            }
        }

        internal class WhereListIterator<TSource> : Iterator<TSource>
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

            public override Iterator<TSource> Clone()
            {
                return new WhereListIterator<TSource>(_source, _predicate);
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

            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
            {
                return new WhereSelectListIterator<TSource, TResult>(_source, _predicate, selector);
            }

            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate)
            {
                return new WhereListIterator<TSource>(_source, CombinePredicates(_predicate, predicate));
            }
        }

        internal class WhereSelectArrayIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly TSource[] _source;
            private readonly Func<TSource, bool> _predicate;
            private readonly Func<TSource, TResult> _selector;
            private int _index;

            public WhereSelectArrayIterator(TSource[] source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(predicate != null);
                Debug.Assert(selector != null);
                _source = source;
                _predicate = predicate;
                _selector = selector;
            }

            public override Iterator<TResult> Clone()
            {
                return new WhereSelectArrayIterator<TSource, TResult>(_source, _predicate, _selector);
            }

            public override bool MoveNext()
            {
                if (_state == 1)
                {
                    while (_index < _source.Length)
                    {
                        TSource item = _source[_index];
                        _index++;
                        if (_predicate(item))
                        {
                            _current = _selector(item);
                            return true;
                        }
                    }

                    Dispose();
                }

                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new WhereSelectArrayIterator<TSource, TResult2>(_source, _predicate, CombineSelectors(_selector, selector));
            }
        }

        internal class WhereSelectListIterator<TSource, TResult> : Iterator<TResult>
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

            public override Iterator<TResult> Clone()
            {
                return new WhereSelectListIterator<TSource, TResult>(_source, _predicate, _selector);
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

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new WhereSelectListIterator<TSource, TResult2>(_source, _predicate, CombineSelectors(_selector, selector));
            }
        }

        internal class WhereSelectEnumerableIterator<TSource, TResult> : Iterator<TResult>
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

            public override Iterator<TResult> Clone()
            {
                return new WhereSelectEnumerableIterator<TSource, TResult>(_source, _predicate, _selector);
            }

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

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new WhereSelectEnumerableIterator<TSource, TResult2>(_source, _predicate, CombineSelectors(_selector, selector));
            }
        }
    }
}
