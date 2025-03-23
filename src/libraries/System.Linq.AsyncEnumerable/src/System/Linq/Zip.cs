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
        /// Applies a specified function to the corresponding elements of two sequences,
        /// producing a sequence of the results.
        /// </summary>
        /// <typeparam name="TFirst">The type of the elements of the first input sequence.</typeparam>
        /// <typeparam name="TSecond">The type of the elements of the second input sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the result sequence.</typeparam>
        /// <param name="first">The first sequence to merge.</param>
        /// <param name="second">The second sequence to merge.</param>
        /// <param name="resultSelector">A function that specifies how to merge the elements from the two sequences.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains merged elements of two input sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TResult> Zip<TFirst, TSecond, TResult>(
            this IAsyncEnumerable<TFirst> first,
            IAsyncEnumerable<TSecond> second,
            Func<TFirst, TSecond, TResult> resultSelector)
        {
            ThrowHelper.ThrowIfNull(first);
            ThrowHelper.ThrowIfNull(second);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                first.IsKnownEmpty() || second.IsKnownEmpty() ? Empty<TResult>() :
                Impl(first, second, resultSelector, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TFirst> first,
                IAsyncEnumerable<TSecond> second,
                Func<TFirst, TSecond, TResult> resultSelector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TFirst> e1 = first.GetAsyncEnumerator(cancellationToken);
                try
                {
                    IAsyncEnumerator<TSecond> e2 = second.GetAsyncEnumerator(cancellationToken);
                    try
                    {
                        while (await e1.MoveNextAsync().ConfigureAwait(false) &&
                               await e2.MoveNextAsync().ConfigureAwait(false))
                        {
                            yield return resultSelector(e1.Current, e2.Current);
                        }
                    }
                    finally
                    {
                        await e2.DisposeAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    await e1.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Applies a specified function to the corresponding elements of two sequences,
        /// producing a sequence of the results.
        /// </summary>
        /// <typeparam name="TFirst">The type of the elements of the first input sequence.</typeparam>
        /// <typeparam name="TSecond">The type of the elements of the second input sequence.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the result sequence.</typeparam>
        /// <param name="first">The first sequence to merge.</param>
        /// <param name="second">The second sequence to merge.</param>
        /// <param name="resultSelector">A function that specifies how to merge the elements from the two sequences.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains merged elements of two input sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TResult> Zip<TFirst, TSecond, TResult>(
            this IAsyncEnumerable<TFirst> first,
            IAsyncEnumerable<TSecond> second,
            Func<TFirst, TSecond, CancellationToken, ValueTask<TResult>> resultSelector)
        {
            ThrowHelper.ThrowIfNull(first);
            ThrowHelper.ThrowIfNull(second);
            ThrowHelper.ThrowIfNull(resultSelector);

            return
                first.IsKnownEmpty() || second.IsKnownEmpty() ? Empty<TResult>() :
                Impl(first, second, resultSelector, default);

            static async IAsyncEnumerable<TResult> Impl(
                IAsyncEnumerable<TFirst> first,
                IAsyncEnumerable<TSecond> second,
                Func<TFirst, TSecond, CancellationToken, ValueTask<TResult>> resultSelector,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TFirst> e1 = first.GetAsyncEnumerator(cancellationToken);
                try
                {
                    IAsyncEnumerator<TSecond> e2 = second.GetAsyncEnumerator(cancellationToken);
                    try
                    {
                        while (await e1.MoveNextAsync().ConfigureAwait(false) &&
                               await e2.MoveNextAsync().ConfigureAwait(false))
                        {
                            yield return await resultSelector(e1.Current, e2.Current, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        await e2.DisposeAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    await e1.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Produces a sequence of tuples with elements from the two specified sequences.</summary>
        /// <typeparam name="TFirst">The type of the elements of the first input sequence.</typeparam>
        /// <typeparam name="TSecond">The type of the elements of the second input sequence.</typeparam>
        /// <param name="first">The first sequence to merge.</param>
        /// <param name="second">The second sequence to merge.</param>
        /// <returns>A sequence of tuples with elements taken from the first and second sequences, in that order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(
            this IAsyncEnumerable<TFirst> first,
            IAsyncEnumerable<TSecond> second)
        {
            ThrowHelper.ThrowIfNull(first);
            ThrowHelper.ThrowIfNull(second);

            return
                first.IsKnownEmpty() || second.IsKnownEmpty() ? Empty<(TFirst, TSecond)>() :
                Impl(first, second, default);

            static async IAsyncEnumerable<(TFirst First, TSecond Second)> Impl(
                IAsyncEnumerable<TFirst> first,
                IAsyncEnumerable<TSecond> second,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TFirst> e1 = first.GetAsyncEnumerator(cancellationToken);
                try
                {
                    IAsyncEnumerator<TSecond> e2 = second.GetAsyncEnumerator(cancellationToken);
                    try
                    {
                        while (await e1.MoveNextAsync().ConfigureAwait(false) &&
                               await e2.MoveNextAsync().ConfigureAwait(false))
                        {
                            yield return (e1.Current, e2.Current);
                        }
                    }
                    finally
                    {
                        await e2.DisposeAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    await e1.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>Produces a sequence of tuples with elements from the three specified sequences.</summary>
        /// <typeparam name="TFirst">The type of the elements of the first input sequence.</typeparam>
        /// <typeparam name="TSecond">The type of the elements of the second input sequence.</typeparam>
        /// <typeparam name="TThird">The type of the elements of the third input sequence.</typeparam>
        /// <param name="first">The first sequence to merge.</param>
        /// <param name="second">The second sequence to merge.</param>
        /// <param name="third">The third sequence to merge.</param>
        /// <returns>A sequence of tuples with elements taken from the first, second, and third sequences, in that order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="third" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<(TFirst First, TSecond Second, TThird Third)> Zip<TFirst, TSecond, TThird>(
            this IAsyncEnumerable<TFirst> first,
            IAsyncEnumerable<TSecond> second,
            IAsyncEnumerable<TThird> third)
        {
            ThrowHelper.ThrowIfNull(first);
            ThrowHelper.ThrowIfNull(second);
            ThrowHelper.ThrowIfNull(third);

            return
                first.IsKnownEmpty() || second.IsKnownEmpty() || third.IsKnownEmpty() ? Empty<(TFirst, TSecond, TThird)>() :
                Impl(first, second, third, default);

            static async IAsyncEnumerable<(TFirst First, TSecond Second, TThird)> Impl(
                IAsyncEnumerable<TFirst> first, IAsyncEnumerable<TSecond> second, IAsyncEnumerable<TThird> third, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TFirst> e1 = first.GetAsyncEnumerator(cancellationToken);
                try
                {
                    IAsyncEnumerator<TSecond> e2 = second.GetAsyncEnumerator(cancellationToken);
                    try
                    {
                        IAsyncEnumerator<TThird> e3 = third.GetAsyncEnumerator(cancellationToken);
                        try
                        {
                            while (await e1.MoveNextAsync().ConfigureAwait(false) &&
                                   await e2.MoveNextAsync().ConfigureAwait(false) &&
                                   await e3.MoveNextAsync().ConfigureAwait(false))
                            {
                                yield return (e1.Current, e2.Current, e3.Current);
                            }
                        }
                        finally
                        {
                            await e3.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        await e2.DisposeAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    await e1.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
