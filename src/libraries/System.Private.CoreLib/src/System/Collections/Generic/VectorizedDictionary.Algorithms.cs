// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Force disables the vectorized suffix search implementations so you can test/benchmark the scalar one
// #define FORCE_SCALAR_IMPLEMENTATION

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace System.Collections.Generic {
    public partial class Dictionary<TKey, TValue> {
        // Extracting all this logic into each caller improves codegen slightly + reduces code size slightly, but the
        //  duplication reduces maintainability, so I'm pretty happy doing this instead.
        // We rely on inlining to cause this struct to completely disappear, and its fields to become registers or individual locals.

        // Will never fail as long as buckets isn't 0-length. You don't need to call Advance before your first loop iteration.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref Bucket NewEnumerator (uint hashCode, out LoopingBucketEnumerator result) {
            Unsafe.SkipInit(out result);
            var buckets = new Span<Bucket>(_Buckets);
            var initialIndex = BucketIndexForHashCode(hashCode, buckets);
            Debug.Assert(buckets.Length > 0);

            // This is calculated by BucketIndexForHashCode, so it won't be out of range, but it's possible FastMod is broken if
            //  you concurrently resize the container, so have span bounds-check it for us.
            ref var initialBucket = ref buckets[initialIndex];
            result.buckets = buckets;
            result.index = result.initialIndex = initialIndex;
            return ref initialBucket;
        }

        private ref struct LoopingBucketEnumerator {
            // The size of this struct is REALLY important! Adding even a single field to this will add stack spills to critical loops.
            // FIXME: This span being a field puts pressure on the JIT to do recursive struct decomposition; I'm not sure it always does
            internal Span<Bucket> buckets;
            internal int index, initialIndex;

            [Obsolete("Use Dictionary.NewEnumerator")]
            public LoopingBucketEnumerator () {
            }

            /// <summary>
            /// Walks forward through buckets, wrapping around at the end of the container.
            /// Never visits a bucket twice.
            /// </summary>
            /// <returns>The next bucket, or NullRef if you have visited every bucket exactly once.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref Bucket Advance () {
                // Operating on the index field directly is harmless as long as the enumerator struct got decomposed, which it seems to
                // Caching index into a local and then doing a writeback at the end increases generated code size so it's not worth it
                if (++index >= buckets.Length)
                    index = 0;

                if (index == initialIndex)
                    return ref Unsafe.NullRef<Bucket>();
                else
                    return ref buckets[index];
            }

            /// <summary>
            /// Walks back through the buckets you have previously visited.
            /// </summary>
            /// <returns>Each bucket you previously visited, exactly once, in reverse order, then NullRef.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref Bucket Retreat () {
                if (index == initialIndex)
                    return ref Unsafe.NullRef<Bucket>();

                if (--index < 0)
                    index = buckets.Length - 1;
                return ref buckets[index];
            }

            /// <summary>
            /// Indicates whether the enumerator has moved away from its original location and retreating is possible.
            /// </summary>
            public bool HasMoved {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => index != initialIndex;
            }
        }

        /// <summary>
        /// Visits every bucket in the container exactly once.
        /// </summary>
        // Callback is passed by-ref so it can be used to store results from the enumeration operation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnumerateBuckets<TCallback> (Span<Bucket> buckets, ref TCallback callback)
            where TCallback : struct, IBucketCallback {
            // FIXME: Using a foreach on this span produces an imul-per-iteration for some reason.
            ref Bucket bucket = ref MemoryMarshal.GetReference(buckets),
                lastBucket = ref Unsafe.Add(ref bucket, buckets.Length - 1);

            while (true) {
                var ok = callback.Bucket(ref bucket);
                if (ok && !Unsafe.AreSame(ref bucket, ref lastBucket))
                    bucket = ref Unsafe.Add(ref bucket, 1);
                else
                    break;
            }
        }

        /// <summary>
        /// Visits every key/value pair in the container exactly once.
        /// </summary>
        // Callback is passed by-ref so it can be used to store results from the enumeration operation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnumeratePairs<TCallback> (Span<Bucket> buckets, ref TCallback callback)
            where TCallback : struct, IPairCallback {
            // FIXME: Using a foreach on this span produces an imul-per-iteration for some reason.
            ref Bucket bucket = ref MemoryMarshal.GetReference(buckets),
                lastBucket = ref Unsafe.Add(ref bucket, buckets.Length - 1);

            while (true) {
                ref var pair = ref bucket.Pairs.Pair0;

                // FIXME: Awkward construction to prevent pair from ever becoming an invalid reference for a full bucket
                int i = 0, c = bucket.Count;
                if (i < c) {
iteration:
                    if (!callback.Pair(ref pair))
                        return;
                    if (++i < c) {
                        pair = ref Unsafe.Add(ref pair, 1);
                        goto iteration;
                    }
                }

                if (!Unsafe.AreSame(ref bucket, ref lastBucket))
                    bucket = ref Unsafe.Add(ref bucket, 1);
                else
                    return;
            }
        }

        /// <summary>
        /// Scans the suffix table for the bucket for suffixes that match the provided search vector.
        /// </summary>
        /// <param name="bucket">The bucket to scan.</param>
        /// <param name="searchVector">A search vector (all bytes must be the desired suffix)</param>
        /// <param name="bucketCount">bucket.Count</param>
        /// <returns>The location of the first match, or 32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int FindSuffixInBucket (ref Bucket bucket, Vector128<byte> searchVector, int bucketCount) {
