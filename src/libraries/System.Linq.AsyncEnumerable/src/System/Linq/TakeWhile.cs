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
        /// <summary>Returns elements from a sequence as long as a specified condition is true.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that contains the elements from the
        /// input sequence that occur before the element at which the test no longer passes.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> TakeWhile<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return
                source.IsKnownEmpty() ? Empty<TSource>() :
                Impl(source, predicate, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source, Func<TSource, bool> predicate,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (!predicate(element))
                    {
                        break;
                    }

                    yield return element;
                }
            }
        }

        /// <summary>Returns elements from a sequence as long as a specified condition is true.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that contains the elements from the
        /// input sequence that occur before the element at which the test no longer passes.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> TakeWhile<TSource>(
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
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (!await predicate(element, cancellationToken).ConfigureAwait(false))
                    {
                        break;
                    }

                    yield return element;
                }
            }
        }

        /// <summary>
        /// Returns elements from a sequence as long as a specified condition is true.
        /// The element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that contains the elements from the
        /// input sequence that occur before the element at which the test no longer passes.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> TakeWhile<TSource>(
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
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (!predicate(element, checked(++index)))
                    {
                        break;
                    }

                    yield return element;
                }
            }
        }

        /// <summary>
        /// Returns elements from a sequence as long as a specified condition is true.
        /// The element's index is used in the logic of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that contains the elements from the
        /// input sequence that occur before the element at which the test no longer passes.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> TakeWhile<TSource>(
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
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (!await predicate(element, checked(++index), cancellationToken).ConfigureAwait(false))
                    {
                        break;
                    }

                    yield return element;
                }
            }
        }
    }
}
