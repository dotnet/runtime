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
        /// <summary>Returns the number of elements in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence that contains elements to be counted.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The number of elements in the input sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="OverflowException">The number of elements in source is larger than <see cref="int.MaxValue"/> (via the returned task).</exception>
        public static ValueTask<int> CountAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source, cancellationToken);

            static async ValueTask<int> Impl(
                IAsyncEnumerable<TSource> source,
                CancellationToken cancellationToken = default)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    int count = 0;
                    while (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        checked { count++; }
                    }

                    return count;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Returns the number of elements in a sequence satisfy a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence that contains elements to be tested and counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The number of elements in the input sequence that satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="OverflowException">The number of elements that satisfy the condition is larger than <see cref="int.MaxValue"/> (via the returned task).</exception>
        public static ValueTask<int> CountAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false), predicate);

            static async ValueTask<int> Impl(
                ConfiguredCancelableAsyncEnumerable<TSource> source,
                Func<TSource, bool> predicate)
            {
                int count = 0;
                await foreach (TSource element in source)
                {
                    if (predicate(element))
                    {
                        checked { count++; }
                    }
                }

                return count;
            }
        }

        /// <summary>Returns the number of elements in a sequence satisfy a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence that contains elements to be tested and counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The number of elements in the input sequence that satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="OverflowException">The number of elements that satisfy the condition is larger than <see cref="int.MaxValue"/> (via the returned task).</exception>
        public static ValueTask<int> CountAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return Impl(source, predicate, cancellationToken);

            static async ValueTask<int> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<bool>> predicate,
                CancellationToken cancellationToken = default)
            {
                int count = 0;
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (await predicate(element, cancellationToken).ConfigureAwait(false))
                    {
                        checked { count++; }
                    }
                }

                return count;
            }
        }

        /// <summary>Returns the number of elements in a sequence satisfy a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence that contains elements to be tested and counted.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The number of elements in the input sequence that satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ValueTask<long> LongCountAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source, cancellationToken);

            static async ValueTask<long> Impl(
                IAsyncEnumerable<TSource> source,
                CancellationToken cancellationToken = default)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    long count = 0;
                    while (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        count++;
                    }

                    return count;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Returns the number of elements in a sequence satisfy a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence that contains elements to be tested and counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The number of elements in the input sequence that satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ValueTask<long> LongCountAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false), predicate);

            static async ValueTask<long> Impl(
                ConfiguredCancelableAsyncEnumerable<TSource> source,
                Func<TSource, bool> predicate)
            {
                long count = 0;
                await foreach (TSource element in source)
                {
                    if (predicate(element))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Returns the number of elements in a sequence satisfy a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence that contains elements to be tested and counted.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The number of elements in the input sequence that satisfy the condition in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ValueTask<long> LongCountAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return Impl(source, predicate, cancellationToken);

            static async ValueTask<long> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<bool>> predicate,
                CancellationToken cancellationToken = default)
            {
                long count = 0;
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (await predicate(element, cancellationToken).ConfigureAwait(false))
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
