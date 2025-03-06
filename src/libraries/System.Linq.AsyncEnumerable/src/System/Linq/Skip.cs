// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Bypasses a specified number of elements in a sequence and then returns the remaining elements.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return elements from.</param>
        /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the elements that occur after the specified index in the input sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> Skip<TSource>(
            this IAsyncEnumerable<TSource> source,
            int count)
        {
            ThrowHelper.ThrowIfNull(source);

            return
                source.IsKnownEmpty() ? Empty<TSource>() :
                count <= 0 ? source :
                Impl(source, count, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                int count,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    while (count > 0 && await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        count--;
                    }

                    if (count <= 0)
                    {
                        while (await e.MoveNextAsync().ConfigureAwait(false))
                        {
                            yield return e.Current;
                        }
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
