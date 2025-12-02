// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }

            if (second is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }

            return ExceptIterator(first, second, null);
        }

        public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            if (first is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }

            if (second is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }

            return ExceptIterator(first, second, comparer);
        }

        /// <summary>
        /// Produces the set difference of two sequences according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequence.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An <see cref="IEnumerable{TSource}" /> whose keys that are not also in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An <see cref="IEnumerable{TKey}" /> whose keys that also occur in the first sequence will cause those elements to be removed from the returned sequence.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
        public static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector) => ExceptBy(first, second, keySelector, null);

        /// <summary>
        /// Produces the set difference of two sequences according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequence.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An <see cref="IEnumerable{TSource}" /> whose keys that are not also in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An <see cref="IEnumerable{TKey}" /> whose keys that also occur in the first sequence will cause those elements to be removed from the returned sequence.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{TKey}" /> to compare values.</param>
        /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
        public static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
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

            return ExceptByIterator(first, second, keySelector, comparer);
        }

        private static IEnumerable<TSource> ExceptIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            var set = new HashSet<TSource>(second, comparer);

            foreach (TSource element in first)
            {
                if (set.Add(element))
                {
                    yield return element;
                }
            }
        }

        private static IEnumerable<TSource> ExceptByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            var set = new HashSet<TKey>(second, comparer);

            foreach (TSource element in first)
            {
                if (set.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}
