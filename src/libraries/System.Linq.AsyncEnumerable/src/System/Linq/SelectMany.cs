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
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/> and
        /// flattens the resulting sequences into one <see cref="IAsyncEnumerable{T}"/> sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by selector.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of
        /// invoking the one-to-many transform function on each element of the input sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IEnumerable<TResult>> selector)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(selector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, selector, default);

            async static IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, IEnumerable<TResult>> selector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    foreach (TResult subElement in selector(element))
                    {
                        yield return subElement;
                    }
                }
            }
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/> and
        /// flattens the resulting sequences into one <see cref="IAsyncEnumerable{T}"/> sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by selector.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of
        /// invoking the one-to-many transform function on each element of the input sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<IEnumerable<TResult>>> selector)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(selector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, selector, default);

            async static IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<IEnumerable<TResult>>> selector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    foreach (TResult subElement in await selector(element, cancellationToken).ConfigureAwait(false))
                    {
                        yield return subElement;
                    }
                }
            }
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/> and
        /// flattens the resulting sequences into one <see cref="IAsyncEnumerable{T}"/> sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by selector.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of
        /// invoking the one-to-many transform function on each element of the input sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IAsyncEnumerable<TResult>> selector)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(selector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, selector, default);

            async static IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, IAsyncEnumerable<TResult>> selector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    await foreach (TResult subElement in selector(element).WithCancellation(cancellationToken).ConfigureAwait(false))
                    {
                        yield return subElement;
                    }
                }
            }
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/> and
        /// flattens the resulting sequences into one <see cref="IAsyncEnumerable{T}"/> sequence.
        /// The index of each source element is used in the projected form of that element.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by selector.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of
        /// invoking the one-to-many transform function on each element of the input sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, IEnumerable<TResult>> selector)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(selector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, selector, default);

            async static IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, int, IEnumerable<TResult>> selector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                int index = -1;
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    foreach (TResult subElement in selector(element, checked(++index)))
                    {
                        yield return subElement;
                    }
                }
            }
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/> and
        /// flattens the resulting sequences into one <see cref="IAsyncEnumerable{T}"/> sequence.
        /// The index of each source element is used in the projected form of that element.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by selector.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of
        /// invoking the one-to-many transform function on each element of the input sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, CancellationToken, ValueTask<IEnumerable<TResult>>> selector)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(selector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, selector, default);

            async static IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, int, CancellationToken, ValueTask<IEnumerable<TResult>>> selector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                int index = -1;
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    foreach (TResult subElement in await selector(element, checked(++index), cancellationToken).ConfigureAwait(false))
                    {
                        yield return subElement;
                    }
                }
            }
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/> and
        /// flattens the resulting sequences into one <see cref="IAsyncEnumerable{T}"/> sequence.
        /// The index of each source element is used in the projected form of that element.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the sequence returned by selector.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of
        /// invoking the one-to-many transform function on each element of the input sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, IAsyncEnumerable<TResult>> selector)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(selector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, selector, default);

            async static IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, int, IAsyncEnumerable<TResult>> selector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                int index = -1;
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    await foreach (TResult subElement in selector(element, checked(++index)).WithCancellation(cancellationToken).ConfigureAwait(false))
                    {
                        yield return subElement;
                    }
                }
            }
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/>,
        /// flattens the resulting sequences into one <see cref="IAsyncEnumerable{T}"/> sequence,
        /// and invokes a result selector function on each element therein. The index of each source element is used in
        /// the intermediate projected form of that element.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of
        /// invoking the one-to-many transform function <paramref name="collectionSelector"/> on each element
        /// of source and then mapping each of those sequence elements and their corresponding
        /// source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(collectionSelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, collectionSelector, resultSelector, default);

            async static IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, IEnumerable<TCollection>> collectionSelector,
                Func<TSource, TCollection, TResult> resultSelector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    foreach (TCollection subElement in collectionSelector(element))
                    {
                        yield return resultSelector(element, subElement);
                    }
                }
            }
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/>,
        /// flattens the resulting sequences into one <see cref="IAsyncEnumerable{T}"/> sequence,
        /// and invokes a result selector function on each element therein. The index of each source element is used in
        /// the intermediate projected form of that element.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of
        /// invoking the one-to-many transform function <paramref name="collectionSelector"/> on each element
        /// of source and then mapping each of those sequence elements and their corresponding
        /// source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, ValueTask<IEnumerable<TCollection>>> collectionSelector,
            Func<TSource, TCollection, CancellationToken, ValueTask<TResult>> resultSelector)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(collectionSelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, collectionSelector, resultSelector, default);

            async static IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<IEnumerable<TCollection>>> collectionSelector,
                Func<TSource, TCollection, CancellationToken, ValueTask<TResult>> resultSelector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    foreach (TCollection subElement in await collectionSelector(element, cancellationToken).ConfigureAwait(false))
                    {
                        yield return await resultSelector(element, subElement, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/>,
        /// flattens the resulting sequences into one <see cref="IAsyncEnumerable{T}"/> sequence,
        /// and invokes a result selector function on each element therein. The index of each source element is used in
        /// the intermediate projected form of that element.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of
        /// invoking the one-to-many transform function <paramref name="collectionSelector"/> on each element
        /// of source and then mapping each of those sequence elements and their corresponding
        /// source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>( // satisfies the C# query-expression pattern
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IAsyncEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(collectionSelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, collectionSelector, resultSelector, default);

            async static IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, IAsyncEnumerable<TCollection>> collectionSelector,
                Func<TSource, TCollection, TResult> resultSelector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    await foreach (TCollection subElement in collectionSelector(element).WithCancellation(cancellationToken).ConfigureAwait(false))
                    {
                        yield return resultSelector(element, subElement);
                    }
                }
            }
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/>,
        /// flattens the resulting sequences into one <see cref="IAsyncEnumerable{T}"/> sequence,
        /// and invokes a result selector function on each element therein. The index of each source element is used in
        /// the intermediate projected form of that element.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of
        /// invoking the one-to-many transform function <paramref name="collectionSelector"/> on each element
        /// of source and then mapping each of those sequence elements and their corresponding
        /// source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IAsyncEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, CancellationToken, ValueTask<TResult>> resultSelector)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(collectionSelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, collectionSelector, resultSelector, default);

            async static IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, IAsyncEnumerable<TCollection>> collectionSelector,
                Func<TSource, TCollection, CancellationToken, ValueTask<TResult>> resultSelector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    await foreach (TCollection subElement in collectionSelector(element).WithCancellation(cancellationToken).ConfigureAwait(false))
                    {
                        yield return await resultSelector(element, subElement, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/>,
        /// flattens the resulting sequences into one <see cref="IAsyncEnumerable{T}"/> sequence,
        /// and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of
        /// invoking the one-to-many transform function <paramref name="collectionSelector"/> on each element
        /// of source and then mapping each of those sequence elements and their corresponding
        /// source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(collectionSelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, collectionSelector, resultSelector, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, int, IEnumerable<TCollection>> collectionSelector,
                Func<TSource, TCollection, TResult> resultSelector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                int index = -1;
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    foreach (TCollection subElement in collectionSelector(element, checked(++index)))
                    {
                        yield return resultSelector(element, subElement);
                    }
                }
            }
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/>,
        /// flattens the resulting sequences into one <see cref="IAsyncEnumerable{T}"/> sequence,
        /// and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of
        /// invoking the one-to-many transform function <paramref name="collectionSelector"/> on each element
        /// of source and then mapping each of those sequence elements and their corresponding
        /// source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, CancellationToken, ValueTask<IEnumerable<TCollection>>> collectionSelector,
            Func<TSource, TCollection, CancellationToken, ValueTask<TResult>> resultSelector)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(collectionSelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, collectionSelector, resultSelector, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, int, CancellationToken, ValueTask<IEnumerable<TCollection>>> collectionSelector,
                Func<TSource, TCollection, CancellationToken, ValueTask<TResult>> resultSelector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                int index = -1;
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    foreach (TCollection subElement in await collectionSelector(element, checked(++index), cancellationToken).ConfigureAwait(false))
                    {
                        yield return await resultSelector(element, subElement, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/>,
        /// flattens the resulting sequences into one <see cref="IAsyncEnumerable{T}"/> sequence,
        /// and invokes a result selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="collectionSelector">A transform function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> whose elements are the result of
        /// invoking the one-to-many transform function <paramref name="collectionSelector"/> on each element
        /// of source and then mapping each of those sequence elements and their corresponding
        /// source element to a result element.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="collectionSelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, IAsyncEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, CancellationToken, ValueTask<TResult>> resultSelector)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(collectionSelector);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                source.IsKnownEmpty() ? Empty<TResult>() :
                Impl(source, collectionSelector, resultSelector, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, int, IAsyncEnumerable<TCollection>> collectionSelector,
                Func<TSource, TCollection, CancellationToken, ValueTask<TResult>> resultSelector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                int index = -1;
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    await foreach (TCollection subElement in collectionSelector(element, checked(++index)).WithCancellation(cancellationToken).ConfigureAwait(false))
                    {
                        yield return await resultSelector(element, subElement, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
