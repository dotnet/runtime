// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Returns the element at a specified index in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="index">The index of the element to retrieve, which is either from the beginning or the end of the sequence.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The element at the specified position in the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the bounds of the source sequence (via the returned task).</exception>
        public static ValueTask<TSource> ElementAtAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            int index,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return ElementAtOrDefaultAsync(source, index, throwIfNotFound: true, cancellationToken)!;
        }

        /// <summary>Returns the element at a specified index in a sequence, or a default value if the index is out of range.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return an element from.</param>
        /// <param name="index">The index of the element to retrieve, which is either from the beginning or the end of the sequence.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>
        /// The default value of <typeparamref name="TSource"/> if <paramref name="index"/> is outside the bounds of the source sequence; otherwise, the
        /// element at the specified position in the source sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ValueTask<TSource?> ElementAtOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            int index,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return ElementAtOrDefaultAsync(source, index, throwIfNotFound: false, cancellationToken);
        }

        /// <summary>Returns the element at a specified index in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}" /> to return an element from.</param>
        /// <param name="index">The index of the element to retrieve, which is either from the start or the end.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is outside the bounds of the <paramref name="source" /> sequence.</exception>
        /// <returns>The element at the specified position in the <paramref name="source" /> sequence.</returns>
        /// <remarks>
        /// <para>If the type of <paramref name="source" /> implements <see cref="IList{T}" />, that implementation is used to obtain the element at the specified index. Otherwise, this method obtains the specified element.</para>
        /// <para>This method throws an exception if <paramref name="index" /> is out of range. To instead return a default value when the specified index is out of range, use the ElementAtOrDefaultAsync method.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the bounds of the source sequence (via the returned task).</exception>
        public static ValueTask<TSource> ElementAtAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Index index,
            CancellationToken cancellationToken = default)
        {
            if (!index.IsFromEnd)
            {
                return ElementAtAsync(source, index.Value, cancellationToken);
            }

            ThrowHelper.ThrowIfNull(source);

            return ElementAtFromEndOrDefault(source, index.Value, throwIfNotFound: true, cancellationToken)!;
        }

        /// <summary>Returns the element at a specified index in a sequence or a default value if the index is out of range.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}" /> to return an element from.</param>
        /// <param name="index">The index of the element to retrieve, which is either from the start or the end.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <returns><see langword="default" /> if <paramref name="index" /> is outside the bounds of the <paramref name="source" /> sequence; otherwise, the element at the specified position in the <paramref name="source" /> sequence.</returns>
        /// <remarks>
        /// <para>If the type of <paramref name="source" /> implements <see cref="IList{T}" />, that implementation is used to obtain the element at the specified index. Otherwise, this method obtains the specified element.</para>
        /// <para>The default value for reference and nullable types is <see langword="null" />.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ValueTask<TSource?> ElementAtOrDefaultAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Index index,
            CancellationToken cancellationToken = default)
        {
            if (!index.IsFromEnd)
            {
                return ElementAtOrDefaultAsync(source, index.Value, cancellationToken);
            }

            ThrowHelper.ThrowIfNull(source);

            return ElementAtFromEndOrDefault(source, index.Value, throwIfNotFound: false, cancellationToken);
        }

        private static async ValueTask<TSource?> ElementAtOrDefaultAsync<TSource>(
            IAsyncEnumerable<TSource> source,
            int index,
            bool throwIfNotFound,
            CancellationToken cancellationToken = default)
        {
            if (index >= 0)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    while (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        if (index == 0)
                        {
                            return e.Current;
                        }

                        index--;
                    }
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }

            if (throwIfNotFound)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
            }

            return default;
        }

        private static async ValueTask<TSource?> ElementAtFromEndOrDefault<TSource>(
            IAsyncEnumerable<TSource> source,
            int indexFromEnd,
            bool throwIfNotFound,
            CancellationToken cancellationToken)
        {
            if (indexFromEnd > 0)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        Queue<TSource> queue = new();
                        queue.Enqueue(e.Current);

                        while (await e.MoveNextAsync().ConfigureAwait(false))
                        {
                            if (queue.Count == indexFromEnd)
                            {
                                queue.Dequeue();
                            }

                            queue.Enqueue(e.Current);
                        }

                        if (queue.Count == indexFromEnd)
                        {
                            return queue.Dequeue();
                        }
                    }
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }

            if (throwIfNotFound)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException("index");
            }

            return default;
        }
    }
}
