// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    internal static class LengthBuckets
    {
        /// <summary>The maximum number of items allowed per bucket.  The larger the value, the longer it can take to search a bucket, which is sequentially examined.</summary>
        internal const int MaxPerLength = 5;
        /// <summary>Allowed ratio between buckets with values and total buckets.  Under this ratio, this implementation won't be used due to too much wasted space.</summary>
        private const double EmptyLengthsRatio = 0.2;

        internal static int[]? CreateLengthBucketsArrayIfAppropriate(string[] keys, IEqualityComparer<string> comparer, int minLength, int maxLength)
        {
            Debug.Assert(comparer == EqualityComparer<string>.Default || comparer == StringComparer.Ordinal || comparer == StringComparer.OrdinalIgnoreCase);
            Debug.Assert(minLength >= 0 && maxLength >= minLength);

            // If without even looking at the keys we know that some bucket will exceed the max per-bucket
            // limit (pigeon hole principle), we can early-exit out without doing any further work.
            int spread = maxLength - minLength + 1;
            if (keys.Length / spread > MaxPerLength)
            {
                return null;
            }

            int arraySize = spread * MaxPerLength;
#if NET
            if (arraySize > Array.MaxLength)
#else
            if (arraySize > 0X7FFFFFC7)
#endif
            {
                // In the future we may lower the value, as it may be quite unlikely
                // to have a LOT of strings of different sizes.
                return null;
            }

            // Instead of creating a dictionary of lists or a multi-dimensional array
            // we rent a single dimension array, where every bucket has five slots.
            // The bucket starts at (key.Length - minLength) * 5.
            // Each value is an index of the key from _keys array
            // or just -1, which represents "null".
            int[] buckets = ArrayPool<int>.Shared.Rent(arraySize);
            buckets.AsSpan(0, arraySize).Fill(-1);

            int nonEmptyCount = 0;
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                int startIndex = (key.Length - minLength) * MaxPerLength;
                int endIndex = startIndex + MaxPerLength;
                int index = startIndex;

                while (index < endIndex)
                {
                    ref int bucket = ref buckets[index];
                    if (bucket < 0)
                    {
                        if (index == startIndex)
                        {
                            nonEmptyCount++;
                        }

                        bucket = i;
                        break;
                    }

                    index++;
                }

                if (index == endIndex)
                {
                    // If we've already hit the max per-bucket limit, bail.
                    ArrayPool<int>.Shared.Return(buckets);
                    return null;
                }
            }

            // If there would be too much empty space in the lookup array, bail.
            if (nonEmptyCount / (double)spread < EmptyLengthsRatio)
            {
                ArrayPool<int>.Shared.Return(buckets);
                return null;
            }

#if NET
            // We don't need an array with every value initialized to zero if we are just about to overwrite every value anyway.
            int[] copy = GC.AllocateUninitializedArray<int>(arraySize);
            Array.Copy(buckets, copy, arraySize);
#else
            int[] copy = buckets.AsSpan(0, arraySize).ToArray();
#endif
            ArrayPool<int>.Shared.Return(buckets);

            return copy;
        }
    }
}
