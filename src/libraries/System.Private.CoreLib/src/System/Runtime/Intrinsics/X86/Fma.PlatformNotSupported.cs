// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel FMA hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public abstract class Fma : Avx
    {
        internal Fma() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        public new abstract class X64 : Avx.X64
        {
            internal X64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        /// __m128 _mm_fmadd_ps (__m128 a, __m128 b, __m128 c)
        ///   VFMADDPS xmm1,         xmm2, xmm3/m128
        ///   VFMADDPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> MultiplyAdd(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_fmadd_pd (__m128d a, __m128d b, __m128d c)
        ///   VFMADDPD xmm1,         xmm2, xmm3/m128
        ///   VFMADDPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<double> MultiplyAdd(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256 _mm256_fmadd_ps (__m256 a, __m256 b, __m256 c)
        ///   VFMADDPS ymm1,         ymm2, ymm3/m256
        ///   VFMADDPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        /// </summary>
        public static Vector256<float> MultiplyAdd(Vector256<float> a, Vector256<float> b, Vector256<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_fmadd_pd (__m256d a, __m256d b, __m256d c)
        ///   VFMADDPD ymm1,         ymm2, ymm3/m256
        ///   VFMADDPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<double> MultiplyAdd(Vector256<double> a, Vector256<double> b, Vector256<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fmadd_ss (__m128 a, __m128 b, __m128 c)
        ///   VFMADDSS xmm1,         xmm2, xmm3/m32
        ///   VFMADDSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}
        /// </summary>
        public static Vector128<float> MultiplyAddScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_fmadd_sd (__m128d a, __m128d b, __m128d c)
        ///   VFMADDSD xmm1,         xmm2, xmm3/m64
        ///   VFMADDSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}
        /// </summary>
        public static Vector128<double> MultiplyAddScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fmaddsub_ps (__m128 a, __m128 b, __m128 c)
        ///   VFMADDSUBPS xmm1,         xmm2, xmm3/m128
        ///   VFMADDSUBPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> MultiplyAddSubtract(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_fmaddsub_pd (__m128d a, __m128d b, __m128d c)
        ///   VFMADDSUBPD xmm1,         xmm2, xmm3/m128
        ///   VFMADDSUBPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<double> MultiplyAddSubtract(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256 _mm256_fmaddsub_ps (__m256 a, __m256 b, __m256 c)
        ///   VFMADDSUBPS ymm1,         ymm2, ymm3/m256
        ///   VFMADDSUBPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        /// </summary>
        public static Vector256<float> MultiplyAddSubtract(Vector256<float> a, Vector256<float> b, Vector256<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_fmaddsub_pd (__m256d a, __m256d b, __m256d c)
        ///   VFMADDSUBPD ymm1,         ymm2, ymm3/m256
        ///   VFMADDSUBPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<double> MultiplyAddSubtract(Vector256<double> a, Vector256<double> b, Vector256<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fmsub_ps (__m128 a, __m128 b, __m128 c)
        ///   VFMSUBPS xmm1,         xmm2, xmm3/m128
        ///   VFMSUBPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> MultiplySubtract(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_fmsub_pd (__m128d a, __m128d b, __m128d c)
        ///   VFMSUBPD xmm1,         xmm2, xmm3/m128
        ///   VFMSUBPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<double> MultiplySubtract(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256 _mm256_fmsub_ps (__m256 a, __m256 b, __m256 c)
        ///   VFMSUBPS ymm1,         ymm2, ymm3/m256
        ///   VFMSUBPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        /// </summary>
        public static Vector256<float> MultiplySubtract(Vector256<float> a, Vector256<float> b, Vector256<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_fmsub_pd (__m256d a, __m256d b, __m256d c)
        ///   VFMSUBPD ymm1,         ymm2, ymm3/m256
        ///   VFMSUBPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<double> MultiplySubtract(Vector256<double> a, Vector256<double> b, Vector256<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fmsub_ss (__m128 a, __m128 b, __m128 c)
        ///   VFMSUBSS xmm1,         xmm2, xmm3/m32
        ///   VFMSUBSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}
        /// </summary>
        public static Vector128<float> MultiplySubtractScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_fmsub_sd (__m128d a, __m128d b, __m128d c)
        ///   VFMSUBSD xmm1,         xmm2, xmm3/m64
        ///   VFMSUBSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}
        /// </summary>
        public static Vector128<double> MultiplySubtractScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fmsubadd_ps (__m128 a, __m128 b, __m128 c)
        ///   VFMSUBADDPS xmm1,         xmm2, xmm3/m128
        ///   VFMSUBADDPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> MultiplySubtractAdd(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_fmsubadd_pd (__m128d a, __m128d b, __m128d c)
        ///   VFMSUBADDPD xmm1,         xmm2, xmm3/m128
        ///   VFMSUBADDPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<double> MultiplySubtractAdd(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256 _mm256_fmsubadd_ps (__m256 a, __m256 b, __m256 c)
        ///   VFMSUBADDPS ymm1,         ymm2, ymm3/m256
        ///   VFMSUBADDPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        /// </summary>
        public static Vector256<float> MultiplySubtractAdd(Vector256<float> a, Vector256<float> b, Vector256<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_fmsubadd_pd (__m256d a, __m256d b, __m256d c)
        ///   VFMSUBADDPD ymm1,         ymm2, ymm3/m256
        ///   VFMSUBADDPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<double> MultiplySubtractAdd(Vector256<double> a, Vector256<double> b, Vector256<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fnmadd_ps (__m128 a, __m128 b, __m128 c)
        ///   VFNMADDPS xmm1,         xmm2, xmm3/m128
        ///   VFNMADDPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> MultiplyAddNegated(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_fnmadd_pd (__m128d a, __m128d b, __m128d c)
        ///   VFNMADDPD xmm1,         xmm2, xmm3/m128
        ///   VFNMADDPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<double> MultiplyAddNegated(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256 _mm256_fnmadd_ps (__m256 a, __m256 b, __m256 c)
        ///   VFNMADDPS ymm1,         ymm2, ymm3/m256
        ///   VFNMADDPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        /// </summary>
        public static Vector256<float> MultiplyAddNegated(Vector256<float> a, Vector256<float> b, Vector256<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_fnmadd_pd (__m256d a, __m256d b, __m256d c)
        ///   VFNMADDPD ymm1,         ymm2, ymm3/m256
        ///   VFNMADDPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<double> MultiplyAddNegated(Vector256<double> a, Vector256<double> b, Vector256<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fnmadd_ss (__m128 a, __m128 b, __m128 c)
        ///   VFNMADDSS xmm1,         xmm2, xmm3/m32
        ///   VFNMADDSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}
        /// </summary>
        public static Vector128<float> MultiplyAddNegatedScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_fnmadd_sd (__m128d a, __m128d b, __m128d c)
        ///   VFNMADDSD xmm1,         xmm2, xmm3/m64
        ///   VFNMADDSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}
        /// </summary>
        public static Vector128<double> MultiplyAddNegatedScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fnmsub_ps (__m128 a, __m128 b, __m128 c)
        ///   VFNMSUBPS xmm1,         xmm2, xmm3/m128
        ///   VFNMSUBPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> MultiplySubtractNegated(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_fnmsub_pd (__m128d a, __m128d b, __m128d c)
        ///   VFNMSUBPD xmm1,         xmm2, xmm3/m128
        ///   VFNMSUBPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<double> MultiplySubtractNegated(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256 _mm256_fnmsub_ps (__m256 a, __m256 b, __m256 c)
        ///   VFNMSUBPS ymm1,         ymm2, ymm3/m256
        ///   VFNMSUBPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        /// </summary>
        public static Vector256<float> MultiplySubtractNegated(Vector256<float> a, Vector256<float> b, Vector256<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_fnmsub_pd (__m256d a, __m256d b, __m256d c)
        ///   VFNMSUBPD ymm1,         ymm2, ymm3/m256
        ///   VFNMSUBPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<double> MultiplySubtractNegated(Vector256<double> a, Vector256<double> b, Vector256<double> c) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fnmsub_ss (__m128 a, __m128 b, __m128 c)
        ///   VFNMSUBSS xmm1,         xmm2, xmm3/m32
        ///   VFNMSUBSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}
        /// </summary>
        public static Vector128<float> MultiplySubtractNegatedScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_fnmsub_sd (__m128d a, __m128d b, __m128d c)
        ///   VFNMSUBSD xmm1,         xmm2, xmm3/m64
        ///   VFNMSUBSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}
        /// </summary>
        public static Vector128<double> MultiplySubtractNegatedScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c) { throw new PlatformNotSupportedException(); }
    }
}
