// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>
        /// Returns a new sequence that contains the elements from <paramref name="source"/>
        /// with the last <paramref name="count"/> elements of the source collection omitted.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> to return elements from.</param>
        /// <param name="count">The number of elements to omit from the end of the sequence.</param>
        /// <returns>
        /// A new sequence that contains the elements from <paramref name="source"/> minus
        /// <paramref name="count"/> elements from the end of the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static IAsyncEnumerable<TSource> SkipLast<TSource>(
            this IAsyncEnumerable<TSource> source,
            int count)
        {
            ThrowHelper.ThrowIfNull(source);

            return
                source.IsKnownEmpty() ? Empty<TSource>() :
                count <= 0 ? source :
                TakeRangeFromEndIterator(source, isStartIndexFromEnd: false, startIndex: 0, isEndIndexFromEnd: true, endIndex: count, default);
        }
    }
}
