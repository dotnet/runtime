// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second) => Intersect(first, second, null);

        public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            if (first == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }

            if (second == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }

            return IntersectIterator(first, second, comparer);
        }

        public static IEnumerable<TSource> IntersectBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector) => IntersectBy(first, second, keySelector, null);

        public static IEnumerable<TSource> IntersectBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (first is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }
            if (second is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            return IntersectByIterator(first, second, keySelector, comparer);
        }

        private static IEnumerable<TSource> IntersectIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            var set = new HashSet<TSource>(second, comparer);

            foreach (TSource element in first)
            {
                if (set.Remove(element))
                {
                    yield return element;
                }
            }
        }

        private static IEnumerable<TSource> IntersectByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            var set = new HashSet<TKey>(second, comparer);

            foreach (TSource element in first)
            {
                if (set.Remove(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
