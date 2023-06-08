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
            /// __m128i _mm_alignr_epi32 (__m128i a, __m128i b, const int count)
            ///   VALIGND xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
            /// </summary>
            public static Vector128<int> AlignRight32(Vector128<int> left, Vector128<int> right, [ConstantExpected] byte mask) => AlignRight32(left, right, mask);
            /// <summary>
            /// __m128i _mm_alignr_epi32 (__m128i a, __m128i b, const int count)
            ///   VALIGND xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
            /// </summary>
            public static Vector128<uint> AlignRight32(Vector128<uint> left, Vector128<uint> right, [ConstantExpected] byte mask) => AlignRight32(left, right, mask);
            /// <summary>
            /// __m256i _mm256_alignr_epi32 (__m256i a, __m256i b, const int count)
            ///   VALIGND ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
            /// </summary>
            public static Vector256<int> AlignRight32(Vector256<int> left, Vector256<int> right, [ConstantExpected] byte mask) => AlignRight32(left, right, mask);
            /// <summary>
            /// __m256i _mm256_alignr_epi32 (__m256i a, __m256i b, const int count)
            ///   VALIGND ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
            /// </summary>
            public static Vector256<uint> AlignRight32(Vector256<uint> left, Vector256<uint> right, [ConstantExpected] byte mask) => AlignRight32(left, right, mask);

            /// <summary>
            /// __m128i _mm_alignr_epi64 (__m128i a, __m128i b, const int count)
            ///   VALIGNQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8
            /// </summary>
            public static Vector128<long> AlignRight64(Vector128<long> left, Vector128<long> right, [ConstantExpected] byte mask) => AlignRight64(left, right, mask);
            /// <summary>
            /// __m128i _mm_alignr_epi64 (__m128i a, __m128i b, const int count)
            ///   VALIGNQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8
            /// </summary>
            public static Vector128<ulong> AlignRight64(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected] byte mask) => AlignRight64(left, right, mask);
            /// <summary>
            /// __m256i _mm256_alignr_epi64 (__m256i a, __m256i b, const int count)
            ///   VALIGNQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
            /// </summary>
            public static Vector256<long> AlignRight64(Vector256<long> left, Vector256<long> right, [ConstantExpected] byte mask) => AlignRight64(left, right, mask);
            /// <summary>
            /// __m256i _mm256_alignr_epi64 (__m256i a, __m256i b, const int count)
            ///   VALIGNQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
            /// </summary>
            public static Vector256<ulong> AlignRight64(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected] byte mask) => AlignRight64(left, right, mask);

            /// <summary>
            /// __m128i _mm_cvtepi32_epi8 (__m128i a)
            ///   VPMOVDB xmm1/m32 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector128<int> value) => ConvertToVector128Byte(value);
            /// <summary>
            /// __m128i _mm_cvtepi64_epi8 (__m128i a)
            ///   VPMOVQB xmm1/m16 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector128<long> value) => ConvertToVector128Byte(value);
            /// <summary>
            /// __m128i _mm_cvtepi32_epi8 (__m128i a)
            ///   VPMOVDB xmm1/m32 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector128<uint> value) => ConvertToVector128Byte(value);
            /// <summary>
            /// __m128i _mm_cvtepi64_epi8 (__m128i a)
            ///   VPMOVQB xmm1/m16 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector128<ulong> value) => ConvertToVector128Byte(value);
            /// <summary>
            /// __m128i _mm256_cvtepi32_epi8 (__m256i a)
            ///   VPMOVDB xmm1/m64 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector256<int> value) => ConvertToVector128Byte(value);
            /// <summary>
            /// __m128i _mm256_cvtepi64_epi8 (__m256i a)
            ///   VPMOVQB xmm1/m32 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector256<long> value) => ConvertToVector128Byte(value);
            /// <summary>
            /// __m128i _mm256_cvtepi32_epi8 (__m256i a)
            ///   VPMOVDB xmm1/m64 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector256<uint> value) => ConvertToVector128Byte(value);
            /// <summary>
            /// __m128i _mm256_cvtepi64_epi8 (__m256i a)
            ///   VPMOVQB xmm1/m32 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector256<ulong> value) => ConvertToVector128Byte(value);
            /// <summary>
            /// __m128i _mm_cvtusepi32_epi8 (__m128i a)
            ///   VPMOVUSDB xmm1/m32 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector128<uint> value) => ConvertToVector128ByteWithSaturation(value);
            /// <summary>
            /// __m128i _mm_cvtusepi64_epi8 (__m128i a)
            ///   VPMOVUSQB xmm1/m16 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector128<ulong> value) => ConvertToVector128ByteWithSaturation(value);
            /// <summary>
            /// __m128i _mm256_cvtusepi32_epi8 (__m256i a)
            ///   VPMOVUSDB xmm1/m64 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector256<uint> value) => ConvertToVector128ByteWithSaturation(value);
            /// <summary>
            /// __m128i _mm256_cvtusepi64_epi8 (__m256i a)
            ///   VPMOVUSQB xmm1/m32 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector256<ulong> value) => ConvertToVector128ByteWithSaturation(value);

            /// <summary>
            /// __m128d _mm_cvtepu32_pd (__m128i a)
            ///   VCVTUDQ2PD xmm1 {k1}{z}, xmm2/m64/m32bcst
            /// </summary>
            public static Vector128<double> ConvertToVector128Double(Vector128<uint> value) => ConvertToVector128Double(value);

            /// <summary>
            /// __m128i _mm_cvtepi32_epi16 (__m128i a)
            ///   VPMOVDW xmm1/m64 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector128<int> value) => ConvertToVector128Int16(value);
            /// <summary>
            /// __m128i _mm_cvtepi64_epi16 (__m128i a)
            ///   VPMOVQW xmm1/m32 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector128<long> value) => ConvertToVector128Int16(value);
            /// <summary>
            /// __m128i _mm_cvtepi32_epi16 (__m128i a)
            ///   VPMOVDW xmm1/m64 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector128<uint> value) => ConvertToVector128Int16(value);
            /// <summary>
            /// __m128i _mm_cvtepi64_epi16 (__m128i a)
            ///   VPMOVQW xmm1/m32 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector128<ulong> value) => ConvertToVector128Int16(value);
            /// <summary>
            /// __m128i _mm256_cvtepi32_epi16 (__m256i a)
            ///   VPMOVDW xmm1/m128 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector256<int> value) => ConvertToVector128Int16(value);
            /// <summary>
            /// __m128i _mm256_cvtepi64_epi16 (__m256i a)
            ///   VPMOVQW xmm1/m64 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector256<long> value) => ConvertToVector128Int16(value);
            /// <summary>
            /// __m128i _mm256_cvtepi32_epi16 (__m256i a)
            ///   VPMOVDW xmm1/m128 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector256<uint> value) => ConvertToVector128Int16(value);
            /// <summary>
            /// __m128i _mm256_cvtepi64_epi16 (__m256i a)
            ///   VPMOVQW xmm1/m64 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16(Vector256<ulong> value) => ConvertToVector128Int16(value);
            /// <summary>
            /// __m128i _mm_cvtsepi32_epi16 (__m128i a)
            ///   VPMOVSDW xmm1/m64 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector128<int> value) => ConvertToVector128Int16WithSaturation(value);
            /// <summary>
            /// __m128i _mm_cvtsepi64_epi16 (__m128i a)
            ///   VPMOVSQW xmm1/m32 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector128<long> value) => ConvertToVector128Int16WithSaturation(value);
            /// <summary>
            /// __m128i _mm256_cvtsepi32_epi16 (__m256i a)
            ///   VPMOVSDW xmm1/m128 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector256<int> value) => ConvertToVector128Int16WithSaturation(value);
            /// <summary>
            /// __m128i _mm256_cvtsepi64_epi16 (__m256i a)
            ///   VPMOVSQW xmm1/m64 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector256<long> value) => ConvertToVector128Int16WithSaturation(value);

            /// <summary>
            /// __m128i _mm_cvtepi64_epi32 (__m128i a)
            ///   VPMOVQD xmm1/m64 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<int> ConvertToVector128Int32(Vector128<long> value) => ConvertToVector128Int32(value);
            /// <summary>
            /// __m128i _mm_cvtepi64_epi32 (__m128i a)
            ///   VPMOVQD xmm1/m64 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<int> ConvertToVector128Int32(Vector128<ulong> value) => ConvertToVector128Int32(value);
            /// <summary>
            /// __m128i _mm256_cvtepi64_epi32 (__m256i a)
            ///   VPMOVQD xmm1/m128 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<int> ConvertToVector128Int32(Vector256<long> value) => ConvertToVector128Int32(value);
            /// <summary>
            /// __m128i _mm256_cvtepi64_epi32 (__m256i a)
            ///   VPMOVQD xmm1/m128 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<int> ConvertToVector128Int32(Vector256<ulong> value) => ConvertToVector128Int32(value);
            /// <summary>
            /// __m128i _mm128_cvtsepi64_epi32 (__m128i a)
            ///   VPMOVSQD xmm1/m64 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<int> ConvertToVector128Int32WithSaturation(Vector128<long> value) => ConvertToVector128Int32WithSaturation(value);
            /// <summary>
            /// __m128i _mm256_cvtsepi64_epi32 (__m256i a)
            ///   VPMOVSQD xmm1/m128 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<int> ConvertToVector128Int32WithSaturation(Vector256<long> value) => ConvertToVector128Int32WithSaturation(value);

            /// <summary>
            /// __m128i _mm_cvtepi32_epi8 (__m128i a)
            ///   VPMOVDB xmm1/m32 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector128<int> value) => ConvertToVector128SByte(value);
            /// <summary>
            /// __m128i _mm_cvtepi64_epi8 (__m128i a)
            ///   VPMOVQB xmm1/m16 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector128<long> value) => ConvertToVector128SByte(value);
            /// <summary>
            /// __m128i _mm_cvtepi32_epi8 (__m128i a)
            ///   VPMOVDB xmm1/m32 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector128<uint> value) => ConvertToVector128SByte(value);
            /// <summary>
            /// __m128i _mm_cvtepi64_epi8 (__m128i a)
            ///   VPMOVQB xmm1/m16 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector128<ulong> value) => ConvertToVector128SByte(value);
            /// <summary>
            /// __m128i _mm256_cvtepi32_epi8 (__m256i a)
            ///   VPMOVDB xmm1/m64 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector256<int> value) => ConvertToVector128SByte(value);
            /// <summary>
            /// __m128i _mm256_cvtepi64_epi8 (__m256i a)
            ///   VPMOVQB xmm1/m32 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector256<long> value) => ConvertToVector128SByte(value);
            /// <summary>
            /// __m128i _mm256_cvtepi32_epi8 (__m256i a)
            ///   VPMOVDB xmm1/m64 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector256<uint> value) => ConvertToVector128SByte(value);
            /// <summary>
            /// __m128i _mm256_cvtepi64_epi8 (__m256i a)
            ///   VPMOVQB xmm1/m32 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector256<ulong> value) => ConvertToVector128SByte(value);
            /// <summary>
            /// __m128i _mm_cvtsepi32_epi8 (__m128i a)
            ///   VPMOVSDB xmm1/m32 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector128<int> value) => ConvertToVector128SByteWithSaturation(value);
            /// <summary>
            /// __m128i _mm_cvtsepi64_epi8 (__m128i a)
            ///   VPMOVSQB xmm1/m16 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector128<long> value) => ConvertToVector128SByteWithSaturation(value);
            /// <summary>
            /// __m128i _mm256_cvtsepi32_epi8 (__m256i a)
            ///   VPMOVSDB xmm1/m64 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector256<int> value) => ConvertToVector128SByteWithSaturation(value);
            /// <summary>
            /// __m128i _mm256_cvtsepi64_epi8 (__m256i a)
            ///   VPMOVSQB xmm1/m32 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector256<long> value) => ConvertToVector128SByteWithSaturation(value);

            /// <summary>
            /// __m128 _mm_cvtepu32_ps (__m128i a)
            ///   VCVTUDQ2PS xmm1 {k1}{z}, xmm2/m128/m32bcst
            /// </summary>
            public static Vector128<float> ConvertToVector128Single(Vector128<uint> value) => ConvertToVector128Single(value);

            /// <summary>
            /// __m128i _mm_cvtepi32_epi16 (__m128i a)
            ///   VPMOVDW xmm1/m64 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector128<int> value) => ConvertToVector128UInt16(value);
            /// <summary>
            /// __m128i _mm_cvtepi64_epi16 (__m128i a)
            ///   VPMOVQW xmm1/m32 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector128<long> value) => ConvertToVector128UInt16(value);
            /// <summary>
            /// __m128i _mm_cvtepi32_epi16 (__m128i a)
            ///   VPMOVDW xmm1/m64 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector128<uint> value) => ConvertToVector128UInt16(value);
            /// <summary>
            /// __m128i _mm_cvtepi64_epi16 (__m128i a)
            ///   VPMOVQW xmm1/m32 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector128<ulong> value) => ConvertToVector128UInt16(value);
            /// <summary>
            /// __m128i _mm256_cvtepi32_epi16 (__m256i a)
            ///   VPMOVDW xmm1/m128 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector256<int> value) => ConvertToVector128UInt16(value);
            /// <summary>
            /// __m128i _mm256_cvtepi64_epi16 (__m256i a)
            ///   VPMOVQW xmm1/m64 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector256<long> value) => ConvertToVector128UInt16(value);
            /// <summary>
            /// __m128i _mm256_cvtepi32_epi16 (__m256i a)
            ///   VPMOVDW xmm1/m128 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector256<uint> value) => ConvertToVector128UInt16(value);
            /// <summary>
            /// __m128i _mm256_cvtepi64_epi16 (__m256i a)
            ///   VPMOVQW xmm1/m64 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16(Vector256<ulong> value) => ConvertToVector128UInt16(value);
            /// <summary>
            /// __m128i _mm_cvtusepi32_epi16 (__m128i a)
            ///   VPMOVUSDW xmm1/m64 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector128<uint> value) => ConvertToVector128UInt16WithSaturation(value);
            /// <summary>
            /// __m128i _mm_cvtusepi64_epi16 (__m128i a)
            ///   VPMOVUSQW xmm1/m32 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector128<ulong> value) => ConvertToVector128UInt16WithSaturation(value);
            /// <summary>
            /// __m128i _mm256_cvtusepi32_epi16 (__m256i a)
            ///   VPMOVUSDW xmm1/m128 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector256<uint> value) => ConvertToVector128UInt16WithSaturation(value);
            /// <summary>
            /// __m128i _mm256_cvtusepi64_epi16 (__m256i a)
            ///   VPMOVUSQW xmm1/m64 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector256<ulong> value) => ConvertToVector128UInt16WithSaturation(value);

            /// <summary>
            /// __m128i _mm128_cvtepi64_epi32 (__m128i a)
            ///   VPMOVQD xmm1/m128 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32(Vector128<long> value) => ConvertToVector128UInt32(value);
            /// <summary>
            /// __m128i _mm128_cvtepi64_epi32 (__m128i a)
            ///   VPMOVQD xmm1/m128 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32(Vector128<ulong> value) => ConvertToVector128UInt32(value);
            /// <summary>
            /// __m128i _mm_cvtps_epu32 (__m128 a)
            ///   VCVTPS2UDQ xmm1 {k1}{z}, xmm2/m128/m32bcst
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32(Vector128<float> value) => ConvertToVector128UInt32(value);
            /// <summary>
            /// __m128i _mm_cvtpd_epu32 (__m128d a)
            ///   VCVTPD2UDQ xmm1 {k1}{z}, xmm2/m128/m64bcst
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32(Vector128<double> value) => ConvertToVector128UInt32(value);
            /// <summary>
            /// __m128i _mm256_cvtepi64_epi32 (__m256i a)
            ///   VPMOVQD xmm1/m128 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32(Vector256<long> value) => ConvertToVector128UInt32(value);
            /// <summary>
            /// __m128i _mm256_cvtepi64_epi32 (__m256i a)
            ///   VPMOVQD xmm1/m128 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32(Vector256<ulong> value) => ConvertToVector128UInt32(value);
            /// <summary>
            /// __m128i _mm256_cvtpd_epu32 (__m256d a)
            ///   VCVTPD2UDQ xmm1 {k1}{z}, ymm2/m256/m64bcst
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32(Vector256<double> value) => ConvertToVector128UInt32(value);
            /// <summary>
            /// __m128i _mm128_cvtusepi64_epi32 (__m128i a)
            ///   VPMOVUSQD xmm1/m128 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32WithSaturation(Vector128<ulong> value) => ConvertToVector128UInt32WithSaturation(value);
            /// <summary>
            /// __m128i _mm256_cvtusepi64_epi32 (__m256i a)
            ///   VPMOVUSQD xmm1/m128 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32WithSaturation(Vector256<ulong> value) => ConvertToVector128UInt32WithSaturation(value);
            /// <summary>
            /// __m128i _mm_cvttps_epu32 (__m128 a)
            ///   VCVTTPS2UDQ xmm1 {k1}{z}, xmm2/m128/m32bcst
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32WithTruncation(Vector128<float> value) => ConvertToVector128UInt32WithTruncation(value);
            /// <summary>
            /// __m128i _mm_cvttpd_epu32 (__m128d a)
            ///   VCVTTPD2UDQ xmm1 {k1}{z}, xmm2/m128/m64bcst
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32WithTruncation(Vector128<double> value) => ConvertToVector128UInt32WithTruncation(value);
            /// <summary>
            /// __m128i _mm256_cvttpd_epu32 (__m256d a)
            ///   VCVTTPD2UDQ xmm1 {k1}{z}, ymm2/m256/m64bcst
            /// </summary>
            public static Vector128<uint> ConvertToVector128UInt32WithTruncation(Vector256<double> value) => ConvertToVector128UInt32WithTruncation(value);

            /// <summary>
            /// __m256d _mm512_cvtepu32_pd (__m128i a)
            ///   VCVTUDQ2PD ymm1 {k1}{z}, xmm2/m128/m32bcst
            /// </summary>
            public static Vector256<double> ConvertToVector256Double(Vector128<uint> value) => ConvertToVector256Double(value);
            /// <summary>
            /// __m256 _mm256_cvtepu32_ps (__m256i a)
            ///   VCVTUDQ2PS ymm1 {k1}{z}, ymm2/m256/m32bcst
            /// </summary>
            public static Vector256<float> ConvertToVector256Single(Vector256<uint> value) => ConvertToVector256Single(value);
            /// <summary>
            /// __m256i _mm256_cvtps_epu32 (__m256 a)
            ///   VCVTPS2UDQ ymm1 {k1}{z}, ymm2/m256/m32bcst
            /// </summary>
            public static Vector256<uint> ConvertToVector256UInt32(Vector256<float> value) => ConvertToVector256UInt32(value);
            /// <summary>
            /// __m256i _mm256_cvttps_epu32 (__m256 a)
            ///   VCVTTPS2UDQ ymm1 {k1}{z}, ymm2/m256/m32bcst
            /// </summary>
            public static Vector256<uint> ConvertToVector256UInt32WithTruncation(Vector256<float> value) => ConvertToVector256UInt32WithTruncation(value);

            /// <summary>
            /// __m128 _mm_fixupimm_ps(__m128 a, __m128 b, __m128i tbl, int imm);
            ///   VFIXUPIMMPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
            /// </summary>
            public static Vector128<float> Fixup(Vector128<float> left, Vector128<float> right, Vector128<int> table, [ConstantExpected] byte control) => Fixup(left, right, table, control);
            /// <summary>
            /// __m128d _mm_fixupimm_pd(__m128d a, __m128d b, __m128i tbl, int imm);
            ///   VFIXUPIMMPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8
            /// </summary>
            public static Vector128<double> Fixup(Vector128<double> left, Vector128<double> right, Vector128<long> table, [ConstantExpected] byte control) => Fixup(left, right, table, control);
            /// <summary>
            /// __m256 _mm256_fixupimm_ps(__m256 a, __m256 b, __m256i tbl, int imm);
            ///   VFIXUPIMMPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
            /// </summary>
            public static Vector256<float> Fixup(Vector256<float> left, Vector256<float> right, Vector256<int> table, [ConstantExpected] byte control) => Fixup(left, right, table, control);
            /// <summary>
            /// __m256d _mm256_fixupimm_pd(__m256d a, __m256d b, __m256i tbl, int imm);
            ///   VFIXUPIMMPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
            /// </summary>
            public static Vector256<double> Fixup(Vector256<double> left, Vector256<double> right, Vector256<long> table, [ConstantExpected] byte control) => Fixup(left, right, table, control);

            /// <summary>
            /// __m128 _mm_getexp_ps (__m128 a)
            ///   VGETEXPPS xmm1 {k1}{z}, xmm2/m128/m32bcst
            /// </summary>
            public static Vector128<float> GetExponent(Vector128<float> value) => GetExponent(value);
            /// <summary>
            /// __m128d _mm_getexp_pd (__m128d a)
            ///   VGETEXPPD xmm1 {k1}{z}, xmm2/m128/m64bcst
            /// </summary>
            public static Vector128<double> GetExponent(Vector128<double> value) => GetExponent(value);
            /// <summary>
            /// __m256 _mm256_getexp_ps (__m256 a)
            ///   VGETEXPPS ymm1 {k1}{z}, ymm2/m256/m32bcst
            /// </summary>
            public static Vector256<float> GetExponent(Vector256<float> value) => GetExponent(value);
            /// <summary>
            /// __m256d _mm256_getexp_pd (__m256d a)
            ///   VGETEXPPD ymm1 {k1}{z}, ymm2/m256/m64bcst
            /// </summary>
            public static Vector256<double> GetExponent(Vector256<double> value) => GetExponent(value);

            /// <summary>
            /// __m128 _mm_getmant_ps (__m128 a)
            ///   VGETMANTPS xmm1 {k1}{z}, xmm2/m128/m32bcst
            /// </summary>
            public static Vector128<float> GetMantissa(Vector128<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissa(value, control);
            /// <summary>
            /// __m128d _mm_getmant_pd (__m128d a)
            ///   VGETMANTPD xmm1 {k1}{z}, xmm2/m128/m64bcst
            /// </summary>
            public static Vector128<double> GetMantissa(Vector128<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissa(value, control);
            /// <summary>
            /// __m256 _mm256_getmant_ps (__m256 a)
            ///   VGETMANTPS ymm1 {k1}{z}, ymm2/m256/m32bcst
            /// </summary>
            public static Vector256<float> GetMantissa(Vector256<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissa(value, control);
            /// <summary>
            /// __m256d _mm256_getmant_pd (__m256d a)
            ///   VGETMANTPD ymm1 {k1}{z}, ymm2/m256/m64bcst
            /// </summary>
            public static Vector256<double> GetMantissa(Vector256<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissa(value, control);

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
            /// __m128i _mm_permutex2var_epi64 (__m128i a, __m128i idx, __m128i b)
            ///   VPERMI2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            ///   VPERMT2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<long> PermuteVar2x64x2(Vector128<long> lower, Vector128<long> indices, Vector128<long> upper) => PermuteVar2x64x2(lower, indices, upper);
            /// <summary>
            /// __m128i _mm_permutex2var_epi64 (__m128i a, __m128i idx, __m128i b)
            ///   VPERMI2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            ///   VPERMT2Q xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<ulong> PermuteVar2x64x2(Vector128<ulong> lower, Vector128<ulong> indices, Vector128<ulong> upper) => PermuteVar2x64x2(lower, indices, upper);
            /// <summary>
            /// __m128d _mm_permutex2var_pd (__m128d a, __m128i idx, __m128i b)
            ///   VPERMI2PD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            ///   VPERMT2PD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<double> PermuteVar2x64x2(Vector128<double> lower, Vector128<long> indices, Vector128<double> upper) => PermuteVar2x64x2(lower, indices, upper);

            /// <summary>
            /// __m128i _mm_permutex2var_epi32 (__m128i a, __m128i idx, __m128i b)
            ///   VPERMI2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
            ///   VPERMT2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
            /// </summary>
            public static Vector128<int> PermuteVar4x32x2(Vector128<int> lower, Vector128<int> indices, Vector128<int> upper) => PermuteVar4x32x2(lower, indices, upper);
            /// <summary>
            /// __m128i _mm_permutex2var_epi32 (__m128i a, __m128i idx, __m128i b)
            ///   VPERMI2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
            ///   VPERMT2D xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
            /// </summary>
            public static Vector128<uint> PermuteVar4x32x2(Vector128<uint> lower, Vector128<uint> indices, Vector128<uint> upper) => PermuteVar4x32x2(lower, indices, upper);
            /// <summary>
            /// __m128 _mm_permutex2var_ps (__m128 a, __m128i idx, __m128i b)
            ///   VPERMI2PS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
            ///   VPERMT2PS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
            /// </summary>
            public static Vector128<float> PermuteVar4x32x2(Vector128<float> lower, Vector128<int> indices, Vector128<float> upper) => PermuteVar4x32x2(lower, indices, upper);

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
            /// __m256i _mm256_permutex2var_epi64 (__m256i a, __m256i idx, __m256i b)
            ///   VPERMI2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            ///   VPERMT2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<long> PermuteVar4x64x2(Vector256<long> lower, Vector256<long> indices, Vector256<long> upper) => PermuteVar4x64x2(lower, indices, upper);
            /// <summary>
            /// __m256i _mm256_permutex2var_epi64 (__m256i a, __m256i idx, __m256i b)
            ///   VPERMI2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            ///   VPERMT2Q ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<ulong> PermuteVar4x64x2(Vector256<ulong> lower, Vector256<ulong> indices, Vector256<ulong> upper) => PermuteVar4x64x2(lower, indices, upper);
            /// <summary>
            /// __m256d _mm256_permutex2var_pd (__m256d a, __m256i idx, __m256i b)
            ///   VPERMI2PD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            ///   VPERMT2PD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<double> PermuteVar4x64x2(Vector256<double> lower, Vector256<long> indices, Vector256<double> upper) => PermuteVar4x64x2(lower, indices, upper);

            /// <summary>
            /// __m256i _mm256_permutex2var_epi32 (__m256i a, __m256i idx, __m256i b)
            ///   VPERMI2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
            ///   VPERMT2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
            /// </summary>
            public static Vector256<int> PermuteVar8x32x2(Vector256<int> lower, Vector256<int> indices, Vector256<int> upper) => PermuteVar8x32x2(lower, indices, upper);
            /// <summary>
            /// __m256i _mm256_permutex2var_epi32 (__m256i a, __m256i idx, __m256i b)
            ///   VPERMI2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
            ///   VPERMT2D ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
            /// </summary>
            public static Vector256<uint> PermuteVar8x32x2(Vector256<uint> lower, Vector256<uint> indices, Vector256<uint> upper) => PermuteVar8x32x2(lower, indices, upper);
            /// <summary>
            /// __m256 _mm256_permutex2var_ps (__m256 a, __m256i idx, __m256i b)
            ///   VPERMI2PS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
            ///   VPERMT2PS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
            /// </summary>
            public static Vector256<float> PermuteVar8x32x2(Vector256<float> lower, Vector256<int> indices, Vector256<float> upper) => PermuteVar8x32x2(lower, indices, upper);

            /// <summary>
            /// __m128 _mm_rcp14_ps (__m128 a, __m128 b)
            ///   VRCP14PS xmm1 {k1}{z}, xmm2/m128/m32bcst
            /// </summary>
            public static Vector128<float> Reciprocal14(Vector128<float> value) => Reciprocal14(value);
            /// <summary>
            /// __m128d _mm_rcp14_pd (__m128d a, __m128d b)
            ///   VRCP14PD xmm1 {k1}{z}, xmm2/m128/m64bcst
            /// </summary>
            public static Vector128<double> Reciprocal14(Vector128<double> value) => Reciprocal14(value);
            /// <summary>
            /// __m256 _mm256_rcp14_ps (__m256 a, __m256 b)
            ///   VRCP14PS ymm1 {k1}{z}, ymm2/m256/m32bcst
            /// </summary>
            public static Vector256<float> Reciprocal14(Vector256<float> value) => Reciprocal14(value);
            /// <summary>
            /// __m256d _mm256_rcp14_pd (__m256d a, __m256d b)
            ///   VRCP14PD ymm1 {k1}{z}, ymm2/m256/m64bcst
            /// </summary>
            public static Vector256<double> Reciprocal14(Vector256<double> value) => Reciprocal14(value);

            /// <summary>
            /// __m128 _mm_rsqrt14_ps (__m128 a, __m128 b)
            ///   VRSQRT14PS xmm1 {k1}{z}, xmm2/m128/m32bcst
            /// </summary>
            public static Vector128<float> ReciprocalSqrt14(Vector128<float> value) => ReciprocalSqrt14(value);
            /// <summary>
            /// __m128d _mm_rsqrt14_pd (__m128d a, __m128d b)
            ///   VRSQRT14PD xmm1 {k1}{z}, xmm2/m128/m64bcst
            /// </summary>
            public static Vector128<double> ReciprocalSqrt14(Vector128<double> value) => ReciprocalSqrt14(value);
            /// <summary>
            /// __m256 _mm256_rsqrt14_ps (__m256 a, __m256 b)
            ///   VRSQRT14PS ymm1 {k1}{z}, ymm2/m256/m32bcst
            /// </summary>
            public static Vector256<float> ReciprocalSqrt14(Vector256<float> value) => ReciprocalSqrt14(value);
            /// <summary>
            /// __m256d _mm256_rsqrt14_pd (__m256d a, __m256d b)
            ///   VRSQRT14PD ymm1 {k1}{z}, ymm2/m256/m64bcst
            /// </summary>
            public static Vector256<double> ReciprocalSqrt14(Vector256<double> value) => ReciprocalSqrt14(value);

            /// <summary>
            /// __m128i _mm_rol_epi32 (__m128i a, int imm8)
            ///   VPROLD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8
            /// </summary>
            public static Vector128<int> RotateLeft(Vector128<int> value, [ConstantExpected] byte count) => RotateLeft(value, count);
            /// <summary>
            /// __m128i _mm_rol_epi32 (__m128i a, int imm8)
            ///   VPROLD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8
            /// </summary>
            public static Vector128<uint> RotateLeft(Vector128<uint> value, [ConstantExpected] byte count) => RotateLeft(value, count);
            /// <summary>
            /// __m128i _mm_rol_epi64 (__m128i a, int imm8)
            ///   VPROLQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8
            /// </summary>
            public static Vector128<long> RotateLeft(Vector128<long> value, [ConstantExpected] byte count) => RotateLeft(value, count);
            /// <summary>
            /// __m128i _mm_rol_epi64 (__m128i a, int imm8)
            ///   VPROLQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8
            /// </summary>
            public static Vector128<ulong> RotateLeft(Vector128<ulong> value, [ConstantExpected] byte count) => RotateLeft(value, count);
            /// <summary>
            /// __m256i _mm256_rol_epi32 (__m256i a, int imm8)
            ///   VPROLD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8
            /// </summary>
            public static Vector256<int> RotateLeft(Vector256<int> value, [ConstantExpected] byte count) => RotateLeft(value, count);
            /// <summary>
            /// __m256i _mm256_rol_epi32 (__m256i a, int imm8)
            ///   VPROLD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8
            /// </summary>
            public static Vector256<uint> RotateLeft(Vector256<uint> value, [ConstantExpected] byte count) => RotateLeft(value, count);
            /// <summary>
            /// __m256i _mm256_rol_epi64 (__m256i a, int imm8)
            ///   VPROLQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8
            /// </summary>
            public static Vector256<long> RotateLeft(Vector256<long> value, [ConstantExpected] byte count) => RotateLeft(value, count);
            /// <summary>
            /// __m256i _mm256_rol_epi64 (__m256i a, int imm8)
            ///   VPROLQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8
            /// </summary>
            public static Vector256<ulong> RotateLeft(Vector256<ulong> value, [ConstantExpected] byte count) => RotateLeft(value, count);

            /// <summary>
            /// __m128i _mm_rolv_epi32 (__m128i a, __m128i b)
            ///   VPROLDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
            /// </summary>
            public static Vector128<int> RotateLeftVariable(Vector128<int> value, Vector128<uint> count) => RotateLeftVariable(value, count);
            /// <summary>
            /// __m128i _mm_rolv_epi32 (__m128i a, __m128i b)
            ///   VPROLDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
            /// </summary>
            public static Vector128<uint> RotateLeftVariable(Vector128<uint> value, Vector128<uint> count) => RotateLeftVariable(value, count);
            /// <summary>
            /// __m128i _mm_rolv_epi64 (__m128i a, __m128i b)
            ///   VPROLQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<long> RotateLeftVariable(Vector128<long> value, Vector128<ulong> count) => RotateLeftVariable(value, count);
            /// <summary>
            /// __m128i _mm_rolv_epi64 (__m128i a, __m128i b)
            ///   VPROLQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<ulong> RotateLeftVariable(Vector128<ulong> value, Vector128<ulong> count) => RotateLeftVariable(value, count);
            /// <summary>
            /// __m256i _mm256_rolv_epi32 (__m256i a, __m256i b)
            ///   VPROLDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
            /// </summary>
            public static Vector256<int> RotateLeftVariable(Vector256<int> value, Vector256<uint> count) => RotateLeftVariable(value, count);
            /// <summary>
            /// __m256i _mm256_rolv_epi32 (__m256i a, __m256i b)
            ///   VPROLDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
            /// </summary>
            public static Vector256<uint> RotateLeftVariable(Vector256<uint> value, Vector256<uint> count) => RotateLeftVariable(value, count);
            /// <summary>
            /// __m256i _mm256_rolv_epi64 (__m256i a, __m256i b)
            ///   VPROLQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<long> RotateLeftVariable(Vector256<long> value, Vector256<ulong> count) => RotateLeftVariable(value, count);
            /// <summary>
            /// __m256i _mm256_rolv_epi64 (__m256i a, __m256i b)
            ///   VPROLQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<ulong> RotateLeftVariable(Vector256<ulong> value, Vector256<ulong> count) => RotateLeftVariable(value, count);

            /// <summary>
            /// __m128i _mm_ror_epi32 (__m128i a, int imm8)
            ///   VPRORD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8
            /// </summary>
            public static Vector128<int> RotateRight(Vector128<int> value, [ConstantExpected] byte count) => RotateRight(value, count);
            /// <summary>
            /// __m128i _mm_ror_epi32 (__m128i a, int imm8)
            ///   VPRORD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8
            /// </summary>
            public static Vector128<uint> RotateRight(Vector128<uint> value, [ConstantExpected] byte count) => RotateRight(value, count);
            /// <summary>
            /// __m128i _mm_ror_epi64 (__m128i a, int imm8)
            ///   VPRORQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8
            /// </summary>
            public static Vector128<long> RotateRight(Vector128<long> value, [ConstantExpected] byte count) => RotateRight(value, count);
            /// <summary>
            /// __m128i _mm_ror_epi64 (__m128i a, int imm8)
            ///   VPRORQ xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8
            /// </summary>
            public static Vector128<ulong> RotateRight(Vector128<ulong> value, [ConstantExpected] byte count) => RotateRight(value, count);
            /// <summary>
            /// __m256i _mm256_ror_epi32 (__m256i a, int imm8)
            ///   VPRORD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8
            /// </summary>
            public static Vector256<int> RotateRight(Vector256<int> value, [ConstantExpected] byte count) => RotateRight(value, count);
            /// <summary>
            /// __m256i _mm256_ror_epi32 (__m256i a, int imm8)
            ///   VPRORD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8
            /// </summary>
            public static Vector256<uint> RotateRight(Vector256<uint> value, [ConstantExpected] byte count) => RotateRight(value, count);
            /// <summary>
            /// __m256i _mm256_ror_epi64 (__m256i a, int imm8)
            ///   VPRORQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8
            /// </summary>
            public static Vector256<long> RotateRight(Vector256<long> value, [ConstantExpected] byte count) => RotateRight(value, count);
            /// <summary>
            /// __m256i _mm256_ror_epi64 (__m256i a, int imm8)
            ///   VPRORQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8
            /// </summary>
            public static Vector256<ulong> RotateRight(Vector256<ulong> value, [ConstantExpected] byte count) => RotateRight(value, count);

            /// <summary>
            /// __m128i _mm_rorv_epi32 (__m128i a, __m128i b)
            ///   VPRORDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
            /// </summary>
            public static Vector128<int> RotateRightVariable(Vector128<int> value, Vector128<uint> count) => RotateRightVariable(value, count);
            /// <summary>
            /// __m128i _mm_rorv_epi32 (__m128i a, __m128i b)
            ///   VPRORDV xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
            /// </summary>
            public static Vector128<uint> RotateRightVariable(Vector128<uint> value, Vector128<uint> count) => RotateRightVariable(value, count);
            /// <summary>
            /// __m128i _mm_rorv_epi64 (__m128i a, __m128i b)
            ///   VPRORQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<long> RotateRightVariable(Vector128<long> value, Vector128<ulong> count) => RotateRightVariable(value, count);
            /// <summary>
            /// __m128i _mm_rorv_epi64 (__m128i a, __m128i b)
            ///   VPRORQV xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<ulong> RotateRightVariable(Vector128<ulong> value, Vector128<ulong> count) => RotateRightVariable(value, count);
            /// <summary>
            /// __m256i _mm256_rorv_epi32 (__m256i a, __m256i b)
            ///   VPRORDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
            /// </summary>
            public static Vector256<int> RotateRightVariable(Vector256<int> value, Vector256<uint> count) => RotateRightVariable(value, count);
            /// <summary>
            /// __m256i _mm256_rorv_epi32 (__m256i a, __m256i b)
            ///   VPRORDV ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
            /// </summary>
            public static Vector256<uint> RotateRightVariable(Vector256<uint> value, Vector256<uint> count) => RotateRightVariable(value, count);
            /// <summary>
            /// __m256i _mm256_rorv_epi64 (__m256i a, __m256i b)
            ///   VPRORQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<long> RotateRightVariable(Vector256<long> value, Vector256<ulong> count) => RotateRightVariable(value, count);
            /// <summary>
            /// __m256i _mm256_rorv_epi64 (__m256i a, __m256i b)
            ///   VPRORQV ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<ulong> RotateRightVariable(Vector256<ulong> value, Vector256<ulong> count) => RotateRightVariable(value, count);

            /// <summary>
            /// __m128 _mm_roundscale_ps (__m128 a, int imm)
            ///   VRNDSCALEPS xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8
            /// </summary>
            public static Vector128<float> RoundScale(Vector128<float> value, [ConstantExpected] byte control) => RoundScale(value, control);
            /// <summary>
            /// __m128d _mm_roundscale_pd (__m128d a, int imm)
            ///   VRNDSCALEPD xmm1 {k1}{z}, xmm2/m128/m64bcst, imm8
            /// </summary>
            public static Vector128<double> RoundScale(Vector128<double> value, [ConstantExpected] byte control) => RoundScale(value, control);
            /// <summary>
            /// __m256 _mm256_roundscale_ps (__m256 a, int imm)
            ///   VRNDSCALEPS ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8
            /// </summary>
            public static Vector256<float> RoundScale(Vector256<float> value, [ConstantExpected] byte control) => RoundScale(value, control);
            /// <summary>
            /// __m256d _mm256_roundscale_pd (__m256d a, int imm)
            ///   VRNDSCALEPD ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8
            /// </summary>
            public static Vector256<double> RoundScale(Vector256<double> value, [ConstantExpected] byte control) => RoundScale(value, control);

            /// <summary>
            /// __m128 _mm_scalef_ps (__m128 a, int imm)
            ///   VSCALEFPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
            /// </summary>
            public static Vector128<float> Scale(Vector128<float> left, Vector128<float> right) => Scale(left, right);
            /// <summary>
            /// __m128d _mm_scalef_pd (__m128d a, int imm)
            ///   VSCALEFPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst
            /// </summary>
            public static Vector128<double> Scale(Vector128<double> left, Vector128<double> right) => Scale(left, right);
            /// <summary>
            /// __m256 _mm256_scalef_ps (__m256 a, int imm)
            ///   VSCALEFPS ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst
            /// </summary>
            public static Vector256<float> Scale(Vector256<float> left, Vector256<float> right) => Scale(left, right);
            /// <summary>
            /// __m256d _mm256_scalef_pd (__m256d a, int imm)
            ///   VSCALEFPD ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst
            /// </summary>
            public static Vector256<double> Scale(Vector256<double> left, Vector256<double> right) => Scale(left, right);

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

            /// <summary>
            /// __m256d _mm256_shuffle_f64x2 (__m256d a, __m256d b, const int imm8)
            ///   VSHUFF64x2 ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
            /// </summary>
            public static Vector256<double> Shuffle2x128(Vector256<double> left, Vector256<double> right, [ConstantExpected] byte control) => Shuffle2x128(left, right, control);
            /// <summary>
            /// __m256i _mm256_shuffle_i32x4 (__m256i a, __m256i b, const int imm8)
            ///   VSHUFI32x4 ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
            /// </summary>
            public static Vector256<int> Shuffle2x128(Vector256<int> left, Vector256<int> right, [ConstantExpected] byte control) => Shuffle2x128(left, right, control);
            /// <summary>
            /// __m256i _mm256_shuffle_i64x2 (__m256i a, __m256i b, const int imm8)
            ///   VSHUFI64x2 ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
            /// </summary>
            public static Vector256<long> Shuffle2x128(Vector256<long> left, Vector256<long> right, [ConstantExpected] byte control) => Shuffle2x128(left, right, control);
            /// <summary>
            /// __m256 _mm256_shuffle_f32x4 (__m256 a, __m256 b, const int imm8)
            ///   VSHUFF32x4 ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
            /// </summary>
            public static Vector256<float> Shuffle2x128(Vector256<float> left, Vector256<float> right, [ConstantExpected] byte control) => Shuffle2x128(left, right, control);
            /// <summary>
            /// __m256i _mm256_shuffle_i32x4 (__m256i a, __m256i b, const int imm8)
            ///   VSHUFI32x4 ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
            /// </summary>
            public static Vector256<uint> Shuffle2x128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected] byte control) => Shuffle2x128(left, right, control);
            /// <summary>
            /// __m256i _mm256_shuffle_i64x2 (__m256i a, __m256i b, const int imm8)
            ///   VSHUFI64x2 ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
            /// </summary>
            public static Vector256<ulong> Shuffle2x128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected] byte control) => Shuffle2x128(left, right, control);

            /// <summary>
            /// __m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, byte imm)
            ///   VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
            /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
            /// </summary>
            public static Vector128<sbyte> TernaryLogic(Vector128<sbyte> a, Vector128<sbyte> b, Vector128<sbyte> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, byte imm)
            ///   VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
            /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
            /// </summary>
            public static Vector128<byte> TernaryLogic(Vector128<byte> a, Vector128<byte> b, Vector128<byte> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, short imm)
            ///   VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
            /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
            /// </summary>
            public static Vector128<short> TernaryLogic(Vector128<short> a, Vector128<short> b, Vector128<short> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m128i _mm_ternarylogic_si128 (__m128i a, __m128i b, __m128i c, short imm)
            ///   VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128, imm8
            /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
            /// </summary>
            public static Vector128<ushort> TernaryLogic(Vector128<ushort> a, Vector128<ushort> b, Vector128<ushort> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m128i _mm_ternarylogic_epi32 (__m128i a, __m128i b, __m128i c, int imm)
            ///   VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
            /// </summary>
            public static Vector128<int> TernaryLogic(Vector128<int> a, Vector128<int> b, Vector128<int> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m128i _mm_ternarylogic_epi32 (__m128i a, __m128i b, __m128i c, int imm)
            ///   VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
            /// </summary>
            public static Vector128<uint> TernaryLogic(Vector128<uint> a, Vector128<uint> b, Vector128<uint> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m128i _mm_ternarylogic_epi64 (__m128i a, __m128i b, __m128i c, int imm)
            ///   VPTERNLOGQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8
            /// </summary>
            public static Vector128<long> TernaryLogic(Vector128<long> a, Vector128<long> b, Vector128<long> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m128i _mm_ternarylogic_epi64 (__m128i a, __m128i b, __m128i c, int imm)
            ///   VPTERNLOGQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8
            /// </summary>
            public static Vector128<ulong> TernaryLogic(Vector128<ulong> a, Vector128<ulong> b, Vector128<ulong> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m128 _mm_ternarylogic_ps (__m128 a, __m128 b, __m128 c, int imm)
            ///   VPTERNLOGD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
            /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
            /// </summary>
            public static Vector128<float> TernaryLogic(Vector128<float> a, Vector128<float> b, Vector128<float> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m128d _mm_ternarylogic_pd (__m128d a, __m128d b, __m128d c, int imm)
            ///   VPTERNLOGQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8
            /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
            /// </summary>
            public static Vector128<double> TernaryLogic(Vector128<double> a, Vector128<double> b, Vector128<double> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, byte imm)
            ///   VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8
            /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
            /// </summary>
            public static Vector256<sbyte> TernaryLogic(Vector256<sbyte> a, Vector256<sbyte> b, Vector256<sbyte> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, byte imm)
            ///   VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8
            /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
            /// </summary>
            public static Vector256<byte> TernaryLogic(Vector256<byte> a, Vector256<byte> b, Vector256<byte> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, short imm)
            ///   VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8
            /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
            /// </summary>
            public static Vector256<short> TernaryLogic(Vector256<short> a, Vector256<short> b, Vector256<short> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m256i _mm256_ternarylogic_si256 (__m256i a, __m256i b, __m256i c, short imm)
            ///   VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256, imm8
            /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
            /// </summary>
            public static Vector256<ushort> TernaryLogic(Vector256<ushort> a, Vector256<ushort> b, Vector256<ushort> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m256i _mm256_ternarylogic_epi32 (__m256i a, __m256i b, __m256i c, int imm)
            ///   VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
            /// </summary>
            public static Vector256<int> TernaryLogic(Vector256<int> a, Vector256<int> b, Vector256<int> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m256i _mm256_ternarylogic_epi32 (__m256i a, __m256i b, __m256i c, int imm)
            ///   VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
            /// </summary>
            public static Vector256<uint> TernaryLogic(Vector256<uint> a, Vector256<uint> b, Vector256<uint> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m256i _mm256_ternarylogic_epi64 (__m256i a, __m256i b, __m256i c, int imm)
            ///   VPTERNLOGQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
            /// </summary>
            public static Vector256<long> TernaryLogic(Vector256<long> a, Vector256<long> b, Vector256<long> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m256i _mm256_ternarylogic_epi64 (__m256i a, __m256i b, __m256i c, int imm)
            ///   VPTERNLOGQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
            /// </summary>
            public static Vector256<ulong> TernaryLogic(Vector256<ulong> a, Vector256<ulong> b, Vector256<ulong> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m256 _mm256_ternarylogic_ps (__m256 a, __m256 b, __m256 c, int imm)
            ///   VPTERNLOGD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst, imm8
            /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
            /// </summary>
            public static Vector256<float> TernaryLogic(Vector256<float> a, Vector256<float> b, Vector256<float> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
            /// <summary>
            /// __m256d _mm256_ternarylogic_pd (__m256d a, __m256d b, __m256d c, int imm)
            ///   VPTERNLOGQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst, imm8
            /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
            /// </summary>
            public static Vector256<double> TernaryLogic(Vector256<double> a, Vector256<double> b, Vector256<double> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        }

        [Intrinsic]
        public new abstract class X64 : Avx2.X64
        {
            internal X64() { }

            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            /// __m128 _mm_cvtsi64_ss (__m128 a, __int64 b)
            ///   VCVTUSI2SS xmm1, xmm2, r/m64
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, ulong value) => ConvertScalarToVector128Single(upper, value);
            /// <summary>
            /// __m128d _mm_cvtsi64_sd (__m128d a, __int64 b)
            ///   VCVTUSI2SD xmm1, xmm2, r/m64
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, ulong value) => ConvertScalarToVector128Double(upper, value);

            /// <summary>
            /// unsigned __int64 _mm_cvtss_u64 (__m128 a)
            ///   VCVTSS2USI r64, xmm1/m32{er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<float> value) => ConvertToUInt64(value);
            /// <summary>
            /// unsigned __int64 _mm_cvtsd_u64 (__m128d a)
            ///   VCVTSD2USI r64, xmm1/m64{er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<double> value) => ConvertToUInt64(value);

            /// <summary>
            /// unsigned __int64 _mm_cvttss_u64 (__m128 a)
            ///   VCVTTSS2USI r64, xmm1/m32{er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong ConvertToUInt64WithTruncation(Vector128<float> value) => ConvertToUInt64WithTruncation(value);
            /// <summary>
            /// unsigned __int64 _mm_cvttsd_u64 (__m128d a)
            ///   VCVTTSD2USI r64, xmm1/m64{er}
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong ConvertToUInt64WithTruncation(Vector128<double> value) => ConvertToUInt64WithTruncation(value);
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
        /// __m512i _mm512_alignr_epi32 (__m512i a, __m512i b, const int count)
        ///   VALIGND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<int> AlignRight32(Vector512<int> left, Vector512<int> right, [ConstantExpected] byte mask) => AlignRight32(left, right, mask);
        /// <summary>
        /// __m512i _mm512_alignr_epi32 (__m512i a, __m512i b, const int count)
        ///   VALIGND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<uint> AlignRight32(Vector512<uint> left, Vector512<uint> right, [ConstantExpected] byte mask) => AlignRight32(left, right, mask);

        /// <summary>
        /// __m512i _mm512_alignr_epi64 (__m512i a, __m512i b, const int count)
        ///   VALIGNQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<long> AlignRight64(Vector512<long> left, Vector512<long> right, [ConstantExpected] byte mask) => AlignRight64(left, right, mask);
        /// <summary>
        /// __m512i _mm512_alignr_epi64 (__m512i a, __m512i b, const int count)
        ///   VALIGNQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<ulong> AlignRight64(Vector512<ulong> left, Vector512<ulong> right, [ConstantExpected] byte mask) => AlignRight64(left, right, mask);

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
        /// __m128 _mm_cvtsi32_ss (__m128 a, int b)
        ///   VCVTUSI2SS xmm1, xmm2, r/m32
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, uint value) => ConvertScalarToVector128Single(upper, value);
        /// <summary>
        /// __m128d _mm_cvtsi32_sd (__m128d a, int b)
        ///   VCVTUSI2SD xmm1, xmm2, r/m32
        /// </summary>
        public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, uint value) => ConvertScalarToVector128Double(upper, value);

        /// <summary>
        /// unsigned int _mm_cvtss_u32 (__m128 a)
        ///   VCVTSS2USI r32, xmm1/m32{er}
        /// </summary>
        public static uint ConvertToUInt32(Vector128<float> value) => ConvertToUInt32(value);
        /// <summary>
        /// unsigned int _mm_cvtsd_u32 (__m128d a)
        ///   VCVTSD2USI r32, xmm1/m64{er}
        /// </summary>
        public static uint ConvertToUInt32(Vector128<double> value) => ConvertToUInt32(value);
        /// <summary>
        /// unsigned int _mm_cvttss_u32 (__m128 a)
        ///   VCVTTSS2USI r32, xmm1/m32{er}
        /// </summary>
        public static uint ConvertToUInt32WithTruncation(Vector128<float> value) => ConvertToUInt32WithTruncation(value);
        /// <summary>
        /// unsigned int _mm_cvttsd_u32 (__m128d a)
        ///   VCVTTSD2USI r32, xmm1/m64{er}
        /// </summary>
        public static uint ConvertToUInt32WithTruncation(Vector128<double> value) => ConvertToUInt32WithTruncation(value);

        /// <summary>
        /// __m128i _mm512_cvtepi32_epi8 (__m512i a)
        ///   VPMOVDB xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector512<int> value) => ConvertToVector128Byte(value);
        /// <summary>
        /// __m128i _mm512_cvtepi64_epi8 (__m512i a)
        ///   VPMOVQB xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector512<long> value) => ConvertToVector128Byte(value);
        /// <summary>
        /// __m128i _mm512_cvtepi32_epi8 (__m512i a)
        ///   VPMOVDB xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector512<uint> value) => ConvertToVector128Byte(value);
        /// <summary>
        /// __m128i _mm512_cvtepi64_epi8 (__m512i a)
        ///   VPMOVQB xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128Byte(Vector512<ulong> value) => ConvertToVector128Byte(value);
        /// <summary>
        /// __m128i _mm512_cvtusepi32_epi8 (__m512i a)
        ///   VPMOVUSDB xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector512<uint> value) => ConvertToVector128ByteWithSaturation(value);
        /// <summary>
        /// __m128i _mm512_cvtusepi64_epi8 (__m512i a)
        ///   VPMOVUSQB xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector512<ulong> value) => ConvertToVector128ByteWithSaturation(value);

        /// <summary>
        /// __m128i _mm512_cvtepi64_epi16 (__m512i a)
        ///   VPMOVQW xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector512<long> value) => ConvertToVector128Int16(value);
        /// <summary>
        /// __m128i _mm512_cvtepi64_epi16 (__m512i a)
        ///   VPMOVQW xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector512<ulong> value) => ConvertToVector128Int16(value);
        /// <summary>
        /// __m128i _mm512_cvtsepi64_epi16 (__m512i a)
        ///   VPMOVSQW xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16WithSaturation(Vector512<long> value) => ConvertToVector128Int16WithSaturation(value);

        /// <summary>
        /// __m128i _mm512_cvtepi32_epi8 (__m512i a)
        ///   VPMOVDB xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector512<int> value) => ConvertToVector128SByte(value);
        /// <summary>
        /// __m128i _mm512_cvtepi64_epi8 (__m512i a)
        ///   VPMOVQB xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector512<long> value) => ConvertToVector128SByte(value);
        /// <summary>
        /// __m128i _mm512_cvtepi32_epi8 (__m512i a)
        ///   VPMOVDB xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector512<uint> value) => ConvertToVector128SByte(value);
        /// <summary>
        /// __m128i _mm512_cvtepi64_epi8 (__m512i a)
        ///   VPMOVQB xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByte(Vector512<ulong> value) => ConvertToVector128SByte(value);
        /// <summary>
        /// __m128i _mm512_cvtsepi32_epi8 (__m512i a)
        ///   VPMOVSDB xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector512<int> value) => ConvertToVector128SByteWithSaturation(value);
        /// <summary>
        /// __m128i _mm512_cvtsepi64_epi8 (__m512i a)
        ///   VPMOVSQB xmm1/m64 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector512<long> value) => ConvertToVector128SByteWithSaturation(value);

        /// <summary>
        /// __m128i _mm512_cvtepi64_epi16 (__m512i a)
        ///   VPMOVQW xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector512<long> value) => ConvertToVector128UInt16(value);
        /// <summary>
        /// __m128i _mm512_cvtepi64_epi16 (__m512i a)
        ///   VPMOVQW xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16(Vector512<ulong> value) => ConvertToVector128UInt16(value);
        /// <summary>
        /// __m128i _mm512_cvtusepi64_epi16 (__m512i a)
        ///   VPMOVUSQW xmm1/m128 {k1}{z}, zmm2
        /// </summary>
        public static Vector128<ushort> ConvertToVector128UInt16WithSaturation(Vector512<ulong> value) => ConvertToVector128UInt16WithSaturation(value);

        /// <summary>
        /// __m256i _mm512_cvtepi32_epi16 (__m512i a)
        ///   VPMOVDW ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<short> ConvertToVector256Int16(Vector512<int> value) => ConvertToVector256Int16(value);
        /// <summary>
        /// __m256i _mm512_cvtepi32_epi16 (__m512i a)
        ///   VPMOVDW ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<short> ConvertToVector256Int16(Vector512<uint> value) => ConvertToVector256Int16(value);
        /// <summary>
        /// __m256i _mm512_cvtsepi32_epi16 (__m512i a)
        ///   VPMOVSDW ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<short> ConvertToVector256Int16WithSaturation(Vector512<int> value) => ConvertToVector256Int16WithSaturation(value);

        /// <summary>
        /// __m256i _mm512_cvtpd_epi32 (__m512d a)
        ///   VCVTPD2DQ ymm1 {k1}{z}, zmm2/m512/m64bcst{er}
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector512<double> value) => ConvertToVector256Int32(value);
        /// <summary>
        /// __m256i _mm512_cvtepi64_epi32 (__m512i a)
        ///   VPMOVQD ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector512<long> value) => ConvertToVector256Int32(value);
        /// <summary>
        /// __m256i _mm512_cvtepi64_epi32 (__m512i a)
        ///   VPMOVQD ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector512<ulong> value) => ConvertToVector256Int32(value);
        /// <summary>
        /// __m256i _mm512_cvtsepi64_epi32 (__m512i a)
        ///   VPMOVSQD ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32WithSaturation(Vector512<long> value) => ConvertToVector256Int32WithSaturation(value);
        /// <summary>
        /// __m256i _mm512_cvttpd_epi32 (__m512d a)
        ///   VCVTTPD2DQ ymm1 {k1}{z}, zmm2/m512/m64bcst{sae}
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32WithTruncation(Vector512<double> value) => ConvertToVector256Int32WithTruncation(value);

        /// <summary>
        /// __m256 _mm512_cvtpd_ps (__m512d a)
        ///   VCVTPD2PS ymm1,         zmm2/m512
        ///   VCVTPD2PS ymm1 {k1}{z}, zmm2/m512/m64bcst{er}
        /// </summary>
        public static Vector256<float> ConvertToVector256Single(Vector512<double> value) => ConvertToVector256Single(value);

        /// <summary>
        /// __m256i _mm512_cvtepi32_epi16 (__m512i a)
        ///   VPMOVDW ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<ushort> ConvertToVector256UInt16(Vector512<int> value) => ConvertToVector256UInt16(value);
        /// <summary>
        /// __m256i _mm512_cvtepi32_epi16 (__m512i a)
        ///   VPMOVDW ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<ushort> ConvertToVector256UInt16(Vector512<uint> value) => ConvertToVector256UInt16(value);
        /// <summary>
        /// __m256i _mm512_cvtusepi32_epi16 (__m512i a)
        ///   VPMOVUSDW ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<ushort> ConvertToVector256UInt16WithSaturation(Vector512<uint> value) => ConvertToVector256UInt16WithSaturation(value);

        /// <summary>
        /// __m256i _mm512_cvtpd_epu32 (__m512d a)
        ///   VCVTPD2UDQ ymm1 {k1}{z}, zmm2/m512/m64bcst{er}
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32(Vector512<double> value) => ConvertToVector256UInt32(value);
        /// <summary>
        /// __m256i _mm512_cvtepi64_epi32 (__m512i a)
        ///   VPMOVQD ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32(Vector512<long> value) => ConvertToVector256UInt32(value);
        /// <summary>
        /// __m256i _mm512_cvtepi64_epi32 (__m512i a)
        ///   VPMOVQD ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32(Vector512<ulong> value) => ConvertToVector256UInt32(value);
        /// <summary>
        /// __m256i _mm512_cvtusepi64_epi32 (__m512i a)
        ///   VPMOVUSQD ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32WithSaturation(Vector512<ulong> value) => ConvertToVector256UInt32WithSaturation(value);
        /// <summary>
        /// __m256i _mm512_cvttpd_epu32 (__m512d a)
        ///   VCVTTPD2UDQ ymm1 {k1}{z}, zmm2/m512/m64bcst{er}
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt32WithTruncation(Vector512<double> value) => ConvertToVector256UInt32WithTruncation(value);

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
        /// __m512d _mm512_cvtepu32_pd (__m256i a)
        ///   VCVTUDQ2PD zmm1 {k1}{z}, ymm2/m256/m32bcst
        /// </summary>
        public static Vector512<double> ConvertToVector512Double(Vector256<uint> value) => ConvertToVector512Double(value);
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
        /// __m512i _mm512_cvttps_epi32 (__m512 a)
        ///   VCVTTPS2DQ zmm1 {k1}{z}, zmm2/m512/m32bcst{sae}
        /// </summary>
        public static Vector512<int> ConvertToVector512Int32WithTruncation(Vector512<float> value) => ConvertToVector512Int32WithTruncation(value);
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
        /// __m512 _mm512_cvtepu32_ps (__m512i a)
        ///   VCVTUDQ2PS zmm1 {k1}{z}, zmm2/m512/m32bcst{er}
        /// </summary>
        public static Vector512<float> ConvertToVector512Single(Vector512<uint> value) => ConvertToVector512Single(value);
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
        /// __m512i _mm512_cvtps_epu32 (__m512 a)
        ///   VCVTPS2UDQ zmm1 {k1}{z}, zmm2/m512/m32bcst{er}
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32(Vector512<float> value) => ConvertToVector512UInt32(value);
        /// <summary>
        /// __m512i _mm512_cvttps_epu32 (__m512 a)
        ///   VCVTTPS2UDQ zmm1 {k1}{z}, zmm2/m512/m32bcst{er}
        /// </summary>
        public static Vector512<uint> ConvertToVector512UInt32WithTruncation(Vector512<float> value) => ConvertToVector512UInt32WithTruncation(value);
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
        /// __m512 _mm512_fixupimm_ps(__m512 a, __m512 b, __m512i tbl, int imm);
        ///   VFIXUPIMMPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{sae}, imm8
        /// </summary>
        public static Vector512<float> Fixup(Vector512<float> left, Vector512<float> right, Vector512<int> table, [ConstantExpected] byte control) => Fixup(left, right, table, control);
        /// <summary>
        /// __m512d _mm512_fixupimm_pd(__m512d a, __m512d b, __m512i tbl, int imm);
        ///   VFIXUPIMMPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{sae}, imm8
        /// </summary>
        public static Vector512<double> Fixup(Vector512<double> left, Vector512<double> right, Vector512<long> table, [ConstantExpected] byte control) => Fixup(left, right, table, control);

        /// <summary>
        /// __m128 _mm_fixupimm_ss(__m128 a, __m128 b, __m128i tbl, int imm);
        ///   VFIXUPIMMSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8
        /// </summary>
        public static Vector128<float> FixupScalar(Vector128<float> left, Vector128<float> right, Vector128<int> table, [ConstantExpected] byte control) => FixupScalar(left, right, table, control);
        /// <summary>
        /// __m128d _mm_fixupimm_sd(__m128d a, __m128d b, __m128i tbl, int imm);
        ///   VFIXUPIMMSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8
        /// </summary>
        public static Vector128<double> FixupScalar(Vector128<double> left, Vector128<double> right, Vector128<long> table, [ConstantExpected] byte control) => FixupScalar(left, right, table, control);

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
        /// __m512 _mm512_getexp_ps (__m512 a)
        ///   VGETEXPPS zmm1 {k1}{z}, zmm2/m512/m32bcst{sae}
        /// </summary>
        public static Vector512<float> GetExponent(Vector512<float> value) => GetExponent(value);
        /// <summary>
        /// __m512d _mm512_getexp_pd (__m512d a)
        ///   VGETEXPPD zmm1 {k1}{z}, zmm2/m512/m64bcst{sae}
        /// </summary>
        public static Vector512<double> GetExponent(Vector512<double> value) => GetExponent(value);

        /// <summary>
        /// __m128 _mm_getexp_ss (__m128 a)
        ///   VGETEXPSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}
        /// </summary>
        public static Vector128<float> GetExponentScalar(Vector128<float> value) => GetExponentScalar(value);
        /// <summary>
        /// __m128d _mm_getexp_sd (__m128d a)
        ///   VGETEXPSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}
        /// </summary>
        public static Vector128<double> GetExponentScalar(Vector128<double> value) => GetExponentScalar(value);
        /// <summary>
        /// __m128 _mm_getexp_ss (__m128 a, __m128 b)
        ///   VGETEXPSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> GetExponentScalar(Vector128<float> upper, Vector128<float> value) => GetExponentScalar(upper, value);
        /// <summary>
        /// __m128d _mm_getexp_sd (__m128d a, __m128d b)
        ///   VGETEXPSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> GetExponentScalar(Vector128<double> upper, Vector128<double> value) => GetExponentScalar(upper, value);

        /// <summary>
        /// __m512 _mm512_getmant_ps (__m512 a)
        ///   VGETMANTPS zmm1 {k1}{z}, zmm2/m512/m32bcst{sae}
        /// </summary>
        public static Vector512<float> GetMantissa(Vector512<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissa(value, control);
        /// <summary>
        /// __m512d _mm512_getmant_pd (__m512d a)
        ///   VGETMANTPD zmm1 {k1}{z}, zmm2/m512/m64bcst{sae}
        /// </summary>
        public static Vector512<double> GetMantissa(Vector512<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissa(value, control);

        /// <summary>
        /// __m128 _mm_getmant_ss (__m128 a)
        ///   VGETMANTSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}
        /// </summary>
        public static Vector128<float> GetMantissaScalar(Vector128<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissaScalar(value, control);
        /// <summary>
        /// __m128d _mm_getmant_sd (__m128d a)
        ///   VGETMANTSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}
        /// </summary>
        public static Vector128<double> GetMantissaScalar(Vector128<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissaScalar(value, control);
        /// <summary>
        /// __m128 _mm_getmant_ss (__m128 a, __m128 b)
        ///   VGETMANTSS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> GetMantissaScalar(Vector128<float> upper, Vector128<float> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissaScalar(upper, value, control);
        /// <summary>
        /// __m128d _mm_getmant_sd (__m128d a, __m128d b)
        ///   VGETMANTSD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> GetMantissaScalar(Vector128<double> upper, Vector128<double> value, [ConstantExpected(Max = (byte)(0x0F))] byte control) => GetMantissaScalar(upper, value, control);

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
        /// __m512i _mm512_permutex2var_epi64 (__m512i a, __m512i idx, __m512i b)
        ///   VPERMI2Q zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        ///   VPERMT2Q zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> PermuteVar8x64x2(Vector512<long> lower, Vector512<long> indices, Vector512<long> upper) => PermuteVar8x64x2(lower, indices, upper);
        /// <summary>
        /// __m512i _mm512_permutex2var_epi64 (__m512i a, __m512i idx, __m512i b)
        ///   VPERMI2Q zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        ///   VPERMT2Q zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> PermuteVar8x64x2(Vector512<ulong> lower, Vector512<ulong> indices, Vector512<ulong> upper) => PermuteVar8x64x2(lower, indices, upper);
        /// <summary>
        /// __m512d _mm512_permutex2var_pd (__m512d a, __m512i idx, __m512i b)
        ///   VPERMI2PD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        ///   VPERMT2PD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<double> PermuteVar8x64x2(Vector512<double> lower, Vector512<long> indices, Vector512<double> upper) => PermuteVar8x64x2(lower, indices, upper);

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
        /// __m512i _mm512_permutex2var_epi32 (__m512i a, __m512i idx, __m512i b)
        ///   VPERMI2D zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        ///   VPERMT2D zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> PermuteVar16x32x2(Vector512<int> lower, Vector512<int> indices, Vector512<int> upper) => PermuteVar16x32x2(lower, indices, upper);
        /// <summary>
        /// __m512i _mm512_permutex2var_epi32 (__m512i a, __m512i idx, __m512i b)
        ///   VPERMI2D zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        ///   VPERMT2D zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> PermuteVar16x32x2(Vector512<uint> lower, Vector512<uint> indices, Vector512<uint> upper) => PermuteVar16x32x2(lower, indices, upper);
        /// <summary>
        /// __m512 _mm512_permutex2var_ps (__m512 a, __m512i idx, __m512i b)
        ///   VPERMI2PS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        ///   VPERMT2PS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<float> PermuteVar16x32x2(Vector512<float> lower, Vector512<int> indices, Vector512<float> upper) => PermuteVar16x32x2(lower, indices, upper);

        /// <summary>
        /// __m512 _mm512_rcp14_ps (__m512 a, __m512 b)
        ///   VRCP14PS zmm1 {k1}{z}, zmm2/m512/m32bcst
        /// </summary>
        public static Vector512<float> Reciprocal14(Vector512<float> value) => Reciprocal14(value);
        /// <summary>
        /// __m512d _mm512_rcp14_pd (__m512d a, __m512d b)
        ///   VRCP14PD zmm1 {k1}{z}, zmm2/m512/m64bcst
        /// </summary>
        public static Vector512<double> Reciprocal14(Vector512<double> value) => Reciprocal14(value);

        /// <summary>
        /// __m128 _mm_rcp14_ss (__m128 a)
        ///   VRCP14SS xmm1 {k1}{z}, xmm2, xmm3/m32
        /// </summary>
        public static Vector128<float> Reciprocal14Scalar(Vector128<float> value) => Reciprocal14Scalar(value);
        /// <summary>
        /// __m128d _mm_rcp14_sd (__m128d a)
        ///   VRCP14SD xmm1 {k1}{z}, xmm2, xmm3/m64
        /// </summary>
        public static Vector128<double> Reciprocal14Scalar(Vector128<double> value) => Reciprocal14Scalar(value);
        /// <summary>
        /// __m128 _mm_rcp14_ss (__m128 a, __m128 b)
        ///   VRCP14SS xmm1 {k1}{z}, xmm2, xmm3/m32
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> Reciprocal14Scalar(Vector128<float> upper, Vector128<float> value) => Reciprocal14Scalar(upper, value);
        /// <summary>
        /// __m128d _mm_rcp14_sd (__m128d a, __m128d b)
        ///   VRCP14SD xmm1 {k1}{z}, xmm2, xmm3/m64
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> Reciprocal14Scalar(Vector128<double> upper, Vector128<double> value) => Reciprocal14Scalar(upper, value);

        /// <summary>
        /// __m512 _mm512_rsqrt14_ps (__m512 a, __m512 b)
        ///   VRSQRT14PS zmm1 {k1}{z}, zmm2/m512/m32bcst
        /// </summary>
        public static Vector512<float> ReciprocalSqrt14(Vector512<float> value) => ReciprocalSqrt14(value);
        /// <summary>
        /// __m512d _mm512_rsqrt14_pd (__m512d a, __m512d b)
        ///   VRSQRT14PD zmm1 {k1}{z}, zmm2/m512/m64bcst
        /// </summary>
        public static Vector512<double> ReciprocalSqrt14(Vector512<double> value) => ReciprocalSqrt14(value);

        /// <summary>
        /// __m128 _mm_rsqrt14_ss (__m128 a)
        ///   VRSQRT14SS xmm1 {k1}{z}, xmm2, xmm3/m32
        /// </summary>
        public static Vector128<float> ReciprocalSqrt14Scalar(Vector128<float> value) => ReciprocalSqrt14Scalar(value);
        /// <summary>
        /// __m128d _mm_rsqrt14_sd (__m128d a)
        ///   VRSQRT14SD xmm1 {k1}{z}, xmm2, xmm3/m64
        /// </summary>
        public static Vector128<double> ReciprocalSqrt14Scalar(Vector128<double> value) => ReciprocalSqrt14Scalar(value);
        /// <summary>
        /// __m128 _mm_rsqrt14_ss (__m128 a, __m128 b)
        ///   VRSQRT14SS xmm1 {k1}{z}, xmm2, xmm3/m32
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> ReciprocalSqrt14Scalar(Vector128<float> upper, Vector128<float> value) => ReciprocalSqrt14Scalar(upper, value);
        /// <summary>
        /// __m128d _mm_rsqrt14_sd (__m128d a, __m128d b)
        ///   VRSQRT14SD xmm1 {k1}{z}, xmm2, xmm3/m64
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> ReciprocalSqrt14Scalar(Vector128<double> upper, Vector128<double> value) => ReciprocalSqrt14Scalar(upper, value);

        /// <summary>
        /// __m512i _mm512_rol_epi32 (__m512i a, int imm8)
        ///   VPROLD zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<int> RotateLeft(Vector512<int> value, [ConstantExpected] byte count) => RotateLeft(value, count);
        /// <summary>
        /// __m512i _mm512_rol_epi32 (__m512i a, int imm8)
        ///   VPROLD zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<uint> RotateLeft(Vector512<uint> value, [ConstantExpected] byte count) => RotateLeft(value, count);
        /// <summary>
        /// __m512i _mm512_rol_epi64 (__m512i a, int imm8)
        ///   VPROLQ zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<long> RotateLeft(Vector512<long> value, [ConstantExpected] byte count) => RotateLeft(value, count);
        /// <summary>
        /// __m512i _mm512_rol_epi64 (__m512i a, int imm8)
        ///   VPROLQ zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<ulong> RotateLeft(Vector512<ulong> value, [ConstantExpected] byte count) => RotateLeft(value, count);

        /// <summary>
        /// __m512i _mm512_rolv_epi32 (__m512i a, __m512i b)
        ///   VPROLDV zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> RotateLeftVariable(Vector512<int> value, Vector512<uint> count) => RotateLeftVariable(value, count);
        /// <summary>
        /// __m512i _mm512_rolv_epi32 (__m512i a, __m512i b)
        ///   VPROLDV zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> RotateLeftVariable(Vector512<uint> value, Vector512<uint> count) => RotateLeftVariable(value, count);
        /// <summary>
        /// __m512i _mm512_rolv_epi64 (__m512i a, __m512i b)
        ///   VPROLQV zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> RotateLeftVariable(Vector512<long> value, Vector512<ulong> count) => RotateLeftVariable(value, count);
        /// <summary>
        /// __m512i _mm512_rolv_epi64 (__m512i a, __m512i b)
        ///   VPROLQV zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> RotateLeftVariable(Vector512<ulong> value, Vector512<ulong> count) => RotateLeftVariable(value, count);

        /// <summary>
        /// __m512i _mm512_ror_epi32 (__m512i a, int imm8)
        ///   VPRORD zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<int> RotateRight(Vector512<int> value, [ConstantExpected] byte count) => RotateRight(value, count);
        /// <summary>
        /// __m512i _mm512_ror_epi32 (__m512i a, int imm8)
        ///   VPRORD zmm1 {k1}{z}, zmm2/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<uint> RotateRight(Vector512<uint> value, [ConstantExpected] byte count) => RotateRight(value, count);
        /// <summary>
        /// __m512i _mm512_ror_epi64 (__m512i a, int imm8)
        ///   VPRORQ zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<long> RotateRight(Vector512<long> value, [ConstantExpected] byte count) => RotateRight(value, count);
        /// <summary>
        /// __m512i _mm512_ror_epi64 (__m512i a, int imm8)
        ///   VPRORQ zmm1 {k1}{z}, zmm2/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<ulong> RotateRight(Vector512<ulong> value, [ConstantExpected] byte count) => RotateRight(value, count);

        /// <summary>
        /// __m512i _mm512_rorv_epi32 (__m512i a, __m512i b)
        ///   VPRORDV zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> RotateRightVariable(Vector512<int> value, Vector512<uint> count) => RotateRightVariable(value, count);
        /// <summary>
        /// __m512i _mm512_rorv_epi32 (__m512i a, __m512i b)
        ///   VPRORDV zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> RotateRightVariable(Vector512<uint> value, Vector512<uint> count) => RotateRightVariable(value, count);
        /// <summary>
        /// __m512i _mm512_rorv_epi64 (__m512i a, __m512i b)
        ///   VPRORQV zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> RotateRightVariable(Vector512<long> value, Vector512<ulong> count) => RotateRightVariable(value, count);
        /// <summary>
        /// __m512i _mm512_rorv_epi64 (__m512i a, __m512i b)
        ///   VPRORQV zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> RotateRightVariable(Vector512<ulong> value, Vector512<ulong> count) => RotateRightVariable(value, count);

        /// <summary>
        /// __m512 _mm512_roundscale_ps (__m512 a, int imm)
        ///   VRNDSCALEPS zmm1 {k1}{z}, zmm2/m512/m32bcst{sae}, imm8
        /// </summary>
        public static Vector512<float> RoundScale(Vector512<float> value, [ConstantExpected] byte control) => RoundScale(value, control);
        /// <summary>
        /// __m512d _mm512_roundscale_pd (__m512d a, int imm)
        ///   VRNDSCALEPD zmm1 {k1}{z}, zmm2/m512/m64bcst{sae}, imm8
        /// </summary>
        public static Vector512<double> RoundScale(Vector512<double> value, [ConstantExpected] byte control) => RoundScale(value, control);

        /// <summary>
        /// __m128 _mm_roundscale_ss (__m128 a, int imm)
        ///   VRNDSCALESS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8
        /// </summary>
        public static Vector128<float> RoundScaleScalar(Vector128<float> value, [ConstantExpected] byte control) => RoundScaleScalar(value, control);
        /// <summary>
        /// __m128d _mm_roundscale_sd (__m128d a, int imm)
        ///   VRNDSCALESD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8
        /// </summary>
        public static Vector128<double> RoundScaleScalar(Vector128<double> value, [ConstantExpected] byte control) => RoundScaleScalar(value, control);
        /// <summary>
        /// __m128 _mm_roundscale_ss (__m128 a, __m128 b, int imm)
        ///   VRNDSCALESS xmm1 {k1}{z}, xmm2, xmm3/m32{sae}, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> RoundScaleScalar(Vector128<float> upper, Vector128<float> value, [ConstantExpected] byte control) => RoundScaleScalar(upper, value, control);
        /// <summary>
        /// __m128d _mm_roundscale_sd (__m128d a, __m128d b, int imm)
        ///   VRNDSCALESD xmm1 {k1}{z}, xmm2, xmm3/m64{sae}, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> RoundScaleScalar(Vector128<double> upper, Vector128<double> value, [ConstantExpected] byte control) => RoundScaleScalar(upper, value, control);

        /// <summary>
        /// __m512 _mm512_scalef_ps (__m512 a, __m512 b)
        ///   VSCALEFPS zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst{er}
        /// </summary>
        public static Vector512<float> Scale(Vector512<float> left, Vector512<float> right) => Scale(left, right);
        /// <summary>
        /// __m512d _mm512_scalef_pd (__m512d a, __m512d b)
        ///   VSCALEFPD zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst{er}
        /// </summary>
        public static Vector512<double> Scale(Vector512<double> left, Vector512<double> right) => Scale(left, right);

        /// <summary>
        /// __m128 _mm_scalef_ss (__m128 a, __m128 b)
        ///   VSCALEFSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}
        /// </summary>
        public static Vector128<float> ScaleScalar(Vector128<float> left, Vector128<float> right) => ScaleScalar(left, right);
        /// <summary>
        /// __m128d _mm_scalef_sd (__m128d a, __m128d b)
        ///   VSCALEFSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}
        /// </summary>
        public static Vector128<double> ScaleScalar(Vector128<double> left, Vector128<double> right) => ScaleScalar(left, right);

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
        /// __m512d _mm512_shuffle_f64x2 (__m512d a, __m512d b, const int imm8)
        ///   VSHUFF64x2 zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<double> Shuffle4x128(Vector512<double> left, Vector512<double> right, [ConstantExpected] byte control) => Shuffle4x128(left, right, control);
        /// <summary>
        /// __m512i _mm512_shuffle_i32x4 (__m512i a, __m512i b, const int imm8)
        ///   VSHUFI32x4 zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<int> Shuffle4x128(Vector512<int> left, Vector512<int> right, [ConstantExpected] byte control) => Shuffle4x128(left, right, control);
        /// <summary>
        /// __m512i _mm512_shuffle_i64x2 (__m512i a, __m512i b, const int imm8)
        ///   VSHUFI64x2 zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<long> Shuffle4x128(Vector512<long> left, Vector512<long> right, [ConstantExpected] byte control) => Shuffle4x128(left, right, control);
        /// <summary>
        /// __m512 _mm512_shuffle_f32x4 (__m512 a, __m512 b, const int imm8)
        ///   VSHUFF32x4 zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<float> Shuffle4x128(Vector512<float> left, Vector512<float> right, [ConstantExpected] byte control) => Shuffle4x128(left, right, control);
        /// <summary>
        /// __m512i _mm512_shuffle_i32x4 (__m512i a, __m512i b, const int imm8)
        ///   VSHUFI32x4 zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<uint> Shuffle4x128(Vector512<uint> left, Vector512<uint> right, [ConstantExpected] byte control) => Shuffle4x128(left, right, control);
        /// <summary>
        /// __m512i _mm512_shuffle_i64x2 (__m512i a, __m512i b, const int imm8)
        ///   VSHUFI64x2 zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<ulong> Shuffle4x128(Vector512<ulong> left, Vector512<ulong> right, [ConstantExpected] byte control) => Shuffle4x128(left, right, control);

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
        /// __m512i _mm512_ternarylogic_si512 (__m512i a, __m512i b, __m512i c, int imm)
        ///   VPTERNLOGD zmm1 {k1}{z}, zmm2, zmm3/m512, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector512<sbyte> TernaryLogic(Vector512<sbyte> a, Vector512<sbyte> b, Vector512<sbyte> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        /// __m512i _mm512_ternarylogic_si512 (__m512i a, __m512i b, __m512i c, byte imm)
        ///   VPTERNLOGD zmm1 {k1}{z}, zmm2, zmm3/m512, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector512<byte> TernaryLogic(Vector512<byte> a, Vector512<byte> b, Vector512<byte> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        /// __m512i _mm512_ternarylogic_si512 (__m512i a, __m512i b, __m512i c, short imm)
        ///   VPTERNLOGD zmm1 {k1}{z}, zmm2, zmm3/m512, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector512<short> TernaryLogic(Vector512<short> a, Vector512<short> b, Vector512<short> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        /// __m512i _mm512_ternarylogic_si512 (__m512i a, __m512i b, __m512i c, short imm)
        ///   VPTERNLOGD zmm1 {k1}{z}, zmm2, zmm3/m512, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector512<ushort> TernaryLogic(Vector512<ushort> a, Vector512<ushort> b, Vector512<ushort> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        /// __m512i _mm512_ternarylogic_epi32 (__m512i a, __m512i b, __m512i c, int imm)
        ///   VPTERNLOGD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<int> TernaryLogic(Vector512<int> a, Vector512<int> b, Vector512<int> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        /// __m512i _mm512_ternarylogic_epi32 (__m512i a, __m512i b, __m512i c, int imm)
        ///   VPTERNLOGD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8
        /// </summary>
        public static Vector512<uint> TernaryLogic(Vector512<uint> a, Vector512<uint> b, Vector512<uint> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        /// __m512i _mm512_ternarylogic_epi64 (__m512i a, __m512i b, __m512i c, int imm)
        ///   VPTERNLOGQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<long> TernaryLogic(Vector512<long> a, Vector512<long> b, Vector512<long> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        /// __m512i _mm512_ternarylogic_epi64 (__m512i a, __m512i b, __m512i c, int imm)
        ///   VPTERNLOGQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8
        /// </summary>
        public static Vector512<ulong> TernaryLogic(Vector512<ulong> a, Vector512<ulong> b, Vector512<ulong> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        /// __m512 _mm512_ternarylogic_ps (__m512 a, __m512 b, __m512 c, int imm)
        ///   VPTERNLOGD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector512<float> TernaryLogic(Vector512<float> a, Vector512<float> b, Vector512<float> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);
        /// <summary>
        /// __m512d _mm512_ternarylogic_pd (__m512d a, __m512d b, __m512d c, int imm)
        ///   VPTERNLOGQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst, imm8
        /// The above native signature does not exist. We provide this additional overload for consistency with the other bitwise APIs.
        /// </summary>
        public static Vector512<double> TernaryLogic(Vector512<double> a, Vector512<double> b, Vector512<double> c, [ConstantExpected] byte control) => TernaryLogic(a, b, c, control);

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
