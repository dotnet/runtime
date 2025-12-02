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
            if (first is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }

            if (second is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }

            return IntersectIterator(first, second, comparer);
        }

        /// <summary>Produces the set intersection of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An <see cref="IEnumerable{T}" /> whose distinct elements that also appear in <paramref name="second" /> will be returned.</param>
        /// <param name="second">An <see cref="IEnumerable{T}" /> whose distinct elements that also appear in the first sequence will be returned.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>A sequence that contains the elements that form the set intersection of two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The intersection of two sets A and B is defined as the set that contains all the elements of A that also appear in B, but no other elements.</para>
        /// <para>When the object returned by this method is enumerated, `Intersect` yields distinct elements occurring in both sequences in the order in which they appear in <paramref name="first" />.</para>
        /// <para>The default equality comparer, <see cref="EqualityComparer{T}.Default" />, is used to compare values.</para>
        /// </remarks>
        public static IEnumerable<TSource> IntersectBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector) => IntersectBy(first, second, keySelector, null);

        /// <summary>Produces the set intersection of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An <see cref="IEnumerable{T}" /> whose distinct elements that also appear in <paramref name="second" /> will be returned.</param>
        /// <param name="second">An <see cref="IEnumerable{T}" /> whose distinct elements that also appear in the first sequence will be returned.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{TKey}" /> to compare keys.</param>
        /// <returns>A sequence that contains the elements that form the set intersection of two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>This method is implemented by using deferred execution. The immediate return value is an object that stores all the information that is required to perform the action. The query represented by this method is not executed until the object is enumerated either by calling its `GetEnumerator` method directly or by using `foreach` in Visual C# or `For Each` in Visual Basic.</para>
        /// <para>The intersection of two sets A and B is defined as the set that contains all the elements of A that also appear in B, but no other elements.</para>
        /// <para>When the object returned by this method is enumerated, `Intersect` yields distinct elements occurring in both sequences in the order in which they appear in <paramref name="first" />.</para>
        /// <para>If <paramref name="comparer" /> is <see langword="null" />, the default equality comparer, <see cref="EqualityComparer{T}.Default" />, is used to compare values.</para>
        /// </remarks>
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
