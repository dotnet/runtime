// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>This class provides access to X86 AVX512BW hardware instructions via intrinsics</summary>
    [CLSCompliant(false)]
    public abstract class Avx512BW : Avx512F
    {
        internal Avx512BW() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        public new abstract class VL : Avx512F.VL
        {
            internal VL() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            /// __m128i _mm_cmpgt_epu8 (__m128i a, __m128i b)
            ///   VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(6)
            /// </summary>
            public static Vector128<byte> CompareGreaterThan(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cmpge_epu8 (__m128i a, __m128i b)
            ///   VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(5)
            /// </summary>
            public static Vector128<byte> CompareGreaterThanOrEqual(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cmplt_epu8 (__m128i a, __m128i b)
            ///   VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(1)
            /// </summary>
            public static Vector128<byte> CompareLessThan(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cmple_epu8 (__m128i a, __m128i b)
            ///   VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(2)
            /// </summary>
            public static Vector128<byte> CompareLessThanOrEqual(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cmpne_epu8 (__m128i a, __m128i b)
            ///   VPCMPUB k1 {k2}, xmm2, xmm3/m128, imm8(4)
            /// </summary>
            public static Vector128<byte> CompareNotEqual(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m256i _mm256_cmpgt_epu8 (__m256i a, __m256i b)
            ///   VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(6)
            /// </summary>
            public static Vector256<byte> CompareGreaterThan(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_cmpge_epu8 (__m256i a, __m256i b)
            ///   VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(5)
            /// </summary>
            public static Vector256<byte> CompareGreaterThanOrEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_cmplt_epu8 (__m256i a, __m256i b)
            ///   VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(1)
            /// </summary>
            public static Vector256<byte> CompareLessThan(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_cmple_epu8 (__m256i a, __m256i b)
            ///   VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(2)
            /// </summary>
            public static Vector256<byte> CompareLessThanOrEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_cmpne_epu8 (__m256i a, __m256i b)
            ///   VPCMPUB k1 {k2}, ymm2, ymm3/m256, imm8(4)
            /// </summary>
            public static Vector256<byte> CompareNotEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128i _mm_cmpge_epi16 (__m128i a, __m128i b)
            ///   VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(5)
            /// </summary>
            public static Vector128<short> CompareGreaterThanOrEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cmplt_epi16 (__m128i a, __m128i b)
            ///   VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(1)
            /// </summary>
            public static Vector128<short> CompareLessThan(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cmple_epi16 (__m128i a, __m128i b)
            ///   VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(2)
            /// </summary>
            public static Vector128<short> CompareLessThanOrEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cmpne_epi16 (__m128i a, __m128i b)
            ///   VPCMPW k1 {k2}, xmm2, xmm3/m128, imm8(4)
            /// </summary>
            public static Vector128<short> CompareNotEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m256i _mm256_cmpge_epi16 (__m256i a, __m256i b)
            ///   VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(5)
            /// </summary>
            public static Vector256<short> CompareGreaterThanOrEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_cmplt_epi16 (__m256i a, __m256i b)
            ///   VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(1)
            /// </summary>
            public static Vector256<short> CompareLessThan(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_cmple_epi16 (__m256i a, __m256i b)
            ///   VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(2)
            /// </summary>
            public static Vector256<short> CompareLessThanOrEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_cmpne_epi16 (__m256i a, __m256i b)
            ///   VPCMPW k1 {k2}, ymm2, ymm3/m256, imm8(4)
            /// </summary>
            public static Vector256<short> CompareNotEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128i _mm_cmpge_epi8 (__m128i a, __m128i b)
            ///   VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(5)
            /// </summary>
            public static Vector128<sbyte> CompareGreaterThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cmplt_epi8 (__m128i a, __m128i b)
            ///   VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(1)
            /// </summary>
            public static Vector128<sbyte> CompareLessThan(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cmple_epi8 (__m128i a, __m128i b)
            ///   VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(2)
            /// </summary>
            public static Vector128<sbyte> CompareLessThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cmpne_epi8 (__m128i a, __m128i b)
            ///   VPCMPB k1 {k2}, xmm2, xmm3/m128, imm8(4)
            /// </summary>
            public static Vector128<sbyte> CompareNotEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m256i _mm256_cmpge_epi8 (__m256i a, __m256i b)
            ///   VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(5)
            /// </summary>
            public static Vector256<sbyte> CompareGreaterThanOrEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_cmplt_epi8 (__m256i a, __m256i b)
            ///   VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(1)
            /// </summary>
            public static Vector256<sbyte> CompareLessThan(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_cmple_epi8 (__m256i a, __m256i b)
            ///   VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(2)
            /// </summary>
            public static Vector256<sbyte> CompareLessThanOrEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_cmpne_epi8 (__m256i a, __m256i b)
            ///   VPCMPB k1 {k2}, ymm2, ymm3/m256, imm8(4)
            /// </summary>
            public static Vector256<sbyte> CompareNotEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128i _mm_cmpgt_epu16 (__m128i a, __m128i b)
            ///   VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(6)
            /// </summary>
            public static Vector128<ushort> CompareGreaterThan(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cmpge_epu16 (__m128i a, __m128i b)
            ///   VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(5)
            /// </summary>
            public static Vector128<ushort> CompareGreaterThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cmplt_epu16 (__m128i a, __m128i b)
            ///   VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(1)
            /// </summary>
            public static Vector128<ushort> CompareLessThan(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cmple_epu16 (__m128i a, __m128i b)
            ///   VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(2)
            /// </summary>
            public static Vector128<ushort> CompareLessThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cmpne_epu16 (__m128i a, __m128i b)
            ///   VPCMPUW k1 {k2}, xmm2, xmm3/m128, imm8(4)
            /// </summary>
            public static Vector128<ushort> CompareNotEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m256i _mm256_cmpgt_epu16 (__m256i a, __m256i b)
            ///   VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(6)
            /// </summary>
            public static Vector256<ushort> CompareGreaterThan(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_cmpge_epu16 (__m256i a, __m256i b)
            ///   VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(5)
            /// </summary>
            public static Vector256<ushort> CompareGreaterThanOrEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_cmplt_epu16 (__m256i a, __m256i b)
            ///   VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(1)
            /// </summary>
            public static Vector256<ushort> CompareLessThan(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_cmple_epu16 (__m256i a, __m256i b)
            ///   VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(2)
            /// </summary>
            public static Vector256<ushort> CompareLessThanOrEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_cmpne_epu16 (__m256i a, __m256i b)
            ///   VPCMPUW k1 {k2}, ymm2, ymm3/m256, imm8(4)
            /// </summary>
            public static Vector256<ushort> CompareNotEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }


            /// <summary>
            /// __m128i _mm_cvtepi16_epi8 (__m128i a)
            ///   VPMOVWB xmm1/m64 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector128<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cvtepi16_epi8 (__m128i a)
            ///   VPMOVWB xmm1/m64 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm256_cvtepi16_epi8 (__m256i a)
            ///   VPMOVWB xmm1/m128 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector256<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm256_cvtepi16_epi8 (__m256i a)
            ///   VPMOVWB xmm1/m128 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128Byte(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cvtusepi16_epi8 (__m128i a)
            ///   VPMOVUWB xmm1/m64 {k1}{z}, xmm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm256_cvtusepi16_epi8 (__m256i a)
            ///   VPMOVUWB xmm1/m128 {k1}{z}, ymm2
            /// </summary>
            public static Vector128<byte> ConvertToVector128ByteWithSaturation(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128i _mm_cvtepi16_epi8 (__m128i a)
            ///   VPMOVWB xmm1/m64 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector128<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cvtepi16_epi8 (__m128i a)
            ///   VPMOVWB xmm1/m64 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm256_cvtepi16_epi8 (__m256i a)
            ///   VPMOVWB xmm1/m128 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector256<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm256_cvtepi16_epi8 (__m256i a)
            ///   VPMOVWB xmm1/m128 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByte(Vector256<ushort> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm_cvtsepi16_epi8 (__m128i a)
            ///   VPMOVSWB xmm1/m64 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector128<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128i _mm256_cvtsepi16_epi8 (__m256i a)
            ///   VPMOVSWB xmm1/m128 {k1}{z}, zmm2
            /// </summary>
            public static Vector128<sbyte> ConvertToVector128SByteWithSaturation(Vector256<short> value) { throw new PlatformNotSupportedException(); }

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
            /// __m128i _mm_srav_epi16 (__m128i a, __m128i count)
            ///   VPSRAVW xmm1 {k1}{z}, xmm2, xmm3/m128
            /// </summary>
            public static Vector128<short> ShiftRightArithmeticVariable(Vector128<short> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }
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
            /// __m128i _mm_dbsad_epu8 (__m128i a, __m128i b, int imm8)
            ///   VDBPSADBW xmm1 {k1}{z}, xmm2, xmm3/m128
            /// </summary>
            public static Vector128<ushort> SumAbsoluteDifferencesInBlock32(Vector128<byte> left, Vector128<byte> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m256i _mm256_dbsad_epu8 (__m256i a, __m256i b, int imm8)
            ///   VDBPSADBW ymm1 {k1}{z}, ymm2, ymm3/m256
            /// </summary>
            public static Vector256<ushort> SumAbsoluteDifferencesInBlock32(Vector256<byte> left, Vector256<byte> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        }

        public new abstract class X64 : Avx512F.X64
        {
            internal X64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        /// __m512i _mm512_abs_epi8 (__m512i a)
        ///   VPABSB zmm1 {k1}{z}, zmm2/m512
        /// </summary>
        public static Vector512<byte> Abs(Vector512<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_abs_epi16 (__m512i a)
        ///   VPABSW zmm1 {k1}{z}, zmm2/m512
        /// </summary>
        public static Vector512<ushort> Abs(Vector512<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_add_epi8 (__m512i a, __m512i b)
        ///   VPADDB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<sbyte> Add(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_add_epi8 (__m512i a, __m512i b)
        ///   VPADDB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<byte> Add(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_add_epi16 (__m512i a, __m512i b)
        ///   VPADDW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> Add(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_add_epi16 (__m512i a, __m512i b)
        ///   VPADDW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> Add(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_adds_epi8 (__m512i a, __m512i b)
        ///   VPADDSB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<sbyte> AddSaturate(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_adds_epu8 (__m512i a, __m512i b)
        ///   VPADDUSB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<byte> AddSaturate(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_adds_epi16 (__m512i a, __m512i b)
        ///   VPADDSW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> AddSaturate(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_adds_epu16 (__m512i a, __m512i b)
        ///   VPADDUSW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> AddSaturate(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_alignr_epi8 (__m512i a, __m512i b, const int count)
        ///   VPALIGNR zmm1 {k1}{z}, zmm2, zmm3/m512, imm8
        /// </summary>
        public static Vector512<sbyte> AlignRight(Vector512<sbyte> left, Vector512<sbyte> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_alignr_epi8 (__m512i a, __m512i b, const int count)
        ///   VPALIGNR zmm1 {k1}{z}, zmm2, zmm3/m512, imm8
        /// </summary>
        public static Vector512<byte> AlignRight(Vector512<byte> left, Vector512<byte> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_avg_epu8 (__m512i a, __m512i b)
        ///   VPAVGB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<byte> Average(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_avg_epu16 (__m512i a, __m512i b)
        ///   VPAVGW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> Average(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_blendv_epu8 (__m512i a, __m512i b, __m512i mask)
        ///   VPBLENDMB zmm1 {k1}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<byte> BlendVariable(Vector512<byte> left, Vector512<byte> right, Vector512<byte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_blendv_epi16 (__m512i a, __m512i b, __m512i mask)
        ///   VPBLENDMW zmm1 {k1}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> BlendVariable(Vector512<short> left, Vector512<short> right, Vector512<short> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_blendv_epi8 (__m512i a, __m512i b, __m512i mask)
        ///   VPBLENDMB zmm1 {k1}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<sbyte> BlendVariable(Vector512<sbyte> left, Vector512<sbyte> right, Vector512<sbyte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_blendv_epu16 (__m512i a, __m512i b, __m512i mask)
        ///   VPBLENDMW zmm1 {k1}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> BlendVariable(Vector512<ushort> left, Vector512<ushort> right, Vector512<ushort> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_broadcastb_epi8 (__m128i a)
        ///   VPBROADCASTB zmm1 {k1}{z}, xmm2/m8
        /// </summary>
        public static Vector512<byte> BroadcastScalarToVector512(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_broadcastb_epi8 (__m128i a)
        ///   VPBROADCASTB zmm1 {k1}{z}, xmm2/m8
        /// </summary>
        public static Vector512<sbyte> BroadcastScalarToVector512(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_broadcastw_epi16 (__m128i a)
        ///   VPBROADCASTW zmm1 {k1}{z}, xmm2/m16
        /// </summary>
        public static Vector512<short> BroadcastScalarToVector512(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_broadcastw_epi16 (__m128i a)
        ///   VPBROADCASTW zmm1 {k1}{z}, xmm2/m16
        /// </summary>
        public static Vector512<ushort> BroadcastScalarToVector512(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_cmpeq_epu8 (__m512i a, __m512i b)
        ///   VPCMPEQB k1 {k2}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<byte> CompareEqual(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmpgt_epu8 (__m512i a, __m512i b)
        ///   VPCMPUB k1 {k2}, zmm2, zmm3/m512, imm8(6)
        /// </summary>
        public static Vector512<byte> CompareGreaterThan(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmpge_epu8 (__m512i a, __m512i b)
        ///   VPCMPUB k1 {k2}, zmm2, zmm3/m512, imm8(5)
        /// </summary>
        public static Vector512<byte> CompareGreaterThanOrEqual(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmplt_epu8 (__m512i a, __m512i b)
        ///   VPCMPUB k1 {k2}, zmm2, zmm3/m512, imm8(1)
        /// </summary>
        public static Vector512<byte> CompareLessThan(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmple_epu8 (__m512i a, __m512i b)
        ///   VPCMPUB k1 {k2}, zmm2, zmm3/m512, imm8(2)
        /// </summary>
        public static Vector512<byte> CompareLessThanOrEqual(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmpne_epu8 (__m512i a, __m512i b)
        ///   VPCMPUB k1 {k2}, zmm2, zmm3/m512, imm8(4)
        /// </summary>
        public static Vector512<byte> CompareNotEqual(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_cmpeq_epi16 (__m512i a, __m512i b)
        ///   VPCMPEQW k1 {k2}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> CompareEqual(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmpgt_epi16 (__m512i a, __m512i b)
        ///   VPCMPGTW k1 {k2}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> CompareGreaterThan(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmpge_epi16 (__m512i a, __m512i b)
        ///   VPCMPW k1 {k2}, zmm2, zmm3/m512, imm8(5)
        /// </summary>
        public static Vector512<short> CompareGreaterThanOrEqual(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmplt_epi16 (__m512i a, __m512i b)
        ///   VPCMPW k1 {k2}, zmm2, zmm3/m512, imm8(1)
        /// </summary>
        public static Vector512<short> CompareLessThan(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmple_epi16 (__m512i a, __m512i b)
        ///   VPCMPW k1 {k2}, zmm2, zmm3/m512, imm8(2)
        /// </summary>
        public static Vector512<short> CompareLessThanOrEqual(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmpne_epi16 (__m512i a, __m512i b)
        ///   VPCMPW k1 {k2}, zmm2, zmm3/m512, imm8(4)
        /// </summary>
        public static Vector512<short> CompareNotEqual(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_cmpeq_epi8 (__m512i a, __m512i b)
        ///   VPCMPEQB k1 {k2}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<sbyte> CompareEqual(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmpgt_epi8 (__m512i a, __m512i b)
        ///   VPCMPGTB k1 {k2}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<sbyte> CompareGreaterThan(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmpge_epi8 (__m512i a, __m512i b)
        ///   VPCMPB k1 {k2}, zmm2, zmm3/m512, imm8(5)
        /// </summary>
        public static Vector512<sbyte> CompareGreaterThanOrEqual(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmplt_epi8 (__m512i a, __m512i b)
        ///   VPCMPB k1 {k2}, zmm2, zmm3/m512, imm8(1)
        /// </summary>
        public static Vector512<sbyte> CompareLessThan(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmple_epi8 (__m512i a, __m512i b)
        ///   VPCMPB k1 {k2}, zmm2, zmm3/m512, imm8(2)
        /// </summary>
        public static Vector512<sbyte> CompareLessThanOrEqual(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmpne_epi8 (__m512i a, __m512i b)
        ///   VPCMPB k1 {k2}, zmm2, zmm3/m512, imm8(4)
        /// </summary>
        public static Vector512<sbyte> CompareNotEqual(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_cmpeq_epu16 (__m512i a, __m512i b)
        ///   VPCMPEQW k1 {k2}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> CompareEqual(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmpgt_epu16 (__m512i a, __m512i b)
        ///   VPCMPUW k1 {k2}, zmm2, zmm3/m512, imm8(6)
        /// </summary>
        public static Vector512<ushort> CompareGreaterThan(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmpge_epu16 (__m512i a, __m512i b)
        ///   VPCMPUW k1 {k2}, zmm2, zmm3/m512, imm8(5)
        /// </summary>
        public static Vector512<ushort> CompareGreaterThanOrEqual(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmplt_epu16 (__m512i a, __m512i b)
        ///   VPCMPUW k1 {k2}, zmm2, zmm3/m512, imm8(1)
        /// </summary>
        public static Vector512<ushort> CompareLessThan(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmple_epu16 (__m512i a, __m512i b)
        ///   VPCMPUW k1 {k2}, zmm2, zmm3/m512, imm8(2)
        /// </summary>
        public static Vector512<ushort> CompareLessThanOrEqual(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cmpne_epu16 (__m512i a, __m512i b)
        ///   VPCMPUW k1 {k2}, zmm2, zmm3/m512, imm8(4)
        /// </summary>
        public static Vector512<ushort> CompareNotEqual(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm512_cvtepi16_epi8 (__m512i a)
        ///   VPMOVWB ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<byte> ConvertToVector256Byte(Vector512<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm512_cvtepi16_epi8 (__m512i a)
        ///   VPMOVWB ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<byte> ConvertToVector256Byte(Vector512<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm512_cvtusepi16_epi8 (__m512i a)
        ///   VPMOVUWB ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<byte> ConvertToVector256ByteWithSaturation(Vector512<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm512_cvtepi16_epi8 (__m512i a)
        ///   VPMOVWB ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<sbyte> ConvertToVector256SByte(Vector512<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm512_cvtepi16_epi8 (__m512i a)
        ///   VPMOVWB ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<sbyte> ConvertToVector256SByte(Vector512<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm512_cvtsepi16_epi8 (__m512i a)
        ///   VPMOVSWB ymm1/m256 {k1}{z}, zmm2
        /// </summary>
        public static Vector256<sbyte> ConvertToVector256SByteWithSaturation(Vector512<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_cvtepi8_epi16 (__m128i a)
        ///   VPMOVSXBW zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<short> ConvertToVector512Int16(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepu8_epi16 (__m128i a)
        ///   VPMOVZXBW zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<short> ConvertToVector512Int16(Vector256<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepi8_epi16 (__m128i a)
        ///   VPMOVSXBW zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<ushort> ConvertToVector512UInt16(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_cvtepu8_epi16 (__m128i a)
        ///   VPMOVZXBW zmm1 {k1}{z}, ymm2/m256
        /// </summary>
        public static Vector512<ushort> ConvertToVector512UInt16(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_loadu_epi8 (__m512i const * mem_addr)
        ///   VMOVDQU8 zmm1 {k1}{z}, m512
        /// </summary>
        public static new unsafe Vector512<sbyte> LoadVector512(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_epi8 (__m512i const * mem_addr)
        ///   VMOVDQU8 zmm1 {k1}{z}, m512
        /// </summary>
        public static new unsafe Vector512<byte> LoadVector512(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_epi16 (__m512i const * mem_addr)
        ///   VMOVDQU16 zmm1 {k1}{z}, m512
        /// </summary>
        public static new unsafe Vector512<short> LoadVector512(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_epi16 (__m512i const * mem_addr)
        ///   VMOVDQU16 zmm1 {k1}{z}, m512
        /// </summary>
        public static new unsafe Vector512<ushort> LoadVector512(ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_max_epi8 (__m512i a, __m512i b)
        ///   VPMAXSB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<sbyte> Max(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_max_epu8 (__m512i a, __m512i b)
        ///   VPMAXUB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<byte> Max(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_max_epi16 (__m512i a, __m512i b)
        ///   VPMAXSW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> Max(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_max_epu16 (__m512i a, __m512i b)
        ///   VPMAXUW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> Max(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_min_epi8 (__m512i a, __m512i b)
        ///   VPMINSB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<sbyte> Min(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_min_epu8 (__m512i a, __m512i b)
        ///   VPMINUB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<byte> Min(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_min_epi16 (__m512i a, __m512i b)
        ///   VPMINSW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> Min(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_min_epu16 (__m512i a, __m512i b)
        ///   VPMINUW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> Min(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_madd_epi16 (__m512i a, __m512i b)
        ///   VPMADDWD zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<int> MultiplyAddAdjacent(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_maddubs_epi16 (__m512i a, __m512i b)
        ///   VPMADDUBSW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> MultiplyAddAdjacent(Vector512<byte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_mulhi_epi16 (__m512i a, __m512i b)
        ///   VPMULHW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> MultiplyHigh(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_mulhi_epu16 (__m512i a, __m512i b)
        ///   VPMULHUW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> MultiplyHigh(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_mulhrs_epi16 (__m512i a, __m512i b)
        ///   VPMULHRSW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> MultiplyHighRoundScale(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_mullo_epi16 (__m512i a, __m512i b)
        ///   VPMULLW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> MultiplyLow(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_mullo_epi16 (__m512i a, __m512i b)
        ///   VPMULLW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> MultiplyLow(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_packs_epi16 (__m512i a, __m512i b)
        ///   VPACKSSWB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<sbyte> PackSignedSaturate(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_packs_epi32 (__m512i a, __m512i b)
        ///   VPACKSSDW zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<short> PackSignedSaturate(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_packus_epi16 (__m512i a, __m512i b)
        ///   VPACKUSWB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<byte> PackUnsignedSaturate(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_packus_epi32 (__m512i a, __m512i b)
        ///   VPACKUSDW zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<ushort> PackUnsignedSaturate(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_permutevar32x16_epi16 (__m512i a, __m512i b)
        ///   VPERMW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> PermuteVar32x16(Vector512<short> left, Vector512<short> control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_permutevar32x16_epi16 (__m512i a, __m512i b)
        ///   VPERMW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> PermuteVar32x16(Vector512<ushort> left, Vector512<ushort> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_permutex2var_epi16 (__m512i a, __m512i idx, __m512i b)
        ///   VPERMI2W zmm1 {k1}{z}, zmm2, zmm3/m512
        ///   VPERMT2W zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> PermuteVar32x16x2(Vector512<short> lower, Vector512<short> indices, Vector512<short> upper) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_permutex2var_epi16 (__m512i a, __m512i idx, __m512i b)
        ///   VPERMI2W zmm1 {k1}{z}, zmm2, zmm3/m512
        ///   VPERMT2W zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> PermuteVar32x16x2(Vector512<ushort> lower, Vector512<ushort> indices, Vector512<ushort> upper) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_sll_epi16 (__m512i a, __m128i count)
        ///   VPSLLW zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<short> ShiftLeftLogical(Vector512<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_sll_epi16 (__m512i a, __m128i count)
        ///   VPSLLW zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<ushort> ShiftLeftLogical(Vector512<ushort> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_slli_epi16 (__m512i a, int imm8)
        ///   VPSLLW zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<short> ShiftLeftLogical(Vector512<short> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_slli_epi16 (__m512i a, int imm8)
        ///   VPSLLW zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<ushort> ShiftLeftLogical(Vector512<ushort> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_bslli_epi128 (__m512i a, const int imm8)
        ///   VPSLLDQ zmm1, zmm2/m512, imm8
        /// </summary>
        public static Vector512<sbyte> ShiftLeftLogical128BitLane(Vector512<sbyte> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_bslli_epi128 (__m512i a, const int imm8)
        ///   VPSLLDQ zmm1, zmm2/m512, imm8
        /// </summary>
        public static Vector512<byte> ShiftLeftLogical128BitLane(Vector512<byte> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_sllv_epi16 (__m512i a, __m512i count)
        ///   VPSLLVW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> ShiftLeftLogicalVariable(Vector512<short> value, Vector512<ushort> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_sllv_epi16 (__m512i a, __m512i count)
        ///   VPSLLVW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> ShiftLeftLogicalVariable(Vector512<ushort> value, Vector512<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// _mm512_sra_epi16 (__m512i a, __m128i count)
        ///   VPSRAW zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<short> ShiftRightArithmetic(Vector512<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_srai_epi16 (__m512i a, int imm8)
        ///   VPSRAW zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<short> ShiftRightArithmetic(Vector512<short> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_srav_epi16 (__m512i a, __m512i count)
        ///   VPSRAVW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> ShiftRightArithmeticVariable(Vector512<short> value, Vector512<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_srl_epi16 (__m512i a, __m128i count)
        ///   VPSRLW zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<short> ShiftRightLogical(Vector512<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_srl_epi16 (__m512i a, __m128i count)
        ///   VPSRLW zmm1 {k1}{z}, zmm2, xmm3/m128
        /// </summary>
        public static Vector512<ushort> ShiftRightLogical(Vector512<ushort> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_srli_epi16 (__m512i a, int imm8)
        ///   VPSRLW zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<short> ShiftRightLogical(Vector512<short> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_srli_epi16 (__m512i a, int imm8)
        ///   VPSRLW zmm1 {k1}{z}, zmm2, imm8
        /// </summary>
        public static Vector512<ushort> ShiftRightLogical(Vector512<ushort> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_bsrli_epi128 (__m512i a, const int imm8)
        ///   VPSRLDQ zmm1, zmm2/m128, imm8
        /// </summary>
        public static Vector512<sbyte> ShiftRightLogical128BitLane(Vector512<sbyte> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_bsrli_epi128 (__m512i a, const int imm8)
        ///   VPSRLDQ zmm1, zmm2/m128, imm8
        /// </summary>
        public static Vector512<byte> ShiftRightLogical128BitLane(Vector512<byte> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_srlv_epi16 (__m512i a, __m512i count)
        ///   VPSRLVW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> ShiftRightLogicalVariable(Vector512<short> value, Vector512<ushort> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_srlv_epi16 (__m512i a, __m512i count)
        ///   VPSRLVW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> ShiftRightLogicalVariable(Vector512<ushort> value, Vector512<ushort> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_shuffle_epi8 (__m512i a, __m512i b)
        ///   VPSHUFB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<sbyte> Shuffle(Vector512<sbyte> value, Vector512<sbyte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_shuffle_epi8 (__m512i a, __m512i b)
        ///   VPSHUFB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<byte> Shuffle(Vector512<byte> value, Vector512<byte> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_shufflehi_epi16 (__m512i a, const int imm8)
        ///   VPSHUFHW zmm1 {k1}{z}, zmm2/m512, imm8
        /// </summary>
        public static Vector512<short> ShuffleHigh(Vector512<short> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_shufflehi_epi16 (__m512i a, const int imm8)
        ///   VPSHUFHW zmm1 {k1}{z}, zmm2/m512, imm8
        /// </summary>
        public static Vector512<ushort> ShuffleHigh(Vector512<ushort> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_shufflelo_epi16 (__m512i a, const int imm8)
        ///   VPSHUFLW zmm1 {k1}{z}, zmm2/m512, imm8
        /// </summary>
        public static Vector512<short> ShuffleLow(Vector512<short> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_shufflelo_epi16 (__m512i a, const int imm8)
        ///   VPSHUFLW zmm1 {k1}{z}, zmm2/m512, imm8
        /// </summary>
        public static Vector512<ushort> ShuffleLow(Vector512<ushort> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm512_storeu_epi8 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU8 m512 {k1}{z}, zmm1
        /// </summary>
        public static new unsafe void Store(sbyte* address, Vector512<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_epi8 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU8 m512 {k1}{z}, zmm1
        /// </summary>
        public static new unsafe void Store(byte* address, Vector512<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_epi16 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU16 m512 {k1}{z}, zmm1
        /// </summary>
        public static new unsafe void Store(short* address, Vector512<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_epi16 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU16 m512 {k1}{z}, zmm1
        /// </summary>
        public static new unsafe void Store(ushort* address, Vector512<ushort> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_sub_epi8 (__m512i a, __m512i b)
        ///   VPSUBB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<sbyte> Subtract(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_sub_epi8 (__m512i a, __m512i b)
        ///   VPSUBB zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<byte> Subtract(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_sub_epi16 (__m512i a, __m512i b)
        ///   VPSUBW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> Subtract(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_sub_epi16 (__m512i a, __m512i b)
        ///   VPSUBW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> Subtract(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_subs_epi8 (__m512i a, __m512i b)
        ///   VPSUBSB zmm1 {k1}{z}, zmm2, zmm3/m128
        /// </summary>
        public static Vector512<sbyte> SubtractSaturate(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_subs_epi16 (__m512i a, __m512i b)
        ///   VPSUBSW zmm1 {k1}{z}, zmm2, zmm3/m128
        /// </summary>
        public static Vector512<short> SubtractSaturate(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_subs_epu8 (__m512i a, __m512i b)
        ///   VPSUBUSB zmm1 {k1}{z}, zmm2, zmm3/m128
        /// </summary>
        public static Vector512<byte> SubtractSaturate(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_subs_epu16 (__m512i a, __m512i b)
        ///   VPSUBUSW zmm1 {k1}{z}, zmm2, zmm3/m128
        /// </summary>
        public static Vector512<ushort> SubtractSaturate(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_sad_epu8 (__m512i a, __m512i b)
        ///   VPSADBW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> SumAbsoluteDifferences(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_dbsad_epu8 (__m512i a, __m512i b)
        ///   VDBPSADBW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> SumAbsoluteDifferencesInBlock32(Vector512<byte> left, Vector512<byte> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_unpackhi_epi8 (__m512i a, __m512i b)
        ///   VPUNPCKHBW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<sbyte> UnpackHigh(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_unpackhi_epi8 (__m512i a, __m512i b)
        ///   VPUNPCKHBW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<byte> UnpackHigh(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_unpackhi_epi16 (__m512i a, __m512i b)
        ///   VPUNPCKHWD zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> UnpackHigh(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_unpackhi_epi16 (__m512i a, __m512i b)
        ///   VPUNPCKHWD zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> UnpackHigh(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_unpacklo_epi8 (__m512i a, __m512i b)
        ///   VPUNPCKLBW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<sbyte> UnpackLow(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_unpacklo_epi8 (__m512i a, __m512i b)
        ///   VPUNPCKLBW zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<byte> UnpackLow(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_unpacklo_epi16 (__m512i a, __m512i b)
        ///   VPUNPCKLWD zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<short> UnpackLow(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_unpacklo_epi16 (__m512i a, __m512i b)
        ///   VPUNPCKLWD zmm1 {k1}{z}, zmm2, zmm3/m512
        /// </summary>
        public static Vector512<ushort> UnpackLow(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
    }
}
