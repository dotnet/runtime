// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides the core hash table for use in frozen collections.</summary>
    /// <remarks>
    /// This hash table doesn't track any of the collection state. It merely keeps track
    /// of hash codes and of mapping these hash codes to spans of entries within the collection.
    /// </remarks>
    internal readonly struct FrozenHashTable
    {
        private readonly Bucket[] _buckets;
        private readonly ulong _fastModMultiplier;

        /// <summary>Initializes the hashtable with the computed hashcodes and bucket information.</summary>
        /// <param name="hashCodes">The array of hashcodes grouped into contiguous regions by bucket. Each bucket is one and only one region of the array.</param>
        /// <param name="buckets">
        /// The array of buckets, indexed by hashCodes % buckets.Length, where each bucket is
        /// the start/end index into <paramref name="hashCodes"/> for all items in that bucket.
        /// </param>
        /// <param name="fastModMultiplier">The multiplier to use as part of a FastMod method call.</param>
        private FrozenHashTable(int[] hashCodes, Bucket[] buckets, ulong fastModMultiplier)
        {
            Debug.Assert(hashCodes.Length != 0);
            Debug.Assert(buckets.Length != 0);

            HashCodes = hashCodes;
            _buckets = buckets;
            _fastModMultiplier = fastModMultiplier;
        }

        /// <summary>Initializes a frozen hash table.</summary>
        /// <param name="entriesLength">The number of entries to track from the hash table.</param>
        /// <param name="hashAtIndex">A delegate that produces a hash code for a given entry. It's passed the index of the entry and returns that entry's hash code.</param>
        /// <param name="storeDestIndexFromSrcIndex">A delegate that assigns the index to a specific entry. It's passed the destination and source indices.</param>
        /// <param name="optimizeForReading">true to spend additional effort tuning for subsequent read speed on the table; false to prioritize construction time.</param>
        /// <remarks>
        /// This method will iterate through the incoming entries and will invoke the hasher on each once.
        /// It will then determine the optimal number of hash buckets to allocate and will populate the
        /// bucket table. In the process of doing so, it calls out to the <paramref name="storeDestIndexFromSrcIndex"/> to indicate
        /// the resulting index for that entry. <see cref="FindMatchingEntries(int, out int, out int)"/>
        /// then uses this index to reference individual entries by indexing into <see cref="HashCodes"/>.
        /// </remarks>
        /// <returns>A frozen hash table.</returns>
        public static FrozenHashTable Create(int entriesLength, Func<int, int> hashAtIndex, Action<int, int> storeDestIndexFromSrcIndex, bool optimizeForReading = true)
        {
            Debug.Assert(entriesLength != 0);

            // Calculate the hashcodes for every entry.
            int[] arrayPoolHashCodes = ArrayPool<int>.Shared.Rent(entriesLength);
            Span<int> hashCodes = arrayPoolHashCodes.AsSpan(0, entriesLength);
            for (int i = 0; i < entriesLength; i++)
            {
                hashCodes[i] = hashAtIndex(i);
            }

            // Determine how many buckets to use.  This might be fewer than the number of entries
            // if any entries have identical hashcodes (not just different hashcodes that might
            // map to the same bucket).
            int numBuckets = CalcNumBuckets(hashCodes, optimizeForReading);
            ulong fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)numBuckets);

            // Create two spans:
            // - bucketStarts: initially filled with all -1s, the ith element stores the index
            //   into hashCodes of the head element of that bucket's chain.
            // - nexts: the ith element stores the index of the next item in the chain.
            int[] arrayPoolBuckets = ArrayPool<int>.Shared.Rent(numBuckets + hashCodes.Length);
            Span<int> bucketStarts = arrayPoolBuckets.AsSpan(0, numBuckets);
            Span<int> nexts = arrayPoolBuckets.AsSpan(numBuckets, hashCodes.Length);
            bucketStarts.Fill(-1);

            // Populate the bucket entries and starts.  For each hash code, compute its bucket,
            // and store at the bucket entry corresponding to the hashcode item the entry for that
            // item, which includes a copy of the hash code and the current bucket start, which
            // is then replaced by this entry as it's pushed into the bucket list.
            for (int index = 0; index < hashCodes.Length; index++)
            {
                int hashCode = hashCodes[index];
                int bucketNum = (int)HashHelpers.FastMod((uint)hashCode, (uint)bucketStarts.Length, fastModMultiplier);

                ref int bucketStart = ref bucketStarts[bucketNum];
                nexts[index] = bucketStart;
                bucketStart = index;
            }

            // Write out the hashcodes and buckets arrays to be used by the FrozenHashtable instance.
            // We iterate through each bucket start, and from each, each item in that chain, writing
            // out all of the items in each chain next to each other in the hashcodes list (and
            // calling the setter to allow the consumer to reorder its entries appropriately).
            // Along the way we could how many items are in each chain, and use that along with
            // the starting index to write out the bucket information for indexing into hashcodes.
            var hashtableHashcodes = new int[hashCodes.Length];
            var hashtableBuckets = new Bucket[bucketStarts.Length];
            int count = 0;
            for (int bucketNum = 0; bucketNum < hashtableBuckets.Length; bucketNum++)
            {
                int bucketStart = bucketStarts[bucketNum];
                if (bucketStart < 0)
                {
                    continue;
                }

                int bucketCount = 0;
                int index = bucketStart;
                bucketStart = count;
                while (index >= 0)
                {
                    hashtableHashcodes[count] = hashCodes[index];
                    storeDestIndexFromSrcIndex(count, index);
                    count++;
                    bucketCount++;

                    index = nexts[index];
                }

                hashtableBuckets[bucketNum] = new Bucket(bucketStart, bucketCount);
            }
            Debug.Assert(count == hashtableHashcodes.Length);

            ArrayPool<int>.Shared.Return(arrayPoolBuckets);
            ArrayPool<int>.Shared.Return(arrayPoolHashCodes);

            return new FrozenHashTable(hashtableHashcodes, hashtableBuckets, fastModMultiplier);
        }

        /// <summary>
        /// Given a hash code, return the first index and last index for the associated matching entries.
        /// </summary>
        /// <param name="hashCode">The hash code to probe for.</param>
        /// <param name="startIndex">A variable that receives the index of the first matching entry.</param>
        /// <param name="endIndex">A variable that receives the index of the last matching entry plus 1.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FindMatchingEntries(int hashCode, out int startIndex, out int endIndex)
        {
            Bucket[] buckets = _buckets;
            ref Bucket b = ref buckets[HashHelpers.FastMod((uint)hashCode, (uint)buckets.Length, _fastModMultiplier)];
            startIndex = b.StartIndex;
            endIndex = b.EndIndex;
        }

        public int Count => HashCodes.Length;

        internal int[] HashCodes { get; }

        /// <summary>
        /// Given a span of hash codes, figure out the best number of hash buckets to use.
        /// </summary>
        /// <remarks>
        /// This tries to select a prime number of buckets. Rather than iterating through all possible bucket
        /// sizes, starting at the exact number of hash codes and incrementing the bucket count by 1 per trial,
        /// this is a trade-off between speed of determining a good number of buckets and maximal density.
        /// </remarks>
        private static int CalcNumBuckets(ReadOnlySpan<int> hashCodes, bool optimizeForReading)
        {
            Debug.Assert(hashCodes.Length != 0);

            const double AcceptableCollisionRate = 0.05;  // What is a satisfactory rate of hash collisions?
            const int LargeInputSizeThreshold = 1000;     // What is the limit for an input to be considered "small"?
            const int MaxSmallBucketTableMultiplier = 16; // How large a bucket table should be allowed for small inputs?
            const int MaxLargeBucketTableMultiplier = 3;  // How large a bucket table should be allowed for large inputs?

            if (!optimizeForReading)
            {
                return HashHelpers.GetPrime(hashCodes.Length);
            }

            // Filter out duplicate codes, since no increase in buckets will avoid collisions from duplicate input hash codes.
            var codes =
#if NETCOREAPP2_0_OR_GREATER
                new HashSet<int>(hashCodes.Length);
#else
                new HashSet<int>();
#endif
            foreach (int hashCode in hashCodes)
            {
                codes.Add(hashCode);
            }
            Debug.Assert(codes.Count != 0);

            // In our precomputed primes table, find the index of the smallest prime that's at least as large as our number of
            // hash codes. If there are more codes than in our precomputed primes table, which accommodates millions of values,
            // give up and just use the next prime.
            ReadOnlySpan<int> primes = HashHelpers.Primes;
            int minPrimeIndexInclusive = 0;
            while ((uint)minPrimeIndexInclusive < (uint)primes.Length && codes.Count > primes[minPrimeIndexInclusive])
            {
                minPrimeIndexInclusive++;
            }

            if (minPrimeIndexInclusive >= primes.Length)
            {
                return HashHelpers.GetPrime(codes.Count);
            }

            // Determine the largest number of buckets we're willing to use, based on a multiple of the number of inputs.
            // For smaller inputs, we allow for a larger multiplier.
            int maxNumBuckets =
                codes.Count *
                (codes.Count >= LargeInputSizeThreshold ? MaxLargeBucketTableMultiplier : MaxSmallBucketTableMultiplier);

            // Find the index of the smallest prime that accommodates our max buckets.
            int maxPrimeIndexExclusive = minPrimeIndexInclusive;
            while ((uint)maxPrimeIndexExclusive < (uint)primes.Length && maxNumBuckets > primes[maxPrimeIndexExclusive])
            {
                maxPrimeIndexExclusive++;
            }

            if (maxPrimeIndexExclusive < primes.Length)
            {
                Debug.Assert(maxPrimeIndexExclusive != 0);
                maxNumBuckets = primes[maxPrimeIndexExclusive - 1];
            }

            const int BitsPerInt32 = 32;
            int[] seenBuckets = ArrayPool<int>.Shared.Rent((maxNumBuckets / BitsPerInt32) + 1);

            int bestNumBuckets = maxNumBuckets;
            int bestNumCollisions = codes.Count;

            // Iterate through each available prime between the min and max discovered. For each, compute
            // the collision ratio.
            for (int primeIndex = minPrimeIndexInclusive; primeIndex < maxPrimeIndexExclusive; primeIndex++)
            {
                // Get the number of buckets to try, and clear our seen bucket bitmap.
                int numBuckets = primes[primeIndex];
                Array.Clear(seenBuckets, 0, Math.Min(numBuckets, seenBuckets.Length));

                // Determine the bucket for each hash code and mark it as seen. If it was already seen,
                // track it as a collision.
                int numCollisions = 0;
                foreach (int code in codes)
                {
                    uint bucketNum = (uint)code % (uint)numBuckets;
                    if ((seenBuckets[bucketNum / BitsPerInt32] & (1 << (int)bucketNum)) != 0)
                    {
                        numCollisions++;
                        if (numCollisions >= bestNumCollisions)
                        {
                            // If we've already hit the previously known best number of collisions,
                            // there's no point in continuing as worst case we'd just use that.
                            break;
                        }
                    }
                    else
                    {
                        seenBuckets[bucketNum / BitsPerInt32] |= 1 << (int)bucketNum;
                    }
                }

                // If this evaluation resulted in fewer collisions, use it as the best instead.
                // And if it's below our collision threshold, we're done.
                if (numCollisions < bestNumCollisions)
                {
                    bestNumBuckets = numBuckets;

                    if (numCollisions / (double)codes.Count <= AcceptableCollisionRate)
                    {
                        break;
                    }

                    bestNumCollisions = numCollisions;
                }
            }

            ArrayPool<int>.Shared.Return(seenBuckets);

            return bestNumBuckets;
        }

        private readonly struct Bucket
        {
            public readonly int StartIndex;
            public readonly int EndIndex;

            public Bucket(int startIndex, int count)
            {
                Debug.Assert(count > 0);

                StartIndex = startIndex;
                EndIndex = startIndex + count - 1;
            }
        }
    }
}
