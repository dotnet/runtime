// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 AVX2 hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Avx2 : Avx
    {
        internal Avx2() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>Provides access to the x86 AVX2 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class X64 : Avx.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        ///   <para>__m256i _mm256_abs_epi8 (__m256i a)</para>
        ///   <para>  VPABSB ymm1,         ymm2/m256</para>
        ///   <para>  VPABSB ymm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector256<byte> Abs(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_abs_epi16 (__m256i a)</para>
        ///   <para>  VPABSW ymm1,         ymm2/m256</para>
        ///   <para>  VPABSW ymm1 {k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector256<ushort> Abs(Vector256<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_abs_epi32 (__m256i a)</para>
        ///   <para>  VPABSD ymm1,         ymm2/m256</para>
        ///   <para>  VPABSD ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> Abs(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_add_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPADDB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPADDB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> Add(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_add_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPADDB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPADDB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> Add(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_add_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPADDW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPADDW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> Add(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_add_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPADDW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPADDW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> Add(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_add_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPADDD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPADDD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> Add(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_add_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPADDD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPADDD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> Add(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_add_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPADDQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPADDQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> Add(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_add_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPADDQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPADDQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> Add(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_adds_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPADDSB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPADDSB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> AddSaturate(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_adds_epu8 (__m256i a, __m256i b)</para>
        ///   <para>  VPADDUSB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPADDUSB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> AddSaturate(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_adds_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPADDSW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPADDSW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> AddSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_adds_epu16 (__m256i a, __m256i b)</para>
        ///   <para>  VPADDUSW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPADDUSW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> AddSaturate(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)</para>
        ///   <para>  VPALIGNR ymm1,         ymm2, ymm3/m256, imm8</para>
        ///   <para>  VPALIGNR ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<sbyte> AlignRight(Vector256<sbyte> left, Vector256<sbyte> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)</para>
        ///   <para>  VPALIGNR ymm1,         ymm2, ymm3/m256, imm8</para>
        ///   <para>  VPALIGNR ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<byte> AlignRight(Vector256<byte> left, Vector256<byte> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)</para>
        ///   <para>  VPALIGNR ymm1,         ymm2, ymm3/m256, imm8</para>
        ///   <para>  VPALIGNR ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
        ///   <para>This intrinsic generates VPALIGNR that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<short> AlignRight(Vector256<short> left, Vector256<short> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)</para>
        ///   <para>  VPALIGNR ymm1,         ymm2, ymm3/m256, imm8</para>
        ///   <para>  VPALIGNR ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
        ///   <para>This intrinsic generates VPALIGNR that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<ushort> AlignRight(Vector256<ushort> left, Vector256<ushort> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)</para>
        ///   <para>  VPALIGNR ymm1,         ymm2, ymm3/m256, imm8</para>
        ///   <para>  VPALIGNR ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
        ///   <para>This intrinsic generates VPALIGNR that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<int> AlignRight(Vector256<int> left, Vector256<int> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)</para>
        ///   <para>  VPALIGNR ymm1,         ymm2, ymm3/m256, imm8</para>
        ///   <para>  VPALIGNR ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
        ///   <para>This intrinsic generates VPALIGNR that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<uint> AlignRight(Vector256<uint> left, Vector256<uint> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)</para>
        ///   <para>  VPALIGNR ymm1,         ymm2, ymm3/m256, imm8</para>
        ///   <para>  VPALIGNR ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
        ///   <para>This intrinsic generates VPALIGNR that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<long> AlignRight(Vector256<long> left, Vector256<long> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)</para>
        ///   <para>  VPALIGNR ymm1,         ymm2, ymm3/m256, imm8</para>
        ///   <para>  VPALIGNR ymm1 {k1}{z}, ymm2, ymm3/m256, imm8</para>
        ///   <para>This intrinsic generates VPALIGNR that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<ulong> AlignRight(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_and_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPAND ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> And(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_and_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPAND ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> And(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_and_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPAND ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> And(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_and_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPAND ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> And(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_and_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPAND  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPANDD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> And(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_and_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPAND  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPANDD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> And(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_and_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPAND  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPANDQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> And(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_and_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPAND  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPANDQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> And(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_andnot_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPANDN ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> AndNot(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_andnot_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPANDN ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> AndNot(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_andnot_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPANDN ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> AndNot(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_andnot_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPANDN ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> AndNot(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_andnot_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPANDN  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPANDND ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> AndNot(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_andnot_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPANDN  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPANDND ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> AndNot(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_andnot_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPANDN  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPANDNQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> AndNot(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_andnot_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPANDN  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPANDNQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> AndNot(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_avg_epu8 (__m256i a, __m256i b)</para>
        ///   <para>  VPAVGB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPAVGB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> Average(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_avg_epu16 (__m256i a, __m256i b)</para>
        ///   <para>  VPAVGW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPAVGW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> Average(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_blend_epi32 (__m128i a, __m128i b, const int imm8)</para>
        ///   <para>  VPBLENDD xmm1, xmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector128<int> Blend(Vector128<int> left, Vector128<int> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_blend_epi32 (__m128i a, __m128i b, const int imm8)</para>
        ///   <para>  VPBLENDD xmm1, xmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector128<uint> Blend(Vector128<uint> left, Vector128<uint> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_blend_epi16 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VPBLENDW ymm1, ymm2, ymm3/m256 imm8</para>
        /// </summary>
        public static Vector256<short> Blend(Vector256<short> left, Vector256<short> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_blend_epi16 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VPBLENDW ymm1, ymm2, ymm3/m256 imm8</para>
        /// </summary>
        public static Vector256<ushort> Blend(Vector256<ushort> left, Vector256<ushort> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_blend_epi32 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VPBLENDD ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<int> Blend(Vector256<int> left, Vector256<int> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_blend_epi32 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VPBLENDD ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<uint> Blend(Vector256<uint> left, Vector256<uint> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)</para>
        ///   <para>  VPBLENDVB ymm1, ymm2, ymm3/m256, ymm4</para>
        /// </summary>
        public static Vector256<sbyte> BlendVariable(Vector256<sbyte> left, Vector256<sbyte> right, Vector256<sbyte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)</para>
        ///   <para>  VPBLENDVB ymm1, ymm2, ymm3/m256, ymm4</para>
        /// </summary>
        public static Vector256<byte> BlendVariable(Vector256<byte> left, Vector256<byte> right, Vector256<byte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)</para>
        ///   <para>  VPBLENDVB ymm1, ymm2, ymm3/m256, ymm4</para>
        ///   <para>This intrinsic generates VPBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.</para>
        /// </summary>
        public static Vector256<short> BlendVariable(Vector256<short> left, Vector256<short> right, Vector256<short> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)</para>
        ///   <para>  VPBLENDVB ymm1, ymm2, ymm3/m256, ymm4</para>
        ///   <para>This intrinsic generates VPBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.</para>
        /// </summary>
        public static Vector256<ushort> BlendVariable(Vector256<ushort> left, Vector256<ushort> right, Vector256<ushort> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)</para>
        ///   <para>  VPBLENDVB ymm1, ymm2, ymm3/m256, ymm4</para>
        ///   <para>This intrinsic generates VPBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.</para>
        /// </summary>
        public static Vector256<int> BlendVariable(Vector256<int> left, Vector256<int> right, Vector256<int> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)</para>
        ///   <para>  VPBLENDVB ymm1, ymm2, ymm3/m256, ymm4</para>
        ///   <para>This intrinsic generates VPBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.</para>
        /// </summary>
        public static Vector256<uint> BlendVariable(Vector256<uint> left, Vector256<uint> right, Vector256<uint> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)</para>
        ///   <para>  VPBLENDVB ymm1, ymm2, ymm3/m256, ymm4</para>
        ///   <para>This intrinsic generates VPBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.</para>
        /// </summary>
        public static Vector256<long> BlendVariable(Vector256<long> left, Vector256<long> right, Vector256<long> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)</para>
        ///   <para>  VPBLENDVB ymm1, ymm2, ymm3/m256, ymm4</para>
        ///   <para>This intrinsic generates VPBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.</para>
        /// </summary>
        public static Vector256<ulong> BlendVariable(Vector256<ulong> left, Vector256<ulong> right, Vector256<ulong> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_broadcastb_epi8 (__m128i a)</para>
        ///   <para>  VPBROADCASTB xmm1,         xmm2/m8</para>
        ///   <para>  VPBROADCASTB xmm1 {k1}{z}, xmm2/m8</para>
        /// </summary>
        public static Vector128<byte> BroadcastScalarToVector128(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcastb_epi8 (__m128i a)</para>
        ///   <para>  VPBROADCASTB xmm1,         xmm2/m8</para>
        ///   <para>  VPBROADCASTB xmm1 {k1}{z}, xmm2/m8</para>
        /// </summary>
        public static Vector128<sbyte> BroadcastScalarToVector128(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcastw_epi16 (__m128i a)</para>
        ///   <para>  VPBROADCASTW xmm1,         xmm2/m16</para>
        ///   <para>  VPBROADCASTW xmm1 {k1}{z}, xmm2/m16</para>
        /// </summary>
        public static Vector128<short> BroadcastScalarToVector128(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcastw_epi16 (__m128i a)</para>
        ///   <para>  VPBROADCASTW xmm1,         xmm2/m16</para>
        ///   <para>  VPBROADCASTW xmm1 {k1}{z}, xmm2/m16</para>
        /// </summary>
        public static Vector128<ushort> BroadcastScalarToVector128(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcastd_epi32 (__m128i a)</para>
        ///   <para>  VPBROADCASTD xmm1,         xmm2/m32</para>
        ///   <para>  VPBROADCASTD xmm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector128<int> BroadcastScalarToVector128(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcastd_epi32 (__m128i a)</para>
        ///   <para>  VPBROADCASTD xmm1,         xmm2/m32</para>
        ///   <para>  VPBROADCASTD xmm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector128<uint> BroadcastScalarToVector128(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcastq_epi64 (__m128i a)</para>
        ///   <para>  VPBROADCASTQ xmm1,         xmm2/m64</para>
        ///   <para>  VPBROADCASTQ xmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector128<long> BroadcastScalarToVector128(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcastq_epi64 (__m128i a)</para>
        ///   <para>  VPBROADCASTQ xmm1,         xmm2/m64</para>
        ///   <para>  VPBROADCASTQ xmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector128<ulong> BroadcastScalarToVector128(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_broadcastss_ps (__m128 a)</para>
        ///   <para>  VBROADCASTSS xmm1,         xmm2/m32</para>
        ///   <para>  VBROADCASTSS xmm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector128<float> BroadcastScalarToVector128(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_broadcastsd_pd (__m128d a)</para>
        ///   <para>  VMOVDDUP xmm1, xmm/m64</para>
        /// </summary>
        public static Vector128<double> BroadcastScalarToVector128(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_broadcastb_epi8 (__m128i a)</para>
        ///   <para>  VPBROADCASTB xmm1,         m8</para>
        ///   <para>  VPBROADCASTB xmm1 {k1}{z}, m8</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector128<byte> BroadcastScalarToVector128(byte* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcastb_epi8 (__m128i a)</para>
        ///   <para>  VPBROADCASTB xmm1,         m8</para>
        ///   <para>  VPBROADCASTB xmm1 {k1}{z}, m8</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector128<sbyte> BroadcastScalarToVector128(sbyte* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcastw_epi16 (__m128i a)</para>
        ///   <para>  VPBROADCASTW xmm1,         m16</para>
        ///   <para>  VPBROADCASTW xmm1 {k1}{z}, m16</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector128<short> BroadcastScalarToVector128(short* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcastw_epi16 (__m128i a)</para>
        ///   <para>  VPBROADCASTW xmm1,         m16</para>
        ///   <para>  VPBROADCASTW xmm1 {k1}{z}, m16</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector128<ushort> BroadcastScalarToVector128(ushort* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcastd_epi32 (__m128i a)</para>
        ///   <para>  VPBROADCASTD xmm1,         m32</para>
        ///   <para>  VPBROADCASTD xmm1 {k1}{z}, m32</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector128<int> BroadcastScalarToVector128(int* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcastd_epi32 (__m128i a)</para>
        ///   <para>  VPBROADCASTD xmm1,         m32</para>
        ///   <para>  VPBROADCASTD xmm1 {k1}{z}, m32</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector128<uint> BroadcastScalarToVector128(uint* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcastq_epi64 (__m128i a)</para>
        ///   <para>  VPBROADCASTQ xmm1,         m64</para>
        ///   <para>  VPBROADCASTQ xmm1 {k1}{z}, m64</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector128<long> BroadcastScalarToVector128(long* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_broadcastq_epi64 (__m128i a)</para>
        ///   <para>  VPBROADCASTQ xmm1,         m64</para>
        ///   <para>  VPBROADCASTQ xmm1 {k1}{z}, m64</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector128<ulong> BroadcastScalarToVector128(ulong* source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_broadcastb_epi8 (__m128i a)</para>
        ///   <para>  VPBROADCASTB ymm1,         xmm2/m8</para>
        ///   <para>  VPBROADCASTB ymm1 {k1}{z}, xmm2/m8</para>
        /// </summary>
        public static Vector256<byte> BroadcastScalarToVector256(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastb_epi8 (__m128i a)</para>
        ///   <para>  VPBROADCASTB ymm1,         xmm2/m8</para>
        ///   <para>  VPBROADCASTB ymm1 {k1}{z}, xmm2/m8</para>
        /// </summary>
        public static Vector256<sbyte> BroadcastScalarToVector256(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastw_epi16 (__m128i a)</para>
        ///   <para>  VPBROADCASTW ymm1,         xmm2/m16</para>
        ///   <para>  VPBROADCASTW ymm1 {k1}{z}, xmm2/m16</para>
        /// </summary>
        public static Vector256<short> BroadcastScalarToVector256(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastw_epi16 (__m128i a)</para>
        ///   <para>  VPBROADCASTW ymm1,         xmm2/m16</para>
        ///   <para>  VPBROADCASTW ymm1 {k1}{z}, xmm2/m16</para>
        /// </summary>
        public static Vector256<ushort> BroadcastScalarToVector256(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastd_epi32 (__m128i a)</para>
        ///   <para>  VPBROADCASTD ymm1,         xmm2/m32</para>
        ///   <para>  VPBROADCASTD ymm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector256<int> BroadcastScalarToVector256(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastd_epi32 (__m128i a)</para>
        ///   <para>  VPBROADCASTD ymm1,         xmm2/m32</para>
        ///   <para>  VPBROADCASTD ymm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector256<uint> BroadcastScalarToVector256(Vector128<uint> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastq_epi64 (__m128i a)</para>
        ///   <para>  VPBROADCASTQ ymm1,         xmm2/m64</para>
        ///   <para>  VPBROADCASTQ ymm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector256<long> BroadcastScalarToVector256(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastq_epi64 (__m128i a)</para>
        ///   <para>  VPBROADCASTQ ymm1,         xmm2/m64</para>
        ///   <para>  VPBROADCASTQ ymm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector256<ulong> BroadcastScalarToVector256(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_broadcastss_ps (__m128 a)</para>
        ///   <para>  VBROADCASTSS ymm1,         xmm2/m32</para>
        ///   <para>  VBROADCASTSS ymm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector256<float> BroadcastScalarToVector256(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_broadcastsd_pd (__m128d a)</para>
        ///   <para>  VBROADCASTSD ymm1,         xmm2/m64</para>
        ///   <para>  VBROADCASTSD ymm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector256<double> BroadcastScalarToVector256(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_broadcastb_epi8 (__m128i a)</para>
        ///   <para>  VPBROADCASTB ymm1,         m8</para>
        ///   <para>  VPBROADCASTB ymm1 {k1}{z}, m8</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<byte> BroadcastScalarToVector256(byte* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastb_epi8 (__m128i a)</para>
        ///   <para>  VPBROADCASTB ymm1,         m8</para>
        ///   <para>  VPBROADCASTB ymm1 {k1}{z}, m8</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<sbyte> BroadcastScalarToVector256(sbyte* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastw_epi16 (__m128i a)</para>
        ///   <para>  VPBROADCASTW ymm1,         m16</para>
        ///   <para>  VPBROADCASTW ymm1 {k1}{z}, m16</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<short> BroadcastScalarToVector256(short* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastw_epi16 (__m128i a)</para>
        ///   <para>  VPBROADCASTW ymm1,         m16</para>
        ///   <para>  VPBROADCASTW ymm1 {k1}{z}, m16</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<ushort> BroadcastScalarToVector256(ushort* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastd_epi32 (__m128i a)</para>
        ///   <para>  VPBROADCASTD ymm1,         m32</para>
        ///   <para>  VPBROADCASTD ymm1 {k1}{z}, m32</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<int> BroadcastScalarToVector256(int* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastd_epi32 (__m128i a)</para>
        ///   <para>  VPBROADCASTD ymm1,         m32</para>
        ///   <para>  VPBROADCASTD ymm1 {k1}{z}, m32</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<uint> BroadcastScalarToVector256(uint* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastq_epi64 (__m128i a)</para>
        ///   <para>  VPBROADCASTQ ymm1,         m64</para>
        ///   <para>  VPBROADCASTQ ymm1 {k1}{z}, m64</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<long> BroadcastScalarToVector256(long* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastq_epi64 (__m128i a)</para>
        ///   <para>  VPBROADCASTQ ymm1,         m64</para>
        ///   <para>  VPBROADCASTQ ymm1 {k1}{z}, m64</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<ulong> BroadcastScalarToVector256(ulong* source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_broadcastsi128_si256 (__m128i a)</para>
        ///   <para>  VBROADCASTI128  ymm1,         m128</para>
        ///   <para>  VBROADCASTI32x4 ymm1 {k1}{z}, m128</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<sbyte> BroadcastVector128ToVector256(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastsi128_si256 (__m128i a)</para>
        ///   <para>  VBROADCASTI128  ymm1,         m128</para>
        ///   <para>  VBROADCASTI32x4 ymm1 {k1}{z}, m128</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<byte> BroadcastVector128ToVector256(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastsi128_si256 (__m128i a)</para>
        ///   <para>  VBROADCASTI128  ymm1,         m128</para>
        ///   <para>  VBROADCASTI32x4 ymm1 {k1}{z}, m128</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<short> BroadcastVector128ToVector256(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastsi128_si256 (__m128i a)</para>
        ///   <para>  VBROADCASTI128  ymm1,         m128</para>
        ///   <para>  VBROADCASTI32x4 ymm1 {k1}{z}, m128</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<ushort> BroadcastVector128ToVector256(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastsi128_si256 (__m128i a)</para>
        ///   <para>  VBROADCASTI128  ymm1,         m128</para>
        ///   <para>  VBROADCASTI32x4 ymm1 {k1}{z}, m128</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<int> BroadcastVector128ToVector256(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastsi128_si256 (__m128i a)</para>
        ///   <para>  VBROADCASTI128  ymm1,         m128</para>
        ///   <para>  VBROADCASTI32x4 ymm1 {k1}{z}, m128</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<uint> BroadcastVector128ToVector256(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastsi128_si256 (__m128i a)</para>
        ///   <para>  VBROADCASTI128  ymm1,         m128</para>
        ///   <para>  VBROADCASTI64x2 ymm1 {k1}{z}, m128</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<long> BroadcastVector128ToVector256(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_broadcastsi128_si256 (__m128i a)</para>
        ///   <para>  VBROADCASTI128  ymm1,         m128</para>
        ///   <para>  VBROADCASTI64x2 ymm1 {k1}{z}, m128</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        public static unsafe Vector256<ulong> BroadcastVector128ToVector256(ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_cmpeq_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPEQB ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> CompareEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cmpeq_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPEQB ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> CompareEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cmpeq_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPEQW ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> CompareEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cmpeq_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPEQW ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> CompareEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cmpeq_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPEQD ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<int> CompareEqual(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cmpeq_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPEQD ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<uint> CompareEqual(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cmpeq_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPEQQ ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<long> CompareEqual(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cmpeq_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPEQQ ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ulong> CompareEqual(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_cmpgt_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPGTB ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> CompareGreaterThan(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cmpgt_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPGTW ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> CompareGreaterThan(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cmpgt_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPGTD ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<int> CompareGreaterThan(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cmpgt_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPCMPGTQ ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<long> CompareGreaterThan(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>int _mm256_cvtsi256_si32 (__m256i a)</para>
        ///   <para>  VMOVD r/m32, ymm1</para>
        /// </summary>
        public static int ConvertToInt32(Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_cvtsi256_si32 (__m256i a)</para>
        ///   <para>  VMOVD r/m32, ymm1</para>
        /// </summary>
        public static uint ConvertToUInt32(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_cvtepi8_epi16 (__m128i a)</para>
        ///   <para>  VPMOVSXBW ymm1,         xmm2/m128</para>
        ///   <para>  VPMOVSXBW ymm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector256<short> ConvertToVector256Int16(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvtepu8_epi16 (__m128i a)</para>
        ///   <para>  VPMOVZXBW ymm1,         xmm2/m128</para>
        ///   <para>  VPMOVZXBW ymm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector256<short> ConvertToVector256Int16(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvtepi8_epi32 (__m128i a)</para>
        ///   <para>  VPMOVSXBD ymm1,         xmm2/m64</para>
        ///   <para>  VPMOVSXBD ymm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvtepu8_epi32 (__m128i a)</para>
        ///   <para>  VPMOVZXBD ymm1,         xmm2/m64</para>
        ///   <para>  VPMOVZXBD ymm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvtepi16_epi32 (__m128i a)</para>
        ///   <para>  VPMOVSXWD ymm1,         xmm2/m128</para>
        ///   <para>  VPMOVSXWD ymm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvtepu16_epi32 (__m128i a)</para>
        ///   <para>  VPMOVZXWD ymm1,         xmm2/m128</para>
        ///   <para>  VPMOVZXWD ymm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvtepi8_epi64 (__m128i a)</para>
        ///   <para>  VPMOVSXBQ ymm1,         xmm2/m32</para>
        ///   <para>  VPMOVSXBQ ymm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvtepu8_epi64 (__m128i a)</para>
        ///   <para>  VPMOVZXBQ ymm1,         xmm2/m32</para>
        ///   <para>  VPMOVZXBQ ymm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvtepi16_epi64 (__m128i a)</para>
        ///   <para>  VPMOVSXWQ ymm1,         xmm2/m64</para>
        ///   <para>  VPMOVSXWQ ymm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvtepu16_epi64 (__m128i a)</para>
        ///   <para>  VPMOVZXWQ ymm1,         xmm2/m64</para>
        ///   <para>  VPMOVZXWQ ymm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvtepi32_epi64 (__m128i a)</para>
        ///   <para>  VPMOVSXDQ ymm1,         xmm2/m128</para>
        ///   <para>  VPMOVSXDQ ymm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_cvtepu32_epi64 (__m128i a)</para>
        ///   <para>  VPMOVZXDQ ymm1,         xmm2/m128</para>
        ///   <para>  VPMOVZXDQ ymm1 {k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  VPMOVSXBW ymm1,         m128</para>
        ///   <para>  VPMOVSXBW ymm1 {k1}{z}, m128</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector256<short> ConvertToVector256Int16(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>  VPMOVZXBW ymm1,         m128</para>
        ///   <para>  VPMOVZXBW ymm1 {k1}{z}, m128</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector256<short> ConvertToVector256Int16(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>  VPMOVSXBD ymm1,         m64</para>
        ///   <para>  VPMOVSXBD ymm1 {k1}{z}, m64</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector256<int> ConvertToVector256Int32(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>  VPMOVZXBD ymm1,         m64</para>
        ///   <para>  VPMOVZXBD ymm1 {k1}{z}, m64</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector256<int> ConvertToVector256Int32(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>  VPMOVSXWD ymm1,         m128</para>
        ///   <para>  VPMOVSXWD ymm1 {k1}{z}, m128</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector256<int> ConvertToVector256Int32(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>  VPMOVZXWD ymm1,         m128</para>
        ///   <para>  VPMOVZXWD ymm1 {k1}{z}, m128</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector256<int> ConvertToVector256Int32(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>  VPMOVSXBQ ymm1,         m32</para>
        ///   <para>  VPMOVSXBQ ymm1 {k1}{z}, m32</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector256<long> ConvertToVector256Int64(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>  VPMOVZXBQ ymm1,         m32</para>
        ///   <para>  VPMOVZXBQ ymm1 {k1}{z}, m32</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector256<long> ConvertToVector256Int64(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>  VPMOVSXWQ ymm1,         m64</para>
        ///   <para>  VPMOVSXWQ ymm1 {k1}{z}, m64</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector256<long> ConvertToVector256Int64(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>  VPMOVZXWQ ymm1,         m64</para>
        ///   <para>  VPMOVZXWQ ymm1 {k1}{z}, m64</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector256<long> ConvertToVector256Int64(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>  VPMOVSXDQ ymm1,         m128</para>
        ///   <para>  VPMOVSXDQ ymm1 {k1}{z}, m128</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector256<long> ConvertToVector256Int64(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>  VPMOVZXDQ ymm1,         m128</para>
        ///   <para>  VPMOVZXDQ ymm1 {k1}{z}, m128</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector256<long> ConvertToVector256Int64(uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm256_extracti128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTI128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTI32x4 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static new Vector128<sbyte> ExtractVector128(Vector256<sbyte> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_extracti128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTI128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTI32x4 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static new Vector128<byte> ExtractVector128(Vector256<byte> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_extracti128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTI128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTI32x4 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static new Vector128<short> ExtractVector128(Vector256<short> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_extracti128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTI128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTI32x4 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static new Vector128<ushort> ExtractVector128(Vector256<ushort> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_extracti128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTI128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTI32x4 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static new Vector128<int> ExtractVector128(Vector256<int> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_extracti128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTI128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTI32x4 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static new Vector128<uint> ExtractVector128(Vector256<uint> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_extracti128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTI128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTI64x2 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static new Vector128<long> ExtractVector128(Vector256<long> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_extracti128_si256 (__m256i a, const int imm8)</para>
        ///   <para>  VEXTRACTI128  xmm1/m128,         ymm2, imm8</para>
        ///   <para>  VEXTRACTI64x2 xmm1/m128 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static new Vector128<ulong> ExtractVector128(Vector256<ulong> value, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_i32gather_epi32 (int const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VPGATHERDD xmm1, vm32x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector128<int> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_i32gather_epi32 (int const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VPGATHERDD xmm1, vm32x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector128<int> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_i32gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VPGATHERDQ xmm1, vm32x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<long> GatherVector128(long* baseAddress, Vector128<int> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_i32gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VPGATHERDQ xmm1, vm32x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<ulong> GatherVector128(ulong* baseAddress, Vector128<int> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_i32gather_ps (float const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VGATHERDPS xmm1, vm32x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector128<int> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_i32gather_pd (double const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VGATHERDPD xmm1, vm32x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<double> GatherVector128(double* baseAddress, Vector128<int> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_i64gather_epi32 (int const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VPGATHERQD xmm1, vm64x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector128<long> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_i64gather_epi32 (int const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VPGATHERQD xmm1, vm64x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector128<long> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_i64gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VPGATHERQQ xmm1, vm64x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<long> GatherVector128(long* baseAddress, Vector128<long> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_i64gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VPGATHERQQ xmm1, vm64x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<ulong> GatherVector128(ulong* baseAddress, Vector128<long> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_i64gather_ps (float const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VGATHERQPS xmm1, vm64x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector128<long> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_i64gather_pd (double const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VGATHERQPD xmm1, vm64x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<double> GatherVector128(double* baseAddress, Vector128<long> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_i32gather_epi32 (int const* base_addr, __m256i vindex, const int scale)</para>
        ///   <para>  VPGATHERDD ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<int> GatherVector256(int* baseAddress, Vector256<int> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_i32gather_epi32 (int const* base_addr, __m256i vindex, const int scale)</para>
        ///   <para>  VPGATHERDD ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<uint> GatherVector256(uint* baseAddress, Vector256<int> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_i32gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VPGATHERDQ ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<long> GatherVector256(long* baseAddress, Vector128<int> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_i32gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VPGATHERDQ ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<ulong> GatherVector256(ulong* baseAddress, Vector128<int> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_i32gather_ps (float const* base_addr, __m256i vindex, const int scale)</para>
        ///   <para>  VGATHERDPS ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<float> GatherVector256(float* baseAddress, Vector256<int> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_i32gather_pd (double const* base_addr, __m128i vindex, const int scale)</para>
        ///   <para>  VGATHERDPD ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<double> GatherVector256(double* baseAddress, Vector128<int> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_i64gather_epi32 (int const* base_addr, __m256i vindex, const int scale)</para>
        ///   <para>  VPGATHERQD xmm1, vm64y, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector256<long> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_i64gather_epi32 (int const* base_addr, __m256i vindex, const int scale)</para>
        ///   <para>  VPGATHERQD xmm1, vm64y, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector256<long> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_i64gather_epi64 (__int64 const* base_addr, __m256i vindex, const int scale)</para>
        ///   <para>  VPGATHERQQ ymm1, vm64y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<long> GatherVector256(long* baseAddress, Vector256<long> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_i64gather_epi64 (__int64 const* base_addr, __m256i vindex, const int scale)</para>
        ///   <para>  VPGATHERQQ ymm1, vm64y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<ulong> GatherVector256(ulong* baseAddress, Vector256<long> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm256_i64gather_ps (float const* base_addr, __m256i vindex, const int scale)</para>
        ///   <para>  VGATHERQPS xmm1, vm64y, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector256<long> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_i64gather_pd (double const* base_addr, __m256i vindex, const int scale)</para>
        ///   <para>  VGATHERQPD ymm1, vm64y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<double> GatherVector256(double* baseAddress, Vector256<long> index, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_mask_i32gather_epi32 (__m128i src, int const* base_addr, __m128i vindex, __m128i mask, const int scale)</para>
        ///   <para>  VPGATHERDD xmm1, vm32x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<int> GatherMaskVector128(Vector128<int> source, int* baseAddress, Vector128<int> index, Vector128<int> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_i32gather_epi32 (__m128i src, int const* base_addr, __m128i vindex, __m128i mask, const int scale)</para>
        ///   <para>  VPGATHERDD xmm1, vm32x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<uint> GatherMaskVector128(Vector128<uint> source, uint* baseAddress, Vector128<int> index, Vector128<uint> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_i32gather_epi64 (__m128i src, __int64 const* base_addr, __m128i vindex, __m128i mask, const int scale)</para>
        ///   <para>  VPGATHERDQ xmm1, vm32x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<long> GatherMaskVector128(Vector128<long> source, long* baseAddress, Vector128<int> index, Vector128<long> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_i32gather_epi64 (__m128i src, __int64 const* base_addr, __m128i vindex, __m128i mask, const int scale)</para>
        ///   <para>  VPGATHERDQ xmm1, vm32x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<ulong> GatherMaskVector128(Vector128<ulong> source, ulong* baseAddress, Vector128<int> index, Vector128<ulong> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_mask_i32gather_ps (__m128 src, float const* base_addr, __m128i vindex, __m128 mask, const int scale)</para>
        ///   <para>  VGATHERDPS xmm1, vm32x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<float> GatherMaskVector128(Vector128<float> source, float* baseAddress, Vector128<int> index, Vector128<float> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_mask_i32gather_pd (__m128d src, double const* base_addr, __m128i vindex, __m128d mask, const int scale)</para>
        ///   <para>  VGATHERDPD xmm1, vm32x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<double> GatherMaskVector128(Vector128<double> source, double* baseAddress, Vector128<int> index, Vector128<double> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_i64gather_epi32 (__m128i src, int const* base_addr, __m128i vindex, __m128i mask, const int scale)</para>
        ///   <para>  VPGATHERQD xmm1, vm64x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<int> GatherMaskVector128(Vector128<int> source, int* baseAddress, Vector128<long> index, Vector128<int> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_i64gather_epi32 (__m128i src, int const* base_addr, __m128i vindex, __m128i mask, const int scale)</para>
        ///   <para>  VPGATHERQD xmm1, vm64x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<uint> GatherMaskVector128(Vector128<uint> source, uint* baseAddress, Vector128<long> index, Vector128<uint> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_i64gather_epi64 (__m128i src, __int64 const* base_addr, __m128i vindex, __m128i mask, const int scale)</para>
        ///   <para>  VPGATHERQQ xmm1, vm64x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<long> GatherMaskVector128(Vector128<long> source, long* baseAddress, Vector128<long> index, Vector128<long> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_mask_i64gather_epi64 (__m128i src, __int64 const* base_addr, __m128i vindex, __m128i mask, const int scale)</para>
        ///   <para>  VPGATHERQQ xmm1, vm64x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<ulong> GatherMaskVector128(Vector128<ulong> source, ulong* baseAddress, Vector128<long> index, Vector128<ulong> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm_mask_i64gather_ps (__m128 src, float const* base_addr, __m128i vindex, __m128 mask, const int scale)</para>
        ///   <para>  VGATHERQPS xmm1, vm64x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<float> GatherMaskVector128(Vector128<float> source, float* baseAddress, Vector128<long> index, Vector128<float> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128d _mm_mask_i64gather_pd (__m128d src, double const* base_addr, __m128i vindex, __m128d mask, const int scale)</para>
        ///   <para>  VGATHERQPD xmm1, vm64x, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<double> GatherMaskVector128(Vector128<double> source, double* baseAddress, Vector128<long> index, Vector128<double> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_i32gather_epi32 (__m256i src, int const* base_addr, __m256i vindex, __m256i mask, const int scale)</para>
        ///   <para>  VPGATHERDD ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<int> GatherMaskVector256(Vector256<int> source, int* baseAddress, Vector256<int> index, Vector256<int> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_i32gather_epi32 (__m256i src, int const* base_addr, __m256i vindex, __m256i mask, const int scale)</para>
        ///   <para>  VPGATHERDD ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<uint> GatherMaskVector256(Vector256<uint> source, uint* baseAddress, Vector256<int> index, Vector256<uint> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_i32gather_epi64 (__m256i src, __int64 const* base_addr, __m128i vindex, __m256i mask, const int scale)</para>
        ///   <para>  VPGATHERDQ ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<long> GatherMaskVector256(Vector256<long> source, long* baseAddress, Vector128<int> index, Vector256<long> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_i32gather_epi64 (__m256i src, __int64 const* base_addr, __m128i vindex, __m256i mask, const int scale)</para>
        ///   <para>  VPGATHERDQ ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<ulong> GatherMaskVector256(Vector256<ulong> source, ulong* baseAddress, Vector128<int> index, Vector256<ulong> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_mask_i32gather_ps (__m256 src, float const* base_addr, __m256i vindex, __m256 mask, const int scale)</para>
        ///   <para>  VPGATHERDPS ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<float> GatherMaskVector256(Vector256<float> source, float* baseAddress, Vector256<int> index, Vector256<float> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_mask_i32gather_pd (__m256d src, double const* base_addr, __m128i vindex, __m256d mask, const int scale)</para>
        ///   <para>  VPGATHERDPD ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<double> GatherMaskVector256(Vector256<double> source, double* baseAddress, Vector128<int> index, Vector256<double> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_mask_i64gather_epi32 (__m128i src, int const* base_addr, __m256i vindex, __m128i mask, const int scale)</para>
        ///   <para>  VPGATHERQD xmm1, vm32y, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<int> GatherMaskVector128(Vector128<int> source, int* baseAddress, Vector256<long> index, Vector128<int> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm256_mask_i64gather_epi32 (__m128i src, int const* base_addr, __m256i vindex, __m128i mask, const int scale)</para>
        ///   <para>  VPGATHERQD xmm1, vm32y, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<uint> GatherMaskVector128(Vector128<uint> source, uint* baseAddress, Vector256<long> index, Vector128<uint> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_i64gather_epi64 (__m256i src, __int64 const* base_addr, __m256i vindex, __m256i mask, const int scale)</para>
        ///   <para>  VPGATHERQQ ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<long> GatherMaskVector256(Vector256<long> source, long* baseAddress, Vector256<long> index, Vector256<long> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mask_i64gather_epi64 (__m256i src, __int64 const* base_addr, __m256i vindex, __m256i mask, const int scale)</para>
        ///   <para>  VPGATHERQQ ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<ulong> GatherMaskVector256(Vector256<ulong> source, ulong* baseAddress, Vector256<long> index, Vector256<ulong> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128 _mm256_mask_i64gather_ps (__m128 src, float const* base_addr, __m256i vindex, __m128 mask, const int scale)</para>
        ///   <para>  VGATHERQPS xmm1, vm32y, xmm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector128<float> GatherMaskVector128(Vector128<float> source, float* baseAddress, Vector256<long> index, Vector128<float> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_mask_i64gather_pd (__m256d src, double const* base_addr, __m256i vindex, __m256d mask, const int scale)</para>
        ///   <para>  VGATHERQPD ymm1, vm32y, ymm2</para>
        ///   <para>The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.</para>
        /// </summary>
        public static unsafe Vector256<double> GatherMaskVector256(Vector256<double> source, double* baseAddress, Vector256<long> index, Vector256<double> mask, [ConstantExpected(Min = (byte)(1), Max = (byte)(8))] byte scale) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_hadd_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPHADDW ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> HorizontalAdd(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_hadd_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPHADDD ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<int> HorizontalAdd(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_hadds_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPHADDSW ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> HorizontalAddSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_hsub_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPHSUBW ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> HorizontalSubtract(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_hsub_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPHSUBD ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<int> HorizontalSubtract(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_hsubs_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPHSUBSW ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> HorizontalSubtractSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTI32x4 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static new Vector256<sbyte> InsertVector128(Vector256<sbyte> value, Vector128<sbyte> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTI32x4 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static new Vector256<byte> InsertVector128(Vector256<byte> value, Vector128<byte> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTI32x4 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static new Vector256<short> InsertVector128(Vector256<short> value, Vector128<short> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTI32x4 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static new Vector256<ushort> InsertVector128(Vector256<ushort> value, Vector128<ushort> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTI32x4 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static new Vector256<int> InsertVector128(Vector256<int> value, Vector128<int> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTI32x4 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static new Vector256<uint> InsertVector128(Vector256<uint> value, Vector128<uint> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTI64x2 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static new Vector256<long> InsertVector128(Vector256<long> value, Vector128<long> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)</para>
        ///   <para>  VINSERTI128  ymm1,         ymm2, xmm3/m128, imm8</para>
        ///   <para>  VINSERTI64x2 ymm1 {k1}{z}, ymm2, xmm3/m128, imm8</para>
        /// </summary>
        public static new Vector256<ulong> InsertVector128(Vector256<ulong> value, Vector128<ulong> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_stream_load_si256 (__m256i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<sbyte> LoadAlignedVector256NonTemporal(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_stream_load_si256 (__m256i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<byte> LoadAlignedVector256NonTemporal(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_stream_load_si256 (__m256i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<short> LoadAlignedVector256NonTemporal(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_stream_load_si256 (__m256i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<ushort> LoadAlignedVector256NonTemporal(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_stream_load_si256 (__m256i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<int> LoadAlignedVector256NonTemporal(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_stream_load_si256 (__m256i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<uint> LoadAlignedVector256NonTemporal(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_stream_load_si256 (__m256i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<long> LoadAlignedVector256NonTemporal(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_stream_load_si256 (__m256i const* mem_addr)</para>
        ///   <para>  VMOVNTDQA ymm1, m256</para>
        /// </summary>
        public static unsafe Vector256<ulong> LoadAlignedVector256NonTemporal(ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_maskload_epi32 (int const* mem_addr, __m128i mask)</para>
        ///   <para>  VPMASKMOVD xmm1, xmm2, m128</para>
        /// </summary>
        public static unsafe Vector128<int> MaskLoad(int* address, Vector128<int> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_maskload_epi32 (int const* mem_addr, __m128i mask)</para>
        ///   <para>  VPMASKMOVD xmm1, xmm2, m128</para>
        /// </summary>
        public static unsafe Vector128<uint> MaskLoad(uint* address, Vector128<uint> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_maskload_epi64 (__int64 const* mem_addr, __m128i mask)</para>
        ///   <para>  VPMASKMOVQ xmm1, xmm2, m128</para>
        /// </summary>
        public static unsafe Vector128<long> MaskLoad(long* address, Vector128<long> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_maskload_epi64 (__int64 const* mem_addr, __m128i mask)</para>
        ///   <para>  VPMASKMOVQ xmm1, xmm2, m128</para>
        /// </summary>
        public static unsafe Vector128<ulong> MaskLoad(ulong* address, Vector128<ulong> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_maskload_epi32 (int const* mem_addr, __m256i mask)</para>
        ///   <para>  VPMASKMOVD ymm1, ymm2, m256</para>
        /// </summary>
        public static unsafe Vector256<int> MaskLoad(int* address, Vector256<int> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_maskload_epi32 (int const* mem_addr, __m256i mask)</para>
        ///   <para>  VPMASKMOVD ymm1, ymm2, m256</para>
        /// </summary>
        public static unsafe Vector256<uint> MaskLoad(uint* address, Vector256<uint> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_maskload_epi64 (__int64 const* mem_addr, __m256i mask)</para>
        ///   <para>  VPMASKMOVQ ymm1, ymm2, m256</para>
        /// </summary>
        public static unsafe Vector256<long> MaskLoad(long* address, Vector256<long> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_maskload_epi64 (__int64 const* mem_addr, __m256i mask)</para>
        ///   <para>  VPMASKMOVQ ymm1, ymm2, m256</para>
        /// </summary>
        public static unsafe Vector256<ulong> MaskLoad(ulong* address, Vector256<ulong> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>void _mm_maskstore_epi32 (int* mem_addr, __m128i mask, __m128i a)</para>
        ///   <para>  VPMASKMOVD m128, xmm1, xmm2</para>
        /// </summary>
        public static unsafe void MaskStore(int* address, Vector128<int> mask, Vector128<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_maskstore_epi32 (int* mem_addr, __m128i mask, __m128i a)</para>
        ///   <para>  VPMASKMOVD m128, xmm1, xmm2</para>
        /// </summary>
        public static unsafe void MaskStore(uint* address, Vector128<uint> mask, Vector128<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_maskstore_epi64 (__int64* mem_addr, __m128i mask, __m128i a)</para>
        ///   <para>  VPMASKMOVQ m128, xmm1, xmm2</para>
        /// </summary>
        public static unsafe void MaskStore(long* address, Vector128<long> mask, Vector128<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm_maskstore_epi64 (__int64* mem_addr, __m128i mask, __m128i a)</para>
        ///   <para>  VPMASKMOVQ m128, xmm1, xmm2</para>
        /// </summary>
        public static unsafe void MaskStore(ulong* address, Vector128<ulong> mask, Vector128<ulong> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_maskstore_epi32 (int* mem_addr, __m256i mask, __m256i a)</para>
        ///   <para>  VPMASKMOVD m256, ymm1, ymm2</para>
        /// </summary>
        public static unsafe void MaskStore(int* address, Vector256<int> mask, Vector256<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_maskstore_epi32 (int* mem_addr, __m256i mask, __m256i a)</para>
        ///   <para>  VPMASKMOVD m256, ymm1, ymm2</para>
        /// </summary>
        public static unsafe void MaskStore(uint* address, Vector256<uint> mask, Vector256<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_maskstore_epi64 (__int64* mem_addr, __m256i mask, __m256i a)</para>
        ///   <para>  VPMASKMOVQ m256, ymm1, ymm2</para>
        /// </summary>
        public static unsafe void MaskStore(long* address, Vector256<long> mask, Vector256<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>void _mm256_maskstore_epi64 (__int64* mem_addr, __m256i mask, __m256i a)</para>
        ///   <para>  VPMASKMOVQ m256, ymm1, ymm2</para>
        /// </summary>
        public static unsafe void MaskStore(ulong* address, Vector256<ulong> mask, Vector256<ulong> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_madd_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPMADDWD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMADDWD ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<int> MultiplyAddAdjacent(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_maddubs_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPMADDUBSW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMADDUBSW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> MultiplyAddAdjacent(Vector256<byte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_max_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPMAXSB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMAXSB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> Max(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_max_epu8 (__m256i a, __m256i b)</para>
        ///   <para>  VPMAXUB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMAXUB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> Max(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_max_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPMAXSW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMAXSW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> Max(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_max_epu16 (__m256i a, __m256i b)</para>
        ///   <para>  VPMAXUW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMAXUW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> Max(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_max_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPMAXSD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMAXSD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> Max(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_max_epu32 (__m256i a, __m256i b)</para>
        ///   <para>  VPMAXUD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMAXUD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> Max(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_min_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPMINSB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMINSB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> Min(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_min_epu8 (__m256i a, __m256i b)</para>
        ///   <para>  VPMINUB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMINUB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> Min(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_min_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPMINSW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMINSW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> Min(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_min_epu16 (__m256i a, __m256i b)</para>
        ///   <para>  VPMINUW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMINUW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> Min(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_min_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPMINSD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMINSD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> Min(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_min_epu32 (__m256i a, __m256i b)</para>
        ///   <para>  VPMINUD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMINUD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> Min(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>int _mm256_movemask_epi8 (__m256i a)</para>
        ///   <para>  VPMOVMSKB r32, ymm1</para>
        /// </summary>
        public static int MoveMask(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>int _mm256_movemask_epi8 (__m256i a)</para>
        ///   <para>  VPMOVMSKB r32, ymm1</para>
        /// </summary>
        public static int MoveMask(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_mpsadbw_epu8 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VMPSADBW ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static Vector256<ushort> MultipleSumAbsoluteDifferences(Vector256<byte> left, Vector256<byte> right, [ConstantExpected] byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_mul_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPMULDQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMULDQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> Multiply(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mul_epu32 (__m256i a, __m256i b)</para>
        ///   <para>  VPMULUDQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMULUDQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> Multiply(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_mulhi_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPMULHW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMULHW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> MultiplyHigh(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mulhi_epu16 (__m256i a, __m256i b)</para>
        ///   <para>  VPMULHUW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMULHUW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> MultiplyHigh(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_mulhrs_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPMULHRSW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMULHRSW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> MultiplyHighRoundScale(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_mullo_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPMULLW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMULLW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> MultiplyLow(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mullo_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPMULLW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMULLW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> MultiplyLow(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mullo_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPMULLD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMULLD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> MultiplyLow(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_mullo_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPMULLD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPMULLD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> MultiplyLow(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_or_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPOR ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> Or(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_or_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPOR ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> Or(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_or_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPOR ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> Or(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_or_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPOR ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> Or(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_or_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPOR  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPORD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> Or(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_or_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPOR  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPORD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> Or(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_or_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPOR  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPORQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> Or(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_or_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPOR  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPORQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> Or(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_packs_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPACKSSWB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPACKSSWB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> PackSignedSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_packs_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPACKSSDW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPACKSSDW ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<short> PackSignedSaturate(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_packus_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPACKUSWB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPACKUSWB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> PackUnsignedSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_packus_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPACKUSDW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPACKUSDW ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<ushort> PackUnsignedSaturate(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VPERM2I128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static new Vector256<sbyte> Permute2x128(Vector256<sbyte> left, Vector256<sbyte> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VPERM2I128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static new Vector256<byte> Permute2x128(Vector256<byte> left, Vector256<byte> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VPERM2I128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static new Vector256<short> Permute2x128(Vector256<short> left, Vector256<short> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VPERM2I128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static new Vector256<ushort> Permute2x128(Vector256<ushort> left, Vector256<ushort> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VPERM2I128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static new Vector256<int> Permute2x128(Vector256<int> left, Vector256<int> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VPERM2I128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static new Vector256<uint> Permute2x128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VPERM2I128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static new Vector256<long> Permute2x128(Vector256<long> left, Vector256<long> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)</para>
        ///   <para>  VPERM2I128 ymm1, ymm2, ymm3/m256, imm8</para>
        /// </summary>
        public static new Vector256<ulong> Permute2x128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_permute4x64_epi64 (__m256i a, const int imm8)</para>
        ///   <para>  VPERMQ ymm1,         ymm2/m256,         imm8</para>
        ///   <para>  VPERMQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<long> Permute4x64(Vector256<long> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permute4x64_epi64 (__m256i a, const int imm8)</para>
        ///   <para>  VPERMQ ymm1,         ymm2/m256,         imm8</para>
        ///   <para>  VPERMQ ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<ulong> Permute4x64(Vector256<ulong> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256d _mm256_permute4x64_pd (__m256d a, const int imm8)</para>
        ///   <para>  VPERMPD ymm1,         ymm2/m256,         imm8</para>
        ///   <para>  VPERMPD ymm1 {k1}{z}, ymm2/m256/m64bcst, imm8</para>
        /// </summary>
        public static Vector256<double> Permute4x64(Vector256<double> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_permutevar8x32_epi32 (__m256i a, __m256i idx)</para>
        ///   <para>  VPERMD ymm1,         ymm2/m256,         imm8</para>
        ///   <para>  VPERMD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<int> PermuteVar8x32(Vector256<int> left, Vector256<int> control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_permutevar8x32_epi32 (__m256i a, __m256i idx)</para>
        ///   <para>  VPERMD ymm1,         ymm2/m256,         imm8</para>
        ///   <para>  VPERMD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<uint> PermuteVar8x32(Vector256<uint> left, Vector256<uint> control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256 _mm256_permutevar8x32_ps (__m256 a, __m256i idx)</para>
        ///   <para>  VPERMPS ymm1,         ymm2/m256,         imm8</para>
        ///   <para>  VPERMPS ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<float> PermuteVar8x32(Vector256<float> left, Vector256<int> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_sll_epi16 (__m256i a, __m128i count)</para>
        ///   <para>  VPSLLW ymm1,         ymm2, xmm3/m128</para>
        ///   <para>  VPSLLW ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<short> ShiftLeftLogical(Vector256<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sll_epi16 (__m256i a, __m128i count)</para>
        ///   <para>  VPSLLW ymm1,         ymm2, xmm3/m128</para>
        ///   <para>  VPSLLW ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<ushort> ShiftLeftLogical(Vector256<ushort> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sll_epi32 (__m256i a, __m128i count)</para>
        ///   <para>  VPSLLD ymm1,         ymm2, xmm3/m128</para>
        ///   <para>  VPSLLD ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<int> ShiftLeftLogical(Vector256<int> value, Vector128<int> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sll_epi32 (__m256i a, __m128i count)</para>
        ///   <para>  VPSLLD ymm1,         ymm2, xmm3/m128</para>
        ///   <para>  VPSLLD ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<uint> ShiftLeftLogical(Vector256<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sll_epi64 (__m256i a, __m128i count)</para>
        ///   <para>  VPSLLQ ymm1,         ymm2, xmm3/m128</para>
        ///   <para>  VPSLLQ ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<long> ShiftLeftLogical(Vector256<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sll_epi64 (__m256i a, __m128i count)</para>
        ///   <para>  VPSLLQ ymm1,         ymm2, xmm3/m128</para>
        ///   <para>  VPSLLQ ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogical(Vector256<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_slli_epi16 (__m256i a, int imm8)</para>
        ///   <para>  VPSLLW ymm1,         ymm2, imm8</para>
        ///   <para>  VPSLLW ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<short> ShiftLeftLogical(Vector256<short> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_slli_epi16 (__m256i a, int imm8)</para>
        ///   <para>  VPSLLW ymm1,         ymm2, imm8</para>
        ///   <para>  VPSLLW ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<ushort> ShiftLeftLogical(Vector256<ushort> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_slli_epi32 (__m256i a, int imm8)</para>
        ///   <para>  VPSLLD ymm1,         ymm2, imm8</para>
        ///   <para>  VPSLLD ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<int> ShiftLeftLogical(Vector256<int> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_slli_epi32 (__m256i a, int imm8)</para>
        ///   <para>  VPSLLD ymm1,         ymm2, imm8</para>
        ///   <para>  VPSLLD ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<uint> ShiftLeftLogical(Vector256<uint> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_slli_epi64 (__m256i a, int imm8)</para>
        ///   <para>  VPSLLQ ymm1,         ymm2, imm8</para>
        ///   <para>  VPSLLQ ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<long> ShiftLeftLogical(Vector256<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_slli_epi64 (__m256i a, int imm8)</para>
        ///   <para>  VPSLLQ ymm1,         ymm2, imm8</para>
        ///   <para>  VPSLLQ ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogical(Vector256<ulong> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_bslli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSLLDQ ymm1, ymm2/m256, imm8</para>
        /// </summary>
        public static Vector256<sbyte> ShiftLeftLogical128BitLane(Vector256<sbyte> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_bslli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSLLDQ ymm1, ymm2/m256, imm8</para>
        /// </summary>
        public static Vector256<byte> ShiftLeftLogical128BitLane(Vector256<byte> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_bslli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSLLDQ ymm1, ymm2/m256, imm8</para>
        ///   <para>This intrinsic generates PSLLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<short> ShiftLeftLogical128BitLane(Vector256<short> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_bslli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSLLDQ ymm1, ymm2/m256, imm8</para>
        ///   <para>This intrinsic generates PSLLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<ushort> ShiftLeftLogical128BitLane(Vector256<ushort> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_bslli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSLLDQ ymm1, ymm2/m256, imm8</para>
        ///   <para>This intrinsic generates PSLLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<int> ShiftLeftLogical128BitLane(Vector256<int> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_bslli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSLLDQ ymm1, ymm2/m256, imm8</para>
        ///   <para>This intrinsic generates PSLLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<uint> ShiftLeftLogical128BitLane(Vector256<uint> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_bslli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSLLDQ ymm1, ymm2/m256, imm8</para>
        ///   <para>This intrinsic generates PSLLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<long> ShiftLeftLogical128BitLane(Vector256<long> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_bslli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSLLDQ ymm1, ymm2/m256, imm8</para>
        ///   <para>This intrinsic generates PSLLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogical128BitLane(Vector256<ulong> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_sllv_epi32 (__m128i a, __m128i count)</para>
        ///   <para>  VPSLLVD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSLLVD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> ShiftLeftLogicalVariable(Vector128<int> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_sllv_epi32 (__m128i a, __m128i count)</para>
        ///   <para>  VPSLLVD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSLLVD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> ShiftLeftLogicalVariable(Vector128<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_sllv_epi64 (__m128i a, __m128i count)</para>
        ///   <para>  VPSLLVQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSLLVQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> ShiftLeftLogicalVariable(Vector128<long> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_sllv_epi64 (__m128i a, __m128i count)</para>
        ///   <para>  VPSLLVQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSLLVQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogicalVariable(Vector128<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sllv_epi32 (__m256i a, __m256i count)</para>
        ///   <para>  VPSLLVD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSLLVD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> ShiftLeftLogicalVariable(Vector256<int> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sllv_epi32 (__m256i a, __m256i count)</para>
        ///   <para>  VPSLLVD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSLLVD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> ShiftLeftLogicalVariable(Vector256<uint> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sllv_epi64 (__m256i a, __m256i count)</para>
        ///   <para>  VPSLLVQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSLLVQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> ShiftLeftLogicalVariable(Vector256<long> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sllv_epi64 (__m256i a, __m256i count)</para>
        ///   <para>  VPSLLVQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSLLVQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogicalVariable(Vector256<ulong> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>_mm256_sra_epi16 (__m256i a, __m128i count)</para>
        ///   <para>  VPSRAW ymm1,         ymm2, xmm3/m128</para>
        ///   <para>  VPSRAW ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<short> ShiftRightArithmetic(Vector256<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>_mm256_sra_epi32 (__m256i a, __m128i count)</para>
        ///   <para>  VPSRAD ymm1,         ymm2, xmm3/m128</para>
        ///   <para>  VPSRAD ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<int> ShiftRightArithmetic(Vector256<int> value, Vector128<int> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_srai_epi16 (__m256i a, int imm8)</para>
        ///   <para>  VPSRAW ymm1,         ymm2, imm8</para>
        ///   <para>  VPSRAW ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<short> ShiftRightArithmetic(Vector256<short> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srai_epi32 (__m256i a, int imm8)</para>
        ///   <para>  VPSRAD ymm1,         ymm2, imm8</para>
        ///   <para>  VPSRAD ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<int> ShiftRightArithmetic(Vector256<int> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_srav_epi32 (__m128i a, __m128i count)</para>
        ///   <para>  VPSRAVD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSRAVD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticVariable(Vector128<int> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srav_epi32 (__m256i a, __m256i count)</para>
        ///   <para>  VPSRAVD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSRAVD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> ShiftRightArithmeticVariable(Vector256<int> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_srl_epi16 (__m256i a, __m128i count)</para>
        ///   <para>  VPSRLW ymm1,         ymm2, xmm3/m128</para>
        ///   <para>  VPSRLW ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<short> ShiftRightLogical(Vector256<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srl_epi16 (__m256i a, __m128i count)</para>
        ///   <para>  VPSRLW ymm1,         ymm2, xmm3/m128</para>
        ///   <para>  VPSRLW ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<ushort> ShiftRightLogical(Vector256<ushort> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srl_epi32 (__m256i a, __m128i count)</para>
        ///   <para>  VPSRLD ymm1,         ymm2, xmm3/m128</para>
        ///   <para>  VPSRLD ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<int> ShiftRightLogical(Vector256<int> value, Vector128<int> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srl_epi32 (__m256i a, __m128i count)</para>
        ///   <para>  VPSRLD ymm1,         ymm2, xmm3/m128</para>
        ///   <para>  VPSRLD ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<uint> ShiftRightLogical(Vector256<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srl_epi64 (__m256i a, __m128i count)</para>
        ///   <para>  VPSRLQ ymm1,         ymm2, xmm3/m128</para>
        ///   <para>  VPSRLQ ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<long> ShiftRightLogical(Vector256<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srl_epi64 (__m256i a, __m128i count)</para>
        ///   <para>  VPSRLQ ymm1,         ymm2, xmm3/m128</para>
        ///   <para>  VPSRLQ ymm1 {k1}{z}, ymm2, xmm3/m128</para>
        /// </summary>
        public static Vector256<ulong> ShiftRightLogical(Vector256<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_srli_epi16 (__m256i a, int imm8)</para>
        ///   <para>  VPSRLW ymm1,         ymm2, imm8</para>
        ///   <para>  VPSRLW ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<short> ShiftRightLogical(Vector256<short> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srli_epi16 (__m256i a, int imm8)</para>
        ///   <para>  VPSRLW ymm1,         ymm2, imm8</para>
        ///   <para>  VPSRLW ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<ushort> ShiftRightLogical(Vector256<ushort> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srli_epi32 (__m256i a, int imm8)</para>
        ///   <para>  VPSRLD ymm1,         ymm2, imm8</para>
        ///   <para>  VPSRLD ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<int> ShiftRightLogical(Vector256<int> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srli_epi32 (__m256i a, int imm8)</para>
        ///   <para>  VPSRLD ymm1,         ymm2, imm8</para>
        ///   <para>  VPSRLD ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<uint> ShiftRightLogical(Vector256<uint> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srli_epi64 (__m256i a, int imm8)</para>
        ///   <para>  VPSRLQ ymm1,         ymm2, imm8</para>
        ///   <para>  VPSRLQ ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<long> ShiftRightLogical(Vector256<long> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srli_epi64 (__m256i a, int imm8)</para>
        ///   <para>  VPSRLQ ymm1,         ymm2, imm8</para>
        ///   <para>  VPSRLQ ymm1 {k1}{z}, ymm2, imm8</para>
        /// </summary>
        public static Vector256<ulong> ShiftRightLogical(Vector256<ulong> value, [ConstantExpected] byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSRLDQ ymm1, ymm2/m128, imm8</para>
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogical128BitLane(Vector256<sbyte> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSRLDQ ymm1, ymm2/m128, imm8</para>
        /// </summary>
        public static Vector256<byte> ShiftRightLogical128BitLane(Vector256<byte> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSRLDQ ymm1, ymm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSRLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<short> ShiftRightLogical128BitLane(Vector256<short> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSRLDQ ymm1, ymm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSRLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<ushort> ShiftRightLogical128BitLane(Vector256<ushort> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSRLDQ ymm1, ymm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSRLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<int> ShiftRightLogical128BitLane(Vector256<int> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSRLDQ ymm1, ymm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSRLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<uint> ShiftRightLogical128BitLane(Vector256<uint> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSRLDQ ymm1, ymm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSRLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<long> ShiftRightLogical128BitLane(Vector256<long> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)</para>
        ///   <para>  VPSRLDQ ymm1, ymm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSRLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector256<ulong> ShiftRightLogical128BitLane(Vector256<ulong> value, [ConstantExpected] byte numBytes) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_srlv_epi32 (__m128i a, __m128i count)</para>
        ///   <para>  VPSRLVD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSRLVD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> ShiftRightLogicalVariable(Vector128<int> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_srlv_epi32 (__m128i a, __m128i count)</para>
        ///   <para>  VPSRLVD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSRLVD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalVariable(Vector128<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_srlv_epi64 (__m128i a, __m128i count)</para>
        ///   <para>  VPSRLVQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSRLVQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> ShiftRightLogicalVariable(Vector128<long> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_srlv_epi64 (__m128i a, __m128i count)</para>
        ///   <para>  VPSRLVQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSRLVQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> ShiftRightLogicalVariable(Vector128<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srlv_epi32 (__m256i a, __m256i count)</para>
        ///   <para>  VPSRLVD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSRLVD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> ShiftRightLogicalVariable(Vector256<int> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srlv_epi32 (__m256i a, __m256i count)</para>
        ///   <para>  VPSRLVD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSRLVD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalVariable(Vector256<uint> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srlv_epi64 (__m256i a, __m256i count)</para>
        ///   <para>  VPSRLVQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSRLVQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> ShiftRightLogicalVariable(Vector256<long> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_srlv_epi64 (__m256i a, __m256i count)</para>
        ///   <para>  VPSRLVQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSRLVQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> ShiftRightLogicalVariable(Vector256<ulong> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_shuffle_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPSHUFB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSHUFB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> Shuffle(Vector256<sbyte> value, Vector256<sbyte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_shuffle_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPSHUFB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSHUFB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> Shuffle(Vector256<byte> value, Vector256<byte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_shuffle_epi32 (__m256i a, const int imm8)</para>
        ///   <para>  VPSHUFD ymm1,         ymm2/m256,         imm8</para>
        ///   <para>  VPSHUFD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<int> Shuffle(Vector256<int> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_shuffle_epi32 (__m256i a, const int imm8)</para>
        ///   <para>  VPSHUFD ymm1,         ymm2/m256,         imm8</para>
        ///   <para>  VPSHUFD ymm1 {k1}{z}, ymm2/m256/m32bcst, imm8</para>
        /// </summary>
        public static Vector256<uint> Shuffle(Vector256<uint> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_shufflehi_epi16 (__m256i a, const int imm8)</para>
        ///   <para>  VPSHUFHW ymm1,         ymm2/m256, imm8</para>
        ///   <para>  VPSHUFHW ymm1 {k1}{z}, ymm2/m256, imm8</para>
        /// </summary>
        public static Vector256<short> ShuffleHigh(Vector256<short> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_shufflehi_epi16 (__m256i a, const int imm8)</para>
        ///   <para>  VPSHUFHW ymm1,         ymm2/m256, imm8</para>
        ///   <para>  VPSHUFHW ymm1 {k1}{z}, ymm2/m256, imm8</para>
        /// </summary>
        public static Vector256<ushort> ShuffleHigh(Vector256<ushort> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_shufflelo_epi16 (__m256i a, const int imm8)</para>
        ///   <para>  VPSHUFLW ymm1,         ymm2/m256, imm8</para>
        ///   <para>  VPSHUFLW ymm1 {k1}{z}, ymm2/m256, imm8</para>
        /// </summary>
        public static Vector256<short> ShuffleLow(Vector256<short> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_shufflelo_epi16 (__m256i a, const int imm8)</para>
        ///   <para>  VPSHUFLW ymm1,         ymm2/m256, imm8</para>
        ///   <para>  VPSHUFLW ymm1 {k1}{z}, ymm2/m256, imm8</para>
        /// </summary>
        public static Vector256<ushort> ShuffleLow(Vector256<ushort> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_sign_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPSIGNB ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> Sign(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sign_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPSIGNW ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> Sign(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sign_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPSIGND ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<int> Sign(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_sub_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPSUBB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSUBB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> Subtract(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sub_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPSUBB ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSUBB ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> Subtract(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sub_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPSUBW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSUBW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> Subtract(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sub_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPSUBW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSUBW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> Subtract(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sub_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPSUBD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSUBD ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<int> Subtract(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sub_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPSUBD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSUBD ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<uint> Subtract(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sub_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPSUBQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSUBQ ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<long> Subtract(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_sub_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPSUBQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSUBQ ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ulong> Subtract(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_subs_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPSUBSB ymm1,         ymm2, ymm3/m128</para>
        ///   <para>  VPSUBSB ymm1 {k1}{z}, ymm2, ymm3/m128</para>
        /// </summary>
        public static Vector256<sbyte> SubtractSaturate(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_subs_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPSUBSW ymm1,         ymm2, ymm3/m128</para>
        ///   <para>  VPSUBSW ymm1 {k1}{z}, ymm2, ymm3/m128</para>
        /// </summary>
        public static Vector256<short> SubtractSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_subs_epu8 (__m256i a, __m256i b)</para>
        ///   <para>  VPSUBUSB ymm1,         ymm2, ymm3/m128</para>
        ///   <para>  VPSUBUSB ymm1 {k1}{z}, ymm2, ymm3/m128</para>
        /// </summary>
        public static Vector256<byte> SubtractSaturate(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_subs_epu16 (__m256i a, __m256i b)</para>
        ///   <para>  VPSUBUSW ymm1,         ymm2, ymm3/m128</para>
        ///   <para>  VPSUBUSW ymm1 {k1}{z}, ymm2, ymm3/m128</para>
        /// </summary>
        public static Vector256<ushort> SubtractSaturate(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_sad_epu8 (__m256i a, __m256i b)</para>
        ///   <para>  VPSADBW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPSADBW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> SumAbsoluteDifferences(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_unpackhi_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKHBW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKHBW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> UnpackHigh(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_unpackhi_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKHBW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKHBW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> UnpackHigh(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_unpackhi_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKHWD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKHWD ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> UnpackHigh(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_unpackhi_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKHWD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKHWD ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> UnpackHigh(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_unpackhi_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKHDQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKHDQ ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> UnpackHigh(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_unpackhi_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKHDQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKHDQ ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> UnpackHigh(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_unpackhi_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKHQDQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKHQDQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> UnpackHigh(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_unpackhi_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKHQDQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKHQDQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> UnpackHigh(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_unpacklo_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKLBW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKLBW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> UnpackLow(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_unpacklo_epi8 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKLBW ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKLBW ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> UnpackLow(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_unpacklo_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKLWD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKLWD ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> UnpackLow(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_unpacklo_epi16 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKLWD ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKLWD ymm1 {k1}{z}, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> UnpackLow(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_unpacklo_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKLDQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKLDQ ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> UnpackLow(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_unpacklo_epi32 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKLDQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKLDQ ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> UnpackLow(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_unpacklo_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKLQDQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKLQDQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> UnpackLow(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_unpacklo_epi64 (__m256i a, __m256i b)</para>
        ///   <para>  VPUNPCKLQDQ ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPUNPCKLQDQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> UnpackLow(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_xor_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPXOR ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<sbyte> Xor(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_xor_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPXOR ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<byte> Xor(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_xor_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPXOR ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<short> Xor(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_xor_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPXOR ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> Xor(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_xor_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPXOR  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPXORD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<int> Xor(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_xor_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPXOR  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPXORD ymm1 {k1}{z}, ymm2, ymm3/m256/m32bcst</para>
        /// </summary>
        public static Vector256<uint> Xor(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_xor_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPXOR  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPXORQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<long> Xor(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m256i _mm256_xor_si256 (__m256i a, __m256i b)</para>
        ///   <para>  VPXOR  ymm1,         ymm2, ymm3/m256</para>
        ///   <para>  VPXORQ ymm1 {k1}{z}, ymm2, ymm3/m256/m64bcst</para>
        /// </summary>
        public static Vector256<ulong> Xor(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }
    }
}
