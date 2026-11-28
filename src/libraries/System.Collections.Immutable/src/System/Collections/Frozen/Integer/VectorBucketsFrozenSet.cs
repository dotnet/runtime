// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Collections.Frozen
{
    /// <summary>Provides initialization methods for instances of the <see cref="VectorBucketsFrozenSet{T, TUnsigned}"/> class.</summary>
    internal static class VectorBucketsFrozenSet
    {
        /// <summary>The maximum number of slots per item allowed when looking for a hash function that doesn't overflow any bucket.</summary>
        private const int MaxSlotsPerItem = 8;

        /// <summary>The odd multipliers tried, in order, as hash functions.</summary>
        /// <remarks>When values are hashed with 32-bit arithmetic, only the lower 32 bits of the multipliers are used, which are odd too.</remarks>
        private static ReadOnlySpan<ulong> Multipliers =>
        [
            0x9E3779B97F4A7C15, 0xBF58476D1CE4E5B9, 0x94D049BB133111EB, 0xD6E8FEB86659FD93,
            0xFF51AFD7ED558CCD, 0xC4CEB9FE1A85EC53, 0x2545F4914F6CDD1D, 0xA0761D6478BD642F,
        ];

        /// <summary>Creates a set for the specified source, if <typeparamref name="T"/> and the values are supported.</summary>
        public static FrozenSet<T>? CreateIfValid<T>(HashSet<T> source)
        {
            Debug.Assert(source.Count != 0);
            Debug.Assert(ReferenceEquals(source.Comparer, EqualityComparer<T>.Default));

            if (Vector128.IsHardwareAccelerated &&
                source.Count <= Constants.MaxItemsInVectorBucketsFrozenCollection &&
                Constants.IsIntegralOrEnum<T>())
            {
                // Single-byte values aren't handled: there are at most 256 of them, so if they don't all fit in a single
                // vector, they're always dense enough for DenseIntegralFrozenSet.
                switch (Unsafe.SizeOf<T>())
                {
                    case sizeof(ushort): return CreateIfValid<T, ushort>(source);
                    case sizeof(uint): return CreateIfValid<T, uint>(source);
                    case sizeof(ulong): return CreateIfValid<T, ulong>(source);
                }
            }

            return null;
        }

        private static FrozenSet<T>? CreateIfValid<T, TUnsigned>(HashSet<T> source)
            where TUnsigned : unmanaged, IBinaryInteger<TUnsigned>, IUnsignedNumber<TUnsigned>
        {
            Debug.Assert(Unsafe.SizeOf<T>() == Unsafe.SizeOf<TUnsigned>());

            int bucketCapacity = Vector128<TUnsigned>.Count;
            int count = source.Count;

            var values = new TUnsigned[count];
            int i = 0;
            foreach (T item in source)
            {
                values[i++] = Unsafe.BitCast<T, TUnsigned>(item);
            }

            // Start with a 50% load factor (and at least 2 buckets, so that the shift is less than the number of bits
            // hashed), and double the number of buckets until a multiplier is found that doesn't overflow any bucket,
            // giving up once there would be more than MaxSlotsPerItem slots per item.
            int bucketCountLog2 = 1;
            while ((bucketCapacity << bucketCountLog2) < 2 * count)
            {
                bucketCountLog2++;
            }

            for (; (bucketCapacity << bucketCountLog2) <= Math.Max(count * MaxSlotsPerItem, 2 * bucketCapacity); bucketCountLog2++)
            {
                byte[] bucketCounts = new byte[1 << bucketCountLog2];
                int shift = GetShift<TUnsigned>(bucketCountLog2);
                foreach (ulong multiplier in Multipliers)
                {
                    if (TryCountBucketItems(values, multiplier, shift, bucketCounts, bucketCapacity))
                    {
                        return new VectorBucketsFrozenSet<T, TUnsigned>(values, bucketCounts, multiplier);
                    }
                }
            }

            return null;
        }

        /// <summary>Counts the number of values in each bucket, failing if a bucket would contain more than <paramref name="bucketCapacity"/> values.</summary>
        private static bool TryCountBucketItems<TUnsigned>(TUnsigned[] values, ulong multiplier, int shift, byte[] bucketCounts, int bucketCapacity)
            where TUnsigned : unmanaged, IBinaryInteger<TUnsigned>, IUnsignedNumber<TUnsigned>
        {
            Array.Clear(bucketCounts);
            foreach (TUnsigned value in values)
            {
                if (++bucketCounts[GetBucket(value, multiplier, shift)] > bucketCapacity)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets whether values of type <typeparamref name="TUnsigned"/> are hashed with 32-bit arithmetic: on 32-bit platforms,
        /// for values of up to 32 bits, as 64-bit multiplications and shifts need helper calls there.
        /// </summary>
        private static bool Hash32<TUnsigned>() => IntPtr.Size == 4 && Unsafe.SizeOf<TUnsigned>() <= sizeof(uint);

        /// <summary>Gets the shift of the hash function for the specified base-2 log of the number of buckets.</summary>
        /// <remarks>It leaves exactly <paramref name="bucketCountLog2"/> bits of the hash, so that every bucket index is in range.</remarks>
        internal static int GetShift<TUnsigned>(int bucketCountLog2)
            where TUnsigned : unmanaged, IBinaryInteger<TUnsigned>, IUnsignedNumber<TUnsigned>
        {
            Debug.Assert(bucketCountLog2 is > 0 and < 32);
            return (Hash32<TUnsigned>() ? 32 : 64) - bucketCountLog2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetBucket<TUnsigned>(TUnsigned value, ulong multiplier, int shift)
            where TUnsigned : unmanaged, IBinaryInteger<TUnsigned>, IUnsignedNumber<TUnsigned> =>
            Hash32<TUnsigned>() ?
                (int)((uint.CreateTruncating(value) * (uint)multiplier) >> shift) :
                (int)((ulong.CreateTruncating(value) * multiplier) >> shift);

        internal sealed class DebuggerProxy<T, TUnsigned>(IEnumerable<T> enumerable) : ImmutableEnumerableDebuggerProxy<T>(enumerable);
    }

    /// <summary>
    /// Provides a <see cref="FrozenSet{T}"/> for a small number of integral values (or enums), stored in a hash table
    /// whose buckets are each searched with a single vector comparison.
    /// </summary>
    /// <typeparam name="T">The type of the values in the set.</typeparam>
    /// <typeparam name="TUnsigned">The unsigned integer type with the same size as <typeparamref name="T"/>, as which the values are hashed and compared.</typeparam>
    /// <remarks>
    /// Each bucket holds a <see cref="Vector128{T}"/> worth of values, e.g. 4 values for <see cref="int"/>. As the set
    /// is immutable, the number of buckets and the hash function are chosen at construction such that no bucket overflows,
    /// so a lookup never needs to look at more than one bucket: it's a multiplication, a shift, and a single vector
    /// comparison, without any data-dependent loop or branch.
    /// For the types supported, equality as defined by <see cref="EqualityComparer{T}.Default"/> is bitwise equality, so
    /// values are stored and compared as the unsigned integer type of the same size, which they're reinterpreted as with
    /// <see cref="Unsafe.BitCast{TFrom, TTo}"/>: both types are integers (or enums) without references, and of the same
    /// size, which <see cref="Unsafe.BitCast{TFrom, TTo}"/> validates too.
    /// </remarks>
    [DebuggerTypeProxy(typeof(VectorBucketsFrozenSet.DebuggerProxy<,>))]
    internal sealed class VectorBucketsFrozenSet<T, TUnsigned> : FrozenSetInternalBase<T, VectorBucketsFrozenSet<T, TUnsigned>.GSW>
        where TUnsigned : unmanaged, IBinaryInteger<TUnsigned>, IUnsignedNumber<TUnsigned>
    {
        /// <summary>The items in the set, grouped by bucket.</summary>
        private readonly T[] _items;

        /// <summary>
        /// The buckets, each <see cref="Vector128{T}.Count"/> values long. The unused slots of a bucket hold a copy of the
        /// first value in the bucket, which can thus be found only once it's already been found in the first slot, or for
        /// an empty bucket, of a value from another bucket, which by definition never gets looked up in this bucket.
        /// </summary>
        private readonly TUnsigned[] _buckets;

        /// <summary>The index in <see cref="_items"/> of the first item of each bucket.</summary>
        /// <remarks>A byte is enough as there are at most <see cref="Constants.MaxItemsInVectorBucketsFrozenCollection"/> items.</remarks>
        private readonly byte[] _bucketStarts;

        /// <summary>The multiplier of the multiplicative hash function.</summary>
        private readonly ulong _multiplier;

        /// <summary>The shift of the multiplicative hash function, which leaves as many bits as needed to index the buckets.</summary>
        private readonly int _shift;

        internal VectorBucketsFrozenSet(TUnsigned[] values, byte[] bucketCounts, ulong multiplier) : base(EqualityComparer<T>.Default)
        {
            Debug.Assert(bucketCounts.Length >= 2 && BitOperations.IsPow2(bucketCounts.Length));

            int bucketCapacity = Vector128<TUnsigned>.Count;

            // The lookups read the buckets without bounds checks, which is safe as long as every hash is less than the number
            // of buckets. Deriving the shift from the number of buckets (a power of 2, and at least 2 so that the shift is less
            // than the number of bits hashed) here guarantees that: it leaves only Log2(bucketCounts.Length) bits of the hash,
            // and _bucketStarts and _buckets are allocated below for bucketCounts.Length buckets.
            _multiplier = multiplier;
            _shift = VectorBucketsFrozenSet.GetShift<TUnsigned>(BitOperations.Log2((uint)bucketCounts.Length));
            _bucketStarts = new byte[bucketCounts.Length];
            int start = 0;
            for (int bucket = 0; bucket < bucketCounts.Length; bucket++)
            {
                Debug.Assert(bucketCounts[bucket] <= bucketCapacity);
                _bucketStarts[bucket] = (byte)start;
                start += bucketCounts[bucket];
            }
            Debug.Assert(start == values.Length);

            // Store the items grouped by bucket.
            _items = new T[values.Length];
            int[] bucketFills = new int[bucketCounts.Length];
            foreach (TUnsigned value in values)
            {
                int bucket = VectorBucketsFrozenSet.GetBucket(value, multiplier, _shift);
                _items[_bucketStarts[bucket] + bucketFills[bucket]++] = Unsafe.BitCast<TUnsigned, T>(value);
            }

            // Lay out the buckets.
            _buckets = new TUnsigned[bucketCounts.Length * bucketCapacity];
            for (int bucket = 0; bucket < bucketCounts.Length; bucket++)
            {
                int bucketCount = bucketCounts[bucket];
                Span<TUnsigned> slots = _buckets.AsSpan(bucket * bucketCapacity, bucketCapacity);
                for (int slot = 0; slot < bucketCount; slot++)
                {
                    slots[slot] = Unsafe.BitCast<T, TUnsigned>(_items[_bucketStarts[bucket] + slot]);
                }

                slots.Slice(bucketCount).Fill(bucketCount != 0 ? slots[0] : Unsafe.BitCast<T, TUnsigned>(_items[0]));
            }
        }

        private protected override T[] ItemsCore => _items;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);
        private protected override int CountCore => _items.Length;

        // This is small enough to be worth inlining into Contains when the call gets devirtualized.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override int FindItemIndex(T item)
        {
            TUnsigned value = Unsafe.BitCast<T, TUnsigned>(item);
            int bucket = VectorBucketsFrozenSet.GetBucket(value, _multiplier, _shift);

            // The bucket is always in range (see the constructor), so the vector can be loaded without bounds checks,
            // which measurably slowed down lookups.
            Debug.Assert((uint)bucket < (uint)_bucketStarts.Length);
            Debug.Assert(_buckets.Length == _bucketStarts.Length * Vector128<TUnsigned>.Count);
            Vector128<TUnsigned> slots = Vector128.LoadUnsafe(ref MemoryMarshal.GetArrayDataReference(_buckets), (nuint)(uint)bucket * (nuint)Vector128<TUnsigned>.Count);

            // The result is computed without branching on whether the value was found (as Vector128.IndexOf would once
            // inlined into a loop), as such a branch is mispredicted half of the time when hits and misses are mixed.
            // There are at most 8 lanes, so with bit 16 set, TrailingZeroCount returns 16 if there's no match, without
            // ever being given 0 (when it isn't a single instruction, i.e. without BMI1, it would then branch on it).
            uint matches = Vector128.Equals(slots, Vector128.Create(value)).ExtractMostSignificantBits();
            int lane = BitOperations.TrailingZeroCount(matches | (1u << 16));
            return (_bucketStarts[bucket] + lane) | -(lane >> 4);
        }

        internal struct GSW : IGenericSpecializedWrapper
        {
            private VectorBucketsFrozenSet<T, TUnsigned> _set;
            public void Store(FrozenSet<T> set) => _set = (VectorBucketsFrozenSet<T, TUnsigned>)set;

            public int Count => _set.Count;
            public IEqualityComparer<T> Comparer => _set.Comparer;
            public int FindItemIndex(T item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
