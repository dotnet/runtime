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
        /// <summary>Filters a sequence of values based on a predicate.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> Where<TSource>( // satisfies the C# query-expression pattern
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return
                source.IsKnownEmpty() ? Empty<TSource>() :
                Impl(source, predicate, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, bool> predicate,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (TSource element in source.WithCancellation(cancellationToken))
                {
                    if (predicate(element))
                    {
                        yield return element;
                    }
                }
            }
        }

        /// <summary>Filters a sequence of values based on a predicate.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to filter.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> Where<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<bool>> predicate)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return
                source.IsKnownEmpty() ? Empty<TSource>() :
                Impl(source, predicate, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<bool>> predicate,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (TSource element in source.WithCancellation(cancellationToken))
                {
                    if (await predicate(element, cancellationToken).ConfigureAwait(false))
                    {
                        yield return element;
                    }
                }
            }
        }

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// Each element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to filter.</param>
        /// <param name="predicate">
        /// A function to test each element for a condition; the second parameter
        /// of the function represents the index of the source element.
        /// </param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> Where<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, bool> predicate)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return
                source.IsKnownEmpty() ? Empty<TSource>() :
                Impl(source, predicate, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, int, bool> predicate,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                int index = -1;
                await foreach (TSource element in source.WithCancellation(cancellationToken))
                {
                    if (predicate(element, checked(++index)))
                    {
                        yield return element;
                    }
                }
            }
        }

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// Each element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to filter.</param>
        /// <param name="predicate">
        /// A function to test each element for a condition; the second parameter
        /// of the function represents the index of the source element.
        /// </param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains elements from the input sequence that satisfy the condition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> Where<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, CancellationToken, ValueTask<bool>> predicate)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return
                source.IsKnownEmpty() ? Empty<TSource>() :
                Impl(source, predicate, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, int, CancellationToken, ValueTask<bool>> predicate,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                int index = -1;
                await foreach (TSource element in source.WithCancellation(cancellationToken))
                {
                    if (await predicate(element, checked(++index), cancellationToken).ConfigureAwait(false))
                    {
                        yield return element;
                    }
                }
            }
        }
    }
}
