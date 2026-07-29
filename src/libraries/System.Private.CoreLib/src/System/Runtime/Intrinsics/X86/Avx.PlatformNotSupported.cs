// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 AVX hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Avx : Sse42
    {
        internal Avx() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>Provides access to the x86 AVX hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class X64 : Sse42.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        ///   <para>__m256 _mm256_add_ps (__m256 a, __m256 b)</para>
        ///   <para>  VADDPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VADDPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> Add(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_add_pd (__m256d a, __m256d b)</para>
        ///   <para>  VADDPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VADDPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> Add(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_addsub_ps (__m256 a, __m256 b)</para>
        ///   <para>  VADDSUBPS ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<float> AddSubtract(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_addsub_pd (__m256d a, __m256d b)</para>
        ///   <para>  VADDSUBPD ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<double> AddSubtract(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_and_ps (__m256 a, __m256 b)</para>
        ///   <para>  VANDPS ymm1,         ymm2, ymm2/m256</para>
        ///   <para>  VANDPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> And(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_and_pd (__m256d a, __m256d b)</para>
        ///   <para>  VANDPD ymm1,         ymm2, ymm2/m256</para>
        ///   <para>  VANDPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> And(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_andnot_ps (__m256 a, __m256 b)</para>
        ///   <para>  VANDNPS ymm1,         ymm2, ymm2/m256</para>
        ///   <para>  VANDNPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> AndNot(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_andnot_pd (__m256d a, __m256d b)</para>
        ///   <para>  VANDNPD ymm1,         ymm2, ymm2/m256</para>
        ///   <para>  VANDNPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> AndNot(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_blend_ps (__m256 a, __m256 b, const int imm8)</para>
        ///   <para>  VBLENDPS ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<float> Blend(Vector256<float> left, Vector256<float> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_blend_pd (__m256d a, __m256d b, const int imm8)</para>
        ///   <para>  VBLENDPD ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<double> Blend(Vector256<double> left, Vector256<double> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_blendv_ps (__m256 a, __m256 b, __m256 mask)</para>
        ///   <para>  VBLENDVPS ymm1, ymm2, ymm3/m256, ymm4</para>
        /// </summary>
        public static Vector256<float> BlendVariable(Vector256<float> left, Vector256<float> right, Vector256<float> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_blendv_pd (__m256d a, __m256d b, __m256d mask)</para>
        ///   <para>  VBLENDVPD ymm1, ymm2, ymm3/m256, ymm4</para>
        /// </summary>
        public static Vector256<double> BlendVariable(Vector256<double> left, Vector256<double> right, Vector256<double> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_broadcast_ss (float const * mem_addr)</para>
        ///   <para>  VBROADCASTSS xmm1,         m32</para>
        ///   <para>  VBROADCASTSS xmm1 {k1}{z}, m32</para>
        /// </summary>
        public static unsafe Vector128<float> BroadcastScalarToVector128(float* source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_broadcast_ss (float const * mem_addr)</para>
        ///   <para>  VBROADCASTSS ymm1,         m32</para>
        ///   <para>  VBROADCASTSS ymm1 {k1}{z}, m32</para>
        /// </summary>
        public static unsafe Vector256<float> BroadcastScalarToVector256(float* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_broadcast_sd (double const * mem_addr)</para>
        ///   <para>  VBROADCASTSD ymm1,         m64</para>
        ///   <para>  VBROADCASTSD ymm1 {k1}{z}, m64</para>
        /// </summary>
        public static unsafe Vector256<double> BroadcastScalarToVector256(double* source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_broadcast_ps (__m128 const * mem_addr)</para>
        ///   <para>  VBROADCASTF128  ymm1,         m128</para>
        ///   <para>  VBROADCASTF32x4 ymm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector256<float> BroadcastVector128ToVector256(float* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_broadcast_pd (__m128d const * mem_addr)</para>
        ///   <para>  VBROADCASTF128  ymm1,         m128</para>
        ///   <para>  VBROADCASTF64x2 ymm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector256<double> BroadcastVector128ToVector256(double* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_ceil_ps (__m128 a)</para>
        ///   <para>  VROUNDPS ymm1, ymm2/m256, imm8(10)</para>
        /// </summary>
        public static Vector256<float> Ceiling(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_ceil_pd (__m128d a)</para>
        ///   <para>  VROUNDPD ymm1, ymm2/m256, imm8(10)</para>
        /// </summary>
        public static Vector256<double> Ceiling(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_cmp_ps (__m128 a, __m128 b, const int imm8)</para>
        ///   <para>  VCMPPS xmm1, xmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector128<float> Compare(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_cmp_ps (__m256 a, __m256 b, const int imm8)</para>
        ///   <para>  VCMPPS ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<float> Compare(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_cmpeq_ps (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS ymm1, ymm2/m256, imm8(0)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<float> CompareEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_cmpgt_ps (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS ymm1, ymm2/m256, imm8(14)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<float> CompareGreaterThan(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_cmpge_ps (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS ymm1, ymm2/m256, imm8(13)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<float> CompareGreaterThanOrEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_cmplt_ps (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS ymm1, ymm2/m256, imm8(1)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<float> CompareLessThan(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_cmple_ps (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS ymm1, ymm2/m256, imm8(2)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<float> CompareLessThanOrEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_cmpneq_ps (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS ymm1, ymm2/m256, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<float> CompareNotEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_cmpngt_ps (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS ymm1, ymm2/m256, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<float> CompareNotGreaterThan(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_cmpnge_ps (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS ymm1, ymm2/m256, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<float> CompareNotGreaterThanOrEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_cmpnlt_ps (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS ymm1, ymm2/m256, imm8(5)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<float> CompareNotLessThan(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_cmpnle_ps (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS ymm1, ymm2/m256, imm8(6)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<float> CompareNotLessThanOrEqual(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_cmpord_ps (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS ymm1, ymm2/m256, imm8(7)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<float> CompareOrdered(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_cmpunord_ps (__m256 a,  __m256 b)</para>
        ///   <para>  VCMPPS ymm1, ymm2/m256, imm8(3)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<float> CompareUnordered(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128d _mm_cmp_pd (__m128d a, __m128d b, const int imm8)</para>
        ///   <para>  VCMPPD xmm1, xmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector128<double> Compare(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cmp_pd (__m256d a, __m256d b, const int imm8)</para>
        ///   <para>  VCMPPD ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<double> Compare(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cmpeq_pd (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD ymm1, ymm2/m256, imm8(0)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<double> CompareEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cmpgt_pd (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD ymm1, ymm2/m256, imm8(14)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<double> CompareGreaterThan(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cmpge_pd (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD ymm1, ymm2/m256, imm8(13)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<double> CompareGreaterThanOrEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cmplt_pd (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD ymm1, ymm2/m256, imm8(1)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<double> CompareLessThan(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cmple_pd (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD ymm1, ymm2/m256, imm8(2)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<double> CompareLessThanOrEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cmpneq_pd (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD ymm1, ymm2/m256, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<double> CompareNotEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cmpngt_pd (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD ymm1, ymm2/m256, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<double> CompareNotGreaterThan(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cmpnge_pd (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD ymm1, ymm2/m256, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<double> CompareNotGreaterThanOrEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cmpnlt_pd (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD ymm1, ymm2/m256, imm8(5)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<double> CompareNotLessThan(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cmpnle_pd (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD ymm1, ymm2/m256, imm8(6)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<double> CompareNotLessThanOrEqual(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cmpord_pd (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD ymm1, ymm2/m256, imm8(7)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<double> CompareOrdered(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cmpunord_pd (__m256d a,  __m256d b)</para>
        ///   <para>  VCMPPD ymm1, ymm2/m256, imm8(3)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector256<double> CompareUnordered(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_cmp_ss (__m128 a, __m128 b, const int imm8)</para>
        ///   <para>  VCMPSD xmm1, xmm2, xmm3/m64, imm8</para>
        /// </summary>
        public static Vector128<float> CompareScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_cmp_sd (__m128d a, __m128d b, const int imm8)</para>
        ///   <para>  VCMPSS xmm1, xmm2, xmm3/m32, imm8</para>
        /// </summary>
        public static Vector128<double> CompareScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm256_cvtpd_epi32 (__m256d a)</para>
        ///   <para>  VCVTPD2DQ xmm1,         ymm2/m256</para>
        ///   <para>  VCVTPD2DQ xmm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm256_cvtpd_ps (__m256d a)</para>
        ///   <para>  VCVTPD2PS xmm1,         ymm2/m256</para>
        ///   <para>  VCVTPD2PS xmm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256d _mm256_cvtepi32_pd (__m128i a)</para>
        ///   <para>  VCVTDQ2PD ymm1,         xmm2/m128</para>
        ///   <para>  VCVTDQ2PD ymm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector256<double> ConvertToVector256Double(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_cvtps_pd (__m128 a)</para>
        ///   <para>  VCVTPS2PD ymm1,         xmm2/m128</para>
        ///   <para>  VCVTPS2PD ymm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector256<double> ConvertToVector256Double(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvtps_epi32 (__m256 a)</para>
        ///   <para>  VCVTPS2DQ ymm1,         ymm2/m256</para>
        ///   <para>  VCVTPS2DQ ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_cvtepi32_ps (__m256i a)</para>
        ///   <para>  VCVTDQ2PS ymm1,         ymm2/m256</para>
        ///   <para>  VCVTDQ2PS ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> ConvertToVector256Single(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_cvttps_epi32 (__m256 a)</para>
        ///   <para>  VCVTTPS2DQ ymm1,         ymm2/m256</para>
        ///   <para>  VCVTTPS2DQ ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32WithTruncation(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_cvttpd_epi32 (__m256d a)</para>
        ///   <para>  VCVTTPD2DQ xmm1,         ymm2/m256</para>
        ///   <para>  VCVTTPD2DQ xmm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32WithTruncation(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_div_ps (__m256 a, __m256 b)</para>
        ///   <para>  VDIVPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VDIVPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> Divide(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_div_pd (__m256d a, __m256d b)</para>
        ///   <para>  VDIVPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VDIVPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> Divide(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_dp_ps (__m256 a, __m256 b, const int imm8)</para>
        ///   <para>  VDPPS ymm, ymm, ymm/m256, imm8</para>
        /// </summary>
        public static Vector256<float> DotProduct(Vector256<float> left, Vector256<float> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_moveldup_ps (__m256 a)</para>
        ///   <para>  VMOVSLDUP ymm1,         ymm2/m256</para>
        ///   <para>  VMOVSLDUP ymm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector256<float> DuplicateEvenIndexed(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_movedup_pd (__m256d a)</para>
        ///   <para>  VMOVDDUP ymm1,         ymm2/m256</para>
        ///   <para>  VMOVDDUP ymm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector256<double> DuplicateEvenIndexed(Vector256<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_movehdup_ps (__m256 a)</para>
        ///   <para>  VMOVSHDUP ymm1,         ymm2/m256</para>
        ///   <para>  VMOVSHDUP ymm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector256<float> DuplicateOddIndexed(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm256_extractf128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTF128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTF32x4 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector128<byte> ExtractVector128(Vector256<byte> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_extractf128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTF128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTF32x4 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector128<sbyte> ExtractVector128(Vector256<sbyte> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_extractf128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTF128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTF32x4 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector128<short> ExtractVector128(Vector256<short> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_extractf128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTF128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTF32x4 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector128<ushort> ExtractVector128(Vector256<ushort> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_extractf128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTF128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTF32x4 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector128<int> ExtractVector128(Vector256<int> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_extractf128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTF128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTF32x4 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector128<uint> ExtractVector128(Vector256<uint> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_extractf128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTF128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTF64x2 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector128<long> ExtractVector128(Vector256<long> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_extractf128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTF128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTF64x2 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector128<ulong> ExtractVector128(Vector256<ulong> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm256_extractf128_ps (__m256 a, const int imm8)</para>
        ///   <para>  VEXTRACTF128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTF32x4 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector128<float> ExtractVector128(Vector256<float> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm256_extractf128_pd (__m256d a, const int imm8)</para>
        ///   <para>  VEXTRACTF128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTF64x2 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector128<double> ExtractVector128(Vector256<double> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_floor_ps (__m256 a)</para>
        ///   <para>  VROUNDPS ymm1, ymm2/m256, imm8(9)</para>
        /// </summary>
        public static Vector256<float> Floor(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_floor_pd (__m256d a)</para>
        ///   <para>  VROUNDPD ymm1, ymm2/m256, imm8(9)</para>
        /// </summary>
        public static Vector256<double> Floor(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_hadd_ps (__m256 a, __m256 b)</para>
        ///   <para>  VHADDPS ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<float> HorizontalAdd(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_hadd_pd (__m256d a, __m256d b)</para>
        ///   <para>  VHADDPD ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<double> HorizontalAdd(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_hsub_ps (__m256 a, __m256 b)</para>
        ///   <para>  VHSUBPS ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<float> HorizontalSubtract(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_hsub_pd (__m256d a, __m256d b)</para>
        ///   <para>  VHSUBPD ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<double> HorizontalSubtract(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_insertf128_si256 (__m256i a, __m128i b, int imm8)</para>
        ///   <para>  VINSERTF128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTF32x4 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector256<byte> InsertVector128(Vector256<byte> value, Vector128<byte> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_insertf128_si256 (__m256i a, __m128i b, int imm8)</para>
        ///   <para>  VINSERTF128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTF32x4 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector256<sbyte> InsertVector128(Vector256<sbyte> value, Vector128<sbyte> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_insertf128_si256 (__m256i a, __m128i b, int imm8)</para>
        ///   <para>  VINSERTF128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTF32x4 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector256<short> InsertVector128(Vector256<short> value, Vector128<short> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_insertf128_si256 (__m256i a, __m128i b, int imm8)</para>
        ///   <para>  VINSERTF128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTF32x4 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector256<ushort> InsertVector128(Vector256<ushort> value, Vector128<ushort> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_insertf128_si256 (__m256i a, __m128i b, int imm8)</para>
        ///   <para>  VINSERTF128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTF32x4 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector256<int> InsertVector128(Vector256<int> value, Vector128<int> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_insertf128_si256 (__m256i a, __m128i b, int imm8)</para>
        ///   <para>  VINSERTF128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTF32x4 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector256<uint> InsertVector128(Vector256<uint> value, Vector128<uint> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_insertf128_si256 (__m256i a, __m128i b, int imm8)</para>
        ///   <para>  VINSERTF128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTF64x2 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector256<long> InsertVector128(Vector256<long> value, Vector128<long> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_insertf128_si256 (__m256i a, __m128i b, int imm8)</para>
        ///   <para>  VINSERTF128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTF64x2 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector256<ulong> InsertVector128(Vector256<ulong> value, Vector128<ulong> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_insertf128_ps (__m256 a, __m128 b, int imm8)</para>
        ///   <para>  VINSERTF128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTF32x4 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector256<float> InsertVector128(Vector256<float> value, Vector128<float> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_insertf128_pd (__m256d a, __m128d b, int imm8)</para>
        ///   <para>  VINSERTF128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTF64x2 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector256<double> InsertVector128(Vector256<double> value, Vector128<double> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_load_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQA   ymm1,         m256</para>
        ///   <para>  VMOVDQA32 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<sbyte> LoadAlignedVector256(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_load_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQA   ymm1,         m256</para>
        ///   <para>  VMOVDQA32 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<byte> LoadAlignedVector256(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_load_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQA   ymm1,         m256</para>
        ///   <para>  VMOVDQA32 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<short> LoadAlignedVector256(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_load_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQA   ymm1,         m256</para>
        ///   <para>  VMOVDQA32 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<ushort> LoadAlignedVector256(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_load_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQA   ymm1,         m256</para>
        ///   <para>  VMOVDQA32 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<int> LoadAlignedVector256(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_load_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQA   ymm1,         m256</para>
        ///   <para>  VMOVDQA32 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<uint> LoadAlignedVector256(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_load_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQA   ymm1,         m256</para>
        ///   <para>  VMOVDQA64 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<long> LoadAlignedVector256(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_load_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQA   ymm1,         m256</para>
        ///   <para>  VMOVDQA64 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<ulong> LoadAlignedVector256(ulong* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_load_ps (float const * mem_addr)</para>
        ///   <para>  VMOVAPS ymm1,         m256</para>
        ///   <para>  VMOVAPS ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<float> LoadAlignedVector256(float* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_load_pd (double const * mem_addr)</para>
        ///   <para>  VMOVAPD ymm1,         m256</para>
        ///   <para>  VMOVAPD ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<double> LoadAlignedVector256(double* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_lddqu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VLDDQU ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<sbyte> LoadDquVector256(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_lddqu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VLDDQU ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<byte> LoadDquVector256(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_lddqu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VLDDQU ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<short> LoadDquVector256(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_lddqu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VLDDQU ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<ushort> LoadDquVector256(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_lddqu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VLDDQU ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<int> LoadDquVector256(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_lddqu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VLDDQU ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<uint> LoadDquVector256(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_lddqu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VLDDQU ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<long> LoadDquVector256(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_lddqu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VLDDQU ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<ulong> LoadDquVector256(ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_loadu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQU  ymm1,         m256</para>
        ///   <para>  VMOVDQU8 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<sbyte> LoadVector256(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_loadu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQU  ymm1,         m256</para>
        ///   <para>  VMOVDQU8 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<byte> LoadVector256(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_loadu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQU   ymm1,         m256</para>
        ///   <para>  VMOVDQU16 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<short> LoadVector256(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_loadu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQU   ymm1,         m256</para>
        ///   <para>  VMOVDQU16 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<ushort> LoadVector256(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_loadu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQU   ymm1,         m256</para>
        ///   <para>  VMOVDQU32 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<int> LoadVector256(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_loadu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQU   ymm1,         m256</para>
        ///   <para>  VMOVDQU32 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<uint> LoadVector256(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_loadu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQU   ymm1,         m256</para>
        ///   <para>  VMOVDQU64 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<long> LoadVector256(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_loadu_si256 (__m256i const * mem_addr)</para>
        ///   <para>  VMOVDQU   ymm1,         m256</para>
        ///   <para>  VMOVDQU64 ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<ulong> LoadVector256(ulong* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_loadu_ps (float const * mem_addr)</para>
        ///   <para>  VMOVUPS ymm1,         m256</para>
        ///   <para>  VMOVUPS ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<float> LoadVector256(float* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_loadu_pd (double const * mem_addr)</para>
        ///   <para>  VMOVUPD ymm1,         m256</para>
        ///   <para>  VMOVUPD ymm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector256<double> LoadVector256(double* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_maskload_ps (float const * mem_addr, __m128i mask)</para>
        ///   <para>  VMASKMOVPS xmm1, xmm2, m128</para>
        /// </summary>
        public static unsafe Vector128<float> MaskLoad(float* address, Vector128<float> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_maskload_pd (double const * mem_addr, __m128i mask)</para>
        ///   <para>  VMASKMOVPD xmm1, xmm2, m128</para>
        /// </summary>
        public static unsafe Vector128<double> MaskLoad(double* address, Vector128<double> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_maskload_ps (float const * mem_addr, __m256i mask)</para>
        ///   <para>  VMASKMOVPS ymm1, ymm2, m256</para>
        /// </summary>
        public static unsafe Vector256<float> MaskLoad(float* address, Vector256<float> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_maskload_pd (double const * mem_addr, __m256i mask)</para>
        ///   <para>  VMASKMOVPD ymm1, ymm2, m256</para>
        /// </summary>
        public static unsafe Vector256<double> MaskLoad(double* address, Vector256<double> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>void _mm_maskstore_ps (float * mem_addr, __m128i mask, __m128 a)</para>
        ///   <para>  VMASKMOVPS m128, xmm1, xmm2</para>
        /// </summary>
        public static unsafe void MaskStore(float* address, Vector128<float> mask, Vector128<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_maskstore_pd (double * mem_addr, __m128i mask, __m128d a)</para>
        ///   <para>  VMASKMOVPD m128, xmm1, xmm2</para>
        /// </summary>
        public static unsafe void MaskStore(double* address, Vector128<double> mask, Vector128<double> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_maskstore_ps (float * mem_addr, __m256i mask, __m256 a)</para>
        ///   <para>  VMASKMOVPS m256, ymm1, ymm2</para>
        /// </summary>
        public static unsafe void MaskStore(float* address, Vector256<float> mask, Vector256<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_maskstore_pd (double * mem_addr, __m256i mask, __m256d a)</para>
        ///   <para>  VMASKMOVPD m256, ymm1, ymm2</para>
        /// </summary>
        public static unsafe void MaskStore(double* address, Vector256<double> mask, Vector256<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_max_ps (__m256 a, __m256 b)</para>
        ///   <para>  VMAXPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VMAXPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> Max(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_max_pd (__m256d a, __m256d b)</para>
        ///   <para>  VMAXPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VMAXPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> Max(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_min_ps (__m256 a, __m256 b)</para>
        ///   <para>  VMINPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VMINPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> Min(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_min_pd (__m256d a, __m256d b)</para>
        ///   <para>  VMINPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VMINPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> Min(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>int _mm256_movemask_ps (__m256 a)</para>
        ///   <para>  VMOVMSKPS r32, ymm1</para>
        /// </summary>
        public static int MoveMask(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_movemask_pd (__m256d a)</para>
        ///   <para>  VMOVMSKPD r32, ymm1</para>
        /// </summary>
        public static int MoveMask(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_mul_ps (__m256 a, __m256 b)</para>
        ///   <para>  VMULPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VMULPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> Multiply(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_mul_pd (__m256d a, __m256d b)</para>
        ///   <para>  VMULPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VMULPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> Multiply(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_or_ps (__m256 a, __m256 b)</para>
        ///   <para>  VORPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VORPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> Or(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_or_pd (__m256d a, __m256d b)</para>
        ///   <para>  VORPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VORPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> Or(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_permute_ps (__m128 a, int imm8)</para>
        ///   <para>  VPERMILPS xmm1,         xmm2/m128,         imm8</para>
        ///   <para>  VPERMILPS xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<float> Permute(Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_permute_pd (__m128d a, int imm8)</para>
        ///   <para>  VPERMILPD xmm1,         xmm2/m128,         imm8</para>
        ///   <para>  VPERMILPD xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<double> Permute(Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_permute_ps (__m256 a, int imm8)</para>
        ///   <para>  VPERMILPS ymm1,         ymm2/m256,         imm8</para>
        ///   <para>  VPERMILPS ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<float> Permute(Vector256<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_permute_pd (__m256d a, int imm8)</para>
        ///   <para>  VPERMILPD ymm1,         ymm2/m256,         imm8</para>
        ///   <para>  VPERMILPD ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<double> Permute(Vector256<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_permute2f128_si256 (__m256i a, __m256i b, int imm8)</para>
        ///   <para>  VPERM2F128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<byte> Permute2x128(Vector256<byte> left, Vector256<byte> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute2f128_si256 (__m256i a, __m256i b, int imm8)</para>
        ///   <para>  VPERM2F128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<sbyte> Permute2x128(Vector256<sbyte> left, Vector256<sbyte> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute2f128_si256 (__m256i a, __m256i b, int imm8)</para>
        ///   <para>  VPERM2F128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<short> Permute2x128(Vector256<short> left, Vector256<short> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute2f128_si256 (__m256i a, __m256i b, int imm8)</para>
        ///   <para>  VPERM2F128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<ushort> Permute2x128(Vector256<ushort> left, Vector256<ushort> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute2f128_si256 (__m256i a, __m256i b, int imm8)</para>
        ///   <para>  VPERM2F128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<int> Permute2x128(Vector256<int> left, Vector256<int> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute2f128_si256 (__m256i a, __m256i b, int imm8)</para>
        ///   <para>  VPERM2F128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<uint> Permute2x128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute2f128_si256 (__m256i a, __m256i b, int imm8)</para>
        ///   <para>  VPERM2F128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<long> Permute2x128(Vector256<long> left, Vector256<long> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute2f128_si256 (__m256i a, __m256i b, int imm8)</para>
        ///   <para>  VPERM2F128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<ulong> Permute2x128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_permute2f128_ps (__m256 a, __m256 b, int imm8)</para>
        ///   <para>  VPERM2F128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<float> Permute2x128(Vector256<float> left, Vector256<float> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_permute2f128_pd (__m256d a, __m256d b, int imm8)</para>
        ///   <para>  VPERM2F128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<double> Permute2x128(Vector256<double> left, Vector256<double> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_permutevar_ps (__m128 a, __m128i b)</para>
        ///   <para>  VPERMILPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPERMILPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> PermuteVar(Vector128<float> left, Vector128<int> control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_permutevar_pd (__m128d a, __m128i b)</para>
        ///   <para>  VPERMILPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPERMILPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> PermuteVar(Vector128<double> left, Vector128<long> control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_permutevar_ps (__m256 a, __m256i b)</para>
        ///   <para>  VPERMILPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPERMILPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> PermuteVar(Vector256<float> left, Vector256<int> control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_permutevar_pd (__m256d a, __m256i b)</para>
        ///   <para>  VPERMILPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPERMILPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> PermuteVar(Vector256<double> left, Vector256<long> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_rcp_ps (__m256 a)</para>
        ///   <para>  VRCPPS ymm1, ymm2/m256</para>
        /// </summary>
        public static Vector256<float> Reciprocal(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_rsqrt_ps (__m256 a)</para>
        ///   <para>  VRSQRTPS ymm1, ymm2/m256</para>
        /// </summary>
        public static Vector256<float> ReciprocalSqrt(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_round_ps (__m256 a, _MM_FROUND_CUR_DIRECTION)</para>
        ///   <para>  VROUNDPS ymm1, ymm2/m256, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector256<float> RoundCurrentDirection(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_round_ps (__m256d a, _MM_FROUND_CUR_DIRECTION)</para>
        ///   <para>  VROUNDPD ymm1, ymm2/m256, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector256<double> RoundCurrentDirection(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_round_ps (__m256 a, _MM_FROUND_TO_NEAREST_INT)</para>
        ///   <para>  VROUNDPS ymm1, ymm2/m256, imm8(8)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector256<float> RoundToNearestInteger(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_round_pd (__m256d a, _MM_FROUND_TO_NEAREST_INT)</para>
        ///   <para>  VROUNDPD ymm1, ymm2/m256, imm8(8)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector256<double> RoundToNearestInteger(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_round_ps (__m256 a, _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC)</para>
        ///   <para>  VROUNDPS ymm1, ymm2/m256, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector256<float> RoundToNegativeInfinity(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_round_pd (__m256d a, _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC)</para>
        ///   <para>  VROUNDPD ymm1, ymm2/m256, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector256<double> RoundToNegativeInfinity(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_round_ps (__m256 a, _MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC)</para>
        ///   <para>  VROUNDPS ymm1, ymm2/m256, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector256<float> RoundToPositiveInfinity(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_round_pd (__m256d a, _MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC)</para>
        ///   <para>  VROUNDPD ymm1, ymm2/m256, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector256<double> RoundToPositiveInfinity(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_round_ps (__m256 a, _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC)</para>
        ///   <para>  VROUNDPS ymm1, ymm2/m256, imm8(11)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector256<float> RoundToZero(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_round_pd (__m256d a, _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC)</para>
        ///   <para>  VROUNDPD ymm1, ymm2/m256, imm8(11)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector256<double> RoundToZero(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_shuffle_ps (__m256 a, __m256 b, const int imm8)</para>
        ///   <para>  VSHUFPS ymm1,         ymm2, ymm3/m256,         imm8</para>
        ///   <para>  VSHUFPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<float> Shuffle(Vector256<float> value, Vector256<float> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_shuffle_pd (__m256d a, __m256d b, const int imm8)</para>
        ///   <para>  VSHUFPD ymm1,         ymm2, ymm3/m256,         imm8</para>
        ///   <para>  VSHUFPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<double> Shuffle(Vector256<double> value, Vector256<double> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_sqrt_ps (__m256 a)</para>
        ///   <para>  VSQRTPS ymm1,         ymm2/m256</para>
        ///   <para>  VSQRTPS ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> Sqrt(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_sqrt_pd (__m256d a)</para>
        ///   <para>  VSQRTPD ymm1,         ymm2/m256</para>
        ///   <para>  VSQRTPD ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> Sqrt(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>void _mm256_storeu_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQU  m256,         ymm1</para>
        ///   <para>  VMOVDQU8 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void Store(sbyte* address, Vector256<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_storeu_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQU  m256,         ymm1</para>
        ///   <para>  VMOVDQU8 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void Store(byte* address, Vector256<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_storeu_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQU   m256,         ymm1</para>
        ///   <para>  VMOVDQU16 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void Store(short* address, Vector256<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_storeu_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQU   m256,         ymm1</para>
        ///   <para>  VMOVDQU16 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void Store(ushort* address, Vector256<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_storeu_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQU   m256,         ymm1</para>
        ///   <para>  VMOVDQU32 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void Store(int* address, Vector256<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_storeu_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQU   m256,         ymm1</para>
        ///   <para>  VMOVDQU32 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void Store(uint* address, Vector256<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_storeu_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQU   m256,         ymm1</para>
        ///   <para>  VMOVDQU64 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void Store(long* address, Vector256<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_storeu_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQU   m256,         ymm1</para>
        ///   <para>  VMOVDQU64 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void Store(ulong* address, Vector256<ulong> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_storeu_ps (float * mem_addr, __m256 a)</para>
        ///   <para>  VMOVUPS m256,         ymm1</para>
        ///   <para>  VMOVUPS m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void Store(float* address, Vector256<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_storeu_pd (double * mem_addr, __m256d a)</para>
        ///   <para>  VMOVUPD m256,         ymm1</para>
        ///   <para>  VMOVUPD m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void Store(double* address, Vector256<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>void _mm256_store_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQA   m256,         ymm1</para>
        ///   <para>  VMOVDQA32 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void StoreAligned(sbyte* address, Vector256<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_store_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQA   m256,         ymm1</para>
        ///   <para>  VMOVDQA32 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void StoreAligned(byte* address, Vector256<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_store_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQA   m256,         ymm1</para>
        ///   <para>  VMOVDQA32 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void StoreAligned(short* address, Vector256<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_store_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQA   m256,         ymm1</para>
        ///   <para>  VMOVDQA32 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void StoreAligned(ushort* address, Vector256<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_store_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQA   m256,         ymm1</para>
        ///   <para>  VMOVDQA32 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void StoreAligned(int* address, Vector256<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_store_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQA   m256,         ymm1</para>
        ///   <para>  VMOVDQA32 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void StoreAligned(uint* address, Vector256<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_store_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQA   m256,         ymm1</para>
        ///   <para>  VMOVDQA64 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void StoreAligned(long* address, Vector256<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_store_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVDQA   m256,         ymm1</para>
        ///   <para>  VMOVDQA64 m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void StoreAligned(ulong* address, Vector256<ulong> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_store_ps (float * mem_addr, __m256 a)</para>
        ///   <para>  VMOVAPS m256,         ymm1</para>
        ///   <para>  VMOVAPS m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void StoreAligned(float* address, Vector256<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_store_pd (double * mem_addr, __m256d a)</para>
        ///   <para>  VMOVAPD m256,         ymm1</para>
        ///   <para>  VMOVAPD m256 {k1}{z}, ymm1</para>
        /// </summary>
        public static unsafe void StoreAligned(double* address, Vector256<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>void _mm256_stream_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVNTDQ m256, ymm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(sbyte* address, Vector256<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_stream_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVNTDQ m256, ymm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(byte* address, Vector256<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_stream_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVNTDQ m256, ymm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(short* address, Vector256<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_stream_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVNTDQ m256, ymm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(ushort* address, Vector256<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_stream_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVNTDQ m256, ymm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(int* address, Vector256<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_stream_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVNTDQ m256, ymm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(uint* address, Vector256<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_stream_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVNTDQ m256, ymm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(long* address, Vector256<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_stream_si256 (__m256i * mem_addr, __m256i a)</para>
        ///   <para>  VMOVNTDQ m256, ymm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(ulong* address, Vector256<ulong> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_stream_ps (float * mem_addr, __m256 a)</para>
        ///   <para>  VMOVNTPS m256, ymm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(float* address, Vector256<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_stream_pd (double * mem_addr, __m256d a)</para>
        ///   <para>  VMOVNTPD m256, ymm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(double* address, Vector256<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_sub_ps (__m256 a, __m256 b)</para>
        ///   <para>  VSUBPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VSUBPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> Subtract(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_sub_pd (__m256d a, __m256d b)</para>
        ///   <para>  VSUBPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VSUBPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> Subtract(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>int _mm_testc_ps (__m128 a, __m128 b)</para>
        ///   <para>  VTESTPS xmm1, xmm2/m128    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm_testc_pd (__m128d a, __m128d b)</para>
        ///   <para>  VTESTPD xmm1, xmm2/m128    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testc_ps (__m256 a, __m256 b)</para>
        ///   <para>  VTESTPS ymm1, ymm2/m256    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testc_pd (__m256d a, __m256d b)</para>
        ///   <para>  VTESTPD ymm1, ymm2/m256    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>int _mm_testnzc_ps (__m128 a, __m128 b)</para>
        ///   <para>  VTESTPS xmm1, ymm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm_testnzc_pd (__m128d a, __m128d b)</para>
        ///   <para>  VTESTPD xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testnzc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testnzc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testnzc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testnzc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testnzc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testnzc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testnzc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testnzc_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testnzc_ps (__m256 a, __m256 b)</para>
        ///   <para>  VTESTPS ymm1, ymm2/m256    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testnzc_pd (__m256d a, __m256d b)</para>
        ///   <para>  VTESTPD ymm1, ymm2/m256    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>int _mm_testz_ps (__m128 a, __m128 b)</para>
        ///   <para>  VTESTPS xmm1, xmm2/m128    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm_testz_pd (__m128d a, __m128d b)</para>
        ///   <para>  VTESTPD xmm1, xmm2/m128    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testz_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testz_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testz_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testz_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testz_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testz_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testz_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testz_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPTEST ymm1, ymm2/m256    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testz_ps (__m256 a, __m256 b)</para>
        ///   <para>  VTESTPS ymm1, ymm2/m256    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_testz_pd (__m256d a, __m256d b)</para>
        ///   <para>  VTESTPD ymm1, ymm2/m256    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_unpackhi_ps (__m256 a, __m256 b)</para>
        ///   <para>  VUNPCKHPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VUNPCKHPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> UnpackHigh(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_unpackhi_pd (__m256d a, __m256d b)</para>
        ///   <para>  VUNPCKHPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VUNPCKHPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> UnpackHigh(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_unpacklo_ps (__m256 a, __m256 b)</para>
        ///   <para>  VUNPCKLPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VUNPCKLPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> UnpackLow(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_unpacklo_pd (__m256d a, __m256d b)</para>
        ///   <para>  VUNPCKLPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VUNPCKLPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> UnpackLow(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256 _mm256_xor_ps (__m256 a, __m256 b)</para>
        ///   <para>  VXORPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VXORPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> Xor(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_xor_pd (__m256d a, __m256d b)</para>
        ///   <para>  VXORPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VXORPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> Xor(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }
    }
}
