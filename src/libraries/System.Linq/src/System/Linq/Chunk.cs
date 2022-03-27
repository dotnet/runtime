// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>
        /// Split the elements of a sequence into chunks of size at most <paramref name="size"/>.
        /// </summary>
        /// <remarks>
        /// Every chunk except the last will be of size <paramref name="size"/>.
        /// The last chunk will contain the remaining elements and may be of a smaller size.
        /// </remarks>
        /// <param name="source">
        /// An <see cref="IEnumerable{T}"/> whose elements to chunk.
        /// </param>
        /// <param name="size">
        /// Maximum size of each chunk.
        /// </param>
        /// <typeparam name="TSource">
        /// The type of the elements of source.
        /// </typeparam>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> that contains the elements the input sequence split into chunks of size <paramref name="size"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="size"/> is below 1.
        /// </exception>
        public static IEnumerable<TSource[]> Chunk<TSource>(this IEnumerable<TSource> source, int size)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (size < 1)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.size);
            }

            return ChunkIterator(source, size);
        }

        private static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source, int size)
        {
            using IEnumerator<TSource> e = source.GetEnumerator();

            // TODO what should the threshold be?
            if (size > 1000)
            {
                if (TryCreateInitialChunkForLargeSize(e, size, out TSource[]? chunk))
                {
                    yield return chunk;
                    if (chunk.Length < size)
                    {
                        yield break;
                    }
                }
                else
                {
                    yield break;
                }
            }

            while (e.MoveNext())
            {
                TSource[] chunk = new TSource[size];
                chunk[0] = e.Current;

                int i = 1;
                for (; i < chunk.Length && e.MoveNext(); i++)
                {
                    chunk[i] = e.Current;
                }

                if (i == chunk.Length)
                {
                    yield return chunk;
                }
                else
                {
                    Array.Resize(ref chunk, i);
                    yield return chunk;
                    yield break;
                }
            }
        }

        /// <summary>
        /// When <paramref name="size"/> is very large, in many cases there will only be one chunk and that chunk will
        /// be much smaller than <paramref name="size"/>. Therefore, it is worthwhile to build that chunk incrementally
        /// rather than pre-allocating a <paramref name="size"/>-length array.
        /// </summary>
        private static bool TryCreateInitialChunkForLargeSize<TSource>(IEnumerator<TSource> enumerator, int size, [NotNullWhen(returnValue: true)] out TSource[]? chunk)
        {
            LargeArrayBuilder<TSource> builder = new();
            for (var i = 0; i < size && enumerator.MoveNext(); i++)
            {
                builder.Add(enumerator.Current);
            }

            if (builder.Count == 0)
            {
                chunk = null;
                return false;
            }

            chunk = builder.ToArray();
            return true;
        }
    }
}
