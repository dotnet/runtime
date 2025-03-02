// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
#if NET
using System.Runtime.InteropServices;
#endif
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Applies an accumulator function over a sequence, grouping results by key.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> to compare keys with.</param>
        /// <returns>An enumerable containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <remarks>
        /// This method is comparable to the GroupBy methods where each grouping is being aggregated into a single value
        /// as opposed to allocating a collection for each group.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keyComparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func,
            IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);
            ThrowHelper.ThrowIfNull(func);

            return
                source.IsKnownEmpty() ? Empty<KeyValuePair<TKey, TAccumulate>>() :
                Impl(source, keySelector, seed, func, keyComparer, default);

            static async IAsyncEnumerable<KeyValuePair<TKey, TAccumulate>> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                TAccumulate seed,
                Func<TAccumulate, TSource, TAccumulate> func,
                IEqualityComparer<TKey>? keyComparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> enumerator = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield break;
                    }

                    Dictionary<TKey, TAccumulate> dict = new(keyComparer);

                    do
                    {
                        TSource value = enumerator.Current;
                        TKey key = keySelector(value);

#if NET
                        ref TAccumulate? acc = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out bool exists);
                        acc = func(exists ? acc! : seed, value);
#else
                        dict[key] = func(dict.TryGetValue(key, out TAccumulate? acc) ? acc : seed, value);
#endif
                    }
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false));

                    foreach (KeyValuePair<TKey, TAccumulate> countBy in dict)
                    {
                        yield return countBy;
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Applies an accumulator function over a sequence, grouping results by key.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> to compare keys with.</param>
        /// <returns>An enumerable containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <remarks>
        /// This method is comparable to the GroupBy methods where each grouping is being aggregated into a single value
        /// as opposed to allocating a collection for each group.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keyComparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            TAccumulate seed,
            Func<TAccumulate, TSource, CancellationToken, ValueTask<TAccumulate>> func,
            IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);
            ThrowHelper.ThrowIfNull(func);

            return
                source.IsKnownEmpty() ? Empty<KeyValuePair<TKey, TAccumulate>>() :
                Impl(source, keySelector, seed, func, keyComparer, default);

            static async IAsyncEnumerable<KeyValuePair<TKey, TAccumulate>> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
                TAccumulate seed,
                Func<TAccumulate, TSource, CancellationToken, ValueTask<TAccumulate>> func,
                IEqualityComparer<TKey>? keyComparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> enumerator = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield break;
                    }

                    Dictionary<TKey, TAccumulate> dict = new(keyComparer);

                    do
                    {
                        TSource value = enumerator.Current;
                        TKey key = await keySelector(value, cancellationToken).ConfigureAwait(false);

                        dict[key] = await func(dict.TryGetValue(key, out TAccumulate? acc) ? acc : seed, value, cancellationToken).ConfigureAwait(false);
                    }
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false));

                    foreach (KeyValuePair<TKey, TAccumulate> countBy in dict)
                    {
                        yield return countBy;
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Applies an accumulator function over a sequence, grouping results by key.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="seedSelector">A factory for the initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> to compare keys with.</param>
        /// <returns>An enumerable containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <remarks>
        /// This method is comparable to the GroupBy methods where each grouping is being aggregated into a single value
        /// as opposed to allocating a collection for each group.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keyComparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="seedSelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TKey, TAccumulate> seedSelector,
            Func<TAccumulate, TSource, TAccumulate> func,
            IEqualityComparer<TKey>? keyComparer = null) where TKey : notnull
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);
            ThrowHelper.ThrowIfNull(seedSelector);
            ThrowHelper.ThrowIfNull(func);

            return
                source.IsKnownEmpty() ? Empty<KeyValuePair<TKey, TAccumulate>>() :
                Impl(source, keySelector, seedSelector, func, keyComparer, default);

            static async IAsyncEnumerable<KeyValuePair<TKey, TAccumulate>> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                Func<TKey, TAccumulate> seedSelector,
                Func<TAccumulate, TSource, TAccumulate> func,
                IEqualityComparer<TKey>? keyComparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> enumerator = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield break;
                    }

                    Dictionary<TKey, TAccumulate> dict = new(keyComparer);

                    do
                    {
                        TSource value = enumerator.Current;
                        TKey key = keySelector(value);

#if NET
                        ref TAccumulate? acc = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out bool exists);
                        acc = func(exists ? acc! : seedSelector(key), value);
#else
                        dict[key] = func(dict.TryGetValue(key, out TAccumulate? acc) ? acc : seedSelector(key), value);
#endif
                    }
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false));

                    foreach (KeyValuePair<TKey, TAccumulate> countBy in dict)
                    {
                        yield return countBy;
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Applies an accumulator function over a sequence, grouping results by key.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="seedSelector">A factory for the initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> to compare keys with.</param>
        /// <returns>An enumerable containing the aggregates corresponding to each key deriving from <paramref name="source"/>.</returns>
        /// <remarks>
        /// This method is comparable to the GroupBy methods where each grouping is being aggregated into a single value
        /// as opposed to allocating a collection for each group.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keyComparer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="seedSelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            Func<TKey, CancellationToken, ValueTask<TAccumulate>> seedSelector,
            Func<TAccumulate, TSource, CancellationToken, ValueTask<TAccumulate>> func,
            IEqualityComparer<TKey>? keyComparer = null) where TKey : notnull
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);
            ThrowHelper.ThrowIfNull(seedSelector);
            ThrowHelper.ThrowIfNull(func);

            return
                source.IsKnownEmpty() ? Empty<KeyValuePair<TKey, TAccumulate>>() :
                Impl(source, keySelector, seedSelector, func, keyComparer, default);

            static async IAsyncEnumerable<KeyValuePair<TKey, TAccumulate>> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
                Func<TKey, CancellationToken, ValueTask<TAccumulate>> seedSelector,
                Func<TAccumulate, TSource, CancellationToken, ValueTask<TAccumulate>> func,
                IEqualityComparer<TKey>? keyComparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> enumerator = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        Dictionary<TKey, TAccumulate> dict = new(keyComparer);

                        do
                        {
                            TSource value = enumerator.Current;
                            TKey key = await keySelector(value, cancellationToken).ConfigureAwait(false);

                            dict[key] = await func(
                                dict.TryGetValue(key, out TAccumulate? acc) ? acc : await seedSelector(key, cancellationToken).ConfigureAwait(false),
                                value,
                                cancellationToken).ConfigureAwait(false);
                        }
                        while (await enumerator.MoveNextAsync().ConfigureAwait(false));

                        foreach (KeyValuePair<TKey, TAccumulate> countBy in dict)
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
