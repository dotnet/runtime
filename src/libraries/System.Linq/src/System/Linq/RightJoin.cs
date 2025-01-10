// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> RightJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter?, TInner, TResult> resultSelector) =>
            RightJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer: null);

        public static IEnumerable<TResult> RightJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter?, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
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

            if (IsEmptyArray(inner))
            {
                return [];
            }

            return RightJoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
        }

        private static IEnumerable<TResult> RightJoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter?, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            using IEnumerator<TInner> e = inner.GetEnumerator();

            if (e.MoveNext())
            {
                Lookup<TKey, TOuter> outerLookup = Lookup<TKey, TOuter>.CreateForJoin(outer, outerKeySelector, comparer);
                do
                {
                    TInner item = e.Current;
                    Grouping<TKey, TOuter>? g = outerLookup.GetGrouping(innerKeySelector(item), create: false);
                    if (g is null)
                    {
                        yield return resultSelector(default, item);
                    }
                    else
                    {
                        int count = g._count;
                        TOuter[] elements = g._elements;
                        for (int i = 0; i != count; ++i)
                        {
                            yield return resultSelector(elements[i], item);
                        }
                    }
                }
                while (e.MoveNext());
            }
        }
    }
}
