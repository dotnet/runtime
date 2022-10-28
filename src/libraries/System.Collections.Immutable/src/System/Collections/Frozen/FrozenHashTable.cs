// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A hash table for frozen collections.
    /// </summary>
    /// <remarks>
    /// Frozen collections are immutable and are optimized for situations where a collection
    /// is created infrequently, but used repeatedly at runtime. They have a relatively high
    /// cost to create, but provide excellent lookup performance. These are thus ideal for cases
    /// where the collection is created at startup of an application and used throughout the life
    /// of the application.
    ///
    /// This hash table doesn't track any of the collection state. It merely keeps track of hash codes
    /// and of mapping these hash codes to spans of entries within the collection.
    /// </remarks>
    internal readonly struct FrozenHashTable
    {
        private static readonly Bucket[] _emptyBuckets = GetEmptyBuckets();

        private readonly Bucket[] _buckets;
        private readonly ulong _fastModMultiplier;

        private FrozenHashTable(int[] hashCodes, Bucket[] buckets, ulong fastModMultiplier)
        {
            HashCodes = hashCodes;
            _buckets = buckets;
            _fastModMultiplier = fastModMultiplier;
        }

        /// <summary>
        /// Initializes a frozen hash table.
        /// </summary>
        /// <param name="entries">The set of entries to track from the hash table.</param>
        /// <param name="hasher">A delegate that produces a hash code for a given entry.</param>
        /// <param name="setter">A delegate that assigns the index to a specific entry.</param>
        /// <typeparam name="T">The type of elements in the hash table.</typeparam>
        /// <exception cref="ArgumentException">If more than 64K pairs are added.</exception>
        /// <remarks>
        /// This method will iterate through the incoming entries and will invoke the hasher on each once.
        /// It will then determine the optimal number of hash buckets to allocate and will populate the
        /// bucket table. In the process of doing so, it calls out to the <paramref name="setter"/> to indicate
        /// the resulting index for that entry. The <see cref="FindMatchingEntries(int, out int, out int)"/> and <see cref="EntryHashCode(int)"/>
        /// then use this index to reference individual entries.
        /// </remarks>
        /// <returns>A frozen hash table.</returns>
        public static FrozenHashTable Create<T>(T[] entries, Func<T, int> hasher, Action<int, T> setter)
        {
            int numEntries = entries.Length;

            int[] hashCodes;
            Bucket[] buckets;
            ulong fastModMultiplier;

            if (numEntries == 0)
            {
                hashCodes = Array.Empty<int>();
                buckets = _emptyBuckets;
                fastModMultiplier = GetFastModMultiplier(1);
            }
            else
            {
                hashCodes = new int[numEntries];
                for (int i = 0; i < numEntries; i++)
                {
                    hashCodes[i] = hasher(entries[i]);
                }

                int numBuckets = CalcNumBuckets(hashCodes);
                fastModMultiplier = GetFastModMultiplier((uint)numBuckets);

                var chainBuddies = new Dictionary<uint, List<ChainBuddy>>();
                for (int index = 0; index < numEntries; index++)
                {
                    int hashCode = hashCodes[index];
                    uint bucket = FastMod((uint)hashCode, (uint)numBuckets, fastModMultiplier);

                    if (!chainBuddies.TryGetValue(bucket, out List<ChainBuddy>? list))
                    {
                        list = new List<ChainBuddy>();
                        chainBuddies[bucket] = list;
                    }

                    list.Add(new ChainBuddy(hashCode, index));
                }

                buckets = new Bucket[numBuckets];

                int count = 0;
                foreach (List<ChainBuddy> list in chainBuddies.Values)
                {
                    uint bucket = FastMod((uint)list[0].HashCode, (uint)buckets.Length, fastModMultiplier);

                    buckets[bucket] = new Bucket(count, list.Count);
                    for (int i = 0; i < list.Count; i++)
                    {
                        hashCodes[count] = list[i].HashCode;
                        setter(count, entries[list[i].Index]);
                        count++;
                    }
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
            ref Bucket b = ref _buckets[FastMod((uint)hashCode, (uint)_buckets.Length, _fastModMultiplier)];
            startIndex = b.StartIndex;
            endIndex = b.EndIndex;
        }

        public int Count => HashCodes.Length;

        /// <summary>
        /// Given an entry, get its hash code.
        /// </summary>
        /// <param name="entry">The entry index to probe.</param>
        /// <returns>The hash code belonging to this entry.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EntryHashCode(int entry) => HashCodes[entry];

        internal int[] HashCodes { get; }

        private static Bucket[] GetEmptyBuckets()
        {
            var buckets = new Bucket[1];
            buckets[0] = new Bucket(1, 0);
            return buckets;
        }

        /// <summary>
        /// Given an array of hash codes, figure out the best number of hash buckets to use.
        /// </summary>
        /// <remarks>
        /// At the moment, this is pretty dumb. It sits in a loop trying a large number of hash buckets
        /// and seeing how many collisions there are. It stops when there are less than 5% collisions.
        /// For a large table with a lot of collisions, this could take an awful long time. Is there a
        /// better strategy possible here? For example, perhaps we should first try to scale the bucket count
        /// to prime values.
        /// </remarks>
        private static int CalcNumBuckets(int[] hashCodes)
        {
            // how big of a bucket table to allow for small inputs
            const int MaxSmallBucketTableMultiplier = 16;

            // how big of a bucket table to allow for large inputs
            const int MaxLargeBucketTableMultiplier = 3;

            // what is the limit for a small input?
            const int LargeInputSize = 1000;

            // what is the satifactory rate of hash collisions?
            const double AcceptableCollisionRate = 0.05;

            // filter out duplicate codes
            var codesSet = new HashSet<int>(hashCodes);
            int[] codes = new int[codesSet.Count];
            codesSet.CopyTo(codes);

            int maxNumBuckets = (codes.Length >= LargeInputSize) ? codes.Length * MaxLargeBucketTableMultiplier : codes.Length * MaxSmallBucketTableMultiplier;
            var buckets = new BitArray(maxNumBuckets);

            int bestNumBuckets = maxNumBuckets;
            int bestNumCollisions = codes.Length;
            for (int numBuckets = codes.Length; numBuckets < maxNumBuckets; numBuckets++)
            {
                buckets.SetAll(false);

                int numCollisions = 0;
                foreach (int code in codes)
                {
                    uint bucketNum = (uint)code % (uint)numBuckets;
                    if (buckets[(int)bucketNum])
                    {
                        numCollisions++;
                        if (numCollisions >= bestNumCollisions)
                        {
                            // no sense in continuing, we already have more collisions than the best so far
                            break;
                        }
                    }
                    else
                    {
                        buckets[(int)bucketNum] = true;
                    }
                }

                if (numCollisions < bestNumCollisions)
                {
                    bestNumBuckets = numBuckets;

                    if (numCollisions / (double)codes.Length <= AcceptableCollisionRate)
                    {
                        break;
                    }

                    bestNumCollisions = numCollisions;
                }
            }

            return bestNumBuckets;
        }

        /// <summary>Returns approximate reciprocal of the divisor: ceil(2**64 / divisor).</summary>
        private static ulong GetFastModMultiplier(uint divisor) => (ulong.MaxValue / divisor) + 1;

        /// <summary>Performs a mod operation using the multiplier pre-computed with <see cref="GetFastModMultiplier"/>.</summary>
        /// <remarks>This should only be used on 64-bit architectures.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint FastMod(uint value, uint divisor, ulong multiplier)
        {
            // Modification of https://github.com/dotnet/runtime/pull/406,
            // which allows to avoid the long multiplication if the divisor is less than 2**31.
            return (uint)(((((multiplier * value) >> 32) + 1) * divisor) >> 32);
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
                if (count == 0)
                {
                    StartIndex = 1;
                    EndIndex = 0;
                }
                else
                {
                    StartIndex = index;
                    EndIndex = index + count - 1;
                }
            }
        }
    }
}
