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
        /// <summary>Determines whether a sequence contains a specified element.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence in which to locate a value.</param>
        /// <param name="value">The value to locate in the sequence.</param>
        /// <param name="comparer">An equality comparer to compare values.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns><see langword="true"/> if the source sequence contains an element that has the specified value; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static ValueTask<bool> ContainsAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            TSource value,
            IEqualityComparer<TSource>? comparer = null,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source.WithCancellation(cancellationToken).ConfigureAwait(false), value, comparer ?? EqualityComparer<TSource>.Default);

            async static ValueTask<bool> Impl(
                ConfiguredCancelableAsyncEnumerable<TSource> source,
                TSource value,
                IEqualityComparer<TSource> comparer)
            {
                await foreach (TSource element in source)
                {
                    if (comparer.Equals(element, value))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
