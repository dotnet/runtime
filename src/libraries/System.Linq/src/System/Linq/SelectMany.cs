// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            return new SelectManySingleSelectorIterator<TSource, TResult>(source, selector);
        }

        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            return SelectManyIterator(source, selector);
        }

        private static IEnumerable<TResult> SelectManyIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
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
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (collectionSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collectionSelector);
            }

            if (resultSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            return SelectManyIterator(source, collectionSelector, resultSelector);
        }

        private static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
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
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (collectionSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collectionSelector);
            }

            if (resultSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            return SelectManyIterator(source, collectionSelector, resultSelector);
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

        private sealed partial class SelectManySingleSelectorIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, IEnumerable<TResult>> _selector;
            private IEnumerator<TSource>? _sourceEnumerator;
            private IEnumerator<TResult>? _subEnumerator;

            internal SelectManySingleSelectorIterator(IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
            {
                Debug.Assert(source is not null);
                Debug.Assert(selector is not null);

                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone()
            {
                return new SelectManySingleSelectorIterator<TSource, TResult>(_source, _selector);
            }

            public override void Dispose()
            {
                if (_subEnumerator is not null)
                {
                    _subEnumerator.Dispose();
                    _subEnumerator = null;
                }

                if (_sourceEnumerator is not null)
                {
                    _sourceEnumerator.Dispose();
                    _sourceEnumerator = null;
                }

                base.Dispose();
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
                        Debug.Assert(_sourceEnumerator is not null);
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
                        Debug.Assert(_subEnumerator is not null);
                        if (!_subEnumerator.MoveNext())
                        {
                            _subEnumerator.Dispose();
                            _subEnumerator = null;
                            _state = 2;
                            goto case 2;
                        }

                        _current = _subEnumerator.Current;
                        return true;
                }

                Dispose();
                return false;
            }
        }
    }
}
