// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Inverts the order of the elements in a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to reverse.</param>
        /// <returns>A sequence whose elements correspond to those of the input sequence in reverse order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TSource> Reverse<TSource>(
            this IAsyncEnumerable<TSource> source)
        {
            ThrowHelper.ThrowIfNull(source);

            return
                source.IsKnownEmpty() ? Empty<TSource>() :
                Impl(source, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                TSource[] array = await source.ToArrayAsync(cancellationToken).ConfigureAwait(false);
                for (int i = array.Length - 1; i >= 0; i--)
                {
                    yield return array[i];
                }
            }
        }
    }
}