#if !FORCE_SCALAR_IMPLEMENTATION
            if (Sse2.IsSupported) {
                return BitOperations.TrailingZeroCount(Sse2.MoveMask(Sse2.CompareEqual(searchVector, bucket.Suffixes)));
            } else if (AdvSimd.Arm64.IsSupported) {
                // Completely untested
                var laneBits = AdvSimd.And(
                    AdvSimd.CompareEqual(searchVector, bucket.Suffixes),
                    Vector128.Create(1, 2, 4, 8, 16, 32, 64, 128, 1, 2, 4, 8, 16, 32, 64, 128)
                );
                var moveMask = AdvSimd.Arm64.AddAcross(laneBits.GetLower()).ToScalar() |
                    (AdvSimd.Arm64.AddAcross(laneBits.GetUpper()).ToScalar() << 8);
                return BitOperations.TrailingZeroCount(moveMask);
            } else if (PackedSimd.IsSupported) {
                // Completely untested
                return BitOperations.TrailingZeroCount(PackedSimd.Bitmask(PackedSimd.CompareEqual(searchVector, bucket.Suffixes)));
            } else {
#else
            {
#endif
#if FALSE
                // Hand-unrolled scan of multiple bytes at a time. If a bucket contains 9 or more items, we will erroneously
                //  check lanes 15 and 16 (which contain the count and cascade count), but finding a false match there is harmless
                // We could do this 4 bytes at a time instead, but that isn't actually faster
                // This produces larger code than a chain of ifs.
                var wideHaystack = (UInt64*)Unsafe.AsPointer(ref bucket);
                for (int i = 0; i < bucketCount; i += 8, wideHaystack += 1) {
                    // Doing a xor this way basically performs a vectorized compare of all the lanes, and we can test the result with
                    //  a == 0 check on the low 8 bits, which is a single 'test rNNb' instruction on x86/x64
                    var matchMask = *wideHaystack ^ searchVector.AsUInt64()[0];
                    if (Step(ref matchMask))
                        return i;
                    if (Step(ref matchMask))
                        return i + 1;
                    if (Step(ref matchMask))
                        return i + 2;
                    if (Step(ref matchMask))
                        return i + 3;
                    if (Step(ref matchMask))
                        return i + 4;
                    if (Step(ref matchMask))
                        return i + 5;
                    if (Step(ref matchMask))
                        return i + 6;
                    if (Step(ref matchMask))
                        return i + 7;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static bool Step (ref UInt64 matchMask) {
                    if ((matchMask & 0xFF) == 0)
                        return true;
                    matchMask >>= 8;
                    return false;
                }

#elif ALSO_FALSE
                var haystack = (byte*)Unsafe.AsPointer(ref bucket);
                for (int i = 0; i < bucketCount; i++, haystack++)
                    if (*haystack == searchVector[0])
                        return i;
#else
                // Hand-unrolling the search into four comparisons per loop iteration is a significant performance improvement
                //  for a moderate code size penalty (733b -> 826b; 399usec -> 321usec, vs BCL's 421b and 270usec)
                // If a bucket contains 13 or more items we will erroneously check lanes 14/15/16 but this is harmless.
                var haystack = (byte*)Unsafe.AsPointer(ref bucket);
                for (int i = 0; i < bucketCount; i += 4, haystack += 4) {
                    if (haystack[0] == searchVector[0])
                        return i;
                    if (haystack[1] == searchVector[0])
                        return i + 1;
                    if (haystack[2] == searchVector[0])
                        return i + 2;
                    if (haystack[3] == searchVector[0])
                        return i + 3;
                }
#endif

                return 32;
            }
        }

        /// <summary>
        /// Walks backwards through previously-visited buckets, adjusting their cascade counter upward or downward.
        /// </summary>
        /// <param name="enumerator">The enumerator that was used to visit buckets.</param>
        /// <param name="increase">true to increase cascade counts (you added something), false to decrease (you removed something).</param>
        // In the common case this method never runs, but inlining allows some smart stuff to happen in terms of stack size and
        //  register usage.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AdjustCascadeCounts (
            LoopingBucketEnumerator enumerator, bool increase
        ) {
            // Early-out before doing setup work since in the common case we won't have cascaded out of a bucket at all
            if (!enumerator.HasMoved)
                return;

            // We may have cascaded out of a previous bucket; if so, scan backwards and update
            //  the cascade count for every bucket we previously scanned.
            ref Bucket bucket = ref enumerator.Retreat();
            while (!Unsafe.IsNullRef(ref bucket)) {
                // FIXME: Track number of times we cascade out of a bucket for string rehashing anti-DoS mitigation!
                var cascadeCount = bucket.CascadeCount;
                if (increase) {
                    // Never overflow (wrap around) the counter
                    if (cascadeCount < DegradedCascadeCount)
                        bucket.CascadeCount = (ushort)(cascadeCount + 1);
                } else {
                    if (cascadeCount == 0)
                        ThrowCorrupted();
                    // If the cascade counter hit the maximum, it's possible the actual cascade count through here is higher,
                    //  so it's no longer safe to decrement. This is a very rare scenario, but it permanently degrades the table.
                    // TODO: Track this and trigger a rehash once too many buckets are in this state + dict is mostly empty.
                    else if (cascadeCount < DegradedCascadeCount)
                        bucket.CascadeCount = (ushort)(cascadeCount - 1);
                }

                bucket = ref enumerator.Retreat();
            }
        }

