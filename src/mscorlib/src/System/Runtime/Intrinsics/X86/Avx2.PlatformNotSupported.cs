// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel AVX2 hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public static class Avx2
    {
        public static bool IsSupported { get { return false; } }
        
        /// <summary>
        /// __m256i _mm256_abs_epi8 (__m256i a)
        /// </summary>
        public static Vector256<byte> Abs(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_abs_epi16 (__m256i a)
        /// </summary>
        public static Vector256<ushort> Abs(Vector256<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_abs_epi32 (__m256i a)
        /// </summary>
        public static Vector256<uint> Abs(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_add_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> Add(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_add_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> Add(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_add_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> Add(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_add_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> Add(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_add_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> Add(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_add_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<uint> Add(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_add_epi64 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<long> Add(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_add_epi64 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ulong> Add(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_adds_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> AddSaturate(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_adds_epu8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> AddSaturate(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_adds_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> AddSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_adds_epu16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> AddSaturate(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)
        /// </summary>
        public static Vector256<sbyte> AlignRight(Vector256<sbyte> left, Vector256<sbyte> right, byte mask) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> And(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> And(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> And(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> And(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> And(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<uint> And(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<long> And(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ulong> And(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> AndNot(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> AndNot(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> AndNot(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> AndNot(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> AndNot(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<uint> AndNot(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<long> AndNot(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ulong> AndNot(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_avg_epu8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> Average(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_avg_epu16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> Average(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_blend_epi32 (__m128i a, __m128i b, const int imm8)
        /// </summary>
        public static Vector128<int> Blend(Vector128<int> left, Vector128<int> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_blend_epi32 (__m128i a, __m128i b, const int imm8)
        /// </summary>
        public static Vector128<uint> Blend(Vector128<uint> left, Vector128<uint> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_blend_epi16 (__m256i a, __m256i b, const int imm8)
        /// </summary>
        public static Vector256<short> Blend(Vector256<short> left, Vector256<short> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_blend_epi16 (__m256i a, __m256i b, const int imm8)
        /// </summary>
        public static Vector256<ushort> Blend(Vector256<ushort> left, Vector256<ushort> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_blend_epi32 (__m256i a, __m256i b, const int imm8)
        /// </summary>
        public static Vector256<int> Blend(Vector256<int> left, Vector256<int> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_blend_epi32 (__m256i a, __m256i b, const int imm8)
        /// </summary>
        public static Vector256<uint> Blend(Vector256<uint> left, Vector256<uint> right, byte control) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)
        /// </summary>
        public static Vector256<sbyte> BlendVariable(Vector256<sbyte> left, Vector256<sbyte> right, Vector256<sbyte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)
        /// </summary>
        public static Vector256<byte> BlendVariable(Vector256<byte> left, Vector256<byte> right, Vector256<byte> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_broadcastb_epi8 (__m128i a)
        /// __m128i _mm_broadcastw_epi16 (__m128i a)
        /// __m128i _mm_broadcastd_epi32 (__m128i a)
        /// __m128i _mm_broadcastq_epi64 (__m128i a)
        /// __m128 _mm_broadcastss_ps (__m128 a)
        /// __m128d _mm_broadcastsd_pd (__m128d a)
        /// </summary>
        public static Vector128<T> BroadcastElementToVector128<T>(Vector128<T> value) where T : struct { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastb_epi8 (__m128i a)
        /// __m256i _mm256_broadcastw_epi16 (__m128i a)
        /// __m256i _mm256_broadcastd_epi32 (__m128i a)
        /// __m256i _mm256_broadcastq_epi64 (__m128i a)
        /// __m256 _mm256_broadcastss_ps (__m128 a)
        /// __m256d _mm256_broadcastsd_pd (__m128d a)
        /// </summary>
        public static Vector256<T> BroadcastElementToVector256<T>(Vector128<T> value) where T : struct { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        /// </summary>
        public static unsafe Vector256<sbyte> BroadcastVector128ToVector256(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        /// </summary>
        public static unsafe Vector256<byte> BroadcastVector128ToVector256(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        /// </summary>
        public static unsafe Vector256<short> BroadcastVector128ToVector256(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        /// </summary>
        public static unsafe Vector256<ushort> BroadcastVector128ToVector256(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        /// </summary>
        public static unsafe Vector256<int> BroadcastVector128ToVector256(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        /// </summary>
        public static unsafe Vector256<uint> BroadcastVector128ToVector256(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        /// </summary>
        public static unsafe Vector256<long> BroadcastVector128ToVector256(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        /// </summary>
        public static unsafe Vector256<ulong> BroadcastVector128ToVector256(ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpeq_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> CompareEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpeq_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> CompareEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpeq_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> CompareEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpeq_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> CompareEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpeq_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> CompareEqual(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpeq_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<uint> CompareEqual(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpeq_epi64 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<long> CompareEqual(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpeq_epi64 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ulong> CompareEqual(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m256i _mm256_cmpgt_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> CompareGreaterThan(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpgt_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> CompareGreaterThan(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpgt_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> CompareGreaterThan(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpgt_epi64 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<long> CompareGreaterThan(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cvtepi8_epi16 (__m128i a)
        /// </summary>
        public static Vector256<short> ConvertToVector256Short(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepu8_epi16 (__m128i a)
        /// </summary>
        public static Vector256<ushort> ConvertToVector256UShort(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepi8_epi32 (__m128i a)
        /// </summary>
        public static Vector256<int> ConvertToVector256Int(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepi16_epi32 (__m128i a)
        /// </summary>
        public static Vector256<int> ConvertToVector256Int(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepu8_epi32 (__m128i a)
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepu16_epi32 (__m128i a)
        /// </summary>
        public static Vector256<uint> ConvertToVector256UInt(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepi8_epi64 (__m128i a)
        /// </summary>
        public static Vector256<long> ConvertToVector256Long(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepi16_epi64 (__m128i a)
        /// </summary>
        public static Vector256<long> ConvertToVector256Long(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepi32_epi64 (__m128i a)
        /// </summary>
        public static Vector256<long> ConvertToVector256Long(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepu8_epi64 (__m128i a)
        /// </summary>
        public static Vector256<ulong> ConvertToVector256ULong(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepu16_epi64 (__m128i a)
        /// </summary>
        public static Vector256<ulong> ConvertToVector256ULong(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepu32_epi64 (__m128i a)
        /// </summary>
        public static Vector256<ulong> ConvertToVector256ULong(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static Vector128<sbyte> ExtractVector128(Vector256<sbyte> value, byte index) { throw new PlatformNotSupportedException(); }
        // <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static unsafe void ExtractVector128(sbyte* address, Vector256<sbyte> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static Vector128<byte> ExtractVector128(Vector256<byte> value, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static unsafe void ExtractVector128(byte* address, Vector256<byte> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static Vector128<short> ExtractVector128(Vector256<short> value, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static unsafe void ExtractVector128(short* address, Vector256<short> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static Vector128<ushort> ExtractVector128(Vector256<ushort> value, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static unsafe void ExtractVector128(ushort* address, Vector256<ushort> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static Vector128<int> ExtractVector128(Vector256<int> value, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static unsafe void ExtractVector128(int* address, Vector256<int> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static Vector128<uint> ExtractVector128(Vector256<uint> value, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static unsafe void ExtractVector128(uint* address, Vector256<uint> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static Vector128<long> ExtractVector128(Vector256<long> value, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static unsafe void ExtractVector128(long* address, Vector256<long> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static Vector128<ulong> ExtractVector128(Vector256<ulong> value, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        /// </summary>
        public static unsafe void ExtractVector128(ulong* address, Vector256<ulong> value, byte index) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m128i _mm_i32gather_epi32 (int const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_i32gather_epi32 (int const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_i32gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<long> GatherVector128(long* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_i32gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<ulong> GatherVector128(ulong* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_i32gather_ps (float const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_i32gather_pd (double const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<double> GatherVector128(double* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_i64gather_epi32 (int const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector128<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_i64gather_epi32 (int const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector128<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_i64gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<long> GatherVector128(long* baseAddress, Vector128<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_i64gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<ulong> GatherVector128(ulong* baseAddress, Vector128<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_i64gather_ps (float const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector128<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_i64gather_pd (double const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<double> GatherVector128(double* baseAddress, Vector128<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_i32gather_epi32 (int const* base_addr, __m256i vindex, const int scale)
        /// </summary>
        public static unsafe Vector256<int> GatherVector256(int* baseAddress, Vector256<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_i32gather_epi32 (int const* base_addr, __m256i vindex, const int scale)
        /// </summary>
        public static unsafe Vector256<uint> GatherVector256(uint* baseAddress, Vector256<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_i32gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector256<long> GatherVector256(long* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_i32gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector256<ulong> GatherVector256(ulong* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256 _mm256_i32gather_ps (float const* base_addr, __m256i vindex, const int scale)
        /// </summary>
        public static unsafe Vector256<float> GatherVector256(float* baseAddress, Vector256<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_i32gather_pd (double const* base_addr, __m128i vindex, const int scale)
        /// </summary>
        public static unsafe Vector256<double> GatherVector256(double* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_i64gather_epi32 (int const* base_addr, __m256i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector256<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_i64gather_epi32 (int const* base_addr, __m256i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector256<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_i64gather_epi64 (__int64 const* base_addr, __m256i vindex, const int scale)
        /// </summary>
        public static unsafe Vector256<long> GatherVector256(long* baseAddress, Vector256<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_i64gather_epi64 (__int64 const* base_addr, __m256i vindex, const int scale)
        /// </summary>
        public static unsafe Vector256<ulong> GatherVector256(ulong* baseAddress, Vector256<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm256_i64gather_ps (float const* base_addr, __m256i vindex, const int scale)
        /// </summary>
        public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector256<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_i64gather_pd (double const* base_addr, __m256i vindex, const int scale)
        /// </summary>
        public static unsafe Vector256<double> GatherVector256(double* baseAddress, Vector256<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m128i _mm_mask_i32gather_epi32 (__m128i src, int const* base_addr, __m128i vindex, __m128i mask, const int scale)
        /// </summary>
        public static unsafe Vector128<int> GatherMaskVector128(Vector128<int> source, int* baseAddress, Vector128<int> index, Vector128<int> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mask_i32gather_epi32 (__m128i src, int const* base_addr, __m128i vindex, __m128i mask, const int scale)
        /// </summary>
        public static unsafe Vector128<uint> GatherMaskVector128(Vector128<uint> source, uint* baseAddress, Vector128<int> index, Vector128<uint> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mask_i32gather_epi64 (__m128i src, __int64 const* base_addr, __m128i vindex, __m128i mask, const int scale)
        /// </summary>
        public static unsafe Vector128<long> GatherMaskVector128(Vector128<long> source, long* baseAddress, Vector128<int> index, Vector128<long> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mask_i32gather_epi64 (__m128i src, __int64 const* base_addr, __m128i vindex, __m128i mask, const int scale)
        /// </summary>
        public static unsafe Vector128<ulong> GatherMaskVector128(Vector128<ulong> source, ulong* baseAddress, Vector128<int> index, Vector128<ulong> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_mask_i32gather_ps (__m128 src, float const* base_addr, __m128i vindex, __m128 mask, const int scale)
        /// </summary>
        public static unsafe Vector128<float> GatherMaskVector128(Vector128<float> source, float* baseAddress, Vector128<int> index, Vector128<float> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_mask_i32gather_pd (__m128d src, double const* base_addr, __m128i vindex, __m128d mask, const int scale)
        /// </summary>
        public static unsafe Vector128<double> GatherMaskVector128(Vector128<double> source, double* baseAddress, Vector128<int> index, Vector128<double> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mask_i64gather_epi32 (__m128i src, int const* base_addr, __m128i vindex, __m128i mask, const int scale)
        /// </summary>
        public static unsafe Vector128<int> GatherMaskVector128(Vector128<int> source, int* baseAddress, Vector128<long> index, Vector128<int> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mask_i64gather_epi32 (__m128i src, int const* base_addr, __m128i vindex, __m128i mask, const int scale)
        /// </summary>
        public static unsafe Vector128<uint> GatherMaskVector128(Vector128<uint> source, uint* baseAddress, Vector128<long> index, Vector128<uint> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mask_i64gather_epi64 (__m128i src, __int64 const* base_addr, __m128i vindex, __m128i mask, const int scale)
        /// </summary>
        public static unsafe Vector128<long> GatherMaskVector128(Vector128<long> source, long* baseAddress, Vector128<long> index, Vector128<long> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mask_i64gather_epi64 (__m128i src, __int64 const* base_addr, __m128i vindex, __m128i mask, const int scale)
        /// </summary>
        public static unsafe Vector128<ulong> GatherMaskVector128(Vector128<ulong> source, ulong* baseAddress, Vector128<long> index, Vector128<ulong> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_mask_i64gather_ps (__m128 src, float const* base_addr, __m128i vindex, __m128 mask, const int scale)
        /// </summary>
        public static unsafe Vector128<float> GatherMaskVector128(Vector128<float> source, float* baseAddress, Vector128<long> index, Vector128<float> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_mask_i64gather_pd (__m128d src, double const* base_addr, __m128i vindex, __m128d mask, const int scale)
        /// </summary>
        public static unsafe Vector128<double> GatherMaskVector128(Vector128<double> source, double* baseAddress, Vector128<long> index, Vector128<double> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mask_i32gather_epi32 (__m256i src, int const* base_addr, __m256i vindex, __m256i mask, const int scale)
        /// </summary>
        public static unsafe Vector256<int> GatherMaskVector256(Vector256<int> source, int* baseAddress, Vector256<int> index, Vector256<int> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mask_i32gather_epi32 (__m256i src, int const* base_addr, __m256i vindex, __m256i mask, const int scale)
        /// </summary>
        public static unsafe Vector256<uint> GatherMaskVector256(Vector256<uint> source, uint* baseAddress, Vector256<int> index, Vector256<uint> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mask_i32gather_epi64 (__m256i src, __int64 const* base_addr, __m128i vindex, __m256i mask, const int scale)
        /// </summary>
        public static unsafe Vector256<long> GatherMaskVector256(Vector256<long> source, long* baseAddress, Vector128<int> index, Vector256<long> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mask_i32gather_epi64 (__m256i src, __int64 const* base_addr, __m128i vindex, __m256i mask, const int scale)
        /// </summary>
        public static unsafe Vector256<ulong> GatherMaskVector256(Vector256<ulong> source, ulong* baseAddress, Vector128<int> index, Vector256<ulong> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256 _mm256_mask_i32gather_ps (__m256 src, float const* base_addr, __m256i vindex, __m256 mask, const int scale)
        /// </summary>
        public static unsafe Vector256<float> GatherMaskVector256(Vector256<float> source, float* baseAddress, Vector256<int> index, Vector256<float> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_mask_i32gather_pd (__m256d src, double const* base_addr, __m128i vindex, __m256d mask, const int scale)
        /// </summary>
        public static unsafe Vector256<double> GatherMaskVector256(Vector256<double> source, double* baseAddress, Vector128<int> index, Vector256<double> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_mask_i64gather_epi32 (__m128i src, int const* base_addr, __m256i vindex, __m128i mask, const int scale)
        /// </summary>
        public static unsafe Vector128<int> GatherMaskVector128(Vector128<int> source, int* baseAddress, Vector256<long> index, Vector128<int> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_mask_i64gather_epi32 (__m128i src, int const* base_addr, __m256i vindex, __m128i mask, const int scale)
        /// </summary>
        public static unsafe Vector128<uint> GatherMaskVector128(Vector128<uint> source, uint* baseAddress, Vector256<long> index, Vector128<uint> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mask_i64gather_epi64 (__m256i src, __int64 const* base_addr, __m256i vindex, __m256i mask, const int scale)
        /// </summary>
        public static unsafe Vector256<long> GatherMaskVector256(Vector256<long> source, long* baseAddress, Vector256<long> index, Vector256<long> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mask_i64gather_epi64 (__m256i src, __int64 const* base_addr, __m256i vindex, __m256i mask, const int scale)
        /// </summary>
        public static unsafe Vector256<ulong> GatherMaskVector256(Vector256<ulong> source, ulong* baseAddress, Vector256<long> index, Vector256<ulong> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm256_mask_i64gather_ps (__m128 src, float const* base_addr, __m256i vindex, __m128 mask, const int scale)
        /// </summary>
        public static unsafe Vector128<float> GatherMaskVector128(Vector128<float> source, float* baseAddress, Vector256<long> index, Vector128<float> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_mask_i64gather_pd (__m256d src, double const* base_addr, __m256i vindex, __m256d mask, const int scale)
        /// </summary>
        public static unsafe Vector256<double> GatherMaskVector256(Vector256<double> source, double* baseAddress, Vector256<long> index, Vector256<double> mask, byte scale) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m256i _mm256_hadd_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> HorizontalAdd(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_hadd_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> HorizontalAdd(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_hadds_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> HorizontalAddSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_hsub_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> HorizontalSubtract(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_hsub_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> HorizontalSubtract(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_hsubs_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> HorizontalSubtractSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static Vector256<sbyte> Insert(Vector256<sbyte> value, Vector128<sbyte> data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static unsafe Vector256<sbyte> Insert(Vector256<sbyte> value, sbyte* address, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static Vector256<byte> Insert(Vector256<byte> value, Vector128<byte> data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static unsafe Vector256<byte> Insert(Vector256<byte> value, byte* address, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static Vector256<short> Insert(Vector256<short> value, Vector128<short> data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static unsafe Vector256<short> Insert(Vector256<short> value, short* address, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static Vector256<ushort> Insert(Vector256<ushort> value, Vector128<ushort> data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static unsafe Vector256<ushort> Insert(Vector256<ushort> value, ushort* address, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static Vector256<int> Insert(Vector256<int> value, Vector128<int> data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static unsafe Vector256<int> Insert(Vector256<int> value, int* address, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static Vector256<uint> Insert(Vector256<uint> value, Vector128<uint> data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static unsafe Vector256<uint> Insert(Vector256<uint> value, uint* address, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static Vector256<long> Insert(Vector256<long> value, Vector128<long> data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static unsafe Vector256<long> Insert(Vector256<long> value, long* address, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static Vector256<ulong> Insert(Vector256<ulong> value, Vector128<ulong> data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        /// </summary>
        public static unsafe Vector256<ulong> Insert(Vector256<ulong> value, ulong* address, byte index) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m128i _mm_maskload_epi32 (int const* mem_addr, __m128i mask)
        /// </summary>
        public static unsafe Vector128<int> MaskLoad(int* address, Vector128<int> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_maskload_epi32 (int const* mem_addr, __m128i mask)
        /// </summary>
        public static unsafe Vector128<uint> MaskLoad(uint* address, Vector128<uint> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_maskload_epi64 (__int64 const* mem_addr, __m128i mask)
        /// </summary>
        public static unsafe Vector128<long> MaskLoad(long* address, Vector128<long> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_maskload_epi64 (__int64 const* mem_addr, __m128i mask)
        /// </summary>
        public static unsafe Vector128<ulong> MaskLoad(ulong* address, Vector128<ulong> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_maskload_epi32 (int const* mem_addr, __m256i mask)
        /// </summary>
        public static unsafe Vector256<int> MaskLoad(int* address, Vector256<int> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_maskload_epi32 (int const* mem_addr, __m256i mask)
        /// </summary>
        public static unsafe Vector256<uint> MaskLoad(uint* address, Vector256<uint> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_maskload_epi64 (__int64 const* mem_addr, __m256i mask)
        /// </summary>
        public static unsafe Vector256<long> MaskLoad(long* address, Vector256<long> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_maskload_epi64 (__int64 const* mem_addr, __m256i mask)
        /// </summary>
        public static unsafe Vector256<ulong> MaskLoad(ulong* address, Vector256<ulong> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_maskstore_epi32 (int* mem_addr, __m128i mask, __m128i a)
        /// </summary>
        public static unsafe void MaskStore(int* address, Vector128<int> mask, Vector128<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_maskstore_epi32 (int* mem_addr, __m128i mask, __m128i a)
        /// </summary>
        public static unsafe void MaskStore(uint* address, Vector128<uint> mask, Vector128<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_maskstore_epi64 (__int64* mem_addr, __m128i mask, __m128i a)
        /// </summary>
        public static unsafe void MaskStore(long* address, Vector128<long> mask, Vector128<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_maskstore_epi64 (__int64* mem_addr, __m128i mask, __m128i a)
        /// </summary>
        public static unsafe void MaskStore(ulong* address, Vector128<ulong> mask, Vector128<ulong> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm256_maskstore_epi32 (int* mem_addr, __m256i mask, __m256i a)
        /// </summary>
        public static unsafe void MaskStore(int* address, Vector256<int> mask, Vector256<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm256_maskstore_epi32 (int* mem_addr, __m256i mask, __m256i a)
        /// </summary>
        public static unsafe void MaskStore(uint* address, Vector256<uint> mask, Vector256<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm256_maskstore_epi64 (__int64* mem_addr, __m256i mask, __m256i a)
        /// </summary>
        public static unsafe void MaskStore(long* address, Vector256<long> mask, Vector256<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm256_maskstore_epi64 (__int64* mem_addr, __m256i mask, __m256i a)
        /// </summary>
        public static unsafe void MaskStore(ulong* address, Vector256<ulong> mask, Vector256<ulong> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_madd_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> MultiplyAddAdjacent(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_maddubs_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> MultiplyAddAdjacent(Vector256<byte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_max_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> Max(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_max_epu8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> Max(Vector256<byte> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_max_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> Max(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_max_epu16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> Max(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_max_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> Max(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_max_epu32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<uint> Max(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m256i _mm256_min_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> Min(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_min_epu8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> Min(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_min_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> Min(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_min_epu16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> Min(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_min_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> Min(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_min_epu32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<uint> Min(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// int _mm256_movemask_epi8 (__m256i a)
        /// </summary>
        public static int MoveMask(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm256_movemask_epi8 (__m256i a)
        /// </summary>
        public static int MoveMask(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_mpsadbw_epu8 (__m256i a, __m256i b, const int imm8)
        /// </summary>
        public static Vector256<ushort> MultipleSumAbsoluteDifferences(Vector256<byte> left, Vector256<byte> right, byte mask) { throw new PlatformNotSupportedException(); }
                
        /// <summary>
        /// __m256i _mm256_mul_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<long> Multiply(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mul_epu32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ulong> Multiply(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_mulhi_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> MultiplyHigh(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mulhi_epu16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> MultiplyHigh(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_mulhrs_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> MultiplyHighRoundScale(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_mullo_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> MultiplyLow(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mullo_epu16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> MultiplyLow(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> Or(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> Or(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> Or(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> Or(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> Or(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<uint> Or(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<long> Or(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ulong> Or(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_packs_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> PackSignedSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_packs_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> PackSignedSaturate(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_packus_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> PackUnsignedSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_packus_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> PackUnsignedSaturate(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        /// </summary>
        public static Vector256<sbyte> Permute2x128(Vector256<sbyte> left, Vector256<sbyte> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        /// </summary>
        public static Vector256<byte> Permute2x128(Vector256<byte> left, Vector256<byte> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        /// </summary>
        public static Vector256<short> Permute2x128(Vector256<short> left, Vector256<short> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        /// </summary>
        public static Vector256<ushort> Permute2x128(Vector256<ushort> left, Vector256<ushort> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        /// </summary>
        public static Vector256<int> Permute2x128(Vector256<int> left, Vector256<int> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        /// </summary>
        public static Vector256<uint> Permute2x128(Vector256<uint> left, Vector256<uint> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        /// </summary>
        public static Vector256<long> Permute2x128(Vector256<long> left, Vector256<long> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        /// </summary>
        public static Vector256<ulong> Permute2x128(Vector256<ulong> left, Vector256<ulong> right, byte control) { throw new PlatformNotSupportedException(); }
                
        /// <summary>
        /// __m256i _mm256_permute4x64_epi64 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<long> Permute4x64(Vector256<long> value, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute4x64_epi64 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<ulong> Permute4x64(Vector256<ulong> value, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_permute4x64_pd (__m256d a, const int imm8)
        /// </summary>
        public static Vector256<double> Permute4x64(Vector256<double> value, byte control) { throw new PlatformNotSupportedException(); }
                
        /// <summary>
        /// __m256i _mm256_permutevar8x32_epi32 (__m256i a, __m256i idx)
        /// </summary>
        public static Vector256<int> PermuteVar8x32(Vector256<int> left, Vector256<int> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permutevar8x32_epi32 (__m256i a, __m256i idx)
        /// </summary>
        public static Vector256<uint> PermuteVar8x32(Vector256<uint> left, Vector256<uint> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256 _mm256_permutevar8x32_ps (__m256 a, __m256i idx)
        /// </summary>
        public static Vector256<float> PermuteVar8x32(Vector256<float> left, Vector256<float> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_slli_epi16 (__m256i a, int imm8)
        /// </summary>
        public static Vector256<short> ShiftLeftLogical(Vector256<short> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_slli_epi16 (__m256i a, int imm8)
        /// </summary>
        public static Vector256<ushort> ShiftLeftLogical(Vector256<ushort> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_slli_epi32 (__m256i a, int imm8)
        /// </summary>
        public static Vector256<int> ShiftLeftLogical(Vector256<int> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_slli_epi32 (__m256i a, int imm8)
        /// </summary>
        public static Vector256<uint> ShiftLeftLogical(Vector256<uint> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_slli_epi64 (__m256i a, int imm8)
        /// </summary>
        public static Vector256<long> ShiftLeftLogical(Vector256<long> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_slli_epi64 (__m256i a, int imm8)
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogical(Vector256<ulong> value, byte count) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<sbyte> ShiftLeftLogical128BitLane(Vector256<sbyte> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<byte> ShiftLeftLogical128BitLane(Vector256<byte> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<short> ShiftLeftLogical128BitLane(Vector256<short> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<ushort> ShiftLeftLogical128BitLane(Vector256<ushort> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<int> ShiftLeftLogical128BitLane(Vector256<int> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<uint> ShiftLeftLogical128BitLane(Vector256<uint> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<long> ShiftLeftLogical128BitLane(Vector256<long> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogical128BitLane(Vector256<ulong> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m256i _mm256_sllv_epi32 (__m256i a, __m256i count)
        /// </summary>
        public static Vector256<int> ShiftLeftLogicalVariable(Vector256<int> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sllv_epi32 (__m256i a, __m256i count)
        /// </summary>
        public static Vector256<uint> ShiftLeftLogicalVariable(Vector256<uint> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sllv_epi64 (__m256i a, __m256i count)
        /// </summary>
        public static Vector256<long> ShiftLeftLogicalVariable(Vector256<long> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sllv_epi64 (__m256i a, __m256i count)
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogicalVariable(Vector256<ulong> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m256i _mm256_srai_epi16 (__m256i a, int imm8)
        /// </summary>
        public static Vector256<short> ShiftRightArithmetic(Vector256<short> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srai_epi32 (__m256i a, int imm8)
        /// </summary>
        public static Vector256<int> ShiftRightArithmetic(Vector256<int> value, byte count) { throw new PlatformNotSupportedException(); }
                
        /// <summary>
        /// __m256i _mm256_srav_epi32 (__m256i a, __m256i count)
        /// </summary>
        public static Vector256<int> ShiftRightArithmeticVariable(Vector256<int> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_srli_epi16 (__m256i a, int imm8)
        /// </summary>
        public static Vector256<short> ShiftRightLogical(Vector256<short> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srli_epi16 (__m256i a, int imm8)
        /// </summary>
        public static Vector256<ushort> ShiftRightLogical(Vector256<ushort> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srli_epi32 (__m256i a, int imm8)
        /// </summary>
        public static Vector256<int> ShiftRightLogical(Vector256<int> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srli_epi32 (__m256i a, int imm8)
        /// </summary>
        public static Vector256<uint> ShiftRightLogical(Vector256<uint> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srli_epi64 (__m256i a, int imm8)
        /// </summary>
        public static Vector256<long> ShiftRightLogical(Vector256<long> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srli_epi64 (__m256i a, int imm8)
        /// </summary>
        public static Vector256<ulong> ShiftRightLogical(Vector256<ulong> value, byte count) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogical128BitLane(Vector256<sbyte> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<byte> ShiftRightLogical128BitLane(Vector256<byte> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<short> ShiftRightLogical128BitLane(Vector256<short> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<ushort> ShiftRightLogical128BitLane(Vector256<ushort> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<int> ShiftRightLogical128BitLane(Vector256<int> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<uint> ShiftRightLogical128BitLane(Vector256<uint> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<long> ShiftRightLogical128BitLane(Vector256<long> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<ulong> ShiftRightLogical128BitLane(Vector256<ulong> value, byte numBytes) { throw new PlatformNotSupportedException(); }
                
        /// <summary>
        /// __m256i _mm256_srlv_epi32 (__m256i a, __m256i count)
        /// </summary>
        public static Vector256<int> ShiftRightLogicalVariable(Vector256<int> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srlv_epi32 (__m256i a, __m256i count)
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalVariable(Vector256<uint> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srlv_epi64 (__m256i a, __m256i count)
        /// </summary>
        public static Vector256<long> ShiftRightLogicalVariable(Vector256<long> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srlv_epi64 (__m256i a, __m256i count)
        /// </summary>
        public static Vector256<ulong> ShiftRightLogicalVariable(Vector256<ulong> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m256i _mm256_shuffle_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> Shuffle(Vector256<sbyte> value, Vector256<sbyte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_shuffle_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> Shuffle(Vector256<byte> value, Vector256<byte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_shuffle_epi32 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<int> Shuffle(Vector256<int> value, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_shuffle_epi32 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<uint> Shuffle(Vector256<uint> value, byte control) { throw new PlatformNotSupportedException(); }
         
        /// <summary>
        /// __m256i _mm256_shufflehi_epi16 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<short> ShuffleHigh(Vector256<short> value, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_shufflehi_epi16 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<ushort> ShuffleHigh(Vector256<ushort> value, byte control) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m256i _mm256_shufflelo_epi16 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<short> ShuffleLow(Vector256<short> value, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_shufflelo_epi16 (__m256i a, const int imm8)
        /// </summary>
        public static Vector256<ushort> ShuffleLow(Vector256<ushort> value, byte control) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m256i _mm256_sign_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> Sign(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sign_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> Sign(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sign_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> Sign(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
    
        /// <summary>
        /// __m256i _mm256_sub_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> Subtract(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sub_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> Subtract(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sub_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> Subtract(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sub_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> Subtract(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sub_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> Subtract(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sub_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<uint> Subtract(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sub_epi64 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<long> Subtract(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sub_epi64 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ulong> Subtract(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_subs_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> SubtractSaturate(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_subs_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> SubtractSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_subs_epu8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> SubtractSaturate(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_subs_epu16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> SubtractSaturate(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_sad_epu8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ulong> SumAbsoluteDifferences(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_unpackhi_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> UnpackHigh(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpackhi_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> UnpackHigh(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpackhi_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> UnpackHigh(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpackhi_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> UnpackHigh(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpackhi_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> UnpackHigh(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpackhi_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<uint> UnpackHigh(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpackhi_epi64 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<long> UnpackHigh(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpackhi_epi64 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ulong> UnpackHigh(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m256i _mm256_unpacklo_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> UnpackLow(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpacklo_epi8 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> UnpackLow(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpacklo_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> UnpackLow(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpacklo_epi16 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> UnpackLow(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpacklo_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> UnpackLow(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpacklo_epi32 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<uint> UnpackLow(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpacklo_epi64 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<long> UnpackLow(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpacklo_epi64 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ulong> UnpackLow(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<sbyte> Xor(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<byte> Xor(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<short> Xor(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ushort> Xor(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<int> Xor(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<uint> Xor(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<long> Xor(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        /// </summary>
        public static Vector256<ulong> Xor(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }
    }
}
