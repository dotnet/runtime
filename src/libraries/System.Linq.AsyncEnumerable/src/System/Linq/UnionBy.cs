// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Produces the set union of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An <see cref="IAsyncEnumerable{T}" /> whose distinct elements form the first set for the union.</param>
        /// <param name="second">An <see cref="IAsyncEnumerable{T}" /> whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}" /> to compare values.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}" /> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> UnionBy<TSource, TKey>(
            this IAsyncEnumerable<TSource> first,
            IAsyncEnumerable<TSource> second,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(first);
            ThrowHelper.ThrowIfNull(second);
            ThrowHelper.ThrowIfNull(keySelector);

            return
                first.IsKnownEmpty() && second.IsKnownEmpty() ? Empty<TSource>() :
                Impl(first, second, keySelector, comparer, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> first,
                IAsyncEnumerable<TSource> second,
                Func<TSource, TKey> keySelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                HashSet<TKey> set = new(comparer);

                await foreach (TSource element in first.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (set.Add(keySelector(element)))
                    {
                        yield return element;
                    }
                }

                await foreach (TSource element in second.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (set.Add(keySelector(element)))
                    {
                        yield return element;
                    }
                }
            }
        }

        /// <summary>Produces the set union of two sequences according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An <see cref="IAsyncEnumerable{T}" /> whose distinct elements form the first set for the union.</param>
        /// <param name="second">An <see cref="IAsyncEnumerable{T}" /> whose distinct elements form the second set for the union.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}" /> to compare values.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}" /> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> UnionBy<TSource, TKey>(
            this IAsyncEnumerable<TSource> first,
            IAsyncEnumerable<TSource> second,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(first);
            ThrowHelper.ThrowIfNull(second);
            ThrowHelper.ThrowIfNull(keySelector);

            return
                first.IsKnownEmpty() && second.IsKnownEmpty() ? Empty<TSource>() :
                Impl(first, second, keySelector, comparer, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> first,
                IAsyncEnumerable<TSource> second,
                Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                HashSet<TKey> set = new(comparer);

                await foreach (TSource element in first.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (set.Add(await keySelector(element, cancellationToken).ConfigureAwait(false)))
                    {
                        yield return element;
                    }
                }

                await foreach (TSource element in second.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (set.Add(await keySelector(element, cancellationToken).ConfigureAwait(false)))
                    {
                        yield return element;
                    }
                }
            }
        }
    }
}
