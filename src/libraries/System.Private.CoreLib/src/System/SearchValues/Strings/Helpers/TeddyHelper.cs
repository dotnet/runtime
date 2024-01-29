// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers
{
    /// <summary>
    /// Contains the implementation of core vectorized Teddy matching operations.
    /// They determine which buckets contain potential matches for each input position.
    /// </summary>
    internal static class TeddyHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        public static (Vector128<byte> Result, Vector128<byte> Prev0) ProcessInputN2(
            Vector128<byte> input,
            Vector128<byte> prev0,
            Vector128<byte> n0Low, Vector128<byte> n0High,
            Vector128<byte> n1Low, Vector128<byte> n1High)
        {
            // See the full description of ProcessInputN3 below for more details.
            // This method follows the same pattern as ProcessInputN3, but compares 2 bytes of each bucket at a time instead of 3.
            // We are dealing with 4 input nibble bitmaps instead of 6, and only 1 result from the previous iteration instead of 2.
            (Vector128<byte> low, Vector128<byte> high) = GetNibbles(input);

            // Shuffle each nibble with the 2 corresponding bitmaps to determine which positions match any bucket.
            Vector128<byte> match0 = Shuffle(n0Low, n0High, low, high);
            Vector128<byte> result1 = Shuffle(n1Low, n1High, low, high);

            // RightShift1 shifts the match0 vector to the right by 1 place and shifts in 1 byte from the previous iteration.
            Vector128<byte> result0 = RightShift1(prev0, match0);

            // AND the results together to obtain a list of only buckets that match at all 4 nibble positions.
            Vector128<byte> result = result0 & result1;

            // Return the result and the current matches for byte 0.
            // The next loop iteration, 'match0' will be passed back to this method as 'prev0'.
            return (result, match0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        public static (Vector256<byte> Result, Vector256<byte> Prev0) ProcessInputN2(
            Vector256<byte> input,
            Vector256<byte> prev0,
            Vector256<byte> n0Low, Vector256<byte> n0High,
            Vector256<byte> n1Low, Vector256<byte> n1High)
        {
            // See comments in 'ProcessInputN2' for Vector128<byte> above.
            // This method is the same, but operates on 32 input characters at a time.
            (Vector256<byte> low, Vector256<byte> high) = GetNibbles(input);

            Vector256<byte> match0 = Shuffle(n0Low, n0High, low, high);
            Vector256<byte> result1 = Shuffle(n1Low, n1High, low, high);

            Vector256<byte> result0 = RightShift1(prev0, match0);

            Vector256<byte> result = result0 & result1;

            return (result, match0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        public static (Vector512<byte> Result, Vector512<byte> Prev0) ProcessInputN2(
            Vector512<byte> input,
            Vector512<byte> prev0,
            Vector512<byte> n0Low, Vector512<byte> n0High,
            Vector512<byte> n1Low, Vector512<byte> n1High)
        {
            // See comments in 'ProcessInputN2' for Vector128<byte> above.
            // This method is the same, but operates on 64 input characters at a time.
            (Vector512<byte> low, Vector512<byte> high) = GetNibbles(input);

            Vector512<byte> match0 = Shuffle(n0Low, n0High, low, high);
            Vector512<byte> result1 = Shuffle(n1Low, n1High, low, high);

            Vector512<byte> result0 = RightShift1(prev0, match0);

            Vector512<byte> result = result0 & result1;

            return (result, match0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        public static (Vector128<byte> Result, Vector128<byte> Prev0, Vector128<byte> Prev1) ProcessInputN3(
            Vector128<byte> input,
            Vector128<byte> prev0, Vector128<byte> prev1,
            Vector128<byte> n0Low, Vector128<byte> n0High,
            Vector128<byte> n1Low, Vector128<byte> n1High,
            Vector128<byte> n2Low, Vector128<byte> n2High)
        {
            // This is the core operation of the Teddy algorithm that determines which of the buckets contain potential matches.
            // Every input bitmap argument (n0Low, n0High, ...) encodes a mapping of each of the possible 16 nibble values into an 8-bit bitmap.
            // We test each nibble in the input against these bitmaps to determine which buckets match a given nibble.
            // We then AND together these results to obtain only a list of buckets that match at all 6 nibble positions.
            // Each byte of the result represents an 8-bit bitmask of buckets that may match at each position.
            (Vector128<byte> low, Vector128<byte> high) = GetNibbles(input);

            // Shuffle each nibble with the 3 corresponding bitmaps to determine which positions match any bucket.
            Vector128<byte> match0 = Shuffle(n0Low, n0High, low, high);
            Vector128<byte> match1 = Shuffle(n1Low, n1High, low, high);
            Vector128<byte> result2 = Shuffle(n2Low, n2High, low, high);

            // match0 contain the information for bucket matches at position 0.
            // match1 contain the information for bucket matches at position 1.
            // result2 contain the information for bucket matches at position 2.
            // If we imagine that we only have 1 bucket with 1 string "ABC", the bitmaps we've just obtained encode the following information:
            // match0 tells us at which positions we matched the letter 'A'
            // match1 tells us at which positions we matched the letter 'B'
            // result2 tells us at which positions we matched the letter 'C'
            // If input represents the text "BC text ABC text", they would contain:
            // input:   [B, C,  , t, e, x, t,  , A, B, C,  , t, e, x, t]
            // match0:  [0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0]
            // match1:  [1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0]
            // result2: [0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0]
            //                                   ^  ^  ^
            // Note how the input contains the string ABC, but the matches are not aligned, so we can't just AND them together.
            // To solve this, we shift 'match0' to the right by 2 places and 'match1' to the right by 1 place.
            // result0: [?, ?, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0]
            // result1: [?, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0]
            // result2: [0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0]
            //           ^  ^                          ^
            // The results are now aligned, but we don't know whether the first two positions matched result0 and result1.
            // To replace the missing bytes, we remember the matches from the previous loop iteration, and look at their last 2 bytes.
            // If the previous loop iteration ended on the character 'A', we might even have an earlier match.
            // For example, if the previous input was "Random strings A":
            // prev0:   [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1]
            // result0: [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
            //                                                     ^  ^
            // We will merge the last two bytes of 'prev0' into 'result0' and the last byte of 'prev1' into 'result1'
            // result0: [0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0]
            // result1: [0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0]
            // result2: [0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0]
            //
            // RightShift1 and RightShift2 perform the above operation of shifting the match vectors
            // to the right by 1 and 2 places and shifting in the bytes from the previous iteration.
            Vector128<byte> result0 = RightShift2(prev0, match0);
            Vector128<byte> result1 = RightShift1(prev1, match1);

            // AND the results together to obtain a list of only buckets that match at all 6 nibble positions.
            // result:  [0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0]
            //              ^                          ^
            // Note that we found the match at index 1, even though that match started 2 bytes earlier, at the end of the previous iteration.
            // The caller must account for that when verifying potential matches, see 'MatchStartOffsetN3 = 2' in 'AsciiStringSearchValuesTeddyBase'.
            Vector128<byte> result = result0 & result1 & result2;

            // Return the result and the current matches for byte 0 and 1.
            // The next loop iteration, 'match0' and 'match1' will be passed back to this method as 'prev0' and 'prev1'.
            return (result, match0, match1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        public static (Vector256<byte> Result, Vector256<byte> Prev0, Vector256<byte> Prev1) ProcessInputN3(
            Vector256<byte> input,
            Vector256<byte> prev0, Vector256<byte> prev1,
            Vector256<byte> n0Low, Vector256<byte> n0High,
            Vector256<byte> n1Low, Vector256<byte> n1High,
            Vector256<byte> n2Low, Vector256<byte> n2High)
        {
            // See comments in 'ProcessInputN3' for Vector128<byte> above.
            // This method is the same, but operates on 32 input characters at a time.
            (Vector256<byte> low, Vector256<byte> high) = GetNibbles(input);

            Vector256<byte> match0 = Shuffle(n0Low, n0High, low, high);
            Vector256<byte> match1 = Shuffle(n1Low, n1High, low, high);
            Vector256<byte> result2 = Shuffle(n2Low, n2High, low, high);

            Vector256<byte> result0 = RightShift2(prev0, match0);
            Vector256<byte> result1 = RightShift1(prev1, match1);

            Vector256<byte> result = result0 & result1 & result2;

            return (result, match0, match1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        public static (Vector512<byte> Result, Vector512<byte> Prev0, Vector512<byte> Prev1) ProcessInputN3(
            Vector512<byte> input,
            Vector512<byte> prev0, Vector512<byte> prev1,
            Vector512<byte> n0Low, Vector512<byte> n0High,
            Vector512<byte> n1Low, Vector512<byte> n1High,
            Vector512<byte> n2Low, Vector512<byte> n2High)
        {
            // See comments in 'ProcessInputN3' for Vector128<byte> above.
            // This method is the same, but operates on 64 input characters at a time.
            (Vector512<byte> low, Vector512<byte> high) = GetNibbles(input);

            Vector512<byte> match0 = Shuffle(n0Low, n0High, low, high);
            Vector512<byte> match1 = Shuffle(n1Low, n1High, low, high);
            Vector512<byte> result2 = Shuffle(n2Low, n2High, low, high);

            Vector512<byte> result0 = RightShift2(prev0, match0);
            Vector512<byte> result1 = RightShift1(prev1, match1);

            Vector512<byte> result = result0 & result1 & result2;

            return (result, match0, match1);
        }

        // Read two Vector512<ushort> and concatenate their lower bytes together into a single Vector512<byte>.
        // On X86, characters above 32767 are turned into 0, but we account for that by not using Teddy if any of the string values contain a 0.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Sse2))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        public static Vector128<byte> LoadAndPack16AsciiChars(ref char source)
        {
            Vector128<ushort> source0 = Vector128.LoadUnsafe(ref source);
            Vector128<ushort> source1 = Vector128.LoadUnsafe(ref source, (nuint)Vector128<ushort>.Count);

            return Sse2.IsSupported
                ? Sse2.PackUnsignedSaturate(source0.AsInt16(), source1.AsInt16())
                : AdvSimd.ExtractNarrowingSaturateUpper(AdvSimd.ExtractNarrowingSaturateLower(source0), source1);
        }

        // Read two Vector512<ushort> and concatenate their lower bytes together into a single Vector512<byte>.
        // Characters above 32767 are turned into 0, but we account for that by not using Teddy if any of the string values contain a 0.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        public static Vector256<byte> LoadAndPack32AsciiChars(ref char source)
        {
            Vector256<ushort> source0 = Vector256.LoadUnsafe(ref source);
            Vector256<ushort> source1 = Vector256.LoadUnsafe(ref source, (nuint)Vector256<ushort>.Count);

            Vector256<byte> packed = Avx2.PackUnsignedSaturate(source0.AsInt16(), source1.AsInt16());

            return PackedSpanHelpers.FixUpPackedVector256Result(packed);
        }

        // Read two Vector512<ushort> and concatenate their lower bytes together into a single Vector512<byte>.
        // Characters above 32767 are turned into 0, but we account for that by not using Teddy if any of the string values contain a 0.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        public static Vector512<byte> LoadAndPack64AsciiChars(ref char source)
        {
            Vector512<ushort> source0 = Vector512.LoadUnsafe(ref source);
            Vector512<ushort> source1 = Vector512.LoadUnsafe(ref source, (nuint)Vector512<ushort>.Count);

            Vector512<byte> packed = Avx512BW.PackUnsignedSaturate(source0.AsInt16(), source1.AsInt16());

            return PackedSpanHelpers.FixUpPackedVector512Result(packed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        private static (Vector128<byte> Low, Vector128<byte> High) GetNibbles(Vector128<byte> input)
        {
            // 'low' is not strictly correct here, but we take advantage of Ssse3.Shuffle's behavior
            // of doing an implicit 'AND 0xF' in order to skip the redundant AND.
            Vector128<byte> low = Ssse3.IsSupported
                ? input
                : input & Vector128.Create((byte)0xF);

            Vector128<byte> high = input >>> 4;

            return (low, high);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (Vector256<byte> Low, Vector256<byte> High) GetNibbles(Vector256<byte> input)
        {
            // 'low' is not strictly correct here, but we take advantage of Avx2.Shuffle's behavior
            // of doing an implicit 'AND 0xF' in order to skip the redundant AND.
            Vector256<byte> low = input;

            Vector256<byte> high = input >>> 4;

            return (low, high);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (Vector512<byte> Low, Vector512<byte> High) GetNibbles(Vector512<byte> input)
        {
            // 'low' is not strictly correct here, but we take advantage of Avx512BW.Shuffle's behavior
            // of doing an implicit 'AND 0xF' in order to skip the redundant AND.
            Vector512<byte> low = input;

            Vector512<byte> high = input >>> 4;

            return (low, high);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private static Vector128<byte> Shuffle(Vector128<byte> maskLow, Vector128<byte> maskHigh, Vector128<byte> low, Vector128<byte> high)
        {
            return Vector128.ShuffleUnsafe(maskLow, low) & Vector128.ShuffleUnsafe(maskHigh, high);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static Vector256<byte> Shuffle(Vector256<byte> maskLow, Vector256<byte> maskHigh, Vector256<byte> low, Vector256<byte> high)
        {
            return Avx2.Shuffle(maskLow, low) & Avx2.Shuffle(maskHigh, high);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        private static Vector512<byte> Shuffle(Vector512<byte> maskLow, Vector512<byte> maskHigh, Vector512<byte> low, Vector512<byte> high)
        {
            return Avx512BW.Shuffle(maskLow, low) & Avx512BW.Shuffle(maskHigh, high);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        private static Vector128<byte> RightShift1(Vector128<byte> left, Vector128<byte> right)
        {
            // Given input vectors like
            // left:   [ 0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15]
            // right:  [16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31]
            // We want to shift the last element of left (15) to be the first element of the result
            // result: [15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30]

            return Ssse3.IsSupported
                ? Ssse3.AlignRight(right, left, 15)
                : AdvSimd.ExtractVector128(left, right, 15);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd))]
        private static Vector128<byte> RightShift2(Vector128<byte> left, Vector128<byte> right)
        {
            // Given input vectors like
            // left:   [ 0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15]
            // right:  [16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31]
            // We want to shift the last two elements of left (14, 15) to be the first elements of the result
            // result: [14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29]

            return Ssse3.IsSupported
                ? Ssse3.AlignRight(right, left, 14)
                : AdvSimd.ExtractVector128(left, right, 14);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static Vector256<byte> RightShift1(Vector256<byte> left, Vector256<byte> right)
        {
            // Given input vectors like
            // left:      0,  1,  2,  3,  4,  5, ... , 26, 27, 28, 29, 30, [31]
            // right:    32, 33, 34, 35, 36, 37, ... , 58, 59, 60, 61, 62, 63
            // We want to shift the last element of left (31) to be the first element of the result
            // result: [31], 32, 33, 34, 35, 36, ... , 57, 58, 59, 60, 61, 62
            //
            // Avx2.AlignRight acts like two separate Ssse3.AlignRight calls on the lower and upper halves of the source operands.
            // Result of Avx2.AlignRight(right, left, 15) is
            // lower: [15], 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46,
            // upper: [31], 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62
            // note how elements at indexes 0 and 16 are off by 16 places.
            // We want to read 31 instead of 15 and 47 instead of 31.
            //
            // To achieve that we create a temporary value where we combine the second half of the first operand and the first half of the second operand (Permute2x128).
            // left:      0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15, [ 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 ] control: (1 << 0)
            // right:  [ 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47 ], 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63   control: (2 << 4)
            // result:   16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, [31], 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, [47]
            // This effectively shifts the 0th and 16th element by 16 places (note values 31 and 47).

            Vector256<byte> leftShifted = Avx2.Permute2x128(left, right, (1 << 0) + (2 << 4));
            return Avx2.AlignRight(right, leftShifted, 15);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static Vector256<byte> RightShift2(Vector256<byte> left, Vector256<byte> right)
        {
            // See comments in 'RightShift1(Vector256<byte> left, Vector256<byte> right)' above.
            Vector256<byte> leftShifted = Avx2.Permute2x128(left, right, (1 << 0) + (2 << 4));
            return Avx2.AlignRight(right, leftShifted, 14);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        private static Vector512<byte> RightShift1(Vector512<byte> left, Vector512<byte> right)
        {
            // Given input vectors like
            // left:     0,   1,   2,   3,   4,   5, ... ,  58,  59,  60,  61,  62, [63]
            // right:   64,  65,  66,  67,  68,  69, ... , 122, 123, 124, 125, 126, 127
            // We want to shift the last element of left (63) to be the first element of the result
            // result: [63], 64,  65,  66,  67,  68, ... , 121, 122, 123, 124, 125, 126
            //
            // Avx512BW.AlignRight acts like four separate Ssse3.AlignRight calls on each 128-bit pair of the of the source operands.
            // Result of Avx512BW.AlignRight(right, left, 15) is
            // lower: [15],  64,  65,  66,  67,  68,  69,  70,  71,  72,  73,  74,  75,  76,  77,  78, [31],  80,  81,  82,  83,  84,  85,  86,  87,  88,  89,  90,  91,  92,  93,  94,
            // upper: [47],  96,  97,  98,  99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, [63], 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126
            // note how elements at indexes 0, 16, 32 and 48 are off by 48 places.
            // We want to read 63 instead of 15, 79 instead of 31, 95 instead of 47, and 111 instead of 63.
            //
            // Similar to Avx2 above, we create a temporary value where we shift these positions by 48 places - shift 8-byte values by 6 places (PermuteVar8x64x2).
            // The indices vector below could be [6, 7, 8, 9, 10, 11, 12, 13], but we only care about the last byte in each 128-bit block (positions with value 0 don't affect the result).

            Vector512<byte> leftShifted = Avx512F.PermuteVar8x64x2(left.AsInt64(), Vector512.Create(0, 7, 0, 9, 0, 11, 0, 13), right.AsInt64()).AsByte();
            return Avx512BW.AlignRight(right, leftShifted, 15);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        private static Vector512<byte> RightShift2(Vector512<byte> left, Vector512<byte> right)
        {
            // See comments in 'RightShift1(Vector512<byte> left, Vector512<byte> right)' above.
            Vector512<byte> leftShifted = Avx512F.PermuteVar8x64x2(left.AsInt64(), Vector512.Create(0, 7, 0, 9, 0, 11, 0, 13), right.AsInt64()).AsByte();
            return Avx512BW.AlignRight(right, leftShifted, 14);
        }
    }
}