#pragma warning disable CS8619
        // These have to be structs so that the JIT will specialize callers instead of Canonizing them
        private struct DefaultComparerKeySearcher : IKeySearcher {

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint GetHashCode (IEqualityComparer<TKey>? comparer, TKey key) {
                return FinalizeHashCode(unchecked((uint)EqualityComparer<TKey>.Default.GetHashCode(key!)));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe ref Pair FindKeyInBucket (
                // We have to use UnscopedRef to allow lazy initialization
                [UnscopedRef] ref Bucket bucket, int indexInBucket, int bucketCount,
                IEqualityComparer<TKey>? comparer, TKey needle, out int matchIndexInBucket
            ) {
                Unsafe.SkipInit(out matchIndexInBucket);
                Debug.Assert(indexInBucket >= 0);

                int count = bucketCount - indexInBucket;
                if (count <= 0)
                    return ref Unsafe.NullRef<Pair>();

                ref Pair pair = ref Unsafe.Add(ref bucket.Pairs.Pair0, indexInBucket);
                while (true) {
                    if (EqualityComparer<TKey>.Default.Equals(needle, pair.Key)) {
                        // We could optimize out the bucketCount local to prevent a stack spill in some cases by doing
                        //  Unsafe.ByteOffset(...) / sizeof(Pair), but the potential idiv is extremely painful
                        matchIndexInBucket = bucketCount - count;
                        return ref pair;
                    }

                    // NOTE: --count <= 0 produces an extra 'test' opcode
                    if (--count == 0)
                        return ref Unsafe.NullRef<Pair>();
                    else
                        pair = ref Unsafe.Add(ref pair, 1);
                }
            }
        }

        private struct ComparerKeySearcher : IKeySearcher {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static uint GetHashCode (IEqualityComparer<TKey>? comparer, TKey key) {
                return FinalizeHashCode(unchecked((uint)comparer!.GetHashCode(key!)));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe ref Pair FindKeyInBucket (
                // We have to use UnscopedRef to allow lazy initialization
                [UnscopedRef] ref Bucket bucket, int indexInBucket, int bucketCount,
                IEqualityComparer<TKey>? comparer, TKey needle, out int matchIndexInBucket
            ) {
                Unsafe.SkipInit(out matchIndexInBucket);
                Debug.Assert(indexInBucket >= 0);
                Debug.Assert(comparer != null);

                int count = bucketCount - indexInBucket;
                if (count <= 0)
                    return ref Unsafe.NullRef<Pair>();

                ref Pair pair = ref Unsafe.Add(ref bucket.Pairs.Pair0, indexInBucket);
                // FIXME: This loop spills two values to/from the stack every iteration, and it's not clear which.
                // The ValueType-with-default-comparer one doesn't.
                while (true) {
                    if (comparer.Equals(needle, pair.Key)) {
                        // We could optimize out the bucketCount local to prevent a stack spill in some cases by doing
                        //  Unsafe.ByteOffset(...) / sizeof(Pair), but the potential idiv is extremely painful
                        matchIndexInBucket = bucketCount - count;
                        return ref pair;
                    }

                    // NOTE: --count <= 0 produces an extra 'test' opcode
                    if (--count == 0)
                        return ref Unsafe.NullRef<Pair>();
                    else
                        pair = ref Unsafe.Add(ref pair, 1);
                }
            }
        }
#pragma warning restore CS8619
    }
}
