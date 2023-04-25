// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
            /// __m128i _mm_broadcast_i32x2 (__m128i a)
            ///   VBROADCASTI32x2 xmm1 {k1}{z}, xmm2/m64
            /// </summary>
            public static Vector128<int> BroadcastPairScalarToVector128(Vector128<int> value) => BroadcastPairScalarToVector128(value);
            /// <summary>
            /// __m128i _mm_broadcast_i32x2 (__m128i a)
            ///   VBROADCASTI32x2 xmm1 {k1}{z}, xmm2/m64
            /// </summary>
            public static Vector128<uint> BroadcastPairScalarToVector128(Vector128<uint> value) => BroadcastPairScalarToVector128(value);
            /// <summary>
            /// __m128 _mm_broadcast_f32x2 (__m128 a)
            ///   VBROADCASTF32x2 xmm1 {k1}{z}, xmm2/m64
            /// </summary>
            public static Vector128<float> BroadcastPairScalarToVector128(Vector128<float> value) => BroadcastPairScalarToVector128(value);

            /// <summary>
            /// __m256i _mm256_broadcast_i32x2 (__m128i a)
            ///   VBROADCASTI32x2 ymm1 {k1}{z}, xmm2/m64
            /// </summary>
            public static Vector256<int> BroadcastPairScalarToVector256(Vector128<int> value) => BroadcastPairScalarToVector256(value);
            /// <summary>
            /// __m256i _mm256_broadcast_i32x2 (__m128i a)
            ///   VBROADCASTI32x2 ymm1 {k1}{z}, xmm2/m64
            /// </summary>
            public static Vector256<uint> BroadcastPairScalarToVector256(Vector128<uint> value) => BroadcastPairScalarToVector256(value);
            /// <summary>
            /// __m256 _mm256_broadcast_f32x2 (__m128 a)
            ///   VBROADCASTF32x2 ymm1 {k1}{z}, xmm2/m64
            /// </summary>
            public static Vector256<float> BroadcastPairScalarToVector256(Vector128<float> value) => BroadcastPairScalarToVector256(value);

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
        /// __m512i _mm512_broadcast_i32x2 (__m128i a)
        ///   VBROADCASTI32x2 zmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector512<int> BroadcastPairScalarToVector512(Vector128<int> value) => BroadcastPairScalarToVector512(value);
        /// <summary>
        /// __m512i _mm512_broadcast_i32x2 (__m128i a)
        ///   VBROADCASTI32x2 zmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector512<uint> BroadcastPairScalarToVector512(Vector128<uint> value) => BroadcastPairScalarToVector512(value);
        /// <summary>
        /// __m512 _mm512_broadcast_f32x2 (__m128 a)
        ///   VBROADCASTF32x2 zmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector512<float> BroadcastPairScalarToVector512(Vector128<float> value) => BroadcastPairScalarToVector512(value);

        /// <summary>
        /// __m512i _mm512_broadcast_i64x2 (__m128i const * mem_addr)
        ///   VBROADCASTI64x2 zmm1 {k1}{z}, m128
        /// </summary>
        public static unsafe Vector512<long> BroadcastVector128ToVector512(long* address) => BroadcastVector128ToVector512(address);
        /// <summary>
        /// __m512i _mm512_broadcast_i64x2 (__m128i const * mem_addr)
        ///   VBROADCASTI64x2 zmm1 {k1}{z}, m128
        /// </summary>
        public static unsafe Vector512<ulong> BroadcastVector128ToVector512(ulong* address) => BroadcastVector128ToVector512(address);
        /// <summary>
        /// __m512d _mm512_broadcast_f64x2 (__m128d const * mem_addr)
        ///   VBROADCASTF64x2 zmm1 {k1}{z}, m128
        /// </summary>
        public static unsafe Vector512<double> BroadcastVector128ToVector512(double* address) => BroadcastVector128ToVector512(address);

        /// <summary>
        /// __m512i _mm512_broadcast_i32x8 (__m256i const * mem_addr)
        ///   VBROADCASTI32x8 zmm1 {k1}{z}, m256
        /// </summary>
        public static unsafe Vector512<int> BroadcastVector256ToVector512(int* address) => BroadcastVector256ToVector512(address);
        /// <summary>
        /// __m512i _mm512_broadcast_i32x8 (__m256i const * mem_addr)
        ///   VBROADCASTI32x8 zmm1 {k1}{z}, m256
        /// </summary>
        public static unsafe Vector512<uint> BroadcastVector256ToVector512(uint* address) => BroadcastVector256ToVector512(address);
        /// <summary>
        /// __m512 _mm512_broadcast_f32x8 (__m256 const * mem_addr)
        ///   VBROADCASTF32x8 zmm1 {k1}{z}, m256
        /// </summary>
        public static unsafe Vector512<float> BroadcastVector256ToVector512(float* address) => BroadcastVector256ToVector512(address);

        /// <summary>
        /// __m128i _mm512_extracti64x2_epi64 (__m512i a, const int imm8)
        ///   VEXTRACTI64x2 xmm1/m128 {k1}{z}, zmm2, imm8
        /// </summary>
        public static new Vector128<long> ExtractVector128(Vector512<long> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        /// __m128i _mm512_extracti64x2_epi64 (__m512i a, const int imm8)
        ///   VEXTRACTI64x2 xmm1/m128 {k1}{z}, zmm2, imm8
        /// </summary>
        public static new Vector128<ulong> ExtractVector128(Vector512<ulong> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        /// __m128d _mm512_extractf64x2_pd (__m512d a, const int imm8)
        ///   VEXTRACTF64x2 xmm1/m128 {k1}{z}, zmm2, imm8
        /// </summary>
        public static new Vector128<double> ExtractVector128(Vector512<double> value, [ConstantExpected] byte index) => ExtractVector128(value, index);

        /// <summary>
        /// __m256i _mm512_extracti32x8_epi32 (__m512i a, const int imm8)
        ///   VEXTRACTI32x8 ymm1/m256 {k1}{z}, zmm2, imm8
        /// </summary>
        public static new Vector256<int> ExtractVector256(Vector512<int> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        /// __m256i _mm512_extracti32x8_epi32 (__m512i a, const int imm8)
        ///   VEXTRACTI32x8 ymm1/m256 {k1}{z}, zmm2, imm8
        /// </summary>
        public static new Vector256<uint> ExtractVector256(Vector512<uint> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        /// __m256 _mm512_extractf32x8_ps (__m512 a, const int imm8)
        ///   VEXTRACTF32x8 ymm1/m256 {k1}{z}, zmm2, imm8
        /// </summary>
        public static new Vector256<float> ExtractVector256(Vector512<float> value, [ConstantExpected] byte index) => ExtractVector256(value, index);

        /// <summary>
        /// __m512i _mm512_inserti64x2_si512 (__m512i a, __m128i b, const int imm8)
        ///   VINSERTI64x2 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
        /// </summary>
        public static new Vector512<long> InsertVector128(Vector512<long> value, Vector128<long> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti64x2_si512 (__m512i a, __m128i b, const int imm8)
        ///   VINSERTI64x2 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
        /// </summary>
        public static new Vector512<ulong> InsertVector128(Vector512<ulong> value, Vector128<ulong> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        /// __m512d _mm512_insertf64x2_pd (__m512d a, __m128d b, int imm8)
        ///   VINSERTF64x2 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
        /// </summary>
        public static new Vector512<double> InsertVector128(Vector512<double> value, Vector128<double> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);

        /// <summary>
        /// __m512i _mm512_inserti32x8_si512 (__m512i a, __m256i b, const int imm8)
        ///   VINSERTI32x8 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
        /// </summary>
        public static new Vector512<int> InsertVector256(Vector512<int> value, Vector256<int> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti32x8_si512 (__m512i a, __m256i b, const int imm8)
        ///   VINSERTI32x8 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
        /// </summary>
        public static new Vector512<uint> InsertVector256(Vector512<uint> value, Vector256<uint> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        /// __m512 _mm512_insertf32x8_ps (__m512 a, __m256 b, int imm8)
        ///   VINSERTF32x8 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
        /// </summary>
        public static new Vector512<float> InsertVector256(Vector512<float> value, Vector256<float> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);

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
