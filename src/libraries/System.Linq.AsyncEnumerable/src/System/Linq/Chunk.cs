// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Split the elements of a sequence into chunks of size at most <paramref name="size"/>.</summary>
        /// <remarks>
        /// Every chunk except the last will be of size <paramref name="size"/>.
        /// The last chunk will contain the remaining elements and may be of a smaller size.
        /// </remarks>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IAsyncEnumerable{T}"/> whose elements to chunk.</param>
        /// <param name="size">Maximum size of each chunk.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{T}"/> that contains the elements of the input sequence split into chunks of size <paramref name="size"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is less than 1.</exception>
        public static IAsyncEnumerable<TSource[]> Chunk<TSource>(
            this IAsyncEnumerable<TSource> source,
            int size)
        {
            ThrowHelper.ThrowIfNull(source);
            ThrowHelper.ThrowIfNegativeOrZero(size);

            return
                source.IsKnownEmpty() ? Empty<TSource[]>() :
                Chunk(source, size, default);

            async static IAsyncEnumerable<TSource[]> Chunk(
                IAsyncEnumerable<TSource> source,
                int size,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                IAsyncEnumerator<TSource> e = source.GetAsyncEnumerator(cancellationToken);
                try
                {
                    // Before allocating anything, make sure there's at least one element.
                    if (await e.MoveNextAsync().ConfigureAwait(false))
                    {
                        // Now that we know we have at least one item, allocate an initial storage array. This is not
                        // the array we'll yield.  It starts out small in order to avoid significantly overallocating
                        // when the source has many fewer elements than the chunk size.
                        int arraySize = Math.Min(size, 4);
                        int i;
                        do
                        {
                            var array = new TSource[arraySize];

                            // Store the first item.
                            array[0] = e.Current;
                            i = 1;

                            if (size != array.Length)
                            {
                                // This is the first chunk. As we fill the array, grow it as needed.
                                for (; i < size && await e.MoveNextAsync().ConfigureAwait(false); i++)
                                {
                                    if (i >= array.Length)
                                    {
                                        arraySize = (int)Math.Min((uint)size, 2 * (uint)array.Length);
                                        Array.Resize(ref array, arraySize);
                                    }

                                    array[i] = e.Current;
                                }
                            }
                            else
                            {
                                // For all but the first chunk, the array will already be correctly sized.
                                // We can just store into it until either it's full or MoveNext returns false.
                                TSource[] local = array; // avoid bounds checks by using cached local (`array` is lifted to iterator object as a field)
                                Debug.Assert(local.Length == size);
                                for (; (uint)i < (uint)local.Length && await e.MoveNextAsync().ConfigureAwait(false); i++)
                                {
                                    local[i] = e.Current;
                                }
                            }

                            if (i != array.Length)
                            {
                                Array.Resize(ref array, i);
                            }

                            yield return array;
                        }
                        while (i >= size && await e.MoveNextAsync().ConfigureAwait(false));
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
