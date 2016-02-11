// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (selector == null) throw Error.ArgumentNull("selector");
            Iterator<TSource> iterator = source as Iterator<TSource>;
            if (iterator != null) return iterator.Select(selector);
            IList<TSource> ilist = source as IList<TSource>;
            if (ilist != null)
            {
                TSource[] array = source as TSource[];
                if (array != null) return new SelectArrayIterator<TSource, TResult>(array, selector);
                List<TSource> list = source as List<TSource>;
                if (list != null) return new SelectListIterator<TSource, TResult>(list, selector);
                return new SelectIListIterator<TSource, TResult>(ilist, selector);
            }
            return new SelectEnumerableIterator<TSource, TResult>(source, selector);
        }

        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (selector == null) throw Error.ArgumentNull("selector");
            return SelectIterator(source, selector);
        }

        private static IEnumerable<TResult> SelectIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked { index++; }
                yield return selector(element, index);
            }
        }

        private static Func<TSource, TResult> CombineSelectors<TSource, TMiddle, TResult>(Func<TSource, TMiddle> selector1, Func<TMiddle, TResult> selector2)
        {
            return x => selector2(selector1(x));
        }

        internal sealed class SelectEnumerableIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private IEnumerator<TSource> _enumerator;

            public SelectEnumerableIterator(IEnumerable<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone()
            {
                return new SelectEnumerableIterator<TSource, TResult>(_source, _selector);
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
                switch (state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        state = 2;
                        goto case 2;
                    case 2:
                        if (_enumerator.MoveNext())
                        {
                            current = _selector(_enumerator.Current);
                            return true;
                        }
                        Dispose();
                        break;
                }
                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new SelectEnumerableIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
            }
        }

        internal sealed class SelectArrayIterator<TSource, TResult> : Iterator<TResult>, IArrayProvider<TResult>, IListProvider<TResult>
        {
            private readonly TSource[] _source;
            private readonly Func<TSource, TResult> _selector;
            private int _index;

            public SelectArrayIterator(TSource[] source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone()
            {
                return new SelectArrayIterator<TSource, TResult>(_source, _selector);
            }

            public override bool MoveNext()
            {
                if (state == 1 && _index < _source.Length)
                {
                    current = _selector(_source[_index++]);
                    return true;
                }
                Dispose();
                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new SelectArrayIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
            }

            public TResult[] ToArray()
            {
                if (_source.Length == 0)
                {
                    return Array.Empty<TResult>();
                }

                var results = new TResult[_source.Length];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = _selector(_source[i]);
                }
                return results;
            }

            public List<TResult> ToList()
            {
                TSource[] source = _source;
                var results = new List<TResult>(source.Length);
                for (int i = 0; i < source.Length; i++)
                {
                    results.Add(_selector(source[i]));
                }
                return results;
            }
        }

        internal sealed class SelectListIterator<TSource, TResult> : Iterator<TResult>, IArrayProvider<TResult>, IListProvider<TResult>
        {
            private readonly List<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private List<TSource>.Enumerator _enumerator;

            public SelectListIterator(List<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone()
            {
                return new SelectListIterator<TSource, TResult>(_source, _selector);
            }

            public override bool MoveNext()
            {
                switch (state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        state = 2;
                        goto case 2;
                    case 2:
                        if (_enumerator.MoveNext())
                        {
                            current = _selector(_enumerator.Current);
                            return true;
                        }
                        Dispose();
                        break;
                }
                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new SelectListIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
            }

            public TResult[] ToArray()
            {
                int count = _source.Count;
                if (count == 0)
                {
                    return Array.Empty<TResult>();
                }

                var results = new TResult[count];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = _selector(_source[i]);
                }
                return results;
            }

            public List<TResult> ToList()
            {
                int count = _source.Count;
                var results = new List<TResult>(count);
                for (int i = 0; i < count; i++)
                {
                    results.Add(_selector(_source[i]));
                }
                return results;
            }
        }

        internal sealed class SelectIListIterator<TSource, TResult> : Iterator<TResult>, IArrayProvider<TResult>, IListProvider<TResult>
        {
            private readonly IList<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private IEnumerator<TSource> _enumerator;

            public SelectIListIterator(IList<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone()
            {
                return new SelectIListIterator<TSource, TResult>(_source, _selector);
            }

            public override bool MoveNext()
            {
                switch (state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        state = 2;
                        goto case 2;
                    case 2:
                        if (_enumerator.MoveNext())
                        {
                            current = _selector(_enumerator.Current);
                            return true;
                        }
                        Dispose();
                        break;
                }
                return false;
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

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new SelectIListIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
            }

            public TResult[] ToArray()
            {
                int count = _source.Count;
                if (count == 0)
                {
                    return Array.Empty<TResult>();
                }

                var results = new TResult[count];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = _selector(_source[i]);
                }
                return results;
            }

            public List<TResult> ToList()
            {
                int count = _source.Count;
                var results = new List<TResult>(count);
                for (int i = 0; i < count; i++)
                {
                    results.Add(_selector(_source[i]));
                }
                return results;
            }
        }
    }
}
