// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            return SelectManyIterator1(source, selector);
        }

        private static IEnumerable<TResult> SelectManyIterator1<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
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
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            return SelectManyIterator2(source, selector);
        }

        private static IEnumerable<TResult> SelectManyIterator2<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked
                {
                    index++;
                }

                foreach (TResult subElement in selector(element, index))
                {
                    yield return subElement;
                }
            }
        }

        public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (collectionSelector == null)
            {
                throw Error.ArgumentNull(nameof(collectionSelector));
            }

            if (resultSelector == null)
            {
                throw Error.ArgumentNull(nameof(resultSelector));
            }

            return SelectManyIterator3(source, collectionSelector, resultSelector);
        }

        private static IEnumerable<TResult> SelectManyIterator3<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked
                {
                    index++;
                }

                foreach (TCollection subElement in collectionSelector(element, index))
                {
                    yield return resultSelector(element, subElement);
                }
            }
        }

        public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (collectionSelector == null)
            {
                throw Error.ArgumentNull(nameof(collectionSelector));
            }

            if (resultSelector == null)
            {
                throw Error.ArgumentNull(nameof(resultSelector));
            }

            return SelectManyIterator4(source, collectionSelector, resultSelector);
        }

        private static IEnumerable<TResult> SelectManyIterator4<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            foreach (TSource element in source)
            {
                foreach (TCollection subElement in collectionSelector(element))
                {
                    yield return resultSelector(element, subElement);
                }
            }
        }

        private sealed class SelectManyIterator<TSource, TResult> : Iterator<TResult>, IIListProvider<TResult>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, IEnumerable<TResult>> _selector;
            private IEnumerator<TSource> _sourceEnumerator;
            private IEnumerator<TResult> _subEnumerator;

            internal SelectManyIterator(IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);

                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone()
            {
                return new SelectManyIterator<TSource, TResult>(_source, _selector);
            }

            public override void Dispose()
            {
                if (_sourceEnumerator != null)
                {
                    _sourceEnumerator.Dispose();
                    _sourceEnumerator = null;
                }

                base.Dispose();
            }

            public int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap)
                {
                    return -1;
                }

                int count = 0;

                foreach (TSource element in _source)
                {
                    checked
                    {
                        count += _selector(element).Count();
                    }
                }

                return count;
            }

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        // Retrieve the source enumerator.
                        _sourceEnumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        // Take the next element from the source enumerator.
                        if (!_sourceEnumerator.MoveNext())
                        {
                            break;
                        }

                        TSource element = _sourceEnumerator.Current;

                        // Project it into a sub-collection and get its enumerator.
                        _subEnumerator = _selector(element).GetEnumerator();
                        _state = 3;
                        goto case 3;
                    case 3:
                        // Take the next element from the sub-collection and yield.
                        if (!_subEnumerator.MoveNext())
                        {
                            _subEnumerator.Dispose();
                            _state = 2;
                            goto case 2;
                        }

                        _current = _subEnumerator.Current;
                        return true;
                }

                Dispose();
                return false;
            }

            public TResult[] ToArray()
            {
                var builder = new LargeArrayBuilder<TResult>(initialize: true);

                foreach (TSource element in _source)
                {
                    builder.AddRange(_selector(element));
                }

                return builder.ToArray();
            }

            public List<TResult> ToList()
            {
                var list = new List<TResult>();

                foreach (TSource element in _source)
                {
                    list.AddRange(_selector(element));
                }

                return list;
            }
        }
    }
}
