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
        /// <summary>Applies an accumulator function over a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements.</exception>
        public static ValueTask<TSource> AggregateAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TSource, TSource> func,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(func);

            return Impl(source, func, cancellationToken);

            static async ValueTask<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, TSource, TSource> func,
                CancellationToken cancellationToken)
            {
                TSource result;

                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        ThrowHelper.ThrowNoElementsException();
                    }

                    result = e.Current;
                    while (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        result = func(result, e.Current);
                    }

                    return result;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Applies an accumulator function over a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> contains no elements.</exception>
        public static ValueTask<TSource> AggregateAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TSource, CancellationToken, ValueTask<TSource>> func,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(func);

            return Impl(source, func, cancellationToken);

            static async ValueTask<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, TSource, CancellationToken, ValueTask<TSource>> func,
                CancellationToken cancellationToken)
            {
                TSource result;

                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        ThrowHelper.ThrowNoElementsException();
                    }

                    result = e.Current;
                    while (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        result = await func(result, e.Current, cancellationToken).ConfigureAwait(false);
                    }

                    return result;
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Applies an accumulator function over a sequence. The specified seed value is used as the initial accumulator value.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        public static ValueTask<TAccumulate> AggregateAsync<TSource, TAccumulate>(
            this IAsyncEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(func);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false), seed, func);

            static async ValueTask<TAccumulate> Impl(
                ConfiguredCancelableAsyncEnumerable<TSource> source,
                TAccumulate seed,
                Func<TAccumulate, TSource, TAccumulate> func)
            {
                TAccumulate result = seed;

                await foreach (TSource element in source)
                {
                    result = func(result, element);
                }

                return result;
            }
        }

        /// <summary>Applies an accumulator function over a sequence. The specified seed value is used as the initial accumulator value.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        public static ValueTask<TAccumulate> AggregateAsync<TSource, TAccumulate>(
            this IAsyncEnumerable<TSource> source, TAccumulate seed,
            Func<TAccumulate, TSource, CancellationToken, ValueTask<TAccumulate>> func,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(func);

            return Impl(source, seed, func, cancellationToken);

            static async ValueTask<TAccumulate> Impl(
                IAsyncEnumerable<TSource> source, TAccumulate seed,
                Func<TAccumulate, TSource, CancellationToken, ValueTask<TAccumulate>> func,
                CancellationToken cancellationToken = default)
            {
                TAccumulate result = seed;

                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    result = await func(result, element, cancellationToken).ConfigureAwait(false);
                }

                return result;
            }
        }

        /// <summary>
        /// Applies an accumulator function over a sequence. The specified seed value is
        /// used as the initial accumulator value, and the specified function is used to
        /// select the result value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <typeparam name="TResult">The type of the resulting value.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The transformed final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static ValueTask<TResult> AggregateAsync<TSource, TAccumulate, TResult>(
            this IAsyncEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func,
            Func<TAccumulate, TResult> resultSelector,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(func);
            ThrowHelper.ThrowIfNull(resultSelector);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false), seed, func, resultSelector);

            static async ValueTask<TResult> Impl(
                ConfiguredCancelableAsyncEnumerable<TSource> source,
                TAccumulate seed,
                Func<TAccumulate, TSource, TAccumulate> func,
                Func<TAccumulate, TResult> resultSelector)
            {
                TAccumulate result = seed;

                await foreach (TSource element in source)
                {
                    result = func(result, element);
                }

                return resultSelector(result);
            }
        }

        /// <summary>
        /// Applies an accumulator function over a sequence. The specified seed value is
        /// used as the initial accumulator value, and the specified function is used to
        /// select the result value.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
        /// <typeparam name="TResult">The type of the resulting value.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to aggregate over.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="func">An accumulator function to be invoked on each element.</param>
        /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The transformed final accumulator value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static ValueTask<TResult> AggregateAsync<TSource, TAccumulate, TResult>(
            this IAsyncEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, CancellationToken, ValueTask<TAccumulate>> func,
            Func<TAccumulate, CancellationToken, ValueTask<TResult>> resultSelector,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(func);
            ThrowHelper.ThrowIfNull(resultSelector);

            return Impl(source, seed, func, resultSelector, cancellationToken);

            static async ValueTask<TResult> Impl(
                IAsyncEnumerable<TSource> source,
                TAccumulate seed,
                Func<TAccumulate, TSource, CancellationToken, ValueTask<TAccumulate>> func,
                Func<TAccumulate, CancellationToken, ValueTask<TResult>> resultSelector,
                CancellationToken cancellationToken)
            {
                TAccumulate result = seed;

                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    result = await func(result, element, cancellationToken).ConfigureAwait(false);
                }

                return await resultSelector(result, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
