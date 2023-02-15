// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

#pragma warning disable IDE0060 // https://github.com/dotnet/roslyn-analyzers/issues/6228

namespace System.Buffers
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
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct ProbabilisticMap
    {
        private const int IndexMask = 0x7;
        private const int IndexShift = 0x3;

        private readonly uint _e0, _e1, _e2, _e3, _e4, _e5, _e6, _e7;

        public ProbabilisticMap(ReadOnlySpan<char> values)
        {
            bool hasAscii = false;
            ref uint charMap = ref _e0;

            for (int i = 0; i < values.Length; ++i)
            {
                int c = values[i];

                // Map low bit
                SetCharBit(ref charMap, (byte)c);

                // Map high bit
                c >>= 8;

                if (c == 0)
                {
                    hasAscii = true;
                }
                else
                {
                    SetCharBit(ref charMap, (byte)c);
                }
            }

            if (hasAscii)
            {
                // Common to search for ASCII symbols. Just set the high value once.
                charMap |= 1u;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetCharBit(ref uint charMap, byte value) =>
            Unsafe.Add(ref charMap, (uint)value & IndexMask) |= 1u << (value >> IndexShift);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsCharBitSet(ref uint charMap, byte value) =>
            (Unsafe.Add(ref charMap, (uint)value & IndexMask) & (1u << (value >> IndexShift))) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Contains(ref uint charMap, ReadOnlySpan<char> values, int ch) =>
            IsCharBitSet(ref charMap, (byte)ch) &&
            IsCharBitSet(ref charMap, (byte)(ch >> 8)) &&
            values.Contains((char)ch);

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
            var valuesSpan = new ReadOnlySpan<char>(ref values, valuesLength);

            // If the search space is relatively short compared to the needle, do a simple O(n * m) search.
            if (ShouldUseSimpleLoop(searchSpaceLength, valuesLength))
            {
                ref char searchSpaceEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
                ref char cur = ref searchSpace;

                while (!Unsafe.AreSame(ref cur, ref searchSpaceEnd))
                {
                    char c = cur;
                    if (TNegator.NegateIfNeeded(valuesSpan.Contains(c)))
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
                for (int i = searchSpaceLength - 1; i >= 0; i--)
                {
                    char c = Unsafe.Add(ref searchSpace, i);
                    if (TNegator.NegateIfNeeded(valuesSpan.Contains(c)))
                    {
                        return i;
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ProbabilisticIndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
            where TNegator : struct, SpanHelpers.INegator<char>
        {
            var valuesSpan = new ReadOnlySpan<char>(ref values, valuesLength);

            var map = new ProbabilisticMap(valuesSpan);
            ref uint charMap = ref Unsafe.As<ProbabilisticMap, uint>(ref map);

            return typeof(TNegator) == typeof(SpanHelpers.DontNegate<char>)
                ? IndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(ref charMap, ref searchSpace, searchSpaceLength, valuesSpan)
                : IndexOfAny<IndexOfAnyAsciiSearcher.Negate>(ref charMap, ref searchSpace, searchSpaceLength, valuesSpan);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ProbabilisticLastIndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
            where TNegator : struct, SpanHelpers.INegator<char>
        {
            var valuesSpan = new ReadOnlySpan<char>(ref values, valuesLength);

            var map = new ProbabilisticMap(valuesSpan);
            ref uint charMap = ref Unsafe.As<ProbabilisticMap, uint>(ref map);

            return typeof(TNegator) == typeof(SpanHelpers.DontNegate<char>)
                ? LastIndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(ref charMap, ref searchSpace, searchSpaceLength, valuesSpan)
                : LastIndexOfAny<IndexOfAnyAsciiSearcher.Negate>(ref charMap, ref searchSpace, searchSpaceLength, valuesSpan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAny<TNegator>(ref uint charMap, ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> values)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            ref char searchSpaceEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
            ref char cur = ref searchSpace;

            while (!Unsafe.AreSame(ref cur, ref searchSpaceEnd))
            {
                int ch = cur;
                if (TNegator.NegateIfNeeded(Contains(ref charMap, values, ch)))
                {
                    return (int)(Unsafe.ByteOffset(ref searchSpace, ref cur) / sizeof(char));
                }

                cur = ref Unsafe.Add(ref cur, 1);
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAny<TNegator>(ref uint charMap, ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> values)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            for (int i = searchSpaceLength - 1; i >= 0; i--)
            {
                int ch = Unsafe.Add(ref searchSpace, i);
                if (TNegator.NegateIfNeeded(Contains(ref charMap, values, ch)))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
