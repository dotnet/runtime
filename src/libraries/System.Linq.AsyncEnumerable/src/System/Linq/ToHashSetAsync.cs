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
        /// <summary>Creates a <see cref="HashSet{T}"/> from an <see cref="IAsyncEnumerable{T}"/>.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to create a <see cref="HashSet{T}"/> from.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare keys.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>A <see cref="HashSet{T}"/> that contains values of type <typeparamref name="TSource"/> selected from the input sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ValueTask<HashSet<TSource>> ToHashSetAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            IEqualityComparer<TSource>? comparer = null,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false), comparer);

            static async ValueTask<HashSet<TSource>> Impl(
                ConfiguredCancelableAsyncEnumerable<TSource> source,
                IEqualityComparer<TSource>? comparer)
            {
                HashSet<TSource> set = new(comparer);
                await foreach (TSource element in source)
                {
                    set.Add(element);
                }

                return set;
            }
        }
    }
}
