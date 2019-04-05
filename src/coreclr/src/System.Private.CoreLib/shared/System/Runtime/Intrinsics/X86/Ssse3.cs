// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel SSSE3 hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Ssse3 : Sse3
    {
        internal Ssse3() { }

        public new static bool IsSupported { get => IsSupported; }

        /// <summary>
        /// __m128i _mm_abs_epi8 (__m128i a)
        ///   PABSB xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> Abs(Vector128<sbyte> value) => Abs(value);
        /// <summary>
        /// __m128i _mm_abs_epi16 (__m128i a)
        ///   PABSW xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> Abs(Vector128<short> value) => Abs(value);
        /// <summary>
        /// __m128i _mm_abs_epi32 (__m128i a)
        ///   PABSD xmm, xmm/m128
        /// </summary>
        public static Vector128<uint> Abs(Vector128<int> value) => Abs(value);

        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///   PALIGNR xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<sbyte> AlignRight(Vector128<sbyte> left, Vector128<sbyte> right, byte mask) => AlignRight(left, right, mask);

        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///   PALIGNR xmm, xmm/m128, imm8
        /// This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector128<byte> AlignRight(Vector128<byte> left, Vector128<byte> right, byte mask) => AlignRight(left, right, mask);

        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///   PALIGNR xmm, xmm/m128, imm8
        /// This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector128<short> AlignRight(Vector128<short> left, Vector128<short> right, byte mask) => AlignRight(left, right, mask);

        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///   PALIGNR xmm, xmm/m128, imm8
        /// This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector128<ushort> AlignRight(Vector128<ushort> left, Vector128<ushort> right, byte mask) => AlignRight(left, right, mask);

        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///   PALIGNR xmm, xmm/m128, imm8
        /// This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector128<int> AlignRight(Vector128<int> left, Vector128<int> right, byte mask) => AlignRight(left, right, mask);

        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///   PALIGNR xmm, xmm/m128, imm8
        /// This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector128<uint> AlignRight(Vector128<uint> left, Vector128<uint> right, byte mask) => AlignRight(left, right, mask);

        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///   PALIGNR xmm, xmm/m128, imm8
        /// This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector128<long> AlignRight(Vector128<long> left, Vector128<long> right, byte mask) => AlignRight(left, right, mask);

        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///   PALIGNR xmm, xmm/m128, imm8
        /// This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector128<ulong> AlignRight(Vector128<ulong> left, Vector128<ulong> right, byte mask) => AlignRight(left, right, mask);

        /// <summary>
        /// __m128i _mm_hadd_epi16 (__m128i a, __m128i b)
        ///   PHADDW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> HorizontalAdd(Vector128<short> left, Vector128<short> right) => HorizontalAdd(left, right);
        /// <summary>
        /// __m128i _mm_hadd_epi32 (__m128i a, __m128i b)
        ///   PHADDD xmm, xmm/m128
        /// </summary>
        public static Vector128<int> HorizontalAdd(Vector128<int> left, Vector128<int> right) => HorizontalAdd(left, right);

        /// <summary>
        /// __m128i _mm_hadds_epi16 (__m128i a, __m128i b)
        ///   PHADDSW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> HorizontalAddSaturate(Vector128<short> left, Vector128<short> right) => HorizontalAddSaturate(left, right);

        /// <summary>
        /// __m128i _mm_hsub_epi16 (__m128i a, __m128i b)
        ///   PHSUBW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> HorizontalSubtract(Vector128<short> left, Vector128<short> right) => HorizontalSubtract(left, right);
        /// <summary>
        /// __m128i _mm_hsub_epi32 (__m128i a, __m128i b)
        ///   PHSUBD xmm, xmm/m128
        /// </summary>
        public static Vector128<int> HorizontalSubtract(Vector128<int> left, Vector128<int> right) => HorizontalSubtract(left, right);

        /// <summary>
        /// __m128i _mm_hsubs_epi16 (__m128i a, __m128i b)
        ///   PHSUBSW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> HorizontalSubtractSaturate(Vector128<short> left, Vector128<short> right) => HorizontalSubtractSaturate(left, right);

        /// <summary>
        /// __m128i _mm_maddubs_epi16 (__m128i a, __m128i b)
        ///   PMADDUBSW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> MultiplyAddAdjacent(Vector128<byte> left, Vector128<sbyte> right) => MultiplyAddAdjacent(left, right);

        /// <summary>
        /// __m128i _mm_mulhrs_epi16 (__m128i a, __m128i b)
        ///   PMULHRSW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> MultiplyHighRoundScale(Vector128<short> left, Vector128<short> right) => MultiplyHighRoundScale(left, right);

        /// <summary>
        /// __m128i _mm_shuffle_epi8 (__m128i a, __m128i b)
        ///   PSHUFB xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> Shuffle(Vector128<sbyte> value, Vector128<sbyte> mask) => Shuffle(value, mask);

        /// <summary>
        /// __m128i _mm_shuffle_epi8 (__m128i a, __m128i b)
        ///   PSHUFB xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> Shuffle(Vector128<byte> value, Vector128<byte> mask) => Shuffle(value, mask);

        /// <summary>
        /// __m128i _mm_sign_epi8 (__m128i a, __m128i b)
        ///   PSIGNB xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> Sign(Vector128<sbyte> left, Vector128<sbyte> right) => Sign(left, right);
        /// <summary>
        /// __m128i _mm_sign_epi16 (__m128i a, __m128i b)
        ///   PSIGNW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> Sign(Vector128<short> left, Vector128<short> right) => Sign(left, right);
        /// <summary>
        /// __m128i _mm_sign_epi32 (__m128i a, __m128i b)
        ///   PSIGND xmm, xmm/m128
        /// </summary>
        public static Vector128<int> Sign(Vector128<int> left, Vector128<int> right) => Sign(left, right);
    }
}
