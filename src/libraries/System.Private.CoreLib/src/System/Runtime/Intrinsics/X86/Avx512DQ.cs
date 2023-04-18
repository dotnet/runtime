// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>This class provides access to X86 AVX512DQ hardware instructions via intrinsics</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Avx512DQ : Avx512F
    {
        internal Avx512DQ() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class VL : Avx512F.VL
        {
            internal VL() { }

            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            /// __m128i _mm_mullo_epi64 (__m128i a, __m128i b)
            ///   VPMULLQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<long> MultiplyLow(Vector128<long> left, Vector128<long> right) => MultiplyLow(left, right);
            /// <summary>
            /// __m128i _mm_mullo_epi64 (__m128i a, __m128i b)
            ///   VPMULLQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<ulong> MultiplyLow(Vector128<ulong> left, Vector128<ulong> right) => MultiplyLow(left, right);
            /// <summary>
            /// __m256i _mm256_mullo_epi64 (__m256i a, __m256i b)
            ///   VPMULLQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<long> MultiplyLow(Vector256<long> left, Vector256<long> right) => MultiplyLow(left, right);
            /// <summary>
            /// __m256i _mm256_mullo_epi64 (__m256i a, __m256i b)
            ///   VPMULLQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<ulong> MultiplyLow(Vector256<ulong> left, Vector256<ulong> right) => MultiplyLow(left, right);
        }

        [Intrinsic]
        public new abstract class X64 : Avx512F.X64
        {
            internal X64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        /// <summary>
        /// __m512 _mm512_and_ps (__m512 a, __m512 b)
        ///   VANDPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> And(Vector512<float> left, Vector512<float> right) => And(left, right);
        /// <summary>
        /// __m512d _mm512_and_pd (__m512d a, __m512d b)
        ///   VANDPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> And(Vector512<double> left, Vector512<double> right) => And(left, right);

        /// <summary>
        /// __m512 _mm512_andnot_ps (__m512 a, __m512 b)
        ///   VANDNPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> AndNot(Vector512<float> left, Vector512<float> right) => AndNot(left, right);
        /// <summary>
        /// __m512d _mm512_andnot_pd (__m512d a, __m512d b)
        ///   VANDNPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> AndNot(Vector512<double> left, Vector512<double> right) => AndNot(left, right);

        /// <summary>
        /// __m512i _mm512_mullo_epi64 (__m512i a, __m512i b)
        ///   VPMULLQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> MultiplyLow(Vector512<long> left, Vector512<long> right) => MultiplyLow(left, right);
        /// <summary>
        /// __m512i _mm512_mullo_epi64 (__m512i a, __m512i b)
        ///   VPMULLQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> MultiplyLow(Vector512<ulong> left, Vector512<ulong> right) => MultiplyLow(left, right);

        /// <summary>
        /// __m512 _mm512_or_ps (__m512 a, __m512 b)
        ///   VORPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> Or(Vector512<float> left, Vector512<float> right) => Or(left, right);
        /// <summary>
        /// __m512d _mm512_or_pd (__m512d a, __m512d b)
        ///   VORPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> Or(Vector512<double> left, Vector512<double> right) => Or(left, right);

        /// <summary>
        /// __m512 _mm512_xor_ps (__m512 a, __m512 b)
        ///   VXORPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> Xor(Vector512<float> left, Vector512<float> right) => Xor(left, right);
        /// <summary>
        /// __m512d _mm512_xor_pd (__m512d a, __m512d b)
        ///   VXORPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> Xor(Vector512<double> left, Vector512<double> right) => Xor(left, right);
    }
}
