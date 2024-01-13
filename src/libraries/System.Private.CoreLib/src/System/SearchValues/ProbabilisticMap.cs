// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;

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
        // The vectorized algorithm operates on bytes instead of uint32s.
        // The index and shift are adjusted so that we represent the structure
        // as "32 x uint8" instead of "8 x uint32".
        // We use the vectorized implementation when we have access to Sse41 or Arm64 intrinsics.
        private const uint VectorizedIndexMask = 31u;
        private const int VectorizedIndexShift = 5;

        // If we don't support vectorization, use uint32 to speed up
        // "IsCharBitSet" checks in scalar loops.
        private const uint PortableIndexMask = 7u;
        private const int PortableIndexShift = 3;

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
                SetCharBit(ref charMap, 0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetCharBit(ref uint charMap, byte value)
        {
            if (Sse41.IsSupported || AdvSimd.Arm64.IsSupported)
            {
                Unsafe.Add(ref Unsafe.As<uint, byte>(ref charMap), value & VectorizedIndexMask) |= (byte)(1u << (value >> VectorizedIndexShift));
            }
            else
            {
                Unsafe.Add(ref charMap, value & PortableIndexMask) |= 1u << (value >> PortableIndexShift);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsCharBitSet(ref uint charMap, byte value) => Sse41.IsSupported || AdvSimd.Arm64.IsSupported
            ? (Unsafe.Add(ref Unsafe.As<uint, byte>(ref charMap), value & VectorizedIndexMask) & (1u << (value >> VectorizedIndexShift))) != 0
            : (Unsafe.Add(ref charMap, value & PortableIndexMask) & (1u << (value >> PortableIndexShift))) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Contains(ref uint charMap, ReadOnlySpan<char> values, int ch) =>
            IsCharBitSet(ref charMap, (byte)ch) &&
            IsCharBitSet(ref charMap, (byte)(ch >> 8)) &&
            Contains(values, (char)ch);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Contains(ReadOnlySpan<char> values, char ch) =>
            SpanHelpers.NonPackedContainsValueType(
                ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(values)),
                (short)ch,
                values.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512Vbmi))]
        private static Vector512<byte> ContainsMask64CharsAvx512(Vector512<byte> charMap, ref char searchSpace0, ref char searchSpace1)
        {
            Vector512<ushort> source0 = Vector512.LoadUnsafe(ref searchSpace0);
            Vector512<ushort> source1 = Vector512.LoadUnsafe(ref searchSpace1);

            Vector512<byte> sourceLower = Avx512BW.PackUnsignedSaturate(
                (source0 & Vector512.Create((ushort)255)).AsInt16(),
                (source1 & Vector512.Create((ushort)255)).AsInt16());

            Vector512<byte> sourceUpper = Avx512BW.PackUnsignedSaturate(
                (source0 >>> 8).AsInt16(),
                (source1 >>> 8).AsInt16());

            Vector512<byte> resultLower = IsCharBitNotSetAvx512(charMap, sourceLower);
            Vector512<byte> resultUpper = IsCharBitNotSetAvx512(charMap, sourceUpper);

            return ~(resultLower | resultUpper);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512Vbmi))]
        private static Vector512<byte> IsCharBitNotSetAvx512(Vector512<byte> charMap, Vector512<byte> values)
        {
            Vector512<byte> shifted = values >>> VectorizedIndexShift;

            Vector512<byte> bitPositions = Avx512BW.Shuffle(Vector512.Create(0x8040201008040201).AsByte(), shifted);

            Vector512<byte> index = values & Vector512.Create((byte)VectorizedIndexMask);
            Vector512<byte> bitMask = Avx512Vbmi.PermuteVar64x8(charMap, index);

            return Vector512.Equals(bitMask & bitPositions, Vector512<byte>.Zero);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512Vbmi.VL))]
        private static Vector256<byte> ContainsMask32CharsAvx512(Vector256<byte> charMap, ref char searchSpace0, ref char searchSpace1)
        {
            Vector256<ushort> source0 = Vector256.LoadUnsafe(ref searchSpace0);
            Vector256<ushort> source1 = Vector256.LoadUnsafe(ref searchSpace1);

            Vector256<byte> sourceLower = Avx2.PackUnsignedSaturate(
                (source0 & Vector256.Create((ushort)255)).AsInt16(),
                (source1 & Vector256.Create((ushort)255)).AsInt16());

            Vector256<byte> sourceUpper = Avx2.PackUnsignedSaturate(
                (source0 >>> 8).AsInt16(),
                (source1 >>> 8).AsInt16());

            Vector256<byte> resultLower = IsCharBitNotSetAvx512(charMap, sourceLower);
            Vector256<byte> resultUpper = IsCharBitNotSetAvx512(charMap, sourceUpper);

            return ~(resultLower | resultUpper);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512Vbmi.VL))]
        private static Vector256<byte> IsCharBitNotSetAvx512(Vector256<byte> charMap, Vector256<byte> values)
        {
            Vector256<byte> shifted = values >>> VectorizedIndexShift;

            Vector256<byte> bitPositions = Avx2.Shuffle(Vector256.Create(0x8040201008040201).AsByte(), shifted);

            Vector256<byte> index = values & Vector256.Create((byte)VectorizedIndexMask);
            Vector256<byte> bitMask = Avx512Vbmi.VL.PermuteVar32x8(charMap, index);

            return Vector256.Equals(bitMask & bitPositions, Vector256<byte>.Zero);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static Vector256<byte> ContainsMask32CharsAvx2(Vector256<byte> charMapLower, Vector256<byte> charMapUpper, ref char searchSpace)
        {
            Vector256<ushort> source0 = Vector256.LoadUnsafe(ref searchSpace);
            Vector256<ushort> source1 = Vector256.LoadUnsafe(ref searchSpace, (nuint)Vector256<ushort>.Count);

            Vector256<byte> sourceLower = Avx2.PackUnsignedSaturate(
                (source0 & Vector256.Create((ushort)255)).AsInt16(),
                (source1 & Vector256.Create((ushort)255)).AsInt16());

            Vector256<byte> sourceUpper = Avx2.PackUnsignedSaturate(
                (source0 >>> 8).AsInt16(),
                (source1 >>> 8).AsInt16());

            Vector256<byte> resultLower = IsCharBitNotSetAvx2(charMapLower, charMapUpper, sourceLower);
            Vector256<byte> resultUpper = IsCharBitNotSetAvx2(charMapLower, charMapUpper, sourceUpper);

            return ~(resultLower | resultUpper);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static Vector256<byte> IsCharBitNotSetAvx2(Vector256<byte> charMapLower, Vector256<byte> charMapUpper, Vector256<byte> values)
        {
            Vector256<byte> shifted = values >>> VectorizedIndexShift;

            Vector256<byte> bitPositions = Avx2.Shuffle(Vector256.Create(0x8040201008040201).AsByte(), shifted);

            Vector256<byte> index = values & Vector256.Create((byte)VectorizedIndexMask);
            Vector256<byte> bitMaskLower = Avx2.Shuffle(charMapLower, index);
            Vector256<byte> bitMaskUpper = Avx2.Shuffle(charMapUpper, index - Vector256.Create((byte)16));
            Vector256<byte> mask = Vector256.GreaterThan(index, Vector256.Create((byte)15));
            Vector256<byte> bitMask = Vector256.ConditionalSelect(mask, bitMaskUpper, bitMaskLower);

            return Vector256.Equals(bitMask & bitPositions, Vector256<byte>.Zero);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        [CompExactlyDependsOn(typeof(Sse2))]
        private static Vector128<byte> ContainsMask16Chars(Vector128<byte> charMapLower, Vector128<byte> charMapUpper, ref char searchSpace)
        {
            Vector128<ushort> source0 = Vector128.LoadUnsafe(ref searchSpace);
            Vector128<ushort> source1 = Vector128.LoadUnsafe(ref searchSpace, (nuint)Vector128<ushort>.Count);

            Vector128<byte> sourceLower = Sse2.IsSupported
                ? Sse2.PackUnsignedSaturate((source0 & Vector128.Create((ushort)255)).AsInt16(), (source1 & Vector128.Create((ushort)255)).AsInt16())
                : AdvSimd.Arm64.UnzipEven(source0.AsByte(), source1.AsByte());

            Vector128<byte> sourceUpper = Sse2.IsSupported
                ? Sse2.PackUnsignedSaturate((source0 >>> 8).AsInt16(), (source1 >>> 8).AsInt16())
                : AdvSimd.Arm64.UnzipOdd(source0.AsByte(), source1.AsByte());

            Vector128<byte> resultLower = IsCharBitNotSet(charMapLower, charMapUpper, sourceLower);
            Vector128<byte> resultUpper = IsCharBitNotSet(charMapLower, charMapUpper, sourceUpper);

            return ~(resultLower | resultUpper);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        private static Vector128<byte> IsCharBitNotSet(Vector128<byte> charMapLower, Vector128<byte> charMapUpper, Vector128<byte> values)
        {
            Vector128<byte> shifted = values >>> VectorizedIndexShift;

            Vector128<byte> bitPositions = Vector128.ShuffleUnsafe(Vector128.Create(0x8040201008040201).AsByte(), shifted);

            Vector128<byte> index = values & Vector128.Create((byte)VectorizedIndexMask);
            Vector128<byte> bitMask;

            if (AdvSimd.Arm64.IsSupported)
            {
                bitMask = AdvSimd.Arm64.VectorTableLookup((charMapLower, charMapUpper), index);
            }
            else
            {
                Vector128<byte> bitMaskLower = Vector128.ShuffleUnsafe(charMapLower, index);
                Vector128<byte> bitMaskUpper = Vector128.ShuffleUnsafe(charMapUpper, index - Vector128.Create((byte)16));
                Vector128<byte> mask = Vector128.GreaterThan(index, Vector128.Create((byte)15));
                bitMask = Vector128.ConditionalSelect(mask, bitMaskUpper, bitMaskLower);
            }

            return Vector128.Equals(bitMask & bitPositions, Vector128<byte>.Zero);
        }

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

        public static int IndexOfAny(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
        {
            var valuesSpan = new ReadOnlySpan<char>(ref values, valuesLength);

            // If the search space is relatively short compared to the needle, do a simple O(n * m) search.
            if (ShouldUseSimpleLoop(searchSpaceLength, valuesLength))
            {
                return IndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.DontNegate>(ref searchSpace, searchSpaceLength, valuesSpan);
            }

            if (IndexOfAnyAsciiSearcher.TryIndexOfAny(ref searchSpace, searchSpaceLength, valuesSpan, out int index))
            {
                return index;
            }

            return ProbabilisticIndexOfAny(ref searchSpace, searchSpaceLength, ref values, valuesLength);
        }

        public static int IndexOfAnyExcept(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
        {
            var valuesSpan = new ReadOnlySpan<char>(ref values, valuesLength);

            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported &&
                !ShouldUseSimpleLoop(searchSpaceLength, valuesLength) &&
                IndexOfAnyAsciiSearcher.TryIndexOfAnyExcept(ref searchSpace, searchSpaceLength, valuesSpan, out int index))
            {
                return index;
            }

            return IndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.Negate>(ref searchSpace, searchSpaceLength, valuesSpan);
        }

        public static int LastIndexOfAny(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
        {
            var valuesSpan = new ReadOnlySpan<char>(ref values, valuesLength);

            // If the search space is relatively short compared to the needle, do a simple O(n * m) search.
            if (ShouldUseSimpleLoop(searchSpaceLength, valuesLength))
            {
                return LastIndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.DontNegate>(ref searchSpace, searchSpaceLength, valuesSpan);
            }

            if (IndexOfAnyAsciiSearcher.TryLastIndexOfAny(ref searchSpace, searchSpaceLength, valuesSpan, out int index))
            {
                return index;
            }

            return ProbabilisticLastIndexOfAny(ref searchSpace, searchSpaceLength, ref values, valuesLength);
        }

        public static int LastIndexOfAnyExcept(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
        {
            var valuesSpan = new ReadOnlySpan<char>(ref values, valuesLength);

            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported &&
                !ShouldUseSimpleLoop(searchSpaceLength, valuesLength) &&
                IndexOfAnyAsciiSearcher.TryLastIndexOfAnyExcept(ref searchSpace, searchSpaceLength, valuesSpan, out int index))
            {
                return index;
            }

            return LastIndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.Negate>(ref searchSpace, searchSpaceLength, valuesSpan);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ProbabilisticIndexOfAny(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
        {
            var valuesSpan = new ReadOnlySpan<char>(ref values, valuesLength);

            var map = new ProbabilisticMap(valuesSpan);
            ref uint charMap = ref Unsafe.As<ProbabilisticMap, uint>(ref map);

            return IndexOfAny(ref charMap, ref searchSpace, searchSpaceLength, valuesSpan);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ProbabilisticLastIndexOfAny(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
        {
            var valuesSpan = new ReadOnlySpan<char>(ref values, valuesLength);

            var map = new ProbabilisticMap(valuesSpan);
            ref uint charMap = ref Unsafe.As<ProbabilisticMap, uint>(ref map);

            return LastIndexOfAny(ref charMap, ref searchSpace, searchSpaceLength, valuesSpan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAny(ref uint charMap, ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> values)
        {
            if ((Sse41.IsSupported || AdvSimd.Arm64.IsSupported) && searchSpaceLength >= 16)
            {
                return Vector512.IsHardwareAccelerated && Avx512Vbmi.VL.IsSupported
                    ? IndexOfAnyVectorizedAvx512(ref charMap, ref searchSpace, searchSpaceLength, values)
                    : IndexOfAnyVectorized(ref charMap, ref searchSpace, searchSpaceLength, values);
            }

            ref char searchSpaceEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
            ref char cur = ref searchSpace;

            while (!Unsafe.AreSame(ref cur, ref searchSpaceEnd))
            {
                int ch = cur;
                if (Contains(ref charMap, values, ch))
                {
                    return MatchOffset(ref searchSpace, ref cur);
                }

                cur = ref Unsafe.Add(ref cur, 1);
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAny(ref uint charMap, ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> values)
        {
            for (int i = searchSpaceLength - 1; i >= 0; i--)
            {
                int ch = Unsafe.Add(ref searchSpace, i);
                if (Contains(ref charMap, values, ch))
                {
                    return i;
                }
            }

            return -1;
        }

        [CompExactlyDependsOn(typeof(Avx512Vbmi.VL))]
        private static int IndexOfAnyVectorizedAvx512(ref uint charMap, ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> values)
        {
            Debug.Assert(Avx512Vbmi.VL.IsSupported);
            Debug.Assert(searchSpaceLength >= 16);

            ref char searchSpaceEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength);

            Vector256<byte> charMap256 = Vector256.LoadUnsafe(ref Unsafe.As<uint, byte>(ref charMap));

            if (searchSpaceLength > 32)
            {
                Vector512<byte> charMap512 = Vector512.Create(charMap256, charMap256);

                if (searchSpaceLength > 64)
                {
                    ref char cur = ref searchSpace;
                    ref char lastStartVector = ref Unsafe.Subtract(ref searchSpaceEnd, 64);

                    while (true)
                    {
                        Vector512<byte> result = ContainsMask64CharsAvx512(charMap512, ref cur, ref Unsafe.Add(ref cur, Vector512<ushort>.Count));

                        if (result != Vector512<byte>.Zero)
                        {
                            if (TryFindMatch(ref cur, PackedSpanHelpers.FixUpPackedVector512Result(result).ExtractMostSignificantBits(), values, out int index))
                            {
                                return MatchOffset(ref searchSpace, ref cur) + index;
                            }
                        }

                        cur = ref Unsafe.Add(ref cur, 64);

                        if (Unsafe.IsAddressGreaterThan(ref cur, ref lastStartVector))
                        {
                            if (Unsafe.AreSame(ref cur, ref searchSpaceEnd))
                            {
                                break;
                            }

                            // Adjust the current vector and do one last iteration.
                            cur = ref lastStartVector;
                        }
                    }
                }
                else
                {
                    Debug.Assert(searchSpaceLength is > 32 and <= 64);

                    // Process the first and last vector in the search space.
                    // They may overlap, but we'll handle that in the index calculation if we do get a match.
                    Vector512<byte> result = ContainsMask64CharsAvx512(charMap512, ref searchSpace, ref Unsafe.Subtract(ref searchSpaceEnd, Vector512<ushort>.Count));

                    if (result != Vector512<byte>.Zero)
                    {
                        if (TryFindMatchOverlapped(ref searchSpace, searchSpaceLength, PackedSpanHelpers.FixUpPackedVector512Result(result).ExtractMostSignificantBits(), values, out int index))
                        {
                            return index;
                        }
                    }
                }
            }
            else
            {
                Debug.Assert(searchSpaceLength is >= 16 and <= 32);

                // Process the first and last vector in the search space.
                // They may overlap, but we'll handle that in the index calculation if we do get a match.
                Vector256<byte> result = ContainsMask32CharsAvx512(charMap256, ref searchSpace, ref Unsafe.Subtract(ref searchSpaceEnd, Vector256<ushort>.Count));

                if (result != Vector256<byte>.Zero)
                {
                    if (TryFindMatchOverlapped(ref searchSpace, searchSpaceLength, PackedSpanHelpers.FixUpPackedVector256Result(result).ExtractMostSignificantBits(), values, out int index))
                    {
                        return index;
                    }
                }
            }

            return -1;
        }

        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        [CompExactlyDependsOn(typeof(Sse41))]
        private static int IndexOfAnyVectorized(ref uint charMap, ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> values)
        {
            Debug.Assert(Sse41.IsSupported || AdvSimd.Arm64.IsSupported);
            Debug.Assert(searchSpaceLength >= 16);

            ref char searchSpaceEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
            ref char cur = ref searchSpace;

            Vector128<byte> charMapLower = Vector128.LoadUnsafe(ref Unsafe.As<uint, byte>(ref charMap));
            Vector128<byte> charMapUpper = Vector128.LoadUnsafe(ref Unsafe.As<uint, byte>(ref charMap), (nuint)Vector128<byte>.Count);

#pragma warning disable IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough // In this case, we have an else clause which has the same semantic meaning whether or not Avx2 is considered supported or unsupported
            if (Avx2.IsSupported && searchSpaceLength >= 32)
#pragma warning restore IntrinsicsInSystemPrivateCoreLibAttributeNotSpecificEnough
            {
                Vector256<byte> charMapLower256 = Vector256.Create(charMapLower, charMapLower);
                Vector256<byte> charMapUpper256 = Vector256.Create(charMapUpper, charMapUpper);

                ref char lastStartVectorAvx2 = ref Unsafe.Subtract(ref searchSpaceEnd, 32);

                while (true)
                {
                    Vector256<byte> result = ContainsMask32CharsAvx2(charMapLower256, charMapUpper256, ref cur);

                    if (result != Vector256<byte>.Zero)
                    {
                        if (TryFindMatch(ref cur, PackedSpanHelpers.FixUpPackedVector256Result(result).ExtractMostSignificantBits(), values, out int index))
                        {
                            return MatchOffset(ref searchSpace, ref cur) + index;
                        }
                    }

                    cur = ref Unsafe.Add(ref cur, 32);

                    if (Unsafe.IsAddressGreaterThan(ref cur, ref lastStartVectorAvx2))
                    {
                        if (Unsafe.AreSame(ref cur, ref searchSpaceEnd))
                        {
                            return -1;
                        }

                        if (Unsafe.ByteOffset(ref cur, ref searchSpaceEnd) > 16 * sizeof(char))
                        {
                            // If we have more than 16 characters left to process, we can
                            // adjust the current vector and do one last iteration of Avx2.
                            cur = ref lastStartVectorAvx2;
                        }
                        else
                        {
                            // Otherwise adjust the vector such that we'll only need to do a single
                            // iteration of ContainsMask16Chars below.
                            cur = ref Unsafe.Subtract(ref searchSpaceEnd, 16);
                            break;
                        }
                    }
                }
            }

            ref char lastStartVector = ref Unsafe.Subtract(ref searchSpaceEnd, 16);

            while (true)
            {
                Vector128<byte> result = ContainsMask16Chars(charMapLower, charMapUpper, ref cur);

                if (result != Vector128<byte>.Zero)
                {
                    if (TryFindMatch(ref cur, result.ExtractMostSignificantBits(), values, out int index))
                    {
                        return MatchOffset(ref searchSpace, ref cur) + index;
                    }
                }

                cur = ref Unsafe.Add(ref cur, 16);

                if (Unsafe.IsAddressGreaterThan(ref cur, ref lastStartVector))
                {
                    if (Unsafe.AreSame(ref cur, ref searchSpaceEnd))
                    {
                        break;
                    }

                    // Adjust the current vector and do one last iteration.
                    cur = ref lastStartVector;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MatchOffset(ref char searchSpace, ref char cur) =>
            (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref cur) / sizeof(char));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFindMatch(ref char cur, uint mask, ReadOnlySpan<char> values, out int index)
        {
            do
            {
                index = BitOperations.TrailingZeroCount(mask);

                if (Contains(values, Unsafe.Add(ref cur, index)))
                {
                    return true;
                }

                mask = BitOperations.ResetLowestSetBit(mask);
            }
            while (mask != 0);

            index = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFindMatchOverlapped(ref char cur, int searchSpaceLength, uint mask, ReadOnlySpan<char> values, out int index)
        {
            do
            {
                index = BitOperations.TrailingZeroCount(mask);

                if (index >= Vector256<ushort>.Count)
                {
                    // The potential match is in the second vector.
                    // Fixup the index to account for how we loaded the second overlapped vector.
                    index += searchSpaceLength - (2 * Vector256<ushort>.Count);
                }

                if (Contains(values, Unsafe.Add(ref cur, index)))
                {
                    return true;
                }

                mask = BitOperations.ResetLowestSetBit(mask);
            }
            while (mask != 0);

            index = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFindMatch(ref char cur, ulong mask, ReadOnlySpan<char> values, out int index)
        {
            do
            {
                index = BitOperations.TrailingZeroCount(mask);

                if (Contains(values, Unsafe.Add(ref cur, index)))
                {
                    return true;
                }

                mask = BitOperations.ResetLowestSetBit(mask);
            }
            while (mask != 0);

            index = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFindMatchOverlapped(ref char cur, int searchSpaceLength, ulong mask, ReadOnlySpan<char> values, out int index)
        {
            do
            {
                index = BitOperations.TrailingZeroCount(mask);

                if (index >= Vector512<ushort>.Count)
                {
                    // The potential match is in the second vector.
                    // Fixup the index to account for how we loaded the second overlapped vector.
                    index += searchSpaceLength - (2 * Vector512<ushort>.Count);
                }

                if (Contains(values, Unsafe.Add(ref cur, index)))
                {
                    return true;
                }

                mask = BitOperations.ResetLowestSetBit(mask);
            }
            while (mask != 0);

            index = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnySimpleLoop<TNegator>(ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> values)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            ref char searchSpaceEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
            ref char cur = ref searchSpace;

            while (!Unsafe.AreSame(ref cur, ref searchSpaceEnd))
            {
                char c = cur;
                if (TNegator.NegateIfNeeded(Contains(values, c)))
                {
                    return MatchOffset(ref searchSpace, ref cur);
                }

                cur = ref Unsafe.Add(ref cur, 1);
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnySimpleLoop<TNegator>(ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> values)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            for (int i = searchSpaceLength - 1; i >= 0; i--)
            {
                char c = Unsafe.Add(ref searchSpace, i);
                if (TNegator.NegateIfNeeded(Contains(values, c)))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
