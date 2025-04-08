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
        /// <summary>Creates an array from an <see cref="IAsyncEnumerable{T}"/>.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to create an array from.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>An array that contains the elements from the input sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static ValueTask<TSource[]> ToArrayAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false));

            static async ValueTask<TSource[]> Impl(
                ConfiguredCancelableAsyncEnumerable<TSource> source)
            {
                ConfiguredCancelableAsyncEnumerable<TSource>.Enumerator e = source.GetAsyncEnumerator();
                try
                {
                    if (await e.MoveNextAsync())
                    {
                        List<TSource> list = [];
                        do
                        {
                            list.Add(e.Current);
                        }
                        while (await e.MoveNextAsync());

                        return list.ToArray();
                    }

                    return [];
                }
                finally
                {
                    await e.DisposeAsync();
                }
            }
        }
    }
}
