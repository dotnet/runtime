// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner?, TResult> resultSelector) =>
            LeftJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer: null);

        public static IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner?, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (outer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outer);
            }

            if (inner is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inner);
            }

            if (outerKeySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outerKeySelector);
            }

            if (innerKeySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.innerKeySelector);
            }

            if (resultSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
            }

            if (IsEmptyArray(outer))
            {
                return [];
            }

            return LeftJoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
        }

        private static IEnumerable<TResult> LeftJoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner?, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            using IEnumerator<TOuter> e = outer.GetEnumerator();

            if (e.MoveNext())
            {
                Lookup<TKey, TInner> innerLookup = Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
                do
                {
                    TOuter item = e.Current;
                    Grouping<TKey, TInner>? g = innerLookup.GetGrouping(outerKeySelector(item), create: false);
                    if (g is null)
                    {
                        yield return resultSelector(item, default);
                    }
                    else
                    {
                        int count = g._count;
                        TInner[] elements = g._elements;
                        for (int i = 0; i != count; ++i)
                        {
                            yield return resultSelector(item, elements[i]);
                        }
                    }
                }
                while (e.MoveNext());
            }
        }
    }
}
