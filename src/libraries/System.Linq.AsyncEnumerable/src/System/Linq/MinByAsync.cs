// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Returns the minimum value in a generic sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IComparer{TKey}" /> to compare keys.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The value with the minimum key in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">No key extracted from <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="IComparable{TKey}" /> interface.</exception>
        /// <remarks>
        /// <para>If <typeparamref name="TKey" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
        /// </remarks>
        public static ValueTask<TSource?> MinByAsync<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey>? comparer = null,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);

            return Impl(source, keySelector, comparer ?? Comparer<TKey>.Default, cancellationToken);

            static async ValueTask<TSource?> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                IComparer<TKey> comparer,
                CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        if (default(TSource) is not null)
                        {
                            ThrowHelper.ThrowNoElementsException();
                        }

                        return default;
                    }

                    TSource value = e.Current;
                    TKey key = keySelector(value);

                    if (default(TKey) is null)
                    {
                        if (key is null)
                        {
                            TSource firstValue = value;

                            do
                            {
                                if (!await e.MoveNextAsync().ConfigureAwait(false))
                                {
                                    // All keys are null, surface the first element.
                                    return firstValue;
                                }

                                value = e.Current;
                                key = keySelector(value);
                            }
                            while (key is null);
                        }

                        while (await e.MoveNextAsync().ConfigureAwait(false))
                        {
                            TSource nextValue = e.Current;
                            TKey nextKey = keySelector(nextValue);
                            if (nextKey is not null && comparer.Compare(nextKey, key) < 0)
                            {
                                key = nextKey;
                                value = nextValue;
                            }
                        }
                    }
                    else
                    {
                        if (comparer == Comparer<TKey>.Default)
                        {
                            while (await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                TSource nextValue = e.Current;
                                TKey nextKey = keySelector(nextValue);
                                if (Comparer<TKey>.Default.Compare(nextKey, key) < 0)
                                {
                                    key = nextKey;
                                    value = nextValue;
                                }
                            }
                        }
                        else
                        {
                            while (await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                TSource nextValue = e.Current;
                                TKey nextKey = keySelector(nextValue);
                                if (comparer.Compare(nextKey, key) < 0)
                                {
                                    key = nextKey;
                                    value = nextValue;
                                }
                            }
                        }
                    }

                    return value;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Returns the minimum value in a generic sequence according to a specified key selector function.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TKey">The type of key to compare elements by.</typeparam>
        /// <param name="source">A sequence of values to determine the minimum value of.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <param name="comparer">The <see cref="IComparer{TKey}" /> to compare keys.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The value with the minimum key in the sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">No key extracted from <paramref name="source" /> implements the <see cref="IComparable" /> or <see cref="IComparable{TKey}" /> interface.</exception>
        /// <remarks>
        /// <para>If <typeparamref name="TKey" /> is a reference type and the source sequence is empty or contains only values that are <see langword="null" />, this method returns <see langword="null" />.</para>
        /// </remarks>
        public static ValueTask<TSource?> MinByAsync<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
            IComparer<TKey>? comparer = null,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(keySelector);

            return Impl(source, keySelector, comparer ?? Comparer<TKey>.Default, cancellationToken);

            static async ValueTask<TSource?> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<TKey>> keySelector,
                IComparer<TKey> comparer,
                CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        if (default(TSource) is not null)
                        {
                            ThrowHelper.ThrowNoElementsException();
                        }

                        return default;
                    }

                    TSource value = e.Current;
                    TKey key = await keySelector(value, cancellationToken).ConfigureAwait(false);

                    if (default(TKey) is null)
                    {
                        if (key is null)
                        {
                            TSource firstValue = value;

                            do
                            {
                                if (!await e.MoveNextAsync().ConfigureAwait(false))
                                {
                                    // All keys are null, surface the first element.
                                    return firstValue;
                                }

                                value = e.Current;
                                key = await keySelector(value, cancellationToken).ConfigureAwait(false);
                            }
                            while (key is null);
                        }

                        while (await e.MoveNextAsync().ConfigureAwait(false))
                        {
                            TSource nextValue = e.Current;
                            TKey nextKey = await keySelector(nextValue, cancellationToken).ConfigureAwait(false);
                            if (nextKey is not null && comparer.Compare(nextKey, key) < 0)
                            {
                                key = nextKey;
                                value = nextValue;
                            }
                        }
                    }
                    else
                    {
                        if (comparer == Comparer<TKey>.Default)
                        {
                            while (await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                TSource nextValue = e.Current;
                                TKey nextKey = await keySelector(nextValue, cancellationToken).ConfigureAwait(false);
                                if (Comparer<TKey>.Default.Compare(nextKey, key) < 0)
                                {
                                    key = nextKey;
                                    value = nextValue;
                                }
                            }
                        }
                        else
                        {
                            while (await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                TSource nextValue = e.Current;
                                TKey nextKey = await keySelector(nextValue, cancellationToken).ConfigureAwait(false);
                                if (comparer.Compare(nextKey, key) < 0)
                                {
                                    key = nextKey;
                                    value = nextValue;
                                }
                            }
                        }
                    }

                    return value;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
