// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel SSSE3 hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public abstract class Ssse3 : Sse3
    {
        internal Ssse3() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        public new abstract class X64 : Sse3.X64
        {
            internal X64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        /// __m128i _mm_abs_epi8 (__m128i a)
        ///    PABSB xmm1,         xmm2/m128
        ///   VPABSB xmm1,         xmm2/m128
        ///   VPABSB xmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector128<byte> Abs(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_abs_epi16 (__m128i a)
        ///    PABSW xmm1,         xmm2/m128
        ///   VPABSW xmm1,         xmm2/m128
        ///   VPABSW xmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector128<ushort> Abs(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_abs_epi32 (__m128i a)
        ///    PABSD xmm1,         xmm2/m128
        ///   VPABSD xmm1,         xmm2/m128
        ///   VPABSD xmm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector128<uint> Abs(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///    PALIGNR xmm1,               xmm2/m128, imm8
        ///   VPALIGNR xmm1,         xmm2, xmm3/m128, imm8
        ///   VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector128<sbyte> AlignRight(Vector128<sbyte> left, Vector128<sbyte> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///    PALIGNR xmm1,               xmm2/m128, imm8
        ///   VPALIGNR xmm1,         xmm2, xmm3/m128, imm8
        ///   VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector128<byte> AlignRight(Vector128<byte> left, Vector128<byte> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///    PALIGNR xmm1,               xmm2/m128, imm8
        ///   VPALIGNR xmm1,         xmm2, xmm3/m128, imm8
        ///   VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
        /// This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector128<short> AlignRight(Vector128<short> left, Vector128<short> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///    PALIGNR xmm1,               xmm2/m128, imm8
        ///   VPALIGNR xmm1,         xmm2, xmm3/m128, imm8
        ///   VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
        /// This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector128<ushort> AlignRight(Vector128<ushort> left, Vector128<ushort> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///    PALIGNR xmm1,               xmm2/m128, imm8
        ///   VPALIGNR xmm1,         xmm2, xmm3/m128, imm8
        ///   VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
        /// This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector128<int> AlignRight(Vector128<int> left, Vector128<int> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///    PALIGNR xmm1,               xmm2/m128, imm8
        ///   VPALIGNR xmm1,         xmm2, xmm3/m128, imm8
        ///   VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
        /// This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector128<uint> AlignRight(Vector128<uint> left, Vector128<uint> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///    PALIGNR xmm1,               xmm2/m128, imm8
        ///   VPALIGNR xmm1,         xmm2, xmm3/m128, imm8
        ///   VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
        /// This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector128<long> AlignRight(Vector128<long> left, Vector128<long> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)
        ///    PALIGNR xmm1,               xmm2/m128, imm8
        ///   VPALIGNR xmm1,         xmm2, xmm3/m128, imm8
        ///   VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector128<ulong> AlignRight(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_hadd_epi16 (__m128i a, __m128i b)
        ///    PHADDW xmm1,       xmm2/m128
        ///   VPHADDW xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<short> HorizontalAdd(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_hadd_epi32 (__m128i a, __m128i b)
        ///    PHADDD xmm1,       xmm2/m128
        ///   VPHADDD xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<int> HorizontalAdd(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_hadds_epi16 (__m128i a, __m128i b)
        ///    PHADDSW xmm1,       xmm2/m128
        ///   VPHADDSW xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<short> HorizontalAddSaturate(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_hsub_epi16 (__m128i a, __m128i b)
        ///    PHSUBW xmm1,       xmm2/m128
        ///   VPHSUBW xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<short> HorizontalSubtract(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_hsub_epi32 (__m128i a, __m128i b)
        ///    PHSUBD xmm1,       xmm2/m128
        ///   VPHSUBD xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<int> HorizontalSubtract(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_hsubs_epi16 (__m128i a, __m128i b)
        ///    PHSUBSW xmm1,       xmm2/m128
        ///   VPHSUBSW xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<short> HorizontalSubtractSaturate(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_maddubs_epi16 (__m128i a, __m128i b)
        ///    PMADDUBSW xmm1,               xmm2/m128
        ///   VPMADDUBSW xmm1,         xmm2, xmm3/m128
        ///   VPMADDUBSW xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<short> MultiplyAddAdjacent(Vector128<byte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_mulhrs_epi16 (__m128i a, __m128i b)
        ///    PMULHRSW xmm1,               xmm2/m128
        ///   VPMULHRSW xmm1,         xmm2, xmm3/m128
        ///   VPMULHRSW xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<short> MultiplyHighRoundScale(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_shuffle_epi8 (__m128i a, __m128i b)
        ///    PSHUFB xmm1,               xmm2/m128
        ///   VPSHUFB xmm1,         xmm2, xmm3/m128
        ///   VPSHUFB xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<sbyte> Shuffle(Vector128<sbyte> value, Vector128<sbyte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_shuffle_epi8 (__m128i a, __m128i b)
        ///    PSHUFB xmm1,               xmm2/m128
        ///   VPSHUFB xmm1,         xmm2, xmm3/m128
        ///   VPSHUFB xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<byte> Shuffle(Vector128<byte> value, Vector128<byte> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_sign_epi8 (__m128i a, __m128i b)
        ///    PSIGNB xmm1,       xmm2/m128
        ///   VPSIGNB xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<sbyte> Sign(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sign_epi16 (__m128i a, __m128i b)
        ///    PSIGNW xmm1,       xmm2/m128
        ///   VPSIGNW xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<short> Sign(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sign_epi32 (__m128i a, __m128i b)
        ///    PSIGND xmm1,       xmm2/m128
        ///   VPSIGND xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<int> Sign(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
    }
}
