// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>This class provides access to X86 AVX512F hardware instructions via intrinsics</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Avx512F : Avx2
    {
        internal Avx512F() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public abstract class VL
        {
            internal VL() { }

            public static bool IsSupported { get => IsSupported; }

            /// <summary>
            /// __m128i _mm_abs_epi64 (__m128i a)
            ///   VPABSQ xmm1 {k1}{z}, xmm2/m128/m64bcst
            /// </summary>
            public static Vector128<ulong> Abs(Vector128<long> value) => Abs(value);
            /// <summary>
            /// __m256i _mm256_abs_epi64 (__m128i a)
            ///   VPABSQ ymm1 {k1}{z}, ymm2/m256/m64bcst
            /// </summary>
            public static Vector256<ulong> Abs(Vector256<long> value) => Abs(value);

            /// <summary>
            /// __m128i _mm_max_epi64 (__m128i a, __m128i b)
            ///   VPMAXSQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<long> Max(Vector128<long> left, Vector128<long> right) => Max(left, right);
            /// <summary>
            /// __m128i _mm_max_epu64 (__m128i a, __m128i b)
            ///   VPMAXUQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<ulong> Max(Vector128<ulong> left, Vector128<ulong> right) => Max(left, right);
            /// <summary>
            /// __m256i _mm256_max_epi64 (__m256i a, __m256i b)
            ///   VPMAXSQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<long> Max(Vector256<long> left, Vector256<long> right) => Max(left, right);
            /// <summary>
            /// __m256i _mm256_max_epu64 (__m256i a, __m256i b)
            ///   VPMAXUQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<ulong> Max(Vector256<ulong> left, Vector256<ulong> right) => Max(left, right);

            /// <summary>
            /// __m128i _mm_min_epi64 (__m128i a, __m128i b)
            ///   VPMINSQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<long> Min(Vector128<long> left, Vector128<long> right) => Min(left, right);
            /// <summary>
            /// __m128i _mm_min_epu64 (__m128i a, __m128i b)
            ///   VPMINUQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<ulong> Min(Vector128<ulong> left, Vector128<ulong> right) => Min(left, right);
            /// <summary>
            /// __m256i _mm256_min_epi64 (__m256i a, __m256i b)
            ///   VPMINSQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<long> Min(Vector256<long> left, Vector256<long> right) => Min(left, right);
            /// <summary>
            /// __m256i _mm256_min_epu64 (__m256i a, __m256i b)
            ///   VPMINUQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<ulong> Min(Vector256<ulong> left, Vector256<ulong> right) => Min(left, right);

            /// <summary>
            /// __m256i _mm256_permute4x64_epi64 (__m256i a, __m256i b)
            ///   VPERMQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<long> PermuteVar4x64(Vector256<long> value, Vector256<long> control) => PermuteVar4x64(value, control);
            /// <summary>
            /// __m256i _mm256_permute4x64_pd (__m256d a, __m256i b)
            ///   VPERMQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<ulong> PermuteVar4x64(Vector256<ulong> value, Vector256<ulong> control) => PermuteVar4x64(value, control);
            /// <summary>
            /// __m256d _mm256_permute4x64_pd (__m256d a, __m256i b)
            ///   VPERMPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<double> PermuteVar4x64(Vector256<double> value, Vector256<long> control) => PermuteVar4x64(value, control);

            /// <summary>
            /// __m128i _mm_sra_epi64 (__m128i a, __m128i count)
            ///   VPSRAQ xmm1 {k1}{z}, xmm2, xmm3/m128
            /// </summary>
            public static Vector128<long> ShiftRightArithmetic(Vector128<long> value, Vector128<long> count) => ShiftRightArithmetic(value, count);
            /// <summary>
            /// __m256i _mm256_sra_epi64 (__m256i a, __m128i count)
            ///   VPSRAQ ymm1 {k1}{z}, ymm2, xmm3/m128
            /// </summary>
            public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, Vector128<long> count) => ShiftRightArithmetic(value, count);

            /// <summary>
            /// __128i _mm_srai_epi64 (__m128i a, int imm8)
            ///   VPSRAQ xmm1 {k1}{z}, xmm2, imm8
            /// </summary>
            public static Vector128<long> ShiftRightArithmetic(Vector128<long> value, [ConstantExpected] byte count) => ShiftRightArithmetic(value, count);
            /// <summary>
            /// __m256i _mm256_srai_epi64 (__m256i a, int imm8)
            ///   VPSRAQ ymm1 {k1}{z}, ymm2, imm8
            /// </summary>
            public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, [ConstantExpected] byte count) => ShiftRightArithmetic(value, count);

            /// <summary>
            /// __m128i _mm_srav_epi64 (__m128i a, __m128i count)
            ///   VPSRAVQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<long> ShiftRightArithmeticVariable(Vector128<long> value, Vector128<ulong> count) => ShiftRightArithmeticVariable(value, count);
            /// <summary>
            /// __m256i _mm256_srav_epi64 (__m256i a, __m256i count)
            ///   VPSRAVQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<long> ShiftRightArithmeticVariable(Vector256<long> value, Vector256<ulong> count) => ShiftRightArithmeticVariable(value, count);
        }

        [Intrinsic]
        public new abstract class X64 : Avx2.X64
        {
            internal X64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        /// <summary>
        /// __m512i _mm512_abs_epi32 (__m512i a)
        ///   VPABSD zmm1 {k1}{z}, zmm2/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Abs(Vector512<int> value) => Abs(value);
        /// <summary>
        /// __m512i _mm512_abs_epi64 (__m512i a)
        ///   VPABSQ zmm1 {k1}{z}, zmm2/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Abs(Vector512<long> value) => Abs(value);

        /// <summary>
        /// __m512i _mm512_add_epi32 (__m512i a, __m512i b)
        ///   VPADDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> Add(Vector512<int> left, Vector512<int> right) => Add(left, right);
        /// <summary>
        /// __m512i _mm512_add_epi32 (__m512i a, __m512i b)
        ///   VPADDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Add(Vector512<uint> left, Vector512<uint> right) => Add(left, right);
        /// <summary>
        /// __m512i _mm512_add_epi64 (__m512i a, __m512i b)
        ///   VPADDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Add(Vector512<long> left, Vector512<long> right) => Add(left, right);
        /// <summary>
        /// __m512i _mm512_add_epi64 (__m512i a, __m512i b)
        ///   VPADDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Add(Vector512<ulong> left, Vector512<ulong> right) => Add(left, right);
        /// <summary>
        /// __m512d _mm512_add_pd (__m512d a, __m512d b)
        ///   VADDPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{er}
        /// </summary>
        public static Vector512<double> Add(Vector512<double> left, Vector512<double> right) => Add(left, right);
        /// <summary>
        /// __m512 _mm512_add_ps (__m512 a, __m512 b)
        ///   VADDPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{er}
        /// </summary>
        public static Vector512<float> Add(Vector512<float> left, Vector512<float> right) => Add(left, right);

        /// <summary>
        /// __m512i _mm512_and_si512 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<byte> And(Vector512<byte> left, Vector512<byte> right) => And(left, right);
        /// <summary>
        /// __m512i _mm512_and_si512 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<sbyte> And(Vector512<sbyte> left, Vector512<sbyte> right) => And(left, right);
        /// <summary>
        /// __m512i _mm512_and_si512 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<short> And(Vector512<short> left, Vector512<short> right) => And(left, right);
        /// <summary>
        /// __m512i _mm512_and_si512 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<ushort> And(Vector512<ushort> left, Vector512<ushort> right) => And(left, right);
        /// <summary>
        /// __m512i _mm512_and_epi32 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> And(Vector512<int> left, Vector512<int> right) => And(left, right);
        /// <summary>
        /// __m512i _mm512_and_epi32 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> And(Vector512<uint> left, Vector512<uint> right) => And(left, right);
        /// <summary>
        /// __m512i _mm512_and_epi64 (__m512i a, __m512i b)
        ///   VPANDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> And(Vector512<long> left, Vector512<long> right) => And(left, right);
        /// <summary>
        /// __m512i _mm512_and_epi64 (__m512i a, __m512i b)
        ///   VPANDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> And(Vector512<ulong> left, Vector512<ulong> right) => And(left, right);

        /// <summary>
        /// __m512i _mm512_andnot_si512 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<byte> AndNot(Vector512<byte> left, Vector512<byte> right) => AndNot(left, right);
        /// <summary>
        /// __m512i _mm512_andnot_si512 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<sbyte> AndNot(Vector512<sbyte> left, Vector512<sbyte> right) => AndNot(left, right);
        /// <summary>
        /// __m512i _mm512_andnot_si512 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<short> AndNot(Vector512<short> left, Vector512<short> right) => AndNot(left, right);
        /// <summary>
        /// __m512i _mm512_andnot_si512 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<ushort> AndNot(Vector512<ushort> left, Vector512<ushort> right) => AndNot(left, right);
        /// <summary>
        /// __m512i _mm512_andnot_epi32 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> AndNot(Vector512<int> left, Vector512<int> right) => AndNot(left, right);
        /// <summary>
        /// __m512i _mm512_andnot_epi32 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> AndNot(Vector512<uint> left, Vector512<uint> right) => AndNot(left, right);
        /// <summary>
        /// __m512i _mm512_andnot_epi64 (__m512i a, __m512i b)
        ///   VPANDNQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> AndNot(Vector512<long> left, Vector512<long> right) => AndNot(left, right);
        /// <summary>
        /// __m512i _mm512_andnot_epi64 (__m512i a, __m512i b)
        ///   VPANDNQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> AndNot(Vector512<ulong> left, Vector512<ulong> right) => AndNot(left, right);

        /// <summary>
        /// __m512i _mm512_broadcastd_epi32 (__m128i a)
        ///   VPBROADCASTD zmm1 {k1}{z}, xmm2/m32
        /// </summary>
        public static Vector512<int> BroadcastScalarToVector512(Vector128<int> value) => BroadcastScalarToVector512(value);
        /// <summary>
        /// __m512i _mm512_broadcastd_epi32 (__m128i a)
        ///   VPBROADCASTD zmm1 {k1}{z}, xmm2/m32
        /// </summary>
        public static Vector512<uint> BroadcastScalarToVector512(Vector128<uint> value) => BroadcastScalarToVector512(value);
        /// <summary>
        /// __m512i _mm512_broadcastq_epi64 (__m128i a)
        ///   VPBROADCASTQ zmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector512<long> BroadcastScalarToVector512(Vector128<long> value) => BroadcastScalarToVector512(value);
        /// <summary>
        /// __m512i _mm512_broadcastq_epi64 (__m128i a)
        ///   VPBROADCASTQ zmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector512<ulong> BroadcastScalarToVector512(Vector128<ulong> value) => BroadcastScalarToVector512(value);
        /// <summary>
        /// __m512 _mm512_broadcastss_ps (__m128 a)
        ///   VBROADCASTSS zmm1 {k1}{z}, xmm2/m32
        /// </summary>
        public static Vector512<float> BroadcastScalarToVector512(Vector128<float> value) => BroadcastScalarToVector512(value);
        /// <summary>
        /// __m512d _mm512_broadcastsd_pd (__m128d a)
        ///   VBROADCASTSD zmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector512<double> BroadcastScalarToVector512(Vector128<double> value) => BroadcastScalarToVector512(value);

        /// <summary>
        /// __m512i _mm512_broadcast_i32x4 (__m128i const * mem_addr)
        ///   VBROADCASTI32x4 zmm1 {k1}{z}, m128
        /// </summary>
        public static unsafe Vector512<int> BroadcastVector128ToVector512(int* address) => BroadcastVector128ToVector512(address);
        /// <summary>
        /// __m512i _mm512_broadcast_i32x4 (__m128i const * mem_addr)
        ///   VBROADCASTI32x4 zmm1 {k1}{z}, m128
        /// </summary>
        public static unsafe Vector512<uint> BroadcastVector128ToVector512(uint* address) => BroadcastVector128ToVector512(address);
        /// <summary>
        /// __m512 _mm512_broadcast_f32x4 (__m128 const * mem_addr)
        ///   VBROADCASTF32x4 zmm1 {k1}{z}, m128
        /// </summary>
        public static unsafe Vector512<float> BroadcastVector128ToVector512(float* address) => BroadcastVector128ToVector512(address);

        /// <summary>
        /// __m512i _mm512_broadcast_i64x4 (__m256i const * mem_addr)
        ///   VBROADCASTI64x4 zmm1 {k1}{z}, m256
        /// </summary>
        public static unsafe Vector512<long> BroadcastVector256ToVector512(long* address) => BroadcastVector256ToVector512(address);
        /// <summary>
        /// __m512i _mm512_broadcast_i64x4 (__m256i const * mem_addr)
        ///   VBROADCASTI64x4 zmm1 {k1}{z}, m256
        /// </summary>
        public static unsafe Vector512<ulong> BroadcastVector256ToVector512(ulong* address) => BroadcastVector256ToVector512(address);
        /// <summary>
        /// __m512d _mm512_broadcast_f64x4 (__m256d const * mem_addr)
        ///   VBROADCASTF64x4 zmm1 {k1}{z}, m256
        /// </summary>
        public static unsafe Vector512<double> BroadcastVector256ToVector512(double* address) => BroadcastVector256ToVector512(address);

        /// <summary>
        /// __m256i _mm512_cvtpd_epi32 (__m512d a)
        ///   VCVTPD2DQ ymm1 {k1}{z}, zmm2/m512/m64bcst{er}
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector512<double> value) => ConvertToVector256Int32(value);
        /// <summary>
        /// __m256 _mm512_cvtpd_ps (__m512d a)
        ///   VCVTPD2PS ymm1,         zmm2/m512
        ///   VCVTPD2PS ymm1 {k1}{z}, zmm2/m512/m64bcst{er}
        /// </summary>
        public static Vector256<float> ConvertToVector256Single(Vector512<double> value) => ConvertToVector256Single(value);

        /// <summary>
        /// __m256i _mm512_cvttpd_epi32 (__m512d a)
        ///   VCVTTPD2DQ ymm1 {k1}{z}, zmm2/m512/m64bcst{sae}
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32WithTruncation(Vector512<double> value) => ConvertToVector256Int32WithTruncation(value);

        /// <summary>
        /// __m512d _mm512_cvtepi32_pd (__m256i a)
        ///   VCVTDQ2PD zmm1 {k1}{z}, ymm2/m256/m32bcst
        /// </summary>
        public static Vector512<double> ConvertToVector512Double(Vector256<int> value) => ConvertToVector512Double(value);
        /// <summary>
        /// __m512d _mm512_cvtps_pd (__m256 a)
        ///   VCVTPS2PD zmm1 {k1}{z}, ymm2/m256/m32bcst{sae}
        /// </summary>
        public static Vector512<double> ConvertToVector512Double(Vector256<float> value) => ConvertToVector512Double(value);
        /// <summary>
        /// __m512i _mm512_cvtepi8_epi32 (__m128i a)
        ///   VPMOVSXBD zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector128<sbyte> value) => ConvertToVector512Int32(value);
        /// <summary>
        /// __m512i _mm512_cvtepu8_epi32 (__m128i a)
        ///   VPMOVZXBD zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector128<byte> value) => ConvertToVector512Int32(value);
        /// <summary>
        /// __m512i _mm512_cvtepi16_epi32 (__m128i a)
        ///   VPMOVSXWD zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector256<short> value) => ConvertToVector512Int32(value);
        /// <summary>
        /// __m512i _mm512_cvtepu16_epi32 (__m128i a)
        ///   VPMOVZXWD zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector256<ushort> value) => ConvertToVector512Int32(value);
        /// <summary>
        /// __m512i _mm512_cvtps_epi32 (__m512 a)
        ///   VCVTPS2DQ zmm1 {k1}{z}, zmm2/m512/m32bcst{er}
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector512<float> value) => ConvertToVector512Int32(value);
        /// <summary>
        /// __m512i _mm512_cvtepi8_epi64 (__m128i a)
        ///   VPMOVSXBQ zmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector128<sbyte> value) => ConvertToVector512Int64(value);
        /// <summary>
        /// __m512i _mm512_cvtepu8_epi64 (__m128i a)
        ///   VPMOVZXBQ zmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector128<byte> value) => ConvertToVector512Int64(value);
        /// <summary>
        /// __m512i _mm512_cvtepi16_epi64 (__m128i a)
        ///   VPMOVSXWQ zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector128<short> value) => ConvertToVector512Int64(value);
        /// <summary>
        /// __m512i _mm512_cvtepu16_epi64 (__m128i a)
        ///   VPMOVZXWQ zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector128<ushort> value) => ConvertToVector512Int64(value);
        /// <summary>
        /// __m512i _mm512_cvtepi32_epi64 (__m128i a)
        ///   VPMOVSXDQ zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector256<int> value) => ConvertToVector512Int64(value);
        /// <summary>
        /// __m512i _mm512_cvtepu32_epi64 (__m128i a)
        ///   VPMOVZXDQ zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector256<uint> value) => ConvertToVector512Int64(value);
        /// <summary>
        /// __m512 _mm512_cvtepi32_ps (__m512i a)
        ///   VCVTDQ2PS zmm1 {k1}{z}, zmm2/m512/m32bcst{er}
        /// </summary>
        public static Vector512<float> ConvertToVector512Single(Vector512<int> value) => ConvertToVector512Single(value);
        /// <summary>
        /// __m512i _mm512_cvtepi8_epi32 (__m128i a)
        ///   VPMOVSXBD zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector128<sbyte> value) => ConvertToVector512UInt32(value);
        /// <summary>
        /// __m512i _mm512_cvtepu8_epi32 (__m128i a)
        ///   VPMOVZXBD zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector128<byte> value) => ConvertToVector512UInt32(value);
        /// <summary>
        /// __m512i _mm512_cvtepi16_epi32 (__m128i a)
        ///   VPMOVSXWD zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector256<short> value) => ConvertToVector512UInt32(value);
        /// <summary>
        /// __m512i _mm512_cvtepu16_epi32 (__m128i a)
        ///   VPMOVZXWD zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector256<ushort> value) => ConvertToVector512UInt32(value);
        /// <summary>
        /// __m512i _mm512_cvtepi8_epi64 (__m128i a)
        ///   VPMOVSXBQ zmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector128<sbyte> value) => ConvertToVector512UInt64(value);
        /// <summary>
        /// __m512i _mm512_cvtepu8_epi64 (__m128i a)
        ///   VPMOVZXBQ zmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector128<byte> value) => ConvertToVector512UInt64(value);
        /// <summary>
        /// __m512i _mm512_cvtepi16_epi64 (__m128i a)
        ///   VPMOVSXWQ zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector128<short> value) => ConvertToVector512UInt64(value);
        /// <summary>
        /// __m512i _mm512_cvtepu16_epi64 (__m128i a)
        ///   VPMOVZXWQ zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector128<ushort> value) => ConvertToVector512UInt64(value);
        /// <summary>
        /// __m512i _mm512_cvtepi32_epi64 (__m128i a)
        ///   VPMOVSXDQ zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector256<int> value) => ConvertToVector512UInt64(value);
        /// <summary>
        /// __m512i _mm512_cvtepu32_epi64 (__m128i a)
        ///   VPMOVZXDQ zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector256<uint> value) => ConvertToVector512UInt64(value);

        /// <summary>
        /// __m512i _mm512_cvttps_epi32 (__m512 a)
        ///   VCVTTPS2DQ zmm1 {k1}{z}, zmm2/m512/m32bcst{sae}
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32WithTruncation(Vector512<float> value) => ConvertToVector512Int32WithTruncation(value);

        /// <summary>
        /// __m512 _mm512_div_ps (__m512 a, __m512 b)
        ///   VDIVPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{er}
        /// </summary>
        public static Vector512<float> Divide(Vector512<float> left, Vector512<float> right) => Divide(left, right);
        /// <summary>
        /// __m512d _mm512_div_pd (__m512d a, __m512d b)
        ///   VDIVPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{er}
        /// </summary>
        public static Vector512<double> Divide(Vector512<double> left, Vector512<double> right) => Divide(left, right);

        /// <summary>
        /// __m512 _mm512_moveldup_ps (__m512 a)
        ///   VMOVSLDUP zmm1 {k1}{z}, zmm2/m512
        /// </summary>
        public static Vector512<float> DuplicateEvenIndexed(Vector512<float> value) => DuplicateEvenIndexed(value);
        /// <summary>
        /// __m512d _mm512_movedup_pd (__m512d a)
        ///   VMOVDDUP zmm1 {k1}{z}, zmm2/m512
        /// </summary>
        public static Vector512<double> DuplicateEvenIndexed(Vector512<double> value) => DuplicateEvenIndexed(value);
        /// <summary>
        /// __m512 _mm512_movehdup_ps (__m512 a)
        ///   VMOVSHDUP zmm1 {k1}{z}, zmm2/m512
        /// </summary>
        public static Vector512<float> DuplicateOddIndexed(Vector512<float> value) => DuplicateOddIndexed(value);

        /// <summary>
        /// __m128i _mm512_extracti128_si512 (__m512i a, const int imm8)
        ///   VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector128<sbyte> ExtractVector128(Vector512<sbyte> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        /// __m128i _mm512_extracti128_si512 (__m512i a, const int imm8)
        ///   VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector128<byte> ExtractVector128(Vector512<byte> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        /// __m128i _mm512_extracti128_si512 (__m512i a, const int imm8)
        ///   VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector128<short> ExtractVector128(Vector512<short> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        /// __m128i _mm512_extracti128_si512 (__m512i a, const int imm8)
        ///   VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector128<ushort> ExtractVector128(Vector512<ushort> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        /// __m128i _mm512_extracti32x4_epi32 (__m512i a, const int imm8)
        ///   VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector128<int> ExtractVector128(Vector512<int> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        /// __m128i _mm512_extracti32x4_epi32 (__m512i a, const int imm8)
        ///   VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector128<uint> ExtractVector128(Vector512<uint> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        /// __m128i _mm512_extracti128_si512 (__m512i a, const int imm8)
        ///   VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector128<long> ExtractVector128(Vector512<long> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        /// __m128i _mm512_extracti128_si512 (__m512i a, const int imm8)
        ///   VEXTRACTI32x4 xmm1/m128 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector128<ulong> ExtractVector128(Vector512<ulong> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        /// __m128 _mm512_extractf32x4_ps (__m512 a, const int imm8)
        ///   VEXTRACTF32x4 xmm1/m128 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector128<float> ExtractVector128(Vector512<float> value, [ConstantExpected] byte index) => ExtractVector128(value, index);
        /// <summary>
        /// __m128d _mm512_extractf128_pd (__m512d a, const int imm8)
        ///   VEXTRACTF32x4 xmm1/m128 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector128<double> ExtractVector128(Vector512<double> value, [ConstantExpected] byte index) => ExtractVector128(value, index);

        /// <summary>
        /// __m256i _mm512_extracti256_si512 (__m512i a, const int imm8)
        ///   VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector256<sbyte> ExtractVector256(Vector512<sbyte> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        /// __m256i _mm512_extracti256_si512 (__m512i a, const int imm8)
        ///   VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector256<byte> ExtractVector256(Vector512<byte> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        /// __m256i _mm512_extracti256_si512 (__m512i a, const int imm8)
        ///   VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector256<short> ExtractVector256(Vector512<short> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        /// __m256i _mm512_extracti256_si512 (__m512i a, const int imm8)
        ///   VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector256<ushort> ExtractVector256(Vector512<ushort> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        /// __m256i _mm512_extracti256_si512 (__m512i a, const int imm8)
        ///   VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector256<int> ExtractVector256(Vector512<int> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        /// __m256i _mm512_extracti256_si512 (__m512i a, const int imm8)
        ///   VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector256<uint> ExtractVector256(Vector512<uint> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        /// __m256i _mm512_extracti64x4_epi64 (__m512i a, const int imm8)
        ///   VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector256<long> ExtractVector256(Vector512<long> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        /// __m256i _mm512_extracti64x4_epi64 (__m512i a, const int imm8)
        ///   VEXTRACTI64x4 ymm1/m256 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector256<ulong> ExtractVector256(Vector512<ulong> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        /// __m256 _mm512_extractf256_ps (__m512 a, const int imm8)
        ///   VEXTRACTF64x4 ymm1/m256 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector256<float> ExtractVector256(Vector512<float> value, [ConstantExpected] byte index) => ExtractVector256(value, index);
        /// <summary>
        /// __m256d _mm512_extractf64x4_pd (__m512d a, const int imm8)
        ///   VEXTRACTF64x4 ymm1/m256 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector256<double> ExtractVector256(Vector512<double> value, [ConstantExpected] byte index) => ExtractVector256(value, index);

        /// <summary>
        /// __m512 _mm512_fmadd_ps (__m512 a, __m512 b, __m512 c)
        ///   VFMADDPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> FusedMultiplyAdd(Vector512<float> a, Vector512<float> b, Vector512<float> c) => FusedMultiplyAdd(a, b, c);
        /// <summary>
        /// __m512d _mm512_fmadd_pd (__m512d a, __m512d b, __m512d c)
        ///   VFMADDPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> FusedMultiplyAdd(Vector512<double> a, Vector512<double> b, Vector512<double> c) => FusedMultiplyAdd(a, b, c);

        /// <summary>
        /// __m512 _mm512_fmaddsub_ps (__m512 a, __m512 b, __m512 c)
        ///   VFMADDSUBPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> FusedMultiplyAddSubtract(Vector512<float> a, Vector512<float> b, Vector512<float> c) => FusedMultiplyAddSubtract(a, b, c);
        /// <summary>
        /// __m512d _mm512_fmaddsub_pd (__m512d a, __m512d b, __m512d c)
        ///   VFMADDSUBPD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<double> FusedMultiplyAddSubtract(Vector512<double> a, Vector512<double> b, Vector512<double> c) => FusedMultiplyAddSubtract(a, b, c);

        /// <summary>
        /// __m512 _mm512_fmsub_ps (__m512 a, __m512 b, __m512 c)
        ///   VFMSUBPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> FusedMultiplySubtract(Vector512<float> a, Vector512<float> b, Vector512<float> c) => FusedMultiplySubtract(a, b, c);
        /// <summary>
        /// __m512d _mm512_fmsub_pd (__m512d a, __m512d b, __m512d c)
        ///   VFMSUBPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> FusedMultiplySubtract(Vector512<double> a, Vector512<double> b, Vector512<double> c) => FusedMultiplySubtract(a, b, c);

        /// <summary>
        /// __m512 _mm512_fmsubadd_ps (__m512 a, __m512 b, __m512 c)
        ///   VFMSUBADDPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> FusedMultiplySubtractAdd(Vector512<float> a, Vector512<float> b, Vector512<float> c) => FusedMultiplySubtractAdd(a, b, c);
        /// <summary>
        /// __m512d _mm512_fmsubadd_pd (__m512d a, __m512d b, __m512d c)
        ///   VFMSUBADDPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> FusedMultiplySubtractAdd(Vector512<double> a, Vector512<double> b, Vector512<double> c) => FusedMultiplySubtractAdd(a, b, c);

        /// <summary>
        /// __m512 _mm512_fnmadd_ps (__m512 a, __m512 b, __m512 c)
        ///   VFNMADDPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> FusedMultiplyAddNegated(Vector512<float> a, Vector512<float> b, Vector512<float> c) => FusedMultiplyAddNegated(a, b, c);
        /// <summary>
        /// __m512d _mm512_fnmadd_pd (__m512d a, __m512d b, __m512d c)
        ///   VFNMADDPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> FusedMultiplyAddNegated(Vector512<double> a, Vector512<double> b, Vector512<double> c) => FusedMultiplyAddNegated(a, b, c);

        /// <summary>
        /// __m512 _mm512_fnmsub_ps (__m512 a, __m512 b, __m512 c)
        ///   VFNMSUBPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> FusedMultiplySubtractNegated(Vector512<float> a, Vector512<float> b, Vector512<float> c) => FusedMultiplySubtractNegated(a, b, c);
        /// <summary>
        /// __m512d _mm512_fnmsub_pd (__m512d a, __m512d b, __m512d c)
        ///   VFNMSUBPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> FusedMultiplySubtractNegated(Vector512<double> a, Vector512<double> b, Vector512<double> c) => FusedMultiplySubtractNegated(a, b, c);

        /// <summary>
        /// __m512i _mm512_inserti128_si512 (__m512i a, __m128i b, const int imm8)
        ///   VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector512<sbyte> InsertVector128(Vector512<sbyte> value, Vector128<sbyte> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti128_si512 (__m512i a, __m128i b, const int imm8)
        ///   VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector512<byte> InsertVector128(Vector512<byte> value, Vector128<byte> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti128_si512 (__m512i a, __m128i b, const int imm8)
        ///   VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector512<short> InsertVector128(Vector512<short> value, Vector128<short> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti128_si512 (__m512i a, __m128i b, const int imm8)
        ///   VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector512<ushort> InsertVector128(Vector512<ushort> value, Vector128<ushort> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti32x4_epi32 (__m512i a, __m128i b, const int imm8)
        ///   VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector512<int> InsertVector128(Vector512<int> value, Vector128<int> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti32x4_epi32 (__m512i a, __m128i b, const int imm8)
        ///   VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector512<uint> InsertVector128(Vector512<uint> value, Vector128<uint> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti128_si512 (__m512i a, __m128i b, const int imm8)
        ///   VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector512<long> InsertVector128(Vector512<long> value, Vector128<long> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti128_si512 (__m512i a, __m128i b, const int imm8)
        ///   VINSERTI32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector512<ulong> InsertVector128(Vector512<ulong> value, Vector128<ulong> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        /// __m512 _mm512_insertf32x4_ps (__m512 a, __m128 b, int imm8)
        ///   VINSERTF32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector512<float> InsertVector128(Vector512<float> value, Vector128<float> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);
        /// <summary>
        /// __m512d _mm512_insertf128_pd (__m512d a, __m128d b, int imm8)
        ///   VINSERTF32x4 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector512<double> InsertVector128(Vector512<double> value, Vector128<double> data, [ConstantExpected] byte index) => InsertVector128(value, data, index);

        /// <summary>
        /// __m512i _mm512_inserti256_si512 (__m512i a, __m256i b, const int imm8)
        ///   VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
        /// </summary>
        public static Vector512<sbyte> InsertVector256(Vector512<sbyte> value, Vector256<sbyte> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti256_si512 (__m512i a, __m256i b, const int imm8)
        ///   VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
        /// </summary>
        public static Vector512<byte> InsertVector256(Vector512<byte> value, Vector256<byte> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti256_si512 (__m512i a, __m256i b, const int imm8)
        ///   VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
        /// </summary>
        public static Vector512<short> InsertVector256(Vector512<short> value, Vector256<short> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti256_si512 (__m512i a, __m256i b, const int imm8)
        ///   VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
        /// </summary>
        public static Vector512<ushort> InsertVector256(Vector512<ushort> value, Vector256<ushort> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti256_si512 (__m512i a, __m256i b, const int imm8)
        ///   VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
        /// </summary>
        public static Vector512<int> InsertVector256(Vector512<int> value, Vector256<int> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti256_si512 (__m512i a, __m256i b, const int imm8)
        ///   VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
        /// </summary>
        public static Vector512<uint> InsertVector256(Vector512<uint> value, Vector256<uint> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti64x4_epi64 (__m512i a, __m256i b, const int imm8)
        ///   VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
        /// </summary>
        public static Vector512<long> InsertVector256(Vector512<long> value, Vector256<long> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        /// __m512i _mm512_inserti64x4_epi64 (__m512i a, __m256i b, const int imm8)
        ///   VINSERTI64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
        /// </summary>
        public static Vector512<ulong> InsertVector256(Vector512<ulong> value, Vector256<ulong> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        /// __m512 _mm512_insertf256_ps (__m512 a, __m256 b, int imm8)
        ///   VINSERTF64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
        /// </summary>
        public static Vector512<float> InsertVector256(Vector512<float> value, Vector256<float> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);
        /// <summary>
        /// __m512d _mm512_insertf64x4_pd (__m512d a, __m256d b, int imm8)
        ///   VINSERTF64x4 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
        /// </summary>
        public static Vector512<double> InsertVector256(Vector512<double> value, Vector256<double> data, [ConstantExpected] byte index) => InsertVector256(value, data, index);

        /// <summary>
        /// __m512i _mm512_load_si512 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<byte> LoadAlignedVector512(byte* address) => LoadAlignedVector512(address);
        /// <summary>
        /// __m512i _mm512_load_si512 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<sbyte> LoadAlignedVector512(sbyte* address) => LoadAlignedVector512(address);
        /// <summary>
        /// __m512i _mm512_load_si512 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<short> LoadAlignedVector512(short* address) => LoadAlignedVector512(address);
        /// <summary>
        /// __m512i _mm512_load_si512 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<ushort> LoadAlignedVector512(ushort* address) => LoadAlignedVector512(address);
        /// <summary>
        /// __m512i _mm512_load_epi32 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<int> LoadAlignedVector512(int* address) => LoadAlignedVector512(address);
        /// <summary>
        /// __m512i _mm512_load_epi32 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<uint> LoadAlignedVector512(uint* address) => LoadAlignedVector512(address);
        /// <summary>
        /// __m512i _mm512_load_epi64 (__m512i const * mem_addr)
        ///   VMOVDQA64 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<long> LoadAlignedVector512(long* address) => LoadAlignedVector512(address);
        /// <summary>
        /// __m512i _mm512_load_epi64 (__m512i const * mem_addr)
        ///   VMOVDQA64 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<ulong> LoadAlignedVector512(ulong* address) => LoadAlignedVector512(address);
        /// <summary>
        /// __m512 _mm512_load_ps (float const * mem_addr)
        ///   VMOVAPS zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<float> LoadAlignedVector512(float* address) => LoadAlignedVector512(address);
        /// <summary>
        /// __m512d _mm512_load_pd (double const * mem_addr)
        ///   VMOVAPD zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<double> LoadAlignedVector512(double* address) => LoadAlignedVector512(address);

        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<sbyte> LoadAlignedVector512NonTemporal(sbyte* address) => LoadAlignedVector512NonTemporal(address);
        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<byte> LoadAlignedVector512NonTemporal(byte* address) => LoadAlignedVector512NonTemporal(address);
        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<short> LoadAlignedVector512NonTemporal(short* address) => LoadAlignedVector512NonTemporal(address);
        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<ushort> LoadAlignedVector512NonTemporal(ushort* address) => LoadAlignedVector512NonTemporal(address);
        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<int> LoadAlignedVector512NonTemporal(int* address) => LoadAlignedVector512NonTemporal(address);
        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<uint> LoadAlignedVector512NonTemporal(uint* address) => LoadAlignedVector512NonTemporal(address);
        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<long> LoadAlignedVector512NonTemporal(long* address) => LoadAlignedVector512NonTemporal(address);
        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<ulong> LoadAlignedVector512NonTemporal(ulong* address) => LoadAlignedVector512NonTemporal(address);

        /// <summary>
        /// __m512i _mm512_loadu_si512 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<sbyte> LoadVector512(sbyte* address) => LoadVector512(address);
        /// <summary>
        /// __m512i _mm512_loadu_si512 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<byte> LoadVector512(byte* address) => LoadVector512(address);
        /// <summary>
        /// __m512i _mm512_loadu_si512 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<short> LoadVector512(short* address) => LoadVector512(address);
        /// <summary>
        /// __m512i _mm512_loadu_si512 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<ushort> LoadVector512(ushort* address) => LoadVector512(address);
        /// <summary>
        /// __m512i _mm512_loadu_epi32 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<int> LoadVector512(int* address) => LoadVector512(address);
        /// <summary>
        /// __m512i _mm512_loadu_epi32 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<uint> LoadVector512(uint* address) => LoadVector512(address);
        /// <summary>
        /// __m512i _mm512_loadu_epi64 (__m512i const * mem_addr)
        ///   VMOVDQU64 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<long> LoadVector512(long* address) => LoadVector512(address);
        /// <summary>
        /// __m512i _mm512_loadu_epi64 (__m512i const * mem_addr)
        ///   VMOVDQU64 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<ulong> LoadVector512(ulong* address) => LoadVector512(address);
        /// <summary>
        /// __m512 _mm512_loadu_ps (float const * mem_addr)
        ///   VMOVUPS zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<float> LoadVector512(float* address) => LoadVector512(address);
        /// <summary>
        /// __m512d _mm512_loadu_pd (double const * mem_addr)
        ///   VMOVUPD zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<double> LoadVector512(double* address) => LoadVector512(address);

        /// <summary>
        /// __m512i _mm512_max_epi32 (__m512i a, __m512i b)
        ///   VPMAXSD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> Max(Vector512<int> left, Vector512<int> right) => Max(left, right);
        /// <summary>
        /// __m512i _mm512_max_epu32 (__m512i a, __m512i b)
        ///   VPMAXUD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Max(Vector512<uint> left, Vector512<uint> right) => Max(left, right);
        /// <summary>
        /// __m512i _mm512_max_epi64 (__m512i a, __m512i b)
        ///   VPMAXSQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Max(Vector512<long> left, Vector512<long> right) => Max(left, right);
        /// <summary>
        /// __m512i _mm512_max_epu64 (__m512i a, __m512i b)
        ///   VPMAXUQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Max(Vector512<ulong> left, Vector512<ulong> right) => Max(left, right);
        /// <summary>
        /// __m512 _mm512_max_ps (__m512 a, __m512 b)
        ///   VMAXPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{sae}
        /// </summary>
        public static Vector512<float> Max(Vector512<float> left, Vector512<float> right) => Max(left, right);
        /// <summary>
        /// __m512d _mm512_max_pd (__m512d a, __m512d b)
        ///   VMAXPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{sae}
        /// </summary>
        public static Vector512<double> Max(Vector512<double> left, Vector512<double> right) => Max(left, right);

        /// <summary>
        /// __m512i _mm512_min_epi32 (__m512i a, __m512i b)
        ///   VPMINSD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> Min(Vector512<int> left, Vector512<int> right) => Min(left, right);
        /// <summary>
        /// __m512i _mm512_min_epu32 (__m512i a, __m512i b)
        ///   VPMINUD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Min(Vector512<uint> left, Vector512<uint> right) => Min(left, right);
        /// <summary>
        /// __m512i _mm512_min_epi64 (__m512i a, __m512i b)
        ///   VPMINSQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Min(Vector512<long> left, Vector512<long> right) => Min(left, right);
        /// <summary>
        /// __m512i _mm512_min_epu64 (__m512i a, __m512i b)
        ///   VPMINUQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Min(Vector512<ulong> left, Vector512<ulong> right) => Min(left, right);
        /// <summary>
        /// __m512 _mm512_min_ps (__m512 a, __m512 b)
        ///   VMINPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{sae}
        /// </summary>
        public static Vector512<float> Min(Vector512<float> left, Vector512<float> right) => Min(left, right);
        /// <summary>
        /// __m512d _mm512_min_pd (__m512d a, __m512d b)
        ///   VMINPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{sae}
        /// </summary>
        public static Vector512<double> Min(Vector512<double> left, Vector512<double> right) => Min(left, right);

        /// <summary>
        /// __m512i _mm512_mul_epi32 (__m512i a, __m512i b)
        ///   VPMULDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Multiply(Vector512<int> left, Vector512<int> right) => Multiply(left, right);
        /// <summary>
        /// __m512i _mm512_mul_epu32 (__m512i a, __m512i b)
        ///   VPMULUDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Multiply(Vector512<uint> left, Vector512<uint> right) => Multiply(left, right);
        /// <summary>
        /// __m512 _mm512_mul_ps (__m512 a, __m512 b)
        ///   VMULPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{er}
        /// </summary>
        public static Vector512<float> Multiply(Vector512<float> left, Vector512<float> right) => Multiply(left, right);
        /// <summary>
        /// __m512d _mm512_mul_pd (__m512d a, __m512d b)
        ///   VMULPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{er}
        /// </summary>
        public static Vector512<double> Multiply(Vector512<double> left, Vector512<double> right) => Multiply(left, right);

        /// <summary>
        /// __m512i _mm512_mullo_epi32 (__m512i a, __m512i b)
        ///   VPMULLD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> MultiplyLow(Vector512<int> left, Vector512<int> right) => MultiplyLow(left, right);
        /// <summary>
        /// __m512i _mm512_mullo_epi32 (__m512i a, __m512i b)
        ///   VPMULLD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> MultiplyLow(Vector512<uint> left, Vector512<uint> right) => MultiplyLow(left, right);

        /// <summary>
        /// __m512i _mm512_or_si512 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<byte> Or(Vector512<byte> left, Vector512<byte> right) => Or(left, right);
        /// <summary>
        /// __m512i _mm512_or_si512 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<sbyte> Or(Vector512<sbyte> left, Vector512<sbyte> right) => Or(left, right);
        /// <summary>
        /// __m512i _mm512_or_si512 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<short> Or(Vector512<short> left, Vector512<short> right) => Or(left, right);
        /// <summary>
        /// __m512i _mm512_or_si512 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<ushort> Or(Vector512<ushort> left, Vector512<ushort> right) => Or(left, right);
        /// <summary>
        /// __m512i _mm512_or_epi32 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> Or(Vector512<int> left, Vector512<int> right) => Or(left, right);
        /// <summary>
        /// __m512i _mm512_or_epi32 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Or(Vector512<uint> left, Vector512<uint> right) => Or(left, right);
        /// <summary>
        /// __m512i _mm512_or_epi64 (__m512i a, __m512i b)
        ///   VPORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Or(Vector512<long> left, Vector512<long> right) => Or(left, right);
        /// <summary>
        /// __m512i _mm512_or_epi64 (__m512i a, __m512i b)
        ///   VPORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Or(Vector512<ulong> left, Vector512<ulong> right) => Or(left, right);

        /// <summary>
        /// __m512d _mm512_permute_pd (__m512d a, int imm8)
        ///   VPERMILPD zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<double> Permute2x64(Vector512<double> value, [ConstantExpected] byte control) => Permute2x64(value, control);

        /// <summary>
        /// __m512 _mm512_permute_ps (__m512 a, int imm8)
        ///   VPERMILPS zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<float> Permute4x32(Vector512<float> value, [ConstantExpected] byte control) => Permute4x32(value, control);

        /// <summary>
        /// __m512i _mm512_permute4x64_epi64 (__m512i a, const int imm8)
        ///   VPERMQ zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<long> Permute4x64(Vector512<long> value, [ConstantExpected] byte control) => Permute4x64(value, control);
        /// <summary>
        /// __m512i _mm512_permute4x64_epi64 (__m512i a, const int imm8)
        ///   VPERMQ zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<ulong> Permute4x64(Vector512<ulong> value, [ConstantExpected] byte control) => Permute4x64(value, control);
        /// <summary>
        /// __m512d _mm512_permute4x64_pd (__m512d a, const int imm8)
        ///   VPERMPD zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<double> Permute4x64(Vector512<double> value, [ConstantExpected] byte control) => Permute4x64(value, control);

        /// <summary>
        /// __m512d _mm512_permutevar_pd (__m512d a, __m512i b)
        ///   VPERMILPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> PermuteVar2x64(Vector512<double> left, Vector512<long> control) => PermuteVar2x64(left, control);

        /// <summary>
        /// __m512 _mm512_permutevar_ps (__m512 a, __m512i b)
        ///   VPERMILPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> PermuteVar4x32(Vector512<float> left, Vector512<int> control) => PermuteVar4x32(left, control);

        /// <summary>
        /// __m512i _mm512_permutevar8x64_epi64 (__m512i a, __m512i b)
        ///   VPERMQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> PermuteVar8x64(Vector512<long> value, Vector512<long> control) => PermuteVar8x64(value, control);
        /// <summary>
        /// __m512i _mm512_permutevar8x64_epi64 (__m512i a, __m512i b)
        ///   VPERMQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> PermuteVar8x64(Vector512<ulong> value, Vector512<ulong> control) => PermuteVar8x64(value, control);
        /// <summary>
        /// __m512d _mm512_permutevar8x64_pd (__m512d a, __m512i b)
        ///   VPERMPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> PermuteVar8x64(Vector512<double> value, Vector512<long> control) => PermuteVar8x64(value, control);

        /// <summary>
        /// __m512i _mm512_permutevar16x32_epi32 (__m512i a, __m512i b)
        ///   VPERMD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> PermuteVar16x32(Vector512<int> left, Vector512<int> control) => PermuteVar16x32(left, control);
        /// <summary>
        /// __m512i _mm512_permutevar16x32_epi32 (__m512i a, __m512i b)
        ///   VPERMD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> PermuteVar16x32(Vector512<uint> left, Vector512<uint> control) => PermuteVar16x32(left, control);
        /// <summary>
        /// __m512 _mm512_permutevar16x32_ps (__m512 a, __m512i b)
        ///   VPERMPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> PermuteVar16x32(Vector512<float> left, Vector512<int> control) => PermuteVar16x32(left, control);

        /// <summary>
        /// __m512i _mm512_sll_epi32 (__m512i a, __m128i count)
        ///   VPSLLD zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<int> ShiftLeftLogical(Vector512<int> value, Vector128<int> count) => ShiftLeftLogical(value, count);
        /// <summary>
        /// __m512i _mm512_sll_epi32 (__m512i a, __m128i count)
        ///   VPSLLD zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<uint> ShiftLeftLogical(Vector512<uint> value, Vector128<uint> count) => ShiftLeftLogical(value, count);
        /// <summary>
        /// __m512i _mm512_sll_epi64 (__m512i a, __m128i count)
        ///   VPSLLQ zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<long> ShiftLeftLogical(Vector512<long> value, Vector128<long> count) => ShiftLeftLogical(value, count);
        /// <summary>
        /// __m512i _mm512_sll_epi64 (__m512i a, __m128i count)
        ///   VPSLLQ zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<ulong> ShiftLeftLogical(Vector512<ulong> value, Vector128<ulong> count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// __m512i _mm512_slli_epi32 (__m512i a, int imm8)
        ///   VPSLLD zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<int> ShiftLeftLogical(Vector512<int> value, [ConstantExpected] byte count) => ShiftLeftLogical(value, count);
        /// <summary>
        /// __m512i _mm512_slli_epi32 (__m512i a, int imm8)
        ///   VPSLLD zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<uint> ShiftLeftLogical(Vector512<uint> value, [ConstantExpected] byte count) => ShiftLeftLogical(value, count);
        /// <summary>
        /// __m512i _mm512_slli_epi64 (__m512i a, int imm8)
        ///   VPSLLQ zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<long> ShiftLeftLogical(Vector512<long> value, [ConstantExpected] byte count) => ShiftLeftLogical(value, count);
        /// <summary>
        /// __m512i _mm512_slli_epi64 (__m512i a, int imm8)
        ///   VPSLLQ zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<ulong> ShiftLeftLogical(Vector512<ulong> value, [ConstantExpected] byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// __m512i _mm512_sllv_epi32 (__m512i a, __m512i count)
        ///   VPSLLVD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> ShiftLeftLogicalVariable(Vector512<int> value, Vector512<uint> count) => ShiftLeftLogicalVariable(value, count);
        /// <summary>
        /// __m512i _mm512_sllv_epi32 (__m512i a, __m512i count)
        ///   VPSLLVD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> ShiftLeftLogicalVariable(Vector512<uint> value, Vector512<uint> count) => ShiftLeftLogicalVariable(value, count);
        /// <summary>
        /// __m512i _mm512_sllv_epi64 (__m512i a, __m512i count)
        ///   VPSLLVQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> ShiftLeftLogicalVariable(Vector512<long> value, Vector512<ulong> count) => ShiftLeftLogicalVariable(value, count);
        /// <summary>
        /// __m512i _mm512_sllv_epi64 (__m512i a, __m512i count)
        ///   VPSLLVQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> ShiftLeftLogicalVariable(Vector512<ulong> value, Vector512<ulong> count) => ShiftLeftLogicalVariable(value, count);

        /// <summary>
        /// _mm512_sra_epi32 (__m512i a, __m128i count)
        ///   VPSRAD zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<int> ShiftRightArithmetic(Vector512<int> value, Vector128<int> count) => ShiftRightArithmetic(value, count);
        /// <summary>
        /// _mm512_sra_epi64 (__m512i a, __m128i count)
        ///   VPSRAQ zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<long> ShiftRightArithmetic(Vector512<long> value, Vector128<long> count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// __m512i _mm512_srai_epi32 (__m512i a, int imm8)
        ///   VPSRAD zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<int> ShiftRightArithmetic(Vector512<int> value, [ConstantExpected] byte count) => ShiftRightArithmetic(value, count);
        /// <summary>
        /// __m512i _mm512_srai_epi64 (__m512i a, int imm8)
        ///   VPSRAQ zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<long> ShiftRightArithmetic(Vector512<long> value, [ConstantExpected] byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// __m512i _mm512_srav_epi32 (__m512i a, __m512i count)
        ///   VPSRAVD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> ShiftRightArithmeticVariable(Vector512<int> value, Vector512<uint> count) => ShiftRightArithmeticVariable(value, count);
        /// <summary>
        /// __m512i _mm512_srav_epi64 (__m512i a, __m512i count)
        ///   VPSRAVQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> ShiftRightArithmeticVariable(Vector512<long> value, Vector512<ulong> count) => ShiftRightArithmeticVariable(value, count);

        /// <summary>
        /// __m512i _mm512_srl_epi32 (__m512i a, __m128i count)
        ///   VPSRLD zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<int> ShiftRightLogical(Vector512<int> value, Vector128<int> count) => ShiftRightLogical(value, count);
        /// <summary>
        /// __m512i _mm512_srl_epi32 (__m512i a, __m128i count)
        ///   VPSRLD zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<uint> ShiftRightLogical(Vector512<uint> value, Vector128<uint> count) => ShiftRightLogical(value, count);
        /// <summary>
        /// __m512i _mm512_srl_epi64 (__m512i a, __m128i count)
        ///   VPSRLQ zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<long> ShiftRightLogical(Vector512<long> value, Vector128<long> count) => ShiftRightLogical(value, count);
        /// <summary>
        /// __m512i _mm512_srl_epi64 (__m512i a, __m128i count)
        ///   VPSRLQ zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<ulong> ShiftRightLogical(Vector512<ulong> value, Vector128<ulong> count) => ShiftRightLogical(value, count);

        /// <summary>
        /// __m512i _mm512_srli_epi32 (__m512i a, int imm8)
        ///   VPSRLD zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<int> ShiftRightLogical(Vector512<int> value, [ConstantExpected] byte count) => ShiftRightLogical(value, count);
        /// <summary>
        /// __m512i _mm512_srli_epi32 (__m512i a, int imm8)
        ///   VPSRLD zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<uint> ShiftRightLogical(Vector512<uint> value, [ConstantExpected] byte count) => ShiftRightLogical(value, count);
        /// <summary>
        /// __m512i _mm512_srli_epi64 (__m512i a, int imm8)
        ///   VPSRLQ zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<long> ShiftRightLogical(Vector512<long> value, [ConstantExpected] byte count) => ShiftRightLogical(value, count);
        /// <summary>
        /// __m512i _mm512_srli_epi64 (__m512i a, int imm8)
        ///   VPSRLQ zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<ulong> ShiftRightLogical(Vector512<ulong> value, [ConstantExpected] byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// __m512i _mm512_srlv_epi32 (__m512i a, __m512i count)
        ///   VPSRLVD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> ShiftRightLogicalVariable(Vector512<int> value, Vector512<uint> count) => ShiftRightLogicalVariable(value, count);
        /// <summary>
        /// __m512i _mm512_srlv_epi32 (__m512i a, __m512i count)
        ///   VPSRLVD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> ShiftRightLogicalVariable(Vector512<uint> value, Vector512<uint> count) => ShiftRightLogicalVariable(value, count);
        /// <summary>
        /// __m512i _mm512_srlv_epi64 (__m512i a, __m512i count)
        ///   VPSRLVQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> ShiftRightLogicalVariable(Vector512<long> value, Vector512<ulong> count) => ShiftRightLogicalVariable(value, count);
        /// <summary>
        /// __m512i _mm512_srlv_epi64 (__m512i a, __m512i count)
        ///   VPSRLVQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> ShiftRightLogicalVariable(Vector512<ulong> value, Vector512<ulong> count) => ShiftRightLogicalVariable(value, count);

        /// <summary>
        /// __m512i _mm512_shuffle_epi32 (__m512i a, const int imm8)
        ///   VPSHUFD zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<int> Shuffle(Vector512<int> value, [ConstantExpected] byte control) => Shuffle(value, control);
        /// <summary>
        /// __m512i _mm512_shuffle_epi32 (__m512i a, const int imm8)
        ///   VPSHUFD zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<uint> Shuffle(Vector512<uint> value, [ConstantExpected] byte control) => Shuffle(value, control);
        /// <summary>
        /// __m512 _mm512_shuffle_ps (__m512 a, __m512 b, const int imm8)
        ///   VSHUFPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<float> Shuffle(Vector512<float> value, Vector512<float> right, [ConstantExpected] byte control) => Shuffle(value, right, control);
        /// <summary>
        /// __m512d _mm512_shuffle_pd (__m512d a, __m512d b, const int imm8)
        ///   VSHUFPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<double> Shuffle(Vector512<double> value, Vector512<double> right, [ConstantExpected] byte control) => Shuffle(value, right, control);

        /// <summary>
        /// __m512 _mm512_sqrt_ps (__m512 a)
        ///   VSQRTPS zmm1 {k1}{z}, zmm2/m512/m32bcst{er}
        /// </summary>
        public static Vector512<float> Sqrt(Vector512<float> value) => Sqrt(value);
        /// <summary>
        /// __m512d _mm512_sqrt_pd (__m512d a)
        ///   VSQRTPD zmm1 {k1}{z}, zmm2/m512/m64bcst{er}
        /// </summary>
        public static Vector512<double> Sqrt(Vector512<double> value) => Sqrt(value);

        /// <summary>
        /// void _mm512_storeu_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(sbyte* address, Vector512<sbyte> source) => Store(address, source);
        /// <summary>
        /// void _mm512_storeu_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(byte* address, Vector512<byte> source) => Store(address, source);
        /// <summary>
        /// void _mm512_storeu_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(short* address, Vector512<short> source) => Store(address, source);
        /// <summary>
        /// void _mm512_storeu_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(ushort* address, Vector512<ushort> source) => Store(address, source);
        /// <summary>
        /// void _mm512_storeu_epi32 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(int* address, Vector512<int> source) => Store(address, source);
        /// <summary>
        /// void _mm512_storeu_epi32 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(uint* address, Vector512<uint> source) => Store(address, source);
        /// <summary>
        /// void _mm512_storeu_epi64 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU64 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(long* address, Vector512<long> source) => Store(address, source);
        /// <summary>
        /// void _mm512_storeu_epi64 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU64 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(ulong* address, Vector512<ulong> source) => Store(address, source);
        /// <summary>
        /// void _mm512_storeu_ps (float * mem_addr, __m512 a)
        ///   VMOVUPS m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(float* address, Vector512<float> source) => Store(address, source);
        /// <summary>
        /// void _mm512_storeu_pd (double * mem_addr, __m512d a)
        ///   VMOVUPD m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(double* address, Vector512<double> source) => Store(address, source);

        /// <summary>
        /// void _mm512_store_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(byte* address, Vector512<byte> source) => StoreAligned(address, source);
        /// <summary>
        /// void _mm512_store_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(sbyte* address, Vector512<sbyte> source) => StoreAligned(address, source);
        /// <summary>
        /// void _mm512_store_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(short* address, Vector512<short> source) => StoreAligned(address, source);
        /// <summary>
        /// void _mm512_store_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(ushort* address, Vector512<ushort> source) => StoreAligned(address, source);
        /// <summary>
        /// void _mm512_store_epi32 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(int* address, Vector512<int> source) => StoreAligned(address, source);
        /// <summary>
        /// void _mm512_store_epi32 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(uint* address, Vector512<uint> source) => StoreAligned(address, source);
        /// <summary>
        /// void _mm512_store_epi64 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(long* address, Vector512<long> source) => StoreAligned(address, source);
        /// <summary>
        /// void _mm512_store_epi64 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(ulong* address, Vector512<ulong> source) => StoreAligned(address, source);
        /// <summary>
        /// void _mm512_store_ps (float * mem_addr, __m512 a)
        ///   VMOVAPS m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(float* address, Vector512<float> source) => StoreAligned(address, source);
        /// <summary>
        /// void _mm512_store_pd (double * mem_addr, __m512d a)
        ///   VMOVAPD m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(double* address, Vector512<double> source) => StoreAligned(address, source);

        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(sbyte* address, Vector512<sbyte> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(byte* address, Vector512<byte> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(short* address, Vector512<short> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(ushort* address, Vector512<ushort> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(int* address, Vector512<int> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(uint* address, Vector512<uint> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(long* address, Vector512<long> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(ulong* address, Vector512<ulong> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        /// void _mm512_stream_ps (float * mem_addr, __m512 a)
        ///   VMOVNTPS m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(float* address, Vector512<float> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        /// void _mm512_stream_pd (double * mem_addr, __m512d a)
        ///   VMOVNTPD m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(double* address, Vector512<double> source) => StoreAlignedNonTemporal(address, source);

        /// <summary>
        /// __m512i _mm512_sub_epi32 (__m512i a, __m512i b)
        ///   VPSUBD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> Subtract(Vector512<int> left, Vector512<int> right) => Subtract(left, right);
        /// <summary>
        /// __m512i _mm512_sub_epi32 (__m512i a, __m512i b)
        ///   VPSUBD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Subtract(Vector512<uint> left, Vector512<uint> right) => Subtract(left, right);
        /// <summary>
        /// __m512i _mm512_sub_epi64 (__m512i a, __m512i b)
        ///   VPSUBQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Subtract(Vector512<long> left, Vector512<long> right) => Subtract(left, right);
        /// <summary>
        /// __m512i _mm512_sub_epi64 (__m512i a, __m512i b)
        ///   VPSUBQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Subtract(Vector512<ulong> left, Vector512<ulong> right) => Subtract(left, right);
        /// <summary>
        /// __m512 _mm512_sub_ps (__m512 a, __m512 b)
        ///   VSUBPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{er}
        /// </summary>
        public static Vector512<float> Subtract(Vector512<float> left, Vector512<float> right) => Subtract(left, right);
        /// <summary>
        /// __m512d _mm512_sub_pd (__m512d a, __m512d b)
        ///   VSUBPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{er}
        /// </summary>
        public static Vector512<double> Subtract(Vector512<double> left, Vector512<double> right) => Subtract(left, right);

        /// <summary>
        /// __m512i _mm512_unpackhi_epi32 (__m512i a, __m512i b)
        ///   VPUNPCKHDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> UnpackHigh(Vector512<int> left, Vector512<int> right) => UnpackHigh(left, right);
        /// <summary>
        /// __m512i _mm512_unpackhi_epi32 (__m512i a, __m512i b)
        ///   VPUNPCKHDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> UnpackHigh(Vector512<uint> left, Vector512<uint> right) => UnpackHigh(left, right);
        /// <summary>
        /// __m512i _mm512_unpackhi_epi64 (__m512i a, __m512i b)
        ///   VPUNPCKHQDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> UnpackHigh(Vector512<long> left, Vector512<long> right) => UnpackHigh(left, right);
        /// <summary>
        /// __m512i _mm512_unpackhi_epi64 (__m512i a, __m512i b)
        ///   VPUNPCKHQDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> UnpackHigh(Vector512<ulong> left, Vector512<ulong> right) => UnpackHigh(left, right);
        /// <summary>
        /// __m512 _mm512_unpackhi_ps (__m512 a, __m512 b)
        ///   VUNPCKHPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> UnpackHigh(Vector512<float> left, Vector512<float> right) => UnpackHigh(left, right);
        /// <summary>
        /// __m512d _mm512_unpackhi_pd (__m512d a, __m512d b)
        ///   VUNPCKHPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> UnpackHigh(Vector512<double> left, Vector512<double> right) => UnpackHigh(left, right);

        /// <summary>
        /// __m512i _mm512_unpacklo_epi32 (__m512i a, __m512i b)
        ///   VPUNPCKLDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> UnpackLow(Vector512<int> left, Vector512<int> right) => UnpackLow(left, right);
        /// <summary>
        /// __m512i _mm512_unpacklo_epi32 (__m512i a, __m512i b)
        ///   VPUNPCKLDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> UnpackLow(Vector512<uint> left, Vector512<uint> right) => UnpackLow(left, right);
        /// <summary>
        /// __m512i _mm512_unpacklo_epi64 (__m512i a, __m512i b)
        ///   VPUNPCKLQDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> UnpackLow(Vector512<long> left, Vector512<long> right) => UnpackLow(left, right);
        /// <summary>
        /// __m512i _mm512_unpacklo_epi64 (__m512i a, __m512i b)
        ///   VPUNPCKLQDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> UnpackLow(Vector512<ulong> left, Vector512<ulong> right) => UnpackLow(left, right);
        /// <summary>
        /// __m512 _mm512_unpacklo_ps (__m512 a, __m512 b)
        ///   VUNPCKLPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> UnpackLow(Vector512<float> left, Vector512<float> right) => UnpackLow(left, right);
        /// <summary>
        /// __m512d _mm512_unpacklo_pd (__m512d a, __m512d b)
        ///   VUNPCKLPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> UnpackLow(Vector512<double> left, Vector512<double> right) => UnpackLow(left, right);

        /// <summary>
        /// __m512i _mm512_xor_si512 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<byte> Xor(Vector512<byte> left, Vector512<byte> right) => Xor(left, right);
        /// <summary>
        /// __m512i _mm512_xor_si512 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<sbyte> Xor(Vector512<sbyte> left, Vector512<sbyte> right) => Xor(left, right);
        /// <summary>
        /// __m512i _mm512_xor_si512 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<short> Xor(Vector512<short> left, Vector512<short> right) => Xor(left, right);
        /// <summary>
        /// __m512i _mm512_xor_si512 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<ushort> Xor(Vector512<ushort> left, Vector512<ushort> right) => Xor(left, right);
        /// <summary>
        /// __m512i _mm512_xor_epi32 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> Xor(Vector512<int> left, Vector512<int> right) => Xor(left, right);
        /// <summary>
        /// __m512i _mm512_xor_epi32 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Xor(Vector512<uint> left, Vector512<uint> right) => Xor(left, right);
        /// <summary>
        /// __m512i _mm512_xor_epi64 (__m512i a, __m512i b)
        ///   VPXORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Xor(Vector512<long> left, Vector512<long> right) => Xor(left, right);
        /// <summary>
        /// __m512i _mm512_xor_epi64 (__m512i a, __m512i b)
        ///   VPXORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Xor(Vector512<ulong> left, Vector512<ulong> right) => Xor(left, right);
    }
}
