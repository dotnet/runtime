// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Returns a new sequence that contains the last <paramref name="count"/> elements from <paramref name="source"/>.</summary>
        /// <typeparam name="TSource">The type of the elements in <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence to return elements from.</param>
        /// <param name="count">The number of elements to take from the end of the sequence.</param>
        /// <returns>A new sequence that contains the last <paramref name="count"/> elements from <paramref name="source"/>.</returns>
        public static IAsyncEnumerable<TSource> TakeLast<TSource>(
            this IAsyncEnumerable<TSource> source,
            int count)
        {
            ThrowHelper.ThrowIfNull(source);

            return
                source.IsKnownEmpty() || count <= 0 ? Empty<TSource>() :
                TakeRangeFromEndIterator(source, isStartIndexFromEnd: true, startIndex: count, isEndIndexFromEnd: true, endIndex: 0, default);
        }
    }
}
