// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 FMA hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Fma : Avx
    {
        internal Fma() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>Provides access to the x86 FMA hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class X64 : Avx.X64
        {
            internal X64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        ///   <para>__m128 _mm_fmadd_ps (__m128 a, __m128 b, __m128 c)</para>
        ///   <para>  VFMADDPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VFMADDPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> MultiplyAdd(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_fmadd_pd (__m128d a, __m128d b, __m128d c)</para>
        ///   <para>  VFMADDPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VFMADDPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> MultiplyAdd(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_fmadd_ps (__m256 a, __m256 b, __m256 c)</para>
        ///   <para>  VFMADDPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VFMADDPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> MultiplyAdd(Vector256<float> a, Vector256<float> b, Vector256<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_fmadd_pd (__m256d a, __m256d b, __m256d c)</para>
        ///   <para>  VFMADDPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VFMADDPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> MultiplyAdd(Vector256<double> a, Vector256<double> b, Vector256<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_fmadd_ss (__m128 a, __m128 b, __m128 c)</para>
        ///   <para>  VFMADDSS xmm1,         xmm2, xmm3/m32</para>
        ///   <para>  VFMADDSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}</para>
        /// </summary>
        public static Vector128<float> MultiplyAddScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_fmadd_sd (__m128d a, __m128d b, __m128d c)</para>
        ///   <para>  VFMADDSD xmm1,         xmm2, xmm3/m64</para>
        ///   <para>  VFMADDSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}</para>
        /// </summary>
        public static Vector128<double> MultiplyAddScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_fmaddsub_ps (__m128 a, __m128 b, __m128 c)</para>
        ///   <para>  VFMADDSUBPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VFMADDSUBPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> MultiplyAddSubtract(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_fmaddsub_pd (__m128d a, __m128d b, __m128d c)</para>
        ///   <para>  VFMADDSUBPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VFMADDSUBPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> MultiplyAddSubtract(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_fmaddsub_ps (__m256 a, __m256 b, __m256 c)</para>
        ///   <para>  VFMADDSUBPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VFMADDSUBPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> MultiplyAddSubtract(Vector256<float> a, Vector256<float> b, Vector256<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_fmaddsub_pd (__m256d a, __m256d b, __m256d c)</para>
        ///   <para>  VFMADDSUBPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VFMADDSUBPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> MultiplyAddSubtract(Vector256<double> a, Vector256<double> b, Vector256<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_fmsub_ps (__m128 a, __m128 b, __m128 c)</para>
        ///   <para>  VFMSUBPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VFMSUBPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> MultiplySubtract(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_fmsub_pd (__m128d a, __m128d b, __m128d c)</para>
        ///   <para>  VFMSUBPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VFMSUBPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> MultiplySubtract(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_fmsub_ps (__m256 a, __m256 b, __m256 c)</para>
        ///   <para>  VFMSUBPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VFMSUBPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> MultiplySubtract(Vector256<float> a, Vector256<float> b, Vector256<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_fmsub_pd (__m256d a, __m256d b, __m256d c)</para>
        ///   <para>  VFMSUBPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VFMSUBPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> MultiplySubtract(Vector256<double> a, Vector256<double> b, Vector256<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_fmsub_ss (__m128 a, __m128 b, __m128 c)</para>
        ///   <para>  VFMSUBSS xmm1,         xmm2, xmm3/m32</para>
        ///   <para>  VFMSUBSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}</para>
        /// </summary>
        public static Vector128<float> MultiplySubtractScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_fmsub_sd (__m128d a, __m128d b, __m128d c)</para>
        ///   <para>  VFMSUBSD xmm1,         xmm2, xmm3/m64</para>
        ///   <para>  VFMSUBSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}</para>
        /// </summary>
        public static Vector128<double> MultiplySubtractScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_fmsubadd_ps (__m128 a, __m128 b, __m128 c)</para>
        ///   <para>  VFMSUBADDPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VFMSUBADDPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> MultiplySubtractAdd(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_fmsubadd_pd (__m128d a, __m128d b, __m128d c)</para>
        ///   <para>  VFMSUBADDPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VFMSUBADDPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> MultiplySubtractAdd(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_fmsubadd_ps (__m256 a, __m256 b, __m256 c)</para>
        ///   <para>  VFMSUBADDPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VFMSUBADDPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> MultiplySubtractAdd(Vector256<float> a, Vector256<float> b, Vector256<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_fmsubadd_pd (__m256d a, __m256d b, __m256d c)</para>
        ///   <para>  VFMSUBADDPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VFMSUBADDPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> MultiplySubtractAdd(Vector256<double> a, Vector256<double> b, Vector256<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_fnmadd_ps (__m128 a, __m128 b, __m128 c)</para>
        ///   <para>  VFNMADDPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VFNMADDPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> MultiplyAddNegated(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_fnmadd_pd (__m128d a, __m128d b, __m128d c)</para>
        ///   <para>  VFNMADDPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VFNMADDPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> MultiplyAddNegated(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_fnmadd_ps (__m256 a, __m256 b, __m256 c)</para>
        ///   <para>  VFNMADDPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VFNMADDPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> MultiplyAddNegated(Vector256<float> a, Vector256<float> b, Vector256<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_fnmadd_pd (__m256d a, __m256d b, __m256d c)</para>
        ///   <para>  VFNMADDPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VFNMADDPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> MultiplyAddNegated(Vector256<double> a, Vector256<double> b, Vector256<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_fnmadd_ss (__m128 a, __m128 b, __m128 c)</para>
        ///   <para>  VFNMADDSS xmm1,         xmm2, xmm3/m32</para>
        ///   <para>  VFNMADDSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}</para>
        /// </summary>
        public static Vector128<float> MultiplyAddNegatedScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_fnmadd_sd (__m128d a, __m128d b, __m128d c)</para>
        ///   <para>  VFNMADDSD xmm1,         xmm2, xmm3/m64</para>
        ///   <para>  VFNMADDSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}</para>
        /// </summary>
        public static Vector128<double> MultiplyAddNegatedScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_fnmsub_ps (__m128 a, __m128 b, __m128 c)</para>
        ///   <para>  VFNMSUBPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VFNMSUBPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> MultiplySubtractNegated(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_fnmsub_pd (__m128d a, __m128d b, __m128d c)</para>
        ///   <para>  VFNMSUBPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VFNMSUBPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> MultiplySubtractNegated(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_fnmsub_ps (__m256 a, __m256 b, __m256 c)</para>
        ///   <para>  VFNMSUBPS ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VFNMSUBPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<float> MultiplySubtractNegated(Vector256<float> a, Vector256<float> b, Vector256<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_fnmsub_pd (__m256d a, __m256d b, __m256d c)</para>
        ///   <para>  VFNMSUBPD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VFNMSUBPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<double> MultiplySubtractNegated(Vector256<double> a, Vector256<double> b, Vector256<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128 _mm_fnmsub_ss (__m128 a, __m128 b, __m128 c)</para>
        ///   <para>  VFNMSUBSS xmm1,         xmm2, xmm3/m32</para>
        ///   <para>  VFNMSUBSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}</para>
        /// </summary>
        public static Vector128<float> MultiplySubtractNegatedScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_fnmsub_sd (__m128d a, __m128d b, __m128d c)</para>
        ///   <para>  VFNMSUBSD xmm1,         xmm2, xmm3/m64</para>
        ///   <para>  VFNMSUBSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}</para>
        /// </summary>
        public static Vector128<double> MultiplySubtractNegatedScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }
    }
}
