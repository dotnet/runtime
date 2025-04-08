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
        /// <summary>Returns the first element of a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}"/> to return the first element of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The first element in the specified sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The source sequence is empty (via the returned task).</exception>
        public static ValueTask<TSource> FirstAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source, cancellationToken);

            static async ValueTask<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        ThrowHelper.ThrowNoElementsException();
                    }

                    return e.Current;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Returns the first element in a sequence that satisfies a specified condition.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The first element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The source sequence is empty, or no element in the sequence satisfies
        /// the condition in predicate (via the returned task).
        /// </exception>
        public static ValueTask<TSource> FirstAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false), predicate);

            static async ValueTask<TSource> Impl(
                ConfiguredCancelableAsyncEnumerable<TSource> source,
                Func<TSource, bool> predicate)
            {
                await foreach (TSource item in source)
                {
                    if (predicate(item))
                    {
                        return item;
                    }
                }

                ThrowHelper.ThrowNoElementsException();
                return default!; // unreachable
            }
        }

        /// <summary>Returns the first element in a sequence that satisfies a specified condition.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The first element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The source sequence is empty, or no element in the sequence satisfies
        /// the condition in predicate (via the returned task).
        /// </exception>
        public static ValueTask<TSource> FirstAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return Impl(source, predicate, cancellationToken);

            static async ValueTask<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<bool>> predicate,
                CancellationToken cancellationToken)
            {
                await foreach (TSource item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (await predicate(item, cancellationToken).ConfigureAwait(false))
                    {
                        return item;
                    }
                }

                ThrowHelper.ThrowNoElementsException();
                return default!; // unreachable
            }
        }

        /// <summary>Returns the first element of a sequence, or the default value of <typeparamref name="TSource"/> if the sequence contains no elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}" /> to return the first element of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The default value of <typeparamref name="TSource"/> if <paramref name="source" /> is empty; otherwise, the first element in <paramref name="source" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static ValueTask<TSource?> FirstOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            CancellationToken cancellationToken = default) =>
            FirstOrDefaultAsync(source, default(TSource), cancellationToken)!;

        /// <summary>Returns the first element of a sequence, or a default value if the sequence contains no elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The <see cref="IAsyncEnumerable{T}" /> to return the first element of.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns><paramref name="defaultValue" /> if <paramref name="source" /> is empty; otherwise, the first element in <paramref name="source" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static ValueTask<TSource> FirstOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            TSource defaultValue,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source, defaultValue, cancellationToken);

            static async ValueTask<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                TSource defaultValue,
                CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    return await e.MoveNextAsync().ConfigureAwait(false) ? e.Current : defaultValue;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Returns the first element of the sequence that satisfies a condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>
        /// The default value of <typeparamref name="TSource"/> if source is empty or if no element passes the test specified
        /// by predicate; otherwise, the first element in source that passes the test specified by predicate.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static ValueTask<TSource?> FirstOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken cancellationToken = default) =>
            FirstOrDefaultAsync(source, predicate!, default, cancellationToken);

        /// <summary>Returns the first element of the sequence that satisfies a condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>
        /// The default value of <typeparamref name="TSource"/> if source is empty or if no element passes the test specified
        /// by predicate; otherwise, the first element in source that passes the test specified by predicate.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static ValueTask<TSource?> FirstOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<bool>> predicate,
            CancellationToken cancellationToken = default) =>
            FirstOrDefaultAsync(source, predicate!, default, cancellationToken);

        /// <summary>Returns the first element of the sequence that satisfies a condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns><paramref name="defaultValue" /> if <paramref name="source" /> is empty or if no element passes the test specified by <paramref name="predicate" />; otherwise, the first element in <paramref name="source" /> that passes the test specified by <paramref name="predicate" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static ValueTask<TSource> FirstOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            TSource defaultValue,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false), predicate, defaultValue);

            static async ValueTask<TSource> Impl(
                ConfiguredCancelableAsyncEnumerable<TSource> source,
                Func<TSource, bool> predicate,
                TSource defaultValue)
            {
                await foreach (TSource item in source)
                {
                    if (predicate(item))
                    {
                        return item;
                    }
                }

                return defaultValue;
            }
        }

        /// <summary>Returns the first element of the sequence that satisfies a condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns><paramref name="defaultValue" /> if <paramref name="source" /> is empty or if no element passes the test specified by <paramref name="predicate" />; otherwise, the first element in <paramref name="source" /> that passes the test specified by <paramref name="predicate" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static ValueTask<TSource> FirstOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<bool>> predicate,
            TSource defaultValue,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return Impl(source, predicate, defaultValue, cancellationToken);

            static async ValueTask<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<bool>> predicate,
                TSource defaultValue,
                CancellationToken cancellationToken)
            {
                await foreach (TSource item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (await predicate(item, cancellationToken).ConfigureAwait(false))
                    {
                        return item;
                    }
                }

                return defaultValue;
            }
        }
    }
}
