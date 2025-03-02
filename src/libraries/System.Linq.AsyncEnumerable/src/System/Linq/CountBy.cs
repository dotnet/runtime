// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Returns the count of elements in the source sequence grouped by key.</summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence that contains elements to be counted.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> to compare keys with.</param>
        /// <returns>An enumerable containing the frequencies of each key occurrence in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey>? keyComparer = null) where TKey : notnull
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);

            return
                source.IsKnownEmpty() ? Empty<KeyValuePair<TKey, int>>() :
                Impl(source, keySelector, keyComparer, default);

            static async IAsyncEnumerable<KeyValuePair<TKey, int>> Impl(
                IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? keyComparer, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> enumerator = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        Dictionary<TKey, int> countsBy = new(keyComparer);
                        do
                        {
                            TSource value = enumerator.Current;
                            TKey key = keySelector(value);

#if NET
                            ref int currentCount = ref CollectionsMarshal.GetValueRefOrAddDefault(countsBy, key, out _);
                            checked { currentCount++; }
#else
                            countsBy[key] = countsBy.TryGetValue(key, out int currentCount) ? checked(currentCount + 1) : 1;
#endif
                        }
                        while (await enumerator.MoveNextAsync().ConfigureAwait(false));

                        foreach (KeyValuePair<TKey, int> countBy in countsBy)
                        {
                            yield return countBy;
                        }
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Returns the count of elements in the source sequence grouped by key.</summary>
        /// <typeparam name="TSource">The type of elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence that contains elements to be counted.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> to compare keys with.</param>
        /// <returns>An enumerable containing the frequencies of each key occurrence in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            IEqualityComparer<TKey>? keyComparer = null) where TKey : notnull
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);

            return
                source.IsKnownEmpty() ? Empty<KeyValuePair<TKey, int>>() :
                Impl(source, keySelector, keyComparer, default);

            static async IAsyncEnumerable<KeyValuePair<TKey, int>> Impl(
                IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask<TKey>> keySelector, IEqualityComparer<TKey>? keyComparer, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> enumerator = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        Dictionary<TKey, int> countsBy = new(keyComparer);
                        do
                        {
                            TSource value = enumerator.Current;
                            TKey key = await keySelector(value, cancellationToken).ConfigureAwait(false);

#if NET
                            ref int currentCount = ref CollectionsMarshal.GetValueRefOrAddDefault(countsBy, key, out _);
                            checked { currentCount++; }
#else
                            countsBy[key] = countsBy.TryGetValue(key, out int currentCount) ? checked(currentCount + 1) : 1;
#endif
                        }
                        while (await enumerator.MoveNextAsync().ConfigureAwait(false));

                        foreach (KeyValuePair<TKey, int> countBy in countsBy)
                        {
                            yield return countBy;
                        }
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
