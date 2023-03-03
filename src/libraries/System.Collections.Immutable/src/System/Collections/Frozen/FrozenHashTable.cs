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

        private FrozenHashTable(int[] hashCodes, Bucket[] buckets, ulong fastModMultiplier)
        {
            Debug.Assert(hashCodes.Length != 0);
            Debug.Assert(buckets.Length != 0);

            HashCodes = hashCodes;
            _buckets = buckets;
            _fastModMultiplier = fastModMultiplier;
        }

        /// <summary>Initializes a frozen hash table.</summary>
        /// <param name="entries">The set of entries to track from the hash table.</param>
        /// <param name="hasher">A delegate that produces a hash code for a given entry.</param>
        /// <param name="setter">A delegate that assigns the index to a specific entry.</param>
        /// <typeparam name="T">The type of elements in the hash table.</typeparam>
        /// <remarks>
        /// This method will iterate through the incoming entries and will invoke the hasher on each once.
        /// It will then determine the optimal number of hash buckets to allocate and will populate the
        /// bucket table. In the process of doing so, it calls out to the <paramref name="setter"/> to indicate
        /// the resulting index for that entry. <see cref="FindMatchingEntries(int, out int, out int)"/>
        /// then uses this index to reference individual entries by indexing into <see cref="HashCodes"/>.
        /// </remarks>
        /// <returns>A frozen hash table.</returns>
        public static FrozenHashTable Create<T>(T[] entries, Func<T, int> hasher, Action<int, T> setter)
        {
            Debug.Assert(entries.Length != 0);

            int[] hashCodes = new int[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                hashCodes[i] = hasher(entries[i]);
            }

            int numBuckets = CalcNumBuckets(hashCodes);
            ulong fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)numBuckets);

            var chainBuddies = new Dictionary<uint, List<ChainBuddy>>();
            for (int index = 0; index < hashCodes.Length; index++)
            {
                int hashCode = hashCodes[index];
                uint bucket = HashHelpers.FastMod((uint)hashCode, (uint)numBuckets, fastModMultiplier);

                if (!chainBuddies.TryGetValue(bucket, out List<ChainBuddy>? list))
                {
                    chainBuddies[bucket] = list = new List<ChainBuddy>();
                }

                list.Add(new ChainBuddy(hashCode, index));
            }

            var buckets = new Bucket[numBuckets];

            int count = 0;
            foreach (List<ChainBuddy> list in chainBuddies.Values)
            {
                uint bucket = HashHelpers.FastMod((uint)list[0].HashCode, (uint)buckets.Length, fastModMultiplier);

                buckets[bucket] = new Bucket(count, list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    hashCodes[count] = list[i].HashCode;
                    setter(count, entries[list[i].Index]);
                    count++;
                }
            }

            return new FrozenHashTable(hashCodes, buckets, fastModMultiplier);
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
            ref Bucket b = ref _buckets[HashHelpers.FastMod((uint)hashCode, (uint)_buckets.Length, _fastModMultiplier)];
            startIndex = b.StartIndex;
            endIndex = b.EndIndex;
        }

        public int Count => HashCodes.Length;

        internal int[] HashCodes { get; }

        /// <summary>
        /// Given an array of hash codes, figure out the best number of hash buckets to use.
        /// </summary>
        /// <remarks>
        /// This tries to select a prime number of buckets. Rather than iterating through all possible bucket
        /// sizes, starting at the exact number of hash codes and incrementing the bucket count by 1 per trial,
        /// this is a trade-off between speed of determining a good number of buckets and maximumal density.
        /// </remarks>
        private static int CalcNumBuckets(int[] hashCodes)
        {
            const double AcceptableCollisionRate = 0.05;  // What is a satifactory rate of hash collisions?
            const int LargeInputSizeThreshold = 1000;     // What is the limit for an input to be considered "small"?
            const int MaxSmallBucketTableMultiplier = 16; // How large a bucket table should be allowed for small inputs?
            const int MaxLargeBucketTableMultiplier = 3;  // How large a bucket table should be allowed for large inputs?

            // Filter out duplicate codes, since no increase in buckets will avoid collisions from duplicate input hash codes.
            var codes = new HashSet<int>(hashCodes);
            Debug.Assert(codes.Count != 0);

            // In our precomputed primes table, find the index of the smallest prime that's at least as large as our number of
            // hash codes. If there are more codes than in our precomputed primes table, which accomodates millions of values,
            // give up and just use the next prime.
            int minPrimeIndexInclusive = 0;
            while (minPrimeIndexInclusive < HashHelpers.s_primes.Length && codes.Count > HashHelpers.s_primes[minPrimeIndexInclusive])
            {
                minPrimeIndexInclusive++;
            }
            if (minPrimeIndexInclusive >= HashHelpers.s_primes.Length)
            {
                return HashHelpers.GetPrime(codes.Count);
            }

            // Determine the largest number of buckets we're willing to use, based on a multiple of the number of inputs.
            // For smaller inputs, we allow for a larger multiplier.
            int maxNumBuckets =
                codes.Count *
                (codes.Count >= LargeInputSizeThreshold ? MaxLargeBucketTableMultiplier : MaxSmallBucketTableMultiplier);

            // Find the index of the smallest prime that accomodates our max buckets.
            int maxPrimeIndexExclusive = minPrimeIndexInclusive;
            while (maxPrimeIndexExclusive < HashHelpers.s_primes.Length && maxNumBuckets > HashHelpers.s_primes[maxPrimeIndexExclusive])
            {
                maxPrimeIndexExclusive++;
            }
            if (maxPrimeIndexExclusive < HashHelpers.s_primes.Length)
            {
                Debug.Assert(maxPrimeIndexExclusive != 0);
                maxNumBuckets = HashHelpers.s_primes[maxPrimeIndexExclusive - 1];
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
                int numBuckets = HashHelpers.s_primes[primeIndex];
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

        private readonly struct ChainBuddy
        {
            public readonly int HashCode;
            public readonly int Index;

            public ChainBuddy(int hashCode, int index)
            {
                HashCode = hashCode;
                Index = index;
            }
        }

        private readonly struct Bucket
        {
            public readonly int StartIndex;
            public readonly int EndIndex;

            public Bucket(int index, int count)
            {
                Debug.Assert(count > 0);

                StartIndex = index;
                EndIndex = index + count - 1;
            }
        }
    }
}
