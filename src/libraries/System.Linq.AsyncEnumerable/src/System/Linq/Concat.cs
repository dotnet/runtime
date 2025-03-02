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
        /// <summary>Concatenates two sequences.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">The first sequence to concatenate.</param>
        /// <param name="second">The sequence to concatenate to the first sequence.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the concatenated elements of the two input sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TSource> Concat<TSource>(
            this IAsyncEnumerable<TSource> first, IAsyncEnumerable<TSource> second)
        {
            ThrowHelper.ThrowIfNull(first);
            ThrowHelper.ThrowIfNull(second);

            return
                first.IsKnownEmpty() ? second :
                second.IsKnownEmpty() ? first :
                Impl(first, second, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> first,
                IAsyncEnumerable<TSource> second,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (TSource item in first.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return item;
                }

                await foreach (TSource item in second.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return item;
                }
            }
        }
    }
}
