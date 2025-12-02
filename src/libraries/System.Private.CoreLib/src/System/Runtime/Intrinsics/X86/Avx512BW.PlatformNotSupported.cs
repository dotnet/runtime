// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 AVX512BW hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Avx512BW : Avx512F
    {
        internal Avx512BW() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>Provides access to the x86 AVX512BW+VL hardware instructions via intrinsics.</summary>
        public new abstract class VL : Avx512F.VL
        {
            internal VL() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            ///   <para>__m128i _mm_mask_blendv_epu8 (__m128i a, __m128i b, __mmask16 mask)</para>
            ///   <para>  VPBLENDMB xmm1 {k1}, xmm2, xmm3/m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector128<byte> BlendVariable(Vector128<byte> left, Vector128<byte> right, Vector128<byte> mask) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_blendv_epi16 (__m128i a, __m128i b, __mmask8 mask)</para>
            ///   <para>  VPBLENDMW xmm1 {k1}, xmm2, xmm3/m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector128<short> BlendVariable(Vector128<short> left, Vector128<short> right, Vector128<short> mask) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_blendv_epi8 (__m128i a, __m128i b, __mmask16 mask)</para>
            ///   <para>  VPBLENDMB xmm1 {k1}, xmm2, xmm3/m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector128<sbyte> BlendVariable(Vector128<sbyte> left, Vector128<sbyte> right, Vector128<sbyte> mask) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_blendv_epu16 (__m128i a, __m128i b, __mmask8 mask)</para>
            ///   <para>  VPBLENDMW xmm1 {k1}, xmm2, xmm3/m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector128<ushort> BlendVariable(Vector128<ushort> left, Vector128<ushort> right, Vector128<ushort> mask) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m256i _mm256_mask_blendv_epu8 (__m256i a, __m256i b, __mmask32 mask)</para>
            ///   <para>  VPBLENDMB ymm1 {k1}, ymm2, ymm3/m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<byte> BlendVariable(Vector256<byte> left, Vector256<byte> right, Vector256<byte> mask) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_blendv_epi16 (__m256i a, __m256i b, __mmask16 mask)</para>
            ///   <para>  VPBLENDMW ymm1 {k1}, ymm2, ymm3/m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<short> BlendVariable(Vector256<short> left, Vector256<short> right, Vector256<short> mask) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_blendv_epi8 (__m256i a, __m256i b, __mmask32 mask)</para>
            ///   <para>  VPBLENDMB ymm1 {k1}, ymm2, ymm3/m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<sbyte> BlendVariable(Vector256<sbyte> left, Vector256<sbyte> right, Vector256<sbyte> mask) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_blendv_epu16 (__m256i a, __m256i b, __mmask16 mask)</para>
            ///   <para>  VPBLENDMW ymm1 {k1}, ymm2, ymm3/m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<ushort> BlendVariable(Vector256<ushort> left, Vector256<ushort> right, Vector256<ushort> mask) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__mmask16 _mm_cmpeq_epu8_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(0)</para>
            /// </summary>
            public static Vector128<byte> CompareEqual(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm_cmpgt_epu8_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(6)</para>
            /// </summary>
            public static Vector128<byte> CompareGreaterThan(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm_cmpge_epu8_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(5)</para>
            /// </summary>
            public static Vector128<byte> CompareGreaterThanOrEqual(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm_cmplt_epu8_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(1)</para>
            /// </summary>
            public static Vector128<byte> CompareLessThan(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm_cmple_epu8_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(2)</para>
            /// </summary>
            public static Vector128<byte> CompareLessThanOrEqual(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm_cmpne_epu8_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(4)</para>
            /// </summary>
            public static Vector128<byte> CompareNotEqual(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__mmask32 _mm256_cmpeq_epu8_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(0)</para>
            /// </summary>
            public static Vector256<byte> CompareEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask32 _mm256_cmpgt_epu8_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(6)</para>
            /// </summary>
            public static Vector256<byte> CompareGreaterThan(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask32 _mm256_cmpge_epu8_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(5)</para>
            /// </summary>
            public static Vector256<byte> CompareGreaterThanOrEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask32 _mm256_cmplt_epu8_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(1)</para>
            /// </summary>
            public static Vector256<byte> CompareLessThan(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask32 _mm256_cmple_epu8_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(2)</para>
            /// </summary>
            public static Vector256<byte> CompareLessThanOrEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask32 _mm256_cmpne_epu8_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(4)</para>
            /// </summary>
            public static Vector256<byte> CompareNotEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__mmask8 _mm_cmpeq_epi16_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(0)</para>
            /// </summary>
            public static Vector128<short> CompareEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask8 _mm_cmpgt_epi16_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(6)</para>
            /// </summary>
            public static Vector128<short> CompareGreaterThan(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask8 _mm_cmpge_epi16_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(5)</para>
            /// </summary>
            public static Vector128<short> CompareGreaterThanOrEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask8 _mm_cmplt_epi16_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(1)</para>
            /// </summary>
            public static Vector128<short> CompareLessThan(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask8 _mm_cmple_epi16_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(2)</para>
            /// </summary>
            public static Vector128<short> CompareLessThanOrEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask8 _mm_cmpne_epi16_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(4)</para>
            /// </summary>
            public static Vector128<short> CompareNotEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__mmask16 _mm256_cmpeq_epi16_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(0)</para>
            /// </summary>
            public static Vector256<short> CompareEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm256_cmpgt_epi16_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(6)</para>
            /// </summary>
            public static Vector256<short> CompareGreaterThan(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm256_cmpge_epi16_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(5)</para>
            /// </summary>
            public static Vector256<short> CompareGreaterThanOrEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm256_cmplt_epi16_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(1)</para>
            /// </summary>
            public static Vector256<short> CompareLessThan(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm256_cmple_epi16_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(2)</para>
            /// </summary>
            public static Vector256<short> CompareLessThanOrEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm256_cmpne_epi16_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(4)</para>
            /// </summary>
            public static Vector256<short> CompareNotEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__mmask16 _mm_cmpeq_epi8_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(0)</para>
            /// </summary>
            public static Vector128<sbyte> CompareEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm_cmpgt_epi8_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(6)</para>
            /// </summary>
            public static Vector128<sbyte> CompareGreaterThan(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm_cmpge_epi8_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(5)</para>
            /// </summary>
            public static Vector128<sbyte> CompareGreaterThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm_cmplt_epi8_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(1)</para>
            /// </summary>
            public static Vector128<sbyte> CompareLessThan(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm_cmple_epi8_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(2)</para>
            /// </summary>
            public static Vector128<sbyte> CompareLessThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm_cmpne_epi8_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(4)</para>
            /// </summary>
            public static Vector128<sbyte> CompareNotEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__mmask32 _mm256_cmpeq_epi8_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(0)</para>
            /// </summary>
            public static Vector256<sbyte> CompareEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask32 _mm256_cmpgt_epi8_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(6)</para>
            /// </summary>
            public static Vector256<sbyte> CompareGreaterThan(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask32 _mm256_cmpge_epi8_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(5)</para>
            /// </summary>
            public static Vector256<sbyte> CompareGreaterThanOrEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask32 _mm256_cmplt_epi8_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(1)</para>
            /// </summary>
            public static Vector256<sbyte> CompareLessThan(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask32 _mm256_cmple_epi8_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(2)</para>
            /// </summary>
            public static Vector256<sbyte> CompareLessThanOrEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask32 _mm256_cmpne_epi8_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(4)</para>
            /// </summary>
            public static Vector256<sbyte> CompareNotEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__mmask8 _mm_cmpeq_epu16_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(0)</para>
            /// </summary>
            public static Vector128<ushort> CompareEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask8 _mm_cmpgt_epu16_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(6)</para>
            /// </summary>
            public static Vector128<ushort> CompareGreaterThan(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask8 _mm_cmpge_epu16_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(5)</para>
            /// </summary>
            public static Vector128<ushort> CompareGreaterThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask8 _mm_cmplt_epu16_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(1)</para>
            /// </summary>
            public static Vector128<ushort> CompareLessThan(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask8 _mm_cmple_epu16_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(2)</para>
            /// </summary>
            public static Vector128<ushort> CompareLessThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask8 _mm_cmpne_epu16_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(4)</para>
            /// </summary>
            public static Vector128<ushort> CompareNotEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__mmask16 _mm256_cmpeq_epu16_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(0)</para>
            /// </summary>
            public static Vector256<ushort> CompareEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm256_cmpgt_epu16_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(6)</para>
            /// </summary>
            public static Vector256<ushort> CompareGreaterThan(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm256_cmpge_epu16_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(5)</para>
            /// </summary>
            public static Vector256<ushort> CompareGreaterThanOrEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm256_cmplt_epu16_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(1)</para>
            /// </summary>
            public static Vector256<ushort> CompareLessThan(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm256_cmple_epu16_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(2)</para>
            /// </summary>
            public static Vector256<ushort> CompareLessThanOrEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm256_cmpne_epu16_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(4)</para>
            /// </summary>
            public static Vector256<ushort> CompareNotEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m128i _mm_cvtepi16_epi8 (__m128i a)</para>
            ///   <para>  VPMOVWB xmm1/m64 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector128<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_cvtepi16_epi8 (__m128i a)</para>
            ///   <para>  VPMOVWB xmm1/m64 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi16_epi8 (__m256i a)</para>
            ///   <para>  VPMOVWB xmm1/m128 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector256<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi16_epi8 (__m256i a)</para>
            ///   <para>  VPMOVWB xmm1/m128 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_cvtusepi16_epi8 (__m128i a)</para>
            ///   <para>  VPMOVUWB xmm1/m64 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm256_cvtusepi16_epi8 (__m256i a)</para>
            ///   <para>  VPMOVUWB xmm1/m128 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m128i _mm_cvtepi16_epi8 (__m128i a)</para>
            ///   <para>  VPMOVWB xmm1/m64 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector128<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_cvtepi16_epi8 (__m128i a)</para>
            ///   <para>  VPMOVWB xmm1/m64 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi16_epi8 (__m256i a)</para>
            ///   <para>  VPMOVWB xmm1/m128 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector256<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi16_epi8 (__m256i a)</para>
            ///   <para>  VPMOVWB xmm1/m128 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_cvtsepi16_epi8 (__m128i a)</para>
            ///   <para>  VPMOVSWB xmm1/m64 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector128<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm256_cvtsepi16_epi8 (__m256i a)</para>
            ///   <para>  VPMOVSWB xmm1/m128 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector256<short> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m128i _mm_mask_loadu_epi8 (__m128i s, __mmask16 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU8 xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<byte> MaskLoad(byte* address, Vector128<byte> mask, Vector128<byte> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_loadu_epi16 (__m128i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU32 xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<short> MaskLoad(short* address, Vector128<short> mask, Vector128<short> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_loadu_epi8 (__m128i s, __mmask16 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU8 xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<sbyte> MaskLoad(sbyte* address, Vector128<sbyte> mask, Vector128<sbyte> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_loadu_epi16 (__m128i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU32 xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<ushort> MaskLoad(ushort* address, Vector128<ushort> mask, Vector128<ushort> merge) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m256i _mm256_mask_loadu_epi8 (__m256i s, __mmask32 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU8 ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<byte> MaskLoad(byte* address, Vector256<byte> mask, Vector256<byte> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_loadu_epi16 (__m256i s, __mmask16 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU32 ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<short> MaskLoad(short* address, Vector256<short> mask, Vector256<short> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_loadu_epi8 (__m256i s, __mmask32 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU8 ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<sbyte> MaskLoad(sbyte* address, Vector256<sbyte> mask, Vector256<sbyte> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_loadu_epi16 (__m256i s, __mmask16 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU32 ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<ushort> MaskLoad(ushort* address, Vector256<ushort> mask, Vector256<ushort> merge) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>void _mm_mask_storeu_si128 (void * mem_addr, __mmask16 k, __m128i a)</para>
            ///   <para>  VMOVDQU8 m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStore(byte* address, Vector128<byte> mask, Vector128<byte> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>void _mm_mask_storeu_si128 (void * mem_addr, __mmask8 k, __m128i a)</para>
            ///   <para>  VMOVDQU16 m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStore(short* address, Vector128<short> mask, Vector128<short> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>void _mm_mask_storeu_si128 (void * mem_addr, __mmask16 k, __m128i a)</para>
            ///   <para>  VMOVDQU8 m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStore(sbyte* address, Vector128<sbyte> mask, Vector128<sbyte> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>void _mm_mask_storeu_si128 (void * mem_addr, __mmask8 k, __m128i a)</para>
            ///   <para>  VMOVDQU16 m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStore(ushort* address, Vector128<ushort> mask, Vector128<ushort> source) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>void _mm256_mask_storeu_si256 (void * mem_addr, __mmask32 k, __m256i a)</para>
            ///   <para>  VMOVDQU8 m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStore(byte* address, Vector256<byte> mask, Vector256<byte> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>void _mm256_mask_storeu_si256 (void * mem_addr, __mmask16 k, __m256i a)</para>
            ///   <para>  VMOVDQU16 m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStore(short* address, Vector256<short> mask, Vector256<short> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>void _mm256_mask_storeu_si256 (void * mem_addr, __mmask32 k, __m256i a)</para>
            ///   <para>  VMOVDQU8 m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStore(sbyte* address, Vector256<sbyte> mask, Vector256<sbyte> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>void _mm256_mask_storeu_si256 (void * mem_addr, __mmask16 k, __m256i a)</para>
            ///   <para>  VMOVDQU16 m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStore(ushort* address, Vector256<ushort> mask, Vector256<ushort> source) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m128i _mm_permutexvar_epi16 (__m128i idx, __m128i a)</para>
            ///   <para>  VPERMW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector128<short> PermuteVar8x16(Vector128<short> left, Vector128<short> control) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_permutexvar_epi16 (__m128i idx, __m128i a)</para>
            ///   <para>  VPERMW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector128<ushort> PermuteVar8x16(Vector128<ushort> left, Vector128<ushort> control) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m128i _mm_permutex2var_epi16 (__m128i a, __m128i idx, __m128i b)</para>
            ///   <para>  VPERMI2W xmm1 {k1}{z}, xmm2, xmm3/m128</para>
            ///   <para>  VPERMT2W xmm1 {k1}{z}, xmm2, xmm3/m128</para>
            /// </summary>
            public static Vector128<short> PermuteVar8x16x2(Vector128<short> lower, Vector128<short> indices, Vector128<short> upper) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_permutex2var_epi16 (__m128i a, __m128i idx, __m128i b)</para>
            ///   <para>  VPERMI2W xmm1 {k1}{z}, xmm2, xmm3/m128</para>
            ///   <para>  VPERMT2W xmm1 {k1}{z}, xmm2, xmm3/m128</para>
            /// </summary>
            public static Vector128<ushort> PermuteVar8x16x2(Vector128<ushort> lower, Vector128<ushort> indices, Vector128<ushort> upper) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m256i _mm256_permutexvar_epi16 (__m256i idx, __m256i a)</para>
            ///   <para>  VPERMW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<short> PermuteVar16x16(Vector256<short> left, Vector256<short> control) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_permutexvar_epi16 (__m256i idx, __m256i a)</para>
            ///   <para>  VPERMW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<ushort> PermuteVar16x16(Vector256<ushort> left, Vector256<ushort> control) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m256i _mm256_permutex2var_epi16 (__m256i a, __m256i idx, __m256i b)</para>
            ///   <para>  VPERMI2W ymm1 {k1}{z}, ymm2, ymm3/m256</para>
            ///   <para>  VPERMT2W ymm1 {k1}{z}, ymm2, ymm3/m256</para>
            /// </summary>
            public static Vector256<short> PermuteVar16x16x2(Vector256<short> lower, Vector256<short> indices, Vector256<short> upper) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_permutex2var_epi16 (__m256i a, __m256i idx, __m256i b)</para>
            ///   <para>  VPERMI2W ymm1 {k1}{z}, ymm2, ymm3/m256</para>
            ///   <para>  VPERMT2W ymm1 {k1}{z}, ymm2, ymm3/m256</para>
            /// </summary>
            public static Vector256<ushort> PermuteVar16x16x2(Vector256<ushort> lower, Vector256<ushort> indices, Vector256<ushort> upper) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m128i _mm_sllv_epi16 (__m128i a, __m128i count)</para>
            ///   <para>  VPSLLVW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
            /// </summary>
            public static Vector128<short> ShiftLeftLogicalVariable(Vector128<short> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_sllv_epi16 (__m128i a, __m128i count)</para>
            ///   <para>  VPSLLVW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
            /// </summary>
            public static Vector128<ushort> ShiftLeftLogicalVariable(Vector128<ushort> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_sllv_epi16 (__m256i a, __m256i count)</para>
            ///   <para>  VPSLLVW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
            /// </summary>
            public static Vector256<short> ShiftLeftLogicalVariable(Vector256<short> value, Vector256<ushort> count) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_sllv_epi16 (__m256i a, __m256i count)</para>
            ///   <para>  VPSLLVW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
            /// </summary>
            public static Vector256<ushort> ShiftLeftLogicalVariable(Vector256<ushort> value, Vector256<ushort> count) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m128i _mm_srav_epi16 (__m128i a, __m128i count)</para>
            ///   <para>  VPSRAVW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
            /// </summary>
            public static Vector128<short> ShiftRightArithmeticVariable(Vector128<short> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_srav_epi16 (__m256i a, __m256i count)</para>
            ///   <para>  VPSRAVW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
            /// </summary>
            public static Vector256<short> ShiftRightArithmeticVariable(Vector256<short> value, Vector256<ushort> count) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m128i _mm_srlv_epi16 (__m128i a, __m128i count)</para>
            ///   <para>  VPSRLVW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
            /// </summary>
            public static Vector128<short> ShiftRightLogicalVariable(Vector128<short> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_srlv_epi16 (__m128i a, __m128i count)</para>
            ///   <para>  VPSRLVW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
            /// </summary>
            public static Vector128<ushort> ShiftRightLogicalVariable(Vector128<ushort> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_srlv_epi16 (__m256i a, __m256i count)</para>
            ///   <para>  VPSRLVW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
            /// </summary>
            public static Vector256<short> ShiftRightLogicalVariable(Vector256<short> value, Vector256<ushort> count) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_srlv_epi16 (__m256i a, __m256i count)</para>
            ///   <para>  VPSRLVW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
            /// </summary>
            public static Vector256<ushort> ShiftRightLogicalVariable(Vector256<ushort> value, Vector256<ushort> count) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m128i _mm_dbsad_epu8 (__m128i a, __m128i b, int imm8)</para>
            ///   <para>  VDBPSADBW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
            /// </summary>
            public static Vector128<ushort> SumAbsoluteDifferencesInBlock32(Vector128<byte> left, Vector128<byte> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_dbsad_epu8 (__m256i a, __m256i b, int imm8)</para>
            ///   <para>  VDBPSADBW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
            /// </summary>
            public static Vector256<ushort> SumAbsoluteDifferencesInBlock32(Vector256<byte> left, Vector256<byte> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>Provides access to the x86 AVX512BW hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class X64 : Avx512F.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        ///   <para>__m512i _mm512_abs_epi8 (__m512i a)</para>
        ///   <para>  VPABSB zmm1 {k1}{z}, zmm2/m512</para>
        /// </summary>
        public static Vector512<byte> Abs(Vector512<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_abs_epi16 (__m512i a)</para>
        ///   <para>  VPABSW zmm1 {k1}{z}, zmm2/m512</para>
        /// </summary>
        public static Vector512<ushort> Abs(Vector512<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_add_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPADDB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<sbyte> Add(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_add_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPADDB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<byte> Add(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_add_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPADDW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> Add(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_add_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPADDW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> Add(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_adds_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPADDSB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<sbyte> AddSaturate(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_adds_epu8 (__m512i a, __m512i b)</para>
        ///   <para>  VPADDUSB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<byte> AddSaturate(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_adds_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPADDSW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> AddSaturate(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_adds_epu16 (__m512i a, __m512i b)</para>
        ///   <para>  VPADDUSW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> AddSaturate(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_alignr_epi8 (__m512i a, __m512i b, const int count)</para>
        ///   <para>  VPALIGNR zmm1 {k1}{z}, zmm2, zmm3/m512, imm8</para>
        /// </summary>
        public static Vector512<sbyte> AlignRight(Vector512<sbyte> left, Vector512<sbyte> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_alignr_epi8 (__m512i a, __m512i b, const int count)</para>
        ///   <para>  VPALIGNR zmm1 {k1}{z}, zmm2, zmm3/m512, imm8</para>
        /// </summary>
        public static Vector512<byte> AlignRight(Vector512<byte> left, Vector512<byte> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_avg_epu8 (__m512i a, __m512i b)</para>
        ///   <para>  VPAVGB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<byte> Average(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_avg_epu16 (__m512i a, __m512i b)</para>
        ///   <para>  VPAVGW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> Average(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_mask_blendv_epu8 (__m512i a, __m512i b, __mmask64 mask)</para>
        ///   <para>  VPBLENDMB zmm1 {k1}, zmm2, zmm3/m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector512<byte> BlendVariable(Vector512<byte> left, Vector512<byte> right, Vector512<byte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_blendv_epi16 (__m512i a, __m512i b, __mmask32 mask)</para>
        ///   <para>  VPBLENDMW zmm1 {k1}, zmm2, zmm3/m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector512<short> BlendVariable(Vector512<short> left, Vector512<short> right, Vector512<short> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_blendv_epi8 (__m512i a, __m512i b, __mmask64 mask)</para>
        ///   <para>  VPBLENDMB zmm1 {k1}, zmm2, zmm3/m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector512<sbyte> BlendVariable(Vector512<sbyte> left, Vector512<sbyte> right, Vector512<sbyte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_blendv_epu16 (__m512i a, __m512i b, __mmask32 mask)</para>
        ///   <para>  VPBLENDMW zmm1 {k1}, zmm2, zmm3/m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector512<ushort> BlendVariable(Vector512<ushort> left, Vector512<ushort> right, Vector512<ushort> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_broadcastb_epi8 (__m128i a)</para>
        ///   <para>  VPBROADCASTB zmm1 {k1}{z}, xmm2/m8</para>
        /// </summary>
        public static Vector512<byte> BroadcastScalarToVector512(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_broadcastb_epi8 (__m128i a)</para>
        ///   <para>  VPBROADCASTB zmm1 {k1}{z}, xmm2/m8</para>
        /// </summary>
        public static Vector512<sbyte> BroadcastScalarToVector512(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_broadcastw_epi16 (__m128i a)</para>
        ///   <para>  VPBROADCASTW zmm1 {k1}{z}, xmm2/m16</para>
        /// </summary>
        public static Vector512<short> BroadcastScalarToVector512(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_broadcastw_epi16 (__m128i a)</para>
        ///   <para>  VPBROADCASTW zmm1 {k1}{z}, xmm2/m16</para>
        /// </summary>
        public static Vector512<ushort> BroadcastScalarToVector512(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask64 _mm512_cmpeq_epu8 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPEQB k1 {k2}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<byte> CompareEqual(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask64 _mm512_cmpgt_epu8 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUB k1 {k2}, zmm2, zmm3/m512, imm8(6)</para>
        /// </summary>
        public static Vector512<byte> CompareGreaterThan(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask64 _mm512_cmpge_epu8 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUB k1 {k2}, zmm2, zmm3/m512, imm8(5)</para>
        /// </summary>
        public static Vector512<byte> CompareGreaterThanOrEqual(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask64 _mm512_cmplt_epu8 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUB k1 {k2}, zmm2, zmm3/m512, imm8(1)</para>
        /// </summary>
        public static Vector512<byte> CompareLessThan(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask64 _mm512_cmple_epu8 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUB k1 {k2}, zmm2, zmm3/m512, imm8(2)</para>
        /// </summary>
        public static Vector512<byte> CompareLessThanOrEqual(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask64 _mm512_cmpne_epu8 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUB k1 {k2}, zmm2, zmm3/m512, imm8(4)</para>
        /// </summary>
        public static Vector512<byte> CompareNotEqual(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask32 _mm512_cmpeq_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPEQW k1 {k2}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> CompareEqual(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask32 _mm512_cmpgt_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPGTW k1 {k2}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> CompareGreaterThan(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask32 _mm512_cmpge_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPW k1 {k2}, zmm2, zmm3/m512, imm8(5)</para>
        /// </summary>
        public static Vector512<short> CompareGreaterThanOrEqual(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask32 _mm512_cmplt_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPW k1 {k2}, zmm2, zmm3/m512, imm8(1)</para>
        /// </summary>
        public static Vector512<short> CompareLessThan(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask32 _mm512_cmple_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPW k1 {k2}, zmm2, zmm3/m512, imm8(2)</para>
        /// </summary>
        public static Vector512<short> CompareLessThanOrEqual(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask32 _mm512_cmpne_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPW k1 {k2}, zmm2, zmm3/m512, imm8(4)</para>
        /// </summary>
        public static Vector512<short> CompareNotEqual(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask64 _mm512_cmpeq_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPEQB k1 {k2}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<sbyte> CompareEqual(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask64 _mm512_cmpgt_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPGTB k1 {k2}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<sbyte> CompareGreaterThan(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask64 _mm512_cmpge_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPB k1 {k2}, zmm2, zmm3/m512, imm8(5)</para>
        /// </summary>
        public static Vector512<sbyte> CompareGreaterThanOrEqual(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask64 _mm512_cmplt_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPB k1 {k2}, zmm2, zmm3/m512, imm8(1)</para>
        /// </summary>
        public static Vector512<sbyte> CompareLessThan(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask64 _mm512_cmple_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPB k1 {k2}, zmm2, zmm3/m512, imm8(2)</para>
        /// </summary>
        public static Vector512<sbyte> CompareLessThanOrEqual(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask64 _mm512_cmpne_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPB k1 {k2}, zmm2, zmm3/m512, imm8(4)</para>
        /// </summary>
        public static Vector512<sbyte> CompareNotEqual(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask32 _mm512_cmpeq_epu16 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPEQW k1 {k2}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> CompareEqual(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask32 _mm512_cmpgt_epu16 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUW k1 {k2}, zmm2, zmm3/m512, imm8(6)</para>
        /// </summary>
        public static Vector512<ushort> CompareGreaterThan(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask32 _mm512_cmpge_epu16 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUW k1 {k2}, zmm2, zmm3/m512, imm8(5)</para>
        /// </summary>
        public static Vector512<ushort> CompareGreaterThanOrEqual(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask32 _mm512_cmplt_epu16 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUW k1 {k2}, zmm2, zmm3/m512, imm8(1)</para>
        /// </summary>
        public static Vector512<ushort> CompareLessThan(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask32 _mm512_cmple_epu16 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUW k1 {k2}, zmm2, zmm3/m512, imm8(2)</para>
        /// </summary>
        public static Vector512<ushort> CompareLessThanOrEqual(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask32 _mm512_cmpne_epu16 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUW k1 {k2}, zmm2, zmm3/m512, imm8(4)</para>
        /// </summary>
        public static Vector512<ushort> CompareNotEqual(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm512_cvtepi16_epi8 (__m512i a)</para>
        ///   <para>  VPMOVWB ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<byte> ConvertToVector256Byte(Vector512<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm512_cvtepi16_epi8 (__m512i a)</para>
        ///   <para>  VPMOVWB ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<byte> ConvertToVector256Byte(Vector512<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm512_cvtusepi16_epi8 (__m512i a)</para>
        ///   <para>  VPMOVUWB ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<byte> ConvertToVector256ByteWithSaturation(Vector512<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm512_cvtepi16_epi8 (__m512i a)</para>
        ///   <para>  VPMOVWB ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<sbyte> ConvertToVector256SByte(Vector512<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm512_cvtepi16_epi8 (__m512i a)</para>
        ///   <para>  VPMOVWB ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<sbyte> ConvertToVector256SByte(Vector512<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm512_cvtsepi16_epi8 (__m512i a)</para>
        ///   <para>  VPMOVSWB ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<sbyte> ConvertToVector256SByteWithSaturation(Vector512<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_cvtepi8_epi16 (__m128i a)</para>
        ///   <para>  VPMOVSXBW zmm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector512<short> ConvertToVector512Int16(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_cvtepu8_epi16 (__m128i a)</para>
        ///   <para>  VPMOVZXBW zmm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector512<short> ConvertToVector512Int16(Vector256<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_cvtepi8_epi16 (__m128i a)</para>
        ///   <para>  VPMOVSXBW zmm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector512<ushort> ConvertToVector512UInt16(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_cvtepu8_epi16 (__m128i a)</para>
        ///   <para>  VPMOVZXBW zmm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector512<ushort> ConvertToVector512UInt16(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_loadu_epi8 (void const * mem_addr)</para>
        ///   <para>  VMOVDQU8 zmm1, m512</para>
        /// </summary>
        public static new unsafe Vector512<sbyte> LoadVector512(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_loadu_epi8 (void const * mem_addr)</para>
        ///   <para>  VMOVDQU8 zmm1, m512</para>
        /// </summary>
        public static new unsafe Vector512<byte> LoadVector512(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_loadu_epi16 (void const * mem_addr)</para>
        ///   <para>  VMOVDQU16 zmm1, m512</para>
        /// </summary>
        public static new unsafe Vector512<short> LoadVector512(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_loadu_epi16 (void const * mem_addr)</para>
        ///   <para>  VMOVDQU16 zmm1, m512</para>
        /// </summary>
        public static new unsafe Vector512<ushort> LoadVector512(ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_mask_loadu_epi8 (__m512i s, __mmask64 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU8 zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<byte> MaskLoad(byte* address, Vector512<byte> mask, Vector512<byte> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_loadu_epi16 (__m512i s, __mmask32 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU32 zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<short> MaskLoad(short* address, Vector512<short> mask, Vector512<short> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_loadu_epi8 (__m512i s, __mmask64 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU8 zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<sbyte> MaskLoad(sbyte* address, Vector512<sbyte> mask, Vector512<sbyte> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_loadu_epi16 (__m512i s, __mmask32 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU32 zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<ushort> MaskLoad(ushort* address, Vector512<ushort> mask, Vector512<ushort> merge) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>void _mm512_mask_storeu_si512 (void * mem_addr, __mmask64 k, __m512i a)</para>
        ///   <para>  VMOVDQU8 m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStore(byte* address, Vector512<byte> mask, Vector512<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm512_mask_storeu_si512 (void * mem_addr, __mmask32 k, __m512i a)</para>
        ///   <para>  VMOVDQU16 m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStore(short* address, Vector512<short> mask, Vector512<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm512_mask_storeu_si512 (void * mem_addr, __mmask64 k, __m512i a)</para>
        ///   <para>  VMOVDQU8 m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStore(sbyte* address, Vector512<sbyte> mask, Vector512<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm512_mask_storeu_si512 (void * mem_addr, __mmask32 k, __m512i a)</para>
        ///   <para>  VMOVDQU16 m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStore(ushort* address, Vector512<ushort> mask, Vector512<ushort> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_max_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPMAXSB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<sbyte> Max(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_max_epu8 (__m512i a, __m512i b)</para>
        ///   <para>  VPMAXUB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<byte> Max(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_max_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPMAXSW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> Max(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_max_epu16 (__m512i a, __m512i b)</para>
        ///   <para>  VPMAXUW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> Max(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_min_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPMINSB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<sbyte> Min(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_min_epu8 (__m512i a, __m512i b)</para>
        ///   <para>  VPMINUB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<byte> Min(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_min_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPMINSW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> Min(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_min_epu16 (__m512i a, __m512i b)</para>
        ///   <para>  VPMINUW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> Min(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>unsigned int _cvtmask32_u32 (__mmask32 a)</para>
        ///   <para>  KMOVD r32, k1</para>
        /// </summary>
        public static new int MoveMask(Vector256<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask32_u32 (__mmask32 a)</para>
        ///   <para>  KMOVD r32, k1</para>
        /// </summary>
        public static new int MoveMask(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>unsigned __int64 _cvtmask64_u64 (__mmask64 a)</para>
        ///   <para>  KMOVQ r64, k1</para>
        /// </summary>
        public static long MoveMask(Vector512<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask32_u32 (__mmask32 a)</para>
        ///   <para>  KMOVD r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector512<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned __int64 _cvtmask64_u64 (__mmask64 a)</para>
        ///   <para>  KMOVQ r64, k1</para>
        /// </summary>
        public static long MoveMask(Vector512<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask32_u32 (__mmask32 a)</para>
        ///   <para>  KMOVD r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector512<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_madd_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPMADDWD zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<int> MultiplyAddAdjacent(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_maddubs_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPMADDUBSW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> MultiplyAddAdjacent(Vector512<byte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_mulhi_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPMULHW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> MultiplyHigh(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mulhi_epu16 (__m512i a, __m512i b)</para>
        ///   <para>  VPMULHUW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> MultiplyHigh(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_mulhrs_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPMULHRSW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> MultiplyHighRoundScale(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_mullo_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPMULLW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> MultiplyLow(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mullo_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPMULLW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> MultiplyLow(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_packs_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPACKSSWB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<sbyte> PackSignedSaturate(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_packs_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPACKSSDW zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<short> PackSignedSaturate(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_packus_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPACKUSWB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<byte> PackUnsignedSaturate(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_packus_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPACKUSDW zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<ushort> PackUnsignedSaturate(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_permutexvar_epi16 (__m512i idx, __m512i a)</para>
        ///   <para>  VPERMW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector512<short> PermuteVar32x16(Vector512<short> left, Vector512<short> control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_permutexvar_epi16 (__m512i idx, __m512i a)</para>
        ///   <para>  VPERMW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector512<ushort> PermuteVar32x16(Vector512<ushort> left, Vector512<ushort> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_permutex2var_epi16 (__m512i a, __m512i idx, __m512i b)</para>
        ///   <para>  VPERMI2W zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        ///   <para>  VPERMT2W zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> PermuteVar32x16x2(Vector512<short> lower, Vector512<short> indices, Vector512<short> upper) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_permutex2var_epi16 (__m512i a, __m512i idx, __m512i b)</para>
        ///   <para>  VPERMI2W zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        ///   <para>  VPERMT2W zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> PermuteVar32x16x2(Vector512<ushort> lower, Vector512<ushort> indices, Vector512<ushort> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_sll_epi16 (__m512i a, __m128i count)</para>
        ///   <para>  VPSLLW zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<short> ShiftLeftLogical(Vector512<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_sll_epi16 (__m512i a, __m128i count)</para>
        ///   <para>  VPSLLW zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<ushort> ShiftLeftLogical(Vector512<ushort> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_slli_epi16 (__m512i a, int imm8)</para>
        ///   <para>  VPSLLW zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<short> ShiftLeftLogical(Vector512<short> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_slli_epi16 (__m512i a, int imm8)</para>
        ///   <para>  VPSLLW zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<ushort> ShiftLeftLogical(Vector512<ushort> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_bslli_epi128 (__m512i a, const int imm8)</para>
        ///   <para>  VPSLLDQ zmm1, zmm2/m512, imm8</para>
        /// </summary>
        public static Vector512<sbyte> ShiftLeftLogical128BitLane(Vector512<sbyte> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_bslli_epi128 (__m512i a, const int imm8)</para>
        ///   <para>  VPSLLDQ zmm1, zmm2/m512, imm8</para>
        /// </summary>
        public static Vector512<byte> ShiftLeftLogical128BitLane(Vector512<byte> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_sllv_epi16 (__m512i a, __m512i count)</para>
        ///   <para>  VPSLLVW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> ShiftLeftLogicalVariable(Vector512<short> value, Vector512<ushort> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_sllv_epi16 (__m512i a, __m512i count)</para>
        ///   <para>  VPSLLVW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> ShiftLeftLogicalVariable(Vector512<ushort> value, Vector512<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>_mm512_sra_epi16 (__m512i a, __m128i count)</para>
        ///   <para>  VPSRAW zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<short> ShiftRightArithmetic(Vector512<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_srai_epi16 (__m512i a, int imm8)</para>
        ///   <para>  VPSRAW zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<short> ShiftRightArithmetic(Vector512<short> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_srav_epi16 (__m512i a, __m512i count)</para>
        ///   <para>  VPSRAVW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> ShiftRightArithmeticVariable(Vector512<short> value, Vector512<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_srl_epi16 (__m512i a, __m128i count)</para>
        ///   <para>  VPSRLW zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<short> ShiftRightLogical(Vector512<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_srl_epi16 (__m512i a, __m128i count)</para>
        ///   <para>  VPSRLW zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<ushort> ShiftRightLogical(Vector512<ushort> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_srli_epi16 (__m512i a, int imm8)</para>
        ///   <para>  VPSRLW zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<short> ShiftRightLogical(Vector512<short> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_srli_epi16 (__m512i a, int imm8)</para>
        ///   <para>  VPSRLW zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<ushort> ShiftRightLogical(Vector512<ushort> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_bsrli_epi128 (__m512i a, const int imm8)</para>
        ///   <para>  VPSRLDQ zmm1, zmm2/m128, imm8</para>
        /// </summary>
        public static Vector512<sbyte> ShiftRightLogical128BitLane(Vector512<sbyte> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_bsrli_epi128 (__m512i a, const int imm8)</para>
        ///   <para>  VPSRLDQ zmm1, zmm2/m128, imm8</para>
        /// </summary>
        public static Vector512<byte> ShiftRightLogical128BitLane(Vector512<byte> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_srlv_epi16 (__m512i a, __m512i count)</para>
        ///   <para>  VPSRLVW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> ShiftRightLogicalVariable(Vector512<short> value, Vector512<ushort> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_srlv_epi16 (__m512i a, __m512i count)</para>
        ///   <para>  VPSRLVW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> ShiftRightLogicalVariable(Vector512<ushort> value, Vector512<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_shuffle_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPSHUFB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<sbyte> Shuffle(Vector512<sbyte> value, Vector512<sbyte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_shuffle_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPSHUFB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<byte> Shuffle(Vector512<byte> value, Vector512<byte> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_shufflehi_epi16 (__m512i a, const int imm8)</para>
        ///   <para>  VPSHUFHW zmm1 {k1}{z}, zmm2/m512, imm8</para>
        /// </summary>
        public static Vector512<short> ShuffleHigh(Vector512<short> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_shufflehi_epi16 (__m512i a, const int imm8)</para>
        ///   <para>  VPSHUFHW zmm1 {k1}{z}, zmm2/m512, imm8</para>
        /// </summary>
        public static Vector512<ushort> ShuffleHigh(Vector512<ushort> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_shufflelo_epi16 (__m512i a, const int imm8)</para>
        ///   <para>  VPSHUFLW zmm1 {k1}{z}, zmm2/m512, imm8</para>
        /// </summary>
        public static Vector512<short> ShuffleLow(Vector512<short> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_shufflelo_epi16 (__m512i a, const int imm8)</para>
        ///   <para>  VPSHUFLW zmm1 {k1}{z}, zmm2/m512, imm8</para>
        /// </summary>
        public static Vector512<ushort> ShuffleLow(Vector512<ushort> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>void _mm512_storeu_epi8 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQU8 m512, zmm1</para>
        /// </summary>
        public static new unsafe void Store(sbyte* address, Vector512<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm512_storeu_epi8 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQU8 m512, zmm1</para>
        /// </summary>
        public static new unsafe void Store(byte* address, Vector512<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm512_storeu_epi16 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQU16 m512, zmm1</para>
        /// </summary>
        public static new unsafe void Store(short* address, Vector512<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm512_storeu_epi16 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQU16 m512, zmm1</para>
        /// </summary>
        public static new unsafe void Store(ushort* address, Vector512<ushort> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_sub_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPSUBB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<sbyte> Subtract(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_sub_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPSUBB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<byte> Subtract(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_sub_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPSUBW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> Subtract(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_sub_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPSUBW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> Subtract(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_subs_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPSUBSB zmm1 {k1}{z}, zmm2, zmm3/m128</para>
        /// </summary>
        public static Vector512<sbyte> SubtractSaturate(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_subs_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPSUBSW zmm1 {k1}{z}, zmm2, zmm3/m128</para>
        /// </summary>
        public static Vector512<short> SubtractSaturate(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_subs_epu8 (__m512i a, __m512i b)</para>
        ///   <para>  VPSUBUSB zmm1 {k1}{z}, zmm2, zmm3/m128</para>
        /// </summary>
        public static Vector512<byte> SubtractSaturate(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_subs_epu16 (__m512i a, __m512i b)</para>
        ///   <para>  VPSUBUSW zmm1 {k1}{z}, zmm2, zmm3/m128</para>
        /// </summary>
        public static Vector512<ushort> SubtractSaturate(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_sad_epu8 (__m512i a, __m512i b)</para>
        ///   <para>  VPSADBW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> SumAbsoluteDifferences(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_dbsad_epu8 (__m512i a, __m512i b)</para>
        ///   <para>  VDBPSADBW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> SumAbsoluteDifferencesInBlock32(Vector512<byte> left, Vector512<byte> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_unpackhi_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKHBW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<sbyte> UnpackHigh(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_unpackhi_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKHBW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<byte> UnpackHigh(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_unpackhi_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKHWD zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> UnpackHigh(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_unpackhi_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKHWD zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> UnpackHigh(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_unpacklo_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKLBW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<sbyte> UnpackLow(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_unpacklo_epi8 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKLBW zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<byte> UnpackLow(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_unpacklo_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKLWD zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<short> UnpackLow(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_unpacklo_epi16 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKLWD zmm1 {k1}{z}, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<ushort> UnpackLow(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
    }
}
