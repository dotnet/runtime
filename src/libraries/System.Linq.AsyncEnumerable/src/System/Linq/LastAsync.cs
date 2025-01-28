// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Returns the last element of a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return the last element of.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The value at the last position in the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The source sequence is empty (via the returned task).</exception>
        public static ValueTask<TSource> LastAsync<TSource>(
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

                    TSource result;
                    do
                    {
                        result = e.Current;
                    }
                    while (await e.MoveNextAsync().ConfigureAwait(false));

                    return result;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Returns the last element of a sequence that satisfies a specified condition.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The last element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The source sequence is empty, or no element in the sequence satisfies
        /// the condition in predicate (via the returned task).
        /// </exception>
        public static ValueTask<TSource> LastAsync<TSource>(
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
                        TSource element = e.Current;
                        if (predicate(element))
                        {
                            TSource result = element;

                            while (await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                element = e.Current;
                                if (predicate(element))
                                {
                                    result = element;
                                }
                            }

                            return result;
                        }
                    }

                    ThrowHelper.ThrowNoMatchException();
                    return default!;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Returns the last element of a sequence that satisfies a specified condition.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The last element in the sequence that passes the test in the specified predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The source sequence is empty, or no element in the sequence satisfies
        /// the condition in predicate (via the returned task).
        /// </exception>
        public static ValueTask<TSource> LastAsync<TSource>(
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
                        TSource element = e.Current;
                        if (await predicate(element, cancellationToken).ConfigureAwait(false))
                        {
                            TSource result = element;

                            while (await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                element = e.Current;
                                if (await predicate(element, cancellationToken).ConfigureAwait(false))
                                {
                                    result = element;
                                }
                            }

                            return result;
                        }
                    }

                    ThrowHelper.ThrowNoMatchException();
                    return default!;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Returns the last element of a sequence, or a default value if the sequence contains no elements.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>
        /// The default value of <typeparamref name="TSource"/> if the source sequence is empty;
        /// otherwise, the last element in the <see cref="IAsyncEnumerable{T}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ValueTask<TSource?> LastOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            CancellationToken cancellationToken = default) =>
            LastOrDefaultAsync(source, default(TSource), cancellationToken);

        /// <summary>Returns the last element of a sequence, or a default value if the sequence contains no elements.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}" /> to return the last element of.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns><paramref name="defaultValue" /> if the source sequence is empty; otherwise, the last element in the <see cref="IAsyncEnumerable{T}" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static ValueTask<TSource> LastOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            TSource defaultValue,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source, defaultValue, cancellationToken);

            static async ValueTask<TSource> Impl(
                IAsyncEnumerable<TSource> source, TSource defaultValue, CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    TSource result = defaultValue;
                    if (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        do
                        {
                            result = e.Current;
                        }
                        while (await e.MoveNextAsync().ConfigureAwait(false));
                    }

                    return result;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Returns the last element of a sequence that satisfies a condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The default value of <typeparamref name="TSource"/> if the sequence is empty or if no elements pass the test in the predicate function; otherwise, the last element that passes the test in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static ValueTask<TSource?> LastOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken cancellationToken = default) =>
            LastOrDefaultAsync(source, predicate!, default, cancellationToken);

        /// <summary>Returns the last element of a sequence that satisfies a condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The default value of <typeparamref name="TSource"/> if the sequence is empty or if no elements pass the test in the predicate function; otherwise, the last element that passes the test in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static ValueTask<TSource?> LastOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<bool>> predicate,
            CancellationToken cancellationToken = default) =>
            LastOrDefaultAsync(source, predicate!, default, cancellationToken);

        /// <summary>Returns the last element of a sequence that satisfies a condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns><paramref name="defaultValue" /> if the sequence is empty or if no elements pass the test in the predicate function; otherwise, the last element that passes the test in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static ValueTask<TSource> LastOrDefaultAsync<TSource>(
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
                    TSource result = defaultValue;
                    while (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        TSource element = e.Current;
                        if (predicate(element))
                        {
                            result = element;

                            while (await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                element = e.Current;
                                if (predicate(element))
                                {
                                    result = element;
                                }
                            }

                            break;
                        }
                    }

                    return result;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Returns the last element of a sequence that satisfies a condition or a default value if no such element is found.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}" /> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="defaultValue">The default value to return if the sequence is empty.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns><paramref name="defaultValue" /> if the sequence is empty or if no elements pass the test in the predicate function; otherwise, the last element that passes the test in the predicate function.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static ValueTask<TSource> LastOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<bool>> predicate,
            TSource defaultValue,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return Impl(source, predicate, defaultValue, cancellationToken);

            static async ValueTask<TSource> Impl(
                IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask<bool>> predicate, TSource defaultValue, CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    TSource result = defaultValue;
                    while (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        TSource element = e.Current;
                        if (await predicate(element, cancellationToken).ConfigureAwait(false))
                        {
                            result = element;

                            while (await e.MoveNextAsync().ConfigureAwait(false))
                            {
                                element = e.Current;
                                if (await predicate(element, cancellationToken).ConfigureAwait(false))
                                {
                                    result = element;
                                }
                            }

                            break;
                        }
                    }

                    return result;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
