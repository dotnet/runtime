// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 SSSE3 hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Ssse3 : Sse3
    {
        internal Ssse3() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the x86 SSSE3 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : Sse3.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }
        }

        /// <summary>
        ///   <para>__m128i _mm_abs_epi8 (__m128i a)</para>
        ///   <para>   PABSB xmm1,         xmm2/m128</para>
        ///   <para>  VPABSB xmm1,         xmm2/m128</para>
        ///   <para>  VPABSB xmm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector128<byte> Abs(Vector128<sbyte> value) => Abs(value);
        /// <summary>
        ///   <para>__m128i _mm_abs_epi16 (__m128i a)</para>
        ///   <para>   PABSW xmm1,         xmm2/m128</para>
        ///   <para>  VPABSW xmm1,         xmm2/m128</para>
        ///   <para>  VPABSW xmm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector128<ushort> Abs(Vector128<short> value) => Abs(value);
        /// <summary>
        ///   <para>__m128i _mm_abs_epi32 (__m128i a)</para>
        ///   <para>   PABSD xmm1,         xmm2/m128</para>
        ///   <para>  VPABSD xmm1,         xmm2/m128</para>
        ///   <para>  VPABSD xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> Abs(Vector128<int> value) => Abs(value);

        /// <summary>
        ///   <para>__m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)</para>
        ///   <para>   PALIGNR xmm1,               xmm2/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1,         xmm2, xmm3/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector128<sbyte> AlignRight(Vector128<sbyte> left, Vector128<sbyte> right, [ConstantExpected] byte mask) => AlignRight(left, right, mask);
        /// <summary>
        ///   <para>__m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)</para>
        ///   <para>   PALIGNR xmm1,               xmm2/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1,         xmm2, xmm3/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector128<byte> AlignRight(Vector128<byte> left, Vector128<byte> right, [ConstantExpected] byte mask) => AlignRight(left, right, mask);
        /// <summary>
        ///   <para>__m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)</para>
        ///   <para>   PALIGNR xmm1,               xmm2/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1,         xmm2, xmm3/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
        ///   <para>This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<short> AlignRight(Vector128<short> left, Vector128<short> right, [ConstantExpected] byte mask) => AlignRight(left, right, mask);
        /// <summary>
        ///   <para>__m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)</para>
        ///   <para>   PALIGNR xmm1,               xmm2/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1,         xmm2, xmm3/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
        ///   <para>This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<ushort> AlignRight(Vector128<ushort> left, Vector128<ushort> right, [ConstantExpected] byte mask) => AlignRight(left, right, mask);
        /// <summary>
        ///   <para>__m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)</para>
        ///   <para>   PALIGNR xmm1,               xmm2/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1,         xmm2, xmm3/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
        ///   <para>This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<int> AlignRight(Vector128<int> left, Vector128<int> right, [ConstantExpected] byte mask) => AlignRight(left, right, mask);
        /// <summary>
        ///   <para>__m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)</para>
        ///   <para>   PALIGNR xmm1,               xmm2/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1,         xmm2, xmm3/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
        ///   <para>This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<uint> AlignRight(Vector128<uint> left, Vector128<uint> right, [ConstantExpected] byte mask) => AlignRight(left, right, mask);
        /// <summary>
        ///   <para>__m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)</para>
        ///   <para>   PALIGNR xmm1,               xmm2/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1,         xmm2, xmm3/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
        ///   <para>This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<long> AlignRight(Vector128<long> left, Vector128<long> right, [ConstantExpected] byte mask) => AlignRight(left, right, mask);
        /// <summary>
        ///   <para>__m128i _mm_alignr_epi8 (__m128i a, __m128i b, int count)</para>
        ///   <para>   PALIGNR xmm1,               xmm2/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1,         xmm2, xmm3/m128, imm8</para>
        ///   <para>  VPALIGNR xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
        ///   <para>This intrinsic generates PALIGNR that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<ulong> AlignRight(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected] byte mask) => AlignRight(left, right, mask);

        /// <summary>
        ///   <para>__m128i _mm_hadd_epi16 (__m128i a, __m128i b)</para>
        ///   <para>   PHADDW xmm1,       xmm2/m128</para>
        ///   <para>  VPHADDW xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> HorizontalAdd(Vector128<short> left, Vector128<short> right) => HorizontalAdd(left, right);
        /// <summary>
        ///   <para>__m128i _mm_hadd_epi32 (__m128i a, __m128i b)</para>
        ///   <para>   PHADDD xmm1,       xmm2/m128</para>
        ///   <para>  VPHADDD xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<int> HorizontalAdd(Vector128<int> left, Vector128<int> right) => HorizontalAdd(left, right);

        /// <summary>
        ///   <para>__m128i _mm_hadds_epi16 (__m128i a, __m128i b)</para>
        ///   <para>   PHADDSW xmm1,       xmm2/m128</para>
        ///   <para>  VPHADDSW xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> HorizontalAddSaturate(Vector128<short> left, Vector128<short> right) => HorizontalAddSaturate(left, right);

        /// <summary>
        ///   <para>__m128i _mm_hsub_epi16 (__m128i a, __m128i b)</para>
        ///   <para>   PHSUBW xmm1,       xmm2/m128</para>
        ///   <para>  VPHSUBW xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> HorizontalSubtract(Vector128<short> left, Vector128<short> right) => HorizontalSubtract(left, right);
        /// <summary>
        ///   <para>__m128i _mm_hsub_epi32 (__m128i a, __m128i b)</para>
        ///   <para>   PHSUBD xmm1,       xmm2/m128</para>
        ///   <para>  VPHSUBD xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<int> HorizontalSubtract(Vector128<int> left, Vector128<int> right) => HorizontalSubtract(left, right);

        /// <summary>
        ///   <para>__m128i _mm_hsubs_epi16 (__m128i a, __m128i b)</para>
        ///   <para>   PHSUBSW xmm1,       xmm2/m128</para>
        ///   <para>  VPHSUBSW xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> HorizontalSubtractSaturate(Vector128<short> left, Vector128<short> right) => HorizontalSubtractSaturate(left, right);

        /// <summary>
        ///   <para>__m128i _mm_maddubs_epi16 (__m128i a, __m128i b)</para>
        ///   <para>   PMADDUBSW xmm1,               xmm2/m128</para>
        ///   <para>  VPMADDUBSW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMADDUBSW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> MultiplyAddAdjacent(Vector128<byte> left, Vector128<sbyte> right) => MultiplyAddAdjacent(left, right);

        /// <summary>
        ///   <para>__m128i _mm_mulhrs_epi16 (__m128i a, __m128i b)</para>
        ///   <para>   PMULHRSW xmm1,               xmm2/m128</para>
        ///   <para>  VPMULHRSW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMULHRSW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> MultiplyHighRoundScale(Vector128<short> left, Vector128<short> right) => MultiplyHighRoundScale(left, right);

        /// <summary>
        ///   <para>__m128i _mm_shuffle_epi8 (__m128i a, __m128i b)</para>
        ///   <para>   PSHUFB xmm1,               xmm2/m128</para>
        ///   <para>  VPSHUFB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSHUFB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> Shuffle(Vector128<sbyte> value, Vector128<sbyte> mask) => Shuffle(value, mask);
        /// <summary>
        ///   <para>__m128i _mm_shuffle_epi8 (__m128i a, __m128i b)</para>
        ///   <para>   PSHUFB xmm1,               xmm2/m128</para>
        ///   <para>  VPSHUFB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSHUFB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> Shuffle(Vector128<byte> value, Vector128<byte> mask) => Shuffle(value, mask);

        /// <summary>
        ///   <para>__m128i _mm_sign_epi8 (__m128i a, __m128i b)</para>
        ///   <para>   PSIGNB xmm1,       xmm2/m128</para>
        ///   <para>  VPSIGNB xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> Sign(Vector128<sbyte> left, Vector128<sbyte> right) => Sign(left, right);
        /// <summary>
        ///   <para>__m128i _mm_sign_epi16 (__m128i a, __m128i b)</para>
        ///   <para>   PSIGNW xmm1,       xmm2/m128</para>
        ///   <para>  VPSIGNW xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> Sign(Vector128<short> left, Vector128<short> right) => Sign(left, right);
        /// <summary>
        ///   <para>__m128i _mm_sign_epi32 (__m128i a, __m128i b)</para>
        ///   <para>   PSIGND xmm1,       xmm2/m128</para>
        ///   <para>  VPSIGND xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<int> Sign(Vector128<int> left, Vector128<int> right) => Sign(left, right);
    }
}
