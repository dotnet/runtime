// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>
        /// Returns the only element of a sequence, and throws an exception if there is not
        /// exactly one element in the sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The single element of the input sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="source"/> sequence is empty (via the returned task).</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="source"/> sequence contains more than one element. (via the returned task).</exception>
        public static ValueTask<TSource> SingleAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source, cancellationToken);

            static async ValueTask<TSource> Impl(
                IAsyncEnumerable<TSource> source, CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        ThrowHelper.ThrowNoElementsException();
                    }

                    TSource result = e.Current;
                    if (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        ThrowHelper.ThrowMoreThanOneElementException();
                    }

                    return result;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition,
        /// and throws an exception if more than one such element exists.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The single element of the input sequence that satisfies a condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="source"/> sequence is empty (via the returned task).</exception>
        /// <exception cref="InvalidOperationException">No element satisfies the condition in <paramref name="predicate"/> (via the returned task).</exception>
        /// <exception cref="InvalidOperationException">More than one element satisfies the condition in <paramref name="predicate"/> (via the returned task).</exception>
        public static ValueTask<TSource> SingleAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return Impl(source, predicate, cancellationToken);

            static async ValueTask<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, bool> predicate,
                CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    while (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        TSource result = e.Current;
                        if (predicate(result))
                        {
                            while (await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                if (predicate(e.Current))
                                {
                                    ThrowHelper.ThrowMoreThanOneMatchException();
                                }
                            }

                            return result;
                        }
                    }

                    ThrowHelper.ThrowNoElementsException();
                    return default!; // Unreachable
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition,
        /// and throws an exception if more than one such element exists.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The single element of the input sequence that satisfies a condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="source"/> sequence is empty (via the returned task).</exception>
        /// <exception cref="InvalidOperationException">No element satisfies the condition in <paramref name="predicate"/> (via the returned task).</exception>
        /// <exception cref="InvalidOperationException">More than one element satisfies the condition in <paramref name="predicate"/> (via the returned task).</exception>
        public static ValueTask<TSource> SingleAsync<TSource>(
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
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    while (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        TSource result = e.Current;
                        if (await predicate(result, cancellationToken).ConfigureAwait(false))
                        {
                            while (await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                if (await predicate(e.Current, cancellationToken).ConfigureAwait(false))
                                {
                                    ThrowHelper.ThrowMoreThanOneMatchException();
                                }
                            }

                            return result;
                        }
                    }

                    ThrowHelper.ThrowNoElementsException();
                    return default!; // Unreachable
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Returns the only element of a sequence, or a default value if the sequence is
        /// empty; this method throws an exception if there is more than one element in the sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the single element of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>
        /// The single element of the input sequence, or the default value of <typeparamref name="TSource"/>
        /// if the sequence contains no elements.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="source"/> sequence contains more than one element. (via the returned task).</exception>
        public static ValueTask<TSource?> SingleOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            CancellationToken cancellationToken = default) =>
            SingleOrDefaultAsync(source, default(TSource), cancellationToken);

        /// <summary>Returns the only element of a sequence, or a default value if the sequence is empty; this method throws an exception if there is more than one element in the sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}" /> to return the single element of.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The single element of the input sequence, or <paramref name="defaultValue" /> if the sequence contains no elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The input sequence contains more than one element.</exception>
        public static ValueTask<TSource> SingleOrDefaultAsync<TSource>(
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
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        return defaultValue;
                    }

                    TSource result = e.Current;
                    if (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        ThrowHelper.ThrowMoreThanOneElementException();
                    }

                    return result;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition or
        /// a default value if no such element exists; this method throws an exception if
        /// more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}" /> to return the single element of.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>
        /// The single element of the input sequence that satisfies the condition, or the default value of
        /// <typeparamref name="TSource"/> if no such element is found.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The input sequence contains more than one element.</exception>
        public static ValueTask<TSource?> SingleOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken cancellationToken = default) =>
            SingleOrDefaultAsync(source, predicate!, default, cancellationToken);

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition or
        /// a default value if no such element exists; this method throws an exception if
        /// more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}" /> to return the single element of.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>
        /// The single element of the input sequence that satisfies the condition, or the default value of
        /// <typeparamref name="TSource"/> if no such element is found.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">The input sequence contains more than one element.</exception>
        public static ValueTask<TSource?> SingleOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<bool>> predicate,
            CancellationToken cancellationToken = default) =>
            SingleOrDefaultAsync(source, predicate!, default, cancellationToken);

        /// <summary>Returns the only element of a sequence that satisfies a specified condition or a default value if no such element exists; this method throws an exception if more than one element satisfies the condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}" /> to return a single element from.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The single element of the input sequence that satisfies the condition, or <paramref name="defaultValue" /> if no such element is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">More than one element satisfies the condition in <paramref name="predicate" />.</exception>
        public static ValueTask<TSource> SingleOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            TSource defaultValue,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return Impl(source, predicate, defaultValue, cancellationToken);

            static async ValueTask<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, bool> predicate,
                TSource defaultValue,
                CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    while (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        TSource result = e.Current;
                        if (predicate(result))
                        {
                            while (await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                if (predicate(e.Current))
                                {
                                    ThrowHelper.ThrowMoreThanOneMatchException();
                                }
                            }

                            return result;
                        }
                    }

                    return defaultValue;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Returns the only element of a sequence that satisfies a specified condition or a default value if no such element exists; this method throws an exception if more than one element satisfies the condition.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}" /> to return a single element from.</param>
        /// <param name="predicate">A function to test an element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The single element of the input sequence that satisfies the condition, or <paramref name="defaultValue" /> if no such element is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">More than one element satisfies the condition in <paramref name="predicate" />.</exception>
        public static ValueTask<TSource> SingleOrDefaultAsync<TSource>(
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
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    while (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        TSource result = e.Current;
                        if (await predicate(result, cancellationToken).ConfigureAwait(false))
                        {
                            while (await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                if (await predicate(e.Current, cancellationToken).ConfigureAwait(false))
                                {
                                    ThrowHelper.ThrowMoreThanOneMatchException();
                                }
                            }

                            return result;
                        }
                    }

                    return defaultValue;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
