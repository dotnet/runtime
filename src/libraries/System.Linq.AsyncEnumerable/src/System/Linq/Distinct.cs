// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Returns distinct elements from a sequence.</summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare values.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains distinct elements from the source sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TSource> Distinct<TSource>(
            this IAsyncEnumerable<TSource> source,
            IEqualityComparer<TSource>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);

            return
                source.IsKnownEmpty() ? Empty<TSource>() :
                Impl(source, comparer, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                IEqualityComparer<TSource>? comparer,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    if (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        HashSet<TSource> set = new(comparer);
                        do
                        {
                            TSource element = e.Current;
                            if (set.Add(element))
                            {
                                yield return element;
                            }
                        }
                        while (await e.MoveNextAsync().ConfigureAwait(false));
                    }
                }
                finally
                {
                    await e.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
