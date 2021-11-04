// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector)
        {
            if (outer == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(outer));
            }

            if (inner == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(inner));
            }

            if (outerKeySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(outerKeySelector));
            }

            if (innerKeySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(innerKeySelector));
            }

            if (resultSelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(resultSelector));
            }

            return GroupJoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
        }

        public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (outer == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(outer));
            }

            if (inner == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(inner));
            }

            if (outerKeySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(outerKeySelector));
            }

            if (innerKeySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(innerKeySelector));
            }

            if (resultSelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(resultSelector));
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
