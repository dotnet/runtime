// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>This class provides access to X86 AVX512F hardware instructions via intrinsics</summary>
    [CLSCompliant(false)]
    public abstract class Avx512F : Avx2
    {
        internal Avx512F() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        public abstract class VL
        {
            internal VL() { }

            public static bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            /// __m128i _mm_abs_epi64 (__m128i a)
            ///   VPABSQ xmm1 {k1}{z}, xmm2/m128/m64bcst
            /// </summary>
            public static Vector128<ulong> Abs(Vector128<long> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_abs_epi64 (__m128i a)
            ///   VPABSQ ymm1 {k1}{z}, ymm2/m256/m64bcst
            /// </summary>
            public static Vector256<ulong> Abs(Vector256<long> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128i _mm_max_epi64 (__m128i a, __m128i b)
            ///   VPMAXSQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<long> Max(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_max_epu64 (__m128i a, __m128i b)
            ///   VPMAXUQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<ulong> Max(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_max_epi64 (__m256i a, __m256i b)
            ///   VPMAXSQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<long> Max(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_max_epu64 (__m256i a, __m256i b)
            ///   VPMAXUQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<ulong> Max(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128i _mm_min_epi64 (__m128i a, __m128i b)
            ///   VPMINSQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<long> Min(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_min_epu64 (__m128i a, __m128i b)
            ///   VPMINUQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<ulong> Min(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_min_epi64 (__m256i a, __m256i b)
            ///   VPMINSQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<long> Min(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_min_epu64 (__m256i a, __m256i b)
            ///   VPMINUQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<ulong> Min(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128i _mm_sra_epi64 (__m128i a, __m128i count)
            ///   VPSRAQ xmm1 {k1}{z}, xmm2, xmm3/m128
            /// </summary>
            public static Vector128<long> ShiftRightArithmetic(Vector128<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_sra_epi64 (__m256i a, __m128i count)
            ///   VPSRAQ ymm1 {k1}{z}, ymm2, xmm3/m128
            /// </summary>
            public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __128i _mm_srai_epi64 (__m128i a, int imm8)
            ///   VPSRAQ xmm1 {k1}{z}, xmm2, imm8
            /// </summary>
            public static Vector128<long> ShiftRightArithmetic(Vector128<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_srai_epi64 (__m256i a, int imm8)
            ///   VPSRAQ ymm1 {k1}{z}, ymm2, imm8
            /// </summary>
            public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        }

        public new abstract class X64 : Avx2.X64
        {
            internal X64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        /// __m512i _mm512_abs_epi32 (__m512i a)
        ///   VPABSD zmm1 {k1}{z}, zmm2/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Abs(Vector512<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_abs_epi64 (__m512i a)
        ///   VPABSQ zmm1 {k1}{z}, zmm2/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Abs(Vector512<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_add_epi32 (__m512i a, __m512i b)
        ///   VPADDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> Add(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_add_epi32 (__m512i a, __m512i b)
        ///   VPADDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Add(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_add_epi64 (__m512i a, __m512i b)
        ///   VPADDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Add(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_add_epi64 (__m512i a, __m512i b)
        ///   VPADDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Add(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_add_pd (__m512d a, __m512d b)
        ///   VADDPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{er}
        /// </summary>
        public static Vector512<double> Add(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512 _mm512_add_ps (__m512 a, __m512 b)
        ///   VADDPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{er}
        /// </summary>
        public static Vector512<float> Add(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_and_si512 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<byte> And(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_and_si512 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<sbyte> And(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_and_si512 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<short> And(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_and_si512 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<ushort> And(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_and_epi32 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> And(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_and_epi32 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> And(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_and_epi64 (__m512i a, __m512i b)
        ///   VPANDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> And(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_and_epi64 (__m512i a, __m512i b)
        ///   VPANDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> And(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_andnot_si512 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<byte> AndNot(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_andnot_si512 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<sbyte> AndNot(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_andnot_si512 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<short> AndNot(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_andnot_si512 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<ushort> AndNot(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_andnot_epi32 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> AndNot(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_andnot_epi32 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> AndNot(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_andnot_epi64 (__m512i a, __m512i b)
        ///   VPANDNQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> AndNot(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_andnot_epi64 (__m512i a, __m512i b)
        ///   VPANDNQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> AndNot(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm512_cvtpd_epi32 (__m512d a)
        ///   VCVTPD2DQ ymm1 {k1}{z}, zmm2/m512/m64bcst{er}
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector512<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256 _mm512_cvtpd_ps (__m512d a)
        ///   VCVTPD2PS ymm1,         zmm2/m512
        ///   VCVTPD2PS ymm1 {k1}{z}, zmm2/m512/m64bcst{er}
        /// </summary>
        public static Vector256<float> ConvertToVector256Single(Vector512<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm512_cvttpd_epi32 (__m512d a)
        ///   VCVTTPD2DQ ymm1 {k1}{z}, zmm2/m512/m64bcst{sae}
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32WithTruncation(Vector512<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512d _mm512_cvtepi32_pd (__m256i a)
        ///   VCVTDQ2PD zmm1 {k1}{z}, ymm2/m256/m32bcst
        /// </summary>
        public static Vector512<double> ConvertToVector512Double(Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_cvtps_pd (__m256 a)
        ///   VCVTPS2PD zmm1 {k1}{z}, ymm2/m256/m32bcst{sae}
        /// </summary>
        public static Vector512<double> ConvertToVector512Double(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepi8_epi32 (__m128i a)
        ///   VPMOVSXBD zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepu8_epi32 (__m128i a)
        ///   VPMOVZXBD zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepi16_epi32 (__m128i a)
        ///   VPMOVSXWD zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector256<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepu16_epi32 (__m128i a)
        ///   VPMOVZXWD zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtps_epi32 (__m512 a)
        ///   VCVTPS2DQ zmm1 {k1}{z}, zmm2/m512/m32bcst{er}
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32(Vector512<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepi8_epi64 (__m128i a)
        ///   VPMOVSXBQ zmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepu8_epi64 (__m128i a)
        ///   VPMOVZXBQ zmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepi16_epi64 (__m128i a)
        ///   VPMOVSXWQ zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepu16_epi64 (__m128i a)
        ///   VPMOVZXWQ zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepi32_epi64 (__m128i a)
        ///   VPMOVSXDQ zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepu32_epi64 (__m128i a)
        ///   VPMOVZXDQ zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<long> ConvertToVector512Int64(Vector256<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512 _mm512_cvtepi32_ps (__m512i a)
        ///   VCVTDQ2PS zmm1 {k1}{z}, zmm2/m512/m32bcst{er}
        /// </summary>
        public static Vector512<float> ConvertToVector512Single(Vector512<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepi8_epi32 (__m128i a)
        ///   VPMOVSXBD zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepu8_epi32 (__m128i a)
        ///   VPMOVZXBD zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepi16_epi32 (__m128i a)
        ///   VPMOVSXWD zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector256<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepu16_epi32 (__m128i a)
        ///   VPMOVZXWD zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepi8_epi64 (__m128i a)
        ///   VPMOVSXBQ zmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepu8_epi64 (__m128i a)
        ///   VPMOVZXBQ zmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepi16_epi64 (__m128i a)
        ///   VPMOVSXWQ zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepu16_epi64 (__m128i a)
        ///   VPMOVZXWQ zmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepi32_epi64 (__m128i a)
        ///   VPMOVSXDQ zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepu32_epi64 (__m128i a)
        ///   VPMOVZXDQ zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<ulong> ConvertToVector512UInt64(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_cvttps_epi32 (__m512 a)
        ///   VCVTTPS2DQ zmm1 {k1}{z}, zmm2/m512/m32bcst{sae}
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32WithTruncation(Vector512<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512 _mm512_div_ps (__m512 a, __m512 b)
        ///   VDIVPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{er}
        /// </summary>
        public static Vector512<float> Divide(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_div_pd (__m512d a, __m512d b)
        ///   VDIVPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{er}
        /// </summary>
        public static Vector512<double> Divide(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512 _mm512_moveldup_ps (__m512 a)
        ///   VMOVSLDUP zmm1 {k1}{z}, zmm2/m512
        /// </summary>
        public static Vector512<float> DuplicateEvenIndexed(Vector512<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_movedup_pd (__m512d a)
        ///   VMOVDDUP zmm1 {k1}{z}, zmm2/m512
        /// </summary>
        public static Vector512<double> DuplicateEvenIndexed(Vector512<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512 _mm512_movehdup_ps (__m512 a)
        ///   VMOVSHDUP zmm1 {k1}{z}, zmm2/m512
        /// </summary>
        public static Vector512<float> DuplicateOddIndexed(Vector512<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_load_si512 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<byte> LoadAlignedVector512(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_load_si512 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<sbyte> LoadAlignedVector512(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_load_si512 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<short> LoadAlignedVector512(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_load_si512 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<ushort> LoadAlignedVector512(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_load_epi32 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<int> LoadAlignedVector512(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_load_epi32 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<uint> LoadAlignedVector512(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_load_epi64 (__m512i const * mem_addr)
        ///   VMOVDQA64 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<long> LoadAlignedVector512(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_load_epi64 (__m512i const * mem_addr)
        ///   VMOVDQA64 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<ulong> LoadAlignedVector512(ulong* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512 _mm512_load_ps (float const * mem_addr)
        ///   VMOVAPS zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<float> LoadAlignedVector512(float* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_load_pd (double const * mem_addr)
        ///   VMOVAPD zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<double> LoadAlignedVector512(double* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<sbyte> LoadAlignedVector512NonTemporal(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<byte> LoadAlignedVector512NonTemporal(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<short> LoadAlignedVector512NonTemporal(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<ushort> LoadAlignedVector512NonTemporal(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<int> LoadAlignedVector512NonTemporal(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<uint> LoadAlignedVector512NonTemporal(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<long> LoadAlignedVector512NonTemporal(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_stream_load_si512 (__m512i const* mem_addr)
        ///   VMOVNTDQA zmm1, m512
        /// </summary>
        public static unsafe Vector512<ulong> LoadAlignedVector512NonTemporal(ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_loadu_si512 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<sbyte> LoadVector512(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_si512 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<byte> LoadVector512(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_si512 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<short> LoadVector512(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_si512 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<ushort> LoadVector512(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_epi32 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<int> LoadVector512(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_epi32 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<uint> LoadVector512(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_epi64 (__m512i const * mem_addr)
        ///   VMOVDQU64 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<long> LoadVector512(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_epi64 (__m512i const * mem_addr)
        ///   VMOVDQU64 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<ulong> LoadVector512(ulong* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512 _mm512_loadu_ps (float const * mem_addr)
        ///   VMOVUPS zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<float> LoadVector512(float* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_loadu_pd (double const * mem_addr)
        ///   VMOVUPD zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<double> LoadVector512(double* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_max_epi32 (__m512i a, __m512i b)
        ///   VPMAXSD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> Max(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_max_epu32 (__m512i a, __m512i b)
        ///   VPMAXUD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Max(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_max_epi64 (__m512i a, __m512i b)
        ///   VPMAXSQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Max(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_max_epu64 (__m512i a, __m512i b)
        ///   VPMAXUQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Max(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512 _mm512_max_ps (__m512 a, __m512 b)
        ///   VMAXPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{sae}
        /// </summary>
        public static Vector512<float> Max(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_max_pd (__m512d a, __m512d b)
        ///   VMAXPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{sae}
        /// </summary>
        public static Vector512<double> Max(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_min_epi32 (__m512i a, __m512i b)
        ///   VPMINSD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> Min(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_min_epu32 (__m512i a, __m512i b)
        ///   VPMINUD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Min(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_min_epi64 (__m512i a, __m512i b)
        ///   VPMINSQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Min(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_min_epu64 (__m512i a, __m512i b)
        ///   VPMINUQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Min(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512 _mm512_min_ps (__m512 a, __m512 b)
        ///   VMINPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{sae}
        /// </summary>
        public static Vector512<float> Min(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_min_pd (__m512d a, __m512d b)
        ///   VMINPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{sae}
        /// </summary>
        public static Vector512<double> Min(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_mul_epi32 (__m512i a, __m512i b)
        ///   VPMULDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Multiply(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_mul_epu32 (__m512i a, __m512i b)
        ///   VPMULUDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Multiply(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512 _mm512_mul_ps (__m512 a, __m512 b)
        ///   VMULPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{er}
        /// </summary>
        public static Vector512<float> Multiply(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_mul_pd (__m512d a, __m512d b)
        ///   VMULPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{er}
        /// </summary>
        public static Vector512<double> Multiply(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_mullo_epi32 (__m512i a, __m512i b)
        ///   VPMULLD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> MultiplyLow(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_mullo_epi32 (__m512i a, __m512i b)
        ///   VPMULLD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> MultiplyLow(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_or_si512 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<byte> Or(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_or_si512 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<sbyte> Or(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_or_si512 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<short> Or(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_or_si512 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<ushort> Or(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_or_epi32 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> Or(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_or_epi32 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Or(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_or_epi64 (__m512i a, __m512i b)
        ///   VPORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Or(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_or_epi64 (__m512i a, __m512i b)
        ///   VPORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Or(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_sll_epi32 (__m512i a, __m128i count)
        ///   VPSLLD zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<int> ShiftLeftLogical(Vector512<int> value, Vector128<int> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_sll_epi32 (__m512i a, __m128i count)
        ///   VPSLLD zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<uint> ShiftLeftLogical(Vector512<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_sll_epi64 (__m512i a, __m128i count)
        ///   VPSLLQ zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<long> ShiftLeftLogical(Vector512<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_sll_epi64 (__m512i a, __m128i count)
        ///   VPSLLQ zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<ulong> ShiftLeftLogical(Vector512<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_slli_epi32 (__m512i a, int imm8)
        ///   VPSLLD zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<int> ShiftLeftLogical(Vector512<int> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_slli_epi32 (__m512i a, int imm8)
        ///   VPSLLD zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<uint> ShiftLeftLogical(Vector512<uint> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_slli_epi64 (__m512i a, int imm8)
        ///   VPSLLQ zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<long> ShiftLeftLogical(Vector512<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_slli_epi64 (__m512i a, int imm8)
        ///   VPSLLQ zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<ulong> ShiftLeftLogical(Vector512<ulong> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// _mm512_sra_epi32 (__m512i a, __m128i count)
        ///   VPSRAD zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<int> ShiftRightArithmetic(Vector512<int> value, Vector128<int> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _mm512_sra_epi64 (__m512i a, __m128i count)
        ///   VPSRAQ zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<long> ShiftRightArithmetic(Vector512<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_srai_epi32 (__m512i a, int imm8)
        ///   VPSRAD zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<int> ShiftRightArithmetic(Vector512<int> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_srai_epi64 (__m512i a, int imm8)
        ///   VPSRAQ zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<long> ShiftRightArithmetic(Vector512<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_srl_epi32 (__m512i a, __m128i count)
        ///   VPSRLD zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<int> ShiftRightLogical(Vector512<int> value, Vector128<int> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_srl_epi32 (__m512i a, __m128i count)
        ///   VPSRLD zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<uint> ShiftRightLogical(Vector512<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_srl_epi64 (__m512i a, __m128i count)
        ///   VPSRLQ zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<long> ShiftRightLogical(Vector512<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_srl_epi64 (__m512i a, __m128i count)
        ///   VPSRLQ zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<ulong> ShiftRightLogical(Vector512<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_srli_epi32 (__m512i a, int imm8)
        ///   VPSRLD zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<int> ShiftRightLogical(Vector512<int> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_srli_epi32 (__m512i a, int imm8)
        ///   VPSRLD zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<uint> ShiftRightLogical(Vector512<uint> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_srli_epi64 (__m512i a, int imm8)
        ///   VPSRLQ zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<long> ShiftRightLogical(Vector512<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_srli_epi64 (__m512i a, int imm8)
        ///   VPSRLQ zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<ulong> ShiftRightLogical(Vector512<ulong> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_shuffle_epi32 (__m512i a, const int imm8)
        ///   VPSHUFD zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<int> Shuffle(Vector512<int> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_shuffle_epi32 (__m512i a, const int imm8)
        ///   VPSHUFD zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<uint> Shuffle(Vector512<uint> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512 _mm512_shuffle_ps (__m512 a, __m512 b, const int imm8)
        ///   VSHUFPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<float> Shuffle(Vector512<float> value, Vector512<float> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_shuffle_pd (__m512d a, __m512d b, const int imm8)
        ///   VSHUFPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<double> Shuffle(Vector512<double> value, Vector512<double> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512 _mm512_sqrt_ps (__m512 a)
        ///   VSQRTPS zmm1 {k1}{z}, zmm2/m512/m32bcst{er}
        /// </summary>
        public static Vector512<float> Sqrt(Vector512<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_sqrt_pd (__m512d a)
        ///   VSQRTPD zmm1 {k1}{z}, zmm2/m512/m64bcst{er}
        /// </summary>
        public static Vector512<double> Sqrt(Vector512<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm512_storeu_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(sbyte* address, Vector512<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(byte* address, Vector512<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(short* address, Vector512<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(ushort* address, Vector512<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_epi32 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(int* address, Vector512<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_epi32 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(uint* address, Vector512<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_epi64 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU64 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(long* address, Vector512<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_epi64 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU64 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(ulong* address, Vector512<ulong> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_ps (float * mem_addr, __m512 a)
        ///   VMOVUPS m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(float* address, Vector512<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_pd (double * mem_addr, __m512d a)
        ///   VMOVUPD m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(double* address, Vector512<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm512_store_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(byte* address, Vector512<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(sbyte* address, Vector512<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(short* address, Vector512<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(ushort* address, Vector512<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_epi32 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(int* address, Vector512<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_epi32 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(uint* address, Vector512<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_epi64 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(long* address, Vector512<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_epi64 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(ulong* address, Vector512<ulong> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_ps (float * mem_addr, __m512 a)
        ///   VMOVAPS m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(float* address, Vector512<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_pd (double * mem_addr, __m512d a)
        ///   VMOVAPD m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(double* address, Vector512<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(sbyte* address, Vector512<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(byte* address, Vector512<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(short* address, Vector512<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(ushort* address, Vector512<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(int* address, Vector512<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(uint* address, Vector512<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(long* address, Vector512<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(ulong* address, Vector512<ulong> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_ps (float * mem_addr, __m512 a)
        ///   VMOVNTPS m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(float* address, Vector512<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_pd (double * mem_addr, __m512d a)
        ///   VMOVNTPD m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(double* address, Vector512<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_sub_epi32 (__m512i a, __m512i b)
        ///   VPSUBD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> Subtract(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_sub_epi32 (__m512i a, __m512i b)
        ///   VPSUBD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Subtract(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_sub_epi64 (__m512i a, __m512i b)
        ///   VPSUBQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Subtract(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_sub_epi64 (__m512i a, __m512i b)
        ///   VPSUBQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Subtract(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512 _mm512_sub_ps (__m512 a, __m512 b)
        ///   VSUBPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{er}
        /// </summary>
        public static Vector512<float> Subtract(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_sub_pd (__m512d a, __m512d b)
        ///   VSUBPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{er}
        /// </summary>
        public static Vector512<double> Subtract(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_unpackhi_epi32 (__m512i a, __m512i b)
        ///   VPUNPCKHDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> UnpackHigh(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_unpackhi_epi32 (__m512i a, __m512i b)
        ///   VPUNPCKHDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> UnpackHigh(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_unpackhi_epi64 (__m512i a, __m512i b)
        ///   VPUNPCKHQDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> UnpackHigh(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_unpackhi_epi64 (__m512i a, __m512i b)
        ///   VPUNPCKHQDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> UnpackHigh(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512 _mm512_unpackhi_ps (__m512 a, __m512 b)
        ///   VUNPCKHPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> UnpackHigh(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_unpackhi_pd (__m512d a, __m512d b)
        ///   VUNPCKHPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> UnpackHigh(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_unpacklo_epi32 (__m512i a, __m512i b)
        ///   VPUNPCKLDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> UnpackLow(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_unpacklo_epi32 (__m512i a, __m512i b)
        ///   VPUNPCKLDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> UnpackLow(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_unpacklo_epi64 (__m512i a, __m512i b)
        ///   VPUNPCKLQDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> UnpackLow(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_unpacklo_epi64 (__m512i a, __m512i b)
        ///   VPUNPCKLQDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> UnpackLow(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512 _mm512_unpacklo_ps (__m512 a, __m512 b)
        ///   VUNPCKLPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> UnpackLow(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_unpacklo_pd (__m512d a, __m512d b)
        ///   VUNPCKLPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> UnpackLow(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_xor_si512 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<byte> Xor(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_xor_si512 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<sbyte> Xor(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_xor_si512 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<short> Xor(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_xor_si512 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<ushort> Xor(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_xor_epi32 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> Xor(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_xor_epi32 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Xor(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_xor_epi64 (__m512i a, __m512i b)
        ///   VPXORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Xor(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_xor_epi64 (__m512i a, __m512i b)
        ///   VPXORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Xor(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }
    }
}
