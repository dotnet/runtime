// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>
        /// Split the elements of a sequence into chunks of size at most <paramref name="maxSize"/>.
        /// </summary>
        /// <remarks>
        /// Every chunk except the last will be of size <paramref name="maxSize"/>.
        /// The last chunk will contain the remaining elements and may be of a smaller size.
        /// </remarks>
        /// <param name="source">
        /// An <see cref="IEnumerable{T}"/> whose elements to chunk.
        /// </param>
        /// <param name="maxSize">
        /// Maximum size of each chunk.
        /// </param>
        /// <typeparam name="TSource">
        /// The type of the elements of source.
        /// </typeparam>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> that contains the elements the input sequence split into chunks of size <paramref name="maxSize"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="maxSize"/> is below 1.
        /// </exception>
        public static IEnumerable<TSource[]> Chunk<TSource>(this IEnumerable<TSource> source, int maxSize)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (maxSize < 1)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.maxSize);
            }

            return source switch
            {
                IReadOnlyCollection<TSource> collection => ChunkIteratorOptimized(source, maxSize, collection.Count),
                ICollection<TSource> collection => ChunkIteratorOptimized(source, maxSize, collection.Count),
                _ => ChunkIterator(source, maxSize)
            };
        }

        private static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source, int maxSize)
        {
            using IEnumerator<TSource> e = source.GetEnumerator();

            while (e.MoveNext())
            {
                TSource[] chunk = new TSource[maxSize];
                chunk[0] = e.Current;

                for (int i = 1; i < maxSize; i++)
                {
                    if (!e.MoveNext())
                    {
                        Array.Resize(ref chunk, i);
                        yield return chunk;
                        yield break;
                    }

                    chunk[i] = e.Current;
                }

                yield return chunk;
            }
        }
    }
}