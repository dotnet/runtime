// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector) =>
            GroupJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer: null);

        public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
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

            return GroupJoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
        }

        private static IEnumerable<TResult> GroupJoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            using (IEnumerator<TOuter> e = outer.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    Lookup<TKey, TInner> lookup = Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
                    do
                    {
                        TOuter item = e.Current;
                        yield return resultSelector(item, lookup[outerKeySelector(item)]);
                    }
                    while (e.MoveNext());
                }
            }
        }
    }
}
