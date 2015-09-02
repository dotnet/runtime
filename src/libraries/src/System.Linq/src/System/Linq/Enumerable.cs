// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace System.Linq
{
    public static class Enumerable
    {
        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            Iterator<TSource> iterator = source as Iterator<TSource>;
            if (iterator != null) return iterator.Where(predicate);
            TSource[] array = source as TSource[];
            if (array != null) return new WhereArrayIterator<TSource>(array, predicate);
            List<TSource> list = source as List<TSource>;
            if (list != null) return new WhereListIterator<TSource>(list, predicate);
            return new WhereEnumerableIterator<TSource>(source, predicate);
        }

        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            return WhereIterator<TSource>(source, predicate);
        }

        private static IEnumerable<TSource> WhereIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked { index++; }
                if (predicate(element, index)) yield return element;
            }
        }

        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (selector == null) throw Error.ArgumentNull("selector");
            Iterator<TSource> iterator = source as Iterator<TSource>;
            if (iterator != null) return iterator.Select(selector);
            TSource[] array = source as TSource[];
            if (array != null) return new WhereSelectArrayIterator<TSource, TResult>(array, null, selector);
            List<TSource> list = source as List<TSource>;
            if (list != null) return new WhereSelectListIterator<TSource, TResult>(list, null, selector);
            return new WhereSelectEnumerableIterator<TSource, TResult>(source, null, selector);
        }

        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (selector == null) throw Error.ArgumentNull("selector");
            return SelectIterator<TSource, TResult>(source, selector);
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

        private static Func<TSource, bool> CombinePredicates<TSource>(Func<TSource, bool> predicate1, Func<TSource, bool> predicate2)
        {
            return x => predicate1(x) && predicate2(x);
        }

        private static Func<TSource, TResult> CombineSelectors<TSource, TMiddle, TResult>(Func<TSource, TMiddle> selector1, Func<TMiddle, TResult> selector2)
        {
            return x => selector2(selector1(x));
        }

        internal abstract class Iterator<TSource> : IEnumerable<TSource>, IEnumerator<TSource>
        {
            private int _threadId;
            internal int state;
            internal TSource current;

            public Iterator()
            {
                _threadId = Environment.CurrentManagedThreadId;
            }

            public TSource Current
            {
                get { return current; }
            }

            public abstract Iterator<TSource> Clone();

            public virtual void Dispose()
            {
                current = default(TSource);
                state = -1;
            }

            public IEnumerator<TSource> GetEnumerator()
            {
                Iterator<TSource> enumerator = state == 0 && _threadId == Environment.CurrentManagedThreadId ? this : Clone();
                enumerator.state = 1;
                return enumerator;
            }

            public abstract bool MoveNext();

            public abstract IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector);

            public abstract IEnumerable<TSource> Where(Func<TSource, bool> predicate);

            object IEnumerator.Current
            {
                get { return Current; }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            void IEnumerator.Reset()
            {
                throw Error.NotSupported();
            }
        }

        internal class WhereEnumerableIterator<TSource> : Iterator<TSource>
        {
            private IEnumerable<TSource> _source;
            private Func<TSource, bool> _predicate;
            private IEnumerator<TSource> _enumerator;

            public WhereEnumerableIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
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
                switch (state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        state = 2;
                        goto case 2;
                    case 2:
                        while (_enumerator.MoveNext())
                        {
                            TSource item = _enumerator.Current;
                            if (_predicate(item))
                            {
                                current = item;
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
            private TSource[] _source;
            private Func<TSource, bool> _predicate;
            private int _index;

            public WhereArrayIterator(TSource[] source, Func<TSource, bool> predicate)
            {
                _source = source;
                _predicate = predicate;
            }

            public override Iterator<TSource> Clone()
            {
                return new WhereArrayIterator<TSource>(_source, _predicate);
            }

            public override bool MoveNext()
            {
                if (state == 1)
                {
                    while (_index < _source.Length)
                    {
                        TSource item = _source[_index];
                        _index++;
                        if (_predicate(item))
                        {
                            current = item;
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
            private List<TSource> _source;
            private Func<TSource, bool> _predicate;
            private List<TSource>.Enumerator _enumerator;

            public WhereListIterator(List<TSource> source, Func<TSource, bool> predicate)
            {
                _source = source;
                _predicate = predicate;
            }

            public override Iterator<TSource> Clone()
            {
                return new WhereListIterator<TSource>(_source, _predicate);
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
                        while (_enumerator.MoveNext())
                        {
                            TSource item = _enumerator.Current;
                            if (_predicate(item))
                            {
                                current = item;
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

        internal class WhereSelectEnumerableIterator<TSource, TResult> : Iterator<TResult>
        {
            private IEnumerable<TSource> _source;
            private Func<TSource, bool> _predicate;
            private Func<TSource, TResult> _selector;
            private IEnumerator<TSource> _enumerator;

            public WhereSelectEnumerableIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
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
                switch (state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        state = 2;
                        goto case 2;
                    case 2:
                        while (_enumerator.MoveNext())
                        {
                            TSource item = _enumerator.Current;
                            if (_predicate == null || _predicate(item))
                            {
                                current = _selector(item);
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

            public override IEnumerable<TResult> Where(Func<TResult, bool> predicate)
            {
                return new WhereEnumerableIterator<TResult>(this, predicate);
            }
        }

        internal class WhereSelectArrayIterator<TSource, TResult> : Iterator<TResult>, IArrayProvider<TResult>
        {
            private TSource[] _source;
            private Func<TSource, bool> _predicate;
            private Func<TSource, TResult> _selector;
            private int _index;

            public WhereSelectArrayIterator(TSource[] source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
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
                if (state == 1)
                {
                    while (_index < _source.Length)
                    {
                        TSource item = _source[_index];
                        _index++;
                        if (_predicate == null || _predicate(item))
                        {
                            current = _selector(item);
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

            public override IEnumerable<TResult> Where(Func<TResult, bool> predicate)
            {
                return new WhereEnumerableIterator<TResult>(this, predicate);
            }

            public TResult[] ToArray()
            {
                if (_predicate != null && _source.Length != 0) return null;

                var results = new TResult[_source.Length];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = _selector(_source[i]);
                }
                return results;
            }
        }

        internal class WhereSelectListIterator<TSource, TResult> : Iterator<TResult>, IArrayProvider<TResult>
        {
            private List<TSource> _source;
            private Func<TSource, bool> _predicate;
            private Func<TSource, TResult> _selector;
            private List<TSource>.Enumerator _enumerator;

            public WhereSelectListIterator(List<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
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
                switch (state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        state = 2;
                        goto case 2;
                    case 2:
                        while (_enumerator.MoveNext())
                        {
                            TSource item = _enumerator.Current;
                            if (_predicate == null || _predicate(item))
                            {
                                current = _selector(item);
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

            public override IEnumerable<TResult> Where(Func<TResult, bool> predicate)
            {
                return new WhereEnumerableIterator<TResult>(this, predicate);
            }

            public TResult[] ToArray()
            {
                if (_predicate != null && _source.Count != 0) return null;

                var results = new TResult[_source.Count];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = _selector(_source[i]);
                }
                return results;
            }
        }

        //public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) {
        //    if (source == null) throw Error.ArgumentNull("source");
        //    if (predicate == null) throw Error.ArgumentNull("predicate");
        //    return WhereIterator<TSource>(source, predicate);
        //}

        //static IEnumerable<TSource> WhereIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate) {
        //    foreach (TSource element in source) {
        //        if (predicate(element)) yield return element;
        //    }
        //}

        //public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) {
        //    if (source == null) throw Error.ArgumentNull("source");
        //    if (selector == null) throw Error.ArgumentNull("selector");
        //    return SelectIterator<TSource, TResult>(source, selector);
        //}

        //static IEnumerable<TResult> SelectIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector) {
        //    foreach (TSource element in source) {
        //        yield return selector(element);
        //    }
        //}

        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (selector == null) throw Error.ArgumentNull("selector");
            return SelectManyIterator<TSource, TResult>(source, selector);
        }

        private static IEnumerable<TResult> SelectManyIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            foreach (TSource element in source)
            {
                foreach (TResult subElement in selector(element))
                {
                    yield return subElement;
                }
            }
        }

        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (selector == null) throw Error.ArgumentNull("selector");
            return SelectManyIterator<TSource, TResult>(source, selector);
        }

        private static IEnumerable<TResult> SelectManyIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked { index++; }
                foreach (TResult subElement in selector(element, index))
                {
                    yield return subElement;
                }
            }
        }
        public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (collectionSelector == null) throw Error.ArgumentNull("collectionSelector");
            if (resultSelector == null) throw Error.ArgumentNull("resultSelector");
            return SelectManyIterator<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
        }

        private static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked { index++; }
                foreach (TCollection subElement in collectionSelector(element, index))
                {
                    yield return resultSelector(element, subElement);
                }
            }
        }

        public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (collectionSelector == null) throw Error.ArgumentNull("collectionSelector");
            if (resultSelector == null) throw Error.ArgumentNull("resultSelector");
            return SelectManyIterator<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
        }

        private static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            foreach (TSource element in source)
            {
                foreach (TCollection subElement in collectionSelector(element))
                {
                    yield return resultSelector(element, subElement);
                }
            }
        }

        public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return TakeIterator<TSource>(source, count);
        }

        private static IEnumerable<TSource> TakeIterator<TSource>(IEnumerable<TSource> source, int count)
        {
            if (count > 0)
            {
                foreach (TSource element in source)
                {
                    yield return element;
                    if (--count == 0) break;
                }
            }
        }

        public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            return TakeWhileIterator<TSource>(source, predicate);
        }

        private static IEnumerable<TSource> TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            foreach (TSource element in source)
            {
                if (!predicate(element)) break;
                yield return element;
            }
        }

        public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            return TakeWhileIterator<TSource>(source, predicate);
        }

        private static IEnumerable<TSource> TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked { index++; }
                if (!predicate(element, index)) break;
                yield return element;
            }
        }

        public static IEnumerable<TSource> Skip<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return SkipIterator<TSource>(source, count);
        }

        private static IEnumerable<TSource> SkipIterator<TSource>(IEnumerable<TSource> source, int count)
        {
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (count > 0 && e.MoveNext()) count--;
                if (count <= 0)
                {
                    while (e.MoveNext()) yield return e.Current;
                }
            }
        }

        public static IEnumerable<TSource> SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            return SkipWhileIterator<TSource>(source, predicate);
        }

        private static IEnumerable<TSource> SkipWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            bool yielding = false;
            foreach (TSource element in source)
            {
                if (!yielding && !predicate(element)) yielding = true;
                if (yielding) yield return element;
            }
        }

        public static IEnumerable<TSource> SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            return SkipWhileIterator<TSource>(source, predicate);
        }

        private static IEnumerable<TSource> SkipWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            int index = -1;
            bool yielding = false;
            foreach (TSource element in source)
            {
                checked { index++; }
                if (!yielding && !predicate(element, index)) yielding = true;
                if (yielding) yield return element;
            }
        }

        public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
        {
            if (outer == null) throw Error.ArgumentNull("outer");
            if (inner == null) throw Error.ArgumentNull("inner");
            if (outerKeySelector == null) throw Error.ArgumentNull("outerKeySelector");
            if (innerKeySelector == null) throw Error.ArgumentNull("innerKeySelector");
            if (resultSelector == null) throw Error.ArgumentNull("resultSelector");
            return JoinIterator<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
        }

        public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            if (outer == null) throw Error.ArgumentNull("outer");
            if (inner == null) throw Error.ArgumentNull("inner");
            if (outerKeySelector == null) throw Error.ArgumentNull("outerKeySelector");
            if (innerKeySelector == null) throw Error.ArgumentNull("innerKeySelector");
            if (resultSelector == null) throw Error.ArgumentNull("resultSelector");
            return JoinIterator<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
        }

        private static IEnumerable<TResult> JoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            Lookup<TKey, TInner> lookup = Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
            foreach (TOuter item in outer)
            {
                Grouping<TKey, TInner> g = lookup.GetGrouping(outerKeySelector(item), false);
                if (g != null)
                {
                    for (int i = 0; i < g.count; i++)
                    {
                        yield return resultSelector(item, g.elements[i]);
                    }
                }
            }
        }

        public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector)
        {
            if (outer == null) throw Error.ArgumentNull("outer");
            if (inner == null) throw Error.ArgumentNull("inner");
            if (outerKeySelector == null) throw Error.ArgumentNull("outerKeySelector");
            if (innerKeySelector == null) throw Error.ArgumentNull("innerKeySelector");
            if (resultSelector == null) throw Error.ArgumentNull("resultSelector");
            return GroupJoinIterator<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
        }

        public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            if (outer == null) throw Error.ArgumentNull("outer");
            if (inner == null) throw Error.ArgumentNull("inner");
            if (outerKeySelector == null) throw Error.ArgumentNull("outerKeySelector");
            if (innerKeySelector == null) throw Error.ArgumentNull("innerKeySelector");
            if (resultSelector == null) throw Error.ArgumentNull("resultSelector");
            return GroupJoinIterator<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
        }

        private static IEnumerable<TResult> GroupJoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            Lookup<TKey, TInner> lookup = Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
            foreach (TOuter item in outer)
            {
                yield return resultSelector(item, lookup[outerKeySelector(item)]);
            }
        }

        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return new OrderedEnumerable<TSource, TKey>(source, keySelector, null, false);
        }

        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            return new OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, false);
        }

        public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return new OrderedEnumerable<TSource, TKey>(source, keySelector, null, true);
        }

        public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            return new OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, true);
        }

        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return source.CreateOrderedEnumerable<TKey>(keySelector, null, false);
        }

        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return source.CreateOrderedEnumerable<TKey>(keySelector, comparer, false);
        }

        public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return source.CreateOrderedEnumerable<TKey>(keySelector, null, true);
        }

        public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return source.CreateOrderedEnumerable<TKey>(keySelector, comparer, true);
        }

        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return new GroupedEnumerable<TSource, TKey, TSource>(source, keySelector, IdentityFunction<TSource>.Instance, null);
        }

        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return new GroupedEnumerable<TSource, TKey, TSource>(source, keySelector, IdentityFunction<TSource>.Instance, comparer);
        }

        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return new GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, null);
        }

        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            return new GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer);
        }

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector)
        {
            return new GroupedEnumerable<TSource, TKey, TSource, TResult>(source, keySelector, IdentityFunction<TSource>.Instance, resultSelector, null);
        }

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            return new GroupedEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, null);
        }

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            return new GroupedEnumerable<TSource, TKey, TSource, TResult>(source, keySelector, IdentityFunction<TSource>.Instance, resultSelector, comparer);
        }

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            return new GroupedEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, comparer);
        }

        public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null) throw Error.ArgumentNull("first");
            if (second == null) throw Error.ArgumentNull("second");
            return ConcatIterator<TSource>(first, second);
        }

        private static IEnumerable<TSource> ConcatIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            foreach (TSource element in first) yield return element;
            foreach (TSource element in second) yield return element;
        }

        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            if (first == null) throw Error.ArgumentNull("first");
            if (second == null) throw Error.ArgumentNull("second");
            if (resultSelector == null) throw Error.ArgumentNull("resultSelector");
            return ZipIterator(first, second, resultSelector);
        }

        private static IEnumerable<TResult> ZipIterator<TFirst, TSecond, TResult>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            using (IEnumerator<TFirst> e1 = first.GetEnumerator())
            using (IEnumerator<TSecond> e2 = second.GetEnumerator())
                while (e1.MoveNext() && e2.MoveNext())
                    yield return resultSelector(e1.Current, e2.Current);
        }


        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return DistinctIterator<TSource>(source, null);
        }

        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return DistinctIterator<TSource>(source, comparer);
        }

        private static IEnumerable<TSource> DistinctIterator<TSource>(IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
        {
            Set<TSource> set = new Set<TSource>(comparer);
            foreach (TSource element in source)
                if (set.Add(element)) yield return element;
        }

        public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null) throw Error.ArgumentNull("first");
            if (second == null) throw Error.ArgumentNull("second");
            return UnionIterator<TSource>(first, second, null);
        }

        public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            if (first == null) throw Error.ArgumentNull("first");
            if (second == null) throw Error.ArgumentNull("second");
            return UnionIterator<TSource>(first, second, comparer);
        }

        private static IEnumerable<TSource> UnionIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            Set<TSource> set = new Set<TSource>(comparer);
            foreach (TSource element in first)
                if (set.Add(element)) yield return element;
            foreach (TSource element in second)
                if (set.Add(element)) yield return element;
        }

        public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null) throw Error.ArgumentNull("first");
            if (second == null) throw Error.ArgumentNull("second");
            return IntersectIterator<TSource>(first, second, null);
        }

        public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            if (first == null) throw Error.ArgumentNull("first");
            if (second == null) throw Error.ArgumentNull("second");
            return IntersectIterator<TSource>(first, second, comparer);
        }

        private static IEnumerable<TSource> IntersectIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            Set<TSource> set = new Set<TSource>(comparer);
            foreach (TSource element in second) set.Add(element);
            foreach (TSource element in first)
                if (set.Remove(element)) yield return element;
        }

        public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null) throw Error.ArgumentNull("first");
            if (second == null) throw Error.ArgumentNull("second");
            return ExceptIterator<TSource>(first, second, null);
        }

        public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            if (first == null) throw Error.ArgumentNull("first");
            if (second == null) throw Error.ArgumentNull("second");
            return ExceptIterator<TSource>(first, second, comparer);
        }

        private static IEnumerable<TSource> ExceptIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            Set<TSource> set = new Set<TSource>(comparer);
            foreach (TSource element in second) set.Add(element);
            foreach (TSource element in first)
                if (set.Add(element)) yield return element;
        }

        public static IEnumerable<TSource> Reverse<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return ReverseIterator<TSource>(source);
        }

        private static IEnumerable<TSource> ReverseIterator<TSource>(IEnumerable<TSource> source)
        {
            Buffer<TSource> buffer = new Buffer<TSource>(source);
            for (int i = buffer.count - 1; i >= 0; i--) yield return buffer.items[i];
        }

        public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            return SequenceEqual<TSource>(first, second, null);
        }

        public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            if (comparer == null) comparer = EqualityComparer<TSource>.Default;
            if (first == null) throw Error.ArgumentNull("first");
            if (second == null) throw Error.ArgumentNull("second");
            using (IEnumerator<TSource> e1 = first.GetEnumerator())
            using (IEnumerator<TSource> e2 = second.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    if (!(e2.MoveNext() && comparer.Equals(e1.Current, e2.Current))) return false;
                }
                if (e2.MoveNext()) return false;
            }
            return true;
        }

        public static IEnumerable<TSource> AsEnumerable<TSource>(this IEnumerable<TSource> source)
        {
            return source;
        }

        public static TSource[] ToArray<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            IArrayProvider<TSource> arrayProvider = source as IArrayProvider<TSource>;
            if (arrayProvider != null)
            {
                TSource[] array = arrayProvider.ToArray();
                if (array != null) return array;
            }
            return new Buffer<TSource>(source).ToArray();
        }

        public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return new List<TSource>(source);
        }

        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return ToDictionary<TSource, TKey, TSource>(source, keySelector, IdentityFunction<TSource>.Instance, null);
        }

        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return ToDictionary<TSource, TKey, TSource>(source, keySelector, IdentityFunction<TSource>.Instance, comparer);
        }

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return ToDictionary<TSource, TKey, TElement>(source, keySelector, elementSelector, null);
        }

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (keySelector == null) throw Error.ArgumentNull("keySelector");
            if (elementSelector == null) throw Error.ArgumentNull("elementSelector");
            Dictionary<TKey, TElement> d = new Dictionary<TKey, TElement>(comparer);
            foreach (TSource element in source) d.Add(keySelector(element), elementSelector(element));
            return d;
        }

        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return Lookup<TKey, TSource>.Create(source, keySelector, IdentityFunction<TSource>.Instance, null);
        }

        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return Lookup<TKey, TSource>.Create(source, keySelector, IdentityFunction<TSource>.Instance, comparer);
        }

        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return Lookup<TKey, TElement>.Create(source, keySelector, elementSelector, null);
        }

        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            return Lookup<TKey, TElement>.Create(source, keySelector, elementSelector, comparer);
        }

        public static IEnumerable<TSource> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source)
        {
            return DefaultIfEmpty(source, default(TSource));
        }

        public static IEnumerable<TSource> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return DefaultIfEmptyIterator<TSource>(source, defaultValue);
        }

        private static IEnumerable<TSource> DefaultIfEmptyIterator<TSource>(IEnumerable<TSource> source, TSource defaultValue)
        {
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    do
                    {
                        yield return e.Current;
                    } while (e.MoveNext());
                }
                else
                {
                    yield return defaultValue;
                }
            }
        }

        public static IEnumerable<TResult> OfType<TResult>(this IEnumerable source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return OfTypeIterator<TResult>(source);
        }

        private static IEnumerable<TResult> OfTypeIterator<TResult>(IEnumerable source)
        {
            foreach (object obj in source)
            {
                if (obj is TResult) yield return (TResult)obj;
            }
        }

        public static IEnumerable<TResult> Cast<TResult>(this IEnumerable source)
        {
            IEnumerable<TResult> typedSource = source as IEnumerable<TResult>;
            if (typedSource != null) return typedSource;
            if (source == null) throw Error.ArgumentNull("source");
            return CastIterator<TResult>(source);
        }

        private static IEnumerable<TResult> CastIterator<TResult>(IEnumerable source)
        {
            foreach (object obj in source) yield return (TResult)obj;
        }

        public static TSource First<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                if (list.Count > 0) return list[0];
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (e.MoveNext()) return e.Current;
                }
            }
            throw Error.NoElements();
        }

        public static TSource First<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            foreach (TSource element in source)
            {
                if (predicate(element)) return element;
            }
            throw Error.NoMatch();
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                if (list.Count > 0) return list[0];
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (e.MoveNext()) return e.Current;
                }
            }
            return default(TSource);
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            foreach (TSource element in source)
            {
                if (predicate(element)) return element;
            }
            return default(TSource);
        }

        public static TSource Last<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                int count = list.Count;
                if (count > 0) return list[count - 1];
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (e.MoveNext())
                    {
                        TSource result;
                        do
                        {
                            result = e.Current;
                        } while (e.MoveNext());
                        return result;
                    }
                }
            }
            throw Error.NoElements();
        }

        public static TSource Last<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    TSource result = list[i];
                    if (predicate(result)) return result;
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    while (e.MoveNext())
                    {
                        TSource result = e.Current;
                        if (predicate(result))
                        {
                            while (e.MoveNext())
                            {
                                TSource element = e.Current;
                                if (predicate(element)) result = element;
                            }
                            return result;
                        }
                    }
                }
            }
            throw Error.NoMatch();
        }

        public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                int count = list.Count;
                if (count > 0) return list[count - 1];
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (e.MoveNext())
                    {
                        TSource result;
                        do
                        {
                            result = e.Current;
                        } while (e.MoveNext());
                        return result;
                    }
                }
            }
            return default(TSource);
        }

        public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    TSource element = list[i];
                    if (predicate(element)) return element;
                }
                return default(TSource);
            }
            else
            {
                TSource result = default(TSource);
                foreach (TSource element in source)
                {
                    if (predicate(element))
                    {
                        result = element;
                    }
                }
                return result;
            }
        }

        public static TSource Single<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                switch (list.Count)
                {
                    case 0: throw Error.NoElements();
                    case 1: return list[0];
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (!e.MoveNext()) throw Error.NoElements();
                    TSource result = e.Current;
                    if (!e.MoveNext()) return result;
                }
            }
            throw Error.MoreThanOneElement();
        }

        public static TSource Single<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    TSource result = e.Current;
                    if (predicate(result))
                    {
                        while (e.MoveNext())
                        {
                            if (predicate(e.Current)) throw Error.MoreThanOneMatch();
                        }
                        return result;
                    }
                }
            }
            throw Error.NoMatch();
        }

        public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                switch (list.Count)
                {
                    case 0: return default(TSource);
                    case 1: return list[0];
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (!e.MoveNext()) return default(TSource);
                    TSource result = e.Current;
                    if (!e.MoveNext()) return result;
                }
            }
            throw Error.MoreThanOneElement();
        }

        public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    TSource result = e.Current;
                    if (predicate(result))
                    {
                        while (e.MoveNext())
                        {
                            if (predicate(e.Current)) throw Error.MoreThanOneMatch();
                        }
                        return result;
                    }
                }
            }
            return default(TSource);
        }

        public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
        {
            if (source == null) throw Error.ArgumentNull("source");
            IList<TSource> list = source as IList<TSource>;
            if (list != null) return list[index];
            if (index < 0) throw Error.ArgumentOutOfRange("index");
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (true)
                {
                    if (!e.MoveNext()) throw Error.ArgumentOutOfRange("index");
                    if (index == 0) return e.Current;
                    index--;
                }
            }
        }

        public static TSource ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int index)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (index >= 0)
            {
                IList<TSource> list = source as IList<TSource>;
                if (list != null)
                {
                    if (index < list.Count) return list[index];
                }
                else
                {
                    using (IEnumerator<TSource> e = source.GetEnumerator())
                    {
                        while (true)
                        {
                            if (!e.MoveNext()) break;
                            if (index == 0) return e.Current;
                            index--;
                        }
                    }
                }
            }
            return default(TSource);
        }

        public static IEnumerable<int> Range(int start, int count)
        {
            long max = ((long)start) + count - 1;
            if (count < 0 || max > Int32.MaxValue) throw Error.ArgumentOutOfRange("count");
            return new RangeIterator(start, count);
        }

        private sealed class RangeIterator : Iterator<int>, IArrayProvider<int>
        {
            private readonly int _start;
            private readonly int _end;
            
            public RangeIterator(int start, int count)
            {
                _start = start;
                _end = start + count;
            }
            
            public override Iterator<int> Clone()
            {
                return new RangeIterator(_start, _end - _start);
            }
            
            public override bool MoveNext()
            {
                switch(state)
                {
                    case 1:
                        if (_start == _end) break;
                        current = _start;
                        state = 2;
                        return true;
                    case 2:
                        if (++current == _end)
                        {
                            --current; // Not crucial but maintains compatibility with previous versions. 
                            break;
                        }
                        return true;
                }
                state = -1;
                return false;
            }
            
            public override void Dispose()
            {
                state = -1; // Don't reset current
            }

            public override IEnumerable<TResult> Select<TResult>(Func<int, TResult> selector)
            {
                return new WhereSelectEnumerableIterator<int, TResult>(this, null, selector);
            }

            public override IEnumerable<int> Where(Func<int, bool> predicate)
            {
                return new WhereEnumerableIterator<int>(this, predicate);
            }

            public int[] ToArray()
            {
                int[] array = new int[_end - _start];
                int cur = _start;
                for (int i = 0; i != array.Length; ++i)
                {
                    array[i] = cur;
                    ++cur;
                }

                return array;
            }
        }

        public static IEnumerable<TResult> Repeat<TResult>(TResult element, int count)
        {
            if (count < 0) throw Error.ArgumentOutOfRange("count");
            return new RepeatIterator<TResult>(element, count);
        }

        private sealed class RepeatIterator<TResult> : Iterator<TResult>, IArrayProvider<TResult>
        {
            private readonly int _count;
            private int _sent;

            public RepeatIterator(TResult element, int count)
            {
                current = element;
                _count = count;
            }
            
            public override Iterator<TResult> Clone()
            {
                return new RepeatIterator<TResult>(current, _count);
            }
            
            public override void Dispose()
            {
                // Don't let base Dispose wipe current.
                state = -1;
            }
            
            public override bool MoveNext()
            {
                if (state == 1 & _sent != _count)
                {
                    ++_sent;
                    return true;
                }
                state = -1;
                return false;
            }

            public override IEnumerable<TSelected> Select<TSelected>(Func<TResult, TSelected> selector)
            {
                return new WhereSelectEnumerableIterator<TResult, TSelected>(this, null, selector);
            }

            public override IEnumerable<TResult> Where(Func<TResult, bool> predicate)
            {
                return new WhereEnumerableIterator<TResult>(this, predicate);
            }
            
            public TResult[] ToArray()
            {
                TResult[] array = new TResult[_count];
                if (current != null)
                {
                    for (int i = 0; i != array.Length; ++i) array[i] = current;
                }

                return array;
            }
        }

        public static IEnumerable<TResult> Empty<TResult>()
        {
            return Array.Empty<TResult>();
        }

        public static bool Any<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                return e.MoveNext();
            }
        }

        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            foreach (TSource element in source)
            {
                if (predicate(element)) return true;
            }
            return false;
        }

        public static bool All<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            foreach (TSource element in source)
            {
                if (!predicate(element)) return false;
            }
            return true;
        }

        public static int Count<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            ICollection<TSource> collectionoft = source as ICollection<TSource>;
            if (collectionoft != null) return collectionoft.Count;
            ICollection collection = source as ICollection;
            if (collection != null) return collection.Count;
            int count = 0;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                checked
                {
                    while (e.MoveNext()) count++;
                }
            }
            return count;
        }

        public static int Count<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            int count = 0;
            foreach (TSource element in source)
            {
                checked
                {
                    if (predicate(element)) count++;
                }
            }
            return count;
        }

        public static long LongCount<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            long count = 0;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                checked
                {
                    while (e.MoveNext()) count++;
                }
            }
            return count;
        }

        public static long LongCount<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            long count = 0;
            foreach (TSource element in source)
            {
                checked
                {
                    if (predicate(element)) count++;
                }
            }
            return count;
        }

        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value)
        {
            ICollection<TSource> collection = source as ICollection<TSource>;
            if (collection != null) return collection.Contains(value);
            return Contains<TSource>(source, value, null);
        }

        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value, IEqualityComparer<TSource> comparer)
        {
            if (comparer == null) comparer = EqualityComparer<TSource>.Default;
            if (source == null) throw Error.ArgumentNull("source");
            foreach (TSource element in source)
                if (comparer.Equals(element, value)) return true;
            return false;
        }

        public static TSource Aggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (func == null) throw Error.ArgumentNull("func");
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                TSource result = e.Current;
                while (e.MoveNext()) result = func(result, e.Current);
                return result;
            }
        }

        public static TAccumulate Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (func == null) throw Error.ArgumentNull("func");
            TAccumulate result = seed;
            foreach (TSource element in source) result = func(result, element);
            return result;
        }

        public static TResult Aggregate<TSource, TAccumulate, TResult>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (func == null) throw Error.ArgumentNull("func");
            if (resultSelector == null) throw Error.ArgumentNull("resultSelector");
            TAccumulate result = seed;
            foreach (TSource element in source) result = func(result, element);
            return resultSelector(result);
        }

        public static int Sum(this IEnumerable<int> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            int sum = 0;
            checked
            {
                foreach (int v in source) sum += v;
            }
            return sum;
        }

        public static int? Sum(this IEnumerable<int?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            int sum = 0;
            checked
            {
                foreach (int? v in source)
                {
                    if (v != null) sum += v.GetValueOrDefault();
                }
            }
            return sum;
        }

        public static long Sum(this IEnumerable<long> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            long sum = 0;
            checked
            {
                foreach (long v in source) sum += v;
            }
            return sum;
        }

        public static long? Sum(this IEnumerable<long?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            long sum = 0;
            checked
            {
                foreach (long? v in source)
                {
                    if (v != null) sum += v.GetValueOrDefault();
                }
            }
            return sum;
        }

        public static float Sum(this IEnumerable<float> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            double sum = 0;
            foreach (float v in source) sum += v;
            return (float)sum;
        }

        public static float? Sum(this IEnumerable<float?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            double sum = 0;
            foreach (float? v in source)
            {
                if (v != null) sum += v.GetValueOrDefault();
            }
            return (float)sum;
        }

        public static double Sum(this IEnumerable<double> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            double sum = 0;
            foreach (double v in source) sum += v;
            return sum;
        }

        public static double? Sum(this IEnumerable<double?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            double sum = 0;
            foreach (double? v in source)
            {
                if (v != null) sum += v.GetValueOrDefault();
            }
            return sum;
        }

        public static decimal Sum(this IEnumerable<decimal> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            decimal sum = 0;
            foreach (decimal v in source) sum += v;
            return sum;
        }

        public static decimal? Sum(this IEnumerable<decimal?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            decimal sum = 0;
            foreach (decimal? v in source)
            {
                if (v != null) sum += v.GetValueOrDefault();
            }
            return sum;
        }

        public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            return Enumerable.Sum(Enumerable.Select(source, selector));
        }

        public static int? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            return Enumerable.Sum(Enumerable.Select(source, selector));
        }

        public static long Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            return Enumerable.Sum(Enumerable.Select(source, selector));
        }

        public static long? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            return Enumerable.Sum(Enumerable.Select(source, selector));
        }

        public static float Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            return Enumerable.Sum(Enumerable.Select(source, selector));
        }

        public static float? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            return Enumerable.Sum(Enumerable.Select(source, selector));
        }

        public static double Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            return Enumerable.Sum(Enumerable.Select(source, selector));
        }

        public static double? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            return Enumerable.Sum(Enumerable.Select(source, selector));
        }

        public static decimal Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            return Enumerable.Sum(Enumerable.Select(source, selector));
        }

        public static decimal? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            return Enumerable.Sum(Enumerable.Select(source, selector));
        }

        public static int Min(this IEnumerable<int> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            int value;
            using (IEnumerator<int> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                value = e.Current;
                while (e.MoveNext())
                {
                    int x = e.Current;
                    if (x < value) value = x;
                }
            }
            return value;
        }

        public static int? Min(this IEnumerable<int?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            int? value = null;
            using (IEnumerator<int?> e = source.GetEnumerator())
            {
                // Start off knowing that we've a non-null value (or exit here, knowing we don't)
                // so we don't have to keep testing for nullity.
                do
                {
                    if (!e.MoveNext()) return value;
                    value = e.Current;
                } while (!value.HasValue);
                // Keep hold of the wrapped value, and do comparisons on that, rather than
                // using the lifted operation each time.
                int valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    int? cur = e.Current;
                    int x = cur.GetValueOrDefault();
                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen. 
                    if (cur.HasValue & x < valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }
            return value;
        }

        public static long Min(this IEnumerable<long> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            long value;
            using (IEnumerator<long> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                value = e.Current;
                while (e.MoveNext())
                {
                    long x = e.Current;
                    if (x < value) value = x;
                }
            }
            return value;
        }

        public static long? Min(this IEnumerable<long?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            long? value = null;
            using (IEnumerator<long?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext()) return value;
                    value = e.Current;
                } while (!value.HasValue);
                long valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    long? cur = e.Current;
                    long x = cur.GetValueOrDefault();
                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen. 
                    if (cur.HasValue & x < valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }
            return value;
        }
        
        public static float Min(this IEnumerable<float> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            float value;
            using (IEnumerator<float> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                value = e.Current;
                while(e.MoveNext())
                {
                    float x = e.Current;
                    if (x < value) value = x;
                    // Normally NaN < anything is false, as is anything < NaN
                    // However, this leads to some irksome outcomes in Min and Max.
                    // If we use those semantics then Min(NaN, 5.0) is NaN, but
                    // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
                    // ordering where NaN is smaller than every value, including
                    // negative infinity.
                    // Not testing for NaN therefore isn't an option, but since we
                    // can't find a smaller value, we can short-circuit.
                    else if (float.IsNaN(x)) return x;
                }
            }
            return value;
        }

        public static float? Min(this IEnumerable<float?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            float? value = null;
            using (IEnumerator<float?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext()) return value;
                    value = e.Current;
                } while (!value.HasValue);
                float valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    float? cur = e.Current;
                    if (cur.HasValue)
                    {
                        float x = cur.GetValueOrDefault();
                        if (x < valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                        else if (float.IsNaN(x)) return cur;
                    }
                }
            }
            return value;
        }

        public static double Min(this IEnumerable<double> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            double value;
            using (IEnumerator<double> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                value = e.Current;
                while(e.MoveNext())
                {
                    double x = e.Current;
                    if (x < value) value = x;
                    else if (double.IsNaN(x)) return x;
                }
            }
            return value;
        }

        public static double? Min(this IEnumerable<double?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            double? value = null;
            using (IEnumerator<double?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext()) return value;
                    value = e.Current;
                } while (!value.HasValue);
                double valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    double? cur = e.Current;
                    if (cur.HasValue)
                    {
                        double x = cur.GetValueOrDefault();
                        if (x < valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                        else if (double.IsNaN(x)) return cur;
                    }
                }
            }
            return value;
        }

        public static decimal Min(this IEnumerable<decimal> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            decimal value;
            using (IEnumerator<decimal> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                value = e.Current;
                while (e.MoveNext())
                {
                    decimal x = e.Current;
                    if (x < value) value = x;
                }
            }
            return value;
        }

        public static decimal? Min(this IEnumerable<decimal?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            decimal? value = null;
            using (IEnumerator<decimal?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext()) return value;
                    value = e.Current;
                } while (!value.HasValue);
                decimal valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    decimal? cur = e.Current;
                    decimal x = cur.GetValueOrDefault();
                    if (cur.HasValue && x < valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }
            return value;
        }

        public static TSource Min<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            Comparer<TSource> comparer = Comparer<TSource>.Default;
            TSource value = default(TSource);
            if (value == null)
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    do
                    {
                        if (!e.MoveNext()) return value;
                        value = e.Current;
                    } while (value == null);
                    while (e.MoveNext())
                    {
                        TSource x = e.Current;
                        if (x != null && comparer.Compare(x, value) < 0) value = x;
                    }
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (!e.MoveNext()) throw Error.NoElements();
                    value = e.Current;
                    while (e.MoveNext())
                    {
                        TSource x = e.Current;
                        if (comparer.Compare(x, value) < 0) value = x;
                    }
                }
            }
            return value;
        }

        public static int Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            return Enumerable.Min(Enumerable.Select(source, selector));
        }

        public static int? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            return Enumerable.Min(Enumerable.Select(source, selector));
        }

        public static long Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            return Enumerable.Min(Enumerable.Select(source, selector));
        }

        public static long? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            return Enumerable.Min(Enumerable.Select(source, selector));
        }

        public static float Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            return Enumerable.Min(Enumerable.Select(source, selector));
        }

        public static float? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            return Enumerable.Min(Enumerable.Select(source, selector));
        }

        public static double Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            return Enumerable.Min(Enumerable.Select(source, selector));
        }

        public static double? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            return Enumerable.Min(Enumerable.Select(source, selector));
        }

        public static decimal Min<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            return Enumerable.Min(Enumerable.Select(source, selector));
        }

        public static decimal? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            return Enumerable.Min(Enumerable.Select(source, selector));
        }

        public static TResult Min<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            return Enumerable.Min(Enumerable.Select(source, selector));
        }

        public static int Max(this IEnumerable<int> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            int value;
            using (IEnumerator<int> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                value = e.Current;
                while (e.MoveNext())
                {
                    int x = e.Current;
                    if (x > value) value = x;
                }
            }
            return value;
        }

        public static int? Max(this IEnumerable<int?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            int? value = null;
            using (IEnumerator<int?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext()) return value;
                    value = e.Current;
                } while (!value.HasValue);
                int valueVal = value.GetValueOrDefault();
                if (valueVal >= 0)
                {
                    // We can fast-path this case where we know HasValue will
                    // never affect the outcome, without constantly checking
                    // if we're in such a state. Similar fast-paths could
                    // be done for other cases, but as all-positive
                    // or mostly-positive integer values are quite common in real-world
                    // uses, it's only been done in this direction for int? and long?.
                    while (e.MoveNext())
                    {
                        int? cur = e.Current;
                        int x = cur.GetValueOrDefault();
                        if (x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
                else
                {
                    while (e.MoveNext())
                    {
                        int? cur = e.Current;
                        int x = cur.GetValueOrDefault();
                        // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                        // unless nulls either never happen or always happen. 
                        if (cur.HasValue & x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
            }
            return value;
        }

        public static long Max(this IEnumerable<long> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            long value;
            using (IEnumerator<long> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                value = e.Current;
                while (e.MoveNext())
                {
                    long x = e.Current;
                    if (x > value) value = x;
                }
            }
            return value;
        }

        public static long? Max(this IEnumerable<long?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            long? value = null;
            using (IEnumerator<long?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext()) return value;
                    value = e.Current;
                } while (!value.HasValue);
                long valueVal = value.GetValueOrDefault();
                if (valueVal >= 0)
                {
                    while (e.MoveNext())
                    {
                        long? cur = e.Current;
                        long x = cur.GetValueOrDefault();
                        if (x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
                else
                {
                    while (e.MoveNext())
                    {
                        long? cur = e.Current;
                        long x = cur.GetValueOrDefault();
                        // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                        // unless nulls either never happen or always happen. 
                        if (cur.HasValue & x > valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                    }
                }
            }
            return value;
        }

        public static double Max(this IEnumerable<double> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            double value;
            using (IEnumerator<double> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                value = e.Current;
                // As described in a comment on Min(this IEnumerable<double>) NaN is ordered
                // less than all other values. We need to do explicit checks to ensure this, but
                // once we've found a value that is not NaN we need no longer worry about it,
                // so first loop until such a value is found (or not, as the case may be).
                while (double.IsNaN(value))
                {
                    if (!e.MoveNext()) return value;
                    value = e.Current;
                }
                while (e.MoveNext())
                {
                    double x = e.Current;
                    if (x > value) value = x;
                }
            }
            return value;
        }

        public static double? Max(this IEnumerable<double?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            double? value = null;
            using (IEnumerator<double?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext()) return value;
                    value = e.Current;
                } while (!value.HasValue);
                double valueVal = value.GetValueOrDefault();
                while (double.IsNaN(valueVal))
                {
                    if (!e.MoveNext()) return value;
                    double? cur = e.Current;
                    if (cur.HasValue) valueVal = (value = cur).GetValueOrDefault();
                }
                while (e.MoveNext())
                {
                    double? cur = e.Current;
                    double x = cur.GetValueOrDefault();
                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen. 
                    if (cur.HasValue & x > valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }
            return value;
        }

        public static float Max(this IEnumerable<float> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            float value;
            using (IEnumerator<float> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                value = e.Current;
                while (float.IsNaN(value))
                {
                    if (!e.MoveNext()) return value;
                    value = e.Current;
                }
                while (e.MoveNext())
                {
                    float x = e.Current;
                    if (x > value) value = x;
                }
            }
            return value;
        }

        public static float? Max(this IEnumerable<float?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            float? value = null;
            using (IEnumerator<float?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext()) return value;
                    value = e.Current;
                } while (!value.HasValue);
                float valueVal = value.GetValueOrDefault();
                while (float.IsNaN(valueVal))
                {
                    if (!e.MoveNext()) return value;
                    float? cur = e.Current;
                    if (cur.HasValue) valueVal = (value = cur).GetValueOrDefault();
                }
                while (e.MoveNext())
                {
                    float? cur = e.Current;
                    float x = cur.GetValueOrDefault();
                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen. 
                    if (cur.HasValue & x > valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }
            return value;
        }

        public static decimal Max(this IEnumerable<decimal> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            decimal value;
            using (IEnumerator<decimal> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                value = e.Current;
                while (e.MoveNext())
                {
                    decimal x = e.Current;
                    if (x > value) value = x;
                }
            }
            return value;
        }

        public static decimal? Max(this IEnumerable<decimal?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            decimal? value = null;
            using (IEnumerator<decimal?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext()) return value;
                    value = e.Current;
                } while (!value.HasValue);
                decimal valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    decimal? cur = e.Current;
                    decimal x = cur.GetValueOrDefault();
                    if (cur.HasValue && x > valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }
            return value;
        }

        public static TSource Max<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            Comparer<TSource> comparer = Comparer<TSource>.Default;
            TSource value = default(TSource);
            if (value == null)
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    do
                    {
                        if (!e.MoveNext()) return value;
                        value = e.Current;
                    } while (value == null);
                    while (e.MoveNext())
                    {
                        TSource x = e.Current;
                        if (x != null && comparer.Compare(x, value) > 0) value = x;
                    }
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (!e.MoveNext()) throw Error.NoElements();
                    value = e.Current;
                    while (e.MoveNext())
                    {
                        TSource x = e.Current;
                        if (comparer.Compare(x, value) > 0) value = x;
                    }
                }
            }
            return value;
        }

        public static int Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            return Enumerable.Max(Enumerable.Select(source, selector));
        }

        public static int? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            return Enumerable.Max(Enumerable.Select(source, selector));
        }

        public static long Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            return Enumerable.Max(Enumerable.Select(source, selector));
        }

        public static long? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            return Enumerable.Max(Enumerable.Select(source, selector));
        }

        public static float Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            return Enumerable.Max(Enumerable.Select(source, selector));
        }

        public static float? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            return Enumerable.Max(Enumerable.Select(source, selector));
        }

        public static double Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            return Enumerable.Max(Enumerable.Select(source, selector));
        }

        public static double? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            return Enumerable.Max(Enumerable.Select(source, selector));
        }

        public static decimal Max<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            return Enumerable.Max(Enumerable.Select(source, selector));
        }

        public static decimal? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            return Enumerable.Max(Enumerable.Select(source, selector));
        }

        public static TResult Max<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            return Enumerable.Max(Enumerable.Select(source, selector));
        }

        public static double Average(this IEnumerable<int> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            using (IEnumerator<int> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                long sum = e.Current;
                long count = 1;
                checked
                {
                    while (e.MoveNext())
                    {
                        sum += e.Current;
                        ++count;
                    }
                }
                return (double)sum / count;
            }
        }

        public static double? Average(this IEnumerable<int?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            using (IEnumerator<int?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    int? v = e.Current;
                    if (v.HasValue)
                    {
                        long sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = e.Current;
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }
                        return (double)sum / count;
                    }
                }
            }
            return null;
        }

        public static double Average(this IEnumerable<long> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            using (IEnumerator<long> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                long sum = e.Current;
                long count = 1;
                checked
                {
                    while (e.MoveNext())
                    {
                        sum += e.Current;
                        ++count;
                    }
                }
                return (double)sum / count;
            }
        }

        public static double? Average(this IEnumerable<long?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            using (IEnumerator<long?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    long? v = e.Current;
                    if (v.HasValue)
                    {
                        long sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = e.Current;
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }
                        return (double)sum / count;
                    }
                }
            }
            return null;
        }

        public static float Average(this IEnumerable<float> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            using (IEnumerator<float> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                double sum = e.Current;
                long count = 1;
                while (e.MoveNext())
                {
                    sum += e.Current;
                    ++count;
                }
                return (float)(sum / count);
            }
        }

        public static float? Average(this IEnumerable<float?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            using (IEnumerator<float?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    float? v = e.Current;
                    if (v.HasValue)
                    {
                        double sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = e.Current;
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }
                        return (float)(sum / count);
                    }
                }
            }
            return null;
        }

        public static double Average(this IEnumerable<double> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            using (IEnumerator<double> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                double sum = e.Current;
                long count = 1;
                while (e.MoveNext())
                {
                    // There is an opportunity to short-circuit here, in that if e.Current is
                    // ever NaN then the result will always be NaN. Assuming that this case is
                    // rare enough that not checking is the better approach generally.
                    sum += e.Current;
                    ++count;
                }
                return sum / count;
            }
        }

        public static double? Average(this IEnumerable<double?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            using (IEnumerator<double?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    double? v = e.Current;
                    if (v.HasValue)
                    {
                        double sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = e.Current;
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }
                        return sum / count;
                    }
                }
            }
            return null;
        }

        public static decimal Average(this IEnumerable<decimal> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            using (IEnumerator<decimal> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                decimal sum = e.Current;
                long count = 1;
                while (e.MoveNext())
                {
                    sum += e.Current;
                    ++count;
                }
                return sum / count;
            }
        }

        public static decimal? Average(this IEnumerable<decimal?> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            using (IEnumerator<decimal?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    decimal? v = e.Current;
                    if (v.HasValue)
                    {
                        decimal sum = v.GetValueOrDefault();
                        long count = 1;
                        while (e.MoveNext())
                        {
                            v = e.Current;
                            if (v.HasValue)
                            {
                                sum += v.GetValueOrDefault();
                                ++count;
                            }
                        }
                        return sum / count;
                    }
                }
            }
            return null;
        }

        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            return Enumerable.Average(Enumerable.Select(source, selector));
        }

        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            return Enumerable.Average(Enumerable.Select(source, selector));
        }

        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            return Enumerable.Average(Enumerable.Select(source, selector));
        }

        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            return Enumerable.Average(Enumerable.Select(source, selector));
        }

        public static float Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            return Enumerable.Average(Enumerable.Select(source, selector));
        }

        public static float? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            return Enumerable.Average(Enumerable.Select(source, selector));
        }

        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            return Enumerable.Average(Enumerable.Select(source, selector));
        }

        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            return Enumerable.Average(Enumerable.Select(source, selector));
        }

        public static decimal Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            return Enumerable.Average(Enumerable.Select(source, selector));
        }

        public static decimal? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            return Enumerable.Average(Enumerable.Select(source, selector));
        }
    }

    /// <summary>
    /// An iterator that can (or sometimes can) produce an array through an optimized path.
    /// </summary>
    internal interface IArrayProvider<TElement>
    {
        /// <summary>
        /// Produce an array of the sequence through an optimized path, if possible.
        /// </summary>
        /// <returns>The array, or null if an optimized path isn't possible, and default behaviour should be used.</returns>
        TElement[] ToArray();
    }

    internal class IdentityFunction<TElement>
    {
        public static Func<TElement, TElement> Instance
        {
            get { return x => x; }
        }
    }

    public interface IOrderedEnumerable<TElement> : IEnumerable<TElement>
    {
        IOrderedEnumerable<TElement> CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending);
    }

    public interface IGrouping<out TKey, out TElement> : IEnumerable<TElement>
    {
        TKey Key { get; }
    }

    public interface ILookup<TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>
    {
        int Count { get; }
        IEnumerable<TElement> this[TKey key] { get; }
        bool Contains(TKey key);
    }

    public class Lookup<TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>, ILookup<TKey, TElement>, IArrayProvider<IGrouping<TKey, TElement>>
    {
        private IEqualityComparer<TKey> _comparer;
        private Grouping<TKey, TElement>[] _groupings;
        private Grouping<TKey, TElement> _lastGrouping;
        private int _count;

        internal static Lookup<TKey, TElement> Create<TSource>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (keySelector == null) throw Error.ArgumentNull("keySelector");
            if (elementSelector == null) throw Error.ArgumentNull("elementSelector");
            Lookup<TKey, TElement> lookup = new Lookup<TKey, TElement>(comparer);
            foreach (TSource item in source)
            {
                lookup.GetGrouping(keySelector(item), true).Add(elementSelector(item));
            }
            return lookup;
        }

        internal static Lookup<TKey, TElement> CreateForJoin(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            Lookup<TKey, TElement> lookup = new Lookup<TKey, TElement>(comparer);
            foreach (TElement item in source)
            {
                TKey key = keySelector(item);
                if (key != null) lookup.GetGrouping(key, true).Add(item);
            }
            return lookup;
        }

        private Lookup(IEqualityComparer<TKey> comparer)
        {
            if (comparer == null) comparer = EqualityComparer<TKey>.Default;
            _comparer = comparer;
            _groupings = new Grouping<TKey, TElement>[7];
        }

        public int Count
        {
            get { return _count; }
        }

        public IEnumerable<TElement> this[TKey key]
        {
            get
            {
                Grouping<TKey, TElement> grouping = GetGrouping(key, false);
                if (grouping != null) return grouping;
                return Array.Empty<TElement>();
            }
        }

        public bool Contains(TKey key)
        {
            return GetGrouping(key, false) != null;
        }

        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            Grouping<TKey, TElement> g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g.next;
                    yield return g;
                } while (g != _lastGrouping);
            }
        }
        
        public IGrouping<TKey, TElement>[] ToArray()
        {
            IGrouping<TKey, TElement>[] array = new IGrouping<TKey, TElement>[_count];
            int index = 0;
            Grouping<TKey, TElement> g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g.next;
                    array[index] = g;
                    ++index;
                } while (g != _lastGrouping);
            }
            return array;
        }

        public IEnumerable<TResult> ApplyResultSelector<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            Grouping<TKey, TElement> g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g.next;
                    if (g.count != g.elements.Length) { Array.Resize<TElement>(ref g.elements, g.count); }
                    yield return resultSelector(g.key, g.elements);
                } while (g != _lastGrouping);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal int InternalGetHashCode(TKey key)
        {
            // Handle comparer implementations that throw when passed null
            return (key == null) ? 0 : _comparer.GetHashCode(key) & 0x7FFFFFFF;
        }

        internal Grouping<TKey, TElement> GetGrouping(TKey key, bool create)
        {
            int hashCode = InternalGetHashCode(key);
            for (Grouping<TKey, TElement> g = _groupings[hashCode % _groupings.Length]; g != null; g = g.hashNext)
                if (g.hashCode == hashCode && _comparer.Equals(g.key, key)) return g;
            if (create)
            {
                if (_count == _groupings.Length) Resize();
                int index = hashCode % _groupings.Length;
                Grouping<TKey, TElement> g = new Grouping<TKey, TElement>();
                g.key = key;
                g.hashCode = hashCode;
                g.elements = new TElement[1];
                g.hashNext = _groupings[index];
                _groupings[index] = g;
                if (_lastGrouping == null)
                {
                    g.next = g;
                }
                else
                {
                    g.next = _lastGrouping.next;
                    _lastGrouping.next = g;
                }
                _lastGrouping = g;
                _count++;
                return g;
            }
            return null;
        }

        private void Resize()
        {
            int newSize = checked(_count * 2 + 1);
            Grouping<TKey, TElement>[] newGroupings = new Grouping<TKey, TElement>[newSize];
            Grouping<TKey, TElement> g = _lastGrouping;
            do
            {
                g = g.next;
                int index = g.hashCode % newSize;
                g.hashNext = newGroupings[index];
                newGroupings[index] = g;
            } while (g != _lastGrouping);
            _groupings = newGroupings;
        }
    }

    //
    // It is (unfortunately) common to databind directly to Grouping.Key.
    // Because of this, we have to declare this internal type public so that we
    // can mark the Key property for public reflection.
    //
    // To limit the damage, the toolchain makes this type appear in a hidden assembly.
    // (This is also why it is no longer a nested type of Lookup<,>).
    //
    public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>, IList<TElement>
    {
        internal TKey key;
        internal int hashCode;
        internal TElement[] elements;
        internal int count;
        internal Grouping<TKey, TElement> hashNext;
        internal Grouping<TKey, TElement> next;

        internal Grouping()
        {
        }

        internal void Add(TElement element)
        {
            if (elements.Length == count) Array.Resize(ref elements, checked(count * 2));
            elements[count] = element;
            count++;
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            for (int i = 0; i < count; i++) yield return elements[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // DDB195907: implement IGrouping<>.Key implicitly
        // so that WPF binding works on this property.
        public TKey Key
        {
            get { return key; }
        }

        int ICollection<TElement>.Count
        {
            get { return count; }
        }

        bool ICollection<TElement>.IsReadOnly
        {
            get { return true; }
        }

        void ICollection<TElement>.Add(TElement item)
        {
            throw Error.NotSupported();
        }

        void ICollection<TElement>.Clear()
        {
            throw Error.NotSupported();
        }

        bool ICollection<TElement>.Contains(TElement item)
        {
            return Array.IndexOf(elements, item, 0, count) >= 0;
        }

        void ICollection<TElement>.CopyTo(TElement[] array, int arrayIndex)
        {
            Array.Copy(elements, 0, array, arrayIndex, count);
        }

        bool ICollection<TElement>.Remove(TElement item)
        {
            throw Error.NotSupported();
        }

        int IList<TElement>.IndexOf(TElement item)
        {
            return Array.IndexOf(elements, item, 0, count);
        }

        void IList<TElement>.Insert(int index, TElement item)
        {
            throw Error.NotSupported();
        }

        void IList<TElement>.RemoveAt(int index)
        {
            throw Error.NotSupported();
        }

        TElement IList<TElement>.this[int index]
        {
            get
            {
                if (index < 0 || index >= count) throw Error.ArgumentOutOfRange("index");
                return elements[index];
            }
            set
            {
                throw Error.NotSupported();
            }
        }
    }

    internal class Set<TElement>
    {
        private int[] _buckets;
        private Slot[] _slots;
        private int _count;
        private int _freeList;
        private IEqualityComparer<TElement> _comparer;

        public Set() : this(null) { }

        public Set(IEqualityComparer<TElement> comparer)
        {
            if (comparer == null) comparer = EqualityComparer<TElement>.Default;
            _comparer = comparer;
            _buckets = new int[7];
            _slots = new Slot[7];
            _freeList = -1;
        }

        // If value is not in set, add it and return true; otherwise return false
        public bool Add(TElement value)
        {
            return !Find(value, true);
        }

        // Check whether value is in set
        public bool Contains(TElement value)
        {
            return Find(value, false);
        }

        // If value is in set, remove it and return true; otherwise return false
        public bool Remove(TElement value)
        {
            int hashCode = InternalGetHashCode(value);
            int bucket = hashCode % _buckets.Length;
            int last = -1;
            for (int i = _buckets[bucket] - 1; i >= 0; last = i, i = _slots[i].next)
            {
                if (_slots[i].hashCode == hashCode && _comparer.Equals(_slots[i].value, value))
                {
                    if (last < 0)
                    {
                        _buckets[bucket] = _slots[i].next + 1;
                    }
                    else
                    {
                        _slots[last].next = _slots[i].next;
                    }
                    _slots[i].hashCode = -1;
                    _slots[i].value = default(TElement);
                    _slots[i].next = _freeList;
                    _freeList = i;
                    return true;
                }
            }
            return false;
        }

        private bool Find(TElement value, bool add)
        {
            int hashCode = InternalGetHashCode(value);
            for (int i = _buckets[hashCode % _buckets.Length] - 1; i >= 0; i = _slots[i].next)
            {
                if (_slots[i].hashCode == hashCode && _comparer.Equals(_slots[i].value, value)) return true;
            }
            if (add)
            {
                int index;
                if (_freeList >= 0)
                {
                    index = _freeList;
                    _freeList = _slots[index].next;
                }
                else
                {
                    if (_count == _slots.Length) Resize();
                    index = _count;
                    _count++;
                }
                int bucket = hashCode % _buckets.Length;
                _slots[index].hashCode = hashCode;
                _slots[index].value = value;
                _slots[index].next = _buckets[bucket] - 1;
                _buckets[bucket] = index + 1;
            }
            return false;
        }

        private void Resize()
        {
            int newSize = checked(_count * 2 + 1);
            int[] newBuckets = new int[newSize];
            Slot[] newSlots = new Slot[newSize];
            Array.Copy(_slots, 0, newSlots, 0, _count);
            for (int i = 0; i < _count; i++)
            {
                int bucket = newSlots[i].hashCode % newSize;
                newSlots[i].next = newBuckets[bucket] - 1;
                newBuckets[bucket] = i + 1;
            }
            _buckets = newBuckets;
            _slots = newSlots;
        }

        internal int InternalGetHashCode(TElement value)
        {
            // Handle comparer implementations that throw when passed null
            return (value == null) ? 0 : _comparer.GetHashCode(value) & 0x7FFFFFFF;
        }

        internal struct Slot
        {
            internal int hashCode;
            internal TElement value;
            internal int next;
        }
    }

    internal class GroupedEnumerable<TSource, TKey, TElement, TResult> : IEnumerable<TResult>
    {
        private IEnumerable<TSource> _source;
        private Func<TSource, TKey> _keySelector;
        private Func<TSource, TElement> _elementSelector;
        private IEqualityComparer<TKey> _comparer;
        private Func<TKey, IEnumerable<TElement>, TResult> _resultSelector;

        public GroupedEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (keySelector == null) throw Error.ArgumentNull("keySelector");
            if (elementSelector == null) throw Error.ArgumentNull("elementSelector");
            if (resultSelector == null) throw Error.ArgumentNull("resultSelector");
            _source = source;
            _keySelector = keySelector;
            _elementSelector = elementSelector;
            _comparer = comparer;
            _resultSelector = resultSelector;
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            Lookup<TKey, TElement> lookup = Lookup<TKey, TElement>.Create<TSource>(_source, _keySelector, _elementSelector, _comparer);
            return lookup.ApplyResultSelector(_resultSelector).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class GroupedEnumerable<TSource, TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>, IArrayProvider<IGrouping<TKey, TElement>>
    {
        private IEnumerable<TSource> _source;
        private Func<TSource, TKey> _keySelector;
        private Func<TSource, TElement> _elementSelector;
        private IEqualityComparer<TKey> _comparer;

        public GroupedEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (keySelector == null) throw Error.ArgumentNull("keySelector");
            if (elementSelector == null) throw Error.ArgumentNull("elementSelector");
            _source = source;
            _keySelector = keySelector;
            _elementSelector = elementSelector;
            _comparer = comparer;
        }

        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            return Lookup<TKey, TElement>.Create<TSource>(_source, _keySelector, _elementSelector, _comparer).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public IGrouping<TKey, TElement>[] ToArray()
        {
            return Lookup<TKey, TElement>.Create<TSource>(_source, _keySelector, _elementSelector, _comparer).ToArray();
        }
    }

    internal abstract class OrderedEnumerable<TElement> : IOrderedEnumerable<TElement>, IArrayProvider<TElement>
    {
        internal IEnumerable<TElement> source;
        
        private int[] SortedMap(Buffer<TElement> buffer)
        {
            return GetEnumerableSorter(null).Sort(buffer.items, buffer.count);
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            Buffer<TElement> buffer = new Buffer<TElement>(source);
            if (buffer.count > 0)
            {
                int[] map = SortedMap(buffer);
                for (int i = 0; i < buffer.count; i++) yield return buffer.items[map[i]];
            }
        }
        
        public TElement[] ToArray()
        {
            Buffer<TElement> buffer = new Buffer<TElement>(source);
            int count = buffer.count;
            TElement[] array = new TElement[count];
            if (count > 0)
            {
                int[] map = SortedMap(buffer);
                for (int i = 0; i != array.Length; i++) array[i] = buffer.items[map[i]];
            }

            return array;
        }

        internal abstract EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement> next);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IOrderedEnumerable<TElement> IOrderedEnumerable<TElement>.CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
        {
            OrderedEnumerable<TElement, TKey> result = new OrderedEnumerable<TElement, TKey>(source, keySelector, comparer, descending);
            result.parent = this;
            return result;
        }
    }

    internal class OrderedEnumerable<TElement, TKey> : OrderedEnumerable<TElement>
    {
        internal OrderedEnumerable<TElement> parent;
        internal Func<TElement, TKey> keySelector;
        internal IComparer<TKey> comparer;
        internal bool descending;

        internal OrderedEnumerable(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (keySelector == null) throw Error.ArgumentNull("keySelector");
            this.source = source;
            this.parent = null;
            this.keySelector = keySelector;
            this.comparer = comparer != null ? comparer : Comparer<TKey>.Default;
            this.descending = descending;
        }

        internal override EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement> next)
        {
            EnumerableSorter<TElement> sorter = new EnumerableSorter<TElement, TKey>(keySelector, comparer, descending, next);
            if (parent != null) sorter = parent.GetEnumerableSorter(sorter);
            return sorter;
        }
    }

    internal abstract class EnumerableSorter<TElement>
    {
        internal abstract void ComputeKeys(TElement[] elements, int count);

        internal abstract int CompareKeys(int index1, int index2);

        internal int[] Sort(TElement[] elements, int count)
        {
            ComputeKeys(elements, count);
            int[] map = new int[count];
            for (int i = 0; i < count; i++) map[i] = i;
            QuickSort(map, 0, count - 1);
            return map;
        }

        private void QuickSort(int[] map, int left, int right)
        {
            do
            {
                int i = left;
                int j = right;
                int x = map[i + ((j - i) >> 1)];
                do
                {
                    while (i < map.Length && CompareKeys(x, map[i]) > 0) i++;
                    while (j >= 0 && CompareKeys(x, map[j]) < 0) j--;
                    if (i > j) break;
                    if (i < j)
                    {
                        int temp = map[i];
                        map[i] = map[j];
                        map[j] = temp;
                    }
                    i++;
                    j--;
                } while (i <= j);
                if (j - left <= right - i)
                {
                    if (left < j) QuickSort(map, left, j);
                    left = i;
                }
                else
                {
                    if (i < right) QuickSort(map, i, right);
                    right = j;
                }
            } while (left < right);
        }
    }

    internal class EnumerableSorter<TElement, TKey> : EnumerableSorter<TElement>
    {
        internal Func<TElement, TKey> keySelector;
        internal IComparer<TKey> comparer;
        internal bool descending;
        internal EnumerableSorter<TElement> next;
        internal TKey[] keys;

        internal EnumerableSorter(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending, EnumerableSorter<TElement> next)
        {
            this.keySelector = keySelector;
            this.comparer = comparer;
            this.descending = descending;
            this.next = next;
        }

        internal override void ComputeKeys(TElement[] elements, int count)
        {
            keys = new TKey[count];
            for (int i = 0; i < count; i++) keys[i] = keySelector(elements[i]);
            if (next != null) next.ComputeKeys(elements, count);
        }

        internal override int CompareKeys(int index1, int index2)
        {
            int c = comparer.Compare(keys[index1], keys[index2]);
            if (c == 0)
            {
                if (next == null) return index1 - index2;
                return next.CompareKeys(index1, index2);
            }
            // -c will result in a negative value for int.MinValue (-int.MinValue == int.MinValue).
            // Flipping keys earlier is more likely to trigger something strange in a comparer,
            // particularly as it comes to the sort being stable.
            return (descending != (c > 0)) ? 1 : -1;
        }
    }

    internal struct Buffer<TElement>
    {
        internal TElement[] items;
        internal int count;

        internal Buffer(IEnumerable<TElement> source)
        {
            IArrayProvider<TElement> iterator = source as IArrayProvider<TElement>;
            if (iterator != null)
            {
                TElement[] array = iterator.ToArray();
                if (array != null)
                {
                    items = array;
                    count = array.Length;
                    return;
                }
            }
            
            items = EnumerableHelpers.ToArray(source, out count);
        }

        internal TElement[] ToArray()
        {
            if (count == 0) return new TElement[0]; // consider replacing with Array.Empty<TElement>()
            if (items.Length == count) return items;

            var arr = new TElement[count];
            Array.Copy(items, 0, arr, 0, count);
            return arr;
        }
    }

    /// <summary>
    /// This class provides the items view for the Enumerable
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class SystemCore_EnumerableDebugView<T>
    {
        public SystemCore_EnumerableDebugView(IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException("enumerable");
            }

            _enumerable = enumerable;
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                List<T> tempList = new List<T>();
                IEnumerator<T> currentEnumerator = _enumerable.GetEnumerator();

                if (currentEnumerator != null)
                {
                    for (_count = 0; currentEnumerator.MoveNext(); _count++)
                    {
                        tempList.Add(currentEnumerator.Current);
                    }
                }
                if (_count == 0)
                {
                    throw new SystemCore_EnumerableDebugViewEmptyException();
                }

                _cachedCollection = tempList.ToArray();
                return _cachedCollection;
            }
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private IEnumerable<T> _enumerable;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private T[] _cachedCollection;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private int _count;
    }

    internal sealed class SystemCore_EnumerableDebugViewEmptyException : Exception
    {
        public string Empty
        {
            get
            {
                return Strings.EmptyEnumerable;
            }
        }
    }

    internal sealed class SystemCore_EnumerableDebugView
    {
        public SystemCore_EnumerableDebugView(IEnumerable enumerable)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException("enumerable");
            }

            _enumerable = enumerable;
            _count = 0;
            _cachedCollection = null;
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        public object[] Items
        {
            get
            {
                List<object> tempList = new List<object>();
                IEnumerator currentEnumerator = _enumerable.GetEnumerator();

                if (currentEnumerator != null)
                {
                    for (_count = 0; currentEnumerator.MoveNext(); _count++)
                    {
                        tempList.Add(currentEnumerator.Current);
                    }
                }
                if (_count == 0)
                {
                    throw new SystemCore_EnumerableDebugViewEmptyException();
                }
                _cachedCollection = new object[_count];
                tempList.CopyTo(_cachedCollection, 0);
                return _cachedCollection;
            }
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private IEnumerable _enumerable;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private object[] _cachedCollection;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private int _count;
    }
}
