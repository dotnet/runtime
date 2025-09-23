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
        /// Bypasses elements in a sequence as long as a specified condition is true and
        /// then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that contains the elements from the
        /// input sequence starting at the first element in the linear series that does not
        /// pass the test specified by predicate.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> SkipWhile<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return
                source.IsKnownEmpty() ? Empty<TSource>() :
                Impl(source, predicate, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, bool> predicate,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await using IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);

                while (await e.MoveNextAsync())
                {
                    TSource element = e.Current;
                    if (!predicate(element))
                    {
                        yield return element;
                        while (await e.MoveNextAsync())
                        {
                            yield return e.Current;
                        }

                        yield break;
                    }
                }
            }
        }

        /// <summary>
        /// Bypasses elements in a sequence as long as a specified condition is true and
        /// then returns the remaining elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return elements from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that contains the elements from the
        /// input sequence starting at the first element in the linear series that does not
        /// pass the test specified by predicate.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> SkipWhile<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return
                source.IsKnownEmpty() ? Empty<TSource>() :
                Impl(source, predicate, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<bool>> predicate,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await using IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);

                while (await e.MoveNextAsync())
                {
                    TSource element = e.Current;
                    if (!await predicate(element, cancellationToken))
                    {
                        yield return element;
                        while (await e.MoveNextAsync())
                        {
                            yield return e.Current;
                        }

                        yield break;
                    }
                }
            }
        }

        /// <summary>
        /// Bypasses elements in a sequence as long as a specified condition is true and
        /// then returns the remaining elements. The element's index is used in the logic
        /// of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return elements from.</param>
        /// <param name="predicate">
        /// A function to test each element for a condition; the second parameter
        /// of the function represents the index of the source element.
        /// </param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that contains the elements from the
        /// input sequence starting at the first element in the linear series that does not
        /// pass the test specified by predicate.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> SkipWhile<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return
                source.IsKnownEmpty() ? Empty<TSource>() :
                Impl(source, predicate, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, int, bool> predicate,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await using IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);

                int index = -1;
                while (await e.MoveNextAsync())
                {
                    TSource element = e.Current;
                    if (!predicate(element, checked(++index)))
                    {
                        yield return element;
                        while (await e.MoveNextAsync())
                        {
                            yield return e.Current;
                        }

                        yield break;
                    }
                }
            }
        }

        /// <summary>
        /// Bypasses elements in a sequence as long as a specified condition is true and
        /// then returns the remaining elements. The element's index is used in the logic
        /// of the predicate function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return elements from.</param>
        /// <param name="predicate">
        /// A function to test each element for a condition; the second parameter
        /// of the function represents the index of the source element.
        /// </param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that contains the elements from the
        /// input sequence starting at the first element in the linear series that does not
        /// pass the test specified by predicate.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> SkipWhile<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, CancellationToken, ValueTask<bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(predicate);

            return
                source.IsKnownEmpty() ? Empty<TSource>() :
                Impl(source, predicate, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, int, CancellationToken, ValueTask<bool>> predicate,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await using IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);

                int index = -1;
                while (await e.MoveNextAsync())
                {
                    TSource element = e.Current;
                    if (!await predicate(element, checked(++index), cancellationToken))
                    {
                        yield return element;
                        while (await e.MoveNextAsync())
                        {
                            yield return e.Current;
                        }

                        yield break;
                    }
                }
            }
        }
    }
}
