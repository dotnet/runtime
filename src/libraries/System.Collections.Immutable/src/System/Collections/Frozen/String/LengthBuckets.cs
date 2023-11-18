// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    internal static class LengthBuckets
    {
        /// <summary>The maximum number of items allowed per bucket.  The larger the value, the longer it can take to search a bucket, which is examined using binary search.</summary>
        internal const int MaxPerLength = 7;
        /// <summary>Allowed ratio between buckets with values and total buckets.  Under this ratio, this implementation won't be used due to too much wasted space.</summary>
        private const double EmptyLengthsRatio = 0.2;

        /// <summary>This value represents a "null" bucket entry. Not "-1" to avoid conflict with ~0, as we use the bit inversion of N to represent that a node is an internal node and not a leaf.</summary>
        internal const int NullSentinel = int.MinValue;

        /// <summary>Precalculated instructions for mapping a sorted array of varying length into a PreOrdered binary tree.</summary>
        private static readonly int[][] s_sortedBucketToBinaryTree = [
            [NullSentinel, NullSentinel, NullSentinel, NullSentinel, NullSentinel, NullSentinel, NullSentinel],
            [0, NullSentinel, NullSentinel, NullSentinel, NullSentinel, NullSentinel, NullSentinel],
            [~1, 0, NullSentinel, NullSentinel, NullSentinel, NullSentinel, NullSentinel],
            [~1, 0, NullSentinel, NullSentinel, 2, NullSentinel, NullSentinel],
            [~1, 0, NullSentinel, NullSentinel, ~3, 2, NullSentinel],
            [~1, 0, NullSentinel, NullSentinel, ~3, 2, 4],
            [~3, ~1, 0, 2, ~5, 4, NullSentinel],
            [~3, ~1, 0, 2, ~5, 4, 6],
        ];

        internal static unsafe int[]? CreateLengthBucketsArrayIfAppropriate(string[] keys, IEqualityComparer<string> comparer, int minLength, int maxLength)
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
#if NET6_0_OR_GREATER
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
            // we rent a single dimension array, where every bucket has seven slots.
            // The bucket starts at (key.Length - minLength) * 7.
            // Each value is an index of the key from _keys array
            // or just NullSentinel, which represents "null".
            int[] buckets = ArrayPool<int>.Shared.Rent(arraySize);
            buckets.AsSpan(0, arraySize).Fill(NullSentinel);

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

#if NET5_0_OR_GREATER
            Span<int> sortingBucket = stackalloc int[MaxPerLength];
#else
            int[] sortingBucket = new int[MaxPerLength];
#endif
            IndexedKeyComparer indexedKeyComparer = new IndexedKeyComparer(
                comparer == EqualityComparer<string>.Default
                ? StringComparer.Ordinal
                : (StringComparer)comparer, keys);
            for (int bucketStartIndex = 0; bucketStartIndex < arraySize; bucketStartIndex += MaxPerLength)
            {
                // Within the bucket we order the elements to make binary search easier.
                // The first element is the first to be compared and thus is the middle value.
                // e.g. 0,1,2,3,4,5,6 becomes 3,1,0,2,5,4,6
#if NET5_0_OR_GREATER
                buckets.AsSpan().Slice(bucketStartIndex, MaxPerLength).CopyTo(sortingBucket);
                int bucketLength = sortingBucket.IndexOf(NullSentinel);
#else
                Array.Copy(buckets, bucketStartIndex, sortingBucket, 0, MaxPerLength);
                int bucketLength = Array.IndexOf(sortingBucket, NullSentinel);
#endif

                if (bucketLength == -1)
                {
                    bucketLength = sortingBucket.Length;
                }

#if DEBUG
                // Assert our expectation that the buckets are filled sequentially
                for (int i = 0; i < MaxPerLength; i++)
                {
                    if (i < bucketLength)
                    {
                        Debug.Assert(sortingBucket[i] != NullSentinel);
                    }
                    else
                    {
                        Debug.Assert(sortingBucket[i] == NullSentinel);
                    }
                }
#endif


                if (bucketLength > 0)
                {
#if NET5_0_OR_GREATER
                    sortingBucket.Slice(0, bucketLength).Sort(indexedKeyComparer);
#else
                    Array.Sort(sortingBucket, 0, bucketLength, indexedKeyComparer);
#endif
                    int[] swapInstructions = s_sortedBucketToBinaryTree[bucketLength];

                    for (int i = 0; i < MaxPerLength; i++)
                    {
                        buckets[bucketStartIndex + i] = swapInstructions[i] switch
                        {
                            NullSentinel => NullSentinel,
                            < 0 => ~sortingBucket[~swapInstructions[i]],
                            _ => sortingBucket[swapInstructions[i]]
                        };
                    }
                }
            }

#if NET6_0_OR_GREATER
            // We don't need an array with every value initialized to zero if we are just about to overwrite every value anyway.
            int[] copy = GC.AllocateUninitializedArray<int>(arraySize);
            Array.Copy(buckets, copy, arraySize);
#else
            int[] copy = buckets.AsSpan(0, arraySize).ToArray();
#endif
            ArrayPool<int>.Shared.Return(buckets);

            return copy;
        }

        private class IndexedKeyComparer : IComparer<int>
        {
            private readonly IComparer<string> _keyComparer;
            private readonly string[] _keys;

            public IndexedKeyComparer(IComparer<string> keyComparer, string[] keys)
            {
                _keyComparer = keyComparer;
                _keys = keys;
            }

            public int Compare(int x, int y) => _keyComparer.Compare(_keys[x], _keys[y]);
        }
    }
}
