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
        /// <summary>Produces the set difference of two sequences.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">An <see cref="IAsyncEnumerable{T}"/> whose elements that are not also in second will be returned.</param>
        /// <param name="second">An <see cref="IAsyncEnumerable{T}"/> whose elements that also occur in the first sequence will cause those elements to be removed from the returned sequence.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TSource> Except<TSource>(
            this IAsyncEnumerable<TSource> first,
            IAsyncEnumerable<TSource> second,
            IEqualityComparer<TSource>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(first);
            ThrowHelper.ThrowIfNull(second);

            return Impl(first, second, comparer, default);

            async static IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> first,
                IAsyncEnumerable<TSource> second,
                IEqualityComparer<TSource>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                HashSet<TSource> set = new(comparer);

                await foreach (TSource element in second.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    set.Add(element);
                }

                await foreach (TSource element in first.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (set.Add(element))
                    {
                        yield return element;
                    }
                }
            }
        }
    }
}
