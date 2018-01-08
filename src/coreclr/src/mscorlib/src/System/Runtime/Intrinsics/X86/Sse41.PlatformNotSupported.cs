// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel SSE4.1 hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public static class Sse41
    {
        public static bool IsSupported { get { return false; } }
        
        /// <summary>
        /// __m128i _mm_blend_epi16 (__m128i a, __m128i b, const int imm8)
        /// </summary>
        public static Vector128<short> Blend(Vector128<short> left, Vector128<short> right, byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_blend_epi16 (__m128i a, __m128i b, const int imm8)
        /// </summary>
        public static Vector128<ushort> Blend(Vector128<ushort> left, Vector128<ushort> right, byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_blend_ps (__m128 a, __m128 b, const int imm8)
        /// </summary>
        public static Vector128<float> Blend(Vector128<float> left, Vector128<float> right, byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_blend_pd (__m128d a, __m128d b, const int imm8)
        /// </summary>
        public static Vector128<double> Blend(Vector128<double> left, Vector128<double> right, byte control) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m128i _mm_blendv_epi8 (__m128i a, __m128i b, __m128i mask)
        /// </summary>
        public static Vector128<sbyte> BlendVariable(Vector128<sbyte> left, Vector128<sbyte> right, Vector128<sbyte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_blendv_epi8 (__m128i a, __m128i b, __m128i mask)
        /// </summary>
        public static Vector128<byte> BlendVariable(Vector128<byte> left, Vector128<byte> right, Vector128<byte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_blendv_ps (__m128 a, __m128 b, __m128 mask)
        /// </summary>
        public static Vector128<float> BlendVariable(Vector128<float> left, Vector128<float> right, Vector128<float> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_blendv_pd (__m128d a, __m128d b, __m128d mask)
        /// </summary>
        public static Vector128<double> BlendVariable(Vector128<double> left, Vector128<double> right, Vector128<double> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_ceil_ps (__m128 a)
        /// </summary>
        public static Vector128<float> Ceiling(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_ceil_pd (__m128d a)
        /// </summary>
        public static Vector128<double> Ceiling(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_ceil_sd (__m128d a)
        /// </summary>
        public static Vector128<double> CeilingScalar(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_ceil_ss (__m128 a)
        /// </summary>
        public static Vector128<float> CeilingScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpeq_epi64 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<long> CompareEqual(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cmpeq_epi64 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<ulong> CompareEqual(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtepi8_epi16 (__m128i a)
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvtepu8_epi16 (__m128i a)
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvtepi8_epi32 (__m128i a)
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvtepu8_epi32 (__m128i a)
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvtepi16_epi32 (__m128i a)
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvtepu16_epi32 (__m128i a)
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvtepi8_epi64 (__m128i a)
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvtepu8_epi64 (__m128i a)
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvtepi16_epi64 (__m128i a)
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvtepu16_epi64 (__m128i a)
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvtepi32_epi64 (__m128i a)
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvtepu32_epi64 (__m128i a)
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_dp_ps (__m128 a, __m128 b, const int imm8)
        /// </summary>
        public static Vector128<float> DotProduct(Vector128<float> left, Vector128<float> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_dp_pd (__m128d a, __m128d b, const int imm8)
        /// </summary>
        public static Vector128<double> DotProduct(Vector128<double> left, Vector128<double> right, byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_extract_epi8 (__m128i a, const int imm8)
        /// </summary>
        public static sbyte Extract(Vector128<sbyte> value, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_extract_epi8 (__m128i a, const int imm8)
        /// </summary>
        public static byte Extract(Vector128<byte> value, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_extract_epi32 (__m128i a, const int imm8)
        /// </summary>
        public static int Extract(Vector128<int> value, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_extract_epi32 (__m128i a, const int imm8)
        /// </summary>
        public static uint Extract(Vector128<uint> value, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __int64 _mm_extract_epi64 (__m128i a, const int imm8)
        /// </summary>
        public static long Extract(Vector128<long> value, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __int64 _mm_extract_epi64 (__m128i a, const int imm8)
        /// </summary>
        public static ulong Extract(Vector128<ulong> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_extract_ps (__m128 a, const int imm8)
        /// </summary>
        public static float Extract(Vector128<float> value, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_floor_ps (__m128 a)
        /// </summary>
        public static Vector128<float> Floor(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_floor_pd (__m128d a)
        /// </summary>
        public static Vector128<double> Floor(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_floor_sd (__m128d a)
        /// </summary>
        public static Vector128<double> FloorScalar(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_floor_ss (__m128 a)
        /// </summary>
        public static Vector128<float> FloorScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_insert_epi8 (__m128i a, int i, const int imm8)
        /// </summary>
        public static Vector128<sbyte> Insert(Vector128<sbyte> value, sbyte data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_insert_epi8 (__m128i a, int i, const int imm8)
        /// </summary>
        public static Vector128<byte> Insert(Vector128<byte> value, byte data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_insert_epi32 (__m128i a, int i, const int imm8)
        /// </summary>
        public static Vector128<int> Insert(Vector128<int> value, int data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_insert_epi32 (__m128i a, int i, const int imm8)
        /// </summary>
        public static Vector128<uint> Insert(Vector128<uint> value, uint data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_insert_epi64 (__m128i a, __int64 i, const int imm8)
        /// </summary>
        public static Vector128<long> Insert(Vector128<long> value, long data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_insert_epi64 (__m128i a, __int64 i, const int imm8)
        /// </summary>
        public static Vector128<ulong> Insert(Vector128<ulong> value, ulong data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_insert_ps (__m128 a, __m128 b, const int imm8)
        /// </summary>
        public static Vector128<float> Insert(Vector128<float> value, float data, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_max_epi8 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<sbyte> Max(Vector128<sbyte> left,  Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_max_epu16 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<ushort> Max(Vector128<ushort> left,  Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_max_epi32 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<int> Max(Vector128<int> left,  Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_max_epu32 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<uint> Max(Vector128<uint> left,  Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_min_epi8 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<sbyte> Min(Vector128<sbyte> left,  Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_min_epu16 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<ushort> Min(Vector128<ushort> left,  Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_min_epi32 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<int> Min(Vector128<int> left,  Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_min_epu32 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<uint> Min(Vector128<uint> left,  Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m128i _mm_minpos_epu16 (__m128i a)
        /// </summary>
        public static Vector128<ushort> MinHorizontal(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_mpsadbw_epu8 (__m128i a, __m128i b, const int imm8)
        /// </summary>
        public static Vector128<ushort> MultipleSumAbsoluteDifferences(Vector128<byte> left, Vector128<byte> right, byte mask) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m128i _mm_mul_epi32 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<long> Multiply(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_mullo_epi32 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<int> MultiplyLow(Vector128<int> left,  Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_packus_epi32 (__m128i a, __m128i b)
        /// </summary>
        public static Vector128<ushort> PackUnsignedSaturate(Vector128<int> left,  Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_round_ps (__m128 a, int rounding)
        /// _MM_FROUND_TO_NEAREST_INT |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<float> RoundToNearestInteger(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _MM_FROUND_TO_NEG_INF |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<float> RoundToNegativeInfinity(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _MM_FROUND_TO_POS_INF |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<float> RoundToPositiveInfinity(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _MM_FROUND_TO_ZERO |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<float> RoundToZero(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _MM_FROUND_CUR_DIRECTION
        /// </summary>
        public static Vector128<float> RoundCurrentDirection(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_round_pd (__m128d a, int rounding)
        /// _MM_FROUND_TO_NEAREST_INT |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<double> RoundToNearestInteger(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _MM_FROUND_TO_NEG_INF |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<double> RoundToNegativeInfinity(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _MM_FROUND_TO_POS_INF |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<double> RoundToPositiveInfinity(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _MM_FROUND_TO_ZERO |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<double> RoundToZero(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _MM_FROUND_CUR_DIRECTION
        /// </summary>
        public static Vector128<double> RoundCurrentDirection(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// _MM_FROUND_CUR_DIRECTION
        /// </summary>
        public static Vector128<double> RoundCurrentDirectionScalar(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_round_sd (__m128d a, int rounding)
        /// _MM_FROUND_TO_NEAREST_INT |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<double> RoundToNearestIntegerScalar(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _MM_FROUND_TO_NEG_INF |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<double> RoundToNegativeInfinityScalar(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _MM_FROUND_TO_POS_INF |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<double> RoundToPositiveInfinityScalar(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _MM_FROUND_TO_ZERO |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<double> RoundToZeroScalar(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// _MM_FROUND_CUR_DIRECTION
        /// </summary>
        public static Vector128<float> RoundCurrentDirectionScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_round_ss (__m128 a, int rounding)
        /// _MM_FROUND_TO_NEAREST_INT |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<float> RoundToNearestIntegerScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _MM_FROUND_TO_NEG_INF |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<float> RoundToNegativeInfinityScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _MM_FROUND_TO_POS_INF |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<float> RoundToPositiveInfinityScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _MM_FROUND_TO_ZERO |_MM_FROUND_NO_EXC
        /// </summary>
        public static Vector128<float> RoundToZeroScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_stream_load_si128 (const __m128i* mem_addr)
        /// </summary>
        public static unsafe Vector128<sbyte> LoadAlignedNonTemporal(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_stream_load_si128 (const __m128i* mem_addr)
        /// </summary>
        public static unsafe Vector128<byte> LoadAlignedNonTemporal(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_stream_load_si128 (const __m128i* mem_addr)
        /// </summary>
        public static unsafe Vector128<short> LoadAlignedNonTemporal(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_stream_load_si128 (const __m128i* mem_addr)
        /// </summary>
        public static unsafe Vector128<ushort> LoadAlignedNonTemporal(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_stream_load_si128 (const __m128i* mem_addr)
        /// </summary>
        public static unsafe Vector128<int> LoadAlignedNonTemporal(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_stream_load_si128 (const __m128i* mem_addr)
        /// </summary>
        public static unsafe Vector128<uint> LoadAlignedNonTemporal(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_stream_load_si128 (const __m128i* mem_addr)
        /// </summary>
        public static unsafe Vector128<long> LoadAlignedNonTemporal(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_stream_load_si128 (const __m128i* mem_addr)
        /// </summary>
        public static unsafe Vector128<ulong> LoadAlignedNonTemporal(ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_test_all_ones (__m128i a)
        /// </summary>
        public static bool TestAllOnes(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        public static bool TestAllOnes(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        public static bool TestAllOnes(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        public static bool TestAllOnes(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        public static bool TestAllOnes(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        public static bool TestAllOnes(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        public static bool TestAllOnes(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        public static bool TestAllOnes(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// int _mm_test_all_zeros (__m128i a, __m128i mask)
        /// </summary>
        public static bool TestAllZeros(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        public static bool TestAllZeros(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        public static bool TestAllZeros(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        public static bool TestAllZeros(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static bool TestAllZeros(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        public static bool TestAllZeros(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        public static bool TestAllZeros(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        public static bool TestAllZeros(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_testc_si128 (__m128i a, __m128i b)
        /// </summary>
        public static bool TestC(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        public static bool TestC(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        public static bool TestC(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        public static bool TestC(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static bool TestC(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        public static bool TestC(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        public static bool TestC(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        public static bool TestC(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// int _mm_test_mix_ones_zeros (__m128i a, __m128i mask)
        /// </summary>
        public static bool TestMixOnesZeros(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        public static bool TestMixOnesZeros(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        public static bool TestMixOnesZeros(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        public static bool TestMixOnesZeros(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static bool TestMixOnesZeros(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        public static bool TestMixOnesZeros(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        public static bool TestMixOnesZeros(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        public static bool TestMixOnesZeros(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_testnzc_si128 (__m128i a, __m128i b)
        /// </summary>
        public static bool TestNotZAndNotC(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        public static bool TestNotZAndNotC(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        public static bool TestNotZAndNotC(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        public static bool TestNotZAndNotC(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static bool TestNotZAndNotC(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        public static bool TestNotZAndNotC(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        public static bool TestNotZAndNotC(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        public static bool TestNotZAndNotC(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// int _mm_testz_si128 (__m128i a, __m128i b)
        /// </summary>
        public static bool TestZ(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        public static bool TestZ(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        public static bool TestZ(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        public static bool TestZ(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        public static bool TestZ(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        public static bool TestZ(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        public static bool TestZ(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        public static bool TestZ(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
    }
}
