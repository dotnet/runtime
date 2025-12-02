// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 AVX512F hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Avx512F : Avx2
    {
        internal Avx512F() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the x86 AVX512F+VL hardware instructions via intrinsics.</summary>
        [Intrinsic]
        public abstract class VL
        {
            internal VL() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>__m128i _mm_abs_epi64 (__m128i a)</para>
            ///   <para>  VPABSQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
            /// </summary>
            public static Vector128<ulong> Abs(Vector128<long> value) => Abs(value);
            /// <summary>
            ///   <para>__m256i _mm256_abs_epi64 (__m128i a)</para>
            ///   <para>  VPABSQ ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
            /// </summary>
            public static Vector256<ulong> Abs(Vector256<long> value) => Abs(value);

            /// <summary>
            ///   <para>__m128i _mm_alignr_epi32 (__m128i a, __m128i b, const int count)</para>
            ///   <para>  VALIGND xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
            /// </summary>
            public static Vector128<int> AlignRight32(Vector128<int> left, Vector128<int> right, [ConstantExpected] byte mask) => AlignRight32(left, right, mask);
            /// <summary>
            ///   <para>__m128i _mm_alignr_epi32 (__m128i a, __m128i b, const int count)</para>
            ///   <para>  VALIGND xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
            /// </summary>
            public static Vector128<uint> AlignRight32(Vector128<uint> left, Vector128<uint> right, [ConstantExpected] byte mask) => AlignRight32(left, right, mask);
            /// <summary>
            ///   <para>__m256i _mm256_alignr_epi32 (__m256i a, __m256i b, const int count)</para>
            ///   <para>  VALIGND ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
            /// </summary>
            public static Vector256<int> AlignRight32(Vector256<int> left, Vector256<int> right, [ConstantExpected] byte mask) => AlignRight32(left, right, mask);
            /// <summary>
            ///   <para>__m256i _mm256_alignr_epi32 (__m256i a, __m256i b, const int count)</para>
            ///   <para>  VALIGND ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
            /// </summary>
            public static Vector256<uint> AlignRight32(Vector256<uint> left, Vector256<uint> right, [ConstantExpected] byte mask) => AlignRight32(left, right, mask);

            /// <summary>
            ///   <para>__m128i _mm_alignr_epi64 (__m128i a, __m128i b, const int count)</para>
            ///   <para>  VALIGNQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
            /// </summary>
            public static Vector128<long> AlignRight64(Vector128<long> left, Vector128<long> right, [ConstantExpected] byte mask) => AlignRight64(left, right, mask);
            /// <summary>
            ///   <para>__m128i _mm_alignr_epi64 (__m128i a, __m128i b, const int count)</para>
            ///   <para>  VALIGNQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
            /// </summary>
            public static Vector128<ulong> AlignRight64(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected] byte mask) => AlignRight64(left, right, mask);
            /// <summary>
            ///   <para>__m256i _mm256_alignr_epi64 (__m256i a, __m256i b, const int count)</para>
            ///   <para>  VALIGNQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<long> AlignRight64(Vector256<long> left, Vector256<long> right, [ConstantExpected] byte mask) => AlignRight64(left, right, mask);
            /// <summary>
            ///   <para>__m256i _mm256_alignr_epi64 (__m256i a, __m256i b, const int count)</para>
            ///   <para>  VALIGNQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<ulong> AlignRight64(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected] byte mask) => AlignRight64(left, right, mask);

            /// <summary>
            ///   <para>__m128d _mm_mask_blendv_pd (__m128d a, __m128d b, __mmask8 mask)</para>
            ///   <para>  VBLENDMPD xmm1 {k1}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector128<double> BlendVariable(Vector128<double> left, Vector128<double> right, Vector128<double> mask) => BlendVariable(left, right, mask);
            /// <summary>
            ///   <para>__m128i _mm_mask_blendv_epi32 (__m128i a, __m128i b, __mmask8 mask)</para>
            ///   <para>  VPBLENDMD xmm1 {k1}, xmm2, xmm3/m128/m32bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector128<int> BlendVariable(Vector128<int> left, Vector128<int> right, Vector128<int> mask) => BlendVariable(left, right, mask);
            /// <summary>
            ///   <para>__m128i _mm_mask_blendv_epi64 (__m128i a, __m128i b, __mmask8 mask)</para>
            ///   <para>  VPBLENDMQ xmm1 {k1}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector128<long> BlendVariable(Vector128<long> left, Vector128<long> right, Vector128<long> mask) => BlendVariable(left, right, mask);
            /// <summary>
            ///   <para>__m128 _mm_mask_blendv_ps (__m128 a, __m128 b, __mmask8 mask)</para>
            ///   <para>  VBLENDMPS xmm1 {k1}, xmm2, xmm3/m128/m32bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector128<float> BlendVariable(Vector128<float> left, Vector128<float> right, Vector128<float> mask) => BlendVariable(left, right, mask);
            /// <summary>
            ///   <para>__m128i _mm_mask_blendv_epu32 (__m128i a, __m128i b, __mmask8 mask)</para>
            ///   <para>  VPBLENDMD xmm1 {k1}, xmm2, xmm3/m128/m32bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector128<uint> BlendVariable(Vector128<uint> left, Vector128<uint> right, Vector128<uint> mask) => BlendVariable(left, right, mask);
            /// <summary>
            ///   <para>__m128i _mm_mask_blendv_epu64 (__m128i a, __m128i b, __mmask8 mask)</para>
            ///   <para>  VPBLENDMQ xmm1 {k1}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector128<ulong> BlendVariable(Vector128<ulong> left, Vector128<ulong> right, Vector128<ulong> mask) => BlendVariable(left, right, mask);

            /// <summary>
            ///   <para>__m256d _mm256_mask_blendv_pd (__m256d a, __m256d b, __mmask8 mask)</para>
            ///   <para>  VBLENDMPD ymm1 {k1}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<double> BlendVariable(Vector256<double> left, Vector256<double> right, Vector256<double> mask) => BlendVariable(left, right, mask);
            /// <summary>
            ///   <para>__m256i _mm256_mask_blendv_epi32 (__m256i a, __m256i b, __mmask8 mask)</para>
            ///   <para>  VPBLENDMD ymm1 {k1}, ymm2, ymm3/m256/m32bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<int> BlendVariable(Vector256<int> left, Vector256<int> right, Vector256<int> mask) => BlendVariable(left, right, mask);
            /// <summary>
            ///   <para>__m256i _mm256_mask_blendv_epi64 (__m256i a, __m256i b, __mmask8 mask)</para>
            ///   <para>  VPBLENDMQ ymm1 {k1}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<long> BlendVariable(Vector256<long> left, Vector256<long> right, Vector256<long> mask) => BlendVariable(left, right, mask);
            /// <summary>
            ///   <para>__m256 _mm256_mask_blendv_ps (__m256 a, __m256 b, __mmask8 mask)</para>
            ///   <para>  VBLENDMPS ymm1 {k1}, ymm2, ymm3/m256/m32bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<float> BlendVariable(Vector256<float> left, Vector256<float> right, Vector256<float> mask) => BlendVariable(left, right, mask);
            /// <summary>
            ///   <para>__m256i _mm256_mask_blendv_epu32 (__m256i a, __m256i b, __mmask8 mask)</para>
            ///   <para>  VPBLENDMD ymm1 {k1}, ymm2, ymm3/m256/m32bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<uint> BlendVariable(Vector256<uint> left, Vector256<uint> right, Vector256<uint> mask) => BlendVariable(left, right, mask);
            /// <summary>
            ///   <para>__m256i _mm256_mask_blendv_epu64 (__m256i a, __m256i b, __mmask8 mask)</para>
            ///   <para>  VPBLENDMQ ymm1 {k1}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<ulong> BlendVariable(Vector256<ulong> left, Vector256<ulong> right, Vector256<ulong> mask) => BlendVariable(left, right, mask);

            /// <summary>
            ///   <para>__mmask8 _mm_cmp_pd_mask (__m128d a, __m128d b, const int imm8)</para>
            ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8</para>
            /// </summary>
            public static Vector128<double> Compare(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) => Compare(left, right, mode);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpeq_pd_mask (__m128d a,  __m128d b)</para>
            ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(0)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<double> CompareEqual(Vector128<double> left, Vector128<double> right) => CompareEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpgt_pd_mask (__m128d a,  __m128d b)</para>
            ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(14)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<double> CompareGreaterThan(Vector128<double> left, Vector128<double> right) => CompareGreaterThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpge_pd_mask (__m128d a,  __m128d b)</para>
            ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(13)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<double> CompareGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmplt_pd_mask (__m128d a,  __m128d b)</para>
            ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(1)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<double> CompareLessThan(Vector128<double> left, Vector128<double> right) => CompareLessThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmple_pd_mask (__m128d a,  __m128d b)</para>
            ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(2)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<double> CompareLessThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpneq_pd_mask (__m128d a,  __m128d b)</para>
            ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(4)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<double> CompareNotEqual(Vector128<double> left, Vector128<double> right) => CompareNotEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpngt_pd_mask (__m128d a,  __m128d b)</para>
            ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(10)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<double> CompareNotGreaterThan(Vector128<double> left, Vector128<double> right) => CompareNotGreaterThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpnge_pd_mask (__m128d a,  __m128d b)</para>
            ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(9)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<double> CompareNotGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareNotGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpnlt_pd_mask (__m128d a,  __m128d b)</para>
            ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(5)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<double> CompareNotLessThan(Vector128<double> left, Vector128<double> right) => CompareNotLessThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpnle_pd_mask (__m128d a,  __m128d b)</para>
            ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(6)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<double> CompareNotLessThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareNotLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpord_pd_mask (__m128d a,  __m128d b)</para>
            ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(7)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<double> CompareOrdered(Vector128<double> left, Vector128<double> right) => CompareOrdered(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpunord_pd_mask (__m128d a,  __m128d b)</para>
            ///   <para>  VCMPPD k1 {k2}, xmm2, xmm3/m128/m64bcst{sae}, imm8(3)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<double> CompareUnordered(Vector128<double> left, Vector128<double> right) => CompareUnordered(left, right);

            /// <summary>
            ///   <para>__mmask8 _mm256_cmp_pd_mask (__m256d a, __m256d b, const int imm8)</para>
            ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8</para>
            /// </summary>
            public static Vector256<double> Compare(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) => Compare(left, right, mode);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpeq_pd_mask (__m256d a,  __m256d b)</para>
            ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(0)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<double> CompareEqual(Vector256<double> left, Vector256<double> right) => CompareEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpgt_pd_mask (__m256d a,  __m256d b)</para>
            ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(14)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<double> CompareGreaterThan(Vector256<double> left, Vector256<double> right) => CompareGreaterThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpge_pd_mask (__m256d a,  __m256d b)</para>
            ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(13)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<double> CompareGreaterThanOrEqual(Vector256<double> left, Vector256<double> right) => CompareGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmplt_pd_mask (__m256d a,  __m256d b)</para>
            ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(1)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<double> CompareLessThan(Vector256<double> left, Vector256<double> right) => CompareLessThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmple_pd_mask (__m256d a,  __m256d b)</para>
            ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(2)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<double> CompareLessThanOrEqual(Vector256<double> left, Vector256<double> right) => CompareLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpneq_pd_mask (__m256d a,  __m256d b)</para>
            ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(4)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<double> CompareNotEqual(Vector256<double> left, Vector256<double> right) => CompareNotEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpngt_pd_mask (__m256d a,  __m256d b)</para>
            ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(10)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<double> CompareNotGreaterThan(Vector256<double> left, Vector256<double> right) => CompareNotGreaterThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpnge_pd_mask (__m256d a,  __m256d b)</para>
            ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(9)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<double> CompareNotGreaterThanOrEqual(Vector256<double> left, Vector256<double> right) => CompareNotGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpnlt_pd_mask (__m256d a,  __m256d b)</para>
            ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(5)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<double> CompareNotLessThan(Vector256<double> left, Vector256<double> right) => CompareNotLessThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpnle_pd_mask (__m256d a,  __m256d b)</para>
            ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(6)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<double> CompareNotLessThanOrEqual(Vector256<double> left, Vector256<double> right) => CompareNotLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpord_pd_mask (__m256d a,  __m256d b)</para>
            ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(7)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<double> CompareOrdered(Vector256<double> left, Vector256<double> right) => CompareOrdered(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpunord_pd_mask (__m256d a,  __m256d b)</para>
            ///   <para>  VCMPPD k1 {k2}, ymm2, ymm3/m256/m64bcst{sae}, imm8(3)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<double> CompareUnordered(Vector256<double> left, Vector256<double> right) => CompareUnordered(left, right);

            /// <summary>
            ///   <para>__mmask8 _mm_cmpeq_epi32_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(0)</para>
            /// </summary>
            public static Vector128<int> CompareEqual(Vector128<int> left, Vector128<int> right) => CompareEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpgt_epi32_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(6)</para>
            /// </summary>
            public static Vector128<int> CompareGreaterThan(Vector128<int> left, Vector128<int> right) => CompareGreaterThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpge_epi32_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(5)</para>
            /// </summary>
            public static Vector128<int> CompareGreaterThanOrEqual(Vector128<int> left, Vector128<int> right) => CompareGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmplt_epi32_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(1)</para>
            /// </summary>
            public static Vector128<int> CompareLessThan(Vector128<int> left, Vector128<int> right) => CompareLessThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmple_epi32_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(2)</para>
            /// </summary>
            public static Vector128<int> CompareLessThanOrEqual(Vector128<int> left, Vector128<int> right) => CompareLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpne_epi32_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(4)</para>
            /// </summary>
            public static Vector128<int> CompareNotEqual(Vector128<int> left, Vector128<int> right) => CompareNotEqual(left, right);

            /// <summary>
            ///   <para>__mmask8 _mm_cmpeq_epi32_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(0)</para>
            /// </summary>
            public static Vector256<int> CompareEqual(Vector256<int> left, Vector256<int> right) => CompareEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpgt_epi32_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(6)</para>
            /// </summary>
            public static Vector256<int> CompareGreaterThan(Vector256<int> left, Vector256<int> right) => CompareGreaterThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpge_epi32_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(5)</para>
            /// </summary>
            public static Vector256<int> CompareGreaterThanOrEqual(Vector256<int> left, Vector256<int> right) => CompareGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmplt_epi32_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(1)</para>
            /// </summary>
            public static Vector256<int> CompareLessThan(Vector256<int> left, Vector256<int> right) => CompareLessThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmple_epi32_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(2)</para>
            /// </summary>
            public static Vector256<int> CompareLessThanOrEqual(Vector256<int> left, Vector256<int> right) => CompareLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpne_epi32_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(4)</para>
            /// </summary>
            public static Vector256<int> CompareNotEqual(Vector256<int> left, Vector256<int> right) => CompareNotEqual(left, right);

            /// <summary>
            ///   <para>__mmask8 _mm_cmpeq_epi64_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(0)</para>
            /// </summary>
            public static Vector128<long> CompareEqual(Vector128<long> left, Vector128<long> right) => CompareEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpgt_epi64_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(6)</para>
            /// </summary>
            public static Vector128<long> CompareGreaterThan(Vector128<long> left, Vector128<long> right) => CompareGreaterThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpge_epi64_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(5)</para>
            /// </summary>
            public static Vector128<long> CompareGreaterThanOrEqual(Vector128<long> left, Vector128<long> right) => CompareGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmplt_epi64_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(1)</para>
            /// </summary>
            public static Vector128<long> CompareLessThan(Vector128<long> left, Vector128<long> right) => CompareLessThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmple_epi64_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(2)</para>
            /// </summary>
            public static Vector128<long> CompareLessThanOrEqual(Vector128<long> left, Vector128<long> right) => CompareLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpne_epi64_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(4)</para>
            /// </summary>
            public static Vector128<long> CompareNotEqual(Vector128<long> left, Vector128<long> right) => CompareNotEqual(left, right);

            /// <summary>
            ///   <para>__mmask8 _mm256_cmpeq_epi64_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(0)</para>
            /// </summary>
            public static Vector256<long> CompareEqual(Vector256<long> left, Vector256<long> right) => CompareEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpgt_epi64_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(6)</para>
            /// </summary>
            public static Vector256<long> CompareGreaterThan(Vector256<long> left, Vector256<long> right) => CompareGreaterThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpge_epi64_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(5)</para>
            /// </summary>
            public static Vector256<long> CompareGreaterThanOrEqual(Vector256<long> left, Vector256<long> right) => CompareGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmplt_epi64_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(1)</para>
            /// </summary>
            public static Vector256<long> CompareLessThan(Vector256<long> left, Vector256<long> right) => CompareLessThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmple_epi64_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(2)</para>
            /// </summary>
            public static Vector256<long> CompareLessThanOrEqual(Vector256<long> left, Vector256<long> right) => CompareLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpne_epi64_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(4)</para>
            /// </summary>
            public static Vector256<long> CompareNotEqual(Vector256<long> left, Vector256<long> right) => CompareNotEqual(left, right);

            /// <summary>
            ///   <para>__mmask8 _mm_cmp_ps_mask (__m128 a, __m128 b, const int imm8)</para>
            ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8</para>
            /// </summary>
            public static Vector128<float> Compare(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) => Compare(left, right, mode);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpeq_ps_mask (__m128 a,  __m128 b)</para>
            ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(0)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<float> CompareEqual(Vector128<float> left, Vector128<float> right) => CompareEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpgt_ps_mask (__m128 a,  __m128 b)</para>
            ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(14)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<float> CompareGreaterThan(Vector128<float> left, Vector128<float> right) => CompareGreaterThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpge_ps_mask (__m128 a,  __m128 b)</para>
            ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(13)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<float> CompareGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmplt_ps_mask (__m128 a,  __m128 b)</para>
            ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(1)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<float> CompareLessThan(Vector128<float> left, Vector128<float> right) => CompareLessThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmple_ps_mask (__m128 a,  __m128 b)</para>
            ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(2)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<float> CompareLessThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpneq_ps_mask (__m128 a,  __m128 b)</para>
            ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(4)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<float> CompareNotEqual(Vector128<float> left, Vector128<float> right) => CompareNotEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpngt_ps_mask (__m128 a,  __m128 b)</para>
            ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(10)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<float> CompareNotGreaterThan(Vector128<float> left, Vector128<float> right) => CompareNotGreaterThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpnge_ps_mask (__m128 a,  __m128 b)</para>
            ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(9)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<float> CompareNotGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareNotGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpnlt_ps_mask (__m128 a,  __m128 b)</para>
            ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(5)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<float> CompareNotLessThan(Vector128<float> left, Vector128<float> right) => CompareNotLessThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpnle_ps_mask (__m128 a,  __m128 b)</para>
            ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(6)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<float> CompareNotLessThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareNotLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpord_ps_mask (__m128 a,  __m128 b)</para>
            ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(7)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<float> CompareOrdered(Vector128<float> left, Vector128<float> right) => CompareOrdered(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpunord_ps_mask (__m128 a,  __m128 b)</para>
            ///   <para>  VCMPPS k1 {k2}, xmm2, xmm3/m128/m32bcst{sae}, imm8(3)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector128<float> CompareUnordered(Vector128<float> left, Vector128<float> right) => CompareUnordered(left, right);

            /// <summary>
            ///   <para>__mmask8 _mm256_cmp_ps_mask (__m256 a, __m256 b, const int imm8)</para>
            ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8</para>
            /// </summary>
            public static Vector256<float> Compare(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) => Compare(left, right, mode);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpeq_ps_mask (__m256 a,  __m256 b)</para>
            ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(0)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<float> CompareEqual(Vector256<float> left, Vector256<float> right) => CompareEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpgt_ps_mask (__m256 a,  __m256 b)</para>
            ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(14)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<float> CompareGreaterThan(Vector256<float> left, Vector256<float> right) => CompareGreaterThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpge_ps_mask (__m256 a,  __m256 b)</para>
            ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(13)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<float> CompareGreaterThanOrEqual(Vector256<float> left, Vector256<float> right) => CompareGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmplt_ps_mask (__m256 a,  __m256 b)</para>
            ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(1)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<float> CompareLessThan(Vector256<float> left, Vector256<float> right) => CompareLessThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmple_ps_mask (__m256 a,  __m256 b)</para>
            ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(2)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<float> CompareLessThanOrEqual(Vector256<float> left, Vector256<float> right) => CompareLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpneq_ps_mask (__m256 a,  __m256 b)</para>
            ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(4)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<float> CompareNotEqual(Vector256<float> left, Vector256<float> right) => CompareNotEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpngt_ps_mask (__m256 a,  __m256 b)</para>
            ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(10)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<float> CompareNotGreaterThan(Vector256<float> left, Vector256<float> right) => CompareNotGreaterThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpnge_ps_mask (__m256 a,  __m256 b)</para>
            ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(9)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<float> CompareNotGreaterThanOrEqual(Vector256<float> left, Vector256<float> right) => CompareNotGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpnlt_ps_mask (__m256 a,  __m256 b)</para>
            ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(5)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<float> CompareNotLessThan(Vector256<float> left, Vector256<float> right) => CompareNotLessThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpnle_ps_mask (__m256 a,  __m256 b)</para>
            ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(6)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<float> CompareNotLessThanOrEqual(Vector256<float> left, Vector256<float> right) => CompareNotLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpord_ps_mask (__m256 a,  __m256 b)</para>
            ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(7)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<float> CompareOrdered(Vector256<float> left, Vector256<float> right) => CompareOrdered(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm256_cmpunord_ps_mask (__m256 a,  __m256 b)</para>
            ///   <para>  VCMPPS k1 {k2}, ymm2, ymm3/m256/m32bcst{sae}, imm8(3)</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
            /// </summary>
            public static Vector256<float> CompareUnordered(Vector256<float> left, Vector256<float> right) => CompareUnordered(left, right);

            /// <summary>
            ///   <para>__mmask8 _mm_cmpeq_epu32_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(0)</para>
            /// </summary>
            public static Vector128<uint> CompareEqual(Vector128<uint> left, Vector128<uint> right) => CompareEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpgt_epu32_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(6)</para>
            /// </summary>
            public static Vector128<uint> CompareGreaterThan(Vector128<uint> left, Vector128<uint> right) => CompareGreaterThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpge_epu32_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(5)</para>
            /// </summary>
            public static Vector128<uint> CompareGreaterThanOrEqual(Vector128<uint> left, Vector128<uint> right) => CompareGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmplt_epu32_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(1)</para>
            /// </summary>
            public static Vector128<uint> CompareLessThan(Vector128<uint> left, Vector128<uint> right) => CompareLessThan(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmple_epu32_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(2)</para>
            /// </summary>
            public static Vector128<uint> CompareLessThanOrEqual(Vector128<uint> left, Vector128<uint> right) => CompareLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mmask8 _mm_cmpne_epu32_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(4)</para>
            /// </summary>
            public static Vector128<uint> CompareNotEqual(Vector128<uint> left, Vector128<uint> right) => CompareNotEqual(left, right);

            /// <summary>
            ///   <para>__mask8 _mm256_cmpeq_epu32_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(0)</para>
            /// </summary>
            public static Vector256<uint> CompareEqual(Vector256<uint> left, Vector256<uint> right) => CompareEqual(left, right);
            /// <summary>
            ///   <para>__mask8 _mm256_cmpgt_epu32_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(6)</para>
            /// </summary>
            public static Vector256<uint> CompareGreaterThan(Vector256<uint> left, Vector256<uint> right) => CompareGreaterThan(left, right);
            /// <summary>
            ///   <para>__mask8 _mm256_cmpge_epu32_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(5)</para>
            /// </summary>
            public static Vector256<uint> CompareGreaterThanOrEqual(Vector256<uint> left, Vector256<uint> right) => CompareGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mask8 _mm256_cmplt_epu32_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(1)</para>
            /// </summary>
            public static Vector256<uint> CompareLessThan(Vector256<uint> left, Vector256<uint> right) => CompareLessThan(left, right);
            /// <summary>
            ///   <para>__mask8 _mm256_cmple_epu32_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(2)</para>
            /// </summary>
            public static Vector256<uint> CompareLessThanOrEqual(Vector256<uint> left, Vector256<uint> right) => CompareLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mask8 _mm256_cmpne_epu32_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(4)</para>
            /// </summary>
            public static Vector256<uint> CompareNotEqual(Vector256<uint> left, Vector256<uint> right) => CompareNotEqual(left, right);

            /// <summary>
            ///   <para>__mask8 _mm_cmpeq_epu64_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(0)</para>
            /// </summary>
            public static Vector128<ulong> CompareEqual(Vector128<ulong> left, Vector128<ulong> right) => CompareEqual(left, right);
            /// <summary>
            ///   <para>__mask8 _mm_cmpgt_epu64_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(6)</para>
            /// </summary>
            public static Vector128<ulong> CompareGreaterThan(Vector128<ulong> left, Vector128<ulong> right) => CompareGreaterThan(left, right);
            /// <summary>
            ///   <para>__mask8 _mm_cmpge_epu64_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(5)</para>
            /// </summary>
            public static Vector128<ulong> CompareGreaterThanOrEqual(Vector128<ulong> left, Vector128<ulong> right) => CompareGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mask8 _mm_cmplt_epu64_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(1)</para>
            /// </summary>
            public static Vector128<ulong> CompareLessThan(Vector128<ulong> left, Vector128<ulong> right) => CompareLessThan(left, right);
            /// <summary>
            ///   <para>__mask8 _mm_cmple_epu64_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(2)</para>
            /// </summary>
            public static Vector128<ulong> CompareLessThanOrEqual(Vector128<ulong> left, Vector128<ulong> right) => CompareLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mask8 _mm_cmpne_epu64_mask (__m128i a, __m128i b)</para>
            ///   <para>  VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(4)</para>
            /// </summary>
            public static Vector128<ulong> CompareNotEqual(Vector128<ulong> left, Vector128<ulong> right) => CompareNotEqual(left, right);

            /// <summary>
            ///   <para>__mask8 _mm256_cmpeq_epu64_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(0)</para>
            /// </summary>
            public static Vector256<ulong> CompareEqual(Vector256<ulong> left, Vector256<ulong> right) => CompareEqual(left, right);
            /// <summary>
            ///   <para>__mask8 _mm256_cmpgt_epu64_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(6)</para>
            /// </summary>
            public static Vector256<ulong> CompareGreaterThan(Vector256<ulong> left, Vector256<ulong> right) => CompareGreaterThan(left, right);
            /// <summary>
            ///   <para>__mask8 _mm256_cmpge_epu64_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(5)</para>
            /// </summary>
            public static Vector256<ulong> CompareGreaterThanOrEqual(Vector256<ulong> left, Vector256<ulong> right) => CompareGreaterThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mask8 _mm256_cmplt_epu64_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(1)</para>
            /// </summary>
            public static Vector256<ulong> CompareLessThan(Vector256<ulong> left, Vector256<ulong> right) => CompareLessThan(left, right);
            /// <summary>
            ///   <para>__mask8 _mm256_cmple_epu64_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(2)</para>
            /// </summary>
            public static Vector256<ulong> CompareLessThanOrEqual(Vector256<ulong> left, Vector256<ulong> right) => CompareLessThanOrEqual(left, right);
            /// <summary>
            ///   <para>__mask8 _mm256_cmpne_epu64_mask (__m256i a, __m256i b)</para>
            ///   <para>  VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(4)</para>
            /// </summary>
            public static Vector256<ulong> CompareNotEqual(Vector256<ulong> left, Vector256<ulong> right) => CompareNotEqual(left, right);

            /// <summary>
            ///   <para>__m128d _mm_mask_compress_pd (__m128d s, __mmask8 k, __m128d a)</para>
            ///   <para>  VCOMPRESSPD xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<double> Compress(Vector128<double> merge, Vector128<double> mask, Vector128<double> value) => Compress(merge, mask, value);
            /// <summary>
            ///   <para>__m128i _mm_mask_compress_epi32 (__m128i s, __mask8 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSD xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<int> Compress(Vector128<int> merge, Vector128<int> mask, Vector128<int> value) => Compress(merge, mask, value);
            /// <summary>
            ///   <para>__m128i _mm_mask_compress_epi64 (__m128i s, __mask8 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSQ xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<long> Compress(Vector128<long> merge, Vector128<long> mask, Vector128<long> value) => Compress(merge, mask, value);
            /// <summary>
            ///   <para>__m128 _mm_mask_compress_ps (__m128 s, __mmask8 k, __m128 a)</para>
            ///   <para>  VCOMPRESSPS xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<float> Compress(Vector128<float> merge, Vector128<float> mask, Vector128<float> value) => Compress(merge, mask, value);
            /// <summary>
            ///   <para>__m128i _mm_mask_compress_epi32 (__m128i s, __mask8 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSD xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<uint> Compress(Vector128<uint> merge, Vector128<uint> mask, Vector128<uint> value) => Compress(merge, mask, value);
            /// <summary>
            ///   <para>__m128i _mm_mask_compress_epi64 (__m128i s, __mask8 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSQ xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<ulong> Compress(Vector128<ulong> merge, Vector128<ulong> mask, Vector128<ulong> value) => Compress(merge, mask, value);

            /// <summary>
            ///   <para>__m256d _mm256_mask_compress_pd (__m256d s, __mmask8 k, __m256d a)</para>
            ///   <para>  VCOMPRESSPD ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<double> Compress(Vector256<double> merge, Vector256<double> mask, Vector256<double> value) => Compress(merge, mask, value);
            /// <summary>
            ///   <para>__m256i _mm256_mask_compress_epi32 (__m256i s, __mmask8 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSD ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<int> Compress(Vector256<int> merge, Vector256<int> mask, Vector256<int> value) => Compress(merge, mask, value);
            /// <summary>
            ///   <para>__m256i _mm256_mask_compress_epi64 (__m256i s, __mmask8 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSQ ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<long> Compress(Vector256<long> merge, Vector256<long> mask, Vector256<long> value) => Compress(merge, mask, value);
            /// <summary>
            ///   <para>__m256 _mm256_mask_compress_ps (__m256 s, __mmask8 k, __m256 a)</para>
            ///   <para>  VCOMPRESSPS ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<float> Compress(Vector256<float> merge, Vector256<float> mask, Vector256<float> value) => Compress(merge, mask, value);
            /// <summary>
            ///   <para>__m256i _mm256_mask_compress_epi32 (__m256i s, __mmask8 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSD ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<uint> Compress(Vector256<uint> merge, Vector256<uint> mask, Vector256<uint> value) => Compress(merge, mask, value);
            /// <summary>
            ///   <para>__m256i _mm256_mask_compress_epi64 (__m256i s, __mmask8 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSQ ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<ulong> Compress(Vector256<ulong> merge, Vector256<ulong> mask, Vector256<ulong> value) => Compress(merge, mask, value);

            /// <summary>
            ///   <para>__m128d _mm_mask_compressstoreu_pd (void * a, __mmask8 k, __m128d a)</para>
            ///   <para>  VCOMPRESSPD m128 {k1}{z}, xmm2</para>
            /// </summary>
            public static unsafe void CompressStore(double* address, Vector128<double> mask, Vector128<double> source) => CompressStore(address, mask, source);
            /// <summary>
            ///   <para>__m128i _mm_mask_compressstoreu_epi32 (void * a, __mask8 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSD m128 {k1}{z}, xmm2</para>
            /// </summary>
            public static unsafe void CompressStore(int* address, Vector128<int> mask, Vector128<int> source) => CompressStore(address, mask, source);
            /// <summary>
            ///   <para>__m128i _mm_mask_compressstoreu_epi64 (void * a, __mask8 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSQ m128 {k1}{z}, xmm2</para>
            /// </summary>
            public static unsafe void CompressStore(long* address, Vector128<long> mask, Vector128<long> source) => CompressStore(address, mask, source);
            /// <summary>
            ///   <para>__m128 _mm_mask_compressstoreu_ps (void * a, __mmask8 k, __m128 a)</para>
            ///   <para>  VCOMPRESSPS m128 {k1}{z}, xmm2</para>
            /// </summary>
            public static unsafe void CompressStore(float* address, Vector128<float> mask, Vector128<float> source) => CompressStore(address, mask, source);
            /// <summary>
            ///   <para>__m128i _mm_mask_compressstoreu_epi32 (void * a, __mask8 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSD m128 {k1}{z}, xmm2</para>
            /// </summary>
            public static unsafe void CompressStore(uint* address, Vector128<uint> mask, Vector128<uint> source) => CompressStore(address, mask, source);
            /// <summary>
            ///   <para>__m128i _mm_mask_compressstoreu_epi64 (void * a, __mask8 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSQ m128 {k1}{z}, xmm2</para>
            /// </summary>
            public static unsafe void CompressStore(ulong* address, Vector128<ulong> mask, Vector128<ulong> source) => CompressStore(address, mask, source);

            /// <summary>
            ///   <para>__m256d _mm256_mask_compressstoreu_pd (void * a, __mmask8 k, __m256d a)</para>
            ///   <para>  VCOMPRESSPD m256 {k1}{z}, ymm2</para>
            /// </summary>
            public static unsafe void CompressStore(double* address, Vector256<double> mask, Vector256<double> source) => CompressStore(address, mask, source);
            /// <summary>
            ///   <para>void _mm256_mask_compressstoreu_epi32 (void * a, __mmask8 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSD m256 {k1}{z}, ymm2</para>
            /// </summary>
            public static unsafe void CompressStore(int* address, Vector256<int> mask, Vector256<int> source) => CompressStore(address, mask, source);
            /// <summary>
            ///   <para>void _mm256_mask_compressstoreu_epi64 (void * a, __mmask8 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSQ m256 {k1}{z}, ymm2</para>
            /// </summary>
            public static unsafe void CompressStore(long* address, Vector256<long> mask, Vector256<long> source) => CompressStore(address, mask, source);
            /// <summary>
            ///   <para>__m256 _mm256_mask_compressstoreu_ps (void * a, __mmask8 k, __m256 a)</para>
            ///   <para>  VCOMPRESSPS m256 {k1}{z}, ymm2</para>
            /// </summary>
            public static unsafe void CompressStore(float* address, Vector256<float> mask, Vector256<float> source) => CompressStore(address, mask, source);
            /// <summary>
            ///   <para>void _mm256_mask_compressstoreu_epi32 (void * a, __mmask8 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSD m256 {k1}{z}, ymm2</para>
            /// </summary>
            public static unsafe void CompressStore(uint* address, Vector256<uint> mask, Vector256<uint> source) => CompressStore(address, mask, source);
            /// <summary>
            ///   <para>void _mm256_mask_compressstoreu_epi64 (void * a, __mmask8 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSQ m256 {k1}{z}, ymm2</para>
            /// </summary>
            public static unsafe void CompressStore(ulong* address, Vector256<ulong> mask, Vector256<ulong> source) => CompressStore(address, mask, source);

            /// <summary>
            ///   <para>__m128i _mm_cvtepi32_epi8 (__m128i a)</para>
            ///   <para>  VPMOVDB xmm1/m32 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector128<int> value) => ConvertToVector128Byte(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtepi64_epi8 (__m128i a)</para>
            ///   <para>  VPMOVQB xmm1/m16 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector128<long> value) => ConvertToVector128Byte(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtepi32_epi8 (__m128i a)</para>
            ///   <para>  VPMOVDB xmm1/m32 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector128<uint> value) => ConvertToVector128Byte(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtepi64_epi8 (__m128i a)</para>
            ///   <para>  VPMOVQB xmm1/m16 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector128<ulong> value) => ConvertToVector128Byte(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi32_epi8 (__m256i a)</para>
            ///   <para>  VPMOVDB xmm1/m64 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector256<int> value) => ConvertToVector128Byte(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi64_epi8 (__m256i a)</para>
            ///   <para>  VPMOVQB xmm1/m32 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector256<long> value) => ConvertToVector128Byte(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi32_epi8 (__m256i a)</para>
            ///   <para>  VPMOVDB xmm1/m64 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector256<uint> value) => ConvertToVector128Byte(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi64_epi8 (__m256i a)</para>
            ///   <para>  VPMOVQB xmm1/m32 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector256<ulong> value) => ConvertToVector128Byte(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtusepi32_epi8 (__m128i a)</para>
            ///   <para>  VPMOVUSDB xmm1/m32 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector128<uint> value) => ConvertToVector128ByteWithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtusepi64_epi8 (__m128i a)</para>
            ///   <para>  VPMOVUSQB xmm1/m16 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector128<ulong> value) => ConvertToVector128ByteWithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtusepi32_epi8 (__m256i a)</para>
            ///   <para>  VPMOVUSDB xmm1/m64 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector256<uint> value) => ConvertToVector128ByteWithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtusepi64_epi8 (__m256i a)</para>
            ///   <para>  VPMOVUSQB xmm1/m32 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector256<ulong> value) => ConvertToVector128ByteWithSaturation(value);

            /// <summary>
            ///   <para>__m128d _mm_cvtepu32_pd (__m128i a)</para>
            ///   <para>  VCVTUDQ2PD xmm1 {k1}{z}, xmm2/m64/m32bcst</para>
            /// </summary>
            public static Vector128<double> ConvertToVector128Double(Vector128<uint> value) => ConvertToVector128Double(value);

            /// <summary>
            ///   <para>__m128i _mm_cvtepi32_epi16 (__m128i a)</para>
            ///   <para>  VPMOVDW xmm1/m64 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector128<int> value) => ConvertToVector128Int16(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtepi64_epi16 (__m128i a)</para>
            ///   <para>  VPMOVQW xmm1/m32 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector128<long> value) => ConvertToVector128Int16(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtepi32_epi16 (__m128i a)</para>
            ///   <para>  VPMOVDW xmm1/m64 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector128<uint> value) => ConvertToVector128Int16(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtepi64_epi16 (__m128i a)</para>
            ///   <para>  VPMOVQW xmm1/m32 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector128<ulong> value) => ConvertToVector128Int16(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi32_epi16 (__m256i a)</para>
            ///   <para>  VPMOVDW xmm1/m128 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector256<int> value) => ConvertToVector128Int16(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi64_epi16 (__m256i a)</para>
            ///   <para>  VPMOVQW xmm1/m64 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector256<long> value) => ConvertToVector128Int16(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi32_epi16 (__m256i a)</para>
            ///   <para>  VPMOVDW xmm1/m128 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector256<uint> value) => ConvertToVector128Int16(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi64_epi16 (__m256i a)</para>
            ///   <para>  VPMOVQW xmm1/m64 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector256<ulong> value) => ConvertToVector128Int16(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtsepi32_epi16 (__m128i a)</para>
            ///   <para>  VPMOVSDW xmm1/m64 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector128<int> value) => ConvertToVector128Int16WithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtsepi64_epi16 (__m128i a)</para>
            ///   <para>  VPMOVSQW xmm1/m32 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector128<long> value) => ConvertToVector128Int16WithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtsepi32_epi16 (__m256i a)</para>
            ///   <para>  VPMOVSDW xmm1/m128 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector256<int> value) => ConvertToVector128Int16WithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtsepi64_epi16 (__m256i a)</para>
            ///   <para>  VPMOVSQW xmm1/m64 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector256<long> value) => ConvertToVector128Int16WithSaturation(value);

            /// <summary>
            ///   <para>__m128i _mm_cvtepi64_epi32 (__m128i a)</para>
            ///   <para>  VPMOVQD xmm1/m64 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<int> ConvertToVector128Int32(Vector128<long> value) => ConvertToVector128Int32(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtepi64_epi32 (__m128i a)</para>
            ///   <para>  VPMOVQD xmm1/m64 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<int> ConvertToVector128Int32(Vector128<ulong> value) => ConvertToVector128Int32(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi64_epi32 (__m256i a)</para>
            ///   <para>  VPMOVQD xmm1/m128 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<int> ConvertToVector128Int32(Vector256<long> value) => ConvertToVector128Int32(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi64_epi32 (__m256i a)</para>
            ///   <para>  VPMOVQD xmm1/m128 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<int> ConvertToVector128Int32(Vector256<ulong> value) => ConvertToVector128Int32(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtsepi64_epi32 (__m128i a)</para>
            ///   <para>  VPMOVSQD xmm1/m64 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<int> ConvertToVector128Int32WithSaturation(Vector128<long> value) => ConvertToVector128Int32WithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtsepi64_epi32 (__m256i a)</para>
            ///   <para>  VPMOVSQD xmm1/m128 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<int> ConvertToVector128Int32WithSaturation(Vector256<long> value) => ConvertToVector128Int32WithSaturation(value);

            /// <summary>
            ///   <para>__m128i _mm_cvtepi32_epi8 (__m128i a)</para>
            ///   <para>  VPMOVDB xmm1/m32 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector128<int> value) => ConvertToVector128SByte(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtepi64_epi8 (__m128i a)</para>
            ///   <para>  VPMOVQB xmm1/m16 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector128<long> value) => ConvertToVector128SByte(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtepi32_epi8 (__m128i a)</para>
            ///   <para>  VPMOVDB xmm1/m32 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector128<uint> value) => ConvertToVector128SByte(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtepi64_epi8 (__m128i a)</para>
            ///   <para>  VPMOVQB xmm1/m16 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector128<ulong> value) => ConvertToVector128SByte(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi32_epi8 (__m256i a)</para>
            ///   <para>  VPMOVDB xmm1/m64 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector256<int> value) => ConvertToVector128SByte(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi64_epi8 (__m256i a)</para>
            ///   <para>  VPMOVQB xmm1/m32 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector256<long> value) => ConvertToVector128SByte(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi32_epi8 (__m256i a)</para>
            ///   <para>  VPMOVDB xmm1/m64 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector256<uint> value) => ConvertToVector128SByte(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi64_epi8 (__m256i a)</para>
            ///   <para>  VPMOVQB xmm1/m32 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector256<ulong> value) => ConvertToVector128SByte(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtsepi32_epi8 (__m128i a)</para>
            ///   <para>  VPMOVSDB xmm1/m32 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector128<int> value) => ConvertToVector128SByteWithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtsepi64_epi8 (__m128i a)</para>
            ///   <para>  VPMOVSQB xmm1/m16 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector128<long> value) => ConvertToVector128SByteWithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtsepi32_epi8 (__m256i a)</para>
            ///   <para>  VPMOVSDB xmm1/m64 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector256<int> value) => ConvertToVector128SByteWithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtsepi64_epi8 (__m256i a)</para>
            ///   <para>  VPMOVSQB xmm1/m32 {k1}{z}, zmm2</para>
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector256<long> value) => ConvertToVector128SByteWithSaturation(value);

            /// <summary>
            ///   <para>__m128 _mm_cvtepu32_ps (__m128i a)</para>
            ///   <para>  VCVTUDQ2PS xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
            /// </summary>
            public static Vector128<float> ConvertToVector128Single(Vector128<uint> value) => ConvertToVector128Single(value);

            /// <summary>
            ///   <para>__m128i _mm_cvtepi32_epi16 (__m128i a)</para>
            ///   <para>  VPMOVDW xmm1/m64 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector128<int> value) => ConvertToVector128UInt16(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtepi64_epi16 (__m128i a)</para>
            ///   <para>  VPMOVQW xmm1/m32 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector128<long> value) => ConvertToVector128UInt16(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtepi32_epi16 (__m128i a)</para>
            ///   <para>  VPMOVDW xmm1/m64 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector128<uint> value) => ConvertToVector128UInt16(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtepi64_epi16 (__m128i a)</para>
            ///   <para>  VPMOVQW xmm1/m32 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector128<ulong> value) => ConvertToVector128UInt16(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi32_epi16 (__m256i a)</para>
            ///   <para>  VPMOVDW xmm1/m128 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector256<int> value) => ConvertToVector128UInt16(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi64_epi16 (__m256i a)</para>
            ///   <para>  VPMOVQW xmm1/m64 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector256<long> value) => ConvertToVector128UInt16(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi32_epi16 (__m256i a)</para>
            ///   <para>  VPMOVDW xmm1/m128 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector256<uint> value) => ConvertToVector128UInt16(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi64_epi16 (__m256i a)</para>
            ///   <para>  VPMOVQW xmm1/m64 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector256<ulong> value) => ConvertToVector128UInt16(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtusepi32_epi16 (__m128i a)</para>
            ///   <para>  VPMOVUSDW xmm1/m64 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector128<uint> value) => ConvertToVector128UInt16WithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtusepi64_epi16 (__m128i a)</para>
            ///   <para>  VPMOVUSQW xmm1/m32 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector128<ulong> value) => ConvertToVector128UInt16WithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtusepi32_epi16 (__m256i a)</para>
            ///   <para>  VPMOVUSDW xmm1/m128 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector256<uint> value) => ConvertToVector128UInt16WithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtusepi64_epi16 (__m256i a)</para>
            ///   <para>  VPMOVUSQW xmm1/m64 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector256<ulong> value) => ConvertToVector128UInt16WithSaturation(value);

            /// <summary>
            ///   <para>__m128i _mm_cvtepi64_epi32 (__m128i a)</para>
            ///   <para>  VPMOVQD xmm1/m128 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32(Vector128<long> value) => ConvertToVector128UInt32(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtepi64_epi32 (__m128i a)</para>
            ///   <para>  VPMOVQD xmm1/m128 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32(Vector128<ulong> value) => ConvertToVector128UInt32(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtps_epu32 (__m128 a)</para>
            ///   <para>  VCVTPS2UDQ xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32(Vector128<float> value) => ConvertToVector128UInt32(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtpd_epu32 (__m128d a)</para>
            ///   <para>  VCVTPD2UDQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32(Vector128<double> value) => ConvertToVector128UInt32(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi64_epi32 (__m256i a)</para>
            ///   <para>  VPMOVQD xmm1/m128 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32(Vector256<long> value) => ConvertToVector128UInt32(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtepi64_epi32 (__m256i a)</para>
            ///   <para>  VPMOVQD xmm1/m128 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32(Vector256<ulong> value) => ConvertToVector128UInt32(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtpd_epu32 (__m256d a)</para>
            ///   <para>  VCVTPD2UDQ xmm1 {k1}{z}, ymm2/m256/m64bcst</para>
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32(Vector256<double> value) => ConvertToVector128UInt32(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtusepi64_epi32 (__m128i a)</para>
            ///   <para>  VPMOVUSQD xmm1/m128 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32WithSaturation(Vector128<ulong> value) => ConvertToVector128UInt32WithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvtusepi64_epi32 (__m256i a)</para>
            ///   <para>  VPMOVUSQD xmm1/m128 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32WithSaturation(Vector256<ulong> value) => ConvertToVector128UInt32WithSaturation(value);
            /// <summary>
            ///   <para>__m128i _mm_cvttps_epu32 (__m128 a)</para>
            ///   <para>  VCVTTPS2UDQ xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32WithTruncation(Vector128<float> value) => ConvertToVector128UInt32WithTruncation(value);
            /// <summary>
            ///   <para>__m128i _mm_cvttpd_epu32 (__m128d a)</para>
            ///   <para>  VCVTTPD2UDQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32WithTruncation(Vector128<double> value) => ConvertToVector128UInt32WithTruncation(value);
            /// <summary>
            ///   <para>__m128i _mm256_cvttpd_epu32 (__m256d a)</para>
            ///   <para>  VCVTTPD2UDQ xmm1 {k1}{z}, ymm2/m256/m64bcst</para>
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32WithTruncation(Vector256<double> value) => ConvertToVector128UInt32WithTruncation(value);

            /// <summary>
            ///   <para>__m256d _mm512_cvtepu32_pd (__m128i a)</para>
            ///   <para>  VCVTUDQ2PD ymm1 {k1}{z}, xmm2/m128/m32bcst</para>
            /// </summary>
            public static Vector256<double> ConvertToVector256Double(Vector128<uint> value) => ConvertToVector256Double(value);
            /// <summary>
            ///   <para>__m256 _mm256_cvtepu32_ps (__m256i a)</para>
            ///   <para>  VCVTUDQ2PS ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
            /// </summary>
            public static Vector256<float> ConvertToVector256Single(Vector256<uint> value) => ConvertToVector256Single(value);
            /// <summary>
            ///   <para>__m256i _mm256_cvtps_epu32 (__m256 a)</para>
            ///   <para>  VCVTPS2UDQ ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
            /// </summary>
            public static Vector256<uint> ConvertToVector256UInt32(Vector256<float> value) => ConvertToVector256UInt32(value);
            /// <summary>
            ///   <para>__m256i _mm256_cvttps_epu32 (__m256 a)</para>
            ///   <para>  VCVTTPS2UDQ ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
            /// </summary>
            public static Vector256<uint> ConvertToVector256UInt32WithTruncation(Vector256<float> value) => ConvertToVector256UInt32WithTruncation(value);

            /// <summary>
            ///   <para>__m128d _mm_mask_expand_pd (__m128d s, __mmask8 k, __m128d a)</para>
            ///   <para>  VEXPANDPD xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<double> Expand(Vector128<double> merge, Vector128<double> mask, Vector128<double> value) => Expand(merge, mask, value);
            /// <summary>
            ///   <para>__m128i _mm_mask_expand_epi32 (__m128i s, __mmask8 k, __m128i a)</para>
            ///   <para>  VPEXPANDD xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<int> Expand(Vector128<int> merge, Vector128<int> mask, Vector128<int> value) => Expand(merge, mask, value);
            /// <summary>
            ///   <para>__m128i _mm_mask_expand_epi64 (__m128i s, __mmask8 k, __m128i a)</para>
            ///   <para>  VPEXPANDQ xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<long> Expand(Vector128<long> merge, Vector128<long> mask, Vector128<long> value) => Expand(merge, mask, value);
            /// <summary>
            ///   <para>__m128 _mm_mask_expand_ps (__m128 s, __mmask8 k, __m128 a)</para>
            ///   <para>  VEXPANDPS xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<float> Expand(Vector128<float> merge, Vector128<float> mask, Vector128<float> value) => Expand(merge, mask, value);
            /// <summary>
            ///   <para>__m128i _mm_mask_expand_epi32 (__m128i s, __mmask8 k, __m128i a)</para>
            ///   <para>  VPEXPANDD xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<uint> Expand(Vector128<uint> merge, Vector128<uint> mask, Vector128<uint> value) => Expand(merge, mask, value);
            /// <summary>
            ///   <para>__m128i _mm_mask_expand_epi64 (__m128i s, __mmask8 k, __m128i a)</para>
            ///   <para>  VPEXPANDQ xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<ulong> Expand(Vector128<ulong> merge, Vector128<ulong> mask, Vector128<ulong> value) => Expand(merge, mask, value);

            /// <summary>
            ///   <para>__m256d _mm256_value_expand_pd (__m256d s, __mmask8 k, __m256d a)</para>
            ///   <para>  VEXPANDPD ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<double> Expand(Vector256<double> merge, Vector256<double> mask, Vector256<double> value) => Expand(merge, mask, value);
            /// <summary>
            ///   <para>__m256i _mm256_value_expand_epi32 (__m256i s, __mmask8 k, __m256i a)</para>
            ///   <para>  VPEXPANDD ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<int> Expand(Vector256<int> merge, Vector256<int> mask, Vector256<int> value) => Expand(merge, mask, value);
            /// <summary>
            ///   <para>__m256i _mm256_value_expand_epi64 (__m256i s, __mmask8 k, __m256i a)</para>
            ///   <para>  VPEXPANDQ ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<long> Expand(Vector256<long> merge, Vector256<long> mask, Vector256<long> value) => Expand(merge, mask, value);
            /// <summary>
            ///   <para>__m256 _mm256_value_expand_ps (__m256 s, __mmask8 k, __m256 a)</para>
            ///   <para>  VEXPANDPS ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<float> Expand(Vector256<float> merge, Vector256<float> mask, Vector256<float> value) => Expand(merge, mask, value);
            /// <summary>
            ///   <para>__m256i _mm256_value_expand_epi32 (__m256i s, __mmask8 k, __m256i a)</para>
            ///   <para>  VPEXPANDD ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<uint> Expand(Vector256<uint> merge, Vector256<uint> mask, Vector256<uint> value) => Expand(merge, mask, value);
            /// <summary>
            ///   <para>__m256i _mm256_value_expand_epi64 (__m256i s, __mmask8 k, __m256i a)</para>
            ///   <para>  VPEXPANDQ ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<ulong> Expand(Vector256<ulong> merge, Vector256<ulong> mask, Vector256<ulong> value) => Expand(merge, mask, value);

            /// <summary>
            ///   <para>__m128d _mm_mask_expandloadu_pd (__m128d s, __mmask8 k, void const * a)</para>
            ///   <para>  VEXPANDPD xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<double> ExpandLoad(double* address, Vector128<double> mask, Vector128<double> merge) => ExpandLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m128i _mm_mask_expandloadu_epi32 (__m128i s, __mmask8 k, void const * a)</para>
            ///   <para>  VPEXPANDD xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<int> ExpandLoad(int* address, Vector128<int> mask, Vector128<int> merge) => ExpandLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m128i _mm_mask_expandloadu_epi64 (__m128i s, __mmask8 k, void const * a)</para>
            ///   <para>  VPEXPANDQ xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<long> ExpandLoad(long* address, Vector128<long> mask, Vector128<long> merge) => ExpandLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m128 _mm_mask_expandloadu_ps (__m128 s, __mmask8 k, void const * a)</para>
            ///   <para>  VEXPANDPS xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<float> ExpandLoad(float* address, Vector128<float> mask, Vector128<float> merge) => ExpandLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m128i _mm_mask_expandloadu_epi32 (__m128i s, __mmask8 k, void const * a)</para>
            ///   <para>  VPEXPANDD xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<uint> ExpandLoad(uint* address, Vector128<uint> mask, Vector128<uint> merge) => ExpandLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m128i _mm_mask_expandloadu_epi64 (__m128i s, __mmask8 k, void const * a)</para>
            ///   <para>  VPEXPANDQ xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<ulong> ExpandLoad(ulong* address, Vector128<ulong> mask, Vector128<ulong> merge) => ExpandLoad(address, mask, merge);

            /// <summary>
            ///   <para>__m256d _mm256_address_expandloadu_pd (__m256d s, __mmask8 k, void const * a)</para>
            ///   <para>  VEXPANDPD ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<double> ExpandLoad(double* address, Vector256<double> mask, Vector256<double> merge) => ExpandLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m256i _mm256_address_expandloadu_epi32 (__m256i s, __mmask8 k, void const * a)</para>
            ///   <para>  VPEXPANDD ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<int> ExpandLoad(int* address, Vector256<int> mask, Vector256<int> merge) => ExpandLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m256i _mm256_address_expandloadu_epi64 (__m256i s, __mmask8 k, void const * a)</para>
            ///   <para>  VPEXPANDQ ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<long> ExpandLoad(long* address, Vector256<long> mask, Vector256<long> merge) => ExpandLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m256 _mm256_address_expandloadu_ps (__m256 s, __mmask8 k, void const * a)</para>
            ///   <para>  VEXPANDPS ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<float> ExpandLoad(float* address, Vector256<float> mask, Vector256<float> merge) => ExpandLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m256i _mm256_address_expandloadu_epi32 (__m256i s, __mmask8 k, void const * a)</para>
            ///   <para>  VPEXPANDD ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<uint> ExpandLoad(uint* address, Vector256<uint> mask, Vector256<uint> merge) => ExpandLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m256i _mm256_address_expandloadu_epi64 (__m256i s, __mmask8 k, void const * a)</para>
            ///   <para>  VPEXPANDQ ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<ulong> ExpandLoad(ulong* address, Vector256<ulong> mask, Vector256<ulong> merge) => ExpandLoad(address, mask, merge);

            /// <summary>
            ///   <para>__m128 _mm_fixupimm_ps(__m128 a, __m128 b, __m128i tbl, int imm);</para>
            ///   <para>  VFIXUPIMMPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
            /// </summary>
            public static Vector128<float> Fixup(Vector128<float> left, Vector128<float> right, Vector128<int> table, [ConstantExpected] byte control) => Fixup(left, right, table, control);
            /// <summary>
            ///   <para>__m128d _mm_fixupimm_pd(__m128d a, __m128d b, __m128i tbl, int imm);</para>
            ///   <para>  VFIXUPIMMPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
            /// </summary>
            public static Vector128<double> Fixup(Vector128<double> left, Vector128<double> right, Vector128<long> table, [ConstantExpected] byte control) => Fixup(left, right, table, control);
            /// <summary>
            ///   <para>__m256 _mm256_fixupimm_ps(__m256 a, __m256 b, __m256i tbl, int imm);</para>
            ///   <para>  VFIXUPIMMPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
            /// </summary>
            public static Vector256<float> Fixup(Vector256<float> left, Vector256<float> right, Vector256<int> table, [ConstantExpected] byte control) => Fixup(left, right, table, control);
            /// <summary>
            ///   <para>__m256d _mm256_fixupimm_pd(__m256d a, __m256d b, __m256i tbl, int imm);</para>
            ///   <para>  VFIXUPIMMPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<double> Fixup(Vector256<double> left, Vector256<double> right, Vector256<long> table, [ConstantExpected] byte control) => Fixup(left, right, table, control);

            /// <summary>
            ///   <para>__m128 _mm_getexp_ps (__m128 a)</para>
            ///   <para>  VGETEXPPS xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
            /// </summary>
            public static Vector128<float> GetExponent(Vector128<float> value) => GetExponent(value);
            /// <summary>
            ///   <para>__m128d _mm_getexp_pd (__m128d a)</para>
            ///   <para>  VGETEXPPD xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
            /// </summary>
            public static Vector128<double> GetExponent(Vector128<double> value) => GetExponent(value);
            /// <summary>
            ///   <para>__m256 _mm256_getexp_ps (__m256 a)</para>
            ///   <para>  VGETEXPPS ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
            /// </summary>
            public static Vector256<float> GetExponent(Vector256<float> value) => GetExponent(value);
            /// <summary>
            ///   <para>__m256d _mm256_getexp_pd (__m256d a)</para>
            ///   <para>  VGETEXPPD ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
            /// </summary>
            public static Vector256<double> GetExponent(Vector256<double> value) => GetExponent(value);

            /// <summary>
            ///   <para>__m128 _mm_getmant_ps (__m128 a)</para>
            ///   <para>  VGETMANTPS xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
            /// </summary>
            public static Vector128<float> GetMantissa(Vector128<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissa(value, control);
            /// <summary>
            ///   <para>__m128d _mm_getmant_pd (__m128d a)</para>
            ///   <para>  VGETMANTPD xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
            /// </summary>
            public static Vector128<double> GetMantissa(Vector128<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissa(value, control);
            /// <summary>
            ///   <para>__m256 _mm256_getmant_ps (__m256 a)</para>
            ///   <para>  VGETMANTPS ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
            /// </summary>
            public static Vector256<float> GetMantissa(Vector256<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissa(value, control);
            /// <summary>
            ///   <para>__m256d _mm256_getmant_pd (__m256d a)</para>
            ///   <para>  VGETMANTPD ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
            /// </summary>
            public static Vector256<double> GetMantissa(Vector256<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissa(value, control);

            /// <summary>
            ///   <para>__m128d _mm_mask_loadu_pd (__m128d s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVUPD xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<double> MaskLoad(double* address, Vector128<double> mask, Vector128<double> merge) => MaskLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m128i _mm_mask_loadu_epi32 (__m128i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU32 xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<int> MaskLoad(int* address, Vector128<int> mask, Vector128<int> merge) => MaskLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m128i _mm_mask_loadu_epi64 (__m128i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU64 xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<long> MaskLoad(long* address, Vector128<long> mask, Vector128<long> merge) => MaskLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m128 _mm_mask_loadu_ps (__m128 s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVUPS xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<float> MaskLoad(float* address, Vector128<float> mask, Vector128<float> merge) => MaskLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m128i _mm_mask_loadu_epi32 (__m128i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU32 xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<uint> MaskLoad(uint* address, Vector128<uint> mask, Vector128<uint> merge) => MaskLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m128i _mm_mask_loadu_epi64 (__m128i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU64 xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<ulong> MaskLoad(ulong* address, Vector128<ulong> mask, Vector128<ulong> merge) => MaskLoad(address, mask, merge);

            /// <summary>
            ///   <para>__m256d _mm256_mask_loadu_pd (__m256d s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVUPD ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<double> MaskLoad(double* address, Vector256<double> mask, Vector256<double> merge) => MaskLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m256i _mm256_mask_loadu_epi32 (__m256i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU32 ymm1 {k1}{z}, m256</para>
            /// </summary>
            public static unsafe Vector256<int> MaskLoad(int* address, Vector256<int> mask, Vector256<int> merge) => MaskLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m256i _mm256_mask_loadu_epi64 (__m256i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU64 ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<long> MaskLoad(long* address, Vector256<long> mask, Vector256<long> merge) => MaskLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m256 _mm256_mask_loadu_ps (__m256 s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVUPS ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<float> MaskLoad(float* address, Vector256<float> mask, Vector256<float> merge) => MaskLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m256i _mm256_mask_loadu_epi32 (__m256i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU32 ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<uint> MaskLoad(uint* address, Vector256<uint> mask, Vector256<uint> merge) => MaskLoad(address, mask, merge);
            /// <summary>
            ///   <para>__m256i _mm256_mask_loadu_epi64 (__m256i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQU64 ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<ulong> MaskLoad(ulong* address, Vector256<ulong> mask, Vector256<ulong> merge) => MaskLoad(address, mask, merge);

            /// <summary>
            ///   <para>__m128d _mm_mask_load_pd (__m128d s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVAPD xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<double> MaskLoadAligned(double* address, Vector128<double> mask, Vector128<double> merge) => MaskLoadAligned(address, mask, merge);
            /// <summary>
            ///   <para>__m128i _mm_mask_load_epi32 (__m128i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQA32 xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<int> MaskLoadAligned(int* address, Vector128<int> mask, Vector128<int> merge) => MaskLoadAligned(address, mask, merge);
            /// <summary>
            ///   <para>__m128i _mm_mask_load_epi64 (__m128i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQA64 xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<long> MaskLoadAligned(long* address, Vector128<long> mask, Vector128<long> merge) => MaskLoadAligned(address, mask, merge);
            /// <summary>
            ///   <para>__m128 _mm_mask_load_ps (__m128 s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVAPS xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<float> MaskLoadAligned(float* address, Vector128<float> mask, Vector128<float> merge) => MaskLoadAligned(address, mask, merge);
            /// <summary>
            ///   <para>__m128i _mm_mask_load_epi32 (__m128i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQA32 xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<uint> MaskLoadAligned(uint* address, Vector128<uint> mask, Vector128<uint> merge) => MaskLoadAligned(address, mask, merge);
            /// <summary>
            ///   <para>__m128i _mm_mask_load_epi64 (__m128i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQA64 xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<ulong> MaskLoadAligned(ulong* address, Vector128<ulong> mask, Vector128<ulong> merge) => MaskLoadAligned(address, mask, merge);

            /// <summary>
            ///   <para>__m256d _mm256_mask_load_pd (__m256d s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVAPD ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<double> MaskLoadAligned(double* address, Vector256<double> mask, Vector256<double> merge) => MaskLoadAligned(address, mask, merge);
            /// <summary>
            ///   <para>__m256i _mm256_mask_load_epi32 (__m256i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQA32 ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<int> MaskLoadAligned(int* address, Vector256<int> mask, Vector256<int> merge) => MaskLoadAligned(address, mask, merge);
            /// <summary>
            ///   <para>__m256i _mm256_mask_load_epi64 (__m256i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQA64 ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<long> MaskLoadAligned(long* address, Vector256<long> mask, Vector256<long> merge) => MaskLoadAligned(address, mask, merge);
            /// <summary>
            ///   <para>__m256 _mm256_mask_load_ps (__m256 s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVAPS ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<float> MaskLoadAligned(float* address, Vector256<float> mask, Vector256<float> merge) => MaskLoadAligned(address, mask, merge);
            /// <summary>
            ///   <para>__m256i _mm256_mask_load_epi32 (__m256i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQA32 ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<uint> MaskLoadAligned(uint* address, Vector256<uint> mask, Vector256<uint> merge) => MaskLoadAligned(address, mask, merge);
            /// <summary>
            ///   <para>__m256i _mm256_mask_load_epi64 (__m256i s, __mmask8 k, void const * mem_addr)</para>
            ///   <para>  VMOVDQA64 ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<ulong> MaskLoadAligned(ulong* address, Vector256<ulong> mask, Vector256<ulong> merge) => MaskLoadAligned(address, mask, merge);

            /// <summary>
            ///   <para>void _mm_mask_storeu_pd (void * mem_addr, __mmask8 k, __m128d a)</para>
            ///   <para>  VMOVUPD m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStore(double* address, Vector128<double> mask, Vector128<double> source) => MaskStore(address, mask, source);
            /// <summary>
            ///   <para>void _mm_mask_storeu_epi32 (void * mem_addr, __mmask8 k, __m128i a)</para>
            ///   <para>  VMOVDQU32 m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStore(int* address, Vector128<int> mask, Vector128<int> source) => MaskStore(address, mask, source);
            /// <summary>
            ///   <para>void _mm_mask_storeu_epi64 (void * mem_addr, __mmask8 k, __m128i a)</para>
            ///   <para>  VMOVDQU64 m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStore(long* address, Vector128<long> mask, Vector128<long> source) => MaskStore(address, mask, source);
            /// <summary>
            ///   <para>void _mm_mask_storeu_ps (void * mem_addr, __mmask8 k, __m128 a)</para>
            ///   <para>  VMOVUPS m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStore(float* address, Vector128<float> mask, Vector128<float> source) => MaskStore(address, mask, source);
            /// <summary>
            ///   <para>void _mm_mask_storeu_epi32 (void * mem_addr, __mmask8 k, __m128i a)</para>
            ///   <para>  VMOVDQU32 m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStore(uint* address, Vector128<uint> mask, Vector128<uint> source) => MaskStore(address, mask, source);
            /// <summary>
            ///   <para>void _mm_mask_storeu_epi64 (void * mem_addr, __mmask8 k, __m128i a)</para>
            ///   <para>  VMOVDQU64 m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStore(ulong* address, Vector128<ulong> mask, Vector128<ulong> source) => MaskStore(address, mask, source);

            /// <summary>
            ///   <para>void _mm256_mask_storeu_pd (void * mem_addr, __mmask8 k, __m256d a)</para>
            ///   <para>  VMOVUPD m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStore(double* address, Vector256<double> mask, Vector256<double> source) => MaskStore(address, mask, source);
            /// <summary>
            ///   <para>void _mm256_mask_storeu_epi32 (void * mem_addr, __mmask8 k, __m256i a)</para>
            ///   <para>  VMOVDQU32 m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStore(int* address, Vector256<int> mask, Vector256<int> source) => MaskStore(address, mask, source);
            /// <summary>
            ///   <para>void _mm256_mask_storeu_epi64 (void * mem_addr, __mmask8 k, __m256i a)</para>
            ///   <para>  VMOVDQU64 m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStore(long* address, Vector256<long> mask, Vector256<long> source) => MaskStore(address, mask, source);
            /// <summary>
            ///   <para>void _mm256_mask_storeu_ps (void * mem_addr, __mmask8 k, __m256 a)</para>
            ///   <para>  VMOVUPS m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStore(float* address, Vector256<float> mask, Vector256<float> source) => MaskStore(address, mask, source);
            /// <summary>
            ///   <para>void _mm256_mask_storeu_epi32 (void * mem_addr, __mmask8 k, __m256i a)</para>
            ///   <para>  VMOVDQU32 m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStore(uint* address, Vector256<uint> mask, Vector256<uint> source) => MaskStore(address, mask, source);
            /// <summary>
            ///   <para>void _mm256_mask_storeu_epi64 (void * mem_addr, __mmask8 k, __m256i a)</para>
            ///   <para>  VMOVDQU64 m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStore(ulong* address, Vector256<ulong> mask, Vector256<ulong> source) => MaskStore(address, mask, source);

            /// <summary>
            ///   <para>void _mm_mask_store_pd (void * mem_addr, __mmask8 k, __m128d a)</para>
            ///   <para>  VMOVAPD m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStoreAligned(double* address, Vector128<double> mask, Vector128<double> source) => MaskStoreAligned(address, mask, source);
            /// <summary>
            ///   <para>void _mm_mask_store_epi32 (void * mem_addr, __mmask8 k, __m128i a)</para>
            ///   <para>  VMOVDQA32 m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStoreAligned(int* address, Vector128<int> mask, Vector128<int> source) => MaskStoreAligned(address, mask, source);
            /// <summary>
            ///   <para>void _mm_mask_store_epi64 (void * mem_addr, __mmask8 k, __m128i a)</para>
            ///   <para>  VMOVDQA32 m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStoreAligned(long* address, Vector128<long> mask, Vector128<long> source) => MaskStoreAligned(address, mask, source);
            /// <summary>
            ///   <para>void _mm_mask_store_ps (void * mem_addr, __mmask8 k, __m128 a)</para>
            ///   <para>  VMOVAPS m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStoreAligned(float* address, Vector128<float> mask, Vector128<float> source) => MaskStoreAligned(address, mask, source);
            /// <summary>
            ///   <para>void _mm_mask_store_epi32 (void * mem_addr, __mmask8 k, __m128i a)</para>
            ///   <para>  VMOVDQA32 m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStoreAligned(uint* address, Vector128<uint> mask, Vector128<uint> source) => MaskStoreAligned(address, mask, source);
            /// <summary>
            ///   <para>void _mm_mask_store_epi64 (void * mem_addr, __mmask8 k, __m128i a)</para>
            ///   <para>  VMOVDQA32 m128 {k1}{z}, xmm1</para>
            /// </summary>
            public static unsafe void MaskStoreAligned(ulong* address, Vector128<ulong> mask, Vector128<ulong> source) => MaskStoreAligned(address, mask, source);

            /// <summary>
            ///   <para>void _mm256_mask_store_pd (void * mem_addr, __mmask8 k, __m256d a)</para>
            ///   <para>  VMOVAPD m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStoreAligned(double* address, Vector256<double> mask, Vector256<double> source) => MaskStoreAligned(address, mask, source);
            /// <summary>
            ///   <para>void _mm256_mask_store_epi32 (void * mem_addr, __mmask8 k, __m256i a)</para>
            ///   <para>  VMOVDQA32 m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStoreAligned(int* address, Vector256<int> mask, Vector256<int> source) => MaskStoreAligned(address, mask, source);
            /// <summary>
            ///   <para>void _mm256_mask_store_epi64 (void * mem_addr, __mmask8 k, __m256i a)</para>
            ///   <para>  VMOVDQA32 m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStoreAligned(long* address, Vector256<long> mask, Vector256<long> source) => MaskStoreAligned(address, mask, source);
            /// <summary>
            ///   <para>void _mm256_mask_store_ps (void * mem_addr, __mmask8 k, __m256 a)</para>
            ///   <para>  VMOVAPS m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStoreAligned(float* address, Vector256<float> mask, Vector256<float> source) => MaskStoreAligned(address, mask, source);
            /// <summary>
            ///   <para>void _mm256_mask_store_epi32 (void * mem_addr, __mmask8 k, __m256i a)</para>
            ///   <para>  VMOVDQA32 m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStoreAligned(uint* address, Vector256<uint> mask, Vector256<uint> source) => MaskStoreAligned(address, mask, source);
            /// <summary>
            ///   <para>void _mm256_mask_store_epi64 (void * mem_addr, __mmask8 k, __m256i a)</para>
            ///   <para>  VMOVDQA32 m256 {k1}{z}, ymm1</para>
            /// </summary>
            public static unsafe void MaskStoreAligned(ulong* address, Vector256<ulong> mask, Vector256<ulong> source) => MaskStoreAligned(address, mask, source);

            /// <summary>
            ///   <para>__m128i _mm_max_epi64 (__m128i a, __m128i b)</para>
            ///   <para>  VPMAXSQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            public static Vector128<long> Max(Vector128<long> left, Vector128<long> right) => Max(left, right);
            /// <summary>
            ///   <para>__m128i _mm_max_epu64 (__m128i a, __m128i b)</para>
            ///   <para>  VPMAXUQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            public static Vector128<ulong> Max(Vector128<ulong> left, Vector128<ulong> right) => Max(left, right);
            /// <summary>
            ///   <para>__m256i _mm256_max_epi64 (__m256i a, __m256i b)</para>
            ///   <para>  VPMAXSQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            public static Vector256<long> Max(Vector256<long> left, Vector256<long> right) => Max(left, right);
            /// <summary>
            ///   <para>__m256i _mm256_max_epu64 (__m256i a, __m256i b)</para>
            ///   <para>  VPMAXUQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            public static Vector256<ulong> Max(Vector256<ulong> left, Vector256<ulong> right) => Max(left, right);

            /// <summary>
            ///   <para>__m128i _mm_min_epi64 (__m128i a, __m128i b)</para>
            ///   <para>  VPMINSQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            public static Vector128<long> Min(Vector128<long> left, Vector128<long> right) => Min(left, right);
            /// <summary>
            ///   <para>__m128i _mm_min_epu64 (__m128i a, __m128i b)</para>
            ///   <para>  VPMINUQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            public static Vector128<ulong> Min(Vector128<ulong> left, Vector128<ulong> right) => Min(left, right);
            /// <summary>
            ///   <para>__m256i _mm256_min_epi64 (__m256i a, __m256i b)</para>
            ///   <para>  VPMINSQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            public static Vector256<long> Min(Vector256<long> left, Vector256<long> right) => Min(left, right);
            /// <summary>
            ///   <para>__m256i _mm256_min_epu64 (__m256i a, __m256i b)</para>
            ///   <para>  VPMINUQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            public static Vector256<ulong> Min(Vector256<ulong> left, Vector256<ulong> right) => Min(left, right);

            /// <summary>
            ///   <para>__m128i _mm_permutex2var_epi64 (__m128i a, __m128i idx, __m128i b)</para>
            ///   <para>  VPERMI2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            ///   <para>  VPERMT2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            public static Vector128<long> PermuteVar2x64x2(Vector128<long> lower, Vector128<long> indices, Vector128<long> upper) => PermuteVar2x64x2(lower, indices, upper);
            /// <summary>
            ///   <para>__m128i _mm_permutex2var_epi64 (__m128i a, __m128i idx, __m128i b)</para>
            ///   <para>  VPERMI2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            ///   <para>  VPERMT2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            public static Vector128<ulong> PermuteVar2x64x2(Vector128<ulong> lower, Vector128<ulong> indices, Vector128<ulong> upper) => PermuteVar2x64x2(lower, indices, upper);
            /// <summary>
            ///   <para>__m128d _mm_permutex2var_pd (__m128d a, __m128i idx, __m128i b)</para>
            ///   <para>  VPERMI2PD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            ///   <para>  VPERMT2PD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            public static Vector128<double> PermuteVar2x64x2(Vector128<double> lower, Vector128<long> indices, Vector128<double> upper) => PermuteVar2x64x2(lower, indices, upper);

            /// <summary>
            ///   <para>__m128i _mm_permutex2var_epi32 (__m128i a, __m128i idx, __m128i b)</para>
            ///   <para>  VPERMI2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
            ///   <para>  VPERMT2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
            /// </summary>
            public static Vector128<int> PermuteVar4x32x2(Vector128<int> lower, Vector128<int> indices, Vector128<int> upper) => PermuteVar4x32x2(lower, indices, upper);
            /// <summary>
            ///   <para>__m128i _mm_permutex2var_epi32 (__m128i a, __m128i idx, __m128i b)</para>
            ///   <para>  VPERMI2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
            ///   <para>  VPERMT2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
            /// </summary>
            public static Vector128<uint> PermuteVar4x32x2(Vector128<uint> lower, Vector128<uint> indices, Vector128<uint> upper) => PermuteVar4x32x2(lower, indices, upper);
            /// <summary>
            ///   <para>__m128 _mm_permutex2var_ps (__m128 a, __m128i idx, __m128i b)</para>
            ///   <para>  VPERMI2PS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
            ///   <para>  VPERMT2PS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
            /// </summary>
            public static Vector128<float> PermuteVar4x32x2(Vector128<float> lower, Vector128<int> indices, Vector128<float> upper) => PermuteVar4x32x2(lower, indices, upper);

            /// <summary>
            ///   <para>__m256i _mm256_permutexvar_epi64 (__m256i idx, __m256i a)</para>
            ///   <para>  VPERMQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<long> PermuteVar4x64(Vector256<long> value, Vector256<long> control) => PermuteVar4x64(value, control);
            /// <summary>
            ///   <para>__m256i _mm256_permutexvar_epi64 (__m256i idx, __m256i a)</para>
            ///   <para>  VPERMQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<ulong> PermuteVar4x64(Vector256<ulong> value, Vector256<ulong> control) => PermuteVar4x64(value, control);
            /// <summary>
            ///   <para>__m256d _mm256_permutexvar_pd (__m256i idx, __m256d a)</para>
            ///   <para>  VPERMPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static Vector256<double> PermuteVar4x64(Vector256<double> value, Vector256<long> control) => PermuteVar4x64(value, control);

            /// <summary>
            ///   <para>__m256i _mm256_permutex2var_epi64 (__m256i a, __m256i idx, __m256i b)</para>
            ///   <para>  VPERMI2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            ///   <para>  VPERMT2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            public static Vector256<long> PermuteVar4x64x2(Vector256<long> lower, Vector256<long> indices, Vector256<long> upper) => PermuteVar4x64x2(lower, indices, upper);
            /// <summary>
            ///   <para>__m256i _mm256_permutex2var_epi64 (__m256i a, __m256i idx, __m256i b)</para>
            ///   <para>  VPERMI2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            ///   <para>  VPERMT2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            public static Vector256<ulong> PermuteVar4x64x2(Vector256<ulong> lower, Vector256<ulong> indices, Vector256<ulong> upper) => PermuteVar4x64x2(lower, indices, upper);
            /// <summary>
            ///   <para>__m256d _mm256_permutex2var_pd (__m256d a, __m256i idx, __m256i b)</para>
            ///   <para>  VPERMI2PD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            ///   <para>  VPERMT2PD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            public static Vector256<double> PermuteVar4x64x2(Vector256<double> lower, Vector256<long> indices, Vector256<double> upper) => PermuteVar4x64x2(lower, indices, upper);

            /// <summary>
            ///   <para>__m256i _mm256_permutex2var_epi32 (__m256i a, __m256i idx, __m256i b)</para>
            ///   <para>  VPERMI2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
            ///   <para>  VPERMT2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
            /// </summary>
            public static Vector256<int> PermuteVar8x32x2(Vector256<int> lower, Vector256<int> indices, Vector256<int> upper) => PermuteVar8x32x2(lower, indices, upper);
            /// <summary>
            ///   <para>__m256i _mm256_permutex2var_epi32 (__m256i a, __m256i idx, __m256i b)</para>
            ///   <para>  VPERMI2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
            ///   <para>  VPERMT2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
            /// </summary>
            public static Vector256<uint> PermuteVar8x32x2(Vector256<uint> lower, Vector256<uint> indices, Vector256<uint> upper) => PermuteVar8x32x2(lower, indices, upper);
            /// <summary>
            ///   <para>__m256 _mm256_permutex2var_ps (__m256 a, __m256i idx, __m256i b)</para>
            ///   <para>  VPERMI2PS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
            ///   <para>  VPERMT2PS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
            /// </summary>
            public static Vector256<float> PermuteVar8x32x2(Vector256<float> lower, Vector256<int> indices, Vector256<float> upper) => PermuteVar8x32x2(lower, indices, upper);

            /// <summary>
            ///   <para>__m128 _mm_rcp14_ps (__m128 a, __m128 b)</para>
            ///   <para>  VRCP14PS xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
            /// </summary>
            public static Vector128<float> Reciprocal14(Vector128<float> value) => Reciprocal14(value);
            /// <summary>
            ///   <para>__m128d _mm_rcp14_pd (__m128d a, __m128d b)</para>
            ///   <para>  VRCP14PD xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
            /// </summary>
            public static Vector128<double> Reciprocal14(Vector128<double> value) => Reciprocal14(value);
            /// <summary>
            ///   <para>__m256 _mm256_rcp14_ps (__m256 a, __m256 b)</para>
            ///   <para>  VRCP14PS ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
            /// </summary>
            public static Vector256<float> Reciprocal14(Vector256<float> value) => Reciprocal14(value);
            /// <summary>
            ///   <para>__m256d _mm256_rcp14_pd (__m256d a, __m256d b)</para>
            ///   <para>  VRCP14PD ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
            /// </summary>
            public static Vector256<double> Reciprocal14(Vector256<double> value) => Reciprocal14(value);

            /// <summary>
            ///   <para>__m128 _mm_rsqrt14_ps (__m128 a, __m128 b)</para>
            ///   <para>  VRSQRT14PS xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
            /// </summary>
            public static Vector128<float> ReciprocalSqrt14(Vector128<float> value) => ReciprocalSqrt14(value);
            /// <summary>
            ///   <para>__m128d _mm_rsqrt14_pd (__m128d a, __m128d b)</para>
            ///   <para>  VRSQRT14PD xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
            /// </summary>
            public static Vector128<double> ReciprocalSqrt14(Vector128<double> value) => ReciprocalSqrt14(value);
            /// <summary>
            ///   <para>__m256 _mm256_rsqrt14_ps (__m256 a, __m256 b)</para>
            ///   <para>  VRSQRT14PS ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
            /// </summary>
            public static Vector256<float> ReciprocalSqrt14(Vector256<float> value) => ReciprocalSqrt14(value);
            /// <summary>
            ///   <para>__m256d _mm256_rsqrt14_pd (__m256d a, __m256d b)</para>
            ///   <para>  VRSQRT14PD ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
            /// </summary>
            public static Vector256<double> ReciprocalSqrt14(Vector256<double> value) => ReciprocalSqrt14(value);

            /// <summary>
            ///   <para>__m128i _mm_rol_epi32 (__m128i a, int imm8)</para>
            ///   <para>  VPROLD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8</para>
            /// </summary>
            public static Vector128<int> RotateLeft(Vector128<int> value, [ConstantExpected] byte count) => RotateLeft(value, count);
            /// <summary>
            ///   <para>__m128i _mm_rol_epi32 (__m128i a, int imm8)</para>
            ///   <para>  VPROLD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8</para>
            /// </summary>
            public static Vector128<uint> RotateLeft(Vector128<uint> value, [ConstantExpected] byte count) => RotateLeft(value, count);
            /// <summary>
            ///   <para>__m128i _mm_rol_epi64 (__m128i a, int imm8)</para>
            ///   <para>  VPROLQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8</para>
            /// </summary>
            public static Vector128<long> RotateLeft(Vector128<long> value, [ConstantExpected] byte count) => RotateLeft(value, count);
            /// <summary>
            ///   <para>__m128i _mm_rol_epi64 (__m128i a, int imm8)</para>
            ///   <para>  VPROLQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8</para>
            /// </summary>
            public static Vector128<ulong> RotateLeft(Vector128<ulong> value, [ConstantExpected] byte count) => RotateLeft(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_rol_epi32 (__m256i a, int imm8)</para>
            ///   <para>  VPROLD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
            /// </summary>
            public static Vector256<int> RotateLeft(Vector256<int> value, [ConstantExpected] byte count) => RotateLeft(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_rol_epi32 (__m256i a, int imm8)</para>
            ///   <para>  VPROLD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
            /// </summary>
            public static Vector256<uint> RotateLeft(Vector256<uint> value, [ConstantExpected] byte count) => RotateLeft(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_rol_epi64 (__m256i a, int imm8)</para>
            ///   <para>  VPROLQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<long> RotateLeft(Vector256<long> value, [ConstantExpected] byte count) => RotateLeft(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_rol_epi64 (__m256i a, int imm8)</para>
            ///   <para>  VPROLQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<ulong> RotateLeft(Vector256<ulong> value, [ConstantExpected] byte count) => RotateLeft(value, count);

            /// <summary>
            ///   <para>__m128i _mm_rolv_epi32 (__m128i a, __m128i b)</para>
            ///   <para>  VPROLDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
            /// </summary>
            public static Vector128<int> RotateLeftVariable(Vector128<int> value, Vector128<uint> count) => RotateLeftVariable(value, count);
            /// <summary>
            ///   <para>__m128i _mm_rolv_epi32 (__m128i a, __m128i b)</para>
            ///   <para>  VPROLDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
            /// </summary>
            public static Vector128<uint> RotateLeftVariable(Vector128<uint> value, Vector128<uint> count) => RotateLeftVariable(value, count);
            /// <summary>
            ///   <para>__m128i _mm_rolv_epi64 (__m128i a, __m128i b)</para>
            ///   <para>  VPROLQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            public static Vector128<long> RotateLeftVariable(Vector128<long> value, Vector128<ulong> count) => RotateLeftVariable(value, count);
            /// <summary>
            ///   <para>__m128i _mm_rolv_epi64 (__m128i a, __m128i b)</para>
            ///   <para>  VPROLQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            public static Vector128<ulong> RotateLeftVariable(Vector128<ulong> value, Vector128<ulong> count) => RotateLeftVariable(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_rolv_epi32 (__m256i a, __m256i b)</para>
            ///   <para>  VPROLDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
            /// </summary>
            public static Vector256<int> RotateLeftVariable(Vector256<int> value, Vector256<uint> count) => RotateLeftVariable(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_rolv_epi32 (__m256i a, __m256i b)</para>
            ///   <para>  VPROLDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
            /// </summary>
            public static Vector256<uint> RotateLeftVariable(Vector256<uint> value, Vector256<uint> count) => RotateLeftVariable(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_rolv_epi64 (__m256i a, __m256i b)</para>
            ///   <para>  VPROLQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            public static Vector256<long> RotateLeftVariable(Vector256<long> value, Vector256<ulong> count) => RotateLeftVariable(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_rolv_epi64 (__m256i a, __m256i b)</para>
            ///   <para>  VPROLQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            public static Vector256<ulong> RotateLeftVariable(Vector256<ulong> value, Vector256<ulong> count) => RotateLeftVariable(value, count);

            /// <summary>
            ///   <para>__m128i _mm_ror_epi32 (__m128i a, int imm8)</para>
            ///   <para>  VPRORD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8</para>
            /// </summary>
            public static Vector128<int> RotateRight(Vector128<int> value, [ConstantExpected] byte count) => RotateRight(value, count);
            /// <summary>
            ///   <para>__m128i _mm_ror_epi32 (__m128i a, int imm8)</para>
            ///   <para>  VPRORD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8</para>
            /// </summary>
            public static Vector128<uint> RotateRight(Vector128<uint> value, [ConstantExpected] byte count) => RotateRight(value, count);
            /// <summary>
            ///   <para>__m128i _mm_ror_epi64 (__m128i a, int imm8)</para>
            ///   <para>  VPRORQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8</para>
            /// </summary>
            public static Vector128<long> RotateRight(Vector128<long> value, [ConstantExpected] byte count) => RotateRight(value, count);
            /// <summary>
            ///   <para>__m128i _mm_ror_epi64 (__m128i a, int imm8)</para>
            ///   <para>  VPRORQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8</para>
            /// </summary>
            public static Vector128<ulong> RotateRight(Vector128<ulong> value, [ConstantExpected] byte count) => RotateRight(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_ror_epi32 (__m256i a, int imm8)</para>
            ///   <para>  VPRORD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
            /// </summary>
            public static Vector256<int> RotateRight(Vector256<int> value, [ConstantExpected] byte count) => RotateRight(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_ror_epi32 (__m256i a, int imm8)</para>
            ///   <para>  VPRORD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
            /// </summary>
            public static Vector256<uint> RotateRight(Vector256<uint> value, [ConstantExpected] byte count) => RotateRight(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_ror_epi64 (__m256i a, int imm8)</para>
            ///   <para>  VPRORQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<long> RotateRight(Vector256<long> value, [ConstantExpected] byte count) => RotateRight(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_ror_epi64 (__m256i a, int imm8)</para>
            ///   <para>  VPRORQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<ulong> RotateRight(Vector256<ulong> value, [ConstantExpected] byte count) => RotateRight(value, count);

            /// <summary>
            ///   <para>__m128i _mm_rorv_epi32 (__m128i a, __m128i b)</para>
            ///   <para>  VPRORDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
            /// </summary>
            public static Vector128<int> RotateRightVariable(Vector128<int> value, Vector128<uint> count) => RotateRightVariable(value, count);
            /// <summary>
            ///   <para>__m128i _mm_rorv_epi32 (__m128i a, __m128i b)</para>
            ///   <para>  VPRORDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
            /// </summary>
            public static Vector128<uint> RotateRightVariable(Vector128<uint> value, Vector128<uint> count) => RotateRightVariable(value, count);
            /// <summary>
            ///   <para>__m128i _mm_rorv_epi64 (__m128i a, __m128i b)</para>
            ///   <para>  VPRORQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            public static Vector128<long> RotateRightVariable(Vector128<long> value, Vector128<ulong> count) => RotateRightVariable(value, count);
            /// <summary>
            ///   <para>__m128i _mm_rorv_epi64 (__m128i a, __m128i b)</para>
            ///   <para>  VPRORQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            public static Vector128<ulong> RotateRightVariable(Vector128<ulong> value, Vector128<ulong> count) => RotateRightVariable(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_rorv_epi32 (__m256i a, __m256i b)</para>
            ///   <para>  VPRORDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
            /// </summary>
            public static Vector256<int> RotateRightVariable(Vector256<int> value, Vector256<uint> count) => RotateRightVariable(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_rorv_epi32 (__m256i a, __m256i b)</para>
            ///   <para>  VPRORDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
            /// </summary>
            public static Vector256<uint> RotateRightVariable(Vector256<uint> value, Vector256<uint> count) => RotateRightVariable(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_rorv_epi64 (__m256i a, __m256i b)</para>
            ///   <para>  VPRORQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            public static Vector256<long> RotateRightVariable(Vector256<long> value, Vector256<ulong> count) => RotateRightVariable(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_rorv_epi64 (__m256i a, __m256i b)</para>
            ///   <para>  VPRORQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            public static Vector256<ulong> RotateRightVariable(Vector256<ulong> value, Vector256<ulong> count) => RotateRightVariable(value, count);

            /// <summary>
            ///   <para>__m128 _mm_roundscale_ps (__m128 a, int imm)</para>
            ///   <para>  VRNDSCALEPS xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8</para>
            /// </summary>
            public static Vector128<float> RoundScale(Vector128<float> value, [ConstantExpected] byte control) => RoundScale(value, control);
            /// <summary>
            ///   <para>__m128d _mm_roundscale_pd (__m128d a, int imm)</para>
            ///   <para>  VRNDSCALEPD xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8</para>
            /// </summary>
            public static Vector128<double> RoundScale(Vector128<double> value, [ConstantExpected] byte control) => RoundScale(value, control);
            /// <summary>
            ///   <para>__m256 _mm256_roundscale_ps (__m256 a, int imm)</para>
            ///   <para>  VRNDSCALEPS ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
            /// </summary>
            public static Vector256<float> RoundScale(Vector256<float> value, [ConstantExpected] byte control) => RoundScale(value, control);
            /// <summary>
            ///   <para>__m256d _mm256_roundscale_pd (__m256d a, int imm)</para>
            ///   <para>  VRNDSCALEPD ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<double> RoundScale(Vector256<double> value, [ConstantExpected] byte control) => RoundScale(value, control);

            /// <summary>
            ///   <para>__m128 _mm_scalef_ps (__m128 a, int imm)</para>
            ///   <para>  VSCALEFPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
            /// </summary>
            public static Vector128<float> Scale(Vector128<float> left, Vector128<float> right) => Scale(left, right);
            /// <summary>
            ///   <para>__m128d _mm_scalef_pd (__m128d a, int imm)</para>
            ///   <para>  VSCALEFPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            public static Vector128<double> Scale(Vector128<double> left, Vector128<double> right) => Scale(left, right);
            /// <summary>
            ///   <para>__m256 _mm256_scalef_ps (__m256 a, int imm)</para>
            ///   <para>  VSCALEFPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
            /// </summary>
            public static Vector256<float> Scale(Vector256<float> left, Vector256<float> right) => Scale(left, right);
            /// <summary>
            ///   <para>__m256d _mm256_scalef_pd (__m256d a, int imm)</para>
            ///   <para>  VSCALEFPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            public static Vector256<double> Scale(Vector256<double> left, Vector256<double> right) => Scale(left, right);

            /// <summary>
            ///   <para>__m128i _mm_sra_epi64 (__m128i a, __m128i count)</para>
            ///   <para>  VPSRAQ xmm1 {k1}{z}, xmm2, xmm3/m128</para>
            /// </summary>
            public static Vector128<long> ShiftRightArithmetic(Vector128<long> value, Vector128<long> count) => ShiftRightArithmetic(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_sra_epi64 (__m256i a, __m128i count)</para>
            ///   <para>  VPSRAQ ymm1 {k1}{z}, ymm2, xmm3/m128</para>
            /// </summary>
            public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, Vector128<long> count) => ShiftRightArithmetic(value, count);

            /// <summary>
            ///   <para>__128i _mm_srai_epi64 (__m128i a, int imm8)</para>
            ///   <para>  VPSRAQ xmm1 {k1}{z}, xmm2, imm8</para>
            /// </summary>
            public static Vector128<long> ShiftRightArithmetic(Vector128<long> value, [ConstantExpected] byte count) => ShiftRightArithmetic(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_srai_epi64 (__m256i a, int imm8)</para>
            ///   <para>  VPSRAQ ymm1 {k1}{z}, ymm2, imm8</para>
            /// </summary>
            public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, [ConstantExpected] byte count) => ShiftRightArithmetic(value, count);

            /// <summary>
            ///   <para>__m128i _mm_srav_epi64 (__m128i a, __m128i count)</para>
            ///   <para>  VPSRAVQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
            /// </summary>
            public static Vector128<long> ShiftRightArithmeticVariable(Vector128<long> value, Vector128<ulong> count) => ShiftRightArithmeticVariable(value, count);
            /// <summary>
            ///   <para>__m256i _mm256_srav_epi64 (__m256i a, __m256i count)</para>
            ///   <para>  VPSRAVQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
            /// </summary>
            public static Vector256<long> ShiftRightArithmeticVariable(Vector256<long> value, Vector256<ulong> count) => ShiftRightArithmeticVariable(value, count);

            /// <summary>
            ///   <para>__m256d _mm256_shuffle_f64x2 (__m256d a, __m256d b, const int imm8)</para>
            ///   <para>  VSHUFF64x2 ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<double> Shuffle2x128(Vector256<double> left, Vector256<double> right, [ConstantExpected] byte control) => Shuffle2x128(left, right, control);
            /// <summary>
            ///   <para>__m256i _mm256_shuffle_i32x4 (__m256i a, __m256i b, const int imm8)</para>
            ///   <para>  VSHUFI32x4 ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
            /// </summary>
            public static Vector256<int> Shuffle2x128(Vector256<int> left, Vector256<int> right, [ConstantExpected] byte control) => Shuffle2x128(left, right, control);
            /// <summary>
            ///   <para>__m256i _mm256_shuffle_i64x2 (__m256i a, __m256i b, const int imm8)</para>
            ///   <para>  VSHUFI64x2 ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<long> Shuffle2x128(Vector256<long> left, Vector256<long> right, [ConstantExpected] byte control) => Shuffle2x128(left, right, control);
            /// <summary>
            ///   <para>__m256 _mm256_shuffle_f32x4 (__m256 a, __m256 b, const int imm8)</para>
            ///   <para>  VSHUFF32x4 ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
            /// </summary>
            public static Vector256<float> Shuffle2x128(Vector256<float> left, Vector256<float> right, [ConstantExpected] byte control) => Shuffle2x128(left, right, control);
            /// <summary>
            ///   <para>__m256i _mm256_shuffle_i32x4 (__m256i a, __m256i b, const int imm8)</para>
            ///   <para>  VSHUFI32x4 ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
            /// </summary>
            public static Vector256<uint> Shuffle2x128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected] byte control) => Shuffle2x128(left, right, control);
            /// <summary>
            ///   <para>__m256i _mm256_shuffle_i64x2 (__m256i a, __m256i b, const int imm8)</para>
            ///   <para>  VSHUFI64x2 ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<ulong> Shuffle2x128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected] byte control) => Shuffle2x128(left, right, control);

            /// <summary>
            ///   <para>__m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, byte imm)</para>
            ///   <para>  VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
            /// </summary>
            public static Vector128<sbyte> TernaryLogic(Vector128<sbyte> a, Vector128<sbyte> b, Vector128<sbyte> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, byte imm)</para>
            ///   <para>  VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
            /// </summary>
            public static Vector128<byte> TernaryLogic(Vector128<byte> a, Vector128<byte> b, Vector128<byte> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, short imm)</para>
            ///   <para>  VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
            /// </summary>
            public static Vector128<short> TernaryLogic(Vector128<short> a, Vector128<short> b, Vector128<short> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, short imm)</para>
            ///   <para>  VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
            /// </summary>
            public static Vector128<ushort> TernaryLogic(Vector128<ushort> a, Vector128<ushort> b, Vector128<ushort> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m128i _mm_ternarylogic_epi32 (__m128i a, __m128i b, __m128i c, int imm)</para>
            ///   <para>  VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
            /// </summary>
            public static Vector128<int> TernaryLogic(Vector128<int> a, Vector128<int> b, Vector128<int> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m128i _mm_ternarylogic_epi32 (__m128i a, __m128i b, __m128i c, int imm)</para>
            ///   <para>  VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
            /// </summary>
            public static Vector128<uint> TernaryLogic(Vector128<uint> a, Vector128<uint> b, Vector128<uint> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m128i _mm_ternarylogic_epi64 (__m128i a, __m128i b, __m128i c, int imm)</para>
            ///   <para>  VPTERNLOGQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
            /// </summary>
            public static Vector128<long> TernaryLogic(Vector128<long> a, Vector128<long> b, Vector128<long> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m128i _mm_ternarylogic_epi64 (__m128i a, __m128i b, __m128i c, int imm)</para>
            ///   <para>  VPTERNLOGQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
            /// </summary>
            public static Vector128<ulong> TernaryLogic(Vector128<ulong> a, Vector128<ulong> b, Vector128<ulong> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m128 _mm_ternarylogic_ps (__m128 a, __m128 b, __m128 c, int imm)</para>
            ///   <para>  VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
            /// </summary>
            public static Vector128<float> TernaryLogic(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m128d _mm_ternarylogic_pd (__m128d a, __m128d b, __m128d c, int imm)</para>
            ///   <para>  VPTERNLOGQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
            /// </summary>
            public static Vector128<double> TernaryLogic(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, byte imm)</para>
            ///   <para>  VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
            /// </summary>
            public static Vector256<sbyte> TernaryLogic(Vector256<sbyte> a, Vector256<sbyte> b, Vector256<sbyte> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, byte imm)</para>
            ///   <para>  VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
            /// </summary>
            public static Vector256<byte> TernaryLogic(Vector256<byte> a, Vector256<byte> b, Vector256<byte> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, short imm)</para>
            ///   <para>  VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
            /// </summary>
            public static Vector256<short> TernaryLogic(Vector256<short> a, Vector256<short> b, Vector256<short> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, short imm)</para>
            ///   <para>  VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
            /// </summary>
            public static Vector256<ushort> TernaryLogic(Vector256<ushort> a, Vector256<ushort> b, Vector256<ushort> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m256i _mm256_ternarylogic_epi32 (__m256i a, __m256i b, __m256i c, int imm)</para>
            ///   <para>  VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
            /// </summary>
            public static Vector256<int> TernaryLogic(Vector256<int> a, Vector256<int> b, Vector256<int> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m256i _mm256_ternarylogic_epi32 (__m256i a, __m256i b, __m256i c, int imm)</para>
            ///   <para>  VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
            /// </summary>
            public static Vector256<uint> TernaryLogic(Vector256<uint> a, Vector256<uint> b, Vector256<uint> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m256i _mm256_ternarylogic_epi64 (__m256i a, __m256i b, __m256i c, int imm)</para>
            ///   <para>  VPTERNLOGQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<long> TernaryLogic(Vector256<long> a, Vector256<long> b, Vector256<long> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m256i _mm256_ternarylogic_epi64 (__m256i a, __m256i b, __m256i c, int imm)</para>
            ///   <para>  VPTERNLOGQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<ulong> TernaryLogic(Vector256<ulong> a, Vector256<ulong> b, Vector256<ulong> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m256 _mm256_ternarylogic_ps (__m256 a, __m256 b, __m256 c, int imm)</para>
            ///   <para>  VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
            /// </summary>
            public static Vector256<float> TernaryLogic(Vector256<float> a, Vector256<float> b, Vector256<float> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            ///   <para>__m256d _mm256_ternarylogic_pd (__m256d a, __m256d b, __m256d c, int imm)</para>
            ///   <para>  VPTERNLOGQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
            ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
            /// </summary>
            public static Vector256<double> TernaryLogic(Vector256<double> a, Vector256<double> b, Vector256<double> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        }

        /// <summary>Provides access to the x86 AVX512F hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : Avx2.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>__m128 _mm_cvt_roundi64_ss (__m128 a, __int64 b, int rounding)</para>
            ///   <para>  VCVTSI2SS xmm1, xmm2, r64 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, long value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertScalarToVector128Single(upper, value, mode);
            /// <summary>
            ///   <para>__m128 _mm_cvtsi64_ss (__m128 a, __int64 b)</para>
            ///   <para>  VCVTUSI2SS xmm1, xmm2, r/m64</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, ulong value) => ConvertScalarToVector128Single(upper, value);
            /// <summary>
            ///   <para>__m128 _mm_cvt_roundu64_ss (__m128 a, unsigned __int64 b, int rounding)</para>
            ///   <para>  VCVTUSI2SS xmm1, xmm2, r64 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, ulong value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertScalarToVector128Single(upper, value, mode);
            /// <summary>
            ///   <para>__m128d _mm_cvt_roundsi64_sd (__m128d a, __int64 b, int rounding)</para>
            ///   <para>  VCVTSI2SD xmm1, xmm2, r64 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, long value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertScalarToVector128Double(upper, value, mode);
            /// <summary>
            ///   <para>__m128d _mm_cvtsi64_sd (__m128d a, __int64 b)</para>
            ///   <para>  VCVTUSI2SD xmm1, xmm2, r/m64</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, ulong value) => ConvertScalarToVector128Double(upper, value);
            /// <summary>
            ///   <para>__m128d _mm_cvt_roundu64_sd (__m128d a, unsigned __int64 b, int rounding)</para>
            ///   <para>  VCVTUSI2SD xmm1, xmm2, r64 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, ulong value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertScalarToVector128Double(upper, value, mode);

            /// <summary>
            ///   <para>__int64 _mm_cvt_roundss_i64 (__m128 a, int rounding)</para>
            ///   <para>  VCVTSS2SI r64, xmm1 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static long ConvertToInt64(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToInt64(value, mode);
            /// <summary>
            ///   <para>__int64 _mm_cvt_roundsd_i64 (__m128d a, int rounding)</para>
            ///   <para>  VCVTSD2SI r64, xmm1 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static long ConvertToInt64(Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToInt64(value, mode);
            /// <summary>
            ///   <para>unsigned __int64 _mm_cvtss_u64 (__m128 a)</para>
            ///   <para>  VCVTSS2USI r64, xmm1/m32{er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<float> value) => ConvertToUInt64(value);
            /// <summary>
            ///   <para>unsigned __int64 _mm_cvt_roundss_u64 (__m128 a, int rounding)</para>
            ///   <para>  VCVTSS2USI r64, xmm1 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToUInt64(value, mode);
            /// <summary>
            ///   <para>unsigned __int64 _mm_cvtsd_u64 (__m128d a)</para>
            ///   <para>  VCVTSD2USI r64, xmm1/m64{er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<double> value) => ConvertToUInt64(value);
            /// <summary>
            ///   <para>unsigned __int64 _mm_cvt_roundsd_u64 (__m128d a, int rounding)</para>
            ///   <para>  VCVTSD2USI r64, xmm1 {er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToUInt64(value, mode);

            /// <summary>
            ///   <para>unsigned __int64 _mm_cvttss_u64 (__m128 a)</para>
            ///   <para>  VCVTTSS2USI r64, xmm1/m32{er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong ConvertToUInt64WithTruncation(Vector128<float> value) => ConvertToUInt64WithTruncation(value);
            /// <summary>
            ///   <para>unsigned __int64 _mm_cvttsd_u64 (__m128d a)</para>
            ///   <para>  VCVTTSD2USI r64, xmm1/m64{er}</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong ConvertToUInt64WithTruncation(Vector128<double> value) => ConvertToUInt64WithTruncation(value);
        }

        /// <summary>
        ///   <para>__m512i _mm512_abs_epi32 (__m512i a)</para>
        ///   <para>  VPABSD zmm1 {k1}{z}, zmm2/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> Abs(Vector512<int> value) => Abs(value);
        /// <summary>
        ///   <para>__m512i _mm512_abs_epi64 (__m512i a)</para>
        ///   <para>  VPABSQ zmm1 {k1}{z}, zmm2/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> Abs(Vector512<long> value) => Abs(value);

        /// <summary>
        ///   <para>__m512i _mm512_add_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPADDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> Add(Vector512<int> left, Vector512<int> right) => Add(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_add_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPADDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> Add(Vector512<uint> left, Vector512<uint> right) => Add(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_add_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPADDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> Add(Vector512<long> left, Vector512<long> right) => Add(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_add_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPADDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> Add(Vector512<ulong> left, Vector512<ulong> right) => Add(left, right);
        /// <summary>
        ///   <para>__m512d _mm512_add_pd (__m512d a, __m512d b)</para>
        ///   <para>  VADDPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{er}</para>
        /// </summary>
        public static Vector512<double> Add(Vector512<double> left, Vector512<double> right) => Add(left, right);
        /// <summary>
        ///   <para>__m512d _mm512_add_round_pd (__m512d a, __m512d b, int rounding)</para>
        ///   <para>  VADDPD zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<double> Add(Vector512<double> left, Vector512<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Add(left, right, mode);
        /// <summary>
        ///   <para>__m512 _mm512_add_ps (__m512 a, __m512 b)</para>
        ///   <para>  VADDPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{er}</para>
        /// </summary>
        public static Vector512<float> Add(Vector512<float> left, Vector512<float> right) => Add(left, right);
        /// <summary>
        ///   <para>__m512 _mm512_add_round_ps (__m512 a, __m512 b, int rounding)</para>
        ///   <para>  VADDPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<float> Add(Vector512<float> left, Vector512<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Add(left, right, mode);
        /// <summary>
        ///   <para>__m128 _mm_add_round_ss (__m128 a, __m128 b, int rounding)</para>
        ///   <para>  VADDSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> AddScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => AddScalar(left, right, mode);
        /// <summary>
        ///   <para>__m128d _mm_add_round_sd (__m128d a, __m128d b, int rounding)</para>
        ///   <para>  VADDSD xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> AddScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => AddScalar(left, right, mode);
        /// <summary>
        ///   <para>__m512i _mm512_alignr_epi32 (__m512i a, __m512i b, const int count)</para>
        ///   <para>  VALIGND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<int> AlignRight32(Vector512<int> left, Vector512<int> right, [ConstantExpected] byte mask) => AlignRight32(left, right, mask);
        /// <summary>
        ///   <para>__m512i _mm512_alignr_epi32 (__m512i a, __m512i b, const int count)</para>
        ///   <para>  VALIGND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<uint> AlignRight32(Vector512<uint> left, Vector512<uint> right, [ConstantExpected] byte mask) => AlignRight32(left, right, mask);

        /// <summary>
        ///   <para>__m512i _mm512_alignr_epi64 (__m512i a, __m512i b, const int count)</para>
        ///   <para>  VALIGNQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<long> AlignRight64(Vector512<long> left, Vector512<long> right, [ConstantExpected] byte mask) => AlignRight64(left, right, mask);
        /// <summary>
        ///   <para>__m512i _mm512_alignr_epi64 (__m512i a, __m512i b, const int count)</para>
        ///   <para>  VALIGNQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<ulong> AlignRight64(Vector512<ulong> left, Vector512<ulong> right, [ConstantExpected] byte mask) => AlignRight64(left, right, mask);

        /// <summary>
        ///   <para>__m512i _mm512_and_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<byte> And(Vector512<byte> left, Vector512<byte> right) => And(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_and_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<sbyte> And(Vector512<sbyte> left, Vector512<sbyte> right) => And(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_and_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<short> And(Vector512<short> left, Vector512<short> right) => And(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_and_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<ushort> And(Vector512<ushort> left, Vector512<ushort> right) => And(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_and_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> And(Vector512<int> left, Vector512<int> right) => And(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_and_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> And(Vector512<uint> left, Vector512<uint> right) => And(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_and_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> And(Vector512<long> left, Vector512<long> right) => And(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_and_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> And(Vector512<ulong> left, Vector512<ulong> right) => And(left, right);

        /// <summary>
        ///   <para>__m512i _mm512_andnot_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<byte> AndNot(Vector512<byte> left, Vector512<byte> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_andnot_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<sbyte> AndNot(Vector512<sbyte> left, Vector512<sbyte> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_andnot_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<short> AndNot(Vector512<short> left, Vector512<short> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_andnot_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<ushort> AndNot(Vector512<ushort> left, Vector512<ushort> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_andnot_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> AndNot(Vector512<int> left, Vector512<int> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_andnot_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> AndNot(Vector512<uint> left, Vector512<uint> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_andnot_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDNQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> AndNot(Vector512<long> left, Vector512<long> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_andnot_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPANDNQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> AndNot(Vector512<ulong> left, Vector512<ulong> right) => AndNot(left, right);

        /// <summary>
        ///   <para>__m512d _mm512_mask_blendv_pd (__m512d a, __m512d b, __mmask8 mask)</para>
        ///   <para>  VBLENDMPD zmm1 {k1}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<double> BlendVariable(Vector512<double> left, Vector512<double> right, Vector512<double> mask) => BlendVariable(left, right, mask);
        /// <summary>
        ///   <para>__m512i _mm512_mask_blendv_epi32 (__m512i a, __m512i b, __mmask16 mask)</para>
        ///   <para>  VPBLENDMD zmm1 {k1}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> BlendVariable(Vector512<int> left, Vector512<int> right, Vector512<int> mask) => BlendVariable(left, right, mask);
        /// <summary>
        ///   <para>__m512i _mm512_mask_blendv_epi64 (__m512i a, __m512i b, __mmask8 mask)</para>
        ///   <para>  VPBLENDMQ zmm1 {k1}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> BlendVariable(Vector512<long> left, Vector512<long> right, Vector512<long> mask) => BlendVariable(left, right, mask);
        /// <summary>
        ///   <para>__m512 _mm512_mask_blendv_ps (__m512 a, __m512 b, __mmask16 mask)</para>
        ///   <para>  VBLENDMPS zmm1 {k1}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<float> BlendVariable(Vector512<float> left, Vector512<float> right, Vector512<float> mask) => BlendVariable(left, right, mask);
        /// <summary>
        ///   <para>__m512i _mm512_mask_blendv_epu32 (__m512i a, __m512i b, __mmask16 mask)</para>
        ///   <para>  VPBLENDMD zmm1 {k1}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> BlendVariable(Vector512<uint> left, Vector512<uint> right, Vector512<uint> mask) => BlendVariable(left, right, mask);
        /// <summary>
        ///   <para>__m512i _mm512_mask_blendv_epu64 (__m512i a, __m512i b, __mmask8 mask)</para>
        ///   <para>  VPBLENDMQ zmm1 {k1}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> BlendVariable(Vector512<ulong> left, Vector512<ulong> right, Vector512<ulong> mask) => BlendVariable(left, right, mask);

        /// <summary>
        ///   <para>__m512i _mm512_broadcastd_epi32 (__m128i a)</para>
        ///   <para>  VPBROADCASTD zmm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector512<int> BroadcastScalarToVector512(Vector128<int> value) => BroadcastScalarToVector512(value);
        /// <summary>
        ///   <para>__m512i _mm512_broadcastd_epi32 (__m128i a)</para>
        ///   <para>  VPBROADCASTD zmm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector512<uint> BroadcastScalarToVector512(Vector128<uint> value) => BroadcastScalarToVector512(value);
        /// <summary>
        ///   <para>__m512i _mm512_broadcastq_epi64 (__m128i a)</para>
        ///   <para>  VPBROADCASTQ zmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector512<long> BroadcastScalarToVector512(Vector128<long> value) => BroadcastScalarToVector512(value);
        /// <summary>
        ///   <para>__m512i _mm512_broadcastq_epi64 (__m128i a)</para>
        ///   <para>  VPBROADCASTQ zmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector512<ulong> BroadcastScalarToVector512(Vector128<ulong> value) => BroadcastScalarToVector512(value);
        /// <summary>
        ///   <para>__m512 _mm512_broadcastss_ps (__m128 a)</para>
        ///   <para>  VBROADCASTSS zmm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector512<float> BroadcastScalarToVector512(Vector128<float> value) => BroadcastScalarToVector512(value);
        /// <summary>
        ///   <para>__m512d _mm512_broadcastsd_pd (__m128d a)</para>
        ///   <para>  VBROADCASTSD zmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector512<double> BroadcastScalarToVector512(Vector128<double> value) => BroadcastScalarToVector512(value);

        /// <summary>
        ///   <para>__m512i _mm512_broadcast_i32x4 (__m128i const * mem_addr)</para>
        ///   <para>  VBROADCASTI32x4 zmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector512<int> BroadcastVector128ToVector512(int* address) => BroadcastVector128ToVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_broadcast_i32x4 (__m128i const * mem_addr)</para>
        ///   <para>  VBROADCASTI32x4 zmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector512<uint> BroadcastVector128ToVector512(uint* address) => BroadcastVector128ToVector512(address);
        /// <summary>
        ///   <para>__m512 _mm512_broadcast_f32x4 (__m128 const * mem_addr)</para>
        ///   <para>  VBROADCASTF32x4 zmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector512<float> BroadcastVector128ToVector512(float* address) => BroadcastVector128ToVector512(address);

        /// <summary>
        ///   <para>__m512i _mm512_broadcast_i64x4 (__m256i const * mem_addr)</para>
        ///   <para>  VBROADCASTI64x4 zmm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector512<long> BroadcastVector256ToVector512(long* address) => BroadcastVector256ToVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_broadcast_i64x4 (__m256i const * mem_addr)</para>
        ///   <para>  VBROADCASTI64x4 zmm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector512<ulong> BroadcastVector256ToVector512(ulong* address) => BroadcastVector256ToVector512(address);
        /// <summary>
        ///   <para>__m512d _mm512_broadcast_f64x4 (__m256d const * mem_addr)</para>
        ///   <para>  VBROADCASTF64x4 zmm1 {k1}{z}, m256</para>
        /// </summary>
        public static unsafe Vector512<double> BroadcastVector256ToVector512(double* address) => BroadcastVector256ToVector512(address);

        /// <summary>
        ///   <para>__mmask8 _mm512_cmp_pd_mask (__m512d a, __m512d b, const int imm8)</para>
        ///   <para>  VCMPPD k1 {k2}, zmm2, zmm3/m512/m64bcst{sae}, imm8</para>
        /// </summary>
        public static Vector512<double> Compare(Vector512<double> left, Vector512<double> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) => Compare(left, right, mode);
        /// <summary>
        ///   <para>__mmask8 _mm512_cmpeq_pd_mask (__m512d a,  __m512d b)</para>
        ///   <para>  VCMPPD k1 {k2}, zmm2, zmm3/m512/m64bcst{sae}, imm8(0)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<double> CompareEqual(Vector512<double> left, Vector512<double> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>__mmask8 _mm512_cmpgt_pd_mask (__m512d a,  __m512d b)</para>
        ///   <para>  VCMPPD k1 {k2}, zmm2, zmm3/m512/m64bcst{sae}, imm8(14)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<double> CompareGreaterThan(Vector512<double> left, Vector512<double> right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>__mmask8 _mm512_cmpge_pd_mask (__m512d a,  __m512d b)</para>
        ///   <para>  VCMPPD k1 {k2}, zmm2, zmm3/m512/m64bcst{sae}, imm8(13)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<double> CompareGreaterThanOrEqual(Vector512<double> left, Vector512<double> right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__mmask8 _mm512_cmplt_pd_mask (__m512d a,  __m512d b)</para>
        ///   <para>  VCMPPD k1 {k2}, zmm2, zmm3/m512/m64bcst{sae}, imm8(1)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<double> CompareLessThan(Vector512<double> left, Vector512<double> right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>__mmask8 _mm512_cmple_pd_mask (__m512d a,  __m512d b)</para>
        ///   <para>  VCMPPD k1 {k2}, zmm2, zmm3/m512/m64bcst{sae}, imm8(2)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<double> CompareLessThanOrEqual(Vector512<double> left, Vector512<double> right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>__mmask8 _mm512_cmpneq_pd_mask (__m512d a,  __m512d b)</para>
        ///   <para>  VCMPPD k1 {k2}, zmm2, zmm3/m512/m64bcst{sae}, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<double> CompareNotEqual(Vector512<double> left, Vector512<double> right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>__mmask8 _mm512_cmpngt_pd_mask (__m512d a,  __m512d b)</para>
        ///   <para>  VCMPPD k1 {k2}, zmm2, zmm3/m512/m64bcst{sae}, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<double> CompareNotGreaterThan(Vector512<double> left, Vector512<double> right) => CompareNotGreaterThan(left, right);
        /// <summary>
        ///   <para>__mmask8 _mm512_cmpnge_pd_mask (__m512d a,  __m512d b)</para>
        ///   <para>  VCMPPD k1 {k2}, zmm2, zmm3/m512/m64bcst{sae}, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<double> CompareNotGreaterThanOrEqual(Vector512<double> left, Vector512<double> right) => CompareNotGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__mmask8 _mm512_cmpnlt_pd_mask (__m512d a,  __m512d b)</para>
        ///   <para>  VCMPPD k1 {k2}, zmm2, zmm3/m512/m64bcst{sae}, imm8(5)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<double> CompareNotLessThan(Vector512<double> left, Vector512<double> right) => CompareNotLessThan(left, right);
        /// <summary>
        ///   <para>__mmask8 _mm512_cmpnle_pd_mask (__m512d a,  __m512d b)</para>
        ///   <para>  VCMPPD k1 {k2}, zmm2, zmm3/m512/m64bcst{sae}, imm8(6)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<double> CompareNotLessThanOrEqual(Vector512<double> left, Vector512<double> right) => CompareNotLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>__mmask8 _mm512_cmpord_pd_mask (__m512d a,  __m512d b)</para>
        ///   <para>  VCMPPD k1 {k2}, zmm2, zmm3/m512/m64bcst{sae}, imm8(7)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<double> CompareOrdered(Vector512<double> left, Vector512<double> right) => CompareOrdered(left, right);
        /// <summary>
        ///   <para>__mmask8 _mm512_cmpunord_pd_mask (__m512d a,  __m512d b)</para>
        ///   <para>  VCMPPD k1 {k2}, zmm2, zmm3/m512/m64bcst{sae}, imm8(3)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<double> CompareUnordered(Vector512<double> left, Vector512<double> right) => CompareUnordered(left, right);

        /// <summary>
        ///   <para>__mmask16 _mm512_cmp_ps_mask (__m512 a, __m512 b, const int imm8)</para>
        ///   <para>  VCMPPS k1 {k2}, zmm2, zmm3/m512/m32bcst{sae}, imm8</para>
        /// </summary>
        public static Vector512<float> Compare(Vector512<float> left, Vector512<float> right, [ConstantExpected(Max = FloatComparisonMode.UnorderedTrueSignaling)] FloatComparisonMode mode) => Compare(left, right, mode);
        /// <summary>
        ///   <para>__mmask16 _mm512_cmpeq_ps_mask (__m512 a,  __m512 b)</para>
        ///   <para>  VCMPPS k1 {k2}, zmm2, zmm3/m512/m32bcst{sae}, imm8(0)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<float> CompareEqual(Vector512<float> left, Vector512<float> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>__mmask16 _mm512_cmpgt_ps_mask (__m512 a,  __m512 b)</para>
        ///   <para>  VCMPPS k1 {k2}, zmm2, zmm3/m512/m32bcst{sae}, imm8(14)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<float> CompareGreaterThan(Vector512<float> left, Vector512<float> right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>__mmask16 _mm512_cmpge_ps_mask (__m512 a,  __m512 b)</para>
        ///   <para>  VCMPPS k1 {k2}, zmm2, zmm3/m512/m32bcst{sae}, imm8(13)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<float> CompareGreaterThanOrEqual(Vector512<float> left, Vector512<float> right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__mmask16 _mm512_cmplt_ps_mask (__m512 a,  __m512 b)</para>
        ///   <para>  VCMPPS k1 {k2}, zmm2, zmm3/m512/m32bcst{sae}, imm8(1)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<float> CompareLessThan(Vector512<float> left, Vector512<float> right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>__mmask16 _mm512_cmple_ps_mask (__m512 a,  __m512 b)</para>
        ///   <para>  VCMPPS k1 {k2}, zmm2, zmm3/m512/m32bcst{sae}, imm8(2)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<float> CompareLessThanOrEqual(Vector512<float> left, Vector512<float> right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>__mmask16 _mm512_cmpneq_ps_mask (__m512 a,  __m512 b)</para>
        ///   <para>  VCMPPS k1 {k2}, zmm2, zmm3/m512/m32bcst{sae}, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<float> CompareNotEqual(Vector512<float> left, Vector512<float> right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>__mmask16 _mm512_cmpngt_ps_mask (__m512 a,  __m512 b)</para>
        ///   <para>  VCMPPS k1 {k2}, zmm2, zmm3/m512/m32bcst{sae}, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<float> CompareNotGreaterThan(Vector512<float> left, Vector512<float> right) => CompareNotGreaterThan(left, right);
        /// <summary>
        ///   <para>__mmask16 _mm512_cmpnge_ps_mask (__m512 a,  __m512 b)</para>
        ///   <para>  VCMPPS k1 {k2}, zmm2, zmm3/m512/m32bcst{sae}, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<float> CompareNotGreaterThanOrEqual(Vector512<float> left, Vector512<float> right) => CompareNotGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__mmask16 _mm512_cmpnlt_ps_mask (__m512 a,  __m512 b)</para>
        ///   <para>  VCMPPS k1 {k2}, zmm2, zmm3/m512/m32bcst{sae}, imm8(5)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<float> CompareNotLessThan(Vector512<float> left, Vector512<float> right) => CompareNotLessThan(left, right);
        /// <summary>
        ///   <para>__mmask16 _mm512_cmpnle_ps_mask (__m512 a,  __m512 b)</para>
        ///   <para>  VCMPPS k1 {k2}, zmm2, zmm3/m512/m32bcst{sae}, imm8(6)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<float> CompareNotLessThanOrEqual(Vector512<float> left, Vector512<float> right) => CompareNotLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>__mmask16 _mm512_cmpord_ps_mask (__m512 a,  __m512 b)</para>
        ///   <para>  VCMPPS k1 {k2}, zmm2, zmm3/m512/m32bcst{sae}, imm8(7)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<float> CompareOrdered(Vector512<float> left, Vector512<float> right) => CompareOrdered(left, right);
        /// <summary>
        ///   <para>__mmask16 _mm512_cmpunord_ps_mask (__m512 a,  __m512 b)</para>
        ///   <para>  VCMPPS k1 {k2}, zmm2, zmm3/m512/m32bcst{sae}, imm8(3)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static Vector512<float> CompareUnordered(Vector512<float> left, Vector512<float> right) => CompareUnordered(left, right);

        /// <summary>
        ///   <para>__m512i _mm512_cmpeq_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPEQD k1 {k2}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> CompareEqual(Vector512<int> left, Vector512<int> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmpgt_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPGTD k1 {k2}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> CompareGreaterThan(Vector512<int> left, Vector512<int> right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmpge_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPD k1 {k2}, zmm2, zmm3/m512/m32bcst, imm8(5)</para>
        /// </summary>
        public static Vector512<int> CompareGreaterThanOrEqual(Vector512<int> left, Vector512<int> right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmplt_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPD k1 {k2}, zmm2, zmm3/m512/m32bcst, imm8(1)</para>
        /// </summary>
        public static Vector512<int> CompareLessThan(Vector512<int> left, Vector512<int> right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmple_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPD k1 {k2}, zmm2, zmm3/m512/m32bcst, imm8(2)</para>
        /// </summary>
        public static Vector512<int> CompareLessThanOrEqual(Vector512<int> left, Vector512<int> right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmpne_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPD k1 {k2}, zmm2, zmm3/m512/m32bcst, imm8(4)</para>
        /// </summary>
        public static Vector512<int> CompareNotEqual(Vector512<int> left, Vector512<int> right) => CompareNotEqual(left, right);

        /// <summary>
        ///   <para>__m512i _mm512_cmpeq_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPEQQ k1 {k2}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> CompareEqual(Vector512<long> left, Vector512<long> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmpgt_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPGTQ k1 {k2}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> CompareGreaterThan(Vector512<long> left, Vector512<long> right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmpge_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, zmm2, zmm3/m512/m64bcst, imm8(5)</para>
        /// </summary>
        public static Vector512<long> CompareGreaterThanOrEqual(Vector512<long> left, Vector512<long> right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmplt_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, zmm2, zmm3/m512/m64bcst, imm8(1)</para>
        /// </summary>
        public static Vector512<long> CompareLessThan(Vector512<long> left, Vector512<long> right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmple_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, zmm2, zmm3/m512/m64bcst, imm8(2)</para>
        /// </summary>
        public static Vector512<long> CompareLessThanOrEqual(Vector512<long> left, Vector512<long> right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmpne_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPQ k1 {k2}, zmm2, zmm3/m512/m64bcst, imm8(4)</para>
        /// </summary>
        public static Vector512<long> CompareNotEqual(Vector512<long> left, Vector512<long> right) => CompareNotEqual(left, right);

        /// <summary>
        ///   <para>__m512i _mm512_cmpeq_epu32 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPEQD k1 {k2}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> CompareEqual(Vector512<uint> left, Vector512<uint> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmpgt_epu32 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, zmm2, zmm3/m512/m32bcst, imm8(6)</para>
        /// </summary>
        public static Vector512<uint> CompareGreaterThan(Vector512<uint> left, Vector512<uint> right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmpge_epu32 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, zmm2, zmm3/m512/m32bcst, imm8(5)</para>
        /// </summary>
        public static Vector512<uint> CompareGreaterThanOrEqual(Vector512<uint> left, Vector512<uint> right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmplt_epu32 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, zmm2, zmm3/m512/m32bcst, imm8(1)</para>
        /// </summary>
        public static Vector512<uint> CompareLessThan(Vector512<uint> left, Vector512<uint> right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmple_epu32 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, zmm2, zmm3/m512/m32bcst, imm8(2)</para>
        /// </summary>
        public static Vector512<uint> CompareLessThanOrEqual(Vector512<uint> left, Vector512<uint> right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmpne_epu32 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUD k1 {k2}, zmm2, zmm3/m512/m32bcst, imm8(4)</para>
        /// </summary>
        public static Vector512<uint> CompareNotEqual(Vector512<uint> left, Vector512<uint> right) => CompareNotEqual(left, right);

        /// <summary>
        ///   <para>__m512i _mm512_cmpeq_epu64 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPEQQ k1 {k2}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> CompareEqual(Vector512<ulong> left, Vector512<ulong> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmpgt_epu64 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, zmm2, zmm3/m512/m64bcst, imm8(6)</para>
        /// </summary>
        public static Vector512<ulong> CompareGreaterThan(Vector512<ulong> left, Vector512<ulong> right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmpge_epu64 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, zmm2, zmm3/m512/m64bcst, imm8(5)</para>
        /// </summary>
        public static Vector512<ulong> CompareGreaterThanOrEqual(Vector512<ulong> left, Vector512<ulong> right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmplt_epu64 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, zmm2, zmm3/m512/m64bcst, imm8(1)</para>
        /// </summary>
        public static Vector512<ulong> CompareLessThan(Vector512<ulong> left, Vector512<ulong> right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmple_epu64 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, zmm2, zmm3/m512/m64bcst, imm8(2)</para>
        /// </summary>
        public static Vector512<ulong> CompareLessThanOrEqual(Vector512<ulong> left, Vector512<ulong> right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_cmpne_epu64 (__m512i a, __m512i b)</para>
        ///   <para>  VPCMPUQ k1 {k2}, zmm2, zmm3/m512/m64bcst, imm8(4)</para>
        /// </summary>
        public static Vector512<ulong> CompareNotEqual(Vector512<ulong> left, Vector512<ulong> right) => CompareNotEqual(left, right);

        /// <summary>
        ///   <para>__m512d _mm512_mask_compress_pd (__m512d s, __mmask8 k, __m512d a)</para>
        ///   <para>  VCOMPRESSPD zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<double> Compress(Vector512<double> merge, Vector512<double> mask, Vector512<double> value) => Compress(merge, mask, value);
        /// <summary>
        ///   <para>__m512i _mm512_mask_compress_epi32 (__m512i s, __mmask16 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSD zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<int> Compress(Vector512<int> merge, Vector512<int> mask, Vector512<int> value) => Compress(merge, mask, value);
        /// <summary>
        ///   <para>__m512i _mm512_mask_compress_epi64 (__m512i s, __mmask8 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSQ zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<long> Compress(Vector512<long> merge, Vector512<long> mask, Vector512<long> value) => Compress(merge, mask, value);
        /// <summary>
        ///   <para>__m512 _mm512_mask_compress_ps (__m512 s, __mmask16 k, __m512 a)</para>
        ///   <para>  VCOMPRESSPS zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<float> Compress(Vector512<float> merge, Vector512<float> mask, Vector512<float> value) => Compress(merge, mask, value);
        /// <summary>
        ///   <para>__m512i _mm512_mask_compress_epi32 (__m512i s, __mmask16 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSD zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<uint> Compress(Vector512<uint> merge, Vector512<uint> mask, Vector512<uint> value) => Compress(merge, mask, value);
        /// <summary>
        ///   <para>__m512i _mm512_mask_compress_epi64 (__m512i s, __mmask8 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSQ zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<ulong> Compress(Vector512<ulong> merge, Vector512<ulong> mask, Vector512<ulong> value) => Compress(merge, mask, value);

        /// <summary>
        ///   <para>__m512d _mm512_mask_compressstoreu_pd (void * s, __mmask8 k, __m512d a)</para>
        ///   <para>  VCOMPRESSPD m512 {k1}{z}, zmm2</para>
        /// </summary>
        public static unsafe void CompressStore(double* address, Vector512<double> mask, Vector512<double> source) => CompressStore(address, mask, source);
        /// <summary>
        ///   <para>void _mm512_mask_compressstoreu_epi32 (void * s, __mmask16 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSD m512 {k1}{z}, zmm2</para>
        /// </summary>
        public static unsafe void CompressStore(int* address, Vector512<int> mask, Vector512<int> source) => CompressStore(address, mask, source);
        /// <summary>
        ///   <para>void _mm512_mask_compressstoreu_epi64 (void * s, __mmask8 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSQ m512 {k1}{z}, zmm2</para>
        /// </summary>
        public static unsafe void CompressStore(long* address, Vector512<long> mask, Vector512<long> source) => CompressStore(address, mask, source);
        /// <summary>
        ///   <para>__m512 _mm512_mask_compressstoreu_ps (void * s, __mmask16 k, __m512 a)</para>
        ///   <para>  VCOMPRESSPS m512 {k1}{z}, zmm2</para>
        /// </summary>
        public static unsafe void CompressStore(float* address, Vector512<float> mask, Vector512<float> source) => CompressStore(address, mask, source);
        /// <summary>
        ///   <para>void _mm512_mask_compressstoreu_epi32 (void * s, __mmask16 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSD m512 {k1}{z}, zmm2</para>
        /// </summary>
        public static unsafe void CompressStore(uint* address, Vector512<uint> mask, Vector512<uint> source) => CompressStore(address, mask, source);
        /// <summary>
        ///   <para>void _mm512_mask_compressstoreu_epi64 (void * s, __mmask8 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSQ m512 {k1}{z}, zmm2</para>
        /// </summary>
        public static unsafe void CompressStore(ulong* address, Vector512<ulong> mask, Vector512<ulong> source) => CompressStore(address, mask, source);

        /// <summary>
        ///   <para>__m128 _mm_cvtsi32_ss (__m128 a, int b)</para>
        ///   <para>  VCVTUSI2SS xmm1, xmm2, r/m32</para>
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, uint value) => ConvertScalarToVector128Single(upper, value);
        /// <summary>
        ///   <para>__m128 _mm_cvt_roundi32_ss (__m128 a, int b, int rounding)</para>
        ///   <para>VCVTUSI2SS xmm1, xmm2, r32 {er}</para>
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, uint value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertScalarToVector128Single(upper, value, mode);
        /// <summary>
        ///   <para>__m128 _mm_cvt_roundi32_ss (__m128 a, int b, int rounding)</para>
        ///   <para>VCVTSI2SS xmm1, xmm2, r32 {er}</para>
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, int value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertScalarToVector128Single(upper, value, mode);
        /// <summary>
        ///   <para>__m128 _mm_cvt_roundsd_ss (__m128 a, __m128d b, int rounding)</para>
        ///   <para>VCVTSD2SS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertScalarToVector128Single(upper, value, mode);
        /// <summary>
        ///   <para>__m128d _mm_cvtsi32_sd (__m128d a, int b)</para>
        ///   <para>  VCVTUSI2SD xmm1, xmm2, r/m32</para>
        /// </summary>
        public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, uint value) => ConvertScalarToVector128Double(upper, value);

        /// <summary>
        ///   <para>int _mm_cvt_roundss_i32 (__m128 a, int rounding)</para>
        ///   <para>  VCVTSS2SIK r32, xmm1 {er}</para>
        /// </summary>
        public static int ConvertToInt32(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToInt32(value, mode);
        /// <summary>
        ///   <para>int _mm_cvt_roundsd_i32 (__m128d a, int rounding)</para>
        ///   <para>  VCVTSD2SI r32, xmm1 {er}</para>
        /// </summary>
        public static int ConvertToInt32(Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToInt32(value, mode);
        /// <summary>
        ///   <para>unsigned int _mm_cvtss_u32 (__m128 a)</para>
        ///   <para>  VCVTSS2USI r32, xmm1/m32{er}</para>
        /// </summary>
        public static uint ConvertToUInt32(Vector128<float> value) => ConvertToUInt32(value);
        /// <summary>
        ///   <para>unsigned int _mm_cvt_roundss_u32 (__m128 a, int rounding)</para>
        ///   <para>  VCVTSS2USI r32, xmm1 {er}</para>
        /// </summary>
        public static uint ConvertToUInt32(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToUInt32(value, mode);
        /// <summary>
        ///   <para>unsigned int _mm_cvtsd_u32 (__m128d a)</para>
        ///   <para>  VCVTSD2USI r32, xmm1/m64{er}</para>
        /// </summary>
        public static uint ConvertToUInt32(Vector128<double> value) => ConvertToUInt32(value);
        /// <summary>
        ///   <para>unsigned int _mm_cvt_roundsd_u32 (__m128d a, int rounding)</para>
        ///   <para>  VCVTSD2USI r32, xmm1 {er}</para>
        /// </summary>
        public static uint ConvertToUInt32(Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToUInt32(value, mode);
        /// <summary>
        ///   <para>unsigned int _mm_cvttss_u32 (__m128 a)</para>
        ///   <para>  VCVTTSS2USI r32, xmm1/m32{er}</para>
        /// </summary>
        public static uint ConvertToUInt32WithTruncation(Vector128<float> value) => ConvertToUInt32WithTruncation(value);
        /// <summary>
        ///   <para>unsigned int _mm_cvttsd_u32 (__m128d a)</para>
        ///   <para>  VCVTTSD2USI r32, xmm1/m64{er}</para>
        /// </summary>
        public static uint ConvertToUInt32WithTruncation(Vector128<double> value) => ConvertToUInt32WithTruncation(value);

        /// <summary>
        ///   <para>__m128i _mm512_cvtepi32_epi8 (__m512i a)</para>
        ///   <para>  VPMOVDB xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector512<int> value) => ConvertToVector128Byte(value);
        /// <summary>
        ///   <para>__m128i _mm512_cvtepi64_epi8 (__m512i a)</para>
        ///   <para>  VPMOVQB xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector512<long> value) => ConvertToVector128Byte(value);
        /// <summary>
        ///   <para>__m128i _mm512_cvtepi32_epi8 (__m512i a)</para>
        ///   <para>  VPMOVDB xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector512<uint> value) => ConvertToVector128Byte(value);
        /// <summary>
        ///   <para>__m128i _mm512_cvtepi64_epi8 (__m512i a)</para>
        ///   <para>  VPMOVQB xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector512<ulong> value) => ConvertToVector128Byte(value);
        /// <summary>
        ///   <para>__m128i _mm512_cvtusepi32_epi8 (__m512i a)</para>
        ///   <para>  VPMOVUSDB xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector512<uint> value) => ConvertToVector128ByteWithSaturation(value);
        /// <summary>
        ///   <para>__m128i _mm512_cvtusepi64_epi8 (__m512i a)</para>
        ///   <para>  VPMOVUSQB xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector512<ulong> value) => ConvertToVector128ByteWithSaturation(value);

        /// <summary>
        ///   <para>__m128i _mm512_cvtepi64_epi16 (__m512i a)</para>
        ///   <para>  VPMOVQW xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector512<long> value) => ConvertToVector128Int16(value);
        /// <summary>
        ///   <para>__m128i _mm512_cvtepi64_epi16 (__m512i a)</para>
        ///   <para>  VPMOVQW xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector512<ulong> value) => ConvertToVector128Int16(value);
        /// <summary>
        ///   <para>__m128i _mm512_cvtsepi64_epi16 (__m512i a)</para>
        ///   <para>  VPMOVSQW xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector512<long> value) => ConvertToVector128Int16WithSaturation(value);

        /// <summary>
        ///   <para>__m128i _mm512_cvtepi32_epi8 (__m512i a)</para>
        ///   <para>  VPMOVDB xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector512<int> value) => ConvertToVector128SByte(value);
        /// <summary>
        ///   <para>__m128i _mm512_cvtepi64_epi8 (__m512i a)</para>
        ///   <para>  VPMOVQB xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector512<long> value) => ConvertToVector128SByte(value);
        /// <summary>
        ///   <para>__m128i _mm512_cvtepi32_epi8 (__m512i a)</para>
        ///   <para>  VPMOVDB xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector512<uint> value) => ConvertToVector128SByte(value);
        /// <summary>
        ///   <para>__m128i _mm512_cvtepi64_epi8 (__m512i a)</para>
        ///   <para>  VPMOVQB xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector512<ulong> value) => ConvertToVector128SByte(value);
        /// <summary>
        ///   <para>__m128i _mm512_cvtsepi32_epi8 (__m512i a)</para>
        ///   <para>  VPMOVSDB xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector512<int> value) => ConvertToVector128SByteWithSaturation(value);
        /// <summary>
        ///   <para>__m128i _mm512_cvtsepi64_epi8 (__m512i a)</para>
        ///   <para>  VPMOVSQB xmm1/m64 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector512<long> value) => ConvertToVector128SByteWithSaturation(value);

        /// <summary>
        ///   <para>__m128i _mm512_cvtepi64_epi16 (__m512i a)</para>
        ///   <para>  VPMOVQW xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector512<long> value) => ConvertToVector128UInt16(value);
        /// <summary>
        ///   <para>__m128i _mm512_cvtepi64_epi16 (__m512i a)</para>
        ///   <para>  VPMOVQW xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector512<ulong> value) => ConvertToVector128UInt16(value);
        /// <summary>
        ///   <para>__m128i _mm512_cvtusepi64_epi16 (__m512i a)</para>
        ///   <para>  VPMOVUSQW xmm1/m128 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector512<ulong> value) => ConvertToVector128UInt16WithSaturation(value);

        /// <summary>
        ///   <para>__m256i _mm512_cvtepi32_epi16 (__m512i a)</para>
        ///   <para>  VPMOVDW ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<short> ConvertToVector256Int16(Vector512<int> value) => ConvertToVector256Int16(value);
        /// <summary>
        ///   <para>__m256i _mm512_cvtepi32_epi16 (__m512i a)</para>
        ///   <para>  VPMOVDW ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<short> ConvertToVector256Int16(Vector512<uint> value) => ConvertToVector256Int16(value);
        /// <summary>
        ///   <para>__m256i _mm512_cvtsepi32_epi16 (__m512i a)</para>
        ///   <para>  VPMOVSDW ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<short> ConvertToVector256Int16WithSaturation(Vector512<int> value) => ConvertToVector256Int16WithSaturation(value);

        /// <summary>
        ///   <para>__m256i _mm512_cvtpd_epi32 (__m512d a)</para>
        ///   <para>  VCVTPD2DQ ymm1 {k1}{z}, zmm2/m512/m64bcst{er}</para>
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector512<double> value) => ConvertToVector256Int32(value);
        /// <summary>
        ///   <para>__m256i _mm512_cvt_roundpd_epi32 (__m512d a, int rounding)</para>
        ///   <para>  VCVTPD2DQ ymm1, zmm2 {er}</para>
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector512<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector256Int32(value, mode);
        /// <summary>
        ///   <para>__m256i _mm512_cvtepi64_epi32 (__m512i a)</para>
        ///   <para>  VPMOVQD ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector512<long> value) => ConvertToVector256Int32(value);
        /// <summary>
        ///   <para>__m256i _mm512_cvtepi64_epi32 (__m512i a)</para>
        ///   <para>  VPMOVQD ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector512<ulong> value) => ConvertToVector256Int32(value);
        /// <summary>
        ///   <para>__m256i _mm512_cvtsepi64_epi32 (__m512i a)</para>
        ///   <para>  VPMOVSQD ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32WithSaturation(Vector512<long> value) => ConvertToVector256Int32WithSaturation(value);
        /// <summary>
        ///   <para>__m256i _mm512_cvttpd_epi32 (__m512d a)</para>
        ///   <para>  VCVTTPD2DQ ymm1 {k1}{z}, zmm2/m512/m64bcst{sae}</para>
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32WithTruncation(Vector512<double> value) => ConvertToVector256Int32WithTruncation(value);

        /// <summary>
        ///   <para>__m256 _mm512_cvtpd_ps (__m512d a)</para>
        ///   <para>  VCVTPD2PS ymm1,         zmm2/m512</para>
        ///   <para>  VCVTPD2PS ymm1 {k1}{z}, zmm2/m512/m64bcst{er}</para>
        /// </summary>
        public static Vector256<float> ConvertToVector256Single(Vector512<double> value) => ConvertToVector256Single(value);
        /// <summary>
        ///   <para>__m256 _mm512_cvt_roundpd_ps (__m512d a, int rounding)</para>
        ///   <para>  VCVTPD2PS ymm1, zmm2 {er}</para>
        /// </summary>
        public static Vector256<float> ConvertToVector256Single(Vector512<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector256Single(value, mode);

        /// <summary>
        ///   <para>__m256i _mm512_cvtepi32_epi16 (__m512i a)</para>
        ///   <para>  VPMOVDW ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<ushort> ConvertToVector256UInt16(Vector512<int> value) => ConvertToVector256UInt16(value);
        /// <summary>
        ///   <para>__m256i _mm512_cvtepi32_epi16 (__m512i a)</para>
        ///   <para>  VPMOVDW ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<ushort> ConvertToVector256UInt16(Vector512<uint> value) => ConvertToVector256UInt16(value);
        /// <summary>
        ///   <para>__m256i _mm512_cvtusepi32_epi16 (__m512i a)</para>
        ///   <para>  VPMOVUSDW ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<ushort> ConvertToVector256UInt16WithSaturation(Vector512<uint> value) => ConvertToVector256UInt16WithSaturation(value);

        /// <summary>
        ///   <para>__m256i _mm512_cvtpd_epu32 (__m512d a)</para>
        ///   <para>  VCVTPD2UDQ ymm1 {k1}{z}, zmm2/m512/m64bcst{er}</para>
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32(Vector512<double> value) => ConvertToVector256UInt32(value);
        /// <summary>
        ///__m256i _mm512_cvt_roundpd_epu32 (__m512d a, int rounding)
        ///   <para>  VCVTPD2UDQ ymm1, zmm2 {er}</para>
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32(Vector512<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector256UInt32(value, mode);
        /// <summary>
        ///   <para>__m256i _mm512_cvtepi64_epi32 (__m512i a)</para>
        ///   <para>  VPMOVQD ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32(Vector512<long> value) => ConvertToVector256UInt32(value);
        /// <summary>
        ///   <para>__m256i _mm512_cvtepi64_epi32 (__m512i a)</para>
        ///   <para>  VPMOVQD ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32(Vector512<ulong> value) => ConvertToVector256UInt32(value);
        /// <summary>
        ///   <para>__m256i _mm512_cvtusepi64_epi32 (__m512i a)</para>
        ///   <para>  VPMOVUSQD ymm1/m256 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32WithSaturation(Vector512<ulong> value) => ConvertToVector256UInt32WithSaturation(value);
        /// <summary>
        ///   <para>__m256i _mm512_cvttpd_epu32 (__m512d a)</para>
        ///   <para>  VCVTTPD2UDQ ymm1 {k1}{z}, zmm2/m512/m64bcst{er}</para>
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32WithTruncation(Vector512<double> value) => ConvertToVector256UInt32WithTruncation(value);

        /// <summary>
        ///   <para>__m512d _mm512_cvtepi32_pd (__m256i a)</para>
        ///   <para>  VCVTDQ2PD zmm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector512<double> ConvertToVector512Double(Vector256<int> value) => ConvertToVector512Double(value);
        /// <summary>
        ///   <para>__m512d _mm512_cvtps_pd (__m256 a)</para>
        ///   <para>  VCVTPS2PD zmm1 {k1}{z}, ymm2/m256/m32bcst{sae}</para>
        /// </summary>
        public static Vector512<double> ConvertToVector512Double(Vector256<float> value) => ConvertToVector512Double(value);
        /// <summary>
        ///   <para>__m512d _mm512_cvtepu32_pd (__m256i a)</para>
        ///   <para>  VCVTUDQ2PD zmm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector512<double> ConvertToVector512Double(Vector256<uint> value) => ConvertToVector512Double(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepi8_epi32 (__m128i a)</para>
        ///   <para>  VPMOVSXBD zmm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector128<sbyte> value) => ConvertToVector512Int32(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepu8_epi32 (__m128i a)</para>
        ///   <para>  VPMOVZXBD zmm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector128<byte> value) => ConvertToVector512Int32(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepi16_epi32 (__m128i a)</para>
        ///   <para>  VPMOVSXWD zmm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector256<short> value) => ConvertToVector512Int32(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepu16_epi32 (__m128i a)</para>
        ///   <para>  VPMOVZXWD zmm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector256<ushort> value) => ConvertToVector512Int32(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtps_epi32 (__m512 a)</para>
        ///   <para>  VCVTPS2DQ zmm1 {k1}{z}, zmm2/m512/m32bcst{er}</para>
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector512<float> value) => ConvertToVector512Int32(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvt_roundps_epi32 (__m512 a, int rounding)</para>
        ///   <para>  VCVTPS2DQ zmm1, zmm2 {er}</para>
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector512<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector512Int32(value, mode);
        /// <summary>
        ///   <para>__m512i _mm512_cvttps_epi32 (__m512 a)</para>
        ///   <para>  VCVTTPS2DQ zmm1 {k1}{z}, zmm2/m512/m32bcst{sae}</para>
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32WithTruncation(Vector512<float> value) => ConvertToVector512Int32WithTruncation(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepi8_epi64 (__m128i a)</para>
        ///   <para>  VPMOVSXBQ zmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector128<sbyte> value) => ConvertToVector512Int64(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepu8_epi64 (__m128i a)</para>
        ///   <para>  VPMOVZXBQ zmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector128<byte> value) => ConvertToVector512Int64(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepi16_epi64 (__m128i a)</para>
        ///   <para>  VPMOVSXWQ zmm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector128<short> value) => ConvertToVector512Int64(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepu16_epi64 (__m128i a)</para>
        ///   <para>  VPMOVZXWQ zmm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector128<ushort> value) => ConvertToVector512Int64(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepi32_epi64 (__m128i a)</para>
        ///   <para>  VPMOVSXDQ zmm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector256<int> value) => ConvertToVector512Int64(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepu32_epi64 (__m128i a)</para>
        ///   <para>  VPMOVZXDQ zmm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector256<uint> value) => ConvertToVector512Int64(value);
        /// <summary>
        ///   <para>__m512 _mm512_cvtepi32_ps (__m512i a)</para>
        ///   <para>  VCVTDQ2PS zmm1 {k1}{z}, zmm2/m512/m32bcst{er}</para>
        /// </summary>
        public static Vector512<float> ConvertToVector512Single(Vector512<int> value) => ConvertToVector512Single(value);
        /// <summary>
        ///   <para>__m512 _mm512_cvt_roundepi32_ps (__m512i a, int rounding)</para>
        ///   <para>  VCVTDQ2PS zmm1, zmm2 {er}</para>
        /// </summary>
        public static Vector512<float> ConvertToVector512Single(Vector512<int> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector512Single(value, mode);
        /// <summary>
        ///   <para>__m512 _mm512_cvtepu32_ps (__m512i a)</para>
        ///   <para>  VCVTUDQ2PS zmm1 {k1}{z}, zmm2/m512/m32bcst{er}</para>
        /// </summary>
        public static Vector512<float> ConvertToVector512Single(Vector512<uint> value) => ConvertToVector512Single(value);
        /// <summary>
        ///   <para>__m512 _mm512_cvt_roundepi32_ps (__m512i a, int rounding)</para>
        ///   <para>  VCVTUDQ2PS zmm1, zmm2 {er}</para>
        /// </summary>
        public static Vector512<float> ConvertToVector512Single(Vector512<uint> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector512Single(value, mode);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepi8_epi32 (__m128i a)</para>
        ///   <para>  VPMOVSXBD zmm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector128<sbyte> value) => ConvertToVector512UInt32(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepu8_epi32 (__m128i a)</para>
        ///   <para>  VPMOVZXBD zmm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector128<byte> value) => ConvertToVector512UInt32(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepi16_epi32 (__m128i a)</para>
        ///   <para>  VPMOVSXWD zmm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector256<short> value) => ConvertToVector512UInt32(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepu16_epi32 (__m128i a)</para>
        ///   <para>  VPMOVZXWD zmm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector256<ushort> value) => ConvertToVector512UInt32(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtps_epu32 (__m512 a)</para>
        ///   <para>  VCVTPS2UDQ zmm1 {k1}{z}, zmm2/m512/m32bcst{er}</para>
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector512<float> value) => ConvertToVector512UInt32(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvt_roundps_epu32 (__m512 a, int rounding)</para>
        ///   <para>  VCVTPS2UDQ zmm1, zmm2 {er}</para>
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector512<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ConvertToVector512UInt32(value, mode);
        /// <summary>
        ///   <para>__m512i _mm512_cvttps_epu32 (__m512 a)</para>
        ///   <para>  VCVTTPS2UDQ zmm1 {k1}{z}, zmm2/m512/m32bcst{er}</para>
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32WithTruncation(Vector512<float> value) => ConvertToVector512UInt32WithTruncation(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepi8_epi64 (__m128i a)</para>
        ///   <para>  VPMOVSXBQ zmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector128<sbyte> value) => ConvertToVector512UInt64(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepu8_epi64 (__m128i a)</para>
        ///   <para>  VPMOVZXBQ zmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector128<byte> value) => ConvertToVector512UInt64(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepi16_epi64 (__m128i a)</para>
        ///   <para>  VPMOVSXWQ zmm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector128<short> value) => ConvertToVector512UInt64(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepu16_epi64 (__m128i a)</para>
        ///   <para>  VPMOVZXWQ zmm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector128<ushort> value) => ConvertToVector512UInt64(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepi32_epi64 (__m128i a)</para>
        ///   <para>  VPMOVSXDQ zmm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector256<int> value) => ConvertToVector512UInt64(value);
        /// <summary>
        ///   <para>__m512i _mm512_cvtepu32_epi64 (__m128i a)</para>
        ///   <para>  VPMOVZXDQ zmm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector256<uint> value) => ConvertToVector512UInt64(value);

        /// <summary>
        ///   <para>__m512 _mm512_div_ps (__m512 a, __m512 b)</para>
        ///   <para>  VDIVPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{er}</para>
        /// </summary>
        public static Vector512<float> Divide(Vector512<float> left, Vector512<float> right) => Divide(left, right);
        /// <summary>
        ///   <para>__m512d _mm512_div_pd (__m512d a, __m512d b)</para>
        ///   <para>  VDIVPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{er}</para>
        /// </summary>
        public static Vector512<double> Divide(Vector512<double> left, Vector512<double> right) => Divide(left, right);
        /// <summary>
        ///   <para>__m512 _mm512_div_round_ps (__m512 a, __m512 b, int rounding)</para>
        ///   <para>  VDIVPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<float> Divide(Vector512<float> left, Vector512<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Divide(left, right, mode);
        /// <summary>
        ///   <para>__m512d _mm512_div_round_pd (__m512d a, __m512d b, int rounding)</para>
        ///   <para>  VDIVPD zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<double> Divide(Vector512<double> left, Vector512<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Divide(left, right, mode);
        /// <summary>
        ///   <para>__m128 _mm_div_round_ss (__m128 a, __m128 b, int rounding)</para>
        ///   <para>  VDIVSD xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> DivideScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => DivideScalar(left, right, mode);
        /// <summary>
        ///   <para>__m128d _mm_div_round_sd (__m128d a, __m128d b, int rounding)</para>
        ///   <para>  VDIVSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> DivideScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => DivideScalar(left, right, mode);
        /// <summary>
        ///   <para>__m512 _mm512_moveldup_ps (__m512 a)</para>
        ///   <para>  VMOVSLDUP zmm1 {k1}{z}, zmm2/m512</para>
        /// </summary>
        public static Vector512<float> DuplicateEvenIndexed(Vector512<float> value) => DuplicateEvenIndexed(value);
        /// <summary>
        ///   <para>__m512d _mm512_movedup_pd (__m512d a)</para>
        ///   <para>  VMOVDDUP zmm1 {k1}{z}, zmm2/m512</para>
        /// </summary>
        public static Vector512<double> DuplicateEvenIndexed(Vector512<double> value) => DuplicateEvenIndexed(value);
        /// <summary>
        ///   <para>__m512 _mm512_movehdup_ps (__m512 a)</para>
        ///   <para>  VMOVSHDUP zmm1 {k1}{z}, zmm2/m512</para>
        /// </summary>
        public static Vector512<float> DuplicateOddIndexed(Vector512<float> value) => DuplicateOddIndexed(value);

        /// <summary>
        ///   <para>__m512d _mm512_mask_expand_pd (__m512d s, __mmask8 k, __m512d a)</para>
        ///   <para>  VEXPANDPD zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<double> Expand(Vector512<double> merge, Vector512<double> mask, Vector512<double> value) => Expand(merge, mask, value);
        /// <summary>
        ///   <para>__m512i _mm512_mask_expand_epi32 (__m512i s, __mmask16 k, __m512i a)</para>
        ///   <para>  VPEXPANDD zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<int> Expand(Vector512<int> merge, Vector512<int> mask, Vector512<int> value) => Expand(merge, mask, value);
        /// <summary>
        ///   <para>__m512i _mm512_mask_expand_epi64 (__m512i s, __mmask8 k, __m512i a)</para>
        ///   <para>  VPEXPANDQ zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<long> Expand(Vector512<long> merge, Vector512<long> mask, Vector512<long> value) => Expand(merge, mask, value);
        /// <summary>
        ///   <para>__m512 _mm512_mask_expand_ps (__m512 s, __mmask16 k, __m512 a)</para>
        ///   <para>  VEXPANDPS zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<float> Expand(Vector512<float> merge, Vector512<float> mask, Vector512<float> value) => Expand(merge, mask, value);
        /// <summary>
        ///   <para>__m512i _mm512_mask_expand_epi32 (__m512i s, __mmask16 k, __m512i a)</para>
        ///   <para>  VPEXPANDD zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<uint> Expand(Vector512<uint> merge, Vector512<uint> mask, Vector512<uint> value) => Expand(merge, mask, value);
        /// <summary>
        ///   <para>__m512i _mm512_mask_expand_epi64 (__m512i s, __mmask8 k, __m512i a)</para>
        ///   <para>  VPEXPANDQ zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<ulong> Expand(Vector512<ulong> merge, Vector512<ulong> mask, Vector512<ulong> value) => Expand(merge, mask, value);

        /// <summary>
        ///   <para>__m512d _mm512_mask_expandloadu_pd (__m512d s, __mmask8 k, void * const a)</para>
        ///   <para>  VEXPANDPD zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<double> ExpandLoad(double* address, Vector512<double> mask, Vector512<double> merge) => ExpandLoad(address, mask, merge);
        /// <summary>
        ///   <para>__m512i _mm512_mask_expandloadu_epi32 (__m512i s, __mmask16 k, void * const a)</para>
        ///   <para>  VPEXPANDD zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<int> ExpandLoad(int* address, Vector512<int> mask, Vector512<int> merge) => ExpandLoad(address, mask, merge);
        /// <summary>
        ///   <para>__m512i _mm512_mask_expandloadu_epi64 (__m512i s, __mmask8 k, void * const a)</para>
        ///   <para>  VPEXPANDQ zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<long> ExpandLoad(long* address, Vector512<long> mask, Vector512<long> merge) => ExpandLoad(address, mask, merge);
        /// <summary>
        ///   <para>__m512 _mm512_mask_expandloadu_ps (__m512 s, __mmask16 k, void * const a)</para>
        ///   <para>  VEXPANDPS zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<float> ExpandLoad(float* address, Vector512<float> mask, Vector512<float> merge) => ExpandLoad(address, mask, merge);
        /// <summary>
        ///   <para>__m512i _mm512_mask_expandloadu_epi32 (__m512i s, __mmask16 k, void * const a)</para>
        ///   <para>  VPEXPANDD zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<uint> ExpandLoad(uint* address, Vector512<uint> mask, Vector512<uint> merge) => ExpandLoad(address, mask, merge);
        /// <summary>
        ///   <para>__m512i _mm512_mask_expandloadu_epi64 (__m512i s, __mmask8 k, void * const a)</para>
        ///   <para>  VPEXPANDQ zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<ulong> ExpandLoad(ulong* address, Vector512<ulong> mask, Vector512<ulong> merge) => ExpandLoad(address, mask, merge);

        /// <summary>
        ///   <para>__m128i _mm512_extracti128_si512 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector128<sbyte> ExtractVector128(Vector512<sbyte> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        ///   <para>__m128i _mm512_extracti128_si512 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector128<byte> ExtractVector128(Vector512<byte> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        ///   <para>__m128i _mm512_extracti128_si512 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector128<short> ExtractVector128(Vector512<short> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        ///   <para>__m128i _mm512_extracti128_si512 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector128<ushort> ExtractVector128(Vector512<ushort> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        ///   <para>__m128i _mm512_extracti32x4_epi32 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector128<int> ExtractVector128(Vector512<int> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        ///   <para>__m128i _mm512_extracti32x4_epi32 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector128<uint> ExtractVector128(Vector512<uint> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        ///   <para>__m128i _mm512_extracti128_si512 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector128<long> ExtractVector128(Vector512<long> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        ///   <para>__m128i _mm512_extracti128_si512 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector128<ulong> ExtractVector128(Vector512<ulong> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        ///   <para>__m128 _mm512_extractf32x4_ps (__m512 a, const int imm8)</para>
        ///   <para>  VEXTRACTF32x4 xmm1/m128 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector128<float> ExtractVector128(Vector512<float> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        ///   <para>__m128d _mm512_extractf128_pd (__m512d a, const int imm8)</para>
        ///   <para>  VEXTRACTF32x4 xmm1/m128 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector128<double> ExtractVector128(Vector512<double> value, [ConstantExpected] byte index) => ExtractVector128(value, index);

        /// <summary>
        ///   <para>__m256i _mm512_extracti256_si512 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector256<sbyte> ExtractVector256(Vector512<sbyte> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        ///   <para>__m256i _mm512_extracti256_si512 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector256<byte> ExtractVector256(Vector512<byte> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        ///   <para>__m256i _mm512_extracti256_si512 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector256<short> ExtractVector256(Vector512<short> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        ///   <para>__m256i _mm512_extracti256_si512 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector256<ushort> ExtractVector256(Vector512<ushort> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        ///   <para>__m256i _mm512_extracti256_si512 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector256<int> ExtractVector256(Vector512<int> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        ///   <para>__m256i _mm512_extracti256_si512 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector256<uint> ExtractVector256(Vector512<uint> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        ///   <para>__m256i _mm512_extracti64x4_epi64 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector256<long> ExtractVector256(Vector512<long> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        ///   <para>__m256i _mm512_extracti64x4_epi64 (__m512i a, const int imm8)</para>
        ///   <para>  VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector256<ulong> ExtractVector256(Vector512<ulong> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        ///   <para>__m256 _mm512_extractf256_ps (__m512 a, const int imm8)</para>
        ///   <para>  VEXTRACTF64x4 ymm1/m256 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector256<float> ExtractVector256(Vector512<float> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        ///   <para>__m256d _mm512_extractf64x4_pd (__m512d a, const int imm8)</para>
        ///   <para>  VEXTRACTF64x4 ymm1/m256 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector256<double> ExtractVector256(Vector512<double> value, [ConstantExpected] byte index) => ExtractVector256(value, index);

        /// <summary>
        ///   <para>__m512 _mm512_fixupimm_ps(__m512 a, __m512 b, __m512i tbl, int imm);</para>
        ///   <para>  VFIXUPIMMPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{sae}, imm8</para>
        /// </summary>
        public static Vector512<float> Fixup(Vector512<float> left, Vector512<float> right, Vector512<int> table, [ConstantExpected] byte control) => Fixup(left, right, table, control);
        /// <summary>
        ///   <para>__m512d _mm512_fixupimm_pd(__m512d a, __m512d b, __m512i tbl, int imm);</para>
        ///   <para>  VFIXUPIMMPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{sae}, imm8</para>
        /// </summary>
        public static Vector512<double> Fixup(Vector512<double> left, Vector512<double> right, Vector512<long> table, [ConstantExpected] byte control) => Fixup(left, right, table, control);

        /// <summary>
        ///   <para>__m128 _mm_fixupimm_ss(__m128 a, __m128 b, __m128i tbl, int imm);</para>
        ///   <para>  VFIXUPIMMSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8</para>
        /// </summary>
        public static Vector128<float> FixupScalar(Vector128<float> left, Vector128<float> right, Vector128<int> table, [ConstantExpected] byte control) => FixupScalar(left, right, table, control);
        /// <summary>
        ///   <para>__m128d _mm_fixupimm_sd(__m128d a, __m128d b, __m128i tbl, int imm);</para>
        ///   <para>  VFIXUPIMMSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8</para>
        /// </summary>
        public static Vector128<double> FixupScalar(Vector128<double> left, Vector128<double> right, Vector128<long> table, [ConstantExpected] byte control) => FixupScalar(left, right, table, control);

        /// <summary>
        ///   <para>__m512 _mm512_fmadd_ps (__m512 a, __m512 b, __m512 c)</para>
        ///   <para>  VFMADDPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<float> FusedMultiplyAdd(Vector512<float> a, Vector512<float> b, Vector512<float> c) => FusedMultiplyAdd(a, b, c);
        /// <summary>
        ///   <para>__m512 _mm512_fmadd_round_ps (__m512 a, __m512 b, __m512 c, int r)</para>
        ///   <para>  VFMADDPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<float> FusedMultiplyAdd(Vector512<float> a, Vector512<float> b, Vector512<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplyAdd(a, b, c, mode);
        /// <summary>
        ///   <para>__m512d _mm512_fmadd_pd (__m512d a, __m512d b, __m512d c)</para>
        ///   <para>  VFMADDPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<double> FusedMultiplyAdd(Vector512<double> a, Vector512<double> b, Vector512<double> c) => FusedMultiplyAdd(a, b, c);
        /// <summary>
        ///   <para>__m512d _mm512_fmadd_round_pd (__m512d a, __m512d b, __m512d c, int r)</para>
        ///   <para>  VFMADDPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<double> FusedMultiplyAdd(Vector512<double> a, Vector512<double> b, Vector512<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplyAdd(a, b, c, mode);
        /// <summary>
        ///   <para>__m128 _mm_fmadd_round_ss (__m128 a, __m128 b, __m128 c, int r)</para>
        ///   <para>  VFMADDSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> FusedMultiplyAddScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplyAddScalar(a, b, c, mode);
        /// <summary>
        ///   <para>__m128d _mm_fmadd_round_sd (__m128d a, __m128d b, __m128d c, int r)</para>
        ///   <para>  VFMADDSD xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> FusedMultiplyAddScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplyAddScalar(a, b, c, mode);

        /// <summary>
        ///   <para>__m512 _mm512_fmaddsub_ps (__m512 a, __m512 b, __m512 c)</para>
        ///   <para>  VFMADDSUBPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<float> FusedMultiplyAddSubtract(Vector512<float> a, Vector512<float> b, Vector512<float> c) => FusedMultiplyAddSubtract(a, b, c);
        /// <summary>
        ///   <para>__m512 _mm512_fmaddsub_ps (__m512 a, __m512 b, __m512 c, int c)</para>
        ///   <para>  VFMADDSUBPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<float> FusedMultiplyAddSubtract(Vector512<float> a, Vector512<float> b, Vector512<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplyAddSubtract(a, b, c, mode);
        /// <summary>
        ///   <para>__m512d _mm512_fmaddsub_pd (__m512d a, __m512d b, __m512d c)</para>
        ///   <para>  VFMADDSUBPD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<double> FusedMultiplyAddSubtract(Vector512<double> a, Vector512<double> b, Vector512<double> c) => FusedMultiplyAddSubtract(a, b, c);
        /// <summary>
        ///   <para>__m512d _mm512_fmaddsub_pd (__m512d a, __m512d b, __m512d c, int c)</para>
        ///   <para>  VFMADDSUBPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<double> FusedMultiplyAddSubtract(Vector512<double> a, Vector512<double> b, Vector512<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplyAddSubtract(a, b, c, mode);

        /// <summary>
        ///   <para>__m512 _mm512_fmsub_ps (__m512 a, __m512 b, __m512 c)</para>
        ///   <para>  VFMSUBPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<float> FusedMultiplySubtract(Vector512<float> a, Vector512<float> b, Vector512<float> c) => FusedMultiplySubtract(a, b, c);
        /// <summary>
        ///   <para>__m512 _mm512_fmsub_round_ps (__m512 a, __m512 b, __m512 c, int r)</para>
        ///   <para>  VFMSUBPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<float> FusedMultiplySubtract(Vector512<float> a, Vector512<float> b, Vector512<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplySubtract(a, b, c, mode);
        /// <summary>
        ///   <para>__m512d _mm512_fmsub_pd (__m512d a, __m512d b, __m512d c)</para>
        ///   <para>  VFMSUBPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<double> FusedMultiplySubtract(Vector512<double> a, Vector512<double> b, Vector512<double> c) => FusedMultiplySubtract(a, b, c);
        /// <summary>
        ///   <para>__m512d _mm512_fmsub_round_pd (__m512d a, __m512d b, __m512d c, int r)</para>
        ///   <para>  VFMSUBPD zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<double> FusedMultiplySubtract(Vector512<double> a, Vector512<double> b, Vector512<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplySubtract(a, b, c, mode);
        /// <summary>
        ///   <para>__m128 _mm_fmsub_round_ss (__m128 a, __m128 b, __m128 c, int r)</para>
        ///   <para>  VFMSUBSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> FusedMultiplySubtractScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplySubtractScalar(a, b, c, mode);
        /// <summary>
        ///   <para>__m128d _mm_fmsub_round_sd (__m128d a, __m128d b, __m128d c, int r)</para>
        ///   <para>  VFMSUBSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> FusedMultiplySubtractScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplySubtractScalar(a, b, c, mode);

        /// <summary>
        ///   <para>__m512 _mm512_fmsubadd_ps (__m512 a, __m512 b, __m512 c)</para>
        ///   <para>  VFMSUBADDPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<float> FusedMultiplySubtractAdd(Vector512<float> a, Vector512<float> b, Vector512<float> c) => FusedMultiplySubtractAdd(a, b, c);
        /// <summary>
        ///   <para>__m512 _mm512_fmsubadd_round_ps (__m512 a, __m512 b, __m512 c)</para>
        ///   <para>  VFMSUBADDPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<float> FusedMultiplySubtractAdd(Vector512<float> a, Vector512<float> b, Vector512<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplySubtractAdd(a, b, c, mode);
        /// <summary>
        ///   <para>__m512d _mm512_fmsubadd_pd (__m512d a, __m512d b, __m512d c)</para>
        ///   <para>  VFMSUBADDPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<double> FusedMultiplySubtractAdd(Vector512<double> a, Vector512<double> b, Vector512<double> c) => FusedMultiplySubtractAdd(a, b, c);
        /// <summary>
        ///   <para>__m512d _mm512_fmsubadd_round_ps (__m512d a, __m512d b, __m512d c)</para>
        ///   <para>  VFMSUBADDPD zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<double> FusedMultiplySubtractAdd(Vector512<double> a, Vector512<double> b, Vector512<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplySubtractAdd(a, b, c, mode);

        /// <summary>
        ///   <para>__m512 _mm512_fnmadd_ps (__m512 a, __m512 b, __m512 c)</para>
        ///   <para>  VFNMADDPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<float> FusedMultiplyAddNegated(Vector512<float> a, Vector512<float> b, Vector512<float> c) => FusedMultiplyAddNegated(a, b, c);
        /// <summary>
        ///   <para>__m512 _mm512_fnmadd_round_ps (__m512 a, __m512 b, __m512 c, int r)</para>
        ///   <para>  VFNMADDPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<float> FusedMultiplyAddNegated(Vector512<float> a, Vector512<float> b, Vector512<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplyAddNegated(a, b, c, mode);
        /// <summary>
        ///   <para>__m512d _mm512_fnmadd_pd (__m512d a, __m512d b, __m512d c)</para>
        ///   <para>  VFNMADDPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<double> FusedMultiplyAddNegated(Vector512<double> a, Vector512<double> b, Vector512<double> c) => FusedMultiplyAddNegated(a, b, c);
        /// <summary>
        ///   <para>__m512d _mm512_fnmadd_round_pdd (__m512d a, __m512d b, __m512d c, int r)</para>
        ///   <para>  VFNMADDPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<double> FusedMultiplyAddNegated(Vector512<double> a, Vector512<double> b, Vector512<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplyAddNegated(a, b, c, mode);
        /// <summary>
        ///   <para>__m128 _mm_fnmadd_round_ss (__m128 a, __m128 b, __m128 c, int r)</para>
        ///   <para>  VFNMADDSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> FusedMultiplyAddNegatedScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplyAddNegatedScalar(a, b, c, mode);
        /// <summary>
        ///   <para>__m128d _mm_fnmadd_round_sd (__m128d a, __m128d b, __m128d c, int r)</para>
        ///   <para>  VFNMADDSD xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> FusedMultiplyAddNegatedScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplyAddNegatedScalar(a, b, c, mode);

        /// <summary>
        ///   <para>__m512 _mm512_fnmsub_ps (__m512 a, __m512 b, __m512 c)</para>
        ///   <para>  VFNMSUBPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<float> FusedMultiplySubtractNegated(Vector512<float> a, Vector512<float> b, Vector512<float> c) => FusedMultiplySubtractNegated(a, b, c);
        /// <summary>
        ///   <para>__m512 _mm512_fnmsub_round_ps (__m512 a, __m512 b, __m512 c, int r)</para>
        ///   <para>  VFNMSUBPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<float> FusedMultiplySubtractNegated(Vector512<float> a, Vector512<float> b, Vector512<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplySubtractNegated(a, b, c, mode);
        /// <summary>
        ///   <para>__m512d _mm512_fnmsub_pd (__m512d a, __m512d b, __m512d c)</para>
        ///   <para>  VFNMSUBPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<double> FusedMultiplySubtractNegated(Vector512<double> a, Vector512<double> b, Vector512<double> c) => FusedMultiplySubtractNegated(a, b, c);
        /// <summary>
        ///   <para>__m512d _mm512_fnmsub_round_pd (__m512d a, __m512d b, __m512d c, int r)</para>
        ///   <para>  VFNMSUBPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<double> FusedMultiplySubtractNegated(Vector512<double> a, Vector512<double> b, Vector512<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplySubtractNegated(a, b, c, mode);
        /// <summary>
        ///   <para>__m128 _mm_fnmsub_round_ss (__m128 a, __m128 b, __m128 c, int r)</para>
        ///   <para>  VFNMSUBSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> FusedMultiplySubtractNegatedScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplySubtractNegatedScalar(a, b, c, mode);
        /// <summary>
        ///   <para>__m128d _mm_fnmsub_round_sd (__m128d a, __m128d b, __m128d c, int r)</para>
        ///   <para>  VFNMSUBSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> FusedMultiplySubtractNegatedScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => FusedMultiplySubtractNegatedScalar(a, b, c, mode);

        /// <summary>
        ///   <para>__m512 _mm512_getexp_ps (__m512 a)</para>
        ///   <para>  VGETEXPPS zmm1 {k1}{z}, zmm2/m512/m32bcst{sae}</para>
        /// </summary>
        public static Vector512<float> GetExponent(Vector512<float> value) => GetExponent(value);
        /// <summary>
        ///   <para>__m512d _mm512_getexp_pd (__m512d a)</para>
        ///   <para>  VGETEXPPD zmm1 {k1}{z}, zmm2/m512/m64bcst{sae}</para>
        /// </summary>
        public static Vector512<double> GetExponent(Vector512<double> value) => GetExponent(value);

        /// <summary>
        ///   <para>__m128 _mm_getexp_ss (__m128 a)</para>
        ///   <para>  VGETEXPSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}</para>
        /// </summary>
        public static Vector128<float> GetExponentScalar(Vector128<float> value) => GetExponentScalar(value);
        /// <summary>
        ///   <para>__m128d _mm_getexp_sd (__m128d a)</para>
        ///   <para>  VGETEXPSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}</para>
        /// </summary>
        public static Vector128<double> GetExponentScalar(Vector128<double> value) => GetExponentScalar(value);
        /// <summary>
        ///   <para>__m128 _mm_getexp_ss (__m128 a, __m128 b)</para>
        ///   <para>  VGETEXPSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<float> GetExponentScalar(Vector128<float> upper, Vector128<float> value) => GetExponentScalar(upper, value);
        /// <summary>
        ///   <para>__m128d _mm_getexp_sd (__m128d a, __m128d b)</para>
        ///   <para>  VGETEXPSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<double> GetExponentScalar(Vector128<double> upper, Vector128<double> value) => GetExponentScalar(upper, value);

        /// <summary>
        ///   <para>__m512 _mm512_getmant_ps (__m512 a)</para>
        ///   <para>  VGETMANTPS zmm1 {k1}{z}, zmm2/m512/m32bcst{sae}</para>
        /// </summary>
        public static Vector512<float> GetMantissa(Vector512<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissa(value, control);
        /// <summary>
        ///   <para>__m512d _mm512_getmant_pd (__m512d a)</para>
        ///   <para>  VGETMANTPD zmm1 {k1}{z}, zmm2/m512/m64bcst{sae}</para>
        /// </summary>
        public static Vector512<double> GetMantissa(Vector512<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissa(value, control);

        /// <summary>
        ///   <para>__m128 _mm_getmant_ss (__m128 a)</para>
        ///   <para>  VGETMANTSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}</para>
        /// </summary>
        public static Vector128<float> GetMantissaScalar(Vector128<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissaScalar(value, control);
        /// <summary>
        ///   <para>__m128d _mm_getmant_sd (__m128d a)</para>
        ///   <para>  VGETMANTSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}</para>
        /// </summary>
        public static Vector128<double> GetMantissaScalar(Vector128<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissaScalar(value, control);
        /// <summary>
        ///   <para>__m128 _mm_getmant_ss (__m128 a, __m128 b)</para>
        ///   <para>  VGETMANTSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<float> GetMantissaScalar(Vector128<float> upper, Vector128<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissaScalar(upper, value, control);
        /// <summary>
        ///   <para>__m128d _mm_getmant_sd (__m128d a, __m128d b)</para>
        ///   <para>  VGETMANTSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<double> GetMantissaScalar(Vector128<double> upper, Vector128<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissaScalar(upper, value, control);

        /// <summary>
        ///   <para>__m512i _mm512_inserti128_si512 (__m512i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector512<sbyte> InsertVector128(Vector512<sbyte> value, Vector128<sbyte> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        ///   <para>__m512i _mm512_inserti128_si512 (__m512i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector512<byte> InsertVector128(Vector512<byte> value, Vector128<byte> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        ///   <para>__m512i _mm512_inserti128_si512 (__m512i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector512<short> InsertVector128(Vector512<short> value, Vector128<short> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        ///   <para>__m512i _mm512_inserti128_si512 (__m512i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector512<ushort> InsertVector128(Vector512<ushort> value, Vector128<ushort> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        ///   <para>__m512i _mm512_inserti32x4_epi32 (__m512i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector512<int> InsertVector128(Vector512<int> value, Vector128<int> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        ///   <para>__m512i _mm512_inserti32x4_epi32 (__m512i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector512<uint> InsertVector128(Vector512<uint> value, Vector128<uint> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        ///   <para>__m512i _mm512_inserti128_si512 (__m512i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector512<long> InsertVector128(Vector512<long> value, Vector128<long> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        ///   <para>__m512i _mm512_inserti128_si512 (__m512i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector512<ulong> InsertVector128(Vector512<ulong> value, Vector128<ulong> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        ///   <para>__m512 _mm512_insertf32x4_ps (__m512 a, __m128 b, int imm8)</para>
        ///   <para>  VINSERTF32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector512<float> InsertVector128(Vector512<float> value, Vector128<float> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        ///   <para>__m512d _mm512_insertf128_pd (__m512d a, __m128d b, int imm8)</para>
        ///   <para>  VINSERTF32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector512<double> InsertVector128(Vector512<double> value, Vector128<double> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);

        /// <summary>
        ///   <para>__m512i _mm512_inserti256_si512 (__m512i a, __m256i b, const int imm8)</para>
        ///   <para>  VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8</para>
        /// </summary>
        public static Vector512<sbyte> InsertVector256(Vector512<sbyte> value, Vector256<sbyte> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        ///   <para>__m512i _mm512_inserti256_si512 (__m512i a, __m256i b, const int imm8)</para>
        ///   <para>  VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8</para>
        /// </summary>
        public static Vector512<byte> InsertVector256(Vector512<byte> value, Vector256<byte> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        ///   <para>__m512i _mm512_inserti256_si512 (__m512i a, __m256i b, const int imm8)</para>
        ///   <para>  VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8</para>
        /// </summary>
        public static Vector512<short> InsertVector256(Vector512<short> value, Vector256<short> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        ///   <para>__m512i _mm512_inserti256_si512 (__m512i a, __m256i b, const int imm8)</para>
        ///   <para>  VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8</para>
        /// </summary>
        public static Vector512<ushort> InsertVector256(Vector512<ushort> value, Vector256<ushort> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        ///   <para>__m512i _mm512_inserti256_si512 (__m512i a, __m256i b, const int imm8)</para>
        ///   <para>  VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8</para>
        /// </summary>
        public static Vector512<int> InsertVector256(Vector512<int> value, Vector256<int> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        ///   <para>__m512i _mm512_inserti256_si512 (__m512i a, __m256i b, const int imm8)</para>
        ///   <para>  VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8</para>
        /// </summary>
        public static Vector512<uint> InsertVector256(Vector512<uint> value, Vector256<uint> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        ///   <para>__m512i _mm512_inserti64x4_epi64 (__m512i a, __m256i b, const int imm8)</para>
        ///   <para>  VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8</para>
        /// </summary>
        public static Vector512<long> InsertVector256(Vector512<long> value, Vector256<long> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        ///   <para>__m512i _mm512_inserti64x4_epi64 (__m512i a, __m256i b, const int imm8)</para>
        ///   <para>  VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8</para>
        /// </summary>
        public static Vector512<ulong> InsertVector256(Vector512<ulong> value, Vector256<ulong> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        ///   <para>__m512 _mm512_insertf256_ps (__m512 a, __m256 b, int imm8)</para>
        ///   <para>  VINSERTF64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8</para>
        /// </summary>
        public static Vector512<float> InsertVector256(Vector512<float> value, Vector256<float> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        ///   <para>__m512d _mm512_insertf64x4_pd (__m512d a, __m256d b, int imm8)</para>
        ///   <para>  VINSERTF64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8</para>
        /// </summary>
        public static Vector512<double> InsertVector256(Vector512<double> value, Vector256<double> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);

        /// <summary>
        ///   <para>__m512i _mm512_load_si512 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQA32 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<byte> LoadAlignedVector512(byte* address) => LoadAlignedVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_load_si512 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQA32 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<sbyte> LoadAlignedVector512(sbyte* address) => LoadAlignedVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_load_si512 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQA32 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<short> LoadAlignedVector512(short* address) => LoadAlignedVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_load_si512 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQA32 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<ushort> LoadAlignedVector512(ushort* address) => LoadAlignedVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_load_epi32 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQA32 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<int> LoadAlignedVector512(int* address) => LoadAlignedVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_load_epi32 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQA32 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<uint> LoadAlignedVector512(uint* address) => LoadAlignedVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_load_epi64 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQA64 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<long> LoadAlignedVector512(long* address) => LoadAlignedVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_load_epi64 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQA64 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<ulong> LoadAlignedVector512(ulong* address) => LoadAlignedVector512(address);
        /// <summary>
        ///   <para>__m512 _mm512_load_ps (float const * mem_addr)</para>
        ///   <para>  VMOVAPS zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<float> LoadAlignedVector512(float* address) => LoadAlignedVector512(address);
        /// <summary>
        ///   <para>__m512d _mm512_load_pd (double const * mem_addr)</para>
        ///   <para>  VMOVAPD zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<double> LoadAlignedVector512(double* address) => LoadAlignedVector512(address);

        /// <summary>
        ///   <para>__m512i _mm512_stream_load_si512 (__m512i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<sbyte> LoadAlignedVector512NonTemporal(sbyte* address) => LoadAlignedVector512NonTemporal(address);
        /// <summary>
        ///   <para>__m512i _mm512_stream_load_si512 (__m512i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<byte> LoadAlignedVector512NonTemporal(byte* address) => LoadAlignedVector512NonTemporal(address);
        /// <summary>
        ///   <para>__m512i _mm512_stream_load_si512 (__m512i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<short> LoadAlignedVector512NonTemporal(short* address) => LoadAlignedVector512NonTemporal(address);
        /// <summary>
        ///   <para>__m512i _mm512_stream_load_si512 (__m512i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<ushort> LoadAlignedVector512NonTemporal(ushort* address) => LoadAlignedVector512NonTemporal(address);
        /// <summary>
        ///   <para>__m512i _mm512_stream_load_si512 (__m512i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<int> LoadAlignedVector512NonTemporal(int* address) => LoadAlignedVector512NonTemporal(address);
        /// <summary>
        ///   <para>__m512i _mm512_stream_load_si512 (__m512i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<uint> LoadAlignedVector512NonTemporal(uint* address) => LoadAlignedVector512NonTemporal(address);
        /// <summary>
        ///   <para>__m512i _mm512_stream_load_si512 (__m512i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<long> LoadAlignedVector512NonTemporal(long* address) => LoadAlignedVector512NonTemporal(address);
        /// <summary>
        ///   <para>__m512i _mm512_stream_load_si512 (__m512i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<ulong> LoadAlignedVector512NonTemporal(ulong* address) => LoadAlignedVector512NonTemporal(address);

        /// <summary>
        ///   <para>__m512i _mm512_loadu_si512 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQU32 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<sbyte> LoadVector512(sbyte* address) => LoadVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_loadu_si512 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQU32 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<byte> LoadVector512(byte* address) => LoadVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_loadu_si512 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQU32 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<short> LoadVector512(short* address) => LoadVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_loadu_si512 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQU32 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<ushort> LoadVector512(ushort* address) => LoadVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_loadu_epi32 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQU32 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<int> LoadVector512(int* address) => LoadVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_loadu_epi32 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQU32 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<uint> LoadVector512(uint* address) => LoadVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_loadu_epi64 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQU64 zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<long> LoadVector512(long* address) => LoadVector512(address);
        /// <summary>
        ///   <para>__m512i _mm512_loadu_epi64 (__m512i const * mem_addr)</para>
        ///   <para>  VMOVDQU64 zmm1 , m512</para>
        /// </summary>
        public static unsafe Vector512<ulong> LoadVector512(ulong* address) => LoadVector512(address);
        /// <summary>
        ///   <para>__m512 _mm512_loadu_ps (float const * mem_addr)</para>
        ///   <para>  VMOVUPS zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<float> LoadVector512(float* address) => LoadVector512(address);
        /// <summary>
        ///   <para>__m512d _mm512_loadu_pd (double const * mem_addr)</para>
        ///   <para>  VMOVUPD zmm1, m512</para>
        /// </summary>
        public static unsafe Vector512<double> LoadVector512(double* address) => LoadVector512(address);

        /// <summary>
        ///   <para>__m512d _mm512_mask_loadu_pd (__m512d s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVUPD zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<double> MaskLoad(double* address, Vector512<double> mask, Vector512<double> merge) => MaskLoad(address, mask, merge);
        /// <summary>
        ///   <para>__m512i _mm512_mask_loadu_epi32 (__m512i s, __mmask16 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU32 zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<int> MaskLoad(int* address, Vector512<int> mask, Vector512<int> merge) => MaskLoad(address, mask, merge);
        /// <summary>
        ///   <para>__m512i _mm512_mask_loadu_epi64 (__m512i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU64 zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<long> MaskLoad(long* address, Vector512<long> mask, Vector512<long> merge) => MaskLoad(address, mask, merge);
        /// <summary>
        ///   <para>__m512 _mm512_mask_loadu_ps (__m512 s, __mmask16 k, void const * mem_addr)</para>
        ///   <para>  VMOVUPS zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<float> MaskLoad(float* address, Vector512<float> mask, Vector512<float> merge) => MaskLoad(address, mask, merge);
        /// <summary>
        ///   <para>__m512i _mm512_mask_loadu_epi32 (__m512i s, __mmask16 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU32 zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<uint> MaskLoad(uint* address, Vector512<uint> mask, Vector512<uint> merge) => MaskLoad(address, mask, merge);
        /// <summary>
        ///   <para>__m512i _mm512_mask_loadu_epi64 (__m512i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQU64 zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<ulong> MaskLoad(ulong* address, Vector512<ulong> mask, Vector512<ulong> merge) => MaskLoad(address, mask, merge);

        /// <summary>
        ///   <para>__m512d _mm512_mask_load_pd (__m512d s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVAPD zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<double> MaskLoadAligned(double* address, Vector512<double> mask, Vector512<double> merge) => MaskLoadAligned(address, mask, merge);
        /// <summary>
        ///   <para>__m512i _mm512_mask_load_epi32 (__m512i s, __mmask16 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQA32 zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<int> MaskLoadAligned(int* address, Vector512<int> mask, Vector512<int> merge) => MaskLoadAligned(address, mask, merge);
        /// <summary>
        ///   <para>__m512i _mm512_mask_load_epi64 (__m512i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQA64 zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<long> MaskLoadAligned(long* address, Vector512<long> mask, Vector512<long> merge) => MaskLoadAligned(address, mask, merge);
        /// <summary>
        ///   <para>__m512 _mm512_mask_load_ps (__m512 s, __mmask16 k, void const * mem_addr)</para>
        ///   <para>  VMOVAPS zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<float> MaskLoadAligned(float* address, Vector512<float> mask, Vector512<float> merge) => MaskLoadAligned(address, mask, merge);
        /// <summary>
        ///   <para>__m512i _mm512_mask_load_epi32 (__m512i s, __mmask16 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQA32 zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<uint> MaskLoadAligned(uint* address, Vector512<uint> mask, Vector512<uint> merge) => MaskLoadAligned(address, mask, merge);
        /// <summary>
        ///   <para>__m512i _mm512_mask_load_epi64 (__m512i s, __mmask8 k, void const * mem_addr)</para>
        ///   <para>  VMOVDQA64 zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<ulong> MaskLoadAligned(ulong* address, Vector512<ulong> mask, Vector512<ulong> merge) => MaskLoadAligned(address, mask, merge);

        /// <summary>
        ///   <para>void _mm512_mask_storeu_pd (void * mem_addr, __mmask8 k, __m512d a)</para>
        ///   <para>  VMOVUPD m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStore(double* address, Vector512<double> mask, Vector512<double> source) => MaskStore(address, mask, source);
        /// <summary>
        ///   <para>void _mm512_mask_storeu_epi32 (void * mem_addr, __mmask16 k, __m512i a)</para>
        ///   <para>  VMOVDQU32 m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStore(int* address, Vector512<int> mask, Vector512<int> source) => MaskStore(address, mask, source);
        /// <summary>
        ///   <para>void _mm512_mask_storeu_epi64 (void * mem_addr, __mmask8 k, __m512i a)</para>
        ///   <para>  VMOVDQU64 m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStore(long* address, Vector512<long> mask, Vector512<long> source) => MaskStore(address, mask, source);
        /// <summary>
        ///   <para>void _mm512_mask_storeu_ps (void * mem_addr, __mmask16 k, __m512 a)</para>
        ///   <para>  VMOVUPS m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStore(float* address, Vector512<float> mask, Vector512<float> source) => MaskStore(address, mask, source);
        /// <summary>
        ///   <para>void _mm512_mask_storeu_epi32 (void * mem_addr, __mmask16 k, __m512i a)</para>
        ///   <para>  VMOVDQU32 m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStore(uint* address, Vector512<uint> mask, Vector512<uint> source) => MaskStore(address, mask, source);
        /// <summary>
        ///   <para>void _mm512_mask_storeu_epi64 (void * mem_addr, __mmask8 k, __m512i a)</para>
        ///   <para>  VMOVDQU64 m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStore(ulong* address, Vector512<ulong> mask, Vector512<ulong> source) => MaskStore(address, mask, source);

        /// <summary>
        ///   <para>void _mm512_mask_store_pd (void * mem_addr, __mmask8 k, __m512d a)</para>
        ///   <para>  VMOVAPD m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(double* address, Vector512<double> mask, Vector512<double> source) => MaskStoreAligned(address, mask, source);
        /// <summary>
        ///   <para>void _mm512_mask_store_epi32 (void * mem_addr, __mmask16 k, __m512i a)</para>
        ///   <para>  VMOVDQA32 m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(int* address, Vector512<int> mask, Vector512<int> source) => MaskStoreAligned(address, mask, source);
        /// <summary>
        ///   <para>void _mm512_mask_store_epi64 (void * mem_addr, __mmask8 k, __m512i a)</para>
        ///   <para>  VMOVDQA32 m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(long* address, Vector512<long> mask, Vector512<long> source) => MaskStoreAligned(address, mask, source);
        /// <summary>
        ///   <para>void _mm512_mask_store_ps (void * mem_addr, __mmask16 k, __m512 a)</para>
        ///   <para>  VMOVAPS m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(float* address, Vector512<float> mask, Vector512<float> source) => MaskStoreAligned(address, mask, source);
        /// <summary>
        ///   <para>void _mm512_mask_store_epi32 (void * mem_addr, __mmask16 k, __m512i a)</para>
        ///   <para>  VMOVDQA32 m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(uint* address, Vector512<uint> mask, Vector512<uint> source) => MaskStoreAligned(address, mask, source);
        /// <summary>
        ///   <para>void _mm512_mask_store_epi64 (void * mem_addr, __mmask8 k, __m512i a)</para>
        ///   <para>  VMOVDQA32 m512 {k1}{z}, zmm1</para>
        /// </summary>
        public static unsafe void MaskStoreAligned(ulong* address, Vector512<ulong> mask, Vector512<ulong> source) => MaskStoreAligned(address, mask, source);

        /// <summary>
        ///   <para>__m512i _mm512_max_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPMAXSD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> Max(Vector512<int> left, Vector512<int> right) => Max(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_max_epu32 (__m512i a, __m512i b)</para>
        ///   <para>  VPMAXUD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> Max(Vector512<uint> left, Vector512<uint> right) => Max(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_max_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPMAXSQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> Max(Vector512<long> left, Vector512<long> right) => Max(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_max_epu64 (__m512i a, __m512i b)</para>
        ///   <para>  VPMAXUQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> Max(Vector512<ulong> left, Vector512<ulong> right) => Max(left, right);
        /// <summary>
        ///   <para>__m512 _mm512_max_ps (__m512 a, __m512 b)</para>
        ///   <para>  VMAXPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{sae}</para>
        /// </summary>
        public static Vector512<float> Max(Vector512<float> left, Vector512<float> right) => Max(left, right);
        /// <summary>
        ///   <para>__m512d _mm512_max_pd (__m512d a, __m512d b)</para>
        ///   <para>  VMAXPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{sae}</para>
        /// </summary>
        public static Vector512<double> Max(Vector512<double> left, Vector512<double> right) => Max(left, right);

        /// <summary>
        ///   <para>__m512i _mm512_min_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPMINSD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> Min(Vector512<int> left, Vector512<int> right) => Min(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_min_epu32 (__m512i a, __m512i b)</para>
        ///   <para>  VPMINUD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> Min(Vector512<uint> left, Vector512<uint> right) => Min(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_min_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPMINSQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> Min(Vector512<long> left, Vector512<long> right) => Min(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_min_epu64 (__m512i a, __m512i b)</para>
        ///   <para>  VPMINUQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> Min(Vector512<ulong> left, Vector512<ulong> right) => Min(left, right);
        /// <summary>
        ///   <para>__m512 _mm512_min_ps (__m512 a, __m512 b)</para>
        ///   <para>  VMINPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{sae}</para>
        /// </summary>
        public static Vector512<float> Min(Vector512<float> left, Vector512<float> right) => Min(left, right);
        /// <summary>
        ///   <para>__m512d _mm512_min_pd (__m512d a, __m512d b)</para>
        ///   <para>  VMINPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{sae}</para>
        /// </summary>
        public static Vector512<double> Min(Vector512<double> left, Vector512<double> right) => Min(left, right);

        /// <summary>
        ///   <para>unsigned int _cvtmask16_u32 (__mmask16 a)</para>
        ///   <para>  KMOVW r32, k1</para>
        /// </summary>
        public static new int MoveMask(Vector128<byte> value) => MoveMask(value);
        /// <summary>
        ///   <para>unsigned int _cvtmask16_u32 (__mmask16 a)</para>
        ///   <para>  KMOVW r32, k1</para>
        /// </summary>
        public static new int MoveMask(Vector128<sbyte> value) => MoveMask(value);

        /// <summary>
        ///   <para>unsigned int _cvtmask16_u32 (__mmask16 a)</para>
        ///   <para>  KMOVW r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector256<short> value) => MoveMask(value);
        /// <summary>
        ///   <para>unsigned int _cvtmask16_u32 (__mmask16 a)</para>
        ///   <para>  KMOVW r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector256<ushort> value) => MoveMask(value);

        /// <summary>
        ///   <para>unsigned int _cvtmask16_u32 (__mmask16 a)</para>
        ///   <para>  KMOVW r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector512<int> value) => MoveMask(value);
        /// <summary>
        ///   <para>unsigned int _cvtmask16_u32 (__mmask16 a)</para>
        ///   <para>  KMOVW r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector512<float> value) => MoveMask(value);
        /// <summary>
        ///   <para>unsigned int _cvtmask16_u32 (__mmask16 a)</para>
        ///   <para>  KMOVW r32, k1</para>
        /// </summary>
        public static int MoveMask(Vector512<uint> value) => MoveMask(value);

        /// <summary>
        ///   <para>__m512i _mm512_mul_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPMULDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> Multiply(Vector512<int> left, Vector512<int> right) => Multiply(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_mul_epu32 (__m512i a, __m512i b)</para>
        ///   <para>  VPMULUDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> Multiply(Vector512<uint> left, Vector512<uint> right) => Multiply(left, right);
        /// <summary>
        ///   <para>__m512 _mm512_mul_ps (__m512 a, __m512 b)</para>
        ///   <para>  VMULPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{er}</para>
        /// </summary>
        public static Vector512<float> Multiply(Vector512<float> left, Vector512<float> right) => Multiply(left, right);
        /// <summary>
        ///   <para>__m512d _mm512_mul_pd (__m512d a, __m512d b)</para>
        ///   <para>  VMULPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{er}</para>
        /// </summary>
        public static Vector512<double> Multiply(Vector512<double> left, Vector512<double> right) => Multiply(left, right);
        /// <summary>
        ///   <para>__m512 _mm512_mul_round_ps (__m512 a, __m512 b, int rounding)</para>
        ///   <para>  VMULPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<float> Multiply(Vector512<float> left, Vector512<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Multiply(left, right, mode);
        /// <summary>
        ///   <para>__m512d _mm512_mul_round_pd (__m512d a, __m512d b, int rounding)</para>
        ///   <para>  VMULPD zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<double> Multiply(Vector512<double> left, Vector512<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Multiply(left, right, mode);
        /// <summary>
        ///   <para>__m128 _mm_mul_round_ss (__m128 a, __m128 b, int rounding)</para>
        ///   <para>  VMULSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> MultiplyScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => MultiplyScalar(left, right, mode);
        /// <summary>
        ///   <para>__m128d _mm_mul_round_sd (__m128d a, __m128d b, int rounding)</para>
        ///   <para>  VMULSD xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> MultiplyScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => MultiplyScalar(left, right, mode);
        /// <summary>
        ///   <para>__m512i _mm512_mullo_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPMULLD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> MultiplyLow(Vector512<int> left, Vector512<int> right) => MultiplyLow(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_mullo_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPMULLD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> MultiplyLow(Vector512<uint> left, Vector512<uint> right) => MultiplyLow(left, right);

        /// <summary>
        ///   <para>__m512i _mm512_or_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<byte> Or(Vector512<byte> left, Vector512<byte> right) => Or(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_or_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<sbyte> Or(Vector512<sbyte> left, Vector512<sbyte> right) => Or(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_or_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<short> Or(Vector512<short> left, Vector512<short> right) => Or(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_or_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<ushort> Or(Vector512<ushort> left, Vector512<ushort> right) => Or(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_or_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> Or(Vector512<int> left, Vector512<int> right) => Or(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_or_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> Or(Vector512<uint> left, Vector512<uint> right) => Or(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_or_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> Or(Vector512<long> left, Vector512<long> right) => Or(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_or_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> Or(Vector512<ulong> left, Vector512<ulong> right) => Or(left, right);

        /// <summary>
        ///   <para>__m512d _mm512_permute_pd (__m512d a, int imm8)</para>
        ///   <para>  VPERMILPD zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<double> Permute2x64(Vector512<double> value, [ConstantExpected] byte control) => Permute2x64(value, control);

        /// <summary>
        ///   <para>__m512 _mm512_permute_ps (__m512 a, int imm8)</para>
        ///   <para>  VPERMILPS zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<float> Permute4x32(Vector512<float> value, [ConstantExpected] byte control) => Permute4x32(value, control);

        /// <summary>
        ///   <para>__m512i _mm512_permute4x64_epi64 (__m512i a, const int imm8)</para>
        ///   <para>  VPERMQ zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<long> Permute4x64(Vector512<long> value, [ConstantExpected] byte control) => Permute4x64(value, control);
        /// <summary>
        ///   <para>__m512i _mm512_permute4x64_epi64 (__m512i a, const int imm8)</para>
        ///   <para>  VPERMQ zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<ulong> Permute4x64(Vector512<ulong> value, [ConstantExpected] byte control) => Permute4x64(value, control);
        /// <summary>
        ///   <para>__m512d _mm512_permute4x64_pd (__m512d a, const int imm8)</para>
        ///   <para>  VPERMPD zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<double> Permute4x64(Vector512<double> value, [ConstantExpected] byte control) => Permute4x64(value, control);

        /// <summary>
        ///   <para>__m512d _mm512_permutevar_pd (__m512d a, __m512i b)</para>
        ///   <para>  VPERMILPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<double> PermuteVar2x64(Vector512<double> left, Vector512<long> control) => PermuteVar2x64(left, control);

        /// <summary>
        ///   <para>__m512 _mm512_permutevar_ps (__m512 a, __m512i b)</para>
        ///   <para>  VPERMILPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<float> PermuteVar4x32(Vector512<float> left, Vector512<int> control) => PermuteVar4x32(left, control);

        /// <summary>
        ///   <para>__m512i _mm512_permutexvar_epi64 (__m512i idx, __m512i a)</para>
        ///   <para>  VPERMQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector512<long> PermuteVar8x64(Vector512<long> value, Vector512<long> control) => PermuteVar8x64(value, control);
        /// <summary>
        ///   <para>__m512i _mm512_permutexvar_epi64 (__m512i idx, __m512i a)</para>
        ///   <para>  VPERMQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector512<ulong> PermuteVar8x64(Vector512<ulong> value, Vector512<ulong> control) => PermuteVar8x64(value, control);
        /// <summary>
        ///   <para>__m512d _mm512_permutexvar_pd (__m512i idx, __m512d a)</para>
        ///   <para>  VPERMPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector512<double> PermuteVar8x64(Vector512<double> value, Vector512<long> control) => PermuteVar8x64(value, control);

        /// <summary>
        ///   <para>__m512i _mm512_permutex2var_epi64 (__m512i a, __m512i idx, __m512i b)</para>
        ///   <para>  VPERMI2Q zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        ///   <para>  VPERMT2Q zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> PermuteVar8x64x2(Vector512<long> lower, Vector512<long> indices, Vector512<long> upper) => PermuteVar8x64x2(lower, indices, upper);
        /// <summary>
        ///   <para>__m512i _mm512_permutex2var_epi64 (__m512i a, __m512i idx, __m512i b)</para>
        ///   <para>  VPERMI2Q zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        ///   <para>  VPERMT2Q zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> PermuteVar8x64x2(Vector512<ulong> lower, Vector512<ulong> indices, Vector512<ulong> upper) => PermuteVar8x64x2(lower, indices, upper);
        /// <summary>
        ///   <para>__m512d _mm512_permutex2var_pd (__m512d a, __m512i idx, __m512i b)</para>
        ///   <para>  VPERMI2PD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        ///   <para>  VPERMT2PD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<double> PermuteVar8x64x2(Vector512<double> lower, Vector512<long> indices, Vector512<double> upper) => PermuteVar8x64x2(lower, indices, upper);

        /// <summary>
        ///   <para>__m512i _mm512_permutexvar_epi32 (__m512i idx, __m512i a)</para>
        ///   <para>  VPERMD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector512<int> PermuteVar16x32(Vector512<int> left, Vector512<int> control) => PermuteVar16x32(left, control);
        /// <summary>
        ///   <para>__m512i _mm512_permutexvar_epi32 (__m512i idx, __m512i a)</para>
        ///   <para>  VPERMD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector512<uint> PermuteVar16x32(Vector512<uint> left, Vector512<uint> control) => PermuteVar16x32(left, control);
        /// <summary>
        ///   <para>__m512 _mm512_permutexvar_ps (__m512i idx, __m512 a)</para>
        ///   <para>  VPERMPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static Vector512<float> PermuteVar16x32(Vector512<float> left, Vector512<int> control) => PermuteVar16x32(left, control);

        /// <summary>
        ///   <para>__m512i _mm512_permutex2var_epi32 (__m512i a, __m512i idx, __m512i b)</para>
        ///   <para>  VPERMI2D zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        ///   <para>  VPERMT2D zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> PermuteVar16x32x2(Vector512<int> lower, Vector512<int> indices, Vector512<int> upper) => PermuteVar16x32x2(lower, indices, upper);
        /// <summary>
        ///   <para>__m512i _mm512_permutex2var_epi32 (__m512i a, __m512i idx, __m512i b)</para>
        ///   <para>  VPERMI2D zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        ///   <para>  VPERMT2D zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> PermuteVar16x32x2(Vector512<uint> lower, Vector512<uint> indices, Vector512<uint> upper) => PermuteVar16x32x2(lower, indices, upper);
        /// <summary>
        ///   <para>__m512 _mm512_permutex2var_ps (__m512 a, __m512i idx, __m512i b)</para>
        ///   <para>  VPERMI2PS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        ///   <para>  VPERMT2PS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<float> PermuteVar16x32x2(Vector512<float> lower, Vector512<int> indices, Vector512<float> upper) => PermuteVar16x32x2(lower, indices, upper);

        /// <summary>
        ///   <para>__m512 _mm512_rcp14_ps (__m512 a, __m512 b)</para>
        ///   <para>  VRCP14PS zmm1 {k1}{z}, zmm2/m512/m32bcst</para>
        /// </summary>
        public static Vector512<float> Reciprocal14(Vector512<float> value) => Reciprocal14(value);
        /// <summary>
        ///   <para>__m512d _mm512_rcp14_pd (__m512d a, __m512d b)</para>
        ///   <para>  VRCP14PD zmm1 {k1}{z}, zmm2/m512/m64bcst</para>
        /// </summary>
        public static Vector512<double> Reciprocal14(Vector512<double> value) => Reciprocal14(value);

        /// <summary>
        ///   <para>__m128 _mm_rcp14_ss (__m128 a)</para>
        ///   <para>  VRCP14SS xmm1 {k1}{z}, xmm2, xmm3/m32</para>
        /// </summary>
        public static Vector128<float> Reciprocal14Scalar(Vector128<float> value) => Reciprocal14Scalar(value);
        /// <summary>
        ///   <para>__m128d _mm_rcp14_sd (__m128d a)</para>
        ///   <para>  VRCP14SD xmm1 {k1}{z}, xmm2, xmm3/m64</para>
        /// </summary>
        public static Vector128<double> Reciprocal14Scalar(Vector128<double> value) => Reciprocal14Scalar(value);
        /// <summary>
        ///   <para>__m128 _mm_rcp14_ss (__m128 a, __m128 b)</para>
        ///   <para>  VRCP14SS xmm1 {k1}{z}, xmm2, xmm3/m32</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<float> Reciprocal14Scalar(Vector128<float> upper, Vector128<float> value) => Reciprocal14Scalar(upper, value);
        /// <summary>
        ///   <para>__m128d _mm_rcp14_sd (__m128d a, __m128d b)</para>
        ///   <para>  VRCP14SD xmm1 {k1}{z}, xmm2, xmm3/m64</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<double> Reciprocal14Scalar(Vector128<double> upper, Vector128<double> value) => Reciprocal14Scalar(upper, value);

        /// <summary>
        ///   <para>__m512 _mm512_rsqrt14_ps (__m512 a, __m512 b)</para>
        ///   <para>  VRSQRT14PS zmm1 {k1}{z}, zmm2/m512/m32bcst</para>
        /// </summary>
        public static Vector512<float> ReciprocalSqrt14(Vector512<float> value) => ReciprocalSqrt14(value);
        /// <summary>
        ///   <para>__m512d _mm512_rsqrt14_pd (__m512d a, __m512d b)</para>
        ///   <para>  VRSQRT14PD zmm1 {k1}{z}, zmm2/m512/m64bcst</para>
        /// </summary>
        public static Vector512<double> ReciprocalSqrt14(Vector512<double> value) => ReciprocalSqrt14(value);

        /// <summary>
        ///   <para>__m128 _mm_rsqrt14_ss (__m128 a)</para>
        ///   <para>  VRSQRT14SS xmm1 {k1}{z}, xmm2, xmm3/m32</para>
        /// </summary>
        public static Vector128<float> ReciprocalSqrt14Scalar(Vector128<float> value) => ReciprocalSqrt14Scalar(value);
        /// <summary>
        ///   <para>__m128d _mm_rsqrt14_sd (__m128d a)</para>
        ///   <para>  VRSQRT14SD xmm1 {k1}{z}, xmm2, xmm3/m64</para>
        /// </summary>
        public static Vector128<double> ReciprocalSqrt14Scalar(Vector128<double> value) => ReciprocalSqrt14Scalar(value);
        /// <summary>
        ///   <para>__m128 _mm_rsqrt14_ss (__m128 a, __m128 b)</para>
        ///   <para>  VRSQRT14SS xmm1 {k1}{z}, xmm2, xmm3/m32</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<float> ReciprocalSqrt14Scalar(Vector128<float> upper, Vector128<float> value) => ReciprocalSqrt14Scalar(upper, value);
        /// <summary>
        ///   <para>__m128d _mm_rsqrt14_sd (__m128d a, __m128d b)</para>
        ///   <para>  VRSQRT14SD xmm1 {k1}{z}, xmm2, xmm3/m64</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<double> ReciprocalSqrt14Scalar(Vector128<double> upper, Vector128<double> value) => ReciprocalSqrt14Scalar(upper, value);

        /// <summary>
        ///   <para>__m512i _mm512_rol_epi32 (__m512i a, int imm8)</para>
        ///   <para>  VPROLD zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<int> RotateLeft(Vector512<int> value, [ConstantExpected] byte count) => RotateLeft(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_rol_epi32 (__m512i a, int imm8)</para>
        ///   <para>  VPROLD zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<uint> RotateLeft(Vector512<uint> value, [ConstantExpected] byte count) => RotateLeft(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_rol_epi64 (__m512i a, int imm8)</para>
        ///   <para>  VPROLQ zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<long> RotateLeft(Vector512<long> value, [ConstantExpected] byte count) => RotateLeft(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_rol_epi64 (__m512i a, int imm8)</para>
        ///   <para>  VPROLQ zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<ulong> RotateLeft(Vector512<ulong> value, [ConstantExpected] byte count) => RotateLeft(value, count);

        /// <summary>
        ///   <para>__m512i _mm512_rolv_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPROLDV zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> RotateLeftVariable(Vector512<int> value, Vector512<uint> count) => RotateLeftVariable(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_rolv_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPROLDV zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> RotateLeftVariable(Vector512<uint> value, Vector512<uint> count) => RotateLeftVariable(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_rolv_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPROLQV zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> RotateLeftVariable(Vector512<long> value, Vector512<ulong> count) => RotateLeftVariable(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_rolv_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPROLQV zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> RotateLeftVariable(Vector512<ulong> value, Vector512<ulong> count) => RotateLeftVariable(value, count);

        /// <summary>
        ///   <para>__m512i _mm512_ror_epi32 (__m512i a, int imm8)</para>
        ///   <para>  VPRORD zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<int> RotateRight(Vector512<int> value, [ConstantExpected] byte count) => RotateRight(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_ror_epi32 (__m512i a, int imm8)</para>
        ///   <para>  VPRORD zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<uint> RotateRight(Vector512<uint> value, [ConstantExpected] byte count) => RotateRight(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_ror_epi64 (__m512i a, int imm8)</para>
        ///   <para>  VPRORQ zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<long> RotateRight(Vector512<long> value, [ConstantExpected] byte count) => RotateRight(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_ror_epi64 (__m512i a, int imm8)</para>
        ///   <para>  VPRORQ zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<ulong> RotateRight(Vector512<ulong> value, [ConstantExpected] byte count) => RotateRight(value, count);

        /// <summary>
        ///   <para>__m512i _mm512_rorv_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPRORDV zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> RotateRightVariable(Vector512<int> value, Vector512<uint> count) => RotateRightVariable(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_rorv_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPRORDV zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> RotateRightVariable(Vector512<uint> value, Vector512<uint> count) => RotateRightVariable(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_rorv_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPRORQV zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> RotateRightVariable(Vector512<long> value, Vector512<ulong> count) => RotateRightVariable(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_rorv_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPRORQV zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> RotateRightVariable(Vector512<ulong> value, Vector512<ulong> count) => RotateRightVariable(value, count);

        /// <summary>
        ///   <para>__m512 _mm512_roundscale_ps (__m512 a, int imm)</para>
        ///   <para>  VRNDSCALEPS zmm1 {k1}{z}, zmm2/m512/m32bcst{sae}, imm8</para>
        /// </summary>
        public static Vector512<float> RoundScale(Vector512<float> value, [ConstantExpected] byte control) => RoundScale(value, control);
        /// <summary>
        ///   <para>__m512d _mm512_roundscale_pd (__m512d a, int imm)</para>
        ///   <para>  VRNDSCALEPD zmm1 {k1}{z}, zmm2/m512/m64bcst{sae}, imm8</para>
        /// </summary>
        public static Vector512<double> RoundScale(Vector512<double> value, [ConstantExpected] byte control) => RoundScale(value, control);

        /// <summary>
        ///   <para>__m128 _mm_roundscale_ss (__m128 a, int imm)</para>
        ///   <para>  VRNDSCALESS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8</para>
        /// </summary>
        public static Vector128<float> RoundScaleScalar(Vector128<float> value, [ConstantExpected] byte control) => RoundScaleScalar(value, control);
        /// <summary>
        ///   <para>__m128d _mm_roundscale_sd (__m128d a, int imm)</para>
        ///   <para>  VRNDSCALESD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8</para>
        /// </summary>
        public static Vector128<double> RoundScaleScalar(Vector128<double> value, [ConstantExpected] byte control) => RoundScaleScalar(value, control);
        /// <summary>
        ///   <para>__m128 _mm_roundscale_ss (__m128 a, __m128 b, int imm)</para>
        ///   <para>  VRNDSCALESS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<float> RoundScaleScalar(Vector128<float> upper, Vector128<float> value, [ConstantExpected] byte control) => RoundScaleScalar(upper, value, control);
        /// <summary>
        ///   <para>__m128d _mm_roundscale_sd (__m128d a, __m128d b, int imm)</para>
        ///   <para>  VRNDSCALESD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<double> RoundScaleScalar(Vector128<double> upper, Vector128<double> value, [ConstantExpected] byte control) => RoundScaleScalar(upper, value, control);

        /// <summary>
        ///   <para>__m512 _mm512_scalef_ps (__m512 a, __m512 b)</para>
        ///   <para>  VSCALEFPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{er}</para>
        /// </summary>
        public static Vector512<float> Scale(Vector512<float> left, Vector512<float> right) => Scale(left, right);
        /// <summary>
        ///   <para>__m512d _mm512_scalef_pd (__m512d a, __m512d b)</para>
        ///   <para>  VSCALEFPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{er}</para>
        /// </summary>
        public static Vector512<double> Scale(Vector512<double> left, Vector512<double> right) => Scale(left, right);
        /// <summary>
        ///   <para>__m512 _mm512_scalef_round_ps (__m512 a, __m512 b, int rounding)</para>
        ///   <para>  VSCALEFPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<float> Scale(Vector512<float> left, Vector512<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Scale(left, right, mode);
        /// <summary>
        ///   <para>__m512d _mm512_scalef_round_pd (__m512d a, __m512d b, int rounding)</para>
        ///   <para>  VSCALEFPD zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<double> Scale(Vector512<double> left, Vector512<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Scale(left, right, mode);

        /// <summary>
        ///   <para>__m128 _mm_scalef_ss (__m128 a, __m128 b)</para>
        ///   <para>  VSCALEFSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}</para>
        /// </summary>
        public static Vector128<float> ScaleScalar(Vector128<float> left, Vector128<float> right) => ScaleScalar(left, right);
        /// <summary>
        ///   <para>__m128d _mm_scalef_sd (__m128d a, __m128d b)</para>
        ///   <para>  VSCALEFSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}</para>
        /// </summary>
        public static Vector128<double> ScaleScalar(Vector128<double> left, Vector128<double> right) => ScaleScalar(left, right);
        /// <summary>
        ///   <para>__m128 _mm_scalef_round_ss (__m128 a, __m128 b)</para>
        ///   <para>  VSCALEFSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> ScaleScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ScaleScalar(left, right, mode);
        /// <summary>
        ///   <para>__m128d _mm_scalef_round_sd (__m128d a, __m128d b)</para>
        ///   <para>  VSCALEFSD xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> ScaleScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => ScaleScalar(left, right, mode);

        /// <summary>
        ///   <para>__m512i _mm512_sll_epi32 (__m512i a, __m128i count)</para>
        ///   <para>  VPSLLD zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<int> ShiftLeftLogical(Vector512<int> value, Vector128<int> count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_sll_epi32 (__m512i a, __m128i count)</para>
        ///   <para>  VPSLLD zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<uint> ShiftLeftLogical(Vector512<uint> value, Vector128<uint> count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_sll_epi64 (__m512i a, __m128i count)</para>
        ///   <para>  VPSLLQ zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<long> ShiftLeftLogical(Vector512<long> value, Vector128<long> count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_sll_epi64 (__m512i a, __m128i count)</para>
        ///   <para>  VPSLLQ zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<ulong> ShiftLeftLogical(Vector512<ulong> value, Vector128<ulong> count) => ShiftLeftLogical(value, count);

        /// <summary>
        ///   <para>__m512i _mm512_slli_epi32 (__m512i a, int imm8)</para>
        ///   <para>  VPSLLD zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<int> ShiftLeftLogical(Vector512<int> value, [ConstantExpected] byte count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_slli_epi32 (__m512i a, int imm8)</para>
        ///   <para>  VPSLLD zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<uint> ShiftLeftLogical(Vector512<uint> value, [ConstantExpected] byte count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_slli_epi64 (__m512i a, int imm8)</para>
        ///   <para>  VPSLLQ zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<long> ShiftLeftLogical(Vector512<long> value, [ConstantExpected] byte count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_slli_epi64 (__m512i a, int imm8)</para>
        ///   <para>  VPSLLQ zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<ulong> ShiftLeftLogical(Vector512<ulong> value, [ConstantExpected] byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        ///   <para>__m512i _mm512_sllv_epi32 (__m512i a, __m512i count)</para>
        ///   <para>  VPSLLVD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> ShiftLeftLogicalVariable(Vector512<int> value, Vector512<uint> count) => ShiftLeftLogicalVariable(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_sllv_epi32 (__m512i a, __m512i count)</para>
        ///   <para>  VPSLLVD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> ShiftLeftLogicalVariable(Vector512<uint> value, Vector512<uint> count) => ShiftLeftLogicalVariable(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_sllv_epi64 (__m512i a, __m512i count)</para>
        ///   <para>  VPSLLVQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> ShiftLeftLogicalVariable(Vector512<long> value, Vector512<ulong> count) => ShiftLeftLogicalVariable(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_sllv_epi64 (__m512i a, __m512i count)</para>
        ///   <para>  VPSLLVQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> ShiftLeftLogicalVariable(Vector512<ulong> value, Vector512<ulong> count) => ShiftLeftLogicalVariable(value, count);

        /// <summary>
        ///   <para>_mm512_sra_epi32 (__m512i a, __m128i count)</para>
        ///   <para>  VPSRAD zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<int> ShiftRightArithmetic(Vector512<int> value, Vector128<int> count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   <para>_mm512_sra_epi64 (__m512i a, __m128i count)</para>
        ///   <para>  VPSRAQ zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<long> ShiftRightArithmetic(Vector512<long> value, Vector128<long> count) => ShiftRightArithmetic(value, count);

        /// <summary>
        ///   <para>__m512i _mm512_srai_epi32 (__m512i a, int imm8)</para>
        ///   <para>  VPSRAD zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<int> ShiftRightArithmetic(Vector512<int> value, [ConstantExpected] byte count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_srai_epi64 (__m512i a, int imm8)</para>
        ///   <para>  VPSRAQ zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<long> ShiftRightArithmetic(Vector512<long> value, [ConstantExpected] byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        ///   <para>__m512i _mm512_srav_epi32 (__m512i a, __m512i count)</para>
        ///   <para>  VPSRAVD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> ShiftRightArithmeticVariable(Vector512<int> value, Vector512<uint> count) => ShiftRightArithmeticVariable(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_srav_epi64 (__m512i a, __m512i count)</para>
        ///   <para>  VPSRAVQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> ShiftRightArithmeticVariable(Vector512<long> value, Vector512<ulong> count) => ShiftRightArithmeticVariable(value, count);

        /// <summary>
        ///   <para>__m512i _mm512_srl_epi32 (__m512i a, __m128i count)</para>
        ///   <para>  VPSRLD zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<int> ShiftRightLogical(Vector512<int> value, Vector128<int> count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_srl_epi32 (__m512i a, __m128i count)</para>
        ///   <para>  VPSRLD zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<uint> ShiftRightLogical(Vector512<uint> value, Vector128<uint> count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_srl_epi64 (__m512i a, __m128i count)</para>
        ///   <para>  VPSRLQ zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<long> ShiftRightLogical(Vector512<long> value, Vector128<long> count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_srl_epi64 (__m512i a, __m128i count)</para>
        ///   <para>  VPSRLQ zmm1 {k1}{z}, zmm2, xmm3/m128</para>
        /// </summary>
        public static Vector512<ulong> ShiftRightLogical(Vector512<ulong> value, Vector128<ulong> count) => ShiftRightLogical(value, count);

        /// <summary>
        ///   <para>__m512i _mm512_srli_epi32 (__m512i a, int imm8)</para>
        ///   <para>  VPSRLD zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<int> ShiftRightLogical(Vector512<int> value, [ConstantExpected] byte count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_srli_epi32 (__m512i a, int imm8)</para>
        ///   <para>  VPSRLD zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<uint> ShiftRightLogical(Vector512<uint> value, [ConstantExpected] byte count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_srli_epi64 (__m512i a, int imm8)</para>
        ///   <para>  VPSRLQ zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<long> ShiftRightLogical(Vector512<long> value, [ConstantExpected] byte count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_srli_epi64 (__m512i a, int imm8)</para>
        ///   <para>  VPSRLQ zmm1 {k1}{z}, zmm2, imm8</para>
        /// </summary>
        public static Vector512<ulong> ShiftRightLogical(Vector512<ulong> value, [ConstantExpected] byte count) => ShiftRightLogical(value, count);

        /// <summary>
        ///   <para>__m512i _mm512_srlv_epi32 (__m512i a, __m512i count)</para>
        ///   <para>  VPSRLVD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> ShiftRightLogicalVariable(Vector512<int> value, Vector512<uint> count) => ShiftRightLogicalVariable(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_srlv_epi32 (__m512i a, __m512i count)</para>
        ///   <para>  VPSRLVD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> ShiftRightLogicalVariable(Vector512<uint> value, Vector512<uint> count) => ShiftRightLogicalVariable(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_srlv_epi64 (__m512i a, __m512i count)</para>
        ///   <para>  VPSRLVQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> ShiftRightLogicalVariable(Vector512<long> value, Vector512<ulong> count) => ShiftRightLogicalVariable(value, count);
        /// <summary>
        ///   <para>__m512i _mm512_srlv_epi64 (__m512i a, __m512i count)</para>
        ///   <para>  VPSRLVQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> ShiftRightLogicalVariable(Vector512<ulong> value, Vector512<ulong> count) => ShiftRightLogicalVariable(value, count);

        /// <summary>
        ///   <para>__m512i _mm512_shuffle_epi32 (__m512i a, const int imm8)</para>
        ///   <para>  VPSHUFD zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<int> Shuffle(Vector512<int> value, [ConstantExpected] byte control) => Shuffle(value, control);
        /// <summary>
        ///   <para>__m512i _mm512_shuffle_epi32 (__m512i a, const int imm8)</para>
        ///   <para>  VPSHUFD zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<uint> Shuffle(Vector512<uint> value, [ConstantExpected] byte control) => Shuffle(value, control);
        /// <summary>
        ///   <para>__m512 _mm512_shuffle_ps (__m512 a, __m512 b, const int imm8)</para>
        ///   <para>  VSHUFPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<float> Shuffle(Vector512<float> value, Vector512<float> right, [ConstantExpected] byte control) => Shuffle(value, right, control);
        /// <summary>
        ///   <para>__m512d _mm512_shuffle_pd (__m512d a, __m512d b, const int imm8)</para>
        ///   <para>  VSHUFPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<double> Shuffle(Vector512<double> value, Vector512<double> right, [ConstantExpected] byte control) => Shuffle(value, right, control);

        /// <summary>
        ///   <para>__m512d _mm512_shuffle_f64x2 (__m512d a, __m512d b, const int imm8)</para>
        ///   <para>  VSHUFF64x2 zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<double> Shuffle4x128(Vector512<double> left, Vector512<double> right, [ConstantExpected] byte control) => Shuffle4x128(left, right, control);
        /// <summary>
        ///   <para>__m512i _mm512_shuffle_i32x4 (__m512i a, __m512i b, const int imm8)</para>
        ///   <para>  VSHUFI32x4 zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<int> Shuffle4x128(Vector512<int> left, Vector512<int> right, [ConstantExpected] byte control) => Shuffle4x128(left, right, control);
        /// <summary>
        ///   <para>__m512i _mm512_shuffle_i64x2 (__m512i a, __m512i b, const int imm8)</para>
        ///   <para>  VSHUFI64x2 zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<long> Shuffle4x128(Vector512<long> left, Vector512<long> right, [ConstantExpected] byte control) => Shuffle4x128(left, right, control);
        /// <summary>
        ///   <para>__m512 _mm512_shuffle_f32x4 (__m512 a, __m512 b, const int imm8)</para>
        ///   <para>  VSHUFF32x4 zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<float> Shuffle4x128(Vector512<float> left, Vector512<float> right, [ConstantExpected] byte control) => Shuffle4x128(left, right, control);
        /// <summary>
        ///   <para>__m512i _mm512_shuffle_i32x4 (__m512i a, __m512i b, const int imm8)</para>
        ///   <para>  VSHUFI32x4 zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<uint> Shuffle4x128(Vector512<uint> left, Vector512<uint> right, [ConstantExpected] byte control) => Shuffle4x128(left, right, control);
        /// <summary>
        ///   <para>__m512i _mm512_shuffle_i64x2 (__m512i a, __m512i b, const int imm8)</para>
        ///   <para>  VSHUFI64x2 zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<ulong> Shuffle4x128(Vector512<ulong> left, Vector512<ulong> right, [ConstantExpected] byte control) => Shuffle4x128(left, right, control);

        /// <summary>
        ///   <para>__m512 _mm512_sqrt_ps (__m512 a)</para>
        ///   <para>  VSQRTPS zmm1 {k1}{z}, zmm2/m512/m32bcst{er}</para>
        /// </summary>
        public static Vector512<float> Sqrt(Vector512<float> value) => Sqrt(value);
        /// <summary>
        ///   <para>__m512d _mm512_sqrt_pd (__m512d a)</para>
        ///   <para>  VSQRTPD zmm1 {k1}{z}, zmm2/m512/m64bcst{er}</para>
        /// </summary>
        public static Vector512<double> Sqrt(Vector512<double> value) => Sqrt(value);
        /// <summary>
        ///   <para>__m512 _mm512_sqrt_round_ps (__m512 a, int rounding)</para>
        ///   <para>  VSQRTPS zmm1, zmm2 {er}</para>
        /// </summary>
        public static Vector512<float> Sqrt(Vector512<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Sqrt(value, mode);
        /// <summary>
        ///   <para>__m512d _mm512_sqrt_round_pd (__m512d a, int rounding)</para>
        ///   <para>  VSQRTPD zmm1, zmm2 {er}</para>
        /// </summary>
        public static Vector512<double> Sqrt(Vector512<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Sqrt(value, mode);
        /// <summary>
        ///   <para>__m128 _mm_sqrt_round_ss (__m128 a, __m128 b, int rounding)</para>
        ///   <para>  VSQRTSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> SqrtScalar(Vector128<float> upper, Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => SqrtScalar(upper, value, mode);
        /// <summary>
        ///   <para>__m128d _mm_sqrt_round_sd (__m128d a, __m128d b, int rounding)</para>
        ///   <para>  VSQRTSD xmm1, xmm2 xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> SqrtScalar(Vector128<double> upper, Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => SqrtScalar(upper, value, mode);

        /// <summary>
        ///   <para>void _mm512_storeu_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQU32 m512, zmm1</para>
        /// </summary>
        public static unsafe void Store(sbyte* address, Vector512<sbyte> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm512_storeu_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQU32 m512, zmm1</para>
        /// </summary>
        public static unsafe void Store(byte* address, Vector512<byte> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm512_storeu_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQU32 m512, zmm1</para>
        /// </summary>
        public static unsafe void Store(short* address, Vector512<short> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm512_storeu_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQU32 m512, zmm1</para>
        /// </summary>
        public static unsafe void Store(ushort* address, Vector512<ushort> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm512_storeu_epi32 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQU32 m512, zmm1</para>
        /// </summary>
        public static unsafe void Store(int* address, Vector512<int> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm512_storeu_epi32 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQU32 m512, zmm1</para>
        /// </summary>
        public static unsafe void Store(uint* address, Vector512<uint> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm512_storeu_epi64 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQU64 m512, zmm1</para>
        /// </summary>
        public static unsafe void Store(long* address, Vector512<long> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm512_storeu_epi64 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQU64 m512, zmm1</para>
        /// </summary>
        public static unsafe void Store(ulong* address, Vector512<ulong> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm512_storeu_ps (float * mem_addr, __m512 a)</para>
        ///   <para>  VMOVUPS m512, zmm1</para>
        /// </summary>
        public static unsafe void Store(float* address, Vector512<float> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm512_storeu_pd (double * mem_addr, __m512d a)</para>
        ///   <para>  VMOVUPD m512, zmm1</para>
        /// </summary>
        public static unsafe void Store(double* address, Vector512<double> source) => Store(address, source);

        /// <summary>
        ///   <para>void _mm512_store_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQA32 m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(byte* address, Vector512<byte> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm512_store_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQA32 m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(sbyte* address, Vector512<sbyte> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm512_store_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQA32 m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(short* address, Vector512<short> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm512_store_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQA32 m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(ushort* address, Vector512<ushort> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm512_store_epi32 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQA32 m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(int* address, Vector512<int> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm512_store_epi32 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQA32 m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(uint* address, Vector512<uint> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm512_store_epi64 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQA32 m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(long* address, Vector512<long> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm512_store_epi64 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVDQA32 m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(ulong* address, Vector512<ulong> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm512_store_ps (float * mem_addr, __m512 a)</para>
        ///   <para>  VMOVAPS m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(float* address, Vector512<float> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm512_store_pd (double * mem_addr, __m512d a)</para>
        ///   <para>  VMOVAPD m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(double* address, Vector512<double> source) => StoreAligned(address, source);

        /// <summary>
        ///   <para>void _mm512_stream_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVNTDQ m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(sbyte* address, Vector512<sbyte> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm512_stream_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVNTDQ m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(byte* address, Vector512<byte> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm512_stream_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVNTDQ m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(short* address, Vector512<short> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm512_stream_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVNTDQ m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(ushort* address, Vector512<ushort> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm512_stream_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVNTDQ m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(int* address, Vector512<int> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm512_stream_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVNTDQ m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(uint* address, Vector512<uint> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm512_stream_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVNTDQ m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(long* address, Vector512<long> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm512_stream_si512 (void * mem_addr, __m512i a)</para>
        ///   <para>  VMOVNTDQ m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(ulong* address, Vector512<ulong> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm512_stream_ps (float * mem_addr, __m512 a)</para>
        ///   <para>  VMOVNTPS m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(float* address, Vector512<float> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm512_stream_pd (double * mem_addr, __m512d a)</para>
        ///   <para>  VMOVNTPD m512, zmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(double* address, Vector512<double> source) => StoreAlignedNonTemporal(address, source);

        /// <summary>
        ///   <para>__m512i _mm512_sub_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPSUBD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> Subtract(Vector512<int> left, Vector512<int> right) => Subtract(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_sub_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPSUBD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> Subtract(Vector512<uint> left, Vector512<uint> right) => Subtract(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_sub_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPSUBQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> Subtract(Vector512<long> left, Vector512<long> right) => Subtract(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_sub_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPSUBQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> Subtract(Vector512<ulong> left, Vector512<ulong> right) => Subtract(left, right);
        /// <summary>
        ///   <para>__m512 _mm512_sub_ps (__m512 a, __m512 b)</para>
        ///   <para>  VSUBPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{er}</para>
        /// </summary>
        public static Vector512<float> Subtract(Vector512<float> left, Vector512<float> right) => Subtract(left, right);
        /// <summary>
        ///   <para>__m512d _mm512_sub_pd (__m512d a, __m512d b)</para>
        ///   <para>  VSUBPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{er}</para>
        /// </summary>
        public static Vector512<double> Subtract(Vector512<double> left, Vector512<double> right) => Subtract(left, right);
        /// <summary>
        ///   <para>__m512 _mm512_sub_round_ps (__m512 a, __m512 b, int rounding)</para>
        ///   <para>  VSUBPS zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<float> Subtract(Vector512<float> left, Vector512<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Subtract(left, right, mode);
        /// <summary>
        ///   <para>__m512d _mm512_sub_round_pd (__m512d a, __m512d b, int rounding)</para>
        ///   <para>  VSUBPD zmm1, zmm2, zmm3 {er}</para>
        /// </summary>
        public static Vector512<double> Subtract(Vector512<double> left, Vector512<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => Subtract(left, right, mode);
        /// <summary>
        ///   <para>__m128 _mm_sub_round_ss (__m128 a, __m128 b, int rounding)</para>
        ///   <para>  VSUBSS xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<float> SubtractScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => SubtractScalar(left, right, mode);
        /// <summary>
        ///   <para>__m128d _mm_sub_round_sd (__m128d a, __m128d b, int rounding)</para>
        ///   <para>  VSUBSD xmm1, xmm2, xmm3 {er}</para>
        /// </summary>
        public static Vector128<double> SubtractScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) => SubtractScalar(left, right, mode);

        /// <summary>
        ///   <para>__m512i _mm512_ternarylogic_si512 (__m512i a, __m512i b, __m512i c, int imm)</para>
        ///   <para>  VPTERNLOGD zmm1 {k1}{z}, zmm2, zmm3/m512, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector512<sbyte> TernaryLogic(Vector512<sbyte> a, Vector512<sbyte> b, Vector512<sbyte> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        ///   <para>__m512i _mm512_ternarylogic_si512 (__m512i a, __m512i b, __m512i c, byte imm)</para>
        ///   <para>  VPTERNLOGD zmm1 {k1}{z}, zmm2, zmm3/m512, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector512<byte> TernaryLogic(Vector512<byte> a, Vector512<byte> b, Vector512<byte> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        ///   <para>__m512i _mm512_ternarylogic_si512 (__m512i a, __m512i b, __m512i c, short imm)</para>
        ///   <para>  VPTERNLOGD zmm1 {k1}{z}, zmm2, zmm3/m512, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector512<short> TernaryLogic(Vector512<short> a, Vector512<short> b, Vector512<short> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        ///   <para>__m512i _mm512_ternarylogic_si512 (__m512i a, __m512i b, __m512i c, short imm)</para>
        ///   <para>  VPTERNLOGD zmm1 {k1}{z}, zmm2, zmm3/m512, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector512<ushort> TernaryLogic(Vector512<ushort> a, Vector512<ushort> b, Vector512<ushort> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        ///   <para>__m512i _mm512_ternarylogic_epi32 (__m512i a, __m512i b, __m512i c, int imm)</para>
        ///   <para>  VPTERNLOGD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<int> TernaryLogic(Vector512<int> a, Vector512<int> b, Vector512<int> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        ///   <para>__m512i _mm512_ternarylogic_epi32 (__m512i a, __m512i b, __m512i c, int imm)</para>
        ///   <para>  VPTERNLOGD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8</para>
        /// </summary>
        public static Vector512<uint> TernaryLogic(Vector512<uint> a, Vector512<uint> b, Vector512<uint> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        ///   <para>__m512i _mm512_ternarylogic_epi64 (__m512i a, __m512i b, __m512i c, int imm)</para>
        ///   <para>  VPTERNLOGQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<long> TernaryLogic(Vector512<long> a, Vector512<long> b, Vector512<long> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        ///   <para>__m512i _mm512_ternarylogic_epi64 (__m512i a, __m512i b, __m512i c, int imm)</para>
        ///   <para>  VPTERNLOGQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8</para>
        /// </summary>
        public static Vector512<ulong> TernaryLogic(Vector512<ulong> a, Vector512<ulong> b, Vector512<ulong> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        ///   <para>__m512 _mm512_ternarylogic_ps (__m512 a, __m512 b, __m512 c, int imm)</para>
        ///   <para>  VPTERNLOGD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector512<float> TernaryLogic(Vector512<float> a, Vector512<float> b, Vector512<float> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        ///   <para>__m512d _mm512_ternarylogic_pd (__m512d a, __m512d b, __m512d c, int imm)</para>
        ///   <para>  VPTERNLOGQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.</para>
        /// </summary>
        public static Vector512<double> TernaryLogic(Vector512<double> a, Vector512<double> b, Vector512<double> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);

        /// <summary>
        ///   <para>__m512i _mm512_unpackhi_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKHDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> UnpackHigh(Vector512<int> left, Vector512<int> right) => UnpackHigh(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_unpackhi_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKHDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> UnpackHigh(Vector512<uint> left, Vector512<uint> right) => UnpackHigh(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_unpackhi_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKHQDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> UnpackHigh(Vector512<long> left, Vector512<long> right) => UnpackHigh(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_unpackhi_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKHQDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> UnpackHigh(Vector512<ulong> left, Vector512<ulong> right) => UnpackHigh(left, right);
        /// <summary>
        ///   <para>__m512 _mm512_unpackhi_ps (__m512 a, __m512 b)</para>
        ///   <para>  VUNPCKHPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<float> UnpackHigh(Vector512<float> left, Vector512<float> right) => UnpackHigh(left, right);
        /// <summary>
        ///   <para>__m512d _mm512_unpackhi_pd (__m512d a, __m512d b)</para>
        ///   <para>  VUNPCKHPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<double> UnpackHigh(Vector512<double> left, Vector512<double> right) => UnpackHigh(left, right);

        /// <summary>
        ///   <para>__m512i _mm512_unpacklo_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKLDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> UnpackLow(Vector512<int> left, Vector512<int> right) => UnpackLow(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_unpacklo_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKLDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> UnpackLow(Vector512<uint> left, Vector512<uint> right) => UnpackLow(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_unpacklo_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKLQDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> UnpackLow(Vector512<long> left, Vector512<long> right) => UnpackLow(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_unpacklo_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPUNPCKLQDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> UnpackLow(Vector512<ulong> left, Vector512<ulong> right) => UnpackLow(left, right);
        /// <summary>
        ///   <para>__m512 _mm512_unpacklo_ps (__m512 a, __m512 b)</para>
        ///   <para>  VUNPCKLPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<float> UnpackLow(Vector512<float> left, Vector512<float> right) => UnpackLow(left, right);
        /// <summary>
        ///   <para>__m512d _mm512_unpacklo_pd (__m512d a, __m512d b)</para>
        ///   <para>  VUNPCKLPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<double> UnpackLow(Vector512<double> left, Vector512<double> right) => UnpackLow(left, right);

        /// <summary>
        ///   <para>__m512i _mm512_xor_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<byte> Xor(Vector512<byte> left, Vector512<byte> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_xor_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<sbyte> Xor(Vector512<sbyte> left, Vector512<sbyte> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_xor_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<short> Xor(Vector512<short> left, Vector512<short> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_xor_si512 (__m512i a, __m512i b)</para>
        ///   <para>  VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<ushort> Xor(Vector512<ushort> left, Vector512<ushort> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_xor_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> Xor(Vector512<int> left, Vector512<int> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_xor_epi32 (__m512i a, __m512i b)</para>
        ///   <para>  VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> Xor(Vector512<uint> left, Vector512<uint> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_xor_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPXORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> Xor(Vector512<long> left, Vector512<long> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m512i _mm512_xor_epi64 (__m512i a, __m512i b)</para>
        ///   <para>  VPXORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> Xor(Vector512<ulong> left, Vector512<ulong> right) => Xor(left, right);
    }
}
