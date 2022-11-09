// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System
{
    /// <summary>Data structure used to optimize checks for whether a char is in a set of chars.</summary>
    /// <remarks>
    /// Like a Bloom filter, the idea is to create a bit map of the characters we are
    /// searching for and use this map as a "cheap" check to decide if the current
    /// character in the string exists in the array of input characters. There are
    /// 256 bits in the map, with each character mapped to 2 bits. Every character is
    /// divided into 2 bytes, and then every byte is mapped to 1 bit. The character map
    /// is an array of 8 integers acting as map blocks. The 3 lsb in each byte in the
    /// character is used to index into this map to get the right block, the value of
    /// the remaining 5 msb are used as the bit position inside this block.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = Size * sizeof(uint))]
    internal struct ProbabilisticMap
    {
        private const int Size = 0x8;
        private const int IndexMask = 0x7;
        private const int IndexShift = 0x3;

        /// <summary>Initializes the map based on the specified values.</summary>
        /// <param name="charMap">A pointer to the beginning of a <see cref="ProbabilisticMap"/>.</param>
        /// <param name="values">The values to set in the map.</param>
        public static unsafe void Initialize(uint* charMap, ReadOnlySpan<char> values)
        {
#if DEBUG
            for (int i = 0; i < Size; i++)
            {
                Debug.Assert(charMap[i] == 0, "Expected charMap to be zero-initialized.");
            }
#endif
            bool hasAscii = false;
            uint* charMapLocal = charMap; // https://github.com/dotnet/runtime/issues/9040

            for (int i = 0; i < values.Length; ++i)
            {
                int c = values[i];

                // Map low bit
                SetCharBit(charMapLocal, (byte)c);

                // Map high bit
                c >>= 8;

                if (c == 0)
                {
                    hasAscii = true;
                }
                else
                {
                    SetCharBit(charMapLocal, (byte)c);
                }
            }

            if (hasAscii)
            {
                // Common to search for ASCII symbols. Just set the high value once.
                charMapLocal[0] |= 1u;
            }
        }

        public static unsafe bool IsCharBitSet(uint* charMap, byte value) =>
            (charMap[(uint)value & IndexMask] & (1u << (value >> IndexShift))) != 0;

        private static unsafe void SetCharBit(uint* charMap, byte value) =>
            charMap[(uint)value & IndexMask] |= 1u << (value >> IndexShift);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldUseSimpleLoop(int searchSpaceLength, int valuesLength)
        {
            // We can perform either
            // - a simple O(haystack * needle) search or
            // - compute a character map of the values in O(needle), followed by an O(haystack) search
            // As the constant factor to compute the character map is relatively high, it's more efficient
            // to perform a simple loop search for short inputs.
            //
            // The following check does an educated guess as to whether computing the bitmap is more expensive.
            // The limit of 20 on the haystack length is arbitrary, determined by experimentation.
            return searchSpaceLength < Vector128<short>.Count
                || (searchSpaceLength < 20 && searchSpaceLength < (valuesLength >> 1));
        }

        public static int IndexOfAny(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength) =>
            IndexOfAny<SpanHelpers.DontNegate<char>>(ref searchSpace, searchSpaceLength, ref values, valuesLength);

        public static int IndexOfAnyExcept(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength) =>
            IndexOfAny<SpanHelpers.Negate<char>>(ref searchSpace, searchSpaceLength, ref values, valuesLength);

        public static int LastIndexOfAny(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength) =>
            LastIndexOfAny<SpanHelpers.DontNegate<char>>(ref searchSpace, searchSpaceLength, ref values, valuesLength);

        public static int LastIndexOfAnyExcept(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength) =>
            LastIndexOfAny<SpanHelpers.Negate<char>>(ref searchSpace, searchSpaceLength, ref values, valuesLength);

        private static int IndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
            where TNegator : struct, SpanHelpers.INegator<char>
        {
            ReadOnlySpan<char> valuesSpan = new ReadOnlySpan<char>(ref values, valuesLength);

            // If the search space is relatively short compared to the needle, do a simple O(n * m) search.
            if (ShouldUseSimpleLoop(searchSpaceLength, valuesLength))
            {
                ref char searchSpaceEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
                ref char cur = ref searchSpace;

                while (!Unsafe.AreSame(ref cur, ref searchSpaceEnd))
                {
                    if (TNegator.NegateIfNeeded(valuesSpan.Contains(cur)))
                    {
                        return (int)(Unsafe.ByteOffset(ref searchSpace, ref cur) / sizeof(char));
                    }

                    cur = ref Unsafe.Add(ref cur, 1);
                }

                return -1;
            }

            if (typeof(TNegator) == typeof(SpanHelpers.DontNegate<char>)
                ? IndexOfAnyAsciiSearcher.TryIndexOfAny(ref searchSpace, searchSpaceLength, valuesSpan, out int index)
                : IndexOfAnyAsciiSearcher.TryIndexOfAnyExcept(ref searchSpace, searchSpaceLength, valuesSpan, out index))
            {
                return index;
            }

            return ProbabilisticIndexOfAny<TNegator>(ref searchSpace, searchSpaceLength, ref values, valuesLength);
        }

        private static int LastIndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
            where TNegator : struct, SpanHelpers.INegator<char>
        {
            var valuesSpan = new ReadOnlySpan<char>(ref values, valuesLength);

            // If the search space is relatively short compared to the needle, do a simple O(n * m) search.
            if (ShouldUseSimpleLoop(searchSpaceLength, valuesLength))
            {
                ref char cur = ref Unsafe.Add(ref searchSpace, searchSpaceLength);

                while (!Unsafe.AreSame(ref searchSpace, ref cur))
                {
                    cur = ref Unsafe.Subtract(ref cur, 1);

                    if (TNegator.NegateIfNeeded(valuesSpan.Contains(cur)))
                    {
                        return (int)(Unsafe.ByteOffset(ref searchSpace, ref cur) / sizeof(char));
                    }
                }

                return -1;
            }

            if (typeof(TNegator) == typeof(SpanHelpers.DontNegate<char>)
                ? IndexOfAnyAsciiSearcher.TryLastIndexOfAny(ref searchSpace, searchSpaceLength, valuesSpan, out int index)
                : IndexOfAnyAsciiSearcher.TryLastIndexOfAnyExcept(ref searchSpace, searchSpaceLength, valuesSpan, out index))
            {
                return index;
            }

            return ProbabilisticLastIndexOfAny<TNegator>(ref searchSpace, searchSpaceLength, ref values, valuesLength);
        }

        private static unsafe int ProbabilisticIndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
            where TNegator : struct, SpanHelpers.INegator<char>
        {
            var valuesSpan = new ReadOnlySpan<char>(ref values, valuesLength);

            ProbabilisticMap map = default;
            uint* charMap = (uint*)&map;
            Initialize(charMap, valuesSpan);

            ref char searchSpaceEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
            ref char cur = ref searchSpace;

            while (!Unsafe.AreSame(ref cur, ref searchSpaceEnd))
            {
                int ch = cur;
                if (TNegator.NegateIfNeeded(
                        IsCharBitSet(charMap, (byte)ch) &&
                        IsCharBitSet(charMap, (byte)(ch >> 8)) &&
                        valuesSpan.Contains((char)ch)))
                {
                    return (int)(Unsafe.ByteOffset(ref searchSpace, ref cur) / sizeof(char));
                }

                cur = ref Unsafe.Add(ref cur, 1);
            }

            return -1;
        }

        private static unsafe int ProbabilisticLastIndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
            where TNegator : struct, SpanHelpers.INegator<char>
        {
            var valuesSpan = new ReadOnlySpan<char>(ref values, valuesLength);

            ProbabilisticMap map = default;
            uint* charMap = (uint*)&map;
            Initialize(charMap, valuesSpan);

            ref char cur = ref Unsafe.Add(ref searchSpace, searchSpaceLength);

            while (!Unsafe.AreSame(ref searchSpace, ref cur))
            {
                cur = ref Unsafe.Subtract(ref cur, 1);

                int ch = cur;
                if (TNegator.NegateIfNeeded(
                        IsCharBitSet(charMap, (byte)ch) &&
                        IsCharBitSet(charMap, (byte)(ch >> 8)) &&
                        valuesSpan.Contains((char)ch)))
                {
                    return (int)(Unsafe.ByteOffset(ref searchSpace, ref cur) / sizeof(char));
                }
            }

            return -1;
        }
    }
}
