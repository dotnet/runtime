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
        /// Creates a <see cref="Dictionary{TKey,TValue}"/> from an <see cref="IAsyncEnumerable{T}"/> according to specified key comparer.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys from elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values from elements of <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to create a <see cref="Dictionary{TKey,TValue}"/> from.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{TKey}"/> to compare keys.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Dictionary{TKey,TValue}"/> that contains keys and values from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> contains one or more duplicate keys (via the returned task).</exception>
        public static ValueTask<Dictionary<TKey, TValue>> ToDictionaryAsync<TKey, TValue>(
            this IAsyncEnumerable<KeyValuePair<TKey, TValue>> source,
            IEqualityComparer<TKey>? comparer = null,
            CancellationToken cancellationToken = default) where TKey : notnull
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false), comparer);

            static async ValueTask<Dictionary<TKey, TValue>> Impl(
                ConfiguredCancelableAsyncEnumerable<KeyValuePair<TKey, TValue>> source,
                IEqualityComparer<TKey>? comparer)
            {
                Dictionary<TKey, TValue> d = new Dictionary<TKey, TValue>(comparer);
                await foreach (KeyValuePair<TKey, TValue> element in source)
                {
                    d.Add(element.Key, element.Value);
                }

                return d;
            }
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey,TValue}"/> from an <see cref="IAsyncEnumerable{T}"/> according to specified key comparer.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys from elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values from elements of <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to create a <see cref="Dictionary{TKey,TValue}"/> from.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{TKey}"/> to compare keys.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Dictionary{TKey,TValue}"/> that contains keys and values from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> contains one or more duplicate keys (via the returned task).</exception>
        public static ValueTask<Dictionary<TKey, TValue>> ToDictionaryAsync<TKey, TValue>(
            this IAsyncEnumerable<(TKey Key, TValue Value)> source, IEqualityComparer<TKey>? comparer = null, CancellationToken cancellationToken = default) where TKey : notnull =>
            source.ToDictionaryAsync(vt => vt.Key, vt => vt.Value, comparer, cancellationToken);

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an <see cref="IAsyncEnumerable{T}"/>
        /// according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to create a <see cref="Dictionary{TKey, TValue}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> that contains keys and values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> contains one or more duplicate keys (via the returned task).</exception>
        public static ValueTask<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey>? comparer = null,
            CancellationToken cancellationToken = default) where TKey : notnull
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false), keySelector, comparer);

            static async ValueTask<Dictionary<TKey, TSource>> Impl(
                ConfiguredCancelableAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                IEqualityComparer<TKey>? comparer)
            {
                Dictionary<TKey, TSource> d = new(comparer);
                await foreach (TSource element in source)
                {
                    d.Add(keySelector(element), element);
                }
                return d;
            }
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an <see cref="IAsyncEnumerable{T}"/>
        /// according to a specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the keys returned by <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to create a <see cref="Dictionary{TKey, TValue}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> that contains keys and values.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> contains one or more duplicate keys (via the returned task).</exception>
        public static ValueTask<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            IEqualityComparer<TKey>? comparer = null,
            CancellationToken cancellationToken = default) where TKey : notnull
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);

            return Impl(source, keySelector, comparer, cancellationToken);

            static async ValueTask<Dictionary<TKey, TSource>> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
                IEqualityComparer<TKey>? comparer,
                CancellationToken cancellationToken)
            {
                Dictionary<TKey, TSource> d = new(comparer);
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    d.Add(await keySelector(element, cancellationToken).ConfigureAwait(false), element);
                }
                return d;
            }
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an <see cref="IAsyncEnumerable{T}"/>"/>
        /// according to specified key selector and element selector functions.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to create a <see cref="Dictionary{TKey, TValue}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TElement"/> selected from the input sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="elementSelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> contains one or more duplicate keys (via the returned task).</exception>
        public static ValueTask<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey>? comparer = null,
            CancellationToken cancellationToken = default) where TKey : notnull
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);
            ThrowHelper.ThrowIfNull(elementSelector);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false), keySelector, elementSelector, comparer);

            static async ValueTask<Dictionary<TKey, TElement>> Impl(
                ConfiguredCancelableAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                Func<TSource, TElement> elementSelector,
                IEqualityComparer<TKey>? comparer)
            {
                Dictionary<TKey, TElement> d = new(comparer);
                await foreach (TSource element in source)
                {
                    d.Add(keySelector(element), elementSelector(element));
                }

                return d;
            }
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from an <see cref="IAsyncEnumerable{T}"/>"/>
        /// according to specified key selector and element selector functions.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to create a <see cref="Dictionary{TKey, TValue}"/> from.</param>
        /// <param name="keySelector">A function to extract a key from each element.</param>
        /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/> that contains values of type <typeparamref name="TElement"/> selected from the input sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="elementSelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> contains one or more duplicate keys (via the returned task).</exception>
        public static ValueTask<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            Func<TSource, CancellationToken, ValueTask<TElement>> elementSelector,
            IEqualityComparer<TKey>? comparer = null,
            CancellationToken cancellationToken = default) where TKey : notnull
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);
            ThrowHelper.ThrowIfNull(elementSelector);

            return Impl(source, keySelector, elementSelector, comparer, cancellationToken);

            static async ValueTask<Dictionary<TKey, TElement>> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
                Func<TSource, CancellationToken, ValueTask<TElement>> elementSelector,
                IEqualityComparer<TKey>? comparer,
                CancellationToken cancellationToken)
            {
                Dictionary<TKey, TElement> d = new(comparer);
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    d.Add(
                        await keySelector(element, cancellationToken).ConfigureAwait(false),
                        await elementSelector(element, cancellationToken).ConfigureAwait(false));
                }

                return d;
            }
        }
    }
}
