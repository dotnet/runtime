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
        /// <summary>Produces the set union of two sequences.</summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="first">An <see cref="IAsyncEnumerable{T}"/> whose distinct elements form the first set for the union.</param>
        /// <param name="second">An <see cref="IAsyncEnumerable{T}"/> whose distinct elements form the second set for the union.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the elements from both input sequences, excluding duplicates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TSource> Union<TSource>(
            this IAsyncEnumerable<TSource> first,
            IAsyncEnumerable<TSource> second,
            IEqualityComparer<TSource>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(first);
            ThrowHelper.ThrowIfNull(second);

            return
                first.IsKnownEmpty() && second.IsKnownEmpty() ? Empty<TSource>() :
                Impl(first, second, comparer, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> first,
                IAsyncEnumerable<TSource> second,
                IEqualityComparer<TSource>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                HashSet<TSource> set = new(comparer);

                await foreach (TSource element in first.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (set.Add(element))
                    {
                        yield return element;
                    }
                }

                await foreach (TSource element in second.WithCancellation(cancellationToken).ConfigureAwait(false))
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
