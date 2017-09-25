// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel FMA hardware instructions via intrinsics
    /// </summary>
    public static class Fma
    {
        public static bool IsSupported { get => IsSupported; }

        /// <summary>
        /// __m128 _mm_fmadd_ps (__m128 a, __m128 b, __m128 c)
        /// </summary>
        public static Vector128<float> MultiplyAdd(Vector128<float> a, Vector128<float> b, Vector128<float> c) => MultiplyAdd(a, b, c);
        /// <summary>
        /// __m128d _mm_fmadd_pd (__m128d a, __m128d b, __m128d c)
        /// </summary>
        public static Vector128<double> MultiplyAdd(Vector128<double> a, Vector128<double> b, Vector128<double> c) => MultiplyAdd(a, b, c);
        /// <summary>
        /// __m256 _mm256_fmadd_ps (__m256 a, __m256 b, __m256 c)
        /// </summary>
        public static Vector256<float> MultiplyAdd(Vector256<float> a, Vector256<float> b, Vector256<float> c) => MultiplyAdd(a, b, c);
        /// <summary>
        /// __m256d _mm256_fmadd_pd (__m256d a, __m256d b, __m256d c)
        /// </summary>
        public static Vector256<double> MultiplyAdd(Vector256<double> a, Vector256<double> b, Vector256<double> c) => MultiplyAdd(a, b, c);

        /// <summary>
        /// __m128 _mm_fmaddsub_ps (__m128 a, __m128 b, __m128 c)
        /// </summary>
        public static Vector128<float> MultiplyAddSubtract(Vector128<float> a, Vector128<float> b, Vector128<float> c) => MultiplyAddSubtract(a, b, c);
        /// <summary>
        /// __m128d _mm_fmaddsub_pd (__m128d a, __m128d b, __m128d c)
        /// </summary>
        public static Vector128<double> MultiplyAddSubtract(Vector128<double> a, Vector128<double> b, Vector128<double> c) => MultiplyAddSubtract(a, b, c);
        /// <summary>
        /// __m256 _mm256_fmaddsub_ps (__m256 a, __m256 b, __m256 c)
        /// </summary>
        public static Vector256<float> MultiplyAddSubtract(Vector256<float> a, Vector256<float> b, Vector256<float> c) => MultiplyAddSubtract(a, b, c);
        /// <summary>
        /// __m256d _mm256_fmaddsub_pd (__m256d a, __m256d b, __m256d c)
        /// </summary>
        public static Vector256<double> MultiplyAddSubtract(Vector256<double> a, Vector256<double> b, Vector256<double> c) => MultiplyAddSubtract(a, b, c);

        /// <summary>
        /// __m128 _mm_fmsub_ps (__m128 a, __m128 b, __m128 c)
        /// </summary>
        public static Vector128<float> MultiplySubtract(Vector128<float> a, Vector128<float> b, Vector128<float> c) => MultiplySubtract(a, b, c);
        /// <summary>
        /// __m128d _mm_fmsub_pd (__m128d a, __m128d b, __m128d c)
        /// </summary>
        public static Vector128<double> MultiplySubtract(Vector128<double> a, Vector128<double> b, Vector128<double> c) => MultiplySubtract(a, b, c);
        /// <summary>
        /// __m256 _mm256_fmsub_ps (__m256 a, __m256 b, __m256 c)
        /// </summary>
        public static Vector256<float> MultiplySubtract(Vector256<float> a, Vector256<float> b, Vector256<float> c) => MultiplySubtract(a, b, c);
        /// <summary>
        /// __m256d _mm256_fmsub_pd (__m256d a, __m256d b, __m256d c)
        /// </summary>
        public static Vector256<double> MultiplySubtract(Vector256<double> a, Vector256<double> b, Vector256<double> c) => MultiplySubtract(a, b, c);

        /// <summary>
        /// __m128 _mm_fmsubadd_ps (__m128 a, __m128 b, __m128 c)
        /// </summary>
        public static Vector128<float> MultiplySubtractAdd(Vector128<float> a, Vector128<float> b, Vector128<float> c) => MultiplySubtractAdd(a, b, c);
        /// <summary>
        /// __m128d _mm_fmsubadd_pd (__m128d a, __m128d b, __m128d c)
        /// </summary>
        public static Vector128<double> MultiplySubtractAdd(Vector128<double> a, Vector128<double> b, Vector128<double> c) => MultiplySubtractAdd(a, b, c);
        /// <summary>
        /// __m256 _mm256_fmsubadd_ps (__m256 a, __m256 b, __m256 c)
        /// </summary>
        public static Vector256<float> MultiplySubtractAdd(Vector256<float> a, Vector256<float> b, Vector256<float> c) => MultiplySubtractAdd(a, b, c);
        /// <summary>
        /// __m256d _mm256_fmsubadd_pd (__m256d a, __m256d b, __m256d c)
        /// </summary>
        public static Vector256<double> MultiplySubtractAdd(Vector256<double> a, Vector256<double> b, Vector256<double> c) => MultiplySubtractAdd(a, b, c);

        /// <summary>
        /// __m128 _mm_fnmadd_ps (__m128 a, __m128 b, __m128 c)
        /// </summary>
        public static Vector128<float> MultiplyAddNegated(Vector128<float> a, Vector128<float> b, Vector128<float> c) => MultiplyAddNegated(a, b, c);
        /// <summary>
        /// __m128d _mm_fnmadd_pd (__m128d a, __m128d b, __m128d c)
        /// </summary>
        public static Vector128<double> MultiplyAddNegated(Vector128<double> a, Vector128<double> b, Vector128<double> c) => MultiplyAddNegated(a, b, c);
        /// <summary>
        /// __m256 _mm256_fnmadd_ps (__m256 a, __m256 b, __m256 c)
        /// </summary>
        public static Vector256<float> MultiplyAddNegated(Vector256<float> a, Vector256<float> b, Vector256<float> c) => MultiplyAddNegated(a, b, c);
        /// <summary>
        /// __m256d _mm256_fnmadd_pd (__m256d a, __m256d b, __m256d c)
        /// </summary>
        public static Vector256<double> MultiplyAddNegated(Vector256<double> a, Vector256<double> b, Vector256<double> c) => MultiplyAddNegated(a, b, c);

        /// <summary>
        /// __m128 _mm_fnmsub_ps (__m128 a, __m128 b, __m128 c)
        /// </summary>
        public static Vector128<float> MultiplySubtractNegated(Vector128<float> a, Vector128<float> b, Vector128<float> c) => MultiplySubtractNegated(a, b, c);
        /// <summary>
        /// __m128d _mm_fnmsub_pd (__m128d a, __m128d b, __m128d c)
        /// </summary>
        public static Vector128<double> MultiplySubtractNegated(Vector128<double> a, Vector128<double> b, Vector128<double> c) => MultiplySubtractNegated(a, b, c);
        /// <summary>
        /// __m256 _mm256_fnmsub_ps (__m256 a, __m256 b, __m256 c)
        /// </summary>
        public static Vector256<float> MultiplySubtractNegated(Vector256<float> a, Vector256<float> b, Vector256<float> c) => MultiplySubtractNegated(a, b, c);
        /// <summary>
        /// __m256d _mm256_fnmsub_pd (__m256d a, __m256d b, __m256d c)
        /// </summary>
        public static Vector256<double> MultiplySubtractNegated(Vector256<double> a, Vector256<double> b, Vector256<double> c) => MultiplySubtractNegated(a, b, c);
    }
}
