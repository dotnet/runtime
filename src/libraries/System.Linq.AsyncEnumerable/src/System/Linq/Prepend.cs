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
        /// <summary>Adds a value to the beginning of the sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="element">The value to prepend to source.</param>
        /// <returns>A new sequence that begins with element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TSource> Prepend<TSource>(
            this IAsyncEnumerable<TSource> source,
            TSource element)
        {
            ThrowHelper.ThrowIfNull(source);

            return Impl(source, element, default);

            static async IAsyncEnumerable<TSource> Impl(
                IAsyncEnumerable<TSource> source,
                TSource element,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                yield return element;

                await foreach (TSource item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return item;
                }
            }
        }
    }
}
