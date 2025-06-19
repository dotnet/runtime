// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 Avx10.1 hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Avx10v1 : Avx2
    {
        internal Avx10v1() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>
        ///   <para>__m128i _mm_abs_epi64 (__m128i a)</para>
        ///   <para>  VPABSQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> Abs(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_abs_epi64 (__m128i a)</para>
        ///   <para>  VPABSQ ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> Abs(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_add_round_sd (__m128d a, __m128d b, int rounding)</para>
        ///   <para>  VADDSD xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> AddScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_add_round_ss (__m128 a, __m128 b, int rounding)</para>
        ///   <para>  VADDSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> AddScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_alignr_epi32 (__m128i a, __m128i b, const int count)</para>
        ///   <para>  VALIGND xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<int> AlignRight32(Vector128<int> left, Vector128<int> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_alignr_epi32 (__m128i a, __m128i b, const int count)</para>
        ///   <para>  VALIGND xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<uint> AlignRight32(Vector128<uint> left, Vector128<uint> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_alignr_epi32 (__m256i a, __m256i b, const int count)</para>
        ///   <para>  VALIGND ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<int> AlignRight32(Vector256<int> left, Vector256<int> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_alignr_epi32 (__m256i a, __m256i b, const int count)</para>
        ///   <para>  VALIGND ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<uint> AlignRight32(Vector256<uint> left, Vector256<uint> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_alignr_epi64 (__m128i a, __m128i b, const int count)</para>
        ///   <para>  VALIGNQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<long> AlignRight64(Vector128<long> left, Vector128<long> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_alignr_epi64 (__m128i a, __m128i b, const int count)</para>
        ///   <para>  VALIGNQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<ulong> AlignRight64(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_alignr_epi64 (__m256i a, __m256i b, const int count)</para>
        ///   <para>  VALIGNQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<long> AlignRight64(Vector256<long> left, Vector256<long> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_alignr_epi64 (__m256i a, __m256i b, const int count)</para>
        ///   <para>  VALIGNQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<ulong> AlignRight64(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_mask_blendv_epu8 (__m128i a, __m128i b, __mmask16 mask)</para>
        ///   <para>  VPBLENDMB xmm1 {k1}, xmm2, xmm3/m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector128<byte> BlendVariable(Vector128<byte> left, Vector128<byte> right, Vector128<byte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_mask_blendv_pd (__m128d a, __m128d b, __mmask8 mask)</para>
        ///   <para>  VBLENDMPD xmm1 {k1}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector128<double> BlendVariable(Vector128<double> left, Vector128<double> right, Vector128<double> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_blendv_epi16 (__m128i a, __m128i b, __mmask8 mask)</para>
        ///   <para>  VPBLENDMW xmm1 {k1}, xmm2, xmm3/m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector128<short> BlendVariable(Vector128<short> left, Vector128<short> right, Vector128<short> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_blendv_epi32 (__m128i a, __m128i b, __mmask8 mask)</para>
        ///   <para>  VPBLENDMD xmm1 {k1}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector128<int> BlendVariable(Vector128<int> left, Vector128<int> right, Vector128<int> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_blendv_epi64 (__m128i a, __m128i b, __mmask8 mask)</para>
        ///   <para>  VPBLENDMQ xmm1 {k1}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector128<long> BlendVariable(Vector128<long> left, Vector128<long> right, Vector128<long> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_blendv_epi8 (__m128i a, __m128i b, __mmask16 mask)</para>
        ///   <para>  VPBLENDMB xmm1 {k1}, xmm2, xmm3/m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector128<sbyte> BlendVariable(Vector128<sbyte> left, Vector128<sbyte> right, Vector128<sbyte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_mask_blendv_ps (__m128 a, __m128 b, __mmask8 mask)</para>
        ///   <para>  VBLENDMPS xmm1 {k1}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector128<float> BlendVariable(Vector128<float> left, Vector128<float> right, Vector128<float> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_blendv_epu16 (__m128i a, __m128i b, __mmask8 mask)</para>
        ///   <para>  VPBLENDMW xmm1 {k1}, xmm2, xmm3/m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector128<ushort> BlendVariable(Vector128<ushort> left, Vector128<ushort> right, Vector128<ushort> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_blendv_epu32 (__m128i a, __m128i b, __mmask8 mask)</para>
        ///   <para>  VPBLENDMD xmm1 {k1}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector128<uint> BlendVariable(Vector128<uint> left, Vector128<uint> right, Vector128<uint> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_blendv_epu64 (__m128i a, __m128i b, __mmask8 mask)</para>
        ///   <para>  VPBLENDMQ xmm1 {k1}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector128<ulong> BlendVariable(Vector128<ulong> left, Vector128<ulong> right, Vector128<ulong> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_mask_blendv_epu8 (__m256i a, __m256i b, __mmask32 mask)</para>
        ///   <para>  VPBLENDMB ymm1 {k1}, ymm2, ymm3/m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector256<byte> BlendVariable(Vector256<byte> left, Vector256<byte> right, Vector256<byte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_mask_blendv_pd (__m256d a, __m256d b, __mmask8 mask)</para>
        ///   <para>  VBLENDMPD ymm1 {k1}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector256<double> BlendVariable(Vector256<double> left, Vector256<double> right, Vector256<double> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_blendv_epi16 (__m256i a, __m256i b, __mmask16 mask)</para>
        ///   <para>  VPBLENDMW ymm1 {k1}, ymm2, ymm3/m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector256<short> BlendVariable(Vector256<short> left, Vector256<short> right, Vector256<short> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_blendv_epi32 (__m256i a, __m256i b, __mmask8 mask)</para>
        ///   <para>  VPBLENDMD ymm1 {k1}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector256<int> BlendVariable(Vector256<int> left, Vector256<int> right, Vector256<int> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_blendv_epi64 (__m256i a, __m256i b, __mmask8 mask)</para>
        ///   <para>  VPBLENDMQ ymm1 {k1}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector256<long> BlendVariable(Vector256<long> left, Vector256<long> right, Vector256<long> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_blendv_epi8 (__m256i a, __m256i b, __mmask32 mask)</para>
        ///   <para>  VPBLENDMB ymm1 {k1}, ymm2, ymm3/m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector256<sbyte> BlendVariable(Vector256<sbyte> left, Vector256<sbyte> right, Vector256<sbyte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_mask_blendv_ps (__m256 a, __m256 b, __mmask8 mask)</para>
        ///   <para>  VBLENDMPS ymm1 {k1}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector256<float> BlendVariable(Vector256<float> left, Vector256<float> right, Vector256<float> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_blendv_epu16 (__m256i a, __m256i b, __mmask16 mask)</para>
        ///   <para>  VPBLENDMW ymm1 {k1}, ymm2, ymm3/m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector256<ushort> BlendVariable(Vector256<ushort> left, Vector256<ushort> right, Vector256<ushort> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_blendv_epu32 (__m256i a, __m256i b, __mmask8 mask)</para>
        ///   <para>  VPBLENDMD ymm1 {k1}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector256<uint> BlendVariable(Vector256<uint> left, Vector256<uint> right, Vector256<uint> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_blendv_epu64 (__m256i a, __m256i b, __mmask8 mask)</para>
        ///   <para>  VPBLENDMQ ymm1 {k1}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static new Vector256<ulong> BlendVariable(Vector256<ulong> left, Vector256<ulong> right, Vector256<ulong> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_broadcast_i32x2 (__m128i a)</para>
        ///   <para>  VBROADCASTI32x2 xmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector128<int> BroadcastPairScalarToVector128(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcast_i32x2 (__m128i a)</para>
        ///   <para>  VBROADCASTI32x2 xmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector128<uint> BroadcastPairScalarToVector128(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_broadcast_f32x2 (__m128 a)</para>
        ///   <para>  VBROADCASTF32x2 ymm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector256<float> BroadcastPairScalarToVector256(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcast_i32x2 (__m128i a)</para>
        ///   <para>  VBROADCASTI32x2 ymm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector256<int> BroadcastPairScalarToVector256(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcast_i32x2 (__m128i a)</para>
        ///   <para>  VBROADCASTI32x2 ymm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector256<uint> BroadcastPairScalarToVector256(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask8 _mm_fpclass_pd_mask (__m128d a, int c)</para>
        ///   <para>  VFPCLASSPD k2 {k1}, xmm2/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<double> Classify(Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_fpclass_ps_mask (__m128 a, int c)</para>
        ///   <para>  VFPCLASSPS k2 {k1}, xmm2/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<float> Classify(Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_fpclass_pd_mask (__m256d a, int c)</para>
        ///   <para>  VFPCLASSPD k2 {k1}, ymm2/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<double> Classify(Vector256<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_fpclass_ps_mask (__m256 a, int c)</para>
        ///   <para>  VFPCLASSPS k2 {k1}, ymm2/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<float> Classify(Vector256<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask8 _mm_fpclass_sd_mask (__m128d a, int c)</para>
        ///   <para>  VFPCLASSSS k2 {k1}, xmm2/m32, imm8</para>
        /// </summary>
        public static Vector128<double> ClassifyScalar(Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_fpclass_ss_mask (__m128 a, int c)</para>
        ///   <para>  VFPCLASSSS k2 {k1}, xmm2/m32, imm8</para>
        /// </summary>
        public static Vector128<float> ClassifyScalar(Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask16 _mm_cmpeq_epu8_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(0)</para>
        /// </summary>
        public static new Vector128<byte> CompareEqual(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
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
        public static new Vector256<byte> CompareEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
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
        ///   <para>__mmask8 _mm_cmp_pd_mask (__m128d a, __m128d b, const int imm8)</para>
        ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8</para>
        /// </summary>
        public static new Vector128<double> Compare(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpeq_pd_mask (__m128d a,  __m128d b)</para>
        ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(0)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<double> CompareEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpgt_pd_mask (__m128d a,  __m128d b)</para>
        ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(14)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<double> CompareGreaterThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpge_pd_mask (__m128d a,  __m128d b)</para>
        ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(13)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<double> CompareGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmplt_pd_mask (__m128d a,  __m128d b)</para>
        ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(1)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<double> CompareLessThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmple_pd_mask (__m128d a,  __m128d b)</para>
        ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(2)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<double> CompareLessThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpneq_pd_mask (__m128d a,  __m128d b)</para>
        ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<double> CompareNotEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpngt_pd_mask (__m128d a,  __m128d b)</para>
        ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<double> CompareNotGreaterThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpnge_pd_mask (__m128d a,  __m128d b)</para>
        ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<double> CompareNotGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpnlt_pd_mask (__m128d a,  __m128d b)</para>
        ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(5)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<double> CompareNotLessThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpnle_pd_mask (__m128d a,  __m128d b)</para>
        ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(6)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<double> CompareNotLessThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpord_pd_mask (__m128d a,  __m128d b)</para>
        ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(7)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<double> CompareOrdered(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpunord_pd_mask (__m128d a,  __m128d b)</para>
        ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(3)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<double> CompareUnordered(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask8 _mm256_cmp_pd_mask (__m256d a, __m256d b, const int imm8)</para>
        ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8</para>
        /// </summary>
        public static new Vector256<double> Compare(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpeq_pd_mask (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(0)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<double> CompareEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpgt_pd_mask (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(14)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<double> CompareGreaterThan(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpge_pd_mask (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(13)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<double> CompareGreaterThanOrEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmplt_pd_mask (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(1)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<double> CompareLessThan(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmple_pd_mask (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(2)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<double> CompareLessThanOrEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpneq_pd_mask (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<double> CompareNotEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpngt_pd_mask (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<double> CompareNotGreaterThan(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpnge_pd_mask (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<double> CompareNotGreaterThanOrEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpnlt_pd_mask (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(5)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<double> CompareNotLessThan(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpnle_pd_mask (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(6)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<double> CompareNotLessThanOrEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpord_pd_mask (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(7)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<double> CompareOrdered(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpunord_pd_mask (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(3)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<double> CompareUnordered(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask8 _mm_cmpeq_epi16_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(0)</para>
        /// </summary>
        public static new Vector128<short> CompareEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpgt_epi16_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(6)</para>
        /// </summary>
        public static new Vector128<short> CompareGreaterThan(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpge_epi16_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(5)</para>
        /// </summary>
        public static Vector128<short> CompareGreaterThanOrEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmplt_epi16_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(1)</para>
        /// </summary>
        public static new Vector128<short> CompareLessThan(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
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
        public static new Vector256<short> CompareEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask16 _mm256_cmpgt_epi16_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(6)</para>
        /// </summary>
        public static new Vector256<short> CompareGreaterThan(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
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
        ///   <para>__mmask8 _mm_cmpeq_epi32_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(0)</para>
        /// </summary>
        public static new Vector128<int> CompareEqual(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpgt_epi32_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(6)</para>
        /// </summary>
        public static new Vector128<int> CompareGreaterThan(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpge_epi32_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(5)</para>
        /// </summary>
        public static Vector128<int> CompareGreaterThanOrEqual(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmplt_epi32_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(1)</para>
        /// </summary>
        public static new Vector128<int> CompareLessThan(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmple_epi32_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(2)</para>
        /// </summary>
        public static Vector128<int> CompareLessThanOrEqual(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpne_epi32_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(4)</para>
        /// </summary>
        public static Vector128<int> CompareNotEqual(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask8 _mm_cmpeq_epi32_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(0)</para>
        /// </summary>
        public static new Vector256<int> CompareEqual(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpgt_epi32_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(6)</para>
        /// </summary>
        public static new Vector256<int> CompareGreaterThan(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpge_epi32_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(5)</para>
        /// </summary>
        public static Vector256<int> CompareGreaterThanOrEqual(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmplt_epi32_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(1)</para>
        /// </summary>
        public static Vector256<int> CompareLessThan(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmple_epi32_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(2)</para>
        /// </summary>
        public static Vector256<int> CompareLessThanOrEqual(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpne_epi32_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(4)</para>
        /// </summary>
        public static Vector256<int> CompareNotEqual(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask8 _mm_cmpeq_epi64_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(0)</para>
        /// </summary>
        public static new Vector128<long> CompareEqual(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpgt_epi64_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(6)</para>
        /// </summary>
        public static new Vector128<long> CompareGreaterThan(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpge_epi64_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(5)</para>
        /// </summary>
        public static Vector128<long> CompareGreaterThanOrEqual(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmplt_epi64_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(1)</para>
        /// </summary>
        public static Vector128<long> CompareLessThan(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmple_epi64_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(2)</para>
        /// </summary>
        public static Vector128<long> CompareLessThanOrEqual(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpne_epi64_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(4)</para>
        /// </summary>
        public static Vector128<long> CompareNotEqual(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask8 _mm256_cmpeq_epi64_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(0)</para>
        /// </summary>
        public static new Vector256<long> CompareEqual(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpgt_epi64_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(6)</para>
        /// </summary>
        public static new Vector256<long> CompareGreaterThan(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpge_epi64_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(5)</para>
        /// </summary>
        public static Vector256<long> CompareGreaterThanOrEqual(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmplt_epi64_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(1)</para>
        /// </summary>
        public static Vector256<long> CompareLessThan(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmple_epi64_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(2)</para>
        /// </summary>
        public static Vector256<long> CompareLessThanOrEqual(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpne_epi64_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(4)</para>
        /// </summary>
        public static Vector256<long> CompareNotEqual(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask16 _mm_cmpeq_epi8_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(0)</para>
        /// </summary>
        public static new Vector128<sbyte> CompareEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask16 _mm_cmpgt_epi8_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(6)</para>
        /// </summary>
        public static new Vector128<sbyte> CompareGreaterThan(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask16 _mm_cmpge_epi8_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(5)</para>
        /// </summary>
        public static Vector128<sbyte> CompareGreaterThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask16 _mm_cmplt_epi8_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(1)</para>
        /// </summary>
        public static new Vector128<sbyte> CompareLessThan(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
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
        public static new Vector256<sbyte> CompareEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask32 _mm256_cmpgt_epi8_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(6)</para>
        /// </summary>
        public static new Vector256<sbyte> CompareGreaterThan(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
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
        ///   <para>__mmask8 _mm_cmp_ps_mask (__m128 a, __m128 b, const int imm8)</para>
        ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8</para>
        /// </summary>
        public static new Vector128<float> Compare(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpeq_ps_mask (__m128 a,  __m128 b)</para>
        ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(0)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<float> CompareEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpgt_ps_mask (__m128 a,  __m128 b)</para>
        ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(14)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<float> CompareGreaterThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpge_ps_mask (__m128 a,  __m128 b)</para>
        ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(13)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<float> CompareGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmplt_ps_mask (__m128 a,  __m128 b)</para>
        ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(1)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<float> CompareLessThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmple_ps_mask (__m128 a,  __m128 b)</para>
        ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(2)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<float> CompareLessThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpneq_ps_mask (__m128 a,  __m128 b)</para>
        ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<float> CompareNotEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpngt_ps_mask (__m128 a,  __m128 b)</para>
        ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<float> CompareNotGreaterThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpnge_ps_mask (__m128 a,  __m128 b)</para>
        ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<float> CompareNotGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpnlt_ps_mask (__m128 a,  __m128 b)</para>
        ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(5)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<float> CompareNotLessThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpnle_ps_mask (__m128 a,  __m128 b)</para>
        ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(6)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<float> CompareNotLessThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpord_ps_mask (__m128 a,  __m128 b)</para>
        ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(7)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<float> CompareOrdered(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm_cmpunord_ps_mask (__m128 a,  __m128 b)</para>
        ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(3)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector128<float> CompareUnordered(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask8 _mm256_cmp_ps_mask (__m256 a, __m256 b, const int imm8)</para>
        ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8</para>
        /// </summary>
        public static new Vector256<float> Compare(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpeq_ps_mask (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(0)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<float> CompareEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpgt_ps_mask (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(14)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<float> CompareGreaterThan(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpge_ps_mask (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(13)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<float> CompareGreaterThanOrEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmplt_ps_mask (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(1)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<float> CompareLessThan(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmple_ps_mask (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(2)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<float> CompareLessThanOrEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpneq_ps_mask (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<float> CompareNotEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpngt_ps_mask (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<float> CompareNotGreaterThan(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpnge_ps_mask (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<float> CompareNotGreaterThanOrEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpnlt_ps_mask (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(5)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<float> CompareNotLessThan(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpnle_ps_mask (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(6)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<float> CompareNotLessThanOrEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpord_ps_mask (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(7)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<float> CompareOrdered(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mmask8 _mm256_cmpunord_ps_mask (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(3)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static new Vector256<float> CompareUnordered(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mmask8 _mm_cmpeq_epu16_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(0)</para>
        /// </summary>
        public static new Vector128<ushort> CompareEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
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
        public static new Vector256<ushort> CompareEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
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
        ///   <para>__mmask8 _mm_cmpeq_epu32_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(0)</para>
        /// </summary>
        public static new Vector128<uint> CompareEqual(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cmpgt_epu32 (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(6)</para>
        /// </summary>
        public static Vector128<uint> CompareGreaterThan(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cmpge_epu32 (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(5)</para>
        /// </summary>
        public static Vector128<uint> CompareGreaterThanOrEqual(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cmplt_epu32 (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(1)</para>
        /// </summary>
        public static Vector128<uint> CompareLessThan(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cmple_epu32 (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(2)</para>
        /// </summary>
        public static Vector128<uint> CompareLessThanOrEqual(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cmpne_epu32 (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(4)</para>
        /// </summary>
        public static Vector128<uint> CompareNotEqual(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mask8 _mm256_cmpeq_epu32_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(0)</para>
        /// </summary>
        public static new Vector256<uint> CompareEqual(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm256_cmpgt_epu32_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(6)</para>
        /// </summary>
        public static Vector256<uint> CompareGreaterThan(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm256_cmpge_epu32_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(5)</para>
        /// </summary>
        public static Vector256<uint> CompareGreaterThanOrEqual(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm256_cmplt_epu32_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(1)</para>
        /// </summary>
        public static Vector256<uint> CompareLessThan(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm256_cmple_epu32_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(2)</para>
        /// </summary>
        public static Vector256<uint> CompareLessThanOrEqual(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm256_cmpne_epu32_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(4)</para>
        /// </summary>
        public static Vector256<uint> CompareNotEqual(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mask8 _mm_cmpeq_epu64_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(0)</para>
        /// </summary>
        public static new Vector128<ulong> CompareEqual(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm_cmpgt_epu64_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(6)</para>
        /// </summary>
        public static Vector128<ulong> CompareGreaterThan(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm_cmpge_epu64_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(5)</para>
        /// </summary>
        public static Vector128<ulong> CompareGreaterThanOrEqual(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm_cmplt_epu64_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(1)</para>
        /// </summary>
        public static Vector128<ulong> CompareLessThan(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm_cmple_epu64_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(2)</para>
        /// </summary>
        public static Vector128<ulong> CompareLessThanOrEqual(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm_cmpne_epu64_mask (__m128i a, __m128i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(4)</para>
        /// </summary>
        public static Vector128<ulong> CompareNotEqual(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__mask8 _mm256_cmpeq_epu64_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(0)</para>
        /// </summary>
        public static new Vector256<ulong> CompareEqual(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm256_cmpgt_epu64_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(6)</para>
        /// </summary>
        public static Vector256<ulong> CompareGreaterThan(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm256_cmpge_epu64_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(5)</para>
        /// </summary>
        public static Vector256<ulong> CompareGreaterThanOrEqual(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm256_cmplt_epu64_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(1)</para>
        /// </summary>
        public static Vector256<ulong> CompareLessThan(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm256_cmple_epu64_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(2)</para>
        /// </summary>
        public static Vector256<ulong> CompareLessThanOrEqual(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__mask8 _mm256_cmpne_epu64_mask (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(4)</para>
        /// </summary>
        public static Vector256<ulong> CompareNotEqual(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_mask_compress_epi8 (__m128i s, __mmask16 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSB xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<byte> Compress(Vector128<byte> merge, Vector128<byte> mask, Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_mask_compress_pd (__m128d s, __mmask8 k, __m128d a)</para>
        ///   <para>  VCOMPRESSPD xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<double> Compress(Vector128<double> merge, Vector128<double> mask, Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_compress_epi16 (__m128i s, __mmask8 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSW xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<short> Compress(Vector128<short> merge, Vector128<short> mask, Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_compress_epi32 (__m128i s, __mask8 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSD xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<int> Compress(Vector128<int> merge, Vector128<int> mask, Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_compress_epi64 (__m128i s, __mask8 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSQ xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<long> Compress(Vector128<long> merge, Vector128<long> mask, Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_compress_epi8 (__m128i s, __mmask16 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSB xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<sbyte> Compress(Vector128<sbyte> merge, Vector128<sbyte> mask, Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_mask_compress_ps (__m128 s, __mmask8 k, __m128 a)</para>
        ///   <para>  VCOMPRESSPS xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<float> Compress(Vector128<float> merge, Vector128<float> mask, Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_compress_epi16 (__m128i s, __mmask8 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSW xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<ushort> Compress(Vector128<ushort> merge, Vector128<ushort> mask, Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_compress_epi32 (__m128i s, __mask8 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSD xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<uint> Compress(Vector128<uint> merge, Vector128<uint> mask, Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_compress_epi64 (__m128i s, __mask8 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSQ xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<ulong> Compress(Vector128<ulong> merge, Vector128<ulong> mask, Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_mask_compress_epi8 (__m256i s, __mmask32 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSB ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<byte> Compress(Vector256<byte> merge, Vector256<byte> mask, Vector256<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_mask_compress_pd (__m256d s, __mmask8 k, __m256d a)</para>
        ///   <para>  VCOMPRESSPD ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<double> Compress(Vector256<double> merge, Vector256<double> mask, Vector256<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_compress_epi16 (__m256i s, __mmask16 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSW ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<short> Compress(Vector256<short> merge, Vector256<short> mask, Vector256<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_compress_epi32 (__m256i s, __mmask8 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSD ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<int> Compress(Vector256<int> merge, Vector256<int> mask, Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_compress_epi64 (__m256i s, __mmask8 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSQ ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<long> Compress(Vector256<long> merge, Vector256<long> mask, Vector256<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_compress_epi8 (__m256i s, __mmask32 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSB ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<sbyte> Compress(Vector256<sbyte> merge, Vector256<sbyte> mask, Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_mask_compress_ps (__m256 s, __mmask8 k, __m256 a)</para>
        ///   <para>  VCOMPRESSPS ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<float> Compress(Vector256<float> merge, Vector256<float> mask, Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_compress_epi16 (__m256i s, __mmask16 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSW ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<ushort> Compress(Vector256<ushort> merge, Vector256<ushort> mask, Vector256<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_compress_epi32 (__m256i s, __mmask8 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSD ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<uint> Compress(Vector256<uint> merge, Vector256<uint> mask, Vector256<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_compress_epi64 (__m256i s, __mmask8 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSQ ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<ulong> Compress(Vector256<ulong> merge, Vector256<ulong> mask, Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_mask_compressstoreu_epi8 (void * s, __mmask16 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSB m128 {k1}{z}, xmm2</para>
        /// </summary>
        public static unsafe void CompressStore(byte* address, Vector128<byte> mask, Vector128<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_mask_compressstoreu_pd (void * a, __mmask8 k, __m128d a)</para>
        ///   <para>  VCOMPRESSPD m128 {k1}{z}, xmm2</para>
        /// </summary>
        public static unsafe void CompressStore(double* address, Vector128<double> mask, Vector128<double> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_compressstoreu_epi16 (void * s, __mmask8 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSW m128 {k1}{z}, xmm2</para>
        /// </summary>
        public static unsafe void CompressStore(short* address, Vector128<short> mask, Vector128<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_compressstoreu_epi32 (void * a, __mask8 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSD m128 {k1}{z}, xmm2</para>
        /// </summary>
        public static unsafe void CompressStore(int* address, Vector128<int> mask, Vector128<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_compressstoreu_epi64 (void * a, __mask8 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSQ m128 {k1}{z}, xmm2</para>
        /// </summary>
        public static unsafe void CompressStore(long* address, Vector128<long> mask, Vector128<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_compressstoreu_epi8 (void * s, __mmask16 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSB m128 {k1}{z}, xmm2</para>
        /// </summary>
        public static unsafe void CompressStore(sbyte* address, Vector128<sbyte> mask, Vector128<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_mask_compressstoreu_ps (void * a, __mmask8 k, __m128 a)</para>
        ///   <para>  VCOMPRESSPS m128 {k1}{z}, xmm2</para>
        /// </summary>
        public static unsafe void CompressStore(float* address, Vector128<float> mask, Vector128<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_compressstoreu_epi16 (void * s, __mmask8 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSW m128 {k1}{z}, xmm2</para>
        /// </summary>
        public static unsafe void CompressStore(ushort* address, Vector128<ushort> mask, Vector128<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_compressstoreu_epi32 (void * a, __mask8 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSD m128 {k1}{z}, xmm2</para>
        /// </summary>
        public static unsafe void CompressStore(uint* address, Vector128<uint> mask, Vector128<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_compressstoreu_epi64 (void * a, __mask8 k, __m128i a)</para>
        ///   <para>  VPCOMPRESSQ m128 {k1}{z}, xmm2</para>
        /// </summary>
        public static unsafe void CompressStore(ulong* address, Vector128<ulong> mask, Vector128<ulong> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>void _mm256_mask_compressstoreu_epi8 (void * s, __mmask32 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSB m256 {k1}{z}, ymm2</para>
        /// </summary>
        public static unsafe void CompressStore(byte* address, Vector256<byte> mask, Vector256<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_mask_compressstoreu_pd (void * a, __mmask8 k, __m256d a)</para>
        ///   <para>  VCOMPRESSPD m256 {k1}{z}, ymm2</para>
        /// </summary>
        public static unsafe void CompressStore(double* address, Vector256<double> mask, Vector256<double> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_compressstoreu_epi16 (void * s, __mmask16 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSW m256 {k1}{z}, ymm2</para>
        /// </summary>
        public static unsafe void CompressStore(short* address, Vector256<short> mask, Vector256<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_compressstoreu_epi32 (void * a, __mmask8 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSD m256 {k1}{z}, ymm2</para>
        /// </summary>
        public static unsafe void CompressStore(int* address, Vector256<int> mask, Vector256<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_compressstoreu_epi64 (void * a, __mmask8 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSQ m256 {k1}{z}, ymm2</para>
        /// </summary>
        public static unsafe void CompressStore(long* address, Vector256<long> mask, Vector256<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_compressstoreu_epi8 (void * s, __mmask32 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSB m256 {k1}{z}, ymm2</para>
        /// </summary>
        public static unsafe void CompressStore(sbyte* address, Vector256<sbyte> mask, Vector256<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_mask_compressstoreu_ps (void * a, __mmask8 k, __m256 a)</para>
        ///   <para>  VCOMPRESSPS m256 {k1}{z}, ymm2</para>
        /// </summary>
        public static unsafe void CompressStore(float* address, Vector256<float> mask, Vector256<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_compressstoreu_epi16 (void * s, __mmask16 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSW m256 {k1}{z}, ymm2</para>
        /// </summary>
        public static unsafe void CompressStore(ushort* address, Vector256<ushort> mask, Vector256<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_compressstoreu_epi32 (void * a, __mmask8 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSD m256 {k1}{z}, ymm2</para>
        /// </summary>
        public static unsafe void CompressStore(uint* address, Vector256<uint> mask, Vector256<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_compressstoreu_epi64 (void * a, __mmask8 k, __m256i a)</para>
        ///   <para>  VPCOMPRESSQ m256 {k1}{z}, ymm2</para>
        /// </summary>
        public static unsafe void CompressStore(ulong* address, Vector256<ulong> mask, Vector256<ulong> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_cvtsi32_sd (__m128d a, int b)</para>
        ///   <para>  VCVTUSI2SD xmm1, xmm2, r/m32</para>
        /// </summary>
        public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_cvt_roundi32_ss (__m128 a, int b, int rounding)</para>
        ///   <para>VCVTSI2SS xmm1, xmm2, r32 {er}</para>
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, int value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_cvt_roundi32_ss (__m128 a, int b, int rounding)</para>
        ///   <para>VCVTUSI2SS xmm1, xmm2, r32 {er}</para>
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, uint value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_cvtsi32_ss (__m128 a, int b)</para>
        ///   <para>  VCVTUSI2SS xmm1, xmm2, r/m32</para>
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, uint value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_cvt_roundsd_ss (__m128 a, __m128d b, int rounding)</para>
        ///   <para>VCVTSD2SS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>int _mm_cvt_roundsd_i32 (__m128d a, int rounding)</para>
        ///   <para>  VCVTSD2SI r32, xmm1 {er}</para>
        /// </summary>
        public static int ConvertToInt32(Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm_cvt_roundss_i32 (__m128 a, int rounding)</para>
        ///   <para>  VCVTSS2SIK r32, xmm1 {er}</para>
        /// </summary>
        public static int ConvertToInt32(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>unsigned int _mm_cvt_roundsd_u32 (__m128d a, int rounding)</para>
        ///   <para>  VCVTSD2USI r32, xmm1 {er}</para>
        /// </summary>
        public static uint ConvertToUInt32(Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _mm_cvtsd_u32 (__m128d a)</para>
        ///   <para>  VCVTSD2USI r32, xmm1/m64{er}</para>
        /// </summary>
        public static uint ConvertToUInt32(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _mm_cvt_roundss_u32 (__m128 a, int rounding)</para>
        ///   <para>  VCVTSS2USI r32, xmm1 {er}</para>
        /// </summary>
        public static uint ConvertToUInt32(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _mm_cvtss_u32 (__m128 a)</para>
        ///   <para>  VCVTSS2USI r32, xmm1/m32{er}</para>
        /// </summary>
        public static uint ConvertToUInt32(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>unsigned int _mm_cvttsd_u32 (__m128d a)</para>
        ///   <para>  VCVTTSD2USI r32, xmm1/m64{er}</para>
        /// </summary>
        public static uint ConvertToUInt32WithTruncation(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _mm_cvttss_u32 (__m128 a)</para>
        ///   <para>  VCVTTSS2USI r32, xmm1/m32{er}</para>
        /// </summary>
        public static uint ConvertToUInt32WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvtepi32_epi8 (__m128i a)</para>
        ///   <para>  VPMOVDB xmm1/m32 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi64_epi8 (__m128i a)</para>
        ///   <para>  VPMOVQB xmm1/m16 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi16_epi8 (__m128i a)</para>
        ///   <para>  VPMOVWB xmm1/m64 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi32_epi8 (__m128i a)</para>
        ///   <para>  VPMOVDB xmm1/m32 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi64_epi8 (__m128i a)</para>
        ///   <para>  VPMOVQB xmm1/m16 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi16_epi8 (__m128i a)</para>
        ///   <para>  VPMOVWB xmm1/m64 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi32_epi8 (__m256i a)</para>
        ///   <para>  VPMOVDB xmm1/m64 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi64_epi8 (__m256i a)</para>
        ///   <para>  VPMOVQB xmm1/m32 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector256<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi16_epi8 (__m256i a)</para>
        ///   <para>  VPMOVWB xmm1/m128 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector256<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi32_epi8 (__m256i a)</para>
        ///   <para>  VPMOVDB xmm1/m64 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector256<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi64_epi8 (__m256i a)</para>
        ///   <para>  VPMOVQB xmm1/m32 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi16_epi8 (__m256i a)</para>
        ///   <para>  VPMOVWB xmm1/m128 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvtusepi32_epi8 (__m128i a)</para>
        ///   <para>  VPMOVUSDB xmm1/m32 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtusepi64_epi8 (__m128i a)</para>
        ///   <para>  VPMOVUSQB xmm1/m16 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtusepi16_epi8 (__m128i a)</para>
        ///   <para>  VPMOVUWB xmm1/m64 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtusepi32_epi8 (__m256i a)</para>
        ///   <para>  VPMOVUSDB xmm1/m64 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector256<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtusepi64_epi8 (__m256i a)</para>
        ///   <para>  VPMOVUSQB xmm1/m32 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtusepi16_epi8 (__m256i a)</para>
        ///   <para>  VPMOVUWB xmm1/m128 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_cvtepi64_pd (__m128i a)</para>
        ///   <para>  VCVTQQ2PD xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> ConvertToVector128Double(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_cvtepu32_pd (__m128i a)</para>
        ///   <para>  VCVTUDQ2PD xmm1 {k1}{z}, xmm2/m64/m32bcst</para>
        /// </summary>
        public static Vector128<double> ConvertToVector128Double(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_cvtepu64_pd (__m128i a)</para>
        ///   <para>  VCVTUQQ2PD xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> ConvertToVector128Double(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvtepi32_epi16 (__m128i a)</para>
        ///   <para>  VPMOVDW xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi64_epi16 (__m128i a)</para>
        ///   <para>  VPMOVQW xmm1/m32 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi32_epi16 (__m128i a)</para>
        ///   <para>  VPMOVDW xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi64_epi16 (__m128i a)</para>
        ///   <para>  VPMOVQW xmm1/m32 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi32_epi16 (__m256i a)</para>
        ///   <para>  VPMOVDW xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi64_epi16 (__m256i a)</para>
        ///   <para>  VPMOVQW xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector256<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi32_epi16 (__m256i a)</para>
        ///   <para>  VPMOVDW xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector256<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi64_epi16 (__m256i a)</para>
        ///   <para>  VPMOVQW xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvtsepi32_epi16 (__m128i a)</para>
        ///   <para>  VPMOVSDW xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtsepi64_epi16 (__m128i a)</para>
        ///   <para>  VPMOVSQW xmm1/m32 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtsepi32_epi16 (__m256i a)</para>
        ///   <para>  VPMOVSDW xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtsepi64_epi16 (__m256i a)</para>
        ///   <para>  VPMOVSQW xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvtepi64_epi32 (__m128i a)</para>
        ///   <para>  VPMOVQD xmm1/m64 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi64_epi32 (__m128i a)</para>
        ///   <para>  VPMOVQD xmm1/m64 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi64_epi32 (__m256i a)</para>
        ///   <para>  VPMOVQD xmm1/m128 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector256<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi64_epi32 (__m256i a)</para>
        ///   <para>  VPMOVQD xmm1/m128 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvtsepi64_epi32 (__m128i a)</para>
        ///   <para>  VPMOVSQD xmm1/m64 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32WithSaturation(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtsepi64_epi32 (__m256i a)</para>
        ///   <para>  VPMOVSQD xmm1/m128 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32WithSaturation(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvtpd_epi64 (__m128d a)</para>
        ///   <para>  VCVTPD2QQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtps_epi64 (__m128 a)</para>
        ///   <para>  VCVTPS2QQ xmm1 {k1}{z}, xmm2/m64/m32bcst</para>
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvttpd_epi64 (__m128d a)</para>
        ///   <para>  VCVTTPD2QQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64WithTruncation(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvttps_epi64 (__m128 a)</para>
        ///   <para>  VCVTTPS2QQ xmm1 {k1}{z}, xmm2/m64/m32bcst</para>
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvtepi32_epi8 (__m128i a)</para>
        ///   <para>  VPMOVDB xmm1/m32 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi64_epi8 (__m128i a)</para>
        ///   <para>  VPMOVQB xmm1/m16 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi16_epi8 (__m128i a)</para>
        ///   <para>  VPMOVWB xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi32_epi8 (__m128i a)</para>
        ///   <para>  VPMOVDB xmm1/m32 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi64_epi8 (__m128i a)</para>
        ///   <para>  VPMOVQB xmm1/m16 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi16_epi8 (__m128i a)</para>
        ///   <para>  VPMOVWB xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi32_epi8 (__m256i a)</para>
        ///   <para>  VPMOVDB xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi64_epi8 (__m256i a)</para>
        ///   <para>  VPMOVQB xmm1/m32 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector256<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi16_epi8 (__m256i a)</para>
        ///   <para>  VPMOVWB xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector256<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi32_epi8 (__m256i a)</para>
        ///   <para>  VPMOVDB xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector256<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi64_epi8 (__m256i a)</para>
        ///   <para>  VPMOVQB xmm1/m32 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi16_epi8 (__m256i a)</para>
        ///   <para>  VPMOVWB xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvtsepi32_epi8 (__m128i a)</para>
        ///   <para>  VPMOVSDB xmm1/m32 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtsepi64_epi8 (__m128i a)</para>
        ///   <para>  VPMOVSQB xmm1/m16 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtsepi16_epi8 (__m128i a)</para>
        ///   <para>  VPMOVSWB xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtsepi32_epi8 (__m256i a)</para>
        ///   <para>  VPMOVSDB xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtsepi64_epi8 (__m256i a)</para>
        ///   <para>  VPMOVSQB xmm1/m32 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector256<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtsepi16_epi8 (__m256i a)</para>
        ///   <para>  VPMOVSWB xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_cvtepi64_ps (__m128i a)</para>
        ///   <para>  VCVTQQ2PS xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_cvtepu32_ps (__m128i a)</para>
        ///   <para>  VCVTUDQ2PS xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_cvtepu64_ps (__m128i a)</para>
        ///   <para>  VCVTUQQ2PS xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm256_cvtepi64_ps (__m256i a)</para>
        ///   <para>  VCVTQQ2PS xmm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector256<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm256_cvtepu64_ps (__m256i a)</para>
        ///   <para>  VCVTUQQ2PS xmm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvtepi32_epi16 (__m128i a)</para>
        ///   <para>  VPMOVDW xmm1/m64 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi64_epi16 (__m128i a)</para>
        ///   <para>  VPMOVQW xmm1/m32 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi32_epi16 (__m128i a)</para>
        ///   <para>  VPMOVDW xmm1/m64 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi64_epi16 (__m128i a)</para>
        ///   <para>  VPMOVQW xmm1/m32 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi32_epi16 (__m256i a)</para>
        ///   <para>  VPMOVDW xmm1/m128 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi64_epi16 (__m256i a)</para>
        ///   <para>  VPMOVQW xmm1/m64 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector256<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi32_epi16 (__m256i a)</para>
        ///   <para>  VPMOVDW xmm1/m128 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector256<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi64_epi16 (__m256i a)</para>
        ///   <para>  VPMOVQW xmm1/m64 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvtusepi32_epi16 (__m128i a)</para>
        ///   <para>  VPMOVUSDW xmm1/m64 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtusepi64_epi16 (__m128i a)</para>
        ///   <para>  VPMOVUSQW xmm1/m32 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtusepi32_epi16 (__m256i a)</para>
        ///   <para>  VPMOVUSDW xmm1/m128 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector256<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtusepi64_epi16 (__m256i a)</para>
        ///   <para>  VPMOVUSQW xmm1/m64 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvtpd_epu32 (__m128d a)</para>
        ///   <para>  VCVTPD2UDQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtps_epu32 (__m128 a)</para>
        ///   <para>  VCVTPS2UDQ xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi64_epi32 (__m128i a)</para>
        ///   <para>  VPMOVQD xmm1/m128 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtepi64_epi32 (__m128i a)</para>
        ///   <para>  VPMOVQD xmm1/m128 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtpd_epu32 (__m256d a)</para>
        ///   <para>  VCVTPD2UDQ xmm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector256<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi64_epi32 (__m256i a)</para>
        ///   <para>  VPMOVQD xmm1/m128 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector256<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtepi64_epi32 (__m256i a)</para>
        ///   <para>  VPMOVQD xmm1/m128 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvtusepi64_epi32 (__m128i a)</para>
        ///   <para>  VPMOVUSQD xmm1/m128 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32WithSaturation(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvtusepi64_epi32 (__m256i a)</para>
        ///   <para>  VPMOVUSQD xmm1/m128 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32WithSaturation(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvttpd_epu32 (__m128d a)</para>
        ///   <para>  VCVTTPD2UDQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32WithTruncation(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvttps_epu32 (__m128 a)</para>
        ///   <para>  VCVTTPS2UDQ xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvttpd_epu32 (__m256d a)</para>
        ///   <para>  VCVTTPD2UDQ xmm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32WithTruncation(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvtpd_epu64 (__m128d a)</para>
        ///   <para>  VCVTPD2UQQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> ConvertToVector128UInt64(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvtps_epu64 (__m128 a)</para>
        ///   <para>  VCVTPS2UQQ xmm1 {k1}{z}, xmm2/m64/m32bcst</para>
        /// </summary>
        public static Vector128<ulong> ConvertToVector128UInt64(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_cvttpd_epu64 (__m128d a)</para>
        ///   <para>  VCVTTPD2UQQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> ConvertToVector128UInt64WithTruncation(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_cvttps_epu64 (__m128 a)</para>
        ///   <para>  VCVTTPS2UQQ xmm1 {k1}{z}, xmm2/m64/m32bcst</para>
        /// </summary>
        public static Vector128<ulong> ConvertToVector128UInt64WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256d _mm512_cvtepu32_pd (__m128i a)</para>
        ///   <para>  VCVTUDQ2PD ymm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector256<double> ConvertToVector256Double(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cvtepi64_pd (__m256i a)</para>
        ///   <para>  VCVTQQ2PD ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> ConvertToVector256Double(Vector256<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cvtepu64_pd (__m256i a)</para>
        ///   <para>  VCVTUQQ2PD ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> ConvertToVector256Double(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_cvtps_epi64 (__m128 a)</para>
        ///   <para>  VCVTPS2QQ ymm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvtpd_epi64 (__m256d a)</para>
        ///   <para>  VCVTPD2QQ ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_cvttps_epi64 (__m128 a)</para>
        ///   <para>  VCVTTPS2QQ ymm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvttpd_epi64 (__m256d a)</para>
        ///   <para>  VCVTTPD2QQ ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64WithTruncation(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_cvtepu32_ps (__m256i a)</para>
        ///   <para>  VCVTUDQ2PS ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> ConvertToVector256Single(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_cvtps_epu32 (__m256 a)</para>
        ///   <para>  VCVTPS2UDQ ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_cvttps_epu32 (__m256 a)</para>
        ///   <para>  VCVTTPS2UDQ ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32WithTruncation(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_cvtps_epu64 (__m128 a)</para>
        ///   <para>  VCVTPS2UQQ ymm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector256<ulong> ConvertToVector256UInt64(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvtpd_epu64 (__m256d a)</para>
        ///   <para>  VCVTPD2UQQ ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> ConvertToVector256UInt64(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_cvttps_epu64 (__m128 a)</para>
        ///   <para>  VCVTTPS2UQQ ymm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector256<ulong> ConvertToVector256UInt64WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvttpd_epu64 (__m256d a)</para>
        ///   <para>  VCVTTPD2UQQ ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> ConvertToVector256UInt64WithTruncation(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_conflict_epi32 (__m128i a)</para>
        ///   <para>  VPCONFLICTD xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> DetectConflicts(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_conflict_epi64 (__m128i a)</para>
        ///   <para>  VPCONFLICTQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> DetectConflicts(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_conflict_epi32 (__m128i a)</para>
        ///   <para>  VPCONFLICTD xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> DetectConflicts(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_conflict_epi64 (__m128i a)</para>
        ///   <para>  VPCONFLICTQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> DetectConflicts(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_conflict_epi32 (__m256i a)</para>
        ///   <para>  VPCONFLICTD ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> DetectConflicts(Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_conflict_epi64 (__m256i a)</para>
        ///   <para>  VPCONFLICTQ ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> DetectConflicts(Vector256<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_conflict_epi32 (__m256i a)</para>
        ///   <para>  VPCONFLICTD ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> DetectConflicts(Vector256<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_conflict_epi64 (__m256i a)</para>
        ///   <para>  VPCONFLICTQ ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> DetectConflicts(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_div_round_sd (__m128d a, __m128d b, int rounding)</para>
        ///   <para>  VDIVSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> DivideScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_div_round_ss (__m128 a, __m128 b, int rounding)</para>
        ///   <para>  VDIVSD xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> DivideScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_mask_expand_epi8 (__m128i s, __mmask16 k, __m128i a)</para>
        ///   <para>  VPEXPANDB xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<byte> Expand(Vector128<byte> merge, Vector128<byte> mask, Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_mask_expand_pd (__m128d s, __mmask8 k, __m128d a)</para>
        ///   <para>  VEXPANDPD xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<double> Expand(Vector128<double> merge, Vector128<double> mask, Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_expand_epi16 (__m128i s, __mmask8 k, __m128i a)</para>
        ///   <para>  VPEXPANDW xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<short> Expand(Vector128<short> merge, Vector128<short> mask, Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_expand_epi32 (__m128i s, __mmask8 k, __m128i a)</para>
        ///   <para>  VPEXPANDD xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<int> Expand(Vector128<int> merge, Vector128<int> mask, Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_expand_epi64 (__m128i s, __mmask8 k, __m128i a)</para>
        ///   <para>  VPEXPANDQ xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<long> Expand(Vector128<long> merge, Vector128<long> mask, Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_expand_epi8 (__m128i s, __mmask16 k, __m128i a)</para>
        ///   <para>  VPEXPANDB xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<sbyte> Expand(Vector128<sbyte> merge, Vector128<sbyte> mask, Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_mask_expand_ps (__m128 s, __mmask8 k, __m128 a)</para>
        ///   <para>  VEXPANDPS xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<float> Expand(Vector128<float> merge, Vector128<float> mask, Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_expand_epi16 (__m128i s, __mmask8 k, __m128i a)</para>
        ///   <para>  VPEXPANDW xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<ushort> Expand(Vector128<ushort> merge, Vector128<ushort> mask, Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_expand_epi32 (__m128i s, __mmask8 k, __m128i a)</para>
        ///   <para>  VPEXPANDD xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<uint> Expand(Vector128<uint> merge, Vector128<uint> mask, Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_expand_epi64 (__m128i s, __mmask8 k, __m128i a)</para>
        ///   <para>  VPEXPANDQ xmm1 {k1}{z}, xmm2</para>
        /// </summary>
        public static Vector128<ulong> Expand(Vector128<ulong> merge, Vector128<ulong> mask, Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_mask_expand_epi8 (__m256i s, __mmask32 k, __m256i a)</para>
        ///   <para>  VPEXPANDB ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<byte> Expand(Vector256<byte> merge, Vector256<byte> mask, Vector256<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_value_expand_pd (__m256d s, __mmask8 k, __m256d a)</para>
        ///   <para>  VEXPANDPD ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<double> Expand(Vector256<double> merge, Vector256<double> mask, Vector256<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_expand_epi16 (__m256i s, __mmask16 k, __m256i a)</para>
        ///   <para>  VPEXPANDW ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<short> Expand(Vector256<short> merge, Vector256<short> mask, Vector256<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_value_expand_epi32 (__m256i s, __mmask8 k, __m256i a)</para>
        ///   <para>  VPEXPANDD ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<int> Expand(Vector256<int> merge, Vector256<int> mask, Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_value_expand_epi64 (__m256i s, __mmask8 k, __m256i a)</para>
        ///   <para>  VPEXPANDQ ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<long> Expand(Vector256<long> merge, Vector256<long> mask, Vector256<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_expand_epi8 (__m256i s, __mmask32 k, __m256i a)</para>
        ///   <para>  VPEXPANDB ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<sbyte> Expand(Vector256<sbyte> merge, Vector256<sbyte> mask, Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_value_expand_ps (__m256 s, __mmask8 k, __m256 a)</para>
        ///   <para>  VEXPANDPS ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<float> Expand(Vector256<float> merge, Vector256<float> mask, Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_expand_epi16 (__m256i s, __mmask16 k, __m256i a)</para>
        ///   <para>  VPEXPANDW ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<ushort> Expand(Vector256<ushort> merge, Vector256<ushort> mask, Vector256<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_value_expand_epi32 (__m256i s, __mmask8 k, __m256i a)</para>
        ///   <para>  VPEXPANDD ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<uint> Expand(Vector256<uint> merge, Vector256<uint> mask, Vector256<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_value_expand_epi64 (__m256i s, __mmask8 k, __m256i a)</para>
        ///   <para>  VPEXPANDQ ymm1 {k1}{z}, ymm2</para>
        /// </summary>
        public static Vector256<ulong> Expand(Vector256<ulong> merge, Vector256<ulong> mask, Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_mask_expandloadu_epi8 (__m128i s, __mmask16 k, void const * a)</para>
        ///   <para>  VPEXPANDB xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<byte> ExpandLoad(byte* address, Vector128<byte> mask, Vector128<byte> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_mask_expandloadu_pd (__m128d s, __mmask8 k, void const * a)</para>
        ///   <para>  VEXPANDPD xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<double> ExpandLoad(double* address, Vector128<double> mask, Vector128<double> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_expandloadu_epi16 (__m128i s, __mmask8 k, void const * a)</para>
        ///   <para>  VPEXPANDW xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<short> ExpandLoad(short* address, Vector128<short> mask, Vector128<short> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_expandloadu_epi32 (__m128i s, __mmask8 k, void const * a)</para>
        ///   <para>  VPEXPANDD xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<int> ExpandLoad(int* address, Vector128<int> mask, Vector128<int> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_expandloadu_epi64 (__m128i s, __mmask8 k, void const * a)</para>
        ///   <para>  VPEXPANDQ xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<long> ExpandLoad(long* address, Vector128<long> mask, Vector128<long> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_expandloadu_epi8 (__m128i s, __mmask16 k, void const * a)</para>
        ///   <para>  VPEXPANDB xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<sbyte> ExpandLoad(sbyte* address, Vector128<sbyte> mask, Vector128<sbyte> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_mask_expandloadu_ps (__m128 s, __mmask8 k, void const * a)</para>
        ///   <para>  VEXPANDPS xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<float> ExpandLoad(float* address, Vector128<float> mask, Vector128<float> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_expandloadu_epi16 (__m128i s, __mmask8 k, void const * a)</para>
        ///   <para>  VPEXPANDW xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<ushort> ExpandLoad(ushort* address, Vector128<ushort> mask, Vector128<ushort> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_expandloadu_epi32 (__m128i s, __mmask8 k, void const * a)</para>
        ///   <para>  VPEXPANDD xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<uint> ExpandLoad(uint* address, Vector128<uint> mask, Vector128<uint> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_expandloadu_epi64 (__m128i s, __mmask8 k, void const * a)</para>
        ///   <para>  VPEXPANDQ xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<ulong> ExpandLoad(ulong* address, Vector128<ulong> mask, Vector128<ulong> merge) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_mask_expandloadu_epi8 (__m256i s, __mmask32 k, void const * a)</para>
        ///   <para>  VPEXPANDB ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<byte> ExpandLoad(byte* address, Vector256<byte> mask, Vector256<byte> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_address_expandloadu_pd (__m256d s, __mmask8 k, void const * a)</para>
        ///   <para>  VEXPANDPD ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<double> ExpandLoad(double* address, Vector256<double> mask, Vector256<double> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_expandloadu_epi16 (__m256i s, __mmask16 k, void const * a)</para>
        ///   <para>  VPEXPANDW ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<short> ExpandLoad(short* address, Vector256<short> mask, Vector256<short> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_address_expandloadu_epi32 (__m256i s, __mmask8 k, void const * a)</para>
        ///   <para>  VPEXPANDD ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<int> ExpandLoad(int* address, Vector256<int> mask, Vector256<int> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_address_expandloadu_epi64 (__m256i s, __mmask8 k, void const * a)</para>
        ///   <para>  VPEXPANDQ ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<long> ExpandLoad(long* address, Vector256<long> mask, Vector256<long> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_expandloadu_epi8 (__m256i s, __mmask32 k, void const * a)</para>
        ///   <para>  VPEXPANDB ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<sbyte> ExpandLoad(sbyte* address, Vector256<sbyte> mask, Vector256<sbyte> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_address_expandloadu_ps (__m256 s, __mmask8 k, void const * a)</para>
        ///   <para>  VEXPANDPS ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<float> ExpandLoad(float* address, Vector256<float> mask, Vector256<float> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_expandloadu_epi16 (__m256i s, __mmask16 k, void const * a)</para>
        ///   <para>  VPEXPANDW ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<ushort> ExpandLoad(ushort* address, Vector256<ushort> mask, Vector256<ushort> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_address_expandloadu_epi32 (__m256i s, __mmask8 k, void const * a)</para>
        ///   <para>  VPEXPANDD ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<uint> ExpandLoad(uint* address, Vector256<uint> mask, Vector256<uint> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_address_expandloadu_epi64 (__m256i s, __mmask8 k, void const * a)</para>
        ///   <para>  VPEXPANDQ ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<ulong> ExpandLoad(ulong* address, Vector256<ulong> mask, Vector256<ulong> merge) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_fixupimm_pd(__m128d a, __m128d b, __m128i tbl, int imm);</para>
        ///   <para>  VFIXUPIMMPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<double> Fixup(Vector128<double> left, Vector128<double> right, Vector128<long> table, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_fixupimm_ps(__m128 a, __m128 b, __m128i tbl, int imm);</para>
        ///   <para>  VFIXUPIMMPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<float> Fixup(Vector128<float> left, Vector128<float> right, Vector128<int> table, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256d _mm256_fixupimm_pd(__m256d a, __m256d b, __m256i tbl, int imm);</para>
        ///   <para>  VFIXUPIMMPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<double> Fixup(Vector256<double> left, Vector256<double> right, Vector256<long> table, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_fixupimm_ps(__m256 a, __m256 b, __m256i tbl, int imm);</para>
        ///   <para>  VFIXUPIMMPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<float> Fixup(Vector256<float> left, Vector256<float> right, Vector256<int> table, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_fixupimm_sd(__m128d a, __m128d b, __m128i tbl, int imm);</para>
        ///   <para>  VFIXUPIMMSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8</para>
        /// </summary>
        public static Vector128<double> FixupScalar(Vector128<double> left, Vector128<double> right, Vector128<long> table, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_fixupimm_ss(__m128 a, __m128 b, __m128i tbl, int imm);</para>
        ///   <para>  VFIXUPIMMSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8</para>
        /// </summary>
        public static Vector128<float> FixupScalar(Vector128<float> left, Vector128<float> right, Vector128<int> table, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_fnmadd_round_sd (__m128d a, __m128d b, __m128d c, int r)</para>
        ///   <para>  VFNMADDSD xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> FusedMultiplyAddNegatedScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_fnmadd_round_ss (__m128 a, __m128 b, __m128 c, int r)</para>
        ///   <para>  VFNMADDSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> FusedMultiplyAddNegatedScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_fmadd_round_sd (__m128d a, __m128d b, __m128d c, int r)</para>
        ///   <para>  VFMADDSD xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> FusedMultiplyAddScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_fmadd_round_ss (__m128 a, __m128 b, __m128 c, int r)</para>
        ///   <para>  VFMADDSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> FusedMultiplyAddScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_fnmsub_round_sd (__m128d a, __m128d b, __m128d c, int r)</para>
        ///   <para>  VFNMSUBSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> FusedMultiplySubtractNegatedScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_fnmsub_round_ss (__m128 a, __m128 b, __m128 c, int r)</para>
        ///   <para>  VFNMSUBSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> FusedMultiplySubtractNegatedScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_fmsub_round_sd (__m128d a, __m128d b, __m128d c, int r)</para>
        ///   <para>  VFMSUBSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> FusedMultiplySubtractScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_fmsub_round_ss (__m128 a, __m128 b, __m128 c, int r)</para>
        ///   <para>  VFMSUBSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> FusedMultiplySubtractScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_getexp_pd (__m128d a)</para>
        ///   <para>  VGETEXPPD xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> GetExponent(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_getexp_ps (__m128 a)</para>
        ///   <para>  VGETEXPPS xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> GetExponent(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256d _mm256_getexp_pd (__m256d a)</para>
        ///   <para>  VGETEXPPD ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> GetExponent(Vector256<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_getexp_ps (__m256 a)</para>
        ///   <para>  VGETEXPPS ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> GetExponent(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_getexp_sd (__m128d a, __m128d b)</para>
        ///   <para>  VGETEXPSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<double> GetExponentScalar(Vector128<double> upper, Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_getexp_sd (__m128d a)</para>
        ///   <para>  VGETEXPSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}</para>
        /// </summary>
        public static Vector128<double> GetExponentScalar(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_getexp_ss (__m128 a, __m128 b)</para>
        ///   <para>  VGETEXPSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<float> GetExponentScalar(Vector128<float> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_getexp_ss (__m128 a)</para>
        ///   <para>  VGETEXPSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}</para>
        /// </summary>
        public static Vector128<float> GetExponentScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_getmant_pd (__m128d a)</para>
        ///   <para>  VGETMANTPD xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> GetMantissa(Vector128<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_getmant_ps (__m128 a)</para>
        ///   <para>  VGETMANTPS xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> GetMantissa(Vector128<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256d _mm256_getmant_pd (__m256d a)</para>
        ///   <para>  VGETMANTPD ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> GetMantissa(Vector256<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_getmant_ps (__m256 a)</para>
        ///   <para>  VGETMANTPS ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> GetMantissa(Vector256<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_getmant_sd (__m128d a, __m128d b)</para>
        ///   <para>  VGETMANTSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<double> GetMantissaScalar(Vector128<double> upper, Vector128<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_getmant_sd (__m128d a)</para>
        ///   <para>  VGETMANTSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}</para>
        /// </summary>
        public static Vector128<double> GetMantissaScalar(Vector128<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_getmant_ss (__m128 a, __m128 b)</para>
        ///   <para>  VGETMANTSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<float> GetMantissaScalar(Vector128<float> upper, Vector128<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_getmant_ss (__m128 a)</para>
        ///   <para>  VGETMANTSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}</para>
        /// </summary>
        public static Vector128<float> GetMantissaScalar(Vector128<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_lzcnt_epi32 (__m128i a)</para>
        ///   <para>  VPLZCNTD xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> LeadingZeroCount(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_lzcnt_epi64 (__m128i a)</para>
        ///   <para>  VPLZCNTQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> LeadingZeroCount(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_lzcnt_epi32 (__m128i a)</para>
        ///   <para>  VPLZCNTD xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> LeadingZeroCount(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_lzcnt_epi64 (__m128i a)</para>
        ///   <para>  VPLZCNTQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> LeadingZeroCount(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_lzcnt_epi32 (__m256i a)</para>
        ///   <para>  VPLZCNTD ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> LeadingZeroCount(Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_lzcnt_epi64 (__m256i a)</para>
        ///   <para>  VPLZCNTQ ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> LeadingZeroCount(Vector256<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_lzcnt_epi32 (__m256i a)</para>
        ///   <para>  VPLZCNTD ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> LeadingZeroCount(Vector256<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_lzcnt_epi64 (__m256i a)</para>
        ///   <para>  VPLZCNTQ ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> LeadingZeroCount(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_mask_loadu_epi8 (__m128i s, __mmask16 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU8 xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<byte> MaskLoad(byte* address, Vector128<byte> mask, Vector128<byte> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_mask_loadu_pd (__m128d s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVUPD xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<double> MaskLoad(double* address, Vector128<double> mask, Vector128<double> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_loadu_epi16 (__m128i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU32 xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<short> MaskLoad(short* address, Vector128<short> mask, Vector128<short> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_loadu_epi32 (__m128i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU32 xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<int> MaskLoad(int* address, Vector128<int> mask, Vector128<int> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_loadu_epi64 (__m128i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU64 xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<long> MaskLoad(long* address, Vector128<long> mask, Vector128<long> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_loadu_epi8 (__m128i s, __mmask16 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU8 xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<sbyte> MaskLoad(sbyte* address, Vector128<sbyte> mask, Vector128<sbyte> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_mask_loadu_ps (__m128 s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVUPS xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<float> MaskLoad(float* address, Vector128<float> mask, Vector128<float> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_loadu_epi16 (__m128i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU32 xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<ushort> MaskLoad(ushort* address, Vector128<ushort> mask, Vector128<ushort> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_loadu_epi32 (__m128i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU32 xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<uint> MaskLoad(uint* address, Vector128<uint> mask, Vector128<uint> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_loadu_epi64 (__m128i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU64 xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<ulong> MaskLoad(ulong* address, Vector128<ulong> mask, Vector128<ulong> merge) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_mask_loadu_epi8 (__m256i s, __mmask32 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU8 ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<byte> MaskLoad(byte* address, Vector256<byte> mask, Vector256<byte> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_mask_loadu_pd (__m256d s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVUPD ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<double> MaskLoad(double* address, Vector256<double> mask, Vector256<double> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_loadu_epi16 (__m256i s, __mmask16 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU32 ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<short> MaskLoad(short* address, Vector256<short> mask, Vector256<short> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_loadu_epi32 (__m256i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU32 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<int> MaskLoad(int* address, Vector256<int> mask, Vector256<int> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_loadu_epi64 (__m256i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU64 ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<long> MaskLoad(long* address, Vector256<long> mask, Vector256<long> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_loadu_epi8 (__m256i s, __mmask32 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU8 ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<sbyte> MaskLoad(sbyte* address, Vector256<sbyte> mask, Vector256<sbyte> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_mask_loadu_ps (__m256 s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVUPS ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<float> MaskLoad(float* address, Vector256<float> mask, Vector256<float> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_loadu_epi16 (__m256i s, __mmask16 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU32 ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<ushort> MaskLoad(ushort* address, Vector256<ushort> mask, Vector256<ushort> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_loadu_epi32 (__m256i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU32 ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<uint> MaskLoad(uint* address, Vector256<uint> mask, Vector256<uint> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_loadu_epi64 (__m256i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU64 ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<ulong> MaskLoad(ulong* address, Vector256<ulong> mask, Vector256<ulong> merge) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_mask_load_pd (__m128d s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVAPD xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<double> MaskLoadAligned(double* address, Vector128<double> mask, Vector128<double> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_load_epi32 (__m128i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQA32 xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<int> MaskLoadAligned(int* address, Vector128<int> mask, Vector128<int> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_load_epi64 (__m128i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQA64 xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<long> MaskLoadAligned(long* address, Vector128<long> mask, Vector128<long> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_mask_load_ps (__m128 s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVAPS xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<float> MaskLoadAligned(float* address, Vector128<float> mask, Vector128<float> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_load_epi32 (__m128i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQA32 xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<uint> MaskLoadAligned(uint* address, Vector128<uint> mask, Vector128<uint> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_load_epi64 (__m128i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQA64 xmm1 {k1}{z}, m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector128<ulong> MaskLoadAligned(ulong* address, Vector128<ulong> mask, Vector128<ulong> merge) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256d _mm256_mask_load_pd (__m256d s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVAPD ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<double> MaskLoadAligned(double* address, Vector256<double> mask, Vector256<double> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_load_epi32 (__m256i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQA32 ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<int> MaskLoadAligned(int* address, Vector256<int> mask, Vector256<int> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_load_epi64 (__m256i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQA64 ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<long> MaskLoadAligned(long* address, Vector256<long> mask, Vector256<long> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_mask_load_ps (__m256 s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVAPS ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<float> MaskLoadAligned(float* address, Vector256<float> mask, Vector256<float> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_load_epi32 (__m256i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQA32 ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<uint> MaskLoadAligned(uint* address, Vector256<uint> mask, Vector256<uint> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_load_epi64 (__m256i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQA64 ymm1 {k1}{z}, m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector256<ulong> MaskLoadAligned(ulong* address, Vector256<ulong> mask, Vector256<ulong> merge) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>void _mm_mask_storeu_si128 (void * mem_addr, __mmask16 k, __m128i a)</para>
        ///   <para>  VMOVDQU8 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void MaskStore(byte* address, Vector128<byte> mask, Vector128<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_mask_storeu_pd (void * mem_addr, __mmask8 k, __m128d a)</para>
        ///   <para>  VMOVUPD m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static new unsafe void MaskStore(double* address, Vector128<double> mask, Vector128<double> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_mask_storeu_si128 (void * mem_addr, __mmask8 k, __m128i a)</para>
        ///   <para>  VMOVDQU16 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void MaskStore(short* address, Vector128<short> mask, Vector128<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_mask_storeu_epi32 (void * mem_addr, __mmask8 k, __m128i a)</para>
        ///   <para>  VMOVDQU32 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static new unsafe void MaskStore(int* address, Vector128<int> mask, Vector128<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_mask_storeu_epi64 (void * mem_addr, __mmask8 k, __m128i a)</para>
        ///   <para>  VMOVDQU64 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static new unsafe void MaskStore(long* address, Vector128<long> mask, Vector128<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_mask_storeu_si128 (void * mem_addr, __mmask16 k, __m128i a)</para>
        ///   <para>  VMOVDQU8 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void MaskStore(sbyte* address, Vector128<sbyte> mask, Vector128<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_mask_storeu_ps (void * mem_addr, __mmask8 k, __m128 a)</para>
        ///   <para>  VMOVUPS m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static new unsafe void MaskStore(float* address, Vector128<float> mask, Vector128<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_mask_storeu_si128 (void * mem_addr, __mmask8 k, __m128i a)</para>
        ///   <para>  VMOVDQU16 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void MaskStore(ushort* address, Vector128<ushort> mask, Vector128<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_mask_storeu_epi32 (void * mem_addr, __mmask8 k, __m128i a)</para>
        ///   <para>  VMOVDQU32 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static new unsafe void MaskStore(uint* address, Vector128<uint> mask, Vector128<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_mask_storeu_epi64 (void * mem_addr, __mmask8 k, __m128i a)</para>
        ///   <para>  VMOVDQU64 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static new unsafe void MaskStore(ulong* address, Vector128<ulong> mask, Vector128<ulong> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>void _mm256_mask_storeu_si256 (void * mem_addr, __mmask32 k, __m256i a)</para>
        ///   <para>  VMOVDQU8 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void MaskStore(byte* address, Vector256<byte> mask, Vector256<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_storeu_pd (void * mem_addr, __mmask8 k, __m256d a)</para>
        ///   <para>  VMOVUPD m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static new unsafe void MaskStore(double* address, Vector256<double> mask, Vector256<double> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_storeu_si256 (void * mem_addr, __mmask16 k, __m256i a)</para>
        ///   <para>  VMOVDQU16 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void MaskStore(short* address, Vector256<short> mask, Vector256<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_storeu_epi32 (void * mem_addr, __mmask8 k, __m256i a)</para>
        ///   <para>  VMOVDQU32 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static new unsafe void MaskStore(int* address, Vector256<int> mask, Vector256<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_storeu_epi64 (void * mem_addr, __mmask8 k, __m256i a)</para>
        ///   <para>  VMOVDQU64 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static new unsafe void MaskStore(long* address, Vector256<long> mask, Vector256<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_storeu_si256 (void * mem_addr, __mmask32 k, __m256i a)</para>
        ///   <para>  VMOVDQU8 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void MaskStore(sbyte* address, Vector256<sbyte> mask, Vector256<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_storeu_ps (void * mem_addr, __mmask8 k, __m256 a)</para>
        ///   <para>  VMOVUPS m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static new unsafe void MaskStore(float* address, Vector256<float> mask, Vector256<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_storeu_si256 (void * mem_addr, __mmask16 k, __m256i a)</para>
        ///   <para>  VMOVDQU16 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void MaskStore(ushort* address, Vector256<ushort> mask, Vector256<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_storeu_epi32 (void * mem_addr, __mmask8 k, __m256i a)</para>
        ///   <para>  VMOVDQU32 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static new unsafe void MaskStore(uint* address, Vector256<uint> mask, Vector256<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_storeu_epi64 (void * mem_addr, __mmask8 k, __m256i a)</para>
        ///   <para>  VMOVDQU64 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static new unsafe void MaskStore(ulong* address, Vector256<ulong> mask, Vector256<ulong> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>void _mm_mask_store_pd (void * mem_addr, __mmask8 k, __m128d a)</para>
        ///   <para>  VMOVAPD m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(double* address, Vector128<double> mask, Vector128<double> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_mask_store_epi32 (void * mem_addr, __mmask8 k, __m128i a)</para>
        ///   <para>  VMOVDQA32 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(int* address, Vector128<int> mask, Vector128<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_mask_store_epi64 (void * mem_addr, __mmask8 k, __m128i a)</para>
        ///   <para>  VMOVDQA32 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(long* address, Vector128<long> mask, Vector128<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_mask_store_ps (void * mem_addr, __mmask8 k, __m128 a)</para>
        ///   <para>  VMOVAPS m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(float* address, Vector128<float> mask, Vector128<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_mask_store_epi32 (void * mem_addr, __mmask8 k, __m128i a)</para>
        ///   <para>  VMOVDQA32 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(uint* address, Vector128<uint> mask, Vector128<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_mask_store_epi64 (void * mem_addr, __mmask8 k, __m128i a)</para>
        ///   <para>  VMOVDQA32 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(ulong* address, Vector128<ulong> mask, Vector128<ulong> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>void _mm256_mask_store_pd (void * mem_addr, __mmask8 k, __m256d a)</para>
        ///   <para>  VMOVAPD m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(double* address, Vector256<double> mask, Vector256<double> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_store_epi32 (void * mem_addr, __mmask8 k, __m256i a)</para>
        ///   <para>  VMOVDQA32 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(int* address, Vector256<int> mask, Vector256<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_store_epi64 (void * mem_addr, __mmask8 k, __m256i a)</para>
        ///   <para>  VMOVDQA32 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(long* address, Vector256<long> mask, Vector256<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_store_ps (void * mem_addr, __mmask8 k, __m256 a)</para>
        ///   <para>  VMOVAPS m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(float* address, Vector256<float> mask, Vector256<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_store_epi32 (void * mem_addr, __mmask8 k, __m256i a)</para>
        ///   <para>  VMOVDQA32 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(uint* address, Vector256<uint> mask, Vector256<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_mask_store_epi64 (void * mem_addr, __mmask8 k, __m256i a)</para>
        ///   <para>  VMOVDQA32 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(ulong* address, Vector256<ulong> mask, Vector256<ulong> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_max_epi64 (__m128i a, __m128i b)</para>
        ///   <para>  VPMAXSQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> Max(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_max_epu64 (__m128i a, __m128i b)</para>
        ///   <para>  VPMAXUQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> Max(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_max_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPMAXSQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> Max(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_max_epu64 (__m256i a, __m256i b)</para>
        ///   <para>  VPMAXUQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> Max(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_min_epi64 (__m128i a, __m128i b)</para>
        ///   <para>  VPMINSQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> Min(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_min_epu64 (__m128i a, __m128i b)</para>
        ///   <para>  VPMINUQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> Min(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_min_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPMINSQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> Min(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_min_epu64 (__m256i a, __m256i b)</para>
        ///   <para>  VPMINUQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> Min(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>unsigned int _cvtmask16_u32 (__mmask16 a)</para>
        ///   <para>  KMOVW r32, k1</para>
        /// </summary>
        public static new int MoveMask(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
        ///   <para>  KMOVB r32, k1</para>
        /// </summary>
        public static new int MoveMask(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
        ///   <para>  KMOVB r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
        ///   <para>  KMOVB r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
        ///   <para>  KMOVB r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask16_u32 (__mmask16 a)</para>
        ///   <para>  KMOVW r32, k1</para>
        /// </summary>
        public static new int MoveMask(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
        ///   <para>  KMOVB r32, k1</para>
        /// </summary>
        public static new int MoveMask(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
        ///   <para>  KMOVB r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
        ///   <para>  KMOVB r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
        ///   <para>  KMOVB r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>unsigned int _cvtmask32_u32 (__mmask32 a)</para>
        ///   <para>  KMOVD r32, k1</para>
        /// </summary>
        public static new int MoveMask(Vector256<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
        ///   <para>  KMOVB r32, k1</para>
        /// </summary>
        public static new int MoveMask(Vector256<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask16_u32 (__mmask16 a)</para>
        ///   <para>  KMOVW r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector256<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
        ///   <para>  KMOVB r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
        ///   <para>  KMOVB r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector256<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask32_u32 (__mmask32 a)</para>
        ///   <para>  KMOVD r32, k1</para>
        /// </summary>
        public static new int MoveMask(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
        ///   <para>  KMOVB r32, k1</para>
        /// </summary>
        public static new int MoveMask(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask16_u32 (__mmask16 a)</para>
        ///   <para>  KMOVW r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
        ///   <para>  KMOVB r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector256<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
        ///   <para>  KMOVB r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_mullo_epi64 (__m128i a, __m128i b)</para>
        ///   <para>  VPMULLQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> MultiplyLow(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mullo_epi64 (__m128i a, __m128i b)</para>
        ///   <para>  VPMULLQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> MultiplyLow(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_mullo_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPMULLQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> MultiplyLow(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mullo_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPMULLQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> MultiplyLow(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_mul_round_sd (__m128d a, __m128d b, int rounding)</para>
        ///   <para>  VMULSD xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> MultiplyScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_mul_round_ss (__m128 a, __m128 b, int rounding)</para>
        ///   <para>  VMULSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> MultiplyScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_multishift_epi64_epi8(__m128i a, __m128i b)</para>
        ///   <para>  VPMULTISHIFTQB xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<byte> MultiShift(Vector128<byte> control, Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_multishift_epi64_epi8(__m128i a, __m128i b)</para>
        ///   <para>  VPMULTISHIFTQB xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<sbyte> MultiShift(Vector128<sbyte> control, Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_multishift_epi64_epi8(__m256i a, __m256i b)</para>
        ///   <para>  VPMULTISHIFTQB ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<byte> MultiShift(Vector256<byte> control, Vector256<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_multishift_epi64_epi8(__m256i a, __m256i b)</para>
        ///   <para>  VPMULTISHIFTQB ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<sbyte> MultiShift(Vector256<sbyte> control, Vector256<long> value) { throw new PlatformNotSupportedException(); }

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
        ///   <para>__m128i _mm_permutexvar_epi8 (__m128i idx, __m128i a)</para>
        ///   <para>  VPERMB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector128<byte> PermuteVar16x8(Vector128<byte> left, Vector128<byte> control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_permutexvar_epi8 (__m128i idx, __m128i a)</para>
        ///   <para>  VPERMB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector128<sbyte> PermuteVar16x8(Vector128<sbyte> left, Vector128<sbyte> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_permutex2var_epi8 (__m128i a, __m128i idx, __m128i b)</para>
        ///   <para>  VPERMI2B xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        ///   <para>  VPERMT2B xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> PermuteVar16x8x2(Vector128<byte> lower, Vector128<byte> indices, Vector128<byte> upper) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_permutex2var_epi8 (__m128i a, __m128i idx, __m128i b)</para>
        ///   <para>  VPERMI2B xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        ///   <para>  VPERMT2B xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> PermuteVar16x8x2(Vector128<sbyte> lower, Vector128<sbyte> indices, Vector128<sbyte> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_permutex2var_pd (__m128d a, __m128i idx, __m128i b)</para>
        ///   <para>  VPERMI2PD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        ///   <para>  VPERMT2PD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> PermuteVar2x64x2(Vector128<double> lower, Vector128<long> indices, Vector128<double> upper) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_permutex2var_epi64 (__m128i a, __m128i idx, __m128i b)</para>
        ///   <para>  VPERMI2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        ///   <para>  VPERMT2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> PermuteVar2x64x2(Vector128<long> lower, Vector128<long> indices, Vector128<long> upper) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_permutex2var_epi64 (__m128i a, __m128i idx, __m128i b)</para>
        ///   <para>  VPERMI2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        ///   <para>  VPERMT2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> PermuteVar2x64x2(Vector128<ulong> lower, Vector128<ulong> indices, Vector128<ulong> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_permutexvar_epi8 (__m256i idx, __m256i a)</para>
        ///   <para>  VPERMB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector256<byte> PermuteVar32x8(Vector256<byte> left, Vector256<byte> control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permutexvar_epi8 (__m256i idx, __m256i a)</para>
        ///   <para>  VPERMB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector256<sbyte> PermuteVar32x8(Vector256<sbyte> left, Vector256<sbyte> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_permutex2var_epi8 (__m256i a, __m256i idx, __m256i b)</para>
        ///   <para>  VPERMI2B ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        ///   <para>  VPERMT2B ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> PermuteVar32x8x2(Vector256<byte> lower, Vector256<byte> indices, Vector256<byte> upper) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permutex2var_epi8 (__m256i a, __m256i idx, __m256i b)</para>
        ///   <para>  VPERMI2B ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        ///   <para>  VPERMT2B ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> PermuteVar32x8x2(Vector256<sbyte> lower, Vector256<sbyte> indices, Vector256<sbyte> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_permutex2var_ps (__m128 a, __m128i idx, __m128i b)</para>
        ///   <para>  VPERMI2PS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        ///   <para>  VPERMT2PS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> PermuteVar4x32x2(Vector128<float> lower, Vector128<int> indices, Vector128<float> upper) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_permutex2var_epi32 (__m128i a, __m128i idx, __m128i b)</para>
        ///   <para>  VPERMI2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        ///   <para>  VPERMT2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> PermuteVar4x32x2(Vector128<int> lower, Vector128<int> indices, Vector128<int> upper) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_permutex2var_epi32 (__m128i a, __m128i idx, __m128i b)</para>
        ///   <para>  VPERMI2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        ///   <para>  VPERMT2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> PermuteVar4x32x2(Vector128<uint> lower, Vector128<uint> indices, Vector128<uint> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256d _mm256_permute4x64_pd (__m256d a, __m256i b)</para>
        ///   <para>  VPERMPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> PermuteVar4x64(Vector256<double> value, Vector256<long> control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permutexvar_epi64 (__m256i idx, __m256i a)</para>
        ///   <para>  VPERMQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector256<long> PermuteVar4x64(Vector256<long> value, Vector256<long> control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permutexvar_epi64 (__m256i idx, __m256i a)</para>
        ///   <para>  VPERMQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector256<ulong> PermuteVar4x64(Vector256<ulong> value, Vector256<ulong> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256d _mm256_permutex2var_pd (__m256d a, __m256i idx, __m256i b)</para>
        ///   <para>  VPERMI2PD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        ///   <para>  VPERMT2PD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> PermuteVar4x64x2(Vector256<double> lower, Vector256<long> indices, Vector256<double> upper) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permutex2var_epi64 (__m256i a, __m256i idx, __m256i b)</para>
        ///   <para>  VPERMI2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        ///   <para>  VPERMT2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> PermuteVar4x64x2(Vector256<long> lower, Vector256<long> indices, Vector256<long> upper) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permutex2var_epi64 (__m256i a, __m256i idx, __m256i b)</para>
        ///   <para>  VPERMI2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        ///   <para>  VPERMT2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> PermuteVar4x64x2(Vector256<ulong> lower, Vector256<ulong> indices, Vector256<ulong> upper) { throw new PlatformNotSupportedException(); }

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
        ///   <para>__m256 _mm256_permutex2var_ps (__m256 a, __m256i idx, __m256i b)</para>
        ///   <para>  VPERMI2PS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        ///   <para>  VPERMT2PS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> PermuteVar8x32x2(Vector256<float> lower, Vector256<int> indices, Vector256<float> upper) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permutex2var_epi32 (__m256i a, __m256i idx, __m256i b)</para>
        ///   <para>  VPERMI2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        ///   <para>  VPERMT2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> PermuteVar8x32x2(Vector256<int> lower, Vector256<int> indices, Vector256<int> upper) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permutex2var_epi32 (__m256i a, __m256i idx, __m256i b)</para>
        ///   <para>  VPERMI2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        ///   <para>  VPERMT2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> PermuteVar8x32x2(Vector256<uint> lower, Vector256<uint> indices, Vector256<uint> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_range_pd(__m128d a, __m128d b, int imm);</para>
        ///   <para>  VRANGEPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<double> Range(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_range_ps(__m128 a, __m128 b, int imm);</para>
        ///   <para>  VRANGEPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<float> Range(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256d _mm256_range_pd(__m256d a, __m256d b, int imm);</para>
        ///   <para>  VRANGEPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<double> Range(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_range_ps(__m256 a, __m256 b, int imm);</para>
        ///   <para>  VRANGEPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<float> Range(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_range_sd(__m128d a, __m128d b, int imm);</para>
        ///   <para>  VRANGESD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8</para>
        /// </summary>
        public static Vector128<double> RangeScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_range_ss(__m128 a, __m128 b, int imm);</para>
        ///   <para>  VRANGESS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8</para>
        /// </summary>
        public static Vector128<float> RangeScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_rcp14_pd (__m128d a, __m128d b)</para>
        ///   <para>  VRCP14PD xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> Reciprocal14(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_rcp14_ps (__m128 a, __m128 b)</para>
        ///   <para>  VRCP14PS xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> Reciprocal14(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256d _mm256_rcp14_pd (__m256d a, __m256d b)</para>
        ///   <para>  VRCP14PD ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> Reciprocal14(Vector256<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_rcp14_ps (__m256 a, __m256 b)</para>
        ///   <para>  VRCP14PS ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> Reciprocal14(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_rcp14_sd (__m128d a, __m128d b)</para>
        ///   <para>  VRCP14SD xmm1 {k1}{z}, xmm2, xmm3/m64</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<double> Reciprocal14Scalar(Vector128<double> upper, Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_rcp14_sd (__m128d a)</para>
        ///   <para>  VRCP14SD xmm1 {k1}{z}, xmm2, xmm3/m64</para>
        /// </summary>
        public static Vector128<double> Reciprocal14Scalar(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_rcp14_ss (__m128 a, __m128 b)</para>
        ///   <para>  VRCP14SS xmm1 {k1}{z}, xmm2, xmm3/m32</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<float> Reciprocal14Scalar(Vector128<float> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_rcp14_ss (__m128 a)</para>
        ///   <para>  VRCP14SS xmm1 {k1}{z}, xmm2, xmm3/m32</para>
        /// </summary>
        public static Vector128<float> Reciprocal14Scalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_rsqrt14_pd (__m128d a, __m128d b)</para>
        ///   <para>  VRSQRT14PD xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> ReciprocalSqrt14(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_rsqrt14_ps (__m128 a, __m128 b)</para>
        ///   <para>  VRSQRT14PS xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> ReciprocalSqrt14(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256d _mm256_rsqrt14_pd (__m256d a, __m256d b)</para>
        ///   <para>  VRSQRT14PD ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> ReciprocalSqrt14(Vector256<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_rsqrt14_ps (__m256 a, __m256 b)</para>
        ///   <para>  VRSQRT14PS ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> ReciprocalSqrt14(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_rsqrt14_sd (__m128d a, __m128d b)</para>
        ///   <para>  VRSQRT14SD xmm1 {k1}{z}, xmm2, xmm3/m64</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<double> ReciprocalSqrt14Scalar(Vector128<double> upper, Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_rsqrt14_sd (__m128d a)</para>
        ///   <para>  VRSQRT14SD xmm1 {k1}{z}, xmm2, xmm3/m64</para>
        /// </summary>
        public static Vector128<double> ReciprocalSqrt14Scalar(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_rsqrt14_ss (__m128 a, __m128 b)</para>
        ///   <para>  VRSQRT14SS xmm1 {k1}{z}, xmm2, xmm3/m32</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<float> ReciprocalSqrt14Scalar(Vector128<float> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_rsqrt14_ss (__m128 a)</para>
        ///   <para>  VRSQRT14SS xmm1 {k1}{z}, xmm2, xmm3/m32</para>
        /// </summary>
        public static Vector128<float> ReciprocalSqrt14Scalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_reduce_pd(__m128d a, int imm);</para>
        ///   <para>  VREDUCEPD xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<double> Reduce(Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_reduce_ps(__m128 a, int imm);</para>
        ///   <para>  VREDUCEPS xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<float> Reduce(Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256d _mm256_reduce_pd(__m256d a, int imm);</para>
        ///   <para>  VREDUCEPD ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<double> Reduce(Vector256<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_reduce_ps(__m256 a, int imm);</para>
        ///   <para>  VREDUCEPS ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<float> Reduce(Vector256<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_reduce_sd(__m128d a, __m128d b, int imm);</para>
        ///   <para>  VREDUCESD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<double> ReduceScalar(Vector128<double> upper, Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_reduce_sd(__m128d a, int imm);</para>
        ///   <para>  VREDUCESD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8</para>
        /// </summary>
        public static Vector128<double> ReduceScalar(Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_reduce_ss(__m128 a, __m128 b, int imm);</para>
        ///   <para>  VREDUCESS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<float> ReduceScalar(Vector128<float> upper, Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_reduce_ss(__m128 a, int imm);</para>
        ///   <para>  VREDUCESS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8</para>
        /// </summary>
        public static Vector128<float> ReduceScalar(Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_rol_epi32 (__m128i a, int imm8)</para>
        ///   <para>  VPROLD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<int> RotateLeft(Vector128<int> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_rol_epi64 (__m128i a, int imm8)</para>
        ///   <para>  VPROLQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<long> RotateLeft(Vector128<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_rol_epi32 (__m128i a, int imm8)</para>
        ///   <para>  VPROLD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<uint> RotateLeft(Vector128<uint> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_rol_epi64 (__m128i a, int imm8)</para>
        ///   <para>  VPROLQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<ulong> RotateLeft(Vector128<ulong> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_rol_epi32 (__m256i a, int imm8)</para>
        ///   <para>  VPROLD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<int> RotateLeft(Vector256<int> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_rol_epi64 (__m256i a, int imm8)</para>
        ///   <para>  VPROLQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<long> RotateLeft(Vector256<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_rol_epi32 (__m256i a, int imm8)</para>
        ///   <para>  VPROLD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<uint> RotateLeft(Vector256<uint> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_rol_epi64 (__m256i a, int imm8)</para>
        ///   <para>  VPROLQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<ulong> RotateLeft(Vector256<ulong> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_rolv_epi32 (__m128i a, __m128i b)</para>
        ///   <para>  VPROLDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> RotateLeftVariable(Vector128<int> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_rolv_epi64 (__m128i a, __m128i b)</para>
        ///   <para>  VPROLQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> RotateLeftVariable(Vector128<long> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_rolv_epi32 (__m128i a, __m128i b)</para>
        ///   <para>  VPROLDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> RotateLeftVariable(Vector128<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_rolv_epi64 (__m128i a, __m128i b)</para>
        ///   <para>  VPROLQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> RotateLeftVariable(Vector128<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_rolv_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPROLDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> RotateLeftVariable(Vector256<int> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_rolv_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPROLQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> RotateLeftVariable(Vector256<long> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_rolv_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPROLDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> RotateLeftVariable(Vector256<uint> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_rolv_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPROLQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> RotateLeftVariable(Vector256<ulong> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_ror_epi32 (__m128i a, int imm8)</para>
        ///   <para>  VPRORD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<int> RotateRight(Vector128<int> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_ror_epi64 (__m128i a, int imm8)</para>
        ///   <para>  VPRORQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<long> RotateRight(Vector128<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_ror_epi32 (__m128i a, int imm8)</para>
        ///   <para>  VPRORD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<uint> RotateRight(Vector128<uint> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_ror_epi64 (__m128i a, int imm8)</para>
        ///   <para>  VPRORQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<ulong> RotateRight(Vector128<ulong> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_ror_epi32 (__m256i a, int imm8)</para>
        ///   <para>  VPRORD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<int> RotateRight(Vector256<int> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_ror_epi64 (__m256i a, int imm8)</para>
        ///   <para>  VPRORQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<long> RotateRight(Vector256<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_ror_epi32 (__m256i a, int imm8)</para>
        ///   <para>  VPRORD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<uint> RotateRight(Vector256<uint> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_ror_epi64 (__m256i a, int imm8)</para>
        ///   <para>  VPRORQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<ulong> RotateRight(Vector256<ulong> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_rorv_epi32 (__m128i a, __m128i b)</para>
        ///   <para>  VPRORDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> RotateRightVariable(Vector128<int> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_rorv_epi64 (__m128i a, __m128i b)</para>
        ///   <para>  VPRORQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> RotateRightVariable(Vector128<long> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_rorv_epi32 (__m128i a, __m128i b)</para>
        ///   <para>  VPRORDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> RotateRightVariable(Vector128<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_rorv_epi64 (__m128i a, __m128i b)</para>
        ///   <para>  VPRORQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> RotateRightVariable(Vector128<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_rorv_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPRORDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> RotateRightVariable(Vector256<int> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_rorv_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPRORQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> RotateRightVariable(Vector256<long> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_rorv_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPRORDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> RotateRightVariable(Vector256<uint> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_rorv_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPRORQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> RotateRightVariable(Vector256<ulong> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_roundscale_pd (__m128d a, int imm)</para>
        ///   <para>  VRNDSCALEPD xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<double> RoundScale(Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_roundscale_ps (__m128 a, int imm)</para>
        ///   <para>  VRNDSCALEPS xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<float> RoundScale(Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256d _mm256_roundscale_pd (__m256d a, int imm)</para>
        ///   <para>  VRNDSCALEPD ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<double> RoundScale(Vector256<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_roundscale_ps (__m256 a, int imm)</para>
        ///   <para>  VRNDSCALEPS ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<float> RoundScale(Vector256<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_roundscale_sd (__m128d a, __m128d b, int imm)</para>
        ///   <para>  VRNDSCALESD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<double> RoundScaleScalar(Vector128<double> upper, Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_roundscale_sd (__m128d a, int imm)</para>
        ///   <para>  VRNDSCALESD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8</para>
        /// </summary>
        public static Vector128<double> RoundScaleScalar(Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_roundscale_ss (__m128 a, __m128 b, int imm)</para>
        ///   <para>  VRNDSCALESS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<float> RoundScaleScalar(Vector128<float> upper, Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_roundscale_ss (__m128 a, int imm)</para>
        ///   <para>  VRNDSCALESS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8</para>
        /// </summary>
        public static Vector128<float> RoundScaleScalar(Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_scalef_pd (__m128d a, int imm)</para>
        ///   <para>  VSCALEFPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> Scale(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_scalef_ps (__m128 a, int imm)</para>
        ///   <para>  VSCALEFPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> Scale(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256d _mm256_scalef_pd (__m256d a, int imm)</para>
        ///   <para>  VSCALEFPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> Scale(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_scalef_ps (__m256 a, int imm)</para>
        ///   <para>  VSCALEFPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> Scale(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_scalef_round_sd (__m128d a, __m128d b)</para>
        ///   <para>  VSCALEFSD xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> ScaleScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_scalef_sd (__m128d a, __m128d b)</para>
        ///   <para>  VSCALEFSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}</para>
        /// </summary>
        public static Vector128<double> ScaleScalar(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_scalef_round_ss (__m128 a, __m128 b)</para>
        ///   <para>  VSCALEFSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> ScaleScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_scalef_ss (__m128 a, __m128 b)</para>
        ///   <para>  VSCALEFSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}</para>
        /// </summary>
        public static Vector128<float> ScaleScalar(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

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
        ///   <para>__128i _mm_srai_epi64 (__m128i a, int imm8)</para>
        ///   <para>  VPSRAQ xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<long> ShiftRightArithmetic(Vector128<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_sra_epi64 (__m128i a, __m128i count)</para>
        ///   <para>  VPSRAQ xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<long> ShiftRightArithmetic(Vector128<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_srai_epi64 (__m256i a, int imm8)</para>
        ///   <para>  VPSRAQ ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sra_epi64 (__m256i a, __m128i count)</para>
        ///   <para>  VPSRAQ ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_srav_epi64 (__m128i a, __m128i count)</para>
        ///   <para>  VPSRAVQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> ShiftRightArithmeticVariable(Vector128<long> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_srav_epi16 (__m128i a, __m128i count)</para>
        ///   <para>  VPSRAVW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticVariable(Vector128<short> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_srav_epi64 (__m256i a, __m256i count)</para>
        ///   <para>  VPSRAVQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> ShiftRightArithmeticVariable(Vector256<long> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }
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
        ///   <para>__m256d _mm256_shuffle_f64x2 (__m256d a, __m256d b, const int imm8)</para>
        ///   <para>  VSHUFF64x2 ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<double> Shuffle2x128(Vector256<double> left, Vector256<double> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_shuffle_f32x4 (__m256 a, __m256 b, const int imm8)</para>
        ///   <para>  VSHUFF32x4 ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<float> Shuffle2x128(Vector256<float> left, Vector256<float> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_shuffle_i32x4 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VSHUFI32x4 ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<int> Shuffle2x128(Vector256<int> left, Vector256<int> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_shuffle_i64x2 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VSHUFI64x2 ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<long> Shuffle2x128(Vector256<long> left, Vector256<long> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_shuffle_i32x4 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VSHUFI32x4 ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<uint> Shuffle2x128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_shuffle_i64x2 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VSHUFI64x2 ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<ulong> Shuffle2x128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_sqrt_round_sd (__m128d a, __m128d b, int rounding)</para>
        ///   <para>  VSQRTSD xmm1, xmm2 xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> SqrtScalar(Vector128<double> upper, Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_sqrt_round_ss (__m128 a, __m128 b, int rounding)</para>
        ///   <para>  VSQRTSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> SqrtScalar(Vector128<float> upper, Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_sub_round_sd (__m128d a, __m128d b, int rounding)</para>
        ///   <para>  VSUBSD xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> SubtractScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_sub_round_ss (__m128 a, __m128 b, int rounding)</para>
        ///   <para>  VSUBSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> SubtractScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

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

        /// <summary>
        ///   <para>__m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, byte imm)</para>
        ///   <para>  VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector128<byte> TernaryLogic(Vector128<byte> a, Vector128<byte> b, Vector128<byte> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_ternarylogic_pd (__m128d a, __m128d b, __m128d c, int imm)</para>
        ///   <para>  VPTERNLOGQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector128<double> TernaryLogic(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_ternarylogic_ps (__m128 a, __m128 b, __m128 c, int imm)</para>
        ///   <para>  VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector128<float> TernaryLogic(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_ternarylogic_epi32 (__m128i a, __m128i b, __m128i c, int imm)</para>
        ///   <para>  VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<int> TernaryLogic(Vector128<int> a, Vector128<int> b, Vector128<int> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_ternarylogic_epi64 (__m128i a, __m128i b, __m128i c, int imm)</para>
        ///   <para>  VPTERNLOGQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<long> TernaryLogic(Vector128<long> a, Vector128<long> b, Vector128<long> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, byte imm)</para>
        ///   <para>  VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector128<sbyte> TernaryLogic(Vector128<sbyte> a, Vector128<sbyte> b, Vector128<sbyte> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, short imm)</para>
        ///   <para>  VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector128<short> TernaryLogic(Vector128<short> a, Vector128<short> b, Vector128<short> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_ternarylogic_epi32 (__m128i a, __m128i b, __m128i c, int imm)</para>
        ///   <para>  VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<uint> TernaryLogic(Vector128<uint> a, Vector128<uint> b, Vector128<uint> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_ternarylogic_epi64 (__m128i a, __m128i b, __m128i c, int imm)</para>
        ///   <para>  VPTERNLOGQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<ulong> TernaryLogic(Vector128<ulong> a, Vector128<ulong> b, Vector128<ulong> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, short imm)</para>
        ///   <para>  VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector128<ushort> TernaryLogic(Vector128<ushort> a, Vector128<ushort> b, Vector128<ushort> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, byte imm)</para>
        ///   <para>  VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector256<byte> TernaryLogic(Vector256<byte> a, Vector256<byte> b, Vector256<byte> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_ternarylogic_pd (__m256d a, __m256d b, __m256d c, int imm)</para>
        ///   <para>  VPTERNLOGQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector256<double> TernaryLogic(Vector256<double> a, Vector256<double> b, Vector256<double> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_ternarylogic_ps (__m256 a, __m256 b, __m256 c, int imm)</para>
        ///   <para>  VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector256<float> TernaryLogic(Vector256<float> a, Vector256<float> b, Vector256<float> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_ternarylogic_epi32 (__m256i a, __m256i b, __m256i c, int imm)</para>
        ///   <para>  VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<int> TernaryLogic(Vector256<int> a, Vector256<int> b, Vector256<int> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_ternarylogic_epi64 (__m256i a, __m256i b, __m256i c, int imm)</para>
        ///   <para>  VPTERNLOGQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<long> TernaryLogic(Vector256<long> a, Vector256<long> b, Vector256<long> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, byte imm)</para>
        ///   <para>  VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector256<sbyte> TernaryLogic(Vector256<sbyte> a, Vector256<sbyte> b, Vector256<sbyte> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, short imm)</para>
        ///   <para>  VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector256<short> TernaryLogic(Vector256<short> a, Vector256<short> b, Vector256<short> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_ternarylogic_epi32 (__m256i a, __m256i b, __m256i c, int imm)</para>
        ///   <para>  VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<uint> TernaryLogic(Vector256<uint> a, Vector256<uint> b, Vector256<uint> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_ternarylogic_epi64 (__m256i a, __m256i b, __m256i c, int imm)</para>
        ///   <para>  VPTERNLOGQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<ulong> TernaryLogic(Vector256<ulong> a, Vector256<ulong> b, Vector256<ulong> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, short imm)</para>
        ///   <para>  VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector256<ushort> TernaryLogic(Vector256<ushort> a, Vector256<ushort> b, Vector256<ushort> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>Provides access to the x86 AVX10.1 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class X64 : Avx2.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            ///   <para>__m128 _mm_cvt_roundi64_ss (__m128 a, __int64 b, int rounding)</para>
            ///   <para>  VCVTSI2SS xmm1, xmm2, r64 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, ulong value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128 _mm_cvtsi64_ss (__m128 a, __int64 b)</para>
            ///   <para>  VCVTUSI2SS xmm1, xmm2, r/m64</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, long value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128 _mm_cvt_roundu64_ss (__m128 a, unsigned __int64 b, int rounding)</para>
            ///   <para>  VCVTUSI2SS xmm1, xmm2, r64 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, ulong value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m128d _mm_cvtsi64_sd (__m128d a, __int64 b)</para>
            ///   <para>  VCVTUSI2SD xmm1, xmm2, r/m64</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, ulong value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128d _mm_cvt_roundsi64_sd (__m128d a, __int64 b, int rounding)</para>
            ///   <para>  VCVTSI2SD xmm1, xmm2, r64 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, long value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128d _mm_cvt_roundu64_sd (__m128d a, unsigned __int64 b, int rounding)</para>
            ///   <para>  VCVTUSI2SD xmm1, xmm2, r64 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, ulong value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__int64 _mm_cvt_roundss_i64 (__m128 a, int rounding)</para>
            ///   <para>  VCVTSS2SI r64, xmm1 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static long ConvertToInt64(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__int64 _mm_cvt_roundsd_i64 (__m128d a, int rounding)</para>
            ///   <para>  VCVTSD2SI r64, xmm1 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static long ConvertToInt64(Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>unsigned __int64 _mm_cvtss_u64 (__m128 a)</para>
            ///   <para>  VCVTSS2USI r64, xmm1/m32{er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<float> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>unsigned __int64 _mm_cvt_roundss_u64 (__m128 a, int rounding)</para>
            ///   <para>  VCVTSS2USI r64, xmm1 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>unsigned __int64 _mm_cvtsd_u64 (__m128d a)</para>
            ///   <para>  VCVTSD2USI r64, xmm1/m64{er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<double> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>unsigned __int64 _mm_cvt_roundsd_u64 (__m128d a, int rounding)</para>
            ///   <para>  VCVTSD2USI r64, xmm1 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>unsigned __int64 _mm_cvttss_u64 (__m128 a)</para>
            ///   <para>  VCVTTSS2USI r64, xmm1/m32{er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong ConvertToUInt64WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>unsigned __int64 _mm_cvttsd_u64 (__m128d a)</para>
            ///   <para>  VCVTTSD2USI r64, xmm1/m64{er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong ConvertToUInt64WithTruncation(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>Provides access to the x86 AVX10.1/512 hardware instructions via intrinsics.</summary>
        public abstract class V512 : Avx512BW
        {
            internal V512() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            ///   <para>__m512 _mm512_and_ps (__m512 a, __m512 b)</para>
            ///   <para>  VANDPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
            /// </summary>
            public static Vector512<float> And(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512d _mm512_and_pd (__m512d a, __m512d b)</para>
            ///   <para>  VANDPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
            /// </summary>
            public static Vector512<double> And(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512 _mm512_andnot_ps (__m512 a, __m512 b)</para>
            ///   <para>  VANDNPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
            /// </summary>
            public static Vector512<float> AndNot(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512d _mm512_andnot_pd (__m512d a, __m512d b)</para>
            ///   <para>  VANDNPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
            /// </summary>
            public static Vector512<double> AndNot(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_broadcast_i32x2 (__m128i a)</para>
            ///   <para>  VBROADCASTI32x2 zmm1 {k1}{z}, xmm2/m64</para>
            /// </summary>
            public static Vector512<int> BroadcastPairScalarToVector512(Vector128<int> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_broadcast_i32x2 (__m128i a)</para>
            ///   <para>  VBROADCASTI32x2 zmm1 {k1}{z}, xmm2/m64</para>
            /// </summary>
            public static Vector512<uint> BroadcastPairScalarToVector512(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512 _mm512_broadcast_f32x2 (__m128 a)</para>
            ///   <para>  VBROADCASTF32x2 zmm1 {k1}{z}, xmm2/m64</para>
            /// </summary>
            public static Vector512<float> BroadcastPairScalarToVector512(Vector128<float> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_broadcast_i64x2 (__m128i const * mem_addr)</para>
            ///   <para>  VBROADCASTI64x2 zmm1 {k1}{z}, m128</para>
            /// </summary>
            public static unsafe Vector512<long> BroadcastVector128ToVector512(long* address) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_broadcast_i64x2 (__m128i const * mem_addr)</para>
            ///   <para>  VBROADCASTI64x2 zmm1 {k1}{z}, m128</para>
            /// </summary>
            public static unsafe Vector512<ulong> BroadcastVector128ToVector512(ulong* address) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512d _mm512_broadcast_f64x2 (__m128d const * mem_addr)</para>
            ///   <para>  VBROADCASTF64x2 zmm1 {k1}{z}, m128</para>
            /// </summary>
            public static unsafe Vector512<double> BroadcastVector128ToVector512(double* address) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_broadcast_i32x8 (__m256i const * mem_addr)</para>
            ///   <para>  VBROADCASTI32x8 zmm1 {k1}{z}, m256</para>
            /// </summary>
            public static unsafe Vector512<int> BroadcastVector256ToVector512(int* address) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_broadcast_i32x8 (__m256i const * mem_addr)</para>
            ///   <para>  VBROADCASTI32x8 zmm1 {k1}{z}, m256</para>
            /// </summary>
            public static unsafe Vector512<uint> BroadcastVector256ToVector512(uint* address) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512 _mm512_broadcast_f32x8 (__m256 const * mem_addr)</para>
            ///   <para>  VBROADCASTF32x8 zmm1 {k1}{z}, m256</para>
            /// </summary>
            public static unsafe Vector512<float> BroadcastVector256ToVector512(float* address) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__mmask8 _mm512_fpclass_pd_mask (__m512d a, int c)</para>
            ///   <para>  VFPCLASSPD k2 {k1}, zmm2/m512/m64bcst, imm8</para>
            /// </summary>
            public static Vector512<double> Classify(Vector512<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__mmask16 _mm512_fpclass_ps_mask (__m512 a, int c)</para>
            ///   <para>  VFPCLASSPS k2 {k1}, zmm2/m512/m32bcst, imm8</para>
            /// </summary>
            public static Vector512<float> Classify(Vector512<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_mask_compress_epi8 (__m512i s, __mmask64 k, __m512i a)</para>
            ///   <para>  VPCOMPRESSB zmm1 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector512<byte> Compress(Vector512<byte> merge, Vector512<byte> mask, Vector512<byte> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_mask_compress_epi16 (__m512i s, __mmask32 k, __m512i a)</para>
            ///   <para>  VPCOMPRESSW zmm1 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector512<short> Compress(Vector512<short> merge, Vector512<short> mask, Vector512<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_mask_compress_epi8 (__m512i s, __mmask64 k, __m512i a)</para>
            ///   <para>  VPCOMPRESSB zmm1 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector512<sbyte> Compress(Vector512<sbyte> merge, Vector512<sbyte> mask, Vector512<sbyte> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_mask_compress_epi16 (__m512i s, __mmask32 k, __m512i a)</para>
            ///   <para>  VPCOMPRESSW zmm1 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector512<ushort> Compress(Vector512<ushort> merge, Vector512<ushort> mask, Vector512<ushort> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_mask_compresstoreu_epi8 (void * s, __mmask64 k, __m512i a)</para>
            ///   <para>  VPCOMPRESSB m512 {k1}{z}, zmm2</para>
            /// </summary>
            public static unsafe void CompressStore(byte* address, Vector512<byte> mask, Vector512<byte> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_mask_compresstoreu_epi16 (void * s, __mmask32 k, __m512i a)</para>
            ///   <para>  VPCOMPRESSW m512 {k1}{z}, zmm2</para>
            /// </summary>
            public static unsafe void CompressStore(short* address, Vector512<short> mask, Vector512<short> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_mask_compresstoreu_epi8 (void * s, __mmask64 k, __m512i a)</para>
            ///   <para>  VPCOMPRESSB m512 {k1}{z}, zmm2</para>
            /// </summary>
            public static unsafe void CompressStore(sbyte* address, Vector512<sbyte> mask, Vector512<sbyte> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_mask_compresstoreu_epi16 (void * s, __mmask32 k, __m512i a)</para>
            ///   <para>  VPCOMPRESSW m512 {k1}{z}, zmm2</para>
            /// </summary>
            public static unsafe void CompressStore(ushort* address, Vector512<ushort> mask, Vector512<ushort> source) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512 _mm512_cvtepi64_ps (__m512i a)</para>
            ///   <para>  VCVTQQ2PS ymm1 {k1}{z}, zmm2/m512/m64bcst</para>
            /// </summary>
            public static Vector256<float> ConvertToVector256Single(Vector512<long> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512 _mm512_cvtepu64_ps (__m512i a)</para>
            ///   <para>  VCVTUQQ2PS ymm1 {k1}{z}, zmm2/m512/m64bcst</para>
            /// </summary>
            public static Vector256<float> ConvertToVector256Single(Vector512<ulong> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256 _mm512_cvt_roundepi64_ps (__m512i a, int r)</para>
            ///   <para>  VCVTQQ2PS ymm1, zmm2 {er}</para>
            /// </summary>
            public static Vector256<float> ConvertToVector256Single(Vector512<long> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256 _mm512_cvt_roundepu64_ps (__m512i a, int r)</para>
            ///   <para>  VCVTUQQ2PS ymm1, zmm2 {er}</para>
            /// </summary>
            public static Vector256<float> ConvertToVector256Single(Vector512<ulong> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512d _mm512_cvtepi64_pd (__m512i a)</para>
            ///   <para>  VCVTQQ2PD zmm1 {k1}{z}, zmm2/m512/m64bcst</para>
            /// </summary>
            public static Vector512<double> ConvertToVector512Double(Vector512<long> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512d _mm512_cvtepu64_pd (__m512i a)</para>
            ///   <para>  VCVTUQQ2PD zmm1 {k1}{z}, zmm2/m512/m64bcst</para>
            /// </summary>
            public static Vector512<double> ConvertToVector512Double(Vector512<ulong> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512d _mm512_cvt_roundepi64_pd (__m512i a, int r)</para>
            ///   <para>  VCVTQQ2PD zmm1, zmm2 {er}</para>
            /// </summary>
            public static Vector512<double> ConvertToVector512Double(Vector512<long> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512d _mm512_cvt_roundepu64_pd (__m512i a, int r)</para>
            ///   <para>  VCVTUQQ2PD zmm1, zmm2 {er}</para>
            /// </summary>
            public static Vector512<double> ConvertToVector512Double(Vector512<ulong> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_cvtps_epi64 (__m512 a)</para>
            ///   <para>  VCVTPS2QQ zmm1 {k1}{z}, ymm2/m256/m32bcst{er}</para>
            /// </summary>
            public static Vector512<long> ConvertToVector512Int64(Vector256<float> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_cvtpd_epi64 (__m512d a)</para>
            ///   <para>  VCVTPD2QQ zmm1 {k1}{z}, zmm2/m512/m64bcst{er}</para>
            /// </summary>
            public static Vector512<long> ConvertToVector512Int64(Vector512<double> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_cvt_roundps_epi64 (__m512 a, int r)</para>
            ///   <para>  VCVTPS2QQ zmm1, ymm2 {er}</para>
            /// </summary>
            public static Vector512<long> ConvertToVector512Int64(Vector256<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_cvt_roundpd_epi64 (__m512d a, int r)</para>
            ///   <para>  VCVTPD2QQ zmm1, zmm2 {er}</para>
            /// </summary>
            public static Vector512<long> ConvertToVector512Int64(Vector512<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_cvttps_epi64 (__m512 a)</para>
            ///   <para>  VCVTTPS2QQ zmm1 {k1}{z}, ymm2/m256/m32bcst{er}</para>
            /// </summary>
            public static Vector512<long> ConvertToVector512Int64WithTruncation(Vector256<float> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_cvttpd_epi64 (__m512 a)</para>
            ///   <para>  VCVTTPD2QQ zmm1 {k1}{z}, zmm2/m512/m64bcst{sae}</para>
            /// </summary>
            public static Vector512<long> ConvertToVector512Int64WithTruncation(Vector512<double> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_cvtps_epu64 (__m512 a)</para>
            ///   <para>  VCVTPS2UQQ zmm1 {k1}{z}, ymm2/m256/m32bcst{er}</para>
            /// </summary>
            public static Vector512<ulong> ConvertToVector512UInt64(Vector256<float> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_cvtpd_epu64 (__m512d a)</para>
            ///   <para>  VCVTPD2UQQ zmm1 {k1}{z}, zmm2/m512/m64bcst{er}</para>
            /// </summary>
            public static Vector512<ulong> ConvertToVector512UInt64(Vector512<double> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_cvt_roundps_epu64 (__m512 a, int r)</para>
            ///   <para>  VCVTPS2UQQ zmm1 {k1}{z}, ymm2/m256/m32bcst{er}</para>
            /// </summary>
            public static Vector512<ulong> ConvertToVector512UInt64(Vector256<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_cvt_roundpd_epu64 (__m512d a, int r)</para>
            ///   <para>  VCVTPD2UQQ zmm1 {k1}{z}, zmm2/m512/m64bcst{er}</para>
            /// </summary>
            public static Vector512<ulong> ConvertToVector512UInt64(Vector512<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_cvttps_epu64 (__m512 a)</para>
            ///   <para>  VCVTTPS2UQQ zmm1 {k1}{z}, ymm2/m256/m32bcst{er}</para>
            /// </summary>
            public static Vector512<ulong> ConvertToVector512UInt64WithTruncation(Vector256<float> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_cvttpd_epu64 (__m512d a)</para>
            ///   <para>  VCVTTPD2UQQ zmm1 {k1}{z}, zmm2/m512/m64bcst{er}</para>
            /// </summary>
            public static Vector512<ulong> ConvertToVector512UInt64WithTruncation(Vector512<double> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_conflict_epi32 (__m512i a)</para>
            ///   <para>  VPCONFLICTD zmm1 {k1}{z}, zmm2/m512/m32bcst</para>
            /// </summary>
            public static Vector512<int> DetectConflicts(Vector512<int> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_conflict_epi32 (__m512i a)</para>
            ///   <para>  VPCONFLICTD zmm1 {k1}{z}, zmm2/m512/m32bcst</para>
            /// </summary>
            public static Vector512<uint> DetectConflicts(Vector512<uint> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_conflict_epi64 (__m512i a)</para>
            ///   <para>  VPCONFLICTQ zmm1 {k1}{z}, zmm2/m512/m64bcst</para>
            /// </summary>
            public static Vector512<long> DetectConflicts(Vector512<long> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_conflict_epi64 (__m512i a)</para>
            ///   <para>  VPCONFLICTQ zmm1 {k1}{z}, zmm2/m512/m64bcst</para>
            /// </summary>
            public static Vector512<ulong> DetectConflicts(Vector512<ulong> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_mask_expand_epi8 (__m512i s, __mmask64 k, __m512i a)</para>
            ///   <para>  VPEXPANDB zmm1 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector512<byte> Expand(Vector512<byte> merge, Vector512<byte> mask, Vector512<byte> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_mask_expand_epi16 (__m512i s, __mmask32 k, __m512i a)</para>
            ///   <para>  VPEXPANDW zmm1 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector512<short> Expand(Vector512<short> merge, Vector512<short> mask, Vector512<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_mask_expand_epi8 (__m512i s, __mmask64 k, __m512i a)</para>
            ///   <para>  VPEXPANDB zmm1 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector512<sbyte> Expand(Vector512<sbyte> merge, Vector512<sbyte> mask, Vector512<sbyte> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_mask_expand_epi16 (__m512i s, __mmask32 k, __m512i a)</para>
            ///   <para>  VPEXPANDW zmm1 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector512<ushort> Expand(Vector512<ushort> merge, Vector512<ushort> mask, Vector512<ushort> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_mask_expandloadu_epi8 (__m512i s, __mmask64 k, void * const a)</para>
            ///   <para>  VPEXPANDB zmm1 {k1}{z}, m512</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector512<byte> ExpandLoad(byte* address, Vector512<byte> mask, Vector512<byte> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_mask_expandloadu_epi16 (__m512i s, __mmask32 k, void * const a)</para>
            ///   <para>  VPEXPANDW zmm1 {k1}{z}, m512</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector512<short> ExpandLoad(short* address, Vector512<short> mask, Vector512<short> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_mask_expandloadu_epi8 (__m512i s, __mmask64 k, void * const a)</para>
            ///   <para>  VPEXPANDB zmm1 {k1}{z}, m512</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector512<sbyte> ExpandLoad(sbyte* address, Vector512<sbyte> mask, Vector512<sbyte> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_mask_expandloadu_epi16 (__m512i s, __mmask32 k, void * const a)</para>
            ///   <para>  VPEXPANDW zmm1 {k1}{z}, m512</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector512<ushort> ExpandLoad(ushort* address, Vector512<ushort> mask, Vector512<ushort> merge) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m128i _mm512_extracti64x2_epi64 (__m512i a, const int imm8)</para>
            ///   <para>  VEXTRACTI64x2 xmm1/m128 {k1}{z}, zmm2, imm8</para>
            /// </summary>
            public static new Vector128<long> ExtractVector128(Vector512<long> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm512_extracti64x2_epi64 (__m512i a, const int imm8)</para>
            ///   <para>  VEXTRACTI64x2 xmm1/m128 {k1}{z}, zmm2, imm8</para>
            /// </summary>
            public static new Vector128<ulong> ExtractVector128(Vector512<ulong> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128d _mm512_extractf64x2_pd (__m512d a, const int imm8)</para>
            ///   <para>  VEXTRACTF64x2 xmm1/m128 {k1}{z}, zmm2, imm8</para>
            /// </summary>
            public static new Vector128<double> ExtractVector128(Vector512<double> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m256i _mm512_extracti32x8_epi32 (__m512i a, const int imm8)</para>
            ///   <para>  VEXTRACTI32x8 ymm1/m256 {k1}{z}, zmm2, imm8</para>
            /// </summary>
            public static new Vector256<int> ExtractVector256(Vector512<int> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm512_extracti32x8_epi32 (__m512i a, const int imm8)</para>
            ///   <para>  VEXTRACTI32x8 ymm1/m256 {k1}{z}, zmm2, imm8</para>
            /// </summary>
            public static new Vector256<uint> ExtractVector256(Vector512<uint> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256 _mm512_extractf32x8_ps (__m512 a, const int imm8)</para>
            ///   <para>  VEXTRACTF32x8 ymm1/m256 {k1}{z}, zmm2, imm8</para>
            /// </summary>
            public static new Vector256<float> ExtractVector256(Vector512<float> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_inserti64x2_si512 (__m512i a, __m128i b, const int imm8)</para>
            ///   <para>  VINSERTI64x2 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8</para>
            /// </summary>
            public static new Vector512<long> InsertVector128(Vector512<long> value, Vector128<long> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_inserti64x2_si512 (__m512i a, __m128i b, const int imm8)</para>
            ///   <para>  VINSERTI64x2 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8</para>
            /// </summary>
            public static new Vector512<ulong> InsertVector128(Vector512<ulong> value, Vector128<ulong> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512d _mm512_insertf64x2_pd (__m512d a, __m128d b, int imm8)</para>
            ///   <para>  VINSERTF64x2 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8</para>
            /// </summary>
            public static new Vector512<double> InsertVector128(Vector512<double> value, Vector128<double> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_inserti32x8_si512 (__m512i a, __m256i b, const int imm8)</para>
            ///   <para>  VINSERTI32x8 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8</para>
            /// </summary>
            public static new Vector512<int> InsertVector256(Vector512<int> value, Vector256<int> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_inserti32x8_si512 (__m512i a, __m256i b, const int imm8)</para>
            ///   <para>  VINSERTI32x8 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8</para>
            /// </summary>
            public static new Vector512<uint> InsertVector256(Vector512<uint> value, Vector256<uint> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512 _mm512_insertf32x8_ps (__m512 a, __m256 b, int imm8)</para>
            ///   <para>  VINSERTF32x8 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8</para>
            /// </summary>
            public static new Vector512<float> InsertVector256(Vector512<float> value, Vector256<float> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_lzcnt_epi32 (__m512i a)</para>
            ///   <para>  VPLZCNTD zmm1 {k1}{z}, zmm2/m512/m32bcst</para>
            /// </summary>
            public static Vector512<int> LeadingZeroCount(Vector512<int> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_lzcnt_epi32 (__m512i a)</para>
            ///   <para>  VPLZCNTD zmm1 {k1}{z}, zmm2/m512/m32bcst</para>
            /// </summary>
            public static Vector512<uint> LeadingZeroCount(Vector512<uint> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_lzcnt_epi64 (__m512i a)</para>
            ///   <para>  VPLZCNTQ zmm1 {k1}{z}, zmm2/m512/m64bcst</para>
            /// </summary>
            public static Vector512<long> LeadingZeroCount(Vector512<long> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_lzcnt_epi64 (__m512i a)</para>
            ///   <para>  VPLZCNTQ zmm1 {k1}{z}, zmm2/m512/m64bcst</para>
            /// </summary>
            public static Vector512<ulong> LeadingZeroCount(Vector512<ulong> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
            ///   <para>  KMOVB r32, k1</para>
            /// </summary>
            public static int MoveMask(Vector512<double> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
            ///   <para>  KMOVB r32, k1</para>
            /// </summary>
            public static int MoveMask(Vector512<long> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>unsigned int _cvtmask8_u32 (__mmask8 a)</para>
            ///   <para>  KMOVB r32, k1</para>
            /// </summary>
            public static int MoveMask(Vector512<ulong> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_mullo_epi64 (__m512i a, __m512i b)</para>
            ///   <para>  VPMULLQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
            /// </summary>
            public static Vector512<long> MultiplyLow(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_mullo_epi64 (__m512i a, __m512i b)</para>
            ///   <para>  VPMULLQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
            /// </summary>
            public static Vector512<ulong> MultiplyLow(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_multishift_epi64_epi8( __m512i a, __m512i b)</para>
            ///   <para>  VPMULTISHIFTQB zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
            /// </summary>
            public static Vector512<byte> MultiShift(Vector512<byte> control, Vector512<ulong> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_multishift_epi64_epi8( __m512i a, __m512i b)</para>
            ///   <para>  VPMULTISHIFTQB zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
            /// </summary>
            public static Vector512<sbyte> MultiShift(Vector512<sbyte> control, Vector512<long> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512 _mm512_or_ps (__m512 a, __m512 b)</para>
            ///   <para>  VORPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
            /// </summary>
            public static Vector512<float> Or(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512d _mm512_or_pd (__m512d a, __m512d b)</para>
            ///   <para>  VORPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
            /// </summary>
            public static Vector512<double> Or(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_permutexvar_epi8 (__m512i idx, __m512i a)</para>
            ///   <para>  VPERMB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector512<sbyte> PermuteVar64x8(Vector512<sbyte> left, Vector512<sbyte> control) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_permutexvar_epi8 (__m512i idx, __m512i a)</para>
            ///   <para>  VPERMB zmm1 {k1}{z}, zmm2, zmm3/m512</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector512<byte> PermuteVar64x8(Vector512<byte> left, Vector512<byte> control) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512i _mm512_permutex2var_epi8 (__m512i a, __m512i idx, __m512i b)</para>
            ///   <para>  VPERMI2B zmm1 {k1}{z}, zmm2, zmm3/m512</para>
            ///   <para>  VPERMT2B zmm1 {k1}{z}, zmm2, zmm3/m512</para>
            /// </summary>
            public static Vector512<byte> PermuteVar64x8x2(Vector512<byte> lower, Vector512<byte> indices, Vector512<byte> upper) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_permutex2var_epi8 (__m512i a, __m512i idx, __m512i b)</para>
            ///   <para>  VPERMI2B zmm1 {k1}{z}, zmm2, zmm3/m512</para>
            ///   <para>  VPERMT2B zmm1 {k1}{z}, zmm2, zmm3/m512</para>
            /// </summary>
            public static Vector512<sbyte> PermuteVar64x8x2(Vector512<sbyte> lower, Vector512<sbyte> indices, Vector512<sbyte> upper) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512 _mm512_range_ps(__m512 a, __m512 b, int imm);</para>
            ///   <para>  VRANGEPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{sae}, imm8</para>
            /// </summary>
            public static Vector512<float> Range(Vector512<float> left, Vector512<float> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512d _mm512_range_pd(__m512d a, __m512d b, int imm);</para>
            ///   <para>  VRANGEPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{sae}, imm8</para>
            /// </summary>
            public static Vector512<double> Range(Vector512<double> left, Vector512<double> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512 _mm512_reduce_ps(__m512 a, int imm);</para>
            ///   <para>  VREDUCEPS zmm1 {k1}{z}, zmm2/m512/m32bcst{sae}, imm8</para>
            /// </summary>
            public static Vector512<float> Reduce(Vector512<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512d _mm512_reduce_pd(__m512d a, int imm);</para>
            ///   <para>  VREDUCEPD zmm1 {k1}{z}, zmm2/m512/m64bcst{sae}, imm8</para>
            /// </summary>
            public static Vector512<double> Reduce(Vector512<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m512 _mm512_xor_ps (__m512 a, __m512 b)</para>
            ///   <para>  VXORPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
            /// </summary>
            public static Vector512<float> Xor(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512d _mm512_xor_pd (__m512d a, __m512d b)</para>
            ///   <para>  VXORPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
            /// </summary>
            public static Vector512<double> Xor(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

            /// <summary>Provides access to the x86 AVX10.1/512 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
            [Intrinsic]
            public new abstract class X64 : Avx512BW.X64
            {
                internal X64() { }

                /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
                /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
                /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
                public static new bool IsSupported { [Intrinsic] get { return false; } }
            }
        }
    }
}
