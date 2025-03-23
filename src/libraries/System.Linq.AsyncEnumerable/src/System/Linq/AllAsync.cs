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
        /// <summary>Determines whether all elements of a sequence satisfy a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> that contains the elements to apply the predicate to.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>
        /// <see langword="true"/> if every element of the source sequence passes the test in the specified predicate,
        /// or if the sequence is empty; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
        public static ValueTask<bool> AllAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false), predicate);

            static async ValueTask<bool> Impl(
                ConfiguredCancelableAsyncEnumerable<TSource> source,
                Func<TSource, bool> predicate)
            {
                await foreach (TSource element in source)
                {
                    if (!predicate(element))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Determines whether all elements of a sequence satisfy a condition.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> that contains the elements to apply the predicate to.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>
        /// <see langword="true"/> if every element of the source sequence passes the test in the specified predicate,
        /// or if the sequence is empty; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
        public static ValueTask<bool> AllAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken,
            ValueTask<bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNull(predicate);

            return Impl(source, predicate, cancellationToken);

            static async ValueTask<bool> Impl(
                IAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, ValueTask<bool>> predicate,
                CancellationToken cancellationToken)
            {
                await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (!await predicate(element, cancellationToken).ConfigureAwait(false))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
