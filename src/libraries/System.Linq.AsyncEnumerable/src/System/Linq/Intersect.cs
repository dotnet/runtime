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
        /// <summary>Produces the set intersection of two sequences.</summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="first">An <see cref="IAsyncEnumerable{T}"/> whose distinct elements that also appear in second will be returned.</param>
        /// <param name="second">An <see cref="IAsyncEnumerable{T}"/> whose distinct elements that also appear in the first sequence will be returned.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        /// <returns>A sequence that contains the elements that form the set intersection of two sequences.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="first" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="second" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> Intersect<TSource>(
            this IAsyncEnumerable<TSource> first,
            IAsyncEnumerable<TSource> second,
            IEqualityComparer<TSource>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(first);
            ThrowHelper.ThrowIfNull(second);

            return
                first.IsKnownEmpty() || second.IsKnownEmpty() ? Empty<TSource>() :
                Impl(first, second, comparer, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> first,
                IAsyncEnumerable<TSource> second,
                IEqualityComparer<TSource>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                HashSet<TSource> set;
                IAsyncEnumerator<TSource> e = second.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (!await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield break;
                    }

                    set = new(comparer);
                    do
                    {
                        set.Add(e.Current);
                    }
                    while (await e.MoveNextAsync().ConfigureAwait(false));
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }

                await foreach (TSource element in first.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (set.Remove(element))
                    {
                        yield return element;
                    }
                }
            }
        }
    }
}
