// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
                switch (Unsafe.SizeOf<T>())
                {
                    case sizeof(byte): return CreateIfValid<T, byte>(source);
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

            // Start with a 50% load factor (and at least 2 buckets, so that the shift below is never 64), and double
            // the number of buckets until a multiplier is found that doesn't overflow any bucket, giving up once there
            // would be more than MaxSlotsPerItem slots per item.
            int bucketCountLog2 = 1;
            while ((bucketCapacity << bucketCountLog2) < 2 * count)
            {
                bucketCountLog2++;
            }

            for (; (bucketCapacity << bucketCountLog2) <= Math.Max(count * MaxSlotsPerItem, 2 * bucketCapacity); bucketCountLog2++)
            {
                byte[] bucketCounts = new byte[1 << bucketCountLog2];
                int shift = 64 - bucketCountLog2;
                foreach (ulong multiplier in Multipliers)
                {
                    if (TryCountBucketItems(values, multiplier, shift, bucketCounts, bucketCapacity))
                    {
                        return new VectorBucketsFrozenSet<T, TUnsigned>(values, bucketCounts, multiplier, shift);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetBucket<TUnsigned>(TUnsigned value, ulong multiplier, int shift)
            where TUnsigned : unmanaged, IBinaryInteger<TUnsigned>, IUnsignedNumber<TUnsigned> =>
            (int)((ulong.CreateTruncating(value) * multiplier) >> shift);
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
    /// comparison, without any data-dependent loop.
    /// For the types supported, equality as defined by <see cref="EqualityComparer{T}.Default"/> is bitwise equality, so
    /// values are stored and compared as the unsigned integer type of the same size.
    /// </remarks>
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

        /// <summary>The shift of the multiplicative hash function: 64 minus the base-2 log of the number of buckets.</summary>
        private readonly int _shift;

        internal VectorBucketsFrozenSet(TUnsigned[] values, byte[] bucketCounts, ulong multiplier, int shift) : base(EqualityComparer<T>.Default)
        {
            int bucketCapacity = Vector128<TUnsigned>.Count;

            _multiplier = multiplier;
            _shift = shift;
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
                int bucket = VectorBucketsFrozenSet.GetBucket(value, multiplier, shift);
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

            // The bucket is always in range, as the shift leaves only as many bits as needed to index the buckets, so the
            // vector can be loaded without bounds checks (which measurably slowed down lookups).
            Debug.Assert((uint)bucket < (uint)_bucketStarts.Length);
            Debug.Assert(_buckets.Length == _bucketStarts.Length * Vector128<TUnsigned>.Count);
            Vector128<TUnsigned> slots = Vector128.LoadUnsafe(ref MemoryMarshal.GetArrayDataReference(_buckets), (nuint)(uint)bucket * (nuint)Vector128<TUnsigned>.Count);

            // This is branch-free with respect to whether the value is found, so that the generated code doesn't depend
            // on whether hits or misses were more frequent when the method was profiled by dynamic PGO.
            int lane = Vector128.IndexOf(slots, value);
            int index = _bucketStarts[bucket] + lane;
            return lane >= 0 ? index : -1;
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
