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
    /// </summary>
    /// <remarks>
    /// TODO: Reworded explanation of how the algorithm works.
    /// https://github.com/jneem/teddy#teddy-1 is a good starting point.
    /// </remarks>
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
            (Vector128<byte> low, Vector128<byte> high) = GetNibbles(input);

            Vector128<byte> match0 = Shuffle(n0Low, n0High, low, high);
            Vector128<byte> result1 = Shuffle(n1Low, n1High, low, high);

            Vector128<byte> result0 = RightShift1(prev0, match0);

            Vector128<byte> result = result0 & result1;

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
            (Vector128<byte> low, Vector128<byte> high) = GetNibbles(input);

            Vector128<byte> match0 = Shuffle(n0Low, n0High, low, high);
            Vector128<byte> match1 = Shuffle(n1Low, n1High, low, high);
            Vector128<byte> result2 = Shuffle(n2Low, n2High, low, high);

            Vector128<byte> result0 = RightShift2(prev0, match0);
            Vector128<byte> result1 = RightShift1(prev1, match1);

            Vector128<byte> result = result0 & result1 & result2;

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
            (Vector512<byte> low, Vector512<byte> high) = GetNibbles(input);

            Vector512<byte> match0 = Shuffle(n0Low, n0High, low, high);
            Vector512<byte> match1 = Shuffle(n1Low, n1High, low, high);
            Vector512<byte> result2 = Shuffle(n2Low, n2High, low, high);

            Vector512<byte> result0 = RightShift2(prev0, match0);
            Vector512<byte> result1 = RightShift1(prev1, match1);

            Vector512<byte> result = result0 & result1 & result2;

            return (result, match0, match1);
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        public static Vector256<byte> LoadAndPack32AsciiChars(ref char source)
        {
            Vector256<ushort> source0 = Vector256.LoadUnsafe(ref source);
            Vector256<ushort> source1 = Vector256.LoadUnsafe(ref source, (nuint)Vector256<ushort>.Count);

            Vector256<byte> packed = Avx2.PackUnsignedSaturate(source0.AsInt16(), source1.AsInt16());

            return PackedSpanHelpers.FixUpPackedVector256Result(packed);
        }

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

            // X86 doesn't have a logical right shift intrinsic for bytes: https://github.com/dotnet/runtime/issues/82564
            Vector128<byte> high = AdvSimd.IsSupported
                ? AdvSimd.ShiftRightLogical(input, 4)
                : (input.AsInt32() >>> 4).AsByte() & Vector128.Create((byte)0xF);

            return (low, high);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (Vector256<byte> Low, Vector256<byte> High) GetNibbles(Vector256<byte> input)
        {
            // 'low' is not strictly correct here, but we take advantage of Avx2.Shuffle's behavior
            // of doing an implicit 'AND 0xF' in order to skip the redundant AND.
            Vector256<byte> low = input;

            // X86 doesn't have a logical right shift intrinsic for bytes: https://github.com/dotnet/runtime/issues/82564
            Vector256<byte> high = (input.AsInt32() >>> 4).AsByte() & Vector256.Create((byte)0xF);

            return (low, high);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (Vector512<byte> Low, Vector512<byte> High) GetNibbles(Vector512<byte> input)
        {
            // 'low' is not strictly correct here, but we take advantage of Avx512BW.Shuffle's behavior
            // of doing an implicit 'AND 0xF' in order to skip the redundant AND.
            Vector512<byte> low = input;

            // X86 doesn't have a logical right shift intrinsic for bytes: https://github.com/dotnet/runtime/issues/82564
            Vector512<byte> high = (input.AsInt32() >>> 4).AsByte() & Vector512.Create((byte)0xF);

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
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private static Vector128<byte> RightShift1(Vector128<byte> left, Vector128<byte> right)
        {
            // Given input vectors like
            // left:   [ 0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15]
            // right:  [16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31]
            // We want to shift the last element of left (15) to be the first element of the result
            // result: [15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30]

            if (Ssse3.IsSupported)
            {
                return Ssse3.AlignRight(right, left, 15);
            }
            else
            {
                Vector128<byte> leftShifted = Vector128.Shuffle(left, Vector128.Create(15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).AsByte());
                return AdvSimd.Arm64.VectorTableLookupExtension(leftShifted, right, Vector128.Create(0xFF, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private static Vector128<byte> RightShift2(Vector128<byte> left, Vector128<byte> right)
        {
            // Given input vectors like
            // left:   [ 0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15]
            // right:  [16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31]
            // We want to shift the last two elements of left (14, 15) to be the first elements of the result
            // result: [14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29]

            if (Ssse3.IsSupported)
            {
                return Ssse3.AlignRight(right, left, 14);
            }
            else
            {
                Vector128<byte> leftShifted = Vector128.Shuffle(left, Vector128.Create(14, 15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).AsByte());
                return AdvSimd.Arm64.VectorTableLookupExtension(leftShifted, right, Vector128.Create(0xFF, 0xFF, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13));
            }
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
