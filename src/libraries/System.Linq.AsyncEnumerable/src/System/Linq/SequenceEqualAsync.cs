// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Determines whether two sequences are equal by comparing their elements.</summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="first">An <see cref="IAsyncEnumerable{T}"/> to compare to <paramref name="second"/>.</param>
        /// <param name="second">An <see cref="IAsyncEnumerable{T}"/> to compare to the first sequence.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to use to compare elements.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the two source sequences are of equal length and their corresponding
        /// elements compare equal according to comparer; otherwise, <see langword="false"/>.
        /// </returns>
        public static ValueTask<bool> SequenceEqualAsync<TSource>(
            this IAsyncEnumerable<TSource> first,
            IAsyncEnumerable<TSource> second,
            IEqualityComparer<TSource>? comparer = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);

            return Impl(first, second, comparer ?? EqualityComparer<TSource>.Default, cancellationToken);

            static async ValueTask<bool> Impl(
                IAsyncEnumerable<TSource> first,
                IAsyncEnumerable<TSource> second,
                IEqualityComparer<TSource> comparer,
                CancellationToken cancellationToken)
            {
                await using IAsyncEnumerator<TSource> e1 = first.GetAsyncEnumerator(cancellationToken);
                await using IAsyncEnumerator<TSource> e2 = second.GetAsyncEnumerator(cancellationToken);

                while (await e1.MoveNextAsync())
                {
                    if (!await e2.MoveNextAsync() || !comparer.Equals(e1.Current, e2.Current))
                    {
                        return false;
                    }
                }

                return !await e2.MoveNextAsync();
            }
        }
    }
}
