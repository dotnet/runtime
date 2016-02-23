// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
        {
            if (outer == null)
            {
                throw Error.ArgumentNull(nameof(outer));
            }

            if (inner == null)
            {
                throw Error.ArgumentNull(nameof(inner));
            }

            if (outerKeySelector == null)
            {
                throw Error.ArgumentNull(nameof(outerKeySelector));
            }

            if (innerKeySelector == null)
            {
                throw Error.ArgumentNull(nameof(innerKeySelector));
            }

            if (resultSelector == null)
            {
                throw Error.ArgumentNull(nameof(resultSelector));
            }

            return JoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
        }

        public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            if (outer == null)
            {
                throw Error.ArgumentNull(nameof(outer));
            }

            if (inner == null)
            {
                throw Error.ArgumentNull(nameof(inner));
            }

            if (outerKeySelector == null)
            {
                throw Error.ArgumentNull(nameof(outerKeySelector));
            }

            if (innerKeySelector == null)
            {
                throw Error.ArgumentNull(nameof(innerKeySelector));
            }

            if (resultSelector == null)
            {
                throw Error.ArgumentNull(nameof(resultSelector));
            }

            return JoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
        }

        private static IEnumerable<TResult> JoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            using (IEnumerator<TOuter> e = outer.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    Lookup<TKey, TInner> lookup = Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
                    if (lookup.Count != 0)
                    {
                        do
                        {
                            TOuter item = e.Current;
                            Grouping<TKey, TInner> g = lookup.GetGrouping(outerKeySelector(item), create: false);
                            if (g != null)
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
    }
}
