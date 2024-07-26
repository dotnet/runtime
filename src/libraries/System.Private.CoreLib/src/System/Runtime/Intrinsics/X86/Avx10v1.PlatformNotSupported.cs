// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>This class provides access to X86 Avx10.1 hardware instructions via intrinsics</summary>
    [CLSCompliant(false)]
    public abstract class Avx10v1 : Avx2
    {
        internal Avx10v1() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

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
        /// __m128d _mm_add_round_sd (__m128d a, __m128d b, int rounding)
        ///   VADDSD xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<double> AddScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_add_round_ss (__m128 a, __m128 b, int rounding)
        ///   VADDSS xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<float> AddScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_alignr_epi32 (__m128i a, __m128i b, const int count)
        ///   VALIGND xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
        /// </summary>
        public static Vector128<int> AlignRight32(Vector128<int> left, Vector128<int> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_alignr_epi32 (__m128i a, __m128i b, const int count)
        ///   VALIGND xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
        /// </summary>
        public static Vector128<uint> AlignRight32(Vector128<uint> left, Vector128<uint> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_alignr_epi32 (__m256i a, __m256i b, const int count)
        ///   VALIGND ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<int> AlignRight32(Vector256<int> left, Vector256<int> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_alignr_epi32 (__m256i a, __m256i b, const int count)
        ///   VALIGND ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<uint> AlignRight32(Vector256<uint> left, Vector256<uint> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_alignr_epi64 (__m128i a, __m128i b, const int count)
        ///   VALIGNQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8
        /// </summary>
        public static Vector128<long> AlignRight64(Vector128<long> left, Vector128<long> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_alignr_epi64 (__m128i a, __m128i b, const int count)
        ///   VALIGNQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8
        /// </summary>
        public static Vector128<ulong> AlignRight64(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_alignr_epi64 (__m256i a, __m256i b, const int count)
        ///   VALIGNQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<long> AlignRight64(Vector256<long> left, Vector256<long> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_alignr_epi64 (__m256i a, __m256i b, const int count)
        ///   VALIGNQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<ulong> AlignRight64(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_broadcast_i32x2 (__m128i a)
        ///   VBROADCASTI32x2 xmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector128<int> BroadcastPairScalarToVector128(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_broadcast_i32x2 (__m128i a)
        ///   VBROADCASTI32x2 xmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector128<uint> BroadcastPairScalarToVector128(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_broadcast_f32x2 (__m128 a)
        ///   VBROADCASTF32x2 ymm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector256<float> BroadcastPairScalarToVector256(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcast_i32x2 (__m128i a)
        ///   VBROADCASTI32x2 ymm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector256<int> BroadcastPairScalarToVector256(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcast_i32x2 (__m128i a)
        ///   VBROADCASTI32x2 ymm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector256<uint> BroadcastPairScalarToVector256(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpgt_epu8 (__m128i a, __m128i b)
        ///   VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(6)
        /// </summary>
        public static Vector128<byte> CompareGreaterThan(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpgt_epu32 (__m128i a, __m128i b)
        ///   VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(6)
        /// </summary>
        public static Vector128<uint> CompareGreaterThan(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpgt_epu64 (__m128i a, __m128i b)
        ///   VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(6)
        /// </summary>
        public static Vector128<ulong> CompareGreaterThan(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpgt_epu16 (__m128i a, __m128i b)
        ///   VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(6)
        /// </summary>
        public static Vector128<ushort> CompareGreaterThan(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpgt_epu8 (__m256i a, __m256i b)
        ///   VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(6)
        /// </summary>
        public static Vector256<byte> CompareGreaterThan(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpgt_epu32 (__m256i a, __m256i b)
        ///   VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(6)
        /// </summary>
        public static Vector256<uint> CompareGreaterThan(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpgt_epu64 (__m256i a, __m256i b)
        ///   VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(6)
        /// </summary>
        public static Vector256<ulong> CompareGreaterThan(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpgt_epu16 (__m256i a, __m256i b)
        ///   VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(6)
        /// </summary>
        public static Vector256<ushort> CompareGreaterThan(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpge_epu8 (__m128i a, __m128i b)
        ///   VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(5)
        /// </summary>
        public static Vector128<byte> CompareGreaterThanOrEqual(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpge_epi32 (__m128i a, __m128i b)
        ///   VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(5)
        /// </summary>
        public static Vector128<int> CompareGreaterThanOrEqual(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpge_epi64 (__m128i a, __m128i b)
        ///   VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(5)
        /// </summary>
        public static Vector128<long> CompareGreaterThanOrEqual(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpge_epi8 (__m128i a, __m128i b)
        ///   VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(5)
        /// </summary>
        public static Vector128<sbyte> CompareGreaterThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpge_epi16 (__m128i a, __m128i b)
        ///   VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(5)
        /// </summary>
        public static Vector128<short> CompareGreaterThanOrEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpge_epu32 (__m128i a, __m128i b)
        ///   VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(5)
        /// </summary>
        public static Vector128<uint> CompareGreaterThanOrEqual(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpge_epu64 (__m128i a, __m128i b)
        ///   VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(5)
        /// </summary>
        public static Vector128<ulong> CompareGreaterThanOrEqual(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpge_epu16 (__m128i a, __m128i b)
        ///   VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(5)
        /// </summary>
        public static Vector128<ushort> CompareGreaterThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpge_epu8 (__m256i a, __m256i b)
        ///   VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(5)
        /// </summary>
        public static Vector256<byte> CompareGreaterThanOrEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpge_epi32 (__m256i a, __m256i b)
        ///   VPCMPD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(5)
        /// </summary>
        public static Vector256<int> CompareGreaterThanOrEqual(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpge_epi64 (__m256i a, __m256i b)
        ///   VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(5)
        /// </summary>
        public static Vector256<long> CompareGreaterThanOrEqual(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpge_epi8 (__m256i a, __m256i b)
        ///   VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(5)
        /// </summary>
        public static Vector256<sbyte> CompareGreaterThanOrEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpge_epi16 (__m256i a, __m256i b)
        ///   VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(5)
        /// </summary>
        public static Vector256<short> CompareGreaterThanOrEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpge_epu32 (__m256i a, __m256i b)
        ///   VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(5)
        /// </summary>
        public static Vector256<uint> CompareGreaterThanOrEqual(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpge_epu64 (__m256i a, __m256i b)
        ///   VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(5)
        /// </summary>
        public static Vector256<ulong> CompareGreaterThanOrEqual(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpge_epu16 (__m256i a, __m256i b)
        ///   VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(5)
        /// </summary>
        public static Vector256<ushort> CompareGreaterThanOrEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmplt_epi32 (__m128i a, __m128i b)
        ///   VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(1)
        /// </summary>
        public static new Vector128<int> CompareLessThan(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmplt_epi8 (__m128i a, __m128i b)
        ///   VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(1)
        /// </summary>
        public static new Vector128<sbyte> CompareLessThan(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmplt_epi16 (__m128i a, __m128i b)
        ///   VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(1)
        /// </summary>
        public static new Vector128<short> CompareLessThan(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmplt_epu8 (__m128i a, __m128i b)
        ///   VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(1)
        /// </summary>
        public static Vector128<byte> CompareLessThan(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmplt_epi64 (__m128i a, __m128i b)
        ///   VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(1)
        /// </summary>
        public static Vector128<long> CompareLessThan(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmplt_epu32 (__m128i a, __m128i b)
        ///   VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(1)
        /// </summary>
        public static Vector128<uint> CompareLessThan(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmplt_epu64 (__m128i a, __m128i b)
        ///   VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(1)
        /// </summary>
        public static Vector128<ulong> CompareLessThan(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmplt_epu16 (__m128i a, __m128i b)
        ///   VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(1)
        /// </summary>
        public static Vector128<ushort> CompareLessThan(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmplt_epu8 (__m256i a, __m256i b)
        ///   VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(1)
        /// </summary>
        public static Vector256<byte> CompareLessThan(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmplt_epi32 (__m256i a, __m256i b)
        ///   VPCMPD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(1)
        /// </summary>
        public static Vector256<int> CompareLessThan(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmplt_epi64 (__m256i a, __m256i b)
        ///   VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(1)
        /// </summary>
        public static Vector256<long> CompareLessThan(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmplt_epi8 (__m256i a, __m256i b)
        ///   VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(1)
        /// </summary>
        public static Vector256<sbyte> CompareLessThan(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmplt_epi16 (__m256i a, __m256i b)
        ///   VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(1)
        /// </summary>
        public static Vector256<short> CompareLessThan(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmplt_epu32 (__m256i a, __m256i b)
        ///   VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(1)
        /// </summary>
        public static Vector256<uint> CompareLessThan(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmplt_epu64 (__m256i a, __m256i b)
        ///   VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(1)
        /// </summary>
        public static Vector256<ulong> CompareLessThan(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmplt_epu16 (__m256i a, __m256i b)
        ///   VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(1)
        /// </summary>
        public static Vector256<ushort> CompareLessThan(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmple_epu8 (__m128i a, __m128i b)
        ///   VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(2)
        /// </summary>
        public static Vector128<byte> CompareLessThanOrEqual(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmple_epi32 (__m128i a, __m128i b)
        ///   VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(2)
        /// </summary>
        public static Vector128<int> CompareLessThanOrEqual(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmple_epi64 (__m128i a, __m128i b)
        ///   VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(2)
        /// </summary>
        public static Vector128<long> CompareLessThanOrEqual(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmple_epi8 (__m128i a, __m128i b)
        ///   VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(2)
        /// </summary>
        public static Vector128<sbyte> CompareLessThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmple_epi16 (__m128i a, __m128i b)
        ///   VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(2)
        /// </summary>
        public static Vector128<short> CompareLessThanOrEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmple_epu32 (__m128i a, __m128i b)
        ///   VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(2)
        /// </summary>
        public static Vector128<uint> CompareLessThanOrEqual(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmple_epu64 (__m128i a, __m128i b)
        ///   VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(2)
        /// </summary>
        public static Vector128<ulong> CompareLessThanOrEqual(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmple_epu16 (__m128i a, __m128i b)
        ///   VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(2)
        /// </summary>
        public static Vector128<ushort> CompareLessThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmple_epu8 (__m256i a, __m256i b)
        ///   VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(2)
        /// </summary>
        public static Vector256<byte> CompareLessThanOrEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmple_epi32 (__m256i a, __m256i b)
        ///   VPCMPD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(2)
        /// </summary>
        public static Vector256<int> CompareLessThanOrEqual(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmple_epi64 (__m256i a, __m256i b)
        ///   VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(2)
        /// </summary>
        public static Vector256<long> CompareLessThanOrEqual(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmple_epi8 (__m256i a, __m256i b)
        ///   VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(2)
        /// </summary>
        public static Vector256<sbyte> CompareLessThanOrEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmple_epi16 (__m256i a, __m256i b)
        ///   VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(2)
        /// </summary>
        public static Vector256<short> CompareLessThanOrEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmple_epu32 (__m256i a, __m256i b)
        ///   VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(2)
        /// </summary>
        public static Vector256<uint> CompareLessThanOrEqual(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmple_epu64 (__m256i a, __m256i b)
        ///   VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(2)
        /// </summary>
        public static Vector256<ulong> CompareLessThanOrEqual(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmple_epu16 (__m256i a, __m256i b)
        ///   VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(2)
        /// </summary>
        public static Vector256<ushort> CompareLessThanOrEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpne_epu8 (__m128i a, __m128i b)
        ///   VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(4)
        /// </summary>
        public static Vector128<byte> CompareNotEqual(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpne_epi32 (__m128i a, __m128i b)
        ///   VPCMPD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(4)
        /// </summary>
        public static Vector128<int> CompareNotEqual(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpne_epi64 (__m128i a, __m128i b)
        ///   VPCMPQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(4)
        /// </summary>
        public static Vector128<long> CompareNotEqual(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpne_epi8 (__m128i a, __m128i b)
        ///   VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(4)
        /// </summary>
        public static Vector128<sbyte> CompareNotEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpne_epi16 (__m128i a, __m128i b)
        ///   VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(4)
        /// </summary>
        public static Vector128<short> CompareNotEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpne_epu32 (__m128i a, __m128i b)
        ///   VPCMPUD k1 {k2}, xmm2, xmm3/m128/m32bcst, imm8(4)
        /// </summary>
        public static Vector128<uint> CompareNotEqual(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpne_epu64 (__m128i a, __m128i b)
        ///   VPCMPUQ k1 {k2}, xmm2, xmm3/m128/m64bcst, imm8(4)
        /// </summary>
        public static Vector128<ulong> CompareNotEqual(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpne_epu16 (__m128i a, __m128i b)
        ///   VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(4)
        /// </summary>
        public static Vector128<ushort> CompareNotEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpne_epu8 (__m256i a, __m256i b)
        ///   VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(4)
        /// </summary>
        public static Vector256<byte> CompareNotEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpne_epi32 (__m256i a, __m256i b)
        ///   VPCMPD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(4)
        /// </summary>
        public static Vector256<int> CompareNotEqual(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpne_epi64 (__m256i a, __m256i b)
        ///   VPCMPQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(4)
        /// </summary>
        public static Vector256<long> CompareNotEqual(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpne_epi8 (__m256i a, __m256i b)
        ///   VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(4)
        /// </summary>
        public static Vector256<sbyte> CompareNotEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpne_epi16 (__m256i a, __m256i b)
        ///   VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(4)
        /// </summary>
        public static Vector256<short> CompareNotEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpne_epu32 (__m256i a, __m256i b)
        ///   VPCMPUD k1 {k2}, ymm2, ymm3/m256/m32bcst, imm8(4)
        /// </summary>
        public static Vector256<uint> CompareNotEqual(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpne_epu64 (__m256i a, __m256i b)
        ///   VPCMPUQ k1 {k2}, ymm2, ymm3/m256/m64bcst, imm8(4)
        /// </summary>
        public static Vector256<ulong> CompareNotEqual(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpne_epu16 (__m256i a, __m256i b)
        ///   VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(4)
        /// </summary>
        public static Vector256<ushort> CompareNotEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cvtsi32_sd (__m128d a, int b)
        ///   VCVTUSI2SD xmm1, xmm2, r/m32
        /// </summary>
        public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cvt_roundi32_ss (__m128 a, int b, int rounding)
        /// VCVTSI2SS xmm1, xmm2, r32 {er}
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, int value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cvt_roundi32_ss (__m128 a, int b, int rounding)
        /// VCVTUSI2SS xmm1, xmm2, r32 {er}
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, uint value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cvtsi32_ss (__m128 a, int b)
        ///   VCVTUSI2SS xmm1, xmm2, r/m32
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cvt_roundsd_ss (__m128 a, __m128d b, int rounding)
        /// VCVTSD2SS xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cvt_roundsd_i32 (__m128d a, int rounding)
        ///   VCVTSD2SI r32, xmm1 {er}
        /// </summary>
        public static int ConvertToInt32(Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cvt_roundss_i32 (__m128 a, int rounding)
        ///   VCVTSS2SIK r32, xmm1 {er}
        /// </summary>
        public static int ConvertToInt32(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// unsigned int _mm_cvt_roundsd_u32 (__m128d a, int rounding)
        ///   VCVTSD2USI r32, xmm1 {er}
        /// </summary>
        public static uint ConvertToUInt32(Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// unsigned int _mm_cvtsd_u32 (__m128d a)
        ///   VCVTSD2USI r32, xmm1/m64{er}
        /// </summary>
        public static uint ConvertToUInt32(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// unsigned int _mm_cvt_roundss_u32 (__m128 a, int rounding)
        ///   VCVTSS2USI r32, xmm1 {er}
        /// </summary>
        public static uint ConvertToUInt32(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// unsigned int _mm_cvtss_u32 (__m128 a)
        ///   VCVTSS2USI r32, xmm1/m32{er}
        /// </summary>
        public static uint ConvertToUInt32(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// unsigned int _mm_cvttsd_u32 (__m128d a)
        ///   VCVTTSD2USI r32, xmm1/m64{er}
        /// </summary>
        public static uint ConvertToUInt32WithTruncation(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// unsigned int _mm_cvttss_u32 (__m128 a)
        ///   VCVTTSS2USI r32, xmm1/m32{er}
        /// </summary>
        public static uint ConvertToUInt32WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi32_epi8 (__m128i a)
        ///   VPMOVDB xmm1/m32 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi64_epi8 (__m128i a)
        ///   VPMOVQB xmm1/m16 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi16_epi8 (__m128i a)
        ///   VPMOVWB xmm1/m64 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector128<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi32_epi8 (__m128i a)
        ///   VPMOVDB xmm1/m32 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi64_epi8 (__m128i a)
        ///   VPMOVQB xmm1/m16 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi16_epi8 (__m128i a)
        ///   VPMOVWB xmm1/m64 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi32_epi8 (__m256i a)
        ///   VPMOVDB xmm1/m64 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi64_epi8 (__m256i a)
        ///   VPMOVQB xmm1/m32 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi16_epi8 (__m256i a)
        ///   VPMOVWB xmm1/m128 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi32_epi8 (__m256i a)
        ///   VPMOVDB xmm1/m64 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi64_epi8 (__m256i a)
        ///   VPMOVQB xmm1/m32 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi16_epi8 (__m256i a)
        ///   VPMOVWB xmm1/m128 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtusepi32_epi8 (__m128i a)
        ///   VPMOVUSDB xmm1/m32 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtusepi64_epi8 (__m128i a)
        ///   VPMOVUSQB xmm1/m16 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtusepi16_epi8 (__m128i a)
        ///   VPMOVUWB xmm1/m64 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtusepi32_epi8 (__m256i a)
        ///   VPMOVUSDB xmm1/m64 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtusepi64_epi8 (__m256i a)
        ///   VPMOVUSQB xmm1/m32 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtusepi16_epi8 (__m256i a)
        ///   VPMOVUWB xmm1/m128 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cvtepi64_pd (__m128i a)
        ///   VCVTQQ2PD xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<double> ConvertToVector128Double(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cvtepu32_pd (__m128i a)
        ///   VCVTUDQ2PD xmm1 {k1}{z}, xmm2/m64/m32bcst
        /// </summary>
        public static Vector128<double> ConvertToVector128Double(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cvtepu64_pd (__m128i a)
        ///   VCVTUQQ2PD xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<double> ConvertToVector128Double(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi32_epi16 (__m128i a)
        ///   VPMOVDW xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi64_epi16 (__m128i a)
        ///   VPMOVQW xmm1/m32 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi32_epi16 (__m128i a)
        ///   VPMOVDW xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi64_epi16 (__m128i a)
        ///   VPMOVQW xmm1/m32 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi32_epi16 (__m256i a)
        ///   VPMOVDW xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi64_epi16 (__m256i a)
        ///   VPMOVQW xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi32_epi16 (__m256i a)
        ///   VPMOVDW xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi64_epi16 (__m256i a)
        ///   VPMOVQW xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtsepi32_epi16 (__m128i a)
        ///   VPMOVSDW xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtsepi64_epi16 (__m128i a)
        ///   VPMOVSQW xmm1/m32 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtsepi32_epi16 (__m256i a)
        ///   VPMOVSDW xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtsepi64_epi16 (__m256i a)
        ///   VPMOVSQW xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi64_epi32 (__m128i a)
        ///   VPMOVQD xmm1/m64 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi64_epi32 (__m128i a)
        ///   VPMOVQD xmm1/m64 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi64_epi32 (__m256i a)
        ///   VPMOVQD xmm1/m128 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi64_epi32 (__m256i a)
        ///   VPMOVQD xmm1/m128 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtsepi64_epi32 (__m128i a)
        ///   VPMOVSQD xmm1/m64 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32WithSaturation(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtsepi64_epi32 (__m256i a)
        ///   VPMOVSQD xmm1/m128 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32WithSaturation(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtpd_epi64 (__m128d a)
        ///   VCVTPD2QQ xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtps_epi64 (__m128 a)
        ///   VCVTPS2QQ xmm1 {k1}{z}, xmm2/m64/m32bcst
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvttpd_epi64 (__m128d a)
        ///   VCVTTPD2QQ xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64WithTruncation(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvttps_epi64 (__m128 a)
        ///   VCVTTPS2QQ xmm1 {k1}{z}, xmm2/m64/m32bcst
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi32_epi8 (__m128i a)
        ///   VPMOVDB xmm1/m32 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi64_epi8 (__m128i a)
        ///   VPMOVQB xmm1/m16 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi16_epi8 (__m128i a)
        ///   VPMOVWB xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector128<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi32_epi8 (__m128i a)
        ///   VPMOVDB xmm1/m32 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi64_epi8 (__m128i a)
        ///   VPMOVQB xmm1/m16 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi16_epi8 (__m128i a)
        ///   VPMOVWB xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi32_epi8 (__m256i a)
        ///   VPMOVDB xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi64_epi8 (__m256i a)
        ///   VPMOVQB xmm1/m32 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi16_epi8 (__m256i a)
        ///   VPMOVWB xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi32_epi8 (__m256i a)
        ///   VPMOVDB xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi64_epi8 (__m256i a)
        ///   VPMOVQB xmm1/m32 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi16_epi8 (__m256i a)
        ///   VPMOVWB xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtsepi32_epi8 (__m128i a)
        ///   VPMOVSDB xmm1/m32 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtsepi64_epi8 (__m128i a)
        ///   VPMOVSQB xmm1/m16 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtsepi16_epi8 (__m128i a)
        ///   VPMOVSWB xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector128<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtsepi32_epi8 (__m256i a)
        ///   VPMOVSDB xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtsepi64_epi8 (__m256i a)
        ///   VPMOVSQB xmm1/m32 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtsepi16_epi8 (__m256i a)
        ///   VPMOVSWB xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector256<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cvtepi64_ps (__m128i a)
        ///   VCVTQQ2PS xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cvtepu32_ps (__m128i a)
        ///   VCVTUDQ2PS xmm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cvtepu64_ps (__m128i a)
        ///   VCVTUQQ2PS xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm256_cvtepi64_ps (__m256i a)
        ///   VCVTQQ2PS xmm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm256_cvtepu64_ps (__m256i a)
        ///   VCVTUQQ2PS xmm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi32_epi16 (__m128i a)
        ///   VPMOVDW xmm1/m64 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi64_epi16 (__m128i a)
        ///   VPMOVQW xmm1/m32 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi32_epi16 (__m128i a)
        ///   VPMOVDW xmm1/m64 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi64_epi16 (__m128i a)
        ///   VPMOVQW xmm1/m32 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi32_epi16 (__m256i a)
        ///   VPMOVDW xmm1/m128 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi64_epi16 (__m256i a)
        ///   VPMOVQW xmm1/m64 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi32_epi16 (__m256i a)
        ///   VPMOVDW xmm1/m128 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi64_epi16 (__m256i a)
        ///   VPMOVQW xmm1/m64 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtusepi32_epi16 (__m128i a)
        ///   VPMOVUSDW xmm1/m64 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtusepi64_epi16 (__m128i a)
        ///   VPMOVUSQW xmm1/m32 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtusepi32_epi16 (__m256i a)
        ///   VPMOVUSDW xmm1/m128 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtusepi64_epi16 (__m256i a)
        ///   VPMOVUSQW xmm1/m64 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtpd_epu32 (__m128d a)
        ///   VCVTPD2UDQ xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtps_epu32 (__m128 a)
        ///   VCVTPS2UDQ xmm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi64_epi32 (__m128i a)
        ///   VPMOVQD xmm1/m128 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi64_epi32 (__m128i a)
        ///   VPMOVQD xmm1/m128 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtpd_epu32 (__m256d a)
        ///   VCVTPD2UDQ xmm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi64_epi32 (__m256i a)
        ///   VPMOVQD xmm1/m128 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtepi64_epi32 (__m256i a)
        ///   VPMOVQD xmm1/m128 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtusepi64_epi32 (__m128i a)
        ///   VPMOVUSQD xmm1/m128 {k1}{z}, xmm2
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32WithSaturation(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvtusepi64_epi32 (__m256i a)
        ///   VPMOVUSQD xmm1/m128 {k1}{z}, ymm2
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32WithSaturation(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvttpd_epu32 (__m128d a)
        ///   VCVTTPD2UDQ xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32WithTruncation(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvttps_epu32 (__m128 a)
        ///   VCVTTPS2UDQ xmm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_cvttpd_epu32 (__m256d a)
        ///   VCVTTPD2UDQ xmm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector128<uint> ConvertToVector128UInt32WithTruncation(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtpd_epu64 (__m128d a)
        ///   VCVTPD2UQQ xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<ulong> ConvertToVector128UInt64(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtps_epu64 (__m128 a)
        ///   VCVTPS2UQQ xmm1 {k1}{z}, xmm2/m64/m32bcst
        /// </summary>
        public static Vector128<ulong> ConvertToVector128UInt64(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvttpd_epu64 (__m128d a)
        ///   VCVTTPD2UQQ xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<ulong> ConvertToVector128UInt64WithTruncation(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvttps_epu64 (__m128 a)
        ///   VCVTTPS2UQQ xmm1 {k1}{z}, xmm2/m64/m32bcst
        /// </summary>
        public static Vector128<ulong> ConvertToVector128UInt64WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm512_cvtepu32_pd (__m128i a)
        ///   VCVTUDQ2PD ymm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector256<double> ConvertToVector256Double(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_cvtepi64_pd (__m256i a)
        ///   VCVTQQ2PD ymm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector256<double> ConvertToVector256Double(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_cvtepu64_pd (__m256i a)
        ///   VCVTUQQ2PD ymm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector256<double> ConvertToVector256Double(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cvtps_epi64 (__m128 a)
        ///   VCVTPS2QQ ymm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cvtpd_epi64 (__m256d a)
        ///   VCVTPD2QQ ymm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cvttps_epi64 (__m128 a)
        ///   VCVTTPS2QQ ymm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cvttpd_epi64 (__m256d a)
        ///   VCVTTPD2QQ ymm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64WithTruncation(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_cvtepu32_ps (__m256i a)
        ///   VCVTUDQ2PS ymm1 {k1}{z}, ymm2/m256/m32bcst
        /// </summary>
        public static Vector256<float> ConvertToVector256Single(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cvtps_epu32 (__m256 a)
        ///   VCVTPS2UDQ ymm1 {k1}{z}, ymm2/m256/m32bcst
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cvttps_epu32 (__m256 a)
        ///   VCVTTPS2UDQ ymm1 {k1}{z}, ymm2/m256/m32bcst
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32WithTruncation(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cvtps_epu64 (__m128 a)
        ///   VCVTPS2UQQ ymm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector256<ulong> ConvertToVector256UInt64(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cvtpd_epu64 (__m256d a)
        ///   VCVTPD2UQQ ymm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector256<ulong> ConvertToVector256UInt64(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cvttps_epu64 (__m128 a)
        ///   VCVTTPS2UQQ ymm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector256<ulong> ConvertToVector256UInt64WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cvttpd_epu64 (__m256d a)
        ///   VCVTTPD2UQQ ymm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector256<ulong> ConvertToVector256UInt64WithTruncation(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_conflict_epi32 (__m128i a)
        ///   VPCONFLICTD xmm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector128<int> DetectConflicts(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_conflict_epi64 (__m128i a)
        ///   VPCONFLICTQ xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<long> DetectConflicts(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_conflict_epi32 (__m128i a)
        ///   VPCONFLICTD xmm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector128<uint> DetectConflicts(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_conflict_epi64 (__m128i a)
        ///   VPCONFLICTQ xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<ulong> DetectConflicts(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_conflict_epi32 (__m256i a)
        ///   VPCONFLICTD ymm1 {k1}{z}, ymm2/m256/m32bcst
        /// </summary>
        public static Vector256<int> DetectConflicts(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_conflict_epi64 (__m256i a)
        ///   VPCONFLICTQ ymm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector256<long> DetectConflicts(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_conflict_epi32 (__m256i a)
        ///   VPCONFLICTD ymm1 {k1}{z}, ymm2/m256/m32bcst
        /// </summary>
        public static Vector256<uint> DetectConflicts(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_conflict_epi64 (__m256i a)
        ///   VPCONFLICTQ ymm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector256<ulong> DetectConflicts(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_div_round_sd (__m128d a, __m128d b, int rounding)
        ///   VDIVSS xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<double> DivideScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_div_round_ss (__m128 a, __m128 b, int rounding)
        ///   VDIVSD xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<float> DivideScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_fixupimm_pd(__m128d a, __m128d b, __m128i tbl, int imm);
        ///   VFIXUPIMMPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8
        /// </summary>
        public static Vector128<double> Fixup(Vector128<double> left, Vector128<double> right, Vector128<long> table, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fixupimm_ps(__m128 a, __m128 b, __m128i tbl, int imm);
        ///   VFIXUPIMMPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
        /// </summary>
        public static Vector128<float> Fixup(Vector128<float> left, Vector128<float> right, Vector128<int> table, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_fixupimm_pd(__m256d a, __m256d b, __m256i tbl, int imm);
        ///   VFIXUPIMMPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<double> Fixup(Vector256<double> left, Vector256<double> right, Vector256<long> table, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_fixupimm_ps(__m256 a, __m256 b, __m256i tbl, int imm);
        ///   VFIXUPIMMPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<float> Fixup(Vector256<float> left, Vector256<float> right, Vector256<int> table, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_fixupimm_sd(__m128d a, __m128d b, __m128i tbl, int imm);
        ///   VFIXUPIMMSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8
        /// </summary>
        public static Vector128<double> FixupScalar(Vector128<double> left, Vector128<double> right, Vector128<long> table, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fixupimm_ss(__m128 a, __m128 b, __m128i tbl, int imm);
        ///   VFIXUPIMMSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8
        /// </summary>
        public static Vector128<float> FixupScalar(Vector128<float> left, Vector128<float> right, Vector128<int> table, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_fnmadd_round_sd (__m128d a, __m128d b, __m128d c, int r)
        ///   VFNMADDSD xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<double> FusedMultiplyAddNegatedScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fnmadd_round_ss (__m128 a, __m128 b, __m128 c, int r)
        ///   VFNMADDSS xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<float> FusedMultiplyAddNegatedScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_fmadd_round_sd (__m128d a, __m128d b, __m128d c, int r)
        ///   VFMADDSD xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<double> FusedMultiplyAddScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fmadd_round_ss (__m128 a, __m128 b, __m128 c, int r)
        ///   VFMADDSS xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<float> FusedMultiplyAddScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_fnmsub_round_sd (__m128d a, __m128d b, __m128d c, int r)
        ///   VFNMSUBSS xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<double> FusedMultiplySubtractNegatedScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fnmsub_round_ss (__m128 a, __m128 b, __m128 c, int r)
        ///   VFNMSUBSS xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<float> FusedMultiplySubtractNegatedScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_fmsub_round_sd (__m128d a, __m128d b, __m128d c, int r)
        ///   VFMSUBSS xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<double> FusedMultiplySubtractScalar(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_fmsub_round_ss (__m128 a, __m128 b, __m128 c, int r)
        ///   VFMSUBSS xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<float> FusedMultiplySubtractScalar(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_getexp_pd (__m128d a)
        ///   VGETEXPPD xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<double> GetExponent(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_getexp_ps (__m128 a)
        ///   VGETEXPPS xmm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector128<float> GetExponent(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_getexp_pd (__m256d a)
        ///   VGETEXPPD ymm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector256<double> GetExponent(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_getexp_ps (__m256 a)
        ///   VGETEXPPS ymm1 {k1}{z}, ymm2/m256/m32bcst
        /// </summary>
        public static Vector256<float> GetExponent(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_getexp_sd (__m128d a, __m128d b)
        ///   VGETEXPSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> GetExponentScalar(Vector128<double> upper, Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_getexp_sd (__m128d a)
        ///   VGETEXPSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}
        /// </summary>
        public static Vector128<double> GetExponentScalar(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_getexp_ss (__m128 a, __m128 b)
        ///   VGETEXPSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> GetExponentScalar(Vector128<float> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_getexp_ss (__m128 a)
        ///   VGETEXPSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}
        /// </summary>
        public static Vector128<float> GetExponentScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_getmant_pd (__m128d a)
        ///   VGETMANTPD xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<double> GetMantissa(Vector128<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_getmant_ps (__m128 a)
        ///   VGETMANTPS xmm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector128<float> GetMantissa(Vector128<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_getmant_pd (__m256d a)
        ///   VGETMANTPD ymm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector256<double> GetMantissa(Vector256<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_getmant_ps (__m256 a)
        ///   VGETMANTPS ymm1 {k1}{z}, ymm2/m256/m32bcst
        /// </summary>
        public static Vector256<float> GetMantissa(Vector256<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_getmant_sd (__m128d a, __m128d b)
        ///   VGETMANTSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> GetMantissaScalar(Vector128<double> upper, Vector128<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_getmant_sd (__m128d a)
        ///   VGETMANTSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}
        /// </summary>
        public static Vector128<double> GetMantissaScalar(Vector128<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_getmant_ss (__m128 a, __m128 b)
        ///   VGETMANTSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> GetMantissaScalar(Vector128<float> upper, Vector128<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_getmant_ss (__m128 a)
        ///   VGETMANTSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}
        /// </summary>
        public static Vector128<float> GetMantissaScalar(Vector128<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_lzcnt_epi32 (__m128i a)
        ///   VPLZCNTD xmm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector128<int> LeadingZeroCount(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_lzcnt_epi64 (__m128i a)
        ///   VPLZCNTQ xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<long> LeadingZeroCount(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_lzcnt_epi32 (__m128i a)
        ///   VPLZCNTD xmm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector128<uint> LeadingZeroCount(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_lzcnt_epi64 (__m128i a)
        ///   VPLZCNTQ xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<ulong> LeadingZeroCount(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_lzcnt_epi32 (__m256i a)
        ///   VPLZCNTD ymm1 {k1}{z}, ymm2/m256/m32bcst
        /// </summary>
        public static Vector256<int> LeadingZeroCount(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_lzcnt_epi64 (__m256i a)
        ///   VPLZCNTQ ymm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector256<long> LeadingZeroCount(Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_lzcnt_epi32 (__m256i a)
        ///   VPLZCNTD ymm1 {k1}{z}, ymm2/m256/m32bcst
        /// </summary>
        public static Vector256<uint> LeadingZeroCount(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_lzcnt_epi64 (__m256i a)
        ///   VPLZCNTQ ymm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector256<ulong> LeadingZeroCount(Vector256<ulong> value) { throw new PlatformNotSupportedException(); }

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
        /// __m128i _mm_mullo_epi64 (__m128i a, __m128i b)
        ///   VPMULLQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<long> MultiplyLow(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_mullo_epi64 (__m128i a, __m128i b)
        ///   VPMULLQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<ulong> MultiplyLow(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_mullo_epi64 (__m256i a, __m256i b)
        ///   VPMULLQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<long> MultiplyLow(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_mullo_epi64 (__m256i a, __m256i b)
        ///   VPMULLQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<ulong> MultiplyLow(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_mul_round_sd (__m128d a, __m128d b, int rounding)
        ///   VMULSD xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<double> MultiplyScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_mul_round_ss (__m128 a, __m128 b, int rounding)
        ///   VMULSS xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<float> MultiplyScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_multishift_epi64_epi8(__m128i a, __m128i b)
        ///   VPMULTISHIFTQB xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<byte> MultiShift(Vector128<byte> control, Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_multishift_epi64_epi8(__m128i a, __m128i b)
        ///   VPMULTISHIFTQB xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<sbyte> MultiShift(Vector128<sbyte> control, Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_multishift_epi64_epi8(__m256i a, __m256i b)
        ///   VPMULTISHIFTQB ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<byte> MultiShift(Vector256<byte> control, Vector256<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_multishift_epi64_epi8(__m256i a, __m256i b)
        ///   VPMULTISHIFTQB ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<sbyte> MultiShift(Vector256<sbyte> control, Vector256<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permutevar16x16_epi16 (__m256i a, __m256i b)
        ///   VPERMW ymm1 {k1}{z}, ymm2, ymm3/m256
        /// </summary>
        public static Vector256<short> PermuteVar16x16(Vector256<short> left, Vector256<short> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permutevar16x16_epi16 (__m256i a, __m256i b)
        ///   VPERMW ymm1 {k1}{z}, ymm2, ymm3/m256
        /// </summary>
        public static Vector256<ushort> PermuteVar16x16(Vector256<ushort> left, Vector256<ushort> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permutex2var_epi16 (__m256i a, __m256i idx, __m256i b)
        ///   VPERMI2W ymm1 {k1}{z}, ymm2, ymm3/m256
        ///   VPERMT2W ymm1 {k1}{z}, ymm2, ymm3/m256
        /// </summary>
        public static Vector256<short> PermuteVar16x16x2(Vector256<short> lower, Vector256<short> indices, Vector256<short> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permutex2var_epi16 (__m256i a, __m256i idx, __m256i b)
        ///   VPERMI2W ymm1 {k1}{z}, ymm2, ymm3/m256
        ///   VPERMT2W ymm1 {k1}{z}, ymm2, ymm3/m256
        /// </summary>
        public static Vector256<ushort> PermuteVar16x16x2(Vector256<ushort> lower, Vector256<ushort> indices, Vector256<ushort> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_permutevar64x8_epi8 (__m128i a, __m128i b)
        ///   VPERMB xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<byte> PermuteVar16x8(Vector128<byte> left, Vector128<byte> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_permutevar64x8_epi8 (__m128i a, __m128i b)
        ///   VPERMB xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<sbyte> PermuteVar16x8(Vector128<sbyte> left, Vector128<sbyte> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_permutex2var_epi8 (__m128i a, __m128i idx, __m128i b)
        ///   VPERMI2B xmm1 {k1}{z}, xmm2, xmm3/m128
        ///   VPERMT2B xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<byte> PermuteVar16x8x2(Vector128<byte> lower, Vector128<byte> indices, Vector128<byte> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_permutex2var_epi8 (__m128i a, __m128i idx, __m128i b)
        ///   VPERMI2B xmm1 {k1}{z}, xmm2, xmm3/m128
        ///   VPERMT2B xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<sbyte> PermuteVar16x8x2(Vector128<sbyte> lower, Vector128<sbyte> indices, Vector128<sbyte> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_permutex2var_pd (__m128d a, __m128i idx, __m128i b)
        ///   VPERMI2PD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        ///   VPERMT2PD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<double> PermuteVar2x64x2(Vector128<double> lower, Vector128<long> indices, Vector128<double> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_permutex2var_epi64 (__m128i a, __m128i idx, __m128i b)
        ///   VPERMI2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        ///   VPERMT2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<long> PermuteVar2x64x2(Vector128<long> lower, Vector128<long> indices, Vector128<long> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_permutex2var_epi64 (__m128i a, __m128i idx, __m128i b)
        ///   VPERMI2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        ///   VPERMT2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<ulong> PermuteVar2x64x2(Vector128<ulong> lower, Vector128<ulong> indices, Vector128<ulong> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permutevar64x8_epi8 (__m256i a, __m256i b)
        ///   VPERMB ymm1 {k1}{z}, ymm2, ymm3/m256
        /// </summary>
        public static Vector256<byte> PermuteVar32x8(Vector256<byte> left, Vector256<byte> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permutevar64x8_epi8 (__m256i a, __m256i b)
        ///   VPERMB ymm1 {k1}{z}, ymm2, ymm3/m256
        /// </summary>
        public static Vector256<sbyte> PermuteVar32x8(Vector256<sbyte> left, Vector256<sbyte> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permutex2var_epi8 (__m256i a, __m256i idx, __m256i b)
        ///   VPERMI2B ymm1 {k1}{z}, ymm2, ymm3/m256
        ///   VPERMT2B ymm1 {k1}{z}, ymm2, ymm3/m256
        /// </summary>
        public static Vector256<byte> PermuteVar32x8x2(Vector256<byte> lower, Vector256<byte> indices, Vector256<byte> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permutex2var_epi8 (__m256i a, __m256i idx, __m256i b)
        ///   VPERMI2B ymm1 {k1}{z}, ymm2, ymm3/m256
        ///   VPERMT2B ymm1 {k1}{z}, ymm2, ymm3/m256
        /// </summary>
        public static Vector256<sbyte> PermuteVar32x8x2(Vector256<sbyte> lower, Vector256<sbyte> indices, Vector256<sbyte> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_permutex2var_ps (__m128 a, __m128i idx, __m128i b)
        ///   VPERMI2PS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        ///   VPERMT2PS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> PermuteVar4x32x2(Vector128<float> lower, Vector128<int> indices, Vector128<float> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_permutex2var_epi32 (__m128i a, __m128i idx, __m128i b)
        ///   VPERMI2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        ///   VPERMT2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<int> PermuteVar4x32x2(Vector128<int> lower, Vector128<int> indices, Vector128<int> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_permutex2var_epi32 (__m128i a, __m128i idx, __m128i b)
        ///   VPERMI2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        ///   VPERMT2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<uint> PermuteVar4x32x2(Vector128<uint> lower, Vector128<uint> indices, Vector128<uint> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_permute4x64_pd (__m256d a, __m256i b)
        ///   VPERMPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<double> PermuteVar4x64(Vector256<double> value, Vector256<long> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permute4x64_epi64 (__m256i a, __m256i b)
        ///   VPERMQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<long> PermuteVar4x64(Vector256<long> value, Vector256<long> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permute4x64_pd (__m256d a, __m256i b)
        ///   VPERMQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<ulong> PermuteVar4x64(Vector256<ulong> value, Vector256<ulong> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_permutex2var_pd (__m256d a, __m256i idx, __m256i b)
        ///   VPERMI2PD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        ///   VPERMT2PD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<double> PermuteVar4x64x2(Vector256<double> lower, Vector256<long> indices, Vector256<double> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permutex2var_epi64 (__m256i a, __m256i idx, __m256i b)
        ///   VPERMI2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        ///   VPERMT2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<long> PermuteVar4x64x2(Vector256<long> lower, Vector256<long> indices, Vector256<long> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permutex2var_epi64 (__m256i a, __m256i idx, __m256i b)
        ///   VPERMI2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        ///   VPERMT2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<ulong> PermuteVar4x64x2(Vector256<ulong> lower, Vector256<ulong> indices, Vector256<ulong> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_permutevar8x16_epi16 (__m128i a, __m128i b)
        ///   VPERMW xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<short> PermuteVar8x16(Vector128<short> left, Vector128<short> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_permutevar8x16_epi16 (__m128i a, __m128i b)
        ///   VPERMW xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<ushort> PermuteVar8x16(Vector128<ushort> left, Vector128<ushort> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_permutex2var_epi16 (__m128i a, __m128i idx, __m128i b)
        ///   VPERMI2W xmm1 {k1}{z}, xmm2, xmm3/m128
        ///   VPERMT2W xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<short> PermuteVar8x16x2(Vector128<short> lower, Vector128<short> indices, Vector128<short> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_permutex2var_epi16 (__m128i a, __m128i idx, __m128i b)
        ///   VPERMI2W xmm1 {k1}{z}, xmm2, xmm3/m128
        ///   VPERMT2W xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<ushort> PermuteVar8x16x2(Vector128<ushort> lower, Vector128<ushort> indices, Vector128<ushort> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_permutex2var_ps (__m256 a, __m256i idx, __m256i b)
        ///   VPERMI2PS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        ///   VPERMT2PS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        /// </summary>
        public static Vector256<float> PermuteVar8x32x2(Vector256<float> lower, Vector256<int> indices, Vector256<float> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permutex2var_epi32 (__m256i a, __m256i idx, __m256i b)
        ///   VPERMI2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        ///   VPERMT2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        /// </summary>
        public static Vector256<int> PermuteVar8x32x2(Vector256<int> lower, Vector256<int> indices, Vector256<int> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permutex2var_epi32 (__m256i a, __m256i idx, __m256i b)
        ///   VPERMI2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        ///   VPERMT2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        /// </summary>
        public static Vector256<uint> PermuteVar8x32x2(Vector256<uint> lower, Vector256<uint> indices, Vector256<uint> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_range_pd(__m128d a, __m128d b, int imm);
        ///   VRANGEPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8
        /// </summary>
        public static Vector128<double> Range(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_range_ps(__m128 a, __m128 b, int imm);
        ///   VRANGEPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
        /// </summary>
        public static Vector128<float> Range(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_range_pd(__m256d a, __m256d b, int imm);
        ///   VRANGEPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<double> Range(Vector256<double> left, Vector256<double> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_range_ps(__m256 a, __m256 b, int imm);
        ///   VRANGEPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<float> Range(Vector256<float> left, Vector256<float> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_range_sd(__m128d a, __m128d b, int imm);
        ///   VRANGESD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8
        /// </summary>
        public static Vector128<double> RangeScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_range_ss(__m128 a, __m128 b, int imm);
        ///   VRANGESS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8
        /// </summary>
        public static Vector128<float> RangeScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_rcp14_pd (__m128d a, __m128d b)
        ///   VRCP14PD xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<double> Reciprocal14(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rcp14_ps (__m128 a, __m128 b)
        ///   VRCP14PS xmm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector128<float> Reciprocal14(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_rcp14_pd (__m256d a, __m256d b)
        ///   VRCP14PD ymm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector256<double> Reciprocal14(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_rcp14_ps (__m256 a, __m256 b)
        ///   VRCP14PS ymm1 {k1}{z}, ymm2/m256/m32bcst
        /// </summary>
        public static Vector256<float> Reciprocal14(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_rcp14_sd (__m128d a, __m128d b)
        ///   VRCP14SD xmm1 {k1}{z}, xmm2, xmm3/m64
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> Reciprocal14Scalar(Vector128<double> upper, Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_rcp14_sd (__m128d a)
        ///   VRCP14SD xmm1 {k1}{z}, xmm2, xmm3/m64
        /// </summary>
        public static Vector128<double> Reciprocal14Scalar(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rcp14_ss (__m128 a, __m128 b)
        ///   VRCP14SS xmm1 {k1}{z}, xmm2, xmm3/m32
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> Reciprocal14Scalar(Vector128<float> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rcp14_ss (__m128 a)
        ///   VRCP14SS xmm1 {k1}{z}, xmm2, xmm3/m32
        /// </summary>
        public static Vector128<float> Reciprocal14Scalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_rsqrt14_pd (__m128d a, __m128d b)
        ///   VRSQRT14PD xmm1 {k1}{z}, xmm2/m128/m64bcst
        /// </summary>
        public static Vector128<double> ReciprocalSqrt14(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rsqrt14_ps (__m128 a, __m128 b)
        ///   VRSQRT14PS xmm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector128<float> ReciprocalSqrt14(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_rsqrt14_pd (__m256d a, __m256d b)
        ///   VRSQRT14PD ymm1 {k1}{z}, ymm2/m256/m64bcst
        /// </summary>
        public static Vector256<double> ReciprocalSqrt14(Vector256<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_rsqrt14_ps (__m256 a, __m256 b)
        ///   VRSQRT14PS ymm1 {k1}{z}, ymm2/m256/m32bcst
        /// </summary>
        public static Vector256<float> ReciprocalSqrt14(Vector256<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_rsqrt14_sd (__m128d a, __m128d b)
        ///   VRSQRT14SD xmm1 {k1}{z}, xmm2, xmm3/m64
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> ReciprocalSqrt14Scalar(Vector128<double> upper, Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_rsqrt14_sd (__m128d a)
        ///   VRSQRT14SD xmm1 {k1}{z}, xmm2, xmm3/m64
        /// </summary>
        public static Vector128<double> ReciprocalSqrt14Scalar(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rsqrt14_ss (__m128 a, __m128 b)
        ///   VRSQRT14SS xmm1 {k1}{z}, xmm2, xmm3/m32
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> ReciprocalSqrt14Scalar(Vector128<float> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rsqrt14_ss (__m128 a)
        ///   VRSQRT14SS xmm1 {k1}{z}, xmm2, xmm3/m32
        /// </summary>
        public static Vector128<float> ReciprocalSqrt14Scalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_reduce_pd(__m128d a, int imm);
        ///   VREDUCEPD xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8
        /// </summary>
        public static Vector128<double> Reduce(Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_reduce_ps(__m128 a, int imm);
        ///   VREDUCEPS xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8
        /// </summary>
        public static Vector128<float> Reduce(Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_reduce_pd(__m256d a, int imm);
        ///   VREDUCEPD ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<double> Reduce(Vector256<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_reduce_ps(__m256 a, int imm);
        ///   VREDUCEPS ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<float> Reduce(Vector256<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_reduce_sd(__m128d a, __m128d b, int imm);
        ///   VREDUCESD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> ReduceScalar(Vector128<double> upper, Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_reduce_sd(__m128d a, int imm);
        ///   VREDUCESD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8
        /// </summary>
        public static Vector128<double> ReduceScalar(Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_reduce_ss(__m128 a, __m128 b, int imm);
        ///   VREDUCESS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> ReduceScalar(Vector128<float> upper, Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_reduce_ss(__m128 a, int imm);
        ///   VREDUCESS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8
        /// </summary>
        public static Vector128<float> ReduceScalar(Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_rol_epi32 (__m128i a, int imm8)
        ///   VPROLD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8
        /// </summary>
        public static Vector128<int> RotateLeft(Vector128<int> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_rol_epi64 (__m128i a, int imm8)
        ///   VPROLQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8
        /// </summary>
        public static Vector128<long> RotateLeft(Vector128<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_rol_epi32 (__m128i a, int imm8)
        ///   VPROLD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8
        /// </summary>
        public static Vector128<uint> RotateLeft(Vector128<uint> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_rol_epi64 (__m128i a, int imm8)
        ///   VPROLQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8
        /// </summary>
        public static Vector128<ulong> RotateLeft(Vector128<ulong> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_rol_epi32 (__m256i a, int imm8)
        ///   VPROLD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<int> RotateLeft(Vector256<int> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_rol_epi64 (__m256i a, int imm8)
        ///   VPROLQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<long> RotateLeft(Vector256<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_rol_epi32 (__m256i a, int imm8)
        ///   VPROLD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<uint> RotateLeft(Vector256<uint> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_rol_epi64 (__m256i a, int imm8)
        ///   VPROLQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<ulong> RotateLeft(Vector256<ulong> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_rolv_epi32 (__m128i a, __m128i b)
        ///   VPROLDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<int> RotateLeftVariable(Vector128<int> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_rolv_epi64 (__m128i a, __m128i b)
        ///   VPROLQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<long> RotateLeftVariable(Vector128<long> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_rolv_epi32 (__m128i a, __m128i b)
        ///   VPROLDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<uint> RotateLeftVariable(Vector128<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_rolv_epi64 (__m128i a, __m128i b)
        ///   VPROLQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<ulong> RotateLeftVariable(Vector128<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_rolv_epi32 (__m256i a, __m256i b)
        ///   VPROLDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        /// </summary>
        public static Vector256<int> RotateLeftVariable(Vector256<int> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_rolv_epi64 (__m256i a, __m256i b)
        ///   VPROLQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<long> RotateLeftVariable(Vector256<long> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_rolv_epi32 (__m256i a, __m256i b)
        ///   VPROLDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        /// </summary>
        public static Vector256<uint> RotateLeftVariable(Vector256<uint> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_rolv_epi64 (__m256i a, __m256i b)
        ///   VPROLQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<ulong> RotateLeftVariable(Vector256<ulong> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_ror_epi32 (__m128i a, int imm8)
        ///   VPRORD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8
        /// </summary>
        public static Vector128<int> RotateRight(Vector128<int> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_ror_epi64 (__m128i a, int imm8)
        ///   VPRORQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8
        /// </summary>
        public static Vector128<long> RotateRight(Vector128<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_ror_epi32 (__m128i a, int imm8)
        ///   VPRORD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8
        /// </summary>
        public static Vector128<uint> RotateRight(Vector128<uint> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_ror_epi64 (__m128i a, int imm8)
        ///   VPRORQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8
        /// </summary>
        public static Vector128<ulong> RotateRight(Vector128<ulong> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_ror_epi32 (__m256i a, int imm8)
        ///   VPRORD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<int> RotateRight(Vector256<int> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_ror_epi64 (__m256i a, int imm8)
        ///   VPRORQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<long> RotateRight(Vector256<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_ror_epi32 (__m256i a, int imm8)
        ///   VPRORD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<uint> RotateRight(Vector256<uint> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_ror_epi64 (__m256i a, int imm8)
        ///   VPRORQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<ulong> RotateRight(Vector256<ulong> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_rorv_epi32 (__m128i a, __m128i b)
        ///   VPRORDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<int> RotateRightVariable(Vector128<int> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_rorv_epi64 (__m128i a, __m128i b)
        ///   VPRORQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<long> RotateRightVariable(Vector128<long> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_rorv_epi32 (__m128i a, __m128i b)
        ///   VPRORDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<uint> RotateRightVariable(Vector128<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_rorv_epi64 (__m128i a, __m128i b)
        ///   VPRORQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<ulong> RotateRightVariable(Vector128<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_rorv_epi32 (__m256i a, __m256i b)
        ///   VPRORDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        /// </summary>
        public static Vector256<int> RotateRightVariable(Vector256<int> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_rorv_epi64 (__m256i a, __m256i b)
        ///   VPRORQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<long> RotateRightVariable(Vector256<long> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_rorv_epi32 (__m256i a, __m256i b)
        ///   VPRORDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        /// </summary>
        public static Vector256<uint> RotateRightVariable(Vector256<uint> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_rorv_epi64 (__m256i a, __m256i b)
        ///   VPRORQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<ulong> RotateRightVariable(Vector256<ulong> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_roundscale_pd (__m128d a, int imm)
        ///   VRNDSCALEPD xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8
        /// </summary>
        public static Vector128<double> RoundScale(Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_roundscale_ps (__m128 a, int imm)
        ///   VRNDSCALEPS xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8
        /// </summary>
        public static Vector128<float> RoundScale(Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_roundscale_pd (__m256d a, int imm)
        ///   VRNDSCALEPD ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<double> RoundScale(Vector256<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_roundscale_ps (__m256 a, int imm)
        ///   VRNDSCALEPS ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<float> RoundScale(Vector256<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_roundscale_sd (__m128d a, __m128d b, int imm)
        ///   VRNDSCALESD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> RoundScaleScalar(Vector128<double> upper, Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_roundscale_sd (__m128d a, int imm)
        ///   VRNDSCALESD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8
        /// </summary>
        public static Vector128<double> RoundScaleScalar(Vector128<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_roundscale_ss (__m128 a, __m128 b, int imm)
        ///   VRNDSCALESS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> RoundScaleScalar(Vector128<float> upper, Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_roundscale_ss (__m128 a, int imm)
        ///   VRNDSCALESS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8
        /// </summary>
        public static Vector128<float> RoundScaleScalar(Vector128<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_scalef_pd (__m128d a, int imm)
        ///   VSCALEFPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<double> Scale(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_scalef_ps (__m128 a, int imm)
        ///   VSCALEFPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> Scale(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_scalef_pd (__m256d a, int imm)
        ///   VSCALEFPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<double> Scale(Vector256<double> left, Vector256<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_scalef_ps (__m256 a, int imm)
        ///   VSCALEFPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
        /// </summary>
        public static Vector256<float> Scale(Vector256<float> left, Vector256<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_scalef_round_sd (__m128d a, __m128d b)
        ///   VSCALEFSD xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<double> ScaleScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_scalef_sd (__m128d a, __m128d b)
        ///   VSCALEFSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}
        /// </summary>
        public static Vector128<double> ScaleScalar(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_scalef_round_ss (__m128 a, __m128 b)
        ///   VSCALEFSS xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<float> ScaleScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_scalef_ss (__m128 a, __m128 b)
        ///   VSCALEFSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}
        /// </summary>
        public static Vector128<float> ScaleScalar(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_sllv_epi16 (__m128i a, __m128i count)
        ///   VPSLLVW xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<short> ShiftLeftLogicalVariable(Vector128<short> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_sllv_epi16 (__m128i a, __m128i count)
        ///   VPSLLVW xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogicalVariable(Vector128<ushort> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_sllv_epi16 (__m256i a, __m256i count)
        ///   VPSLLVW ymm1 {k1}{z}, ymm2, ymm3/m256
        /// </summary>
        public static Vector256<short> ShiftLeftLogicalVariable(Vector256<short> value, Vector256<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_sllv_epi16 (__m256i a, __m256i count)
        ///   VPSLLVW ymm1 {k1}{z}, ymm2, ymm3/m256
        /// </summary>
        public static Vector256<ushort> ShiftLeftLogicalVariable(Vector256<ushort> value, Vector256<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __128i _mm_srai_epi64 (__m128i a, int imm8)
        ///   VPSRAQ xmm1 {k1}{z}, xmm2, imm8
        /// </summary>
        public static Vector128<long> ShiftRightArithmetic(Vector128<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_sra_epi64 (__m128i a, __m128i count)
        ///   VPSRAQ xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<long> ShiftRightArithmetic(Vector128<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_srai_epi64 (__m256i a, int imm8)
        ///   VPSRAQ ymm1 {k1}{z}, ymm2, imm8
        /// </summary>
        public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_sra_epi64 (__m256i a, __m128i count)
        ///   VPSRAQ ymm1 {k1}{z}, ymm2, xmm3/m128
        /// </summary>
        public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_srav_epi64 (__m128i a, __m128i count)
        ///   VPSRAVQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
        /// </summary>
        public static Vector128<long> ShiftRightArithmeticVariable(Vector128<long> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_srav_epi16 (__m128i a, __m128i count)
        ///   VPSRAVW xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticVariable(Vector128<short> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_srav_epi64 (__m256i a, __m256i count)
        ///   VPSRAVQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
        /// </summary>
        public static Vector256<long> ShiftRightArithmeticVariable(Vector256<long> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_srav_epi16 (__m256i a, __m256i count)
        ///   VPSRAVW ymm1 {k1}{z}, ymm2, ymm3/m256
        /// </summary>
        public static Vector256<short> ShiftRightArithmeticVariable(Vector256<short> value, Vector256<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_srlv_epi16 (__m128i a, __m128i count)
        ///   VPSRLVW xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<short> ShiftRightLogicalVariable(Vector128<short> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_srlv_epi16 (__m128i a, __m128i count)
        ///   VPSRLVW xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalVariable(Vector128<ushort> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_srlv_epi16 (__m256i a, __m256i count)
        ///   VPSRLVW ymm1 {k1}{z}, ymm2, ymm3/m256
        /// </summary>
        public static Vector256<short> ShiftRightLogicalVariable(Vector256<short> value, Vector256<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_srlv_epi16 (__m256i a, __m256i count)
        ///   VPSRLVW ymm1 {k1}{z}, ymm2, ymm3/m256
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalVariable(Vector256<ushort> value, Vector256<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_shuffle_f64x2 (__m256d a, __m256d b, const int imm8)
        ///   VSHUFF64x2 ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<double> Shuffle2x128(Vector256<double> left, Vector256<double> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_shuffle_f32x4 (__m256 a, __m256 b, const int imm8)
        ///   VSHUFF32x4 ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<float> Shuffle2x128(Vector256<float> left, Vector256<float> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_shuffle_i32x4 (__m256i a, __m256i b, const int imm8)
        ///   VSHUFI32x4 ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<int> Shuffle2x128(Vector256<int> left, Vector256<int> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_shuffle_i64x2 (__m256i a, __m256i b, const int imm8)
        ///   VSHUFI64x2 ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<long> Shuffle2x128(Vector256<long> left, Vector256<long> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_shuffle_i32x4 (__m256i a, __m256i b, const int imm8)
        ///   VSHUFI32x4 ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<uint> Shuffle2x128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_shuffle_i64x2 (__m256i a, __m256i b, const int imm8)
        ///   VSHUFI64x2 ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<ulong> Shuffle2x128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_sqrt_round_sd (__m128d a, __m128d b, int rounding)
        ///   VSQRTSD xmm1, xmm2 xmm3 {er}
        /// </summary>
        public static Vector128<double> SqrtScalar(Vector128<double> upper, Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_sqrt_round_ss (__m128 a, __m128 b, int rounding)
        ///   VSQRTSS xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<float> SqrtScalar(Vector128<float> upper, Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_sub_round_sd (__m128d a, __m128d b, int rounding)
        ///   VSUBSD xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<double> SubtractScalar(Vector128<double> left, Vector128<double> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_sub_round_ss (__m128 a, __m128 b, int rounding)
        ///   VSUBSS xmm1, xmm2, xmm3 {er}
        /// </summary>
        public static Vector128<float> SubtractScalar(Vector128<float> left, Vector128<float> right, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_dbsad_epu8 (__m128i a, __m128i b, int imm8)
        ///   VDBPSADBW xmm1 {k1}{z}, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<ushort> SumAbsoluteDifferencesInBlock32(Vector128<byte> left, Vector128<byte> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_dbsad_epu8 (__m256i a, __m256i b, int imm8)
        ///   VDBPSADBW ymm1 {k1}{z}, ymm2, ymm3/m256
        /// </summary>
        public static Vector256<ushort> SumAbsoluteDifferencesInBlock32(Vector256<byte> left, Vector256<byte> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, byte imm)
        ///   VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector128<byte> TernaryLogic(Vector128<byte> a, Vector128<byte> b, Vector128<byte> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_ternarylogic_pd (__m128d a, __m128d b, __m128d c, int imm)
        ///   VPTERNLOGQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector128<double> TernaryLogic(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_ternarylogic_ps (__m128 a, __m128 b, __m128 c, int imm)
        ///   VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector128<float> TernaryLogic(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_ternarylogic_epi32 (__m128i a, __m128i b, __m128i c, int imm)
        ///   VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
        /// </summary>
        public static Vector128<int> TernaryLogic(Vector128<int> a, Vector128<int> b, Vector128<int> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_ternarylogic_epi64 (__m128i a, __m128i b, __m128i c, int imm)
        ///   VPTERNLOGQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8
        /// </summary>
        public static Vector128<long> TernaryLogic(Vector128<long> a, Vector128<long> b, Vector128<long> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, byte imm)
        ///   VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector128<sbyte> TernaryLogic(Vector128<sbyte> a, Vector128<sbyte> b, Vector128<sbyte> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, short imm)
        ///   VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector128<short> TernaryLogic(Vector128<short> a, Vector128<short> b, Vector128<short> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_ternarylogic_epi32 (__m128i a, __m128i b, __m128i c, int imm)
        ///   VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
        /// </summary>
        public static Vector128<uint> TernaryLogic(Vector128<uint> a, Vector128<uint> b, Vector128<uint> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_ternarylogic_epi64 (__m128i a, __m128i b, __m128i c, int imm)
        ///   VPTERNLOGQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8
        /// </summary>
        public static Vector128<ulong> TernaryLogic(Vector128<ulong> a, Vector128<ulong> b, Vector128<ulong> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, short imm)
        ///   VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector128<ushort> TernaryLogic(Vector128<ushort> a, Vector128<ushort> b, Vector128<ushort> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, byte imm)
        ///   VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector256<byte> TernaryLogic(Vector256<byte> a, Vector256<byte> b, Vector256<byte> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_ternarylogic_pd (__m256d a, __m256d b, __m256d c, int imm)
        ///   VPTERNLOGQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector256<double> TernaryLogic(Vector256<double> a, Vector256<double> b, Vector256<double> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_ternarylogic_ps (__m256 a, __m256 b, __m256 c, int imm)
        ///   VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector256<float> TernaryLogic(Vector256<float> a, Vector256<float> b, Vector256<float> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_ternarylogic_epi32 (__m256i a, __m256i b, __m256i c, int imm)
        ///   VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<int> TernaryLogic(Vector256<int> a, Vector256<int> b, Vector256<int> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_ternarylogic_epi64 (__m256i a, __m256i b, __m256i c, int imm)
        ///   VPTERNLOGQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<long> TernaryLogic(Vector256<long> a, Vector256<long> b, Vector256<long> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, byte imm)
        ///   VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector256<sbyte> TernaryLogic(Vector256<sbyte> a, Vector256<sbyte> b, Vector256<sbyte> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, short imm)
        ///   VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector256<short> TernaryLogic(Vector256<short> a, Vector256<short> b, Vector256<short> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_ternarylogic_epi32 (__m256i a, __m256i b, __m256i c, int imm)
        ///   VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
        /// </summary>
        public static Vector256<uint> TernaryLogic(Vector256<uint> a, Vector256<uint> b, Vector256<uint> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_ternarylogic_epi64 (__m256i a, __m256i b, __m256i c, int imm)
        ///   VPTERNLOGQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
        /// </summary>
        public static Vector256<ulong> TernaryLogic(Vector256<ulong> a, Vector256<ulong> b, Vector256<ulong> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, short imm)
        ///   VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector256<ushort> TernaryLogic(Vector256<ushort> a, Vector256<ushort> b, Vector256<ushort> c, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        public new abstract class X64 : Avx2.X64
        {
            internal X64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            /// __m128 _mm_cvt_roundi64_ss (__m128 a, __int64 b, int rounding)
            ///   VCVTSI2SS xmm1, xmm2, r64 {er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, ulong value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128 _mm_cvtsi64_ss (__m128 a, __int64 b)
            ///   VCVTUSI2SS xmm1, xmm2, r/m64
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, long value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128 _mm_cvt_roundu64_ss (__m128 a, unsigned __int64 b, int rounding)
            ///   VCVTUSI2SS xmm1, xmm2, r64 {er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, ulong value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128d _mm_cvtsi64_sd (__m128d a, __int64 b)
            ///   VCVTUSI2SD xmm1, xmm2, r/m64
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, ulong value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128d _mm_cvt_roundsi64_sd (__m128d a, __int64 b, int rounding)
            ///   VCVTSI2SD xmm1, xmm2, r64 {er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, long value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128d _mm_cvt_roundu64_sd (__m128d a, unsigned __int64 b, int rounding)
            ///   VCVTUSI2SD xmm1, xmm2, r64 {er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, ulong value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __int64 _mm_cvt_roundss_i64 (__m128 a, int rounding)
            ///   VCVTSS2SI r64, xmm1 {er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static long ConvertToInt64(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __int64 _mm_cvt_roundsd_i64 (__m128d a, int rounding)
            ///   VCVTSD2SI r64, xmm1 {er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static long ConvertToInt64(Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// unsigned __int64 _mm_cvtss_u64 (__m128 a)
            ///   VCVTSS2USI r64, xmm1/m32{er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<float> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// unsigned __int64 _mm_cvt_roundss_u64 (__m128 a, int rounding)
            ///   VCVTSS2USI r64, xmm1 {er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// unsigned __int64 _mm_cvtsd_u64 (__m128d a)
            ///   VCVTSD2USI r64, xmm1/m64{er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<double> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// unsigned __int64 _mm_cvt_roundsd_u64 (__m128d a, int rounding)
            ///   VCVTSD2USI r64, xmm1 {er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// unsigned __int64 _mm_cvttss_u64 (__m128 a)
            ///   VCVTTSS2USI r64, xmm1/m32{er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong ConvertToUInt64WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// unsigned __int64 _mm_cvttsd_u64 (__m128d a)
            ///   VCVTTSD2USI r64, xmm1/m64{er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong ConvertToUInt64WithTruncation(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        }

        public abstract class V512 : Avx512BW
        {
            internal V512() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            /// __m512i _mm512_conflict_epi32 (__m512i a)
            ///   VPCONFLICTD zmm1 {k1}{z}, zmm2/m512/m32bcst
            /// </summary>
            public static Vector512<int> DetectConflicts(Vector512<int> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_conflict_epi32 (__m512i a)
            ///   VPCONFLICTD zmm1 {k1}{z}, zmm2/m512/m32bcst
            /// </summary>
            public static Vector512<uint> DetectConflicts(Vector512<uint> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_conflict_epi64 (__m512i a)
            ///   VPCONFLICTQ zmm1 {k1}{z}, zmm2/m512/m64bcst
            /// </summary>
            public static Vector512<long> DetectConflicts(Vector512<long> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_conflict_epi64 (__m512i a)
            ///   VPCONFLICTQ zmm1 {k1}{z}, zmm2/m512/m64bcst
            /// </summary>
            public static Vector512<ulong> DetectConflicts(Vector512<ulong> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_lzcnt_epi32 (__m512i a)
            ///   VPLZCNTD zmm1 {k1}{z}, zmm2/m512/m32bcst
            /// </summary>
            public static Vector512<int> LeadingZeroCount(Vector512<int> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_lzcnt_epi32 (__m512i a)
            ///   VPLZCNTD zmm1 {k1}{z}, zmm2/m512/m32bcst
            /// </summary>
            public static Vector512<uint> LeadingZeroCount(Vector512<uint> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_lzcnt_epi64 (__m512i a)
            ///   VPLZCNTQ zmm1 {k1}{z}, zmm2/m512/m64bcst
            /// </summary>
            public static Vector512<long> LeadingZeroCount(Vector512<long> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_lzcnt_epi64 (__m512i a)
            ///   VPLZCNTQ zmm1 {k1}{z}, zmm2/m512/m64bcst
            /// </summary>
            public static Vector512<ulong> LeadingZeroCount(Vector512<ulong> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512 _mm512_and_ps (__m512 a, __m512 b)
            ///   VANDPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
            /// </summary>
            public static Vector512<float> And(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512d _mm512_and_pd (__m512d a, __m512d b)
            ///   VANDPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
            /// </summary>
            public static Vector512<double> And(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512 _mm512_andnot_ps (__m512 a, __m512 b)
            ///   VANDNPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
            /// </summary>
            public static Vector512<float> AndNot(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512d _mm512_andnot_pd (__m512d a, __m512d b)
            ///   VANDNPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
            /// </summary>
            public static Vector512<double> AndNot(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_broadcast_i32x2 (__m128i a)
            ///   VBROADCASTI32x2 zmm1 {k1}{z}, xmm2/m64
            /// </summary>
            public static Vector512<int> BroadcastPairScalarToVector512(Vector128<int> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_broadcast_i32x2 (__m128i a)
            ///   VBROADCASTI32x2 zmm1 {k1}{z}, xmm2/m64
            /// </summary>
            public static Vector512<uint> BroadcastPairScalarToVector512(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512 _mm512_broadcast_f32x2 (__m128 a)
            ///   VBROADCASTF32x2 zmm1 {k1}{z}, xmm2/m64
            /// </summary>
            public static Vector512<float> BroadcastPairScalarToVector512(Vector128<float> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_broadcast_i64x2 (__m128i const * mem_addr)
            ///   VBROADCASTI64x2 zmm1 {k1}{z}, m128
            /// </summary>
            public static unsafe Vector512<long> BroadcastVector128ToVector512(long* address) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_broadcast_i64x2 (__m128i const * mem_addr)
            ///   VBROADCASTI64x2 zmm1 {k1}{z}, m128
            /// </summary>
            public static unsafe Vector512<ulong> BroadcastVector128ToVector512(ulong* address) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512d _mm512_broadcast_f64x2 (__m128d const * mem_addr)
            ///   VBROADCASTF64x2 zmm1 {k1}{z}, m128
            /// </summary>
            public static unsafe Vector512<double> BroadcastVector128ToVector512(double* address) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_broadcast_i32x8 (__m256i const * mem_addr)
            ///   VBROADCASTI32x8 zmm1 {k1}{z}, m256
            /// </summary>
            public static unsafe Vector512<int> BroadcastVector256ToVector512(int* address) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_broadcast_i32x8 (__m256i const * mem_addr)
            ///   VBROADCASTI32x8 zmm1 {k1}{z}, m256
            /// </summary>
            public static unsafe Vector512<uint> BroadcastVector256ToVector512(uint* address) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512 _mm512_broadcast_f32x8 (__m256 const * mem_addr)
            ///   VBROADCASTF32x8 zmm1 {k1}{z}, m256
            /// </summary>
            public static unsafe Vector512<float> BroadcastVector256ToVector512(float* address) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512 _mm512_cvtepi64_ps (__m512i a)
            ///   VCVTQQ2PS ymm1 {k1}{z}, zmm2/m512/m64bcst
            /// </summary>
            public static Vector256<float> ConvertToVector256Single(Vector512<long> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512 _mm512_cvtepu64_ps (__m512i a)
            ///   VCVTUQQ2PS ymm1 {k1}{z}, zmm2/m512/m64bcst
            /// </summary>
            public static Vector256<float> ConvertToVector256Single(Vector512<ulong> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m256 _mm512_cvt_roundepi64_ps (__m512i a, int r)
            ///   VCVTQQ2PS ymm1, zmm2 {er}
            /// </summary>
            public static Vector256<float> ConvertToVector256Single(Vector512<long> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m256 _mm512_cvt_roundepu64_ps (__m512i a, int r)
            ///   VCVTUQQ2PS ymm1, zmm2 {er}
            /// </summary>
            public static Vector256<float> ConvertToVector256Single(Vector512<ulong> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512d _mm512_cvtepi64_pd (__m512i a)
            ///   VCVTQQ2PD zmm1 {k1}{z}, zmm2/m512/m64bcst
            /// </summary>
            public static Vector512<double> ConvertToVector512Double(Vector512<long> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512d _mm512_cvtepu64_pd (__m512i a)
            ///   VCVTUQQ2PD zmm1 {k1}{z}, zmm2/m512/m64bcst
            /// </summary>
            public static Vector512<double> ConvertToVector512Double(Vector512<ulong> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512d _mm512_cvt_roundepi64_pd (__m512i a, int r)
            ///   VCVTQQ2PD zmm1, zmm2 {er}
            /// </summary>
            public static Vector512<double> ConvertToVector512Double(Vector512<long> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512d _mm512_cvt_roundepu64_pd (__m512i a, int r)
            ///   VCVTUQQ2PD zmm1, zmm2 {er}
            /// </summary>
            public static Vector512<double> ConvertToVector512Double(Vector512<ulong> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_cvtps_epi64 (__m512 a)
            ///   VCVTPS2QQ zmm1 {k1}{z}, ymm2/m256/m32bcst{er}
            /// </summary>
            public static Vector512<long> ConvertToVector512Int64(Vector256<float> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_cvtpd_epi64 (__m512d a)
            ///   VCVTPD2QQ zmm1 {k1}{z}, zmm2/m512/m64bcst{er}
            /// </summary>
            public static Vector512<long> ConvertToVector512Int64(Vector512<double> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_cvt_roundps_epi64 (__m512 a, int r)
            ///   VCVTPS2QQ zmm1, ymm2 {er}
            /// </summary>
            public static Vector512<long> ConvertToVector512Int64(Vector256<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_cvt_roundpd_epi64 (__m512d a, int r)
            ///   VCVTPD2QQ zmm1, zmm2 {er}
            /// </summary>
            public static Vector512<long> ConvertToVector512Int64(Vector512<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_cvttps_epi64 (__m512 a)
            ///   VCVTTPS2QQ zmm1 {k1}{z}, ymm2/m256/m32bcst{er}
            /// </summary>
            public static Vector512<long> ConvertToVector512Int64WithTruncation(Vector256<float> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_cvttpd_epi64 (__m512 a)
            ///   VCVTTPD2QQ zmm1 {k1}{z}, zmm2/m512/m64bcst{sae}
            /// </summary>
            public static Vector512<long> ConvertToVector512Int64WithTruncation(Vector512<double> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_cvtps_epu64 (__m512 a)
            ///   VCVTPS2UQQ zmm1 {k1}{z}, ymm2/m256/m32bcst{er}
            /// </summary>
            public static Vector512<ulong> ConvertToVector512UInt64(Vector256<float> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_cvtpd_epu64 (__m512d a)
            ///   VCVTPD2UQQ zmm1 {k1}{z}, zmm2/m512/m64bcst{er}
            /// </summary>
            public static Vector512<ulong> ConvertToVector512UInt64(Vector512<double> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_cvt_roundps_epu64 (__m512 a, int r)
            ///   VCVTPS2UQQ zmm1 {k1}{z}, ymm2/m256/m32bcst{er}
            /// </summary>
            public static Vector512<ulong> ConvertToVector512UInt64(Vector256<float> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_cvt_roundpd_epu64 (__m512d a, int r)
            ///   VCVTPD2UQQ zmm1 {k1}{z}, zmm2/m512/m64bcst{er}
            /// </summary>
            public static Vector512<ulong> ConvertToVector512UInt64(Vector512<double> value, [ConstantExpected(Max = FloatRoundingMode.ToZero)] FloatRoundingMode mode) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_cvttps_epu64 (__m512 a)
            ///   VCVTTPS2UQQ zmm1 {k1}{z}, ymm2/m256/m32bcst{er}
            /// </summary>
            public static Vector512<ulong> ConvertToVector512UInt64WithTruncation(Vector256<float> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_cvttpd_epu64 (__m512d a)
            ///   VCVTTPD2UQQ zmm1 {k1}{z}, zmm2/m512/m64bcst{er}
            /// </summary>
            public static Vector512<ulong> ConvertToVector512UInt64WithTruncation(Vector512<double> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128i _mm512_extracti64x2_epi64 (__m512i a, const int imm8)
            ///   VEXTRACTI64x2 xmm1/m128 {k1}{z}, zmm2, imm8
            /// </summary>
            public static new Vector128<long> ExtractVector128(Vector512<long> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128i _mm512_extracti64x2_epi64 (__m512i a, const int imm8)
            ///   VEXTRACTI64x2 xmm1/m128 {k1}{z}, zmm2, imm8
            /// </summary>
            public static new Vector128<ulong> ExtractVector128(Vector512<ulong> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128d _mm512_extractf64x2_pd (__m512d a, const int imm8)
            ///   VEXTRACTF64x2 xmm1/m128 {k1}{z}, zmm2, imm8
            /// </summary>
            public static new Vector128<double> ExtractVector128(Vector512<double> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m256i _mm512_extracti32x8_epi32 (__m512i a, const int imm8)
            ///   VEXTRACTI32x8 ymm1/m256 {k1}{z}, zmm2, imm8
            /// </summary>
            public static new Vector256<int> ExtractVector256(Vector512<int> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m256i _mm512_extracti32x8_epi32 (__m512i a, const int imm8)
            ///   VEXTRACTI32x8 ymm1/m256 {k1}{z}, zmm2, imm8
            /// </summary>
            public static new Vector256<uint> ExtractVector256(Vector512<uint> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m256 _mm512_extractf32x8_ps (__m512 a, const int imm8)
            ///   VEXTRACTF32x8 ymm1/m256 {k1}{z}, zmm2, imm8
            /// </summary>
            public static new Vector256<float> ExtractVector256(Vector512<float> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_inserti64x2_si512 (__m512i a, __m128i b, const int imm8)
            ///   VINSERTI64x2 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
            /// </summary>
            public static new Vector512<long> InsertVector128(Vector512<long> value, Vector128<long> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_inserti64x2_si512 (__m512i a, __m128i b, const int imm8)
            ///   VINSERTI64x2 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
            /// </summary>
            public static new Vector512<ulong> InsertVector128(Vector512<ulong> value, Vector128<ulong> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512d _mm512_insertf64x2_pd (__m512d a, __m128d b, int imm8)
            ///   VINSERTF64x2 zmm1 {k1}{z}, zmm2, xmm3/m128, imm8
            /// </summary>
            public static new Vector512<double> InsertVector128(Vector512<double> value, Vector128<double> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_inserti32x8_si512 (__m512i a, __m256i b, const int imm8)
            ///   VINSERTI32x8 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
            /// </summary>
            public static new Vector512<int> InsertVector256(Vector512<int> value, Vector256<int> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_inserti32x8_si512 (__m512i a, __m256i b, const int imm8)
            ///   VINSERTI32x8 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
            /// </summary>
            public static new Vector512<uint> InsertVector256(Vector512<uint> value, Vector256<uint> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512 _mm512_insertf32x8_ps (__m512 a, __m256 b, int imm8)
            ///   VINSERTF32x8 zmm1 {k1}{z}, zmm2, xmm3/m256, imm8
            /// </summary>
            public static new Vector512<float> InsertVector256(Vector512<float> value, Vector256<float> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_mullo_epi64 (__m512i a, __m512i b)
            ///   VPMULLQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
            /// </summary>
            public static Vector512<long> MultiplyLow(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_mullo_epi64 (__m512i a, __m512i b)
            ///   VPMULLQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
            /// </summary>
            public static Vector512<ulong> MultiplyLow(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_multishift_epi64_epi8( __m512i a, __m512i b)
            ///   VPMULTISHIFTQB zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
            /// </summary>
            public static Vector512<byte> MultiShift(Vector512<byte> control, Vector512<ulong> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m512i _mm512_multishift_epi64_epi8( __m512i a, __m512i b)
            ///   VPMULTISHIFTQB zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
            /// </summary>
            public static Vector512<sbyte> MultiShift(Vector512<sbyte> control, Vector512<long> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512 _mm512_or_ps (__m512 a, __m512 b)
            ///   VORPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
            /// </summary>
            public static Vector512<float> Or(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512d _mm512_or_pd (__m512d a, __m512d b)
            ///   VORPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
            /// </summary>
            public static Vector512<double> Or(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512 _mm512_range_ps(__m512 a, __m512 b, int imm);
            ///   VRANGEPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{sae}, imm8
            /// </summary>
            public static Vector512<float> Range(Vector512<float> left, Vector512<float> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512d _mm512_range_pd(__m512d a, __m512d b, int imm);
            ///   VRANGEPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{sae}, imm8
            /// </summary>
            public static Vector512<double> Range(Vector512<double> left, Vector512<double> right, [ConstantExpected(Max = (byte)(0x0F))] byte control) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512 _mm512_reduce_ps(__m512 a, int imm);
            ///   VREDUCEPS zmm1 {k1}{z}, zmm2/m512/m32bcst{sae}, imm8
            /// </summary>
            public static Vector512<float> Reduce(Vector512<float> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512d _mm512_reduce_pd(__m512d a, int imm);
            ///   VREDUCEPD zmm1 {k1}{z}, zmm2/m512/m64bcst{sae}, imm8
            /// </summary>
            public static Vector512<double> Reduce(Vector512<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512 _mm512_xor_ps (__m512 a, __m512 b)
            ///   VXORPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
            /// </summary>
            public static Vector512<float> Xor(Vector512<float> left, Vector512<float> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512d _mm512_xor_pd (__m512d a, __m512d b)
            ///   VXORPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
            /// </summary>
            public static Vector512<double> Xor(Vector512<double> left, Vector512<double> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_permutevar64x8_epi8 (__m512i a, __m512i b)
            ///   VPERMB zmm1 {k1}{z}, zmm2, zmm3/m512
            /// </summary>
            public static Vector512<sbyte> PermuteVar64x8(Vector512<sbyte> left, Vector512<sbyte> control) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_permutevar64x8_epi8 (__m512i a, __m512i b)
            ///   VPERMB zmm1 {k1}{z}, zmm2, zmm3/m512
            /// </summary>
            public static Vector512<byte> PermuteVar64x8(Vector512<byte> left, Vector512<byte> control) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_permutex2var_epi8 (__m512i a, __m512i idx, __m512i b)
            ///   VPERMI2B zmm1 {k1}{z}, zmm2, zmm3/m512
            ///   VPERMT2B zmm1 {k1}{z}, zmm2, zmm3/m512
            /// </summary>
            public static Vector512<byte> PermuteVar64x8x2(Vector512<byte> lower, Vector512<byte> indices, Vector512<byte> upper) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m512i _mm512_permutex2var_epi8 (__m512i a, __m512i idx, __m512i b)
            ///   VPERMI2B zmm1 {k1}{z}, zmm2, zmm3/m512
            ///   VPERMT2B zmm1 {k1}{z}, zmm2, zmm3/m512
            /// </summary>
            public static Vector512<sbyte> PermuteVar64x8x2(Vector512<sbyte> lower, Vector512<sbyte> indices, Vector512<sbyte> upper) { throw new PlatformNotSupportedException(); }

            [Intrinsic]
            public new abstract class X64 : Avx512BW.X64
            {
                internal X64() { }

                public static new bool IsSupported { [Intrinsic] get { return false; } }
            }
        }
    }
}
