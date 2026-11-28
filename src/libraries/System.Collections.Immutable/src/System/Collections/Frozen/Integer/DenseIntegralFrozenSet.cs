// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a <see cref="FrozenSet{T}"/> for densely-packed integral values.</summary>
    internal static class DenseIntegralFrozenSet
    {
        /// <summary>
        /// Maximum allowed factor by which the spread between the min and max of values in the set may exceed the count.
        /// </summary>
        /// <remarks>
        /// This is dialable. The larger this value, the more likely this implementation will be used,
        /// and the more memory will be consumed by the bitmap. Unlike <see cref="DenseIntegralFrozenDictionary"/>, which
        /// needs a whole value slot per possible key, the bitmap only needs a single bit per possible value (stored
        /// as 8-byte words covering 32 values each). With a value of 64, the bitmap thus needs at most 16 bytes per
        /// item, which is no more than the bucket table alone of the hashing-based implementations (at least two
        /// 8-byte buckets per item).
        /// </remarks>
        private const int LengthToCountFactor = 64;

        /// <summary>Creates a set for the specified source, if <typeparamref name="T"/> is supported and its values are dense enough.</summary>
        /// <param name="source">The values.</param>
        /// <param name="allowBitmap">
        /// Whether to use a bitmap if the values are dense but not contiguous. If false, a set is only created if all the values
        /// between the min and the max are present.
        /// </param>
        public static FrozenSet<T>? CreateIfValid<T>(HashSet<T> source, bool allowBitmap)
        {
            // Int32 and integer types that fit within Int32. This is to minimize difficulty later validating that
            // inputs are in range of int: we can always cast everything here to Int32 without loss of information.
            return
                typeof(T) == typeof(byte) || (typeof(T).IsEnum && typeof(T).GetEnumUnderlyingType() == typeof(byte)) ? CreateIfValid<T, byte>(source, allowBitmap) :
                typeof(T) == typeof(sbyte) || (typeof(T).IsEnum && typeof(T).GetEnumUnderlyingType() == typeof(sbyte)) ? CreateIfValid<T, sbyte>(source, allowBitmap) :
                typeof(T) == typeof(ushort) || (typeof(T).IsEnum && typeof(T).GetEnumUnderlyingType() == typeof(ushort)) ? CreateIfValid<T, ushort>(source, allowBitmap) :
                typeof(T) == typeof(short) || (typeof(T).IsEnum && typeof(T).GetEnumUnderlyingType() == typeof(short)) ? CreateIfValid<T, short>(source, allowBitmap) :
                typeof(T) == typeof(char) ? CreateIfValid<T, char>(source, allowBitmap) :
                typeof(T) == typeof(int) || (typeof(T).IsEnum && typeof(T).GetEnumUnderlyingType() == typeof(int)) ? CreateIfValid<T, int>(source, allowBitmap) :
                null;
        }

        private static FrozenSet<T>? CreateIfValid<T, TUnderlying>(HashSet<T> source, bool allowBitmap)
            where TUnderlying : unmanaged, IBinaryInteger<TUnderlying>
        {
            Debug.Assert(source.Count != 0);
            Debug.Assert(Unsafe.SizeOf<T>() == Unsafe.SizeOf<TUnderlying>());

            int count = source.Count;
            long maxAllowedLength = allowBitmap ? Math.Min((long)count * LengthToCountFactor, Array.MaxLength) : count;

            // Find the min and max of the values, bailing out as soon as they're known to be too spread out
            // (which for values that aren't dense typically happens after just a few of them).
            HashSet<T>.Enumerator e = source.GetEnumerator();
            e.MoveNext();
            int min = ToInt32<T, TUnderlying>(e.Current);
            int max = min;
            while (e.MoveNext())
            {
                int value = ToInt32<T, TUnderlying>(e.Current);
                if (value < min)
                {
                    min = value;
                }
                else if (value > max)
                {
                    max = value;
                }

                if ((long)max - min >= maxAllowedLength)
                {
                    return null;
                }
            }

            // The items are stored sorted, such that the index of an item is its rank among the values in the set.
            var items = new T[count];
            long length = (long)max - min + 1;
            if (length == count)
            {
                // All of the values between min and max are present. The index of a value is just its offset from min.
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = FromInt32<T, TUnderlying>(min + i);
                }

                return new WithContiguousValues<T, TUnderlying>(items, min);
            }

            // Some of the values in the range are missing. Store a bitmap of which values are present, and
            // for every 32-bit word of the bitmap, the number of values present in all previous words, such
            // that a value's index (its rank among the values in the set) can be computed from its word.
            // The words are 32-bit rather than 64-bit so that lookups don't need helper calls for 64-bit shifts
            // on 32-bit platforms, which doesn't use more memory as the rank is 32-bit anyway.
            source.CopyTo(items);
            var words = new BitmapWord[(int)((length + 31) / 32)];
            foreach (T item in items)
            {
                int offset = ToInt32<T, TUnderlying>(item) - min;
                words[offset >> 5].Bits |= 1u << offset;
            }

            int rank = 0;
            for (int i = 0; i < words.Length; i++)
            {
                words[i].Rank = rank;
                for (uint bits = words[i].Bits; bits != 0; bits &= bits - 1)
                {
                    items[rank++] = FromInt32<T, TUnderlying>(min + (i * 32) + BitOperations.TrailingZeroCount(bits));
                }
            }
            Debug.Assert(rank == count);

            return new WithBitmap<T, TUnderlying>(items, words, min);
        }

        // T is either TUnderlying or an enum whose underlying type is TUnderlying, so it can be reinterpreted as TUnderlying
        // with Unsafe.BitCast (which also validates that both have the same size), and back.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ToInt32<T, TUnderlying>(T value) where TUnderlying : unmanaged, IBinaryInteger<TUnderlying> =>
            int.CreateTruncating(Unsafe.BitCast<T, TUnderlying>(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T FromInt32<T, TUnderlying>(int value) where TUnderlying : unmanaged, IBinaryInteger<TUnderlying>
        {
            return Unsafe.BitCast<TUnderlying, T>(TUnderlying.CreateTruncating(value));
        }

        /// <summary>A word of the bitmap along with the number of bits set in all preceding words.</summary>
        private struct BitmapWord
        {
            public uint Bits;
            public int Rank;
        }

        /// <summary>Implementation used when all values between the min and the max are present.</summary>
        [DebuggerTypeProxy(typeof(DebuggerProxy<,>))]
        private sealed class WithContiguousValues<T, TUnderlying> : FrozenSetInternalBase<T, WithContiguousValues<T, TUnderlying>.GSW>
            where TUnderlying : unmanaged, IBinaryInteger<TUnderlying>
        {
            private readonly T[] _items;
            private readonly int _min;
            private readonly uint _maxIndex;

            internal WithContiguousValues(T[] items, int min) : base(EqualityComparer<T>.Default)
            {
                _items = items;
                _min = min;
                _maxIndex = (uint)items.Length - 1;
            }

            private protected override T[] ItemsCore => _items;
            private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);
            private protected override int CountCore => _items.Length;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private protected override int FindItemIndex(T item)
            {
                // Values below the min wrap around to a large unsigned index. The result is computed without branching on whether
                // the value was found, as such a branch is mispredicted half of the time when hits and misses are mixed: as the max
                // index is less than 2^31, the index is out of range if and only if either it or the max index minus it (which then
                // wraps around) has its top bit set.
                uint index = (uint)(ToInt32<T, TUnderlying>(item) - _min);
                return (int)index | ((int)(index | (_maxIndex - index)) >> 31);
            }

            internal struct GSW : IGenericSpecializedWrapper
            {
                private WithContiguousValues<T, TUnderlying> _set;
                public void Store(FrozenSet<T> set) => _set = (WithContiguousValues<T, TUnderlying>)set;

                public int Count => _set.Count;
                public IEqualityComparer<T> Comparer => _set.Comparer;
                public int FindItemIndex(T item) => _set.FindItemIndex(item);
                public Enumerator GetEnumerator() => _set.GetEnumerator();
            }
        }

        /// <summary>Implementation used when only some of the values between the min and the max are present.</summary>
        [DebuggerTypeProxy(typeof(DebuggerProxy<,>))]
        private sealed class WithBitmap<T, TUnderlying> : FrozenSetInternalBase<T, WithBitmap<T, TUnderlying>.GSW>
            where TUnderlying : unmanaged, IBinaryInteger<TUnderlying>
        {
            private readonly T[] _items;
            private readonly BitmapWord[] _words;
            private readonly int _min;

            internal WithBitmap(T[] items, BitmapWord[] words, int min) : base(EqualityComparer<T>.Default)
            {
                _items = items;
                _words = words;
                _min = min;
            }

            private protected override T[] ItemsCore => _items;
            private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);
            private protected override int CountCore => _items.Length;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private protected override int FindItemIndex(T item)
            {
                // Values below the min wrap around to a large unsigned offset, and values above the max land past the
                // last word (or on unset bits of the last word). The result is computed without branching on whether the
                // value was found (or is in range at all), as such a branch is mispredicted half of the time when hits and
                // misses are mixed.
                uint offset = (uint)(ToInt32<T, TUnderlying>(item) - _min);
                BitmapWord[] words = _words;

                // -1 if the offset is within the bitmap, 0 otherwise (the word index is less than 2^27, so this doesn't overflow).
                int inRange = ((int)(offset >> 5) - words.Length) >> 31;

                // Values outside of the bitmap look at the first word, without any of its bits. The word index is thus always
                // within the bitmap, so the word can be read without a bounds check.
                Debug.Assert(words.Length > 0);
                ref BitmapWord word = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(words), (int)((offset >> 5) & (uint)inRange));

                // The bits of the word up to and including the value's bit, moved to the top (shifts of 32-bit values only use
                // the lower 5 bits of the count, i.e. 31 minus the offset within the word): the value is present if and only if
                // the top bit is set, and its index is then the rank of the word plus the number of bits set before its own.
                uint bitsUpToValue = (word.Bits & (uint)inRange) << (int)~offset;
                int found = (int)bitsUpToValue >> 31; // -1 if found, 0 otherwise
                return (word.Rank + BitOperations.PopCount(bitsUpToValue) - 1) | ~found;
            }

            internal struct GSW : IGenericSpecializedWrapper
            {
                private WithBitmap<T, TUnderlying> _set;
                public void Store(FrozenSet<T> set) => _set = (WithBitmap<T, TUnderlying>)set;

                public int Count => _set.Count;
                public IEqualityComparer<T> Comparer => _set.Comparer;
                public int FindItemIndex(T item) => _set.FindItemIndex(item);
                public Enumerator GetEnumerator() => _set.GetEnumerator();
            }
        }

        private sealed class DebuggerProxy<T, TUnderlying>(IEnumerable<T> enumerable) : ImmutableEnumerableDebuggerProxy<T>(enumerable);
    }
}
