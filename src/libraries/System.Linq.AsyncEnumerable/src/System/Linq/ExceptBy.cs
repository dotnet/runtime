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
        /// <summary>
        /// Produces the set difference of two sequences according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequence.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An <see cref="IAsyncEnumerable{TSource}" /> whose keys that are not also in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An <see cref="IAsyncEnumerable{TKey}" /> whose keys that also occur in the first sequence will cause those elements to be removed from the returned sequence.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{TKey}" /> to compare values.</param>
        /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TSource> ExceptBy<TSource, TKey>(
            this IAsyncEnumerable<TSource> first,
            IAsyncEnumerable<TKey> second,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(first);
            ThrowHelper.ThrowIfNull(second);
            ThrowHelper.ThrowIfNull(keySelector);

            return Impl(first, second, keySelector, comparer, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> first,
                IAsyncEnumerable<TKey> second,
                Func<TSource, TKey> keySelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                HashSet<TKey> set = new(comparer);

                await foreach (TKey key in second.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    set.Add(key);
                }

                await foreach (TSource element in first.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (set.Add(keySelector(element)))
                    {
                        yield return element;
                    }
                }
            }
        }

        /// <summary>
        /// Produces the set difference of two sequences according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequence.</typeparam>
        /// <typeparam name="TKey">The type of key to identify elements by.</typeparam>
        /// <param name="first">An <see cref="IAsyncEnumerable{TSource}" /> whose keys that are not also in <paramref name="second"/> will be returned.</param>
        /// <param name="second">An <see cref="IAsyncEnumerable{TKey}" /> whose keys that also occur in the first sequence will cause those elements to be removed from the returned sequence.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{TKey}" /> to compare values.</param>
        /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TSource> ExceptBy<TSource, TKey>(
            this IAsyncEnumerable<TSource> first,
            IAsyncEnumerable<TKey> second,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(first);
            ThrowHelper.ThrowIfNull(second);
            ThrowHelper.ThrowIfNull(keySelector);

            return Impl(first, second, keySelector, comparer, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> first,
                IAsyncEnumerable<TKey> second,
                Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
                IEqualityComparer<TKey>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                HashSet<TKey> set = new(comparer);

                await foreach (TKey key in second.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    set.Add(key);
                }

                await foreach (TSource element in first.WithCancellation(cancellationToken).ConfigureAwait(false))
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
