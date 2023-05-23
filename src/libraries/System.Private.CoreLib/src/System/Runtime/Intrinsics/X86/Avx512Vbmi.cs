// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>This class provides access to X86 AVX512VBMI hardware instructions via intrinsics</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Avx512Vbmi : Avx512BW
    {
        internal Avx512Vbmi() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class VL : Avx512BW.VL
        {
            internal VL() { }

            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            /// __m128i _mm_permutevar64x8_epi8 (__m128i a, __m128i b)
            ///   VPERMB xmm1 {k1}{z}, xmm2, xmm3/m128
            /// </summary>
            public static Vector128<sbyte> PermuteVar16x8(Vector128<sbyte> left, Vector128<sbyte> control) => PermuteVar16x8(left, control);
            /// <summary>
            /// __m128i _mm_permutevar64x8_epi8 (__m128i a, __m128i b)
            ///   VPERMB xmm1 {k1}{z}, xmm2, xmm3/m128
            /// </summary>
            public static Vector128<byte> PermuteVar16x8(Vector128<byte> left, Vector128<byte> control) => PermuteVar16x8(left, control);

            /// <summary>
            /// __m128i _mm_permutex2var_epi8 (__m128i a, __m128i idx, __m128i b)
            ///   VPERMI2B xmm1 {k1}{z}, xmm2, xmm3/m128
            ///   VPERMT2B xmm1 {k1}{z}, xmm2, xmm3/m128
            /// </summary>
            public static Vector128<byte> PermuteVar16x8x2(Vector128<byte> lower, Vector128<byte> indices, Vector128<byte> upper) => PermuteVar16x8x2(lower, indices, upper);
            /// <summary>
            /// __m128i _mm_permutex2var_epi8 (__m128i a, __m128i idx, __m128i b)
            ///   VPERMI2B xmm1 {k1}{z}, xmm2, xmm3/m128
            ///   VPERMT2B xmm1 {k1}{z}, xmm2, xmm3/m128
            /// </summary>
            public static Vector128<sbyte> PermuteVar16x8x2(Vector128<sbyte> lower, Vector128<sbyte> indices, Vector128<sbyte> upper) => PermuteVar16x8x2(lower, indices, upper);

            /// <summary>
            /// __m256i _mm256_permutevar64x8_epi8 (__m256i a, __m256i b)
            ///   VPERMB ymm1 {k1}{z}, ymm2, ymm3/m256
            /// </summary>
            public static Vector256<sbyte> PermuteVar32x8(Vector256<sbyte> left, Vector256<sbyte> control) => PermuteVar32x8(left, control);
            /// <summary>
            /// __m256i _mm256_permutevar64x8_epi8 (__m256i a, __m256i b)
            ///   VPERMB ymm1 {k1}{z}, ymm2, ymm3/m256
            /// </summary>
            public static Vector256<byte> PermuteVar32x8(Vector256<byte> left, Vector256<byte> control) => PermuteVar32x8(left, control);

            /// <summary>
            /// __m256i _mm256_permutex2var_epi8 (__m256i a, __m256i idx, __m256i b)
            ///   VPERMI2B ymm1 {k1}{z}, ymm2, ymm3/m256
            ///   VPERMT2B ymm1 {k1}{z}, ymm2, ymm3/m256
            /// </summary>
            public static Vector256<byte> PermuteVar32x8x2(Vector256<byte> lower, Vector256<byte> indices, Vector256<byte> upper) => PermuteVar32x8x2(lower, indices, upper);
            /// <summary>
            /// __m256i _mm256_permutex2var_epi8 (__m256i a, __m256i idx, __m256i b)
            ///   VPERMI2B ymm1 {k1}{z}, ymm2, ymm3/m256
            ///   VPERMT2B ymm1 {k1}{z}, ymm2, ymm3/m256
            /// </summary>
            public static Vector256<sbyte> PermuteVar32x8x2(Vector256<sbyte> lower, Vector256<sbyte> indices, Vector256<sbyte> upper) => PermuteVar32x8x2(lower, indices, upper);
        }

        [Intrinsic]
        public new abstract class X64 : Avx512BW.X64
        {
            internal X64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        /// <summary>
        /// __m512i _mm512_permutevar64x8_epi8 (__m512i a, __m512i b)
        ///   VPERMB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<sbyte> PermuteVar64x8(Vector512<sbyte> left, Vector512<sbyte> control) => PermuteVar64x8(left, control);
        /// <summary>
        /// __m512i _mm512_permutevar64x8_epi8 (__m512i a, __m512i b)
        ///   VPERMB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<byte> PermuteVar64x8(Vector512<byte> left, Vector512<byte> control) => PermuteVar64x8(left, control);

        /// <summary>
        /// __m512i _mm512_permutex2var_epi8 (__m512i a, __m512i idx, __m512i b)
        ///   VPERMI2B zmm1 {k1}{z}, zmm2, zmm3/m512
        ///   VPERMT2B zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<byte> PermuteVar64x8x2(Vector512<byte> lower, Vector512<byte> indices, Vector512<byte> upper) => PermuteVar64x8x2(lower, indices, upper);
        /// <summary>
        /// __m512i _mm512_permutex2var_epi8 (__m512i a, __m512i idx, __m512i b)
        ///   VPERMI2B zmm1 {k1}{z}, zmm2, zmm3/m512
        ///   VPERMT2B zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<sbyte> PermuteVar64x8x2(Vector512<sbyte> lower, Vector512<sbyte> indices, Vector512<sbyte> upper) => PermuteVar64x8x2(lower, indices, upper);
    }
}
