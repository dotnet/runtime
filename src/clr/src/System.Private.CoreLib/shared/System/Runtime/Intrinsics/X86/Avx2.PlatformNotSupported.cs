// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel AVX2 hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public abstract class Avx2 : Avx
    {
        internal Avx2() { }

        public new static bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>
        /// __m256i _mm256_abs_epi8 (__m256i a)
        ///   VPABSB ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> Abs(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_abs_epi16 (__m256i a)
        ///   VPABSW ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> Abs(Vector256<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_abs_epi32 (__m256i a)
        ///   VPABSD ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> Abs(Vector256<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_add_epi8 (__m256i a, __m256i b)
        ///   VPADDB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> Add(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_add_epi8 (__m256i a, __m256i b)
        ///   VPADDB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> Add(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_add_epi16 (__m256i a, __m256i b)
        ///   VPADDW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> Add(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_add_epi16 (__m256i a, __m256i b)
        ///   VPADDW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> Add(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_add_epi32 (__m256i a, __m256i b)
        ///   VPADDD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> Add(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_add_epi32 (__m256i a, __m256i b)
        ///   VPADDD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> Add(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_add_epi64 (__m256i a, __m256i b)
        ///   VPADDQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<long> Add(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_add_epi64 (__m256i a, __m256i b)
        ///   VPADDQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ulong> Add(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_adds_epi8 (__m256i a, __m256i b)
        ///   VPADDSB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> AddSaturate(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_adds_epu8 (__m256i a, __m256i b)
        ///   VPADDUSB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> AddSaturate(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_adds_epi16 (__m256i a, __m256i b)
        ///   VPADDSW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> AddSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_adds_epu16 (__m256i a, __m256i b)
        ///   VPADDUSW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> AddSaturate(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)
        ///   VPALIGNR ymm, ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<sbyte> AlignRight(Vector256<sbyte> left, Vector256<sbyte> right, byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)
        ///   VPALIGNR ymm, ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<byte> AlignRight(Vector256<byte> left, Vector256<byte> right, byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)
        ///   VPALIGNR ymm, ymm, ymm/m256, imm8
        /// This intrinsic generates VPALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector256<short> AlignRight(Vector256<short> left, Vector256<short> right, byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)
        ///   VPALIGNR ymm, ymm, ymm/m256, imm8
        /// This intrinsic generates VPALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector256<ushort> AlignRight(Vector256<ushort> left, Vector256<ushort> right, byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)
        ///   VPALIGNR ymm, ymm, ymm/m256, imm8
        /// This intrinsic generates VPALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector256<int> AlignRight(Vector256<int> left, Vector256<int> right, byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)
        ///   VPALIGNR ymm, ymm, ymm/m256, imm8
        /// This intrinsic generates VPALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector256<uint> AlignRight(Vector256<uint> left, Vector256<uint> right, byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)
        ///   VPALIGNR ymm, ymm, ymm/m256, imm8
        /// This intrinsic generates VPALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector256<long> AlignRight(Vector256<long> left, Vector256<long> right, byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_alignr_epi8 (__m256i a, __m256i b, const int count)
        ///   VPALIGNR ymm, ymm, ymm/m256, imm8
        /// This intrinsic generates VPALIGNR that operates over bytes rather than elements of the vectors.
        /// </summary>
        public static Vector256<ulong> AlignRight(Vector256<ulong> left, Vector256<ulong> right, byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        ///   VPAND ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> And(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        ///   VPAND ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> And(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        ///   VPAND ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> And(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        ///   VPAND ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> And(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        ///   VPAND ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> And(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        ///   VPAND ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> And(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        ///   VPAND ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<long> And(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_and_si256 (__m256i a, __m256i b)
        ///   VPAND ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ulong> And(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        ///   VPANDN ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> AndNot(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        ///   VPANDN ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> AndNot(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        ///   VPANDN ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> AndNot(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        ///   VPANDN ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> AndNot(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        ///   VPANDN ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> AndNot(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        ///   VPANDN ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> AndNot(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        ///   VPANDN ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<long> AndNot(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_andnot_si256 (__m256i a, __m256i b)
        ///   VPANDN ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ulong> AndNot(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_avg_epu8 (__m256i a, __m256i b)
        ///   VPAVGB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> Average(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_avg_epu16 (__m256i a, __m256i b)
        ///   VPAVGW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> Average(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_blend_epi32 (__m128i a, __m128i b, const int imm8)
        ///   VPBLENDD xmm, xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<int> Blend(Vector128<int> left, Vector128<int> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_blend_epi32 (__m128i a, __m128i b, const int imm8)
        ///   VPBLENDD xmm, xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<uint> Blend(Vector128<uint> left, Vector128<uint> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_blend_epi16 (__m256i a, __m256i b, const int imm8)
        ///   VPBLENDW ymm, ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<short> Blend(Vector256<short> left, Vector256<short> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_blend_epi16 (__m256i a, __m256i b, const int imm8)
        ///   VPBLENDW ymm, ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<ushort> Blend(Vector256<ushort> left, Vector256<ushort> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_blend_epi32 (__m256i a, __m256i b, const int imm8)
        ///   VPBLENDD ymm, ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<int> Blend(Vector256<int> left, Vector256<int> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_blend_epi32 (__m256i a, __m256i b, const int imm8)
        ///   VPBLENDD ymm, ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<uint> Blend(Vector256<uint> left, Vector256<uint> right, byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)
        ///   VPBLENDVB ymm, ymm, ymm/m256, ymm
        /// </summary>
        public static Vector256<sbyte> BlendVariable(Vector256<sbyte> left, Vector256<sbyte> right, Vector256<sbyte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)
        ///   VPBLENDVB ymm, ymm, ymm/m256, ymm
        /// </summary>
        public static Vector256<byte> BlendVariable(Vector256<byte> left, Vector256<byte> right, Vector256<byte> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)
        ///   VPBLENDVB ymm, ymm, ymm/m256, ymm
        /// This intrinsic generates VPBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.
        /// </summary>
        public static Vector256<short> BlendVariable(Vector256<short> left, Vector256<short> right, Vector256<short> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)
        ///   VPBLENDVB ymm, ymm, ymm/m256, ymm
        /// This intrinsic generates VPBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.
        /// </summary>
        public static Vector256<ushort> BlendVariable(Vector256<ushort> left, Vector256<ushort> right, Vector256<ushort> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)
        ///   VPBLENDVB ymm, ymm, ymm/m256, ymm
        /// This intrinsic generates VPBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.
        /// </summary>
        public static Vector256<int> BlendVariable(Vector256<int> left, Vector256<int> right, Vector256<int> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)
        ///   VPBLENDVB ymm, ymm, ymm/m256, ymm
        /// This intrinsic generates VPBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.
        /// </summary>
        public static Vector256<uint> BlendVariable(Vector256<uint> left, Vector256<uint> right, Vector256<uint> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)
        ///   VPBLENDVB ymm, ymm, ymm/m256, ymm
        /// This intrinsic generates VPBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.
        /// </summary>
        public static Vector256<long> BlendVariable(Vector256<long> left, Vector256<long> right, Vector256<long> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_blendv_epi8 (__m256i a, __m256i b, __m256i mask)
        ///   VPBLENDVB ymm, ymm, ymm/m256, ymm
        /// This intrinsic generates VPBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.
        /// </summary>
        public static Vector256<ulong> BlendVariable(Vector256<ulong> left, Vector256<ulong> right, Vector256<ulong> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_broadcastb_epi8 (__m128i a)
        ///   VPBROADCASTB xmm, xmm
        /// </summary>
        public static Vector128<byte> BroadcastScalarToVector128(Vector128<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_broadcastb_epi8 (__m128i a)
        ///   VPBROADCASTB xmm, xmm
        /// </summary>
        public static Vector128<sbyte> BroadcastScalarToVector128(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_broadcastw_epi16 (__m128i a)
        ///   VPBROADCASTW xmm, xmm
        /// </summary>
        public static Vector128<short> BroadcastScalarToVector128(Vector128<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_broadcastw_epi16 (__m128i a)
        ///   VPBROADCASTW xmm, xmm
        /// </summary>
        public static Vector128<ushort> BroadcastScalarToVector128(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_broadcastd_epi32 (__m128i a)
        ///   VPBROADCASTD xmm, xmm
        /// </summary>
        public static Vector128<int> BroadcastScalarToVector128(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_broadcastd_epi32 (__m128i a)
        ///   VPBROADCASTD xmm, xmm
        /// </summary>
        public static Vector128<uint> BroadcastScalarToVector128(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_broadcastq_epi64 (__m128i a)
        ///   VPBROADCASTQ xmm, xmm
        /// </summary>
        public static Vector128<long> BroadcastScalarToVector128(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_broadcastq_epi64 (__m128i a)
        ///   VPBROADCASTQ xmm, xmm
        /// </summary>
        public static Vector128<ulong> BroadcastScalarToVector128(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_broadcastss_ps (__m128 a)
        ///   VBROADCASTSS xmm, xmm
        /// </summary>
        public static Vector128<float> BroadcastScalarToVector128(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_broadcastsd_pd (__m128d a)
        ///   VMOVDDUP xmm, xmm
        /// </summary>
        public static Vector128<double> BroadcastScalarToVector128(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_broadcastb_epi8 (__m128i a)
        ///   VPBROADCASTB xmm, m8
        /// The above native signature does not directly correspond to the managed signature. 
        /// We provide this additional overload for the lack of pointers to managed.
        /// </summary>
        public static unsafe Vector128<byte> BroadcastScalarToVector128(byte* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_broadcastb_epi8 (__m128i a)
        ///   VPBROADCASTB xmm, m8
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector128<sbyte> BroadcastScalarToVector128(sbyte* source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_broadcastw_epi16 (__m128i a)
        ///   VPBROADCASTW xmm, m16
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector128<short> BroadcastScalarToVector128(short* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_broadcastw_epi16 (__m128i a)
        ///   VPBROADCASTW xmm, m16
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector128<ushort> BroadcastScalarToVector128(ushort* source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_broadcastd_epi32 (__m128i a)
        ///   VPBROADCASTD xmm, m32
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector128<int> BroadcastScalarToVector128(int* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_broadcastd_epi32 (__m128i a)
        ///   VPBROADCASTD xmm, m32
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector128<uint> BroadcastScalarToVector128(uint* source) { throw new PlatformNotSupportedException(); }
        
        /// <summary>
        /// __m128i _mm_broadcastq_epi64 (__m128i a)
        ///   VPBROADCASTQ xmm, m64
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector128<long> BroadcastScalarToVector128(long* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_broadcastq_epi64 (__m128i a)
        ///   VPBROADCASTQ xmm, m64
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector128<ulong> BroadcastScalarToVector128(ulong* source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastb_epi8 (__m128i a)
        ///   VPBROADCASTB ymm, xmm
        /// </summary>
        public static Vector256<byte> BroadcastScalarToVector256(Vector128<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastb_epi8 (__m128i a)
        ///   VPBROADCASTB ymm, xmm
        /// </summary>
        public static Vector256<sbyte> BroadcastScalarToVector256(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastw_epi16 (__m128i a)
        ///   VPBROADCASTW ymm, xmm
        /// </summary>
        public static Vector256<short> BroadcastScalarToVector256(Vector128<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastw_epi16 (__m128i a)
        ///   VPBROADCASTW ymm, xmm
        /// </summary>
        public static Vector256<ushort> BroadcastScalarToVector256(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastd_epi32 (__m128i a)
        ///   VPBROADCASTD ymm, xmm
        /// </summary>
        public static Vector256<int> BroadcastScalarToVector256(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastd_epi32 (__m128i a)
        ///   VPBROADCASTD ymm, xmm
        /// </summary>
        public static Vector256<uint> BroadcastScalarToVector256(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastq_epi64 (__m128i a)
        ///   VPBROADCASTQ ymm, xmm
        /// </summary>
        public static Vector256<long> BroadcastScalarToVector256(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastq_epi64 (__m128i a)
        ///   VPBROADCASTQ ymm, xmm
        /// </summary>
        public static Vector256<ulong> BroadcastScalarToVector256(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256 _mm256_broadcastss_ps (__m128 a)
        ///   VBROADCASTSS ymm, xmm
        /// </summary>
        public static Vector256<float> BroadcastScalarToVector256(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256d _mm256_broadcastsd_pd (__m128d a)
        ///   VBROADCASTSD ymm, xmm
        /// </summary>
        public static Vector256<double> BroadcastScalarToVector256(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastb_epi8 (__m128i a)
        ///   VPBROADCASTB ymm, m8
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<byte> BroadcastScalarToVector256(byte* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastb_epi8 (__m128i a)
        ///   VPBROADCASTB ymm, m8
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<sbyte> BroadcastScalarToVector256(sbyte* source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastw_epi16 (__m128i a)
        ///   VPBROADCASTW ymm, m16
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<short> BroadcastScalarToVector256(short* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastw_epi16 (__m128i a)
        ///   VPBROADCASTW ymm, m16
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<ushort> BroadcastScalarToVector256(ushort* source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastd_epi32 (__m128i a)
        ///   VPBROADCASTD ymm, m32
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<int> BroadcastScalarToVector256(int* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastd_epi32 (__m128i a)
        ///   VPBROADCASTD ymm, m32
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<uint> BroadcastScalarToVector256(uint* source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastq_epi64 (__m128i a)
        ///   VPBROADCASTQ ymm, m64
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<long> BroadcastScalarToVector256(long* source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastq_epi64 (__m128i a)
        ///   VPBROADCASTQ ymm, m64
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<ulong> BroadcastScalarToVector256(ulong* source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        ///   VBROADCASTI128 ymm, m128
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<sbyte> BroadcastVector128ToVector256(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        ///   VBROADCASTI128 ymm, m128
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<byte> BroadcastVector128ToVector256(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        ///   VBROADCASTI128 ymm, m128
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<short> BroadcastVector128ToVector256(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        ///   VBROADCASTI128 ymm, m128
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<ushort> BroadcastVector128ToVector256(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        ///   VBROADCASTI128 ymm, m128
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<int> BroadcastVector128ToVector256(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        ///   VBROADCASTI128 ymm, m128
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<uint> BroadcastVector128ToVector256(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        ///   VBROADCASTI128 ymm, m128
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<long> BroadcastVector128ToVector256(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_broadcastsi128_si256 (__m128i a)
        ///   VBROADCASTI128 ymm, m128
        /// The above native signature does not directly correspond to the managed signature. 
        /// </summary>
        public static unsafe Vector256<ulong> BroadcastVector128ToVector256(ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpeq_epi8 (__m256i a, __m256i b)
        ///   VPCMPEQB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> CompareEqual(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpeq_epi8 (__m256i a, __m256i b)
        ///   VPCMPEQB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> CompareEqual(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpeq_epi16 (__m256i a, __m256i b)
        ///   VPCMPEQW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> CompareEqual(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpeq_epi16 (__m256i a, __m256i b)
        ///   VPCMPEQW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> CompareEqual(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpeq_epi32 (__m256i a, __m256i b)
        ///   VPCMPEQD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> CompareEqual(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpeq_epi32 (__m256i a, __m256i b)
        ///   VPCMPEQD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> CompareEqual(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpeq_epi64 (__m256i a, __m256i b)
        ///   VPCMPEQQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<long> CompareEqual(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpeq_epi64 (__m256i a, __m256i b)
        ///   VPCMPEQQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ulong> CompareEqual(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cmpgt_epi8 (__m256i a, __m256i b)
        ///   VPCMPGTB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> CompareGreaterThan(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpgt_epi16 (__m256i a, __m256i b)
        ///   VPCMPGTW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> CompareGreaterThan(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpgt_epi32 (__m256i a, __m256i b)
        ///   VPCMPGTD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> CompareGreaterThan(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cmpgt_epi64 (__m256i a, __m256i b)
        ///   VPCMPGTQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<long> CompareGreaterThan(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm256_cvtsi256_si32 (__m256i a)
        ///   MOVD reg/m32, xmm
        /// </summary>
        public static int ConvertToInt32(Vector256<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm256_cvtsi256_si32 (__m256i a)
        ///   MOVD reg/m32, xmm
        /// </summary>
        public static uint ConvertToUInt32(Vector256<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_cvtepi8_epi16 (__m128i a)
        ///   VPMOVSXBW ymm, xmm
        /// </summary>
        public static Vector256<short> ConvertToVector256Int16(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepu8_epi16 (__m128i a)
        ///   VPMOVZXBW ymm, xmm
        /// </summary>
        public static Vector256<short> ConvertToVector256Int16(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepi8_epi32 (__m128i a)
        ///   VPMOVSXBD ymm, xmm
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepu8_epi32 (__m128i a)
        ///   VPMOVZXBD ymm, xmm
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepi16_epi32 (__m128i a)
        ///   VPMOVSXWD ymm, xmm
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepu16_epi32 (__m128i a)
        ///   VPMOVZXWD ymm, xmm
        /// </summary>
        public static Vector256<int> ConvertToVector256Int32(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepi8_epi64 (__m128i a)
        ///   VPMOVSXBQ ymm, xmm
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepu8_epi64 (__m128i a)
        ///   VPMOVZXBQ ymm, xmm
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepi16_epi64 (__m128i a)
        ///   VPMOVSXWQ ymm, xmm
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepu16_epi64 (__m128i a)
        ///   VPMOVZXWQ ymm, xmm
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepi32_epi64 (__m128i a)
        ///   VPMOVSXDQ ymm, xmm
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_cvtepu32_epi64 (__m128i a)
        ///   VPMOVZXDQ ymm, xmm
        /// </summary>
        public static Vector256<long> ConvertToVector256Int64(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   VPMOVSXBW ymm, m128
        /// The native signature does not exist. We provide this additional overload for completeness.
        /// </summary>
        public static unsafe Vector256<short> ConvertToVector256Int16(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   VPMOVZXBW ymm, m128
        /// The native signature does not exist. We provide this additional overload for completeness.
        /// </summary>
        public static unsafe Vector256<short> ConvertToVector256Int16(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   VPMOVSXBD ymm, m64
        /// The native signature does not exist. We provide this additional overload for completeness.
        /// </summary>
        public static unsafe Vector256<int> ConvertToVector256Int32(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   VPMOVZXBD ymm, m64
        /// The native signature does not exist. We provide this additional overload for completeness.
        /// </summary>
        public static unsafe Vector256<int> ConvertToVector256Int32(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   VPMOVSXWD ymm, m128
        /// The native signature does not exist. We provide this additional overload for completeness.
        /// </summary>
        public static unsafe Vector256<int> ConvertToVector256Int32(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   VPMOVZXWD ymm, m128
        /// The native signature does not exist. We provide this additional overload for completeness.
        /// </summary>
        public static unsafe Vector256<int> ConvertToVector256Int32(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   VPMOVSXBQ ymm, m32
        /// The native signature does not exist. We provide this additional overload for completeness.
        /// </summary>
        public static unsafe Vector256<long> ConvertToVector256Int64(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   VPMOVZXBQ ymm, m32
        /// The native signature does not exist. We provide this additional overload for completeness.
        /// </summary>
        public static unsafe Vector256<long> ConvertToVector256Int64(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   VPMOVSXWQ ymm, m64
        /// The native signature does not exist. We provide this additional overload for completeness.
        /// </summary>
        public static unsafe Vector256<long> ConvertToVector256Int64(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   VPMOVZXWQ ymm, m64
        /// The native signature does not exist. We provide this additional overload for completeness.
        /// </summary>
        public static unsafe Vector256<long> ConvertToVector256Int64(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   VPMOVSXDQ ymm, m128
        /// The native signature does not exist. We provide this additional overload for completeness.
        /// </summary>
        public static unsafe Vector256<long> ConvertToVector256Int64(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   VPMOVZXDQ ymm, m128
        /// The native signature does not exist. We provide this additional overload for completeness.
        /// </summary>
        public static unsafe Vector256<long> ConvertToVector256Int64(uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        ///   VEXTRACTI128 xmm, ymm, imm8
        /// </summary>
        public new static Vector128<sbyte> ExtractVector128(Vector256<sbyte> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        ///   VEXTRACTI128 xmm, ymm, imm8
        /// </summary>
        public new static Vector128<byte> ExtractVector128(Vector256<byte> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        ///   VEXTRACTI128 xmm, ymm, imm8
        /// </summary>
        public new static Vector128<short> ExtractVector128(Vector256<short> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        ///   VEXTRACTI128 xmm, ymm, imm8
        /// </summary>
        public new static Vector128<ushort> ExtractVector128(Vector256<ushort> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        ///   VEXTRACTI128 xmm, ymm, imm8
        /// </summary>
        public new static Vector128<int> ExtractVector128(Vector256<int> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        ///   VEXTRACTI128 xmm, ymm, imm8
        /// </summary>
        public new static Vector128<uint> ExtractVector128(Vector256<uint> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        ///   VEXTRACTI128 xmm, ymm, imm8
        /// </summary>
        public new static Vector128<long> ExtractVector128(Vector256<long> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm256_extracti128_si256 (__m256i a, const int imm8)
        ///   VEXTRACTI128 xmm, ymm, imm8
        /// </summary>
        public new static Vector128<ulong> ExtractVector128(Vector256<ulong> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_i32gather_epi32 (int const* base_addr, __m128i vindex, const int scale)
        ///   VPGATHERDD xmm, vm32x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_i32gather_epi32 (int const* base_addr, __m128i vindex, const int scale)
        ///   VPGATHERDD xmm, vm32x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_i32gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)
        ///   VPGATHERDQ xmm, vm32x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<long> GatherVector128(long* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_i32gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)
        ///   VPGATHERDQ xmm, vm32x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<ulong> GatherVector128(ulong* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_i32gather_ps (float const* base_addr, __m128i vindex, const int scale)
        ///   VGATHERDPS xmm, vm32x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_i32gather_pd (double const* base_addr, __m128i vindex, const int scale)
        ///   VGATHERDPD xmm, vm32x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<double> GatherVector128(double* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_i64gather_epi32 (int const* base_addr, __m128i vindex, const int scale)
        ///   VPGATHERQD xmm, vm64x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector128<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_i64gather_epi32 (int const* base_addr, __m128i vindex, const int scale)
        ///   VPGATHERQD xmm, vm64x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector128<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_i64gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)
        ///   VPGATHERQQ xmm, vm64x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<long> GatherVector128(long* baseAddress, Vector128<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_i64gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)
        ///   VPGATHERQQ xmm, vm64x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<ulong> GatherVector128(ulong* baseAddress, Vector128<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_i64gather_ps (float const* base_addr, __m128i vindex, const int scale)
        ///   VGATHERQPS xmm, vm64x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector128<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_i64gather_pd (double const* base_addr, __m128i vindex, const int scale)
        ///   VGATHERQPD xmm, vm64x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<double> GatherVector128(double* baseAddress, Vector128<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_i32gather_epi32 (int const* base_addr, __m256i vindex, const int scale)
        ///   VPGATHERDD ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<int> GatherVector256(int* baseAddress, Vector256<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_i32gather_epi32 (int const* base_addr, __m256i vindex, const int scale)
        ///   VPGATHERDD ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<uint> GatherVector256(uint* baseAddress, Vector256<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_i32gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)
        ///   VPGATHERDQ ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<long> GatherVector256(long* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_i32gather_epi64 (__int64 const* base_addr, __m128i vindex, const int scale)
        ///   VPGATHERDQ ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<ulong> GatherVector256(ulong* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256 _mm256_i32gather_ps (float const* base_addr, __m256i vindex, const int scale)
        ///   VGATHERDPS ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<float> GatherVector256(float* baseAddress, Vector256<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_i32gather_pd (double const* base_addr, __m128i vindex, const int scale)
        ///   VGATHERDPD ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<double> GatherVector256(double* baseAddress, Vector128<int> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_i64gather_epi32 (int const* base_addr, __m256i vindex, const int scale)
        ///   VPGATHERQD xmm, vm64y, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<int> GatherVector128(int* baseAddress, Vector256<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_i64gather_epi32 (int const* base_addr, __m256i vindex, const int scale)
        ///   VPGATHERQD xmm, vm64y, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<uint> GatherVector128(uint* baseAddress, Vector256<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_i64gather_epi64 (__int64 const* base_addr, __m256i vindex, const int scale)
        ///   VPGATHERQQ ymm, vm64y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<long> GatherVector256(long* baseAddress, Vector256<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_i64gather_epi64 (__int64 const* base_addr, __m256i vindex, const int scale)
        ///   VPGATHERQQ ymm, vm64y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<ulong> GatherVector256(ulong* baseAddress, Vector256<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm256_i64gather_ps (float const* base_addr, __m256i vindex, const int scale)
        ///   VGATHERQPS xmm, vm64y, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<float> GatherVector128(float* baseAddress, Vector256<long> index, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_i64gather_pd (double const* base_addr, __m256i vindex, const int scale)
        ///   VGATHERQPD ymm, vm64y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<double> GatherVector256(double* baseAddress, Vector256<long> index, byte scale) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_mask_i32gather_epi32 (__m128i src, int const* base_addr, __m128i vindex, __m128i mask, const int scale)
        ///   VPGATHERDD xmm, vm32x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<int> GatherMaskVector128(Vector128<int> source, int* baseAddress, Vector128<int> index, Vector128<int> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mask_i32gather_epi32 (__m128i src, int const* base_addr, __m128i vindex, __m128i mask, const int scale)
        ///   VPGATHERDD xmm, vm32x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<uint> GatherMaskVector128(Vector128<uint> source, uint* baseAddress, Vector128<int> index, Vector128<uint> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mask_i32gather_epi64 (__m128i src, __int64 const* base_addr, __m128i vindex, __m128i mask, const int scale)
        ///   VPGATHERDQ xmm, vm32x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<long> GatherMaskVector128(Vector128<long> source, long* baseAddress, Vector128<int> index, Vector128<long> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mask_i32gather_epi64 (__m128i src, __int64 const* base_addr, __m128i vindex, __m128i mask, const int scale)
        ///   VPGATHERDQ xmm, vm32x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<ulong> GatherMaskVector128(Vector128<ulong> source, ulong* baseAddress, Vector128<int> index, Vector128<ulong> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_mask_i32gather_ps (__m128 src, float const* base_addr, __m128i vindex, __m128 mask, const int scale)
        ///   VGATHERDPS xmm, vm32x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<float> GatherMaskVector128(Vector128<float> source, float* baseAddress, Vector128<int> index, Vector128<float> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_mask_i32gather_pd (__m128d src, double const* base_addr, __m128i vindex, __m128d mask, const int scale)
        ///   VGATHERDPD xmm, vm32x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<double> GatherMaskVector128(Vector128<double> source, double* baseAddress, Vector128<int> index, Vector128<double> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mask_i64gather_epi32 (__m128i src, int const* base_addr, __m128i vindex, __m128i mask, const int scale)
        ///   VPGATHERQD xmm, vm64x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<int> GatherMaskVector128(Vector128<int> source, int* baseAddress, Vector128<long> index, Vector128<int> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mask_i64gather_epi32 (__m128i src, int const* base_addr, __m128i vindex, __m128i mask, const int scale)
        ///   VPGATHERQD xmm, vm64x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<uint> GatherMaskVector128(Vector128<uint> source, uint* baseAddress, Vector128<long> index, Vector128<uint> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mask_i64gather_epi64 (__m128i src, __int64 const* base_addr, __m128i vindex, __m128i mask, const int scale)
        ///   VPGATHERQQ xmm, vm64x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<long> GatherMaskVector128(Vector128<long> source, long* baseAddress, Vector128<long> index, Vector128<long> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mask_i64gather_epi64 (__m128i src, __int64 const* base_addr, __m128i vindex, __m128i mask, const int scale)
        ///   VPGATHERQQ xmm, vm64x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<ulong> GatherMaskVector128(Vector128<ulong> source, ulong* baseAddress, Vector128<long> index, Vector128<ulong> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_mask_i64gather_ps (__m128 src, float const* base_addr, __m128i vindex, __m128 mask, const int scale)
        ///   VGATHERQPS xmm, vm64x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<float> GatherMaskVector128(Vector128<float> source, float* baseAddress, Vector128<long> index, Vector128<float> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_mask_i64gather_pd (__m128d src, double const* base_addr, __m128i vindex, __m128d mask, const int scale)
        ///   VGATHERQPD xmm, vm64x, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<double> GatherMaskVector128(Vector128<double> source, double* baseAddress, Vector128<long> index, Vector128<double> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mask_i32gather_epi32 (__m256i src, int const* base_addr, __m256i vindex, __m256i mask, const int scale)
        ///   VPGATHERDD ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<int> GatherMaskVector256(Vector256<int> source, int* baseAddress, Vector256<int> index, Vector256<int> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mask_i32gather_epi32 (__m256i src, int const* base_addr, __m256i vindex, __m256i mask, const int scale)
        ///   VPGATHERDD ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<uint> GatherMaskVector256(Vector256<uint> source, uint* baseAddress, Vector256<int> index, Vector256<uint> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mask_i32gather_epi64 (__m256i src, __int64 const* base_addr, __m128i vindex, __m256i mask, const int scale)
        ///   VPGATHERDQ ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<long> GatherMaskVector256(Vector256<long> source, long* baseAddress, Vector128<int> index, Vector256<long> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mask_i32gather_epi64 (__m256i src, __int64 const* base_addr, __m128i vindex, __m256i mask, const int scale)
        ///   VPGATHERDQ ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<ulong> GatherMaskVector256(Vector256<ulong> source, ulong* baseAddress, Vector128<int> index, Vector256<ulong> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256 _mm256_mask_i32gather_ps (__m256 src, float const* base_addr, __m256i vindex, __m256 mask, const int scale)
        ///   VPGATHERDPS ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<float> GatherMaskVector256(Vector256<float> source, float* baseAddress, Vector256<int> index, Vector256<float> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_mask_i32gather_pd (__m256d src, double const* base_addr, __m128i vindex, __m256d mask, const int scale)
        ///   VPGATHERDPD ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<double> GatherMaskVector256(Vector256<double> source, double* baseAddress, Vector128<int> index, Vector256<double> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_mask_i64gather_epi32 (__m128i src, int const* base_addr, __m256i vindex, __m128i mask, const int scale)
        ///   VPGATHERQD xmm, vm32y, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<int> GatherMaskVector128(Vector128<int> source, int* baseAddress, Vector256<long> index, Vector128<int> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm256_mask_i64gather_epi32 (__m128i src, int const* base_addr, __m256i vindex, __m128i mask, const int scale)
        ///   VPGATHERQD xmm, vm32y, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<uint> GatherMaskVector128(Vector128<uint> source, uint* baseAddress, Vector256<long> index, Vector128<uint> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mask_i64gather_epi64 (__m256i src, __int64 const* base_addr, __m256i vindex, __m256i mask, const int scale)
        ///   VPGATHERQQ ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<long> GatherMaskVector256(Vector256<long> source, long* baseAddress, Vector256<long> index, Vector256<long> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mask_i64gather_epi64 (__m256i src, __int64 const* base_addr, __m256i vindex, __m256i mask, const int scale)
        ///   VPGATHERQQ ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<ulong> GatherMaskVector256(Vector256<ulong> source, ulong* baseAddress, Vector256<long> index, Vector256<ulong> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm256_mask_i64gather_ps (__m128 src, float const* base_addr, __m256i vindex, __m128 mask, const int scale)
        ///   VGATHERQPS xmm, vm32y, xmm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector128<float> GatherMaskVector128(Vector128<float> source, float* baseAddress, Vector256<long> index, Vector128<float> mask, byte scale) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_mask_i64gather_pd (__m256d src, double const* base_addr, __m256i vindex, __m256d mask, const int scale)
        ///   VGATHERQPD ymm, vm32y, ymm
        /// The scale parameter should be 1, 2, 4 or 8, otherwise, ArgumentOutOfRangeException will be thrown.
        /// </summary>
        public static unsafe Vector256<double> GatherMaskVector256(Vector256<double> source, double* baseAddress, Vector256<long> index, Vector256<double> mask, byte scale) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_hadd_epi16 (__m256i a, __m256i b)
        ///   VPHADDW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> HorizontalAdd(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_hadd_epi32 (__m256i a, __m256i b)
        ///   VPHADDD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> HorizontalAdd(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_hadds_epi16 (__m256i a, __m256i b)
        ///   VPHADDSW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> HorizontalAddSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_hsub_epi16 (__m256i a, __m256i b)
        ///   VPHSUBW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> HorizontalSubtract(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_hsub_epi32 (__m256i a, __m256i b)
        ///   VPHSUBD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> HorizontalSubtract(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_hsubs_epi16 (__m256i a, __m256i b)
        ///   VPHSUBSW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> HorizontalSubtractSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        ///   VINSERTI128 ymm, ymm, xmm, imm8
        /// </summary>
        public new static Vector256<sbyte> InsertVector128(Vector256<sbyte> value, Vector128<sbyte> data, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        ///   VINSERTI128 ymm, ymm, xmm, imm8
        /// </summary>
        public new static Vector256<byte> InsertVector128(Vector256<byte> value, Vector128<byte> data, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        ///   VINSERTI128 ymm, ymm, xmm, imm8
        /// </summary>
        public new static Vector256<short> InsertVector128(Vector256<short> value, Vector128<short> data, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        ///   VINSERTI128 ymm, ymm, xmm, imm8
        /// </summary>
        public new static Vector256<ushort> InsertVector128(Vector256<ushort> value, Vector128<ushort> data, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        ///   VINSERTI128 ymm, ymm, xmm, imm8
        /// </summary>
        public new static Vector256<int> InsertVector128(Vector256<int> value, Vector128<int> data, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        ///   VINSERTI128 ymm, ymm, xmm, imm8
        /// </summary>
        public new static Vector256<uint> InsertVector128(Vector256<uint> value, Vector128<uint> data, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        ///   VINSERTI128 ymm, ymm, xmm, imm8
        /// </summary>
        public new static Vector256<long> InsertVector128(Vector256<long> value, Vector128<long> data, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_inserti128_si256 (__m256i a, __m128i b, const int imm8)
        ///   VINSERTI128 ymm, ymm, xmm, imm8
        /// </summary>
        public new static Vector256<ulong> InsertVector128(Vector256<ulong> value, Vector128<ulong> data, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_stream_load_si256 (__m256i const* mem_addr)
        ///   VMOVNTDQA ymm, m256
        /// </summary>
        public static unsafe Vector256<sbyte> LoadAlignedVector256NonTemporal(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_stream_load_si256 (__m256i const* mem_addr)
        ///   VMOVNTDQA ymm, m256
        /// </summary>
        public static unsafe Vector256<byte> LoadAlignedVector256NonTemporal(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_stream_load_si256 (__m256i const* mem_addr)
        ///   VMOVNTDQA ymm, m256
        /// </summary>
        public static unsafe Vector256<short> LoadAlignedVector256NonTemporal(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_stream_load_si256 (__m256i const* mem_addr)
        ///   VMOVNTDQA ymm, m256
        /// </summary>
        public static unsafe Vector256<ushort> LoadAlignedVector256NonTemporal(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_stream_load_si256 (__m256i const* mem_addr)
        ///   VMOVNTDQA ymm, m256
        /// </summary>
        public static unsafe Vector256<int> LoadAlignedVector256NonTemporal(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_stream_load_si256 (__m256i const* mem_addr)
        ///   VMOVNTDQA ymm, m256
        /// </summary>
        public static unsafe Vector256<uint> LoadAlignedVector256NonTemporal(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_stream_load_si256 (__m256i const* mem_addr)
        ///   VMOVNTDQA ymm, m256
        /// </summary>
        public static unsafe Vector256<long> LoadAlignedVector256NonTemporal(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_stream_load_si256 (__m256i const* mem_addr)
        ///   VMOVNTDQA ymm, m256
        /// </summary>
        public static unsafe Vector256<ulong> LoadAlignedVector256NonTemporal(ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_maskload_epi32 (int const* mem_addr, __m128i mask)
        ///   VPMASKMOVD xmm, xmm, m128
        /// </summary>
        public static unsafe Vector128<int> MaskLoad(int* address, Vector128<int> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_maskload_epi32 (int const* mem_addr, __m128i mask)
        ///   VPMASKMOVD xmm, xmm, m128
        /// </summary>
        public static unsafe Vector128<uint> MaskLoad(uint* address, Vector128<uint> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_maskload_epi64 (__int64 const* mem_addr, __m128i mask)
        ///   VPMASKMOVQ xmm, xmm, m128
        /// </summary>
        public static unsafe Vector128<long> MaskLoad(long* address, Vector128<long> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_maskload_epi64 (__int64 const* mem_addr, __m128i mask)
        ///   VPMASKMOVQ xmm, xmm, m128
        /// </summary>
        public static unsafe Vector128<ulong> MaskLoad(ulong* address, Vector128<ulong> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_maskload_epi32 (int const* mem_addr, __m256i mask)
        ///   VPMASKMOVD ymm, ymm, m256
        /// </summary>
        public static unsafe Vector256<int> MaskLoad(int* address, Vector256<int> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_maskload_epi32 (int const* mem_addr, __m256i mask)
        ///   VPMASKMOVD ymm, ymm, m256
        /// </summary>
        public static unsafe Vector256<uint> MaskLoad(uint* address, Vector256<uint> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_maskload_epi64 (__int64 const* mem_addr, __m256i mask)
        ///   VPMASKMOVQ ymm, ymm, m256
        /// </summary>
        public static unsafe Vector256<long> MaskLoad(long* address, Vector256<long> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_maskload_epi64 (__int64 const* mem_addr, __m256i mask)
        ///   VPMASKMOVQ ymm, ymm, m256
        /// </summary>
        public static unsafe Vector256<ulong> MaskLoad(ulong* address, Vector256<ulong> mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_maskstore_epi32 (int* mem_addr, __m128i mask, __m128i a)
        ///   VPMASKMOVD m128, xmm, xmm
        /// </summary>
        public static unsafe void MaskStore(int* address, Vector128<int> mask, Vector128<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_maskstore_epi32 (int* mem_addr, __m128i mask, __m128i a)
        ///   VPMASKMOVD m128, xmm, xmm
        /// </summary>
        public static unsafe void MaskStore(uint* address, Vector128<uint> mask, Vector128<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_maskstore_epi64 (__int64* mem_addr, __m128i mask, __m128i a)
        ///   VPMASKMOVQ m128, xmm, xmm
        /// </summary>
        public static unsafe void MaskStore(long* address, Vector128<long> mask, Vector128<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_maskstore_epi64 (__int64* mem_addr, __m128i mask, __m128i a)
        ///   VPMASKMOVQ m128, xmm, xmm
        /// </summary>
        public static unsafe void MaskStore(ulong* address, Vector128<ulong> mask, Vector128<ulong> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm256_maskstore_epi32 (int* mem_addr, __m256i mask, __m256i a)
        ///   VPMASKMOVD m256, ymm, ymm
        /// </summary>
        public static unsafe void MaskStore(int* address, Vector256<int> mask, Vector256<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm256_maskstore_epi32 (int* mem_addr, __m256i mask, __m256i a)
        ///   VPMASKMOVD m256, ymm, ymm
        /// </summary>
        public static unsafe void MaskStore(uint* address, Vector256<uint> mask, Vector256<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm256_maskstore_epi64 (__int64* mem_addr, __m256i mask, __m256i a)
        ///   VPMASKMOVQ m256, ymm, ymm
        /// </summary>
        public static unsafe void MaskStore(long* address, Vector256<long> mask, Vector256<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm256_maskstore_epi64 (__int64* mem_addr, __m256i mask, __m256i a)
        ///   VPMASKMOVQ m256, ymm, ymm
        /// </summary>
        public static unsafe void MaskStore(ulong* address, Vector256<ulong> mask, Vector256<ulong> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_madd_epi16 (__m256i a, __m256i b)
        ///   VPMADDWD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> MultiplyAddAdjacent(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_maddubs_epi16 (__m256i a, __m256i b)
        ///   VPMADDUBSW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> MultiplyAddAdjacent(Vector256<byte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_max_epi8 (__m256i a, __m256i b)
        ///   VPMAXSB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> Max(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_max_epu8 (__m256i a, __m256i b)
        ///   VPMAXUB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> Max(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_max_epi16 (__m256i a, __m256i b)
        ///   VPMAXSW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> Max(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_max_epu16 (__m256i a, __m256i b)
        ///   VPMAXUW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> Max(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_max_epi32 (__m256i a, __m256i b)
        ///   VPMAXSD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> Max(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_max_epu32 (__m256i a, __m256i b)
        ///   VPMAXUD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> Max(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_min_epi8 (__m256i a, __m256i b)
        ///   VPMINSB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> Min(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_min_epu8 (__m256i a, __m256i b)
        ///   VPMINUB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> Min(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_min_epi16 (__m256i a, __m256i b)
        ///   VPMINSW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> Min(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_min_epu16 (__m256i a, __m256i b)
        ///   VPMINUW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> Min(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_min_epi32 (__m256i a, __m256i b)
        ///   VPMINSD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> Min(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_min_epu32 (__m256i a, __m256i b)
        ///   VPMINUD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> Min(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm256_movemask_epi8 (__m256i a)
        ///   VPMOVMSKB reg, ymm
        /// </summary>
        public static int MoveMask(Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm256_movemask_epi8 (__m256i a)
        ///   VPMOVMSKB reg, ymm
        /// </summary>
        public static int MoveMask(Vector256<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_mpsadbw_epu8 (__m256i a, __m256i b, const int imm8)
        ///   VMPSADBW ymm, ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<ushort> MultipleSumAbsoluteDifferences(Vector256<byte> left, Vector256<byte> right, byte mask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_mul_epi32 (__m256i a, __m256i b)
        ///   VPMULDQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<long> Multiply(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mul_epu32 (__m256i a, __m256i b)
        ///   VPMULUDQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ulong> Multiply(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_mulhi_epi16 (__m256i a, __m256i b)
        ///   VPMULHW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> MultiplyHigh(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mulhi_epu16 (__m256i a, __m256i b)
        ///   VPMULHUW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> MultiplyHigh(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_mulhrs_epi16 (__m256i a, __m256i b)
        ///   VPMULHRSW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> MultiplyHighRoundScale(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_mullo_epi16 (__m256i a, __m256i b)
        ///   VPMULLW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> MultiplyLow(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mullo_epi16 (__m256i a, __m256i b)
        ///   VPMULLW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> MultiplyLow(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_mullo_epi32 (__m256i a, __m256i b)
        ///   VPMULLD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> MultiplyLow(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_mullo_epi32 (__m256i a, __m256i b)
        ///   VPMULLD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> MultiplyLow(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        ///   VPOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> Or(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        ///   VPOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> Or(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        ///   VPOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> Or(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        ///   VPOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> Or(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        ///   VPOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> Or(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        ///   VPOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> Or(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        ///   VPOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<long> Or(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_or_si256 (__m256i a, __m256i b)
        ///   VPOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ulong> Or(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_packs_epi16 (__m256i a, __m256i b)
        ///   VPACKSSWB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> PackSignedSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_packs_epi32 (__m256i a, __m256i b)
        ///   VPACKSSDW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> PackSignedSaturate(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_packus_epi16 (__m256i a, __m256i b)
        ///   VPACKUSWB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> PackUnsignedSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_packus_epi32 (__m256i a, __m256i b)
        ///   VPACKUSDW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> PackUnsignedSaturate(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        ///   VPERM2I128 ymm, ymm, ymm/m256, imm8
        /// </summary>
        public new static Vector256<sbyte> Permute2x128(Vector256<sbyte> left, Vector256<sbyte> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        ///   VPERM2I128 ymm, ymm, ymm/m256, imm8
        /// </summary>
        public new static Vector256<byte> Permute2x128(Vector256<byte> left, Vector256<byte> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        ///   VPERM2I128 ymm, ymm, ymm/m256, imm8
        /// </summary>
        public new static Vector256<short> Permute2x128(Vector256<short> left, Vector256<short> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        ///   VPERM2I128 ymm, ymm, ymm/m256, imm8
        /// </summary>
        public new static Vector256<ushort> Permute2x128(Vector256<ushort> left, Vector256<ushort> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        ///   VPERM2I128 ymm, ymm, ymm/m256, imm8
        /// </summary>
        public new static Vector256<int> Permute2x128(Vector256<int> left, Vector256<int> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        ///   VPERM2I128 ymm, ymm, ymm/m256, imm8
        /// </summary>
        public new static Vector256<uint> Permute2x128(Vector256<uint> left, Vector256<uint> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        ///   VPERM2I128 ymm, ymm, ymm/m256, imm8
        /// </summary>
        public new static Vector256<long> Permute2x128(Vector256<long> left, Vector256<long> right, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute2x128_si256 (__m256i a, __m256i b, const int imm8)
        ///   VPERM2I128 ymm, ymm, ymm/m256, imm8
        /// </summary>
        public new static Vector256<ulong> Permute2x128(Vector256<ulong> left, Vector256<ulong> right, byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permute4x64_epi64 (__m256i a, const int imm8)
        ///   VPERMQ ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<long> Permute4x64(Vector256<long> value, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permute4x64_epi64 (__m256i a, const int imm8)
        ///   VPERMQ ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<ulong> Permute4x64(Vector256<ulong> value, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256d _mm256_permute4x64_pd (__m256d a, const int imm8)
        ///   VPERMPD ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<double> Permute4x64(Vector256<double> value, byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_permutevar8x32_epi32 (__m256i a, __m256i idx)
        ///   VPERMD ymm, ymm/m256, ymm
        /// </summary>
        public static Vector256<int> PermuteVar8x32(Vector256<int> left, Vector256<int> control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_permutevar8x32_epi32 (__m256i a, __m256i idx)
        ///   VPERMD ymm, ymm/m256, ymm
        /// </summary>
        public static Vector256<uint> PermuteVar8x32(Vector256<uint> left, Vector256<uint> control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256 _mm256_permutevar8x32_ps (__m256 a, __m256i idx)
        ///   VPERMPS ymm, ymm/m256, ymm
        /// </summary>
        public static Vector256<float> PermuteVar8x32(Vector256<float> left, Vector256<int> control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_sll_epi16 (__m256i a, __m128i count)
        ///   VPSLLW ymm, ymm, xmm/m128
        /// </summary>
        public static Vector256<short> ShiftLeftLogical(Vector256<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sll_epi16 (__m256i a, __m128i count)
        ///   VPSLLW ymm, ymm, xmm/m128
        /// </summary>
        public static Vector256<ushort> ShiftLeftLogical(Vector256<ushort> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sll_epi32 (__m256i a, __m128i count)
        ///   VPSLLD ymm, ymm, xmm/m128
        /// </summary>
        public static Vector256<int> ShiftLeftLogical(Vector256<int> value, Vector128<int> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sll_epi32 (__m256i a, __m128i count)
        ///   VPSLLD ymm, ymm, xmm/m128
        /// </summary>
        public static Vector256<uint> ShiftLeftLogical(Vector256<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sll_epi64 (__m256i a, __m128i count)
        ///   VPSLLQ ymm, ymm, xmm/m128
        /// </summary>
        public static Vector256<long> ShiftLeftLogical(Vector256<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sll_epi64 (__m256i a, __m128i count)
        ///   VPSLLQ ymm, ymm, xmm/m128
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogical(Vector256<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_slli_epi16 (__m256i a, int imm8)
        ///   VPSLLW ymm, ymm, imm8
        /// </summary>
        public static Vector256<short> ShiftLeftLogical(Vector256<short> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_slli_epi16 (__m256i a, int imm8)
        ///   VPSLLW ymm, ymm, imm8
        /// </summary>
        public static Vector256<ushort> ShiftLeftLogical(Vector256<ushort> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_slli_epi32 (__m256i a, int imm8)
        ///   VPSLLD ymm, ymm, imm8
        /// </summary>
        public static Vector256<int> ShiftLeftLogical(Vector256<int> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_slli_epi32 (__m256i a, int imm8)
        ///   VPSLLD ymm, ymm, imm8
        /// </summary>
        public static Vector256<uint> ShiftLeftLogical(Vector256<uint> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_slli_epi64 (__m256i a, int imm8)
        ///   VPSLLQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<long> ShiftLeftLogical(Vector256<long> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_slli_epi64 (__m256i a, int imm8)
        ///   VPSLLQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogical(Vector256<ulong> value, byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        ///   VPSLLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<sbyte> ShiftLeftLogical128BitLane(Vector256<sbyte> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        ///   VPSLLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<byte> ShiftLeftLogical128BitLane(Vector256<byte> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        ///   VPSLLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<short> ShiftLeftLogical128BitLane(Vector256<short> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        ///   VPSLLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<ushort> ShiftLeftLogical128BitLane(Vector256<ushort> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        ///   VPSLLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<int> ShiftLeftLogical128BitLane(Vector256<int> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        ///   VPSLLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<uint> ShiftLeftLogical128BitLane(Vector256<uint> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        ///   VPSLLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<long> ShiftLeftLogical128BitLane(Vector256<long> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bslli_epi128 (__m256i a, const int imm8)
        ///   VPSLLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogical128BitLane(Vector256<ulong> value, byte numBytes) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_sllv_epi32 (__m256i a, __m256i count)
        ///   VPSLLVD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> ShiftLeftLogicalVariable(Vector256<int> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sllv_epi32 (__m256i a, __m256i count)
        ///   VPSLLVD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> ShiftLeftLogicalVariable(Vector256<uint> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sllv_epi64 (__m256i a, __m256i count)
        ///   VPSLLVQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<long> ShiftLeftLogicalVariable(Vector256<long> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sllv_epi64 (__m256i a, __m256i count)
        ///   VPSLLVQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogicalVariable(Vector256<ulong> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_sllv_epi32 (__m128i a, __m128i count)
        ///   VPSLLVD xmm, ymm, xmm/m128
        /// </summary>
        public static Vector128<int> ShiftLeftLogicalVariable(Vector128<int> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sllv_epi32 (__m128i a, __m128i count)
        ///   VPSLLVD xmm, ymm, xmm/m128
        /// </summary>
        public static Vector128<uint> ShiftLeftLogicalVariable(Vector128<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sllv_epi64 (__m128i a, __m128i count)
        ///   VPSLLVQ xmm, ymm, xmm/m128
        /// </summary>
        public static Vector128<long> ShiftLeftLogicalVariable(Vector128<long> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sllv_epi64 (__m128i a, __m128i count)
        ///   VPSLLVQ xmm, ymm, xmm/m128
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogicalVariable(Vector128<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// _mm256_sra_epi16 (__m256i a, __m128i count)
        ///   VPSRAW ymm, ymm, xmm/m128
        /// </summary>
        public static Vector256<short> ShiftRightArithmetic(Vector256<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// _mm256_sra_epi32 (__m256i a, __m128i count)
        ///   VPSRAD ymm, ymm, xmm/m128
        /// </summary>
        public static Vector256<int> ShiftRightArithmetic(Vector256<int> value, Vector128<int> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_srai_epi16 (__m256i a, int imm8)
        ///   VPSRAW ymm, ymm, imm8
        /// </summary>
        public static Vector256<short> ShiftRightArithmetic(Vector256<short> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srai_epi32 (__m256i a, int imm8)
        ///   VPSRAD ymm, ymm, imm8
        /// </summary>
        public static Vector256<int> ShiftRightArithmetic(Vector256<int> value, byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_srav_epi32 (__m256i a, __m256i count)
        ///   VPSRAVD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> ShiftRightArithmeticVariable(Vector256<int> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_srav_epi32 (__m128i a, __m128i count)
        ///   VPSRAVD xmm, xmm, xmm/m128
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticVariable(Vector128<int> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_srl_epi16 (__m256i a, __m128i count)
        ///   VPSRLW ymm, ymm, xmm/m128
        /// </summary>
        public static Vector256<short> ShiftRightLogical(Vector256<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srl_epi16 (__m256i a, __m128i count)
        ///   VPSRLW ymm, ymm, xmm/m128
        /// </summary>
        public static Vector256<ushort> ShiftRightLogical(Vector256<ushort> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srl_epi32 (__m256i a, __m128i count)
        ///   VPSRLD ymm, ymm, xmm/m128
        /// </summary>
        public static Vector256<int> ShiftRightLogical(Vector256<int> value, Vector128<int> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srl_epi32 (__m256i a, __m128i count)
        ///   VPSRLD ymm, ymm, xmm/m128
        /// </summary>
        public static Vector256<uint> ShiftRightLogical(Vector256<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srl_epi64 (__m256i a, __m128i count)
        ///   VPSRLQ ymm, ymm, xmm/m128
        /// </summary>
        public static Vector256<long> ShiftRightLogical(Vector256<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srl_epi64 (__m256i a, __m128i count)
        ///   VPSRLQ ymm, ymm, xmm/m128
        /// </summary>
        public static Vector256<ulong> ShiftRightLogical(Vector256<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_srli_epi16 (__m256i a, int imm8)
        ///   VPSRLW ymm, ymm, imm8
        /// </summary>
        public static Vector256<short> ShiftRightLogical(Vector256<short> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srli_epi16 (__m256i a, int imm8)
        ///   VPSRLW ymm, ymm, imm8
        /// </summary>
        public static Vector256<ushort> ShiftRightLogical(Vector256<ushort> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srli_epi32 (__m256i a, int imm8)
        ///   VPSRLD ymm, ymm, imm8
        /// </summary>
        public static Vector256<int> ShiftRightLogical(Vector256<int> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srli_epi32 (__m256i a, int imm8)
        ///   VPSRLD ymm, ymm, imm8
        /// </summary>
        public static Vector256<uint> ShiftRightLogical(Vector256<uint> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srli_epi64 (__m256i a, int imm8)
        ///   VPSRLQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<long> ShiftRightLogical(Vector256<long> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srli_epi64 (__m256i a, int imm8)
        ///   VPSRLQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<ulong> ShiftRightLogical(Vector256<ulong> value, byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        ///   VPSRLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogical128BitLane(Vector256<sbyte> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        ///   VPSRLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<byte> ShiftRightLogical128BitLane(Vector256<byte> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        ///   VPSRLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<short> ShiftRightLogical128BitLane(Vector256<short> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        ///   VPSRLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<ushort> ShiftRightLogical128BitLane(Vector256<ushort> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        ///   VPSRLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<int> ShiftRightLogical128BitLane(Vector256<int> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        ///   VPSRLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<uint> ShiftRightLogical128BitLane(Vector256<uint> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        ///   VPSRLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<long> ShiftRightLogical128BitLane(Vector256<long> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_bsrli_epi128 (__m256i a, const int imm8)
        ///   VPSRLDQ ymm, ymm, imm8
        /// </summary>
        public static Vector256<ulong> ShiftRightLogical128BitLane(Vector256<ulong> value, byte numBytes) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_srlv_epi32 (__m256i a, __m256i count)
        ///   VPSRLVD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> ShiftRightLogicalVariable(Vector256<int> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srlv_epi32 (__m256i a, __m256i count)
        ///   VPSRLVD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalVariable(Vector256<uint> value, Vector256<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srlv_epi64 (__m256i a, __m256i count)
        ///   VPSRLVQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<long> ShiftRightLogicalVariable(Vector256<long> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_srlv_epi64 (__m256i a, __m256i count)
        ///   VPSRLVQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ulong> ShiftRightLogicalVariable(Vector256<ulong> value, Vector256<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_srlv_epi32 (__m128i a, __m128i count)
        ///   VPSRLVD xmm, xmm, xmm/m128
        /// </summary>
        public static Vector128<int> ShiftRightLogicalVariable(Vector128<int> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_srlv_epi32 (__m128i a, __m128i count)
        ///   VPSRLVD xmm, xmm, xmm/m128
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalVariable(Vector128<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_srlv_epi64 (__m128i a, __m128i count)
        ///   VPSRLVQ xmm, xmm, xmm/m128
        /// </summary>
        public static Vector128<long> ShiftRightLogicalVariable(Vector128<long> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_srlv_epi64 (__m128i a, __m128i count)
        ///   VPSRLVQ xmm, xmm, xmm/m128
        /// </summary>
        public static Vector128<ulong> ShiftRightLogicalVariable(Vector128<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_shuffle_epi8 (__m256i a, __m256i b)
        ///   VPSHUFB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> Shuffle(Vector256<sbyte> value, Vector256<sbyte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_shuffle_epi8 (__m256i a, __m256i b)
        ///   VPSHUFB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> Shuffle(Vector256<byte> value, Vector256<byte> mask) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_shuffle_epi32 (__m256i a, const int imm8)
        ///   VPSHUFD ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<int> Shuffle(Vector256<int> value, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_shuffle_epi32 (__m256i a, const int imm8)
        ///   VPSHUFD ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<uint> Shuffle(Vector256<uint> value, byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_shufflehi_epi16 (__m256i a, const int imm8)
        ///   VPSHUFHW ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<short> ShuffleHigh(Vector256<short> value, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_shufflehi_epi16 (__m256i a, const int imm8)
        ///   VPSHUFHW ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<ushort> ShuffleHigh(Vector256<ushort> value, byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_shufflelo_epi16 (__m256i a, const int imm8)
        ///   VPSHUFLW ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<short> ShuffleLow(Vector256<short> value, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_shufflelo_epi16 (__m256i a, const int imm8)
        ///   VPSHUFLW ymm, ymm/m256, imm8
        /// </summary>
        public static Vector256<ushort> ShuffleLow(Vector256<ushort> value, byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_sign_epi8 (__m256i a, __m256i b)
        ///   VPSIGNB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> Sign(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sign_epi16 (__m256i a, __m256i b)
        ///   VPSIGNW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> Sign(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sign_epi32 (__m256i a, __m256i b)
        ///   VPSIGND ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> Sign(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_sub_epi8 (__m256i a, __m256i b)
        ///   VPSUBB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> Subtract(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sub_epi8 (__m256i a, __m256i b)
        ///   VPSUBB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> Subtract(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sub_epi16 (__m256i a, __m256i b)
        ///   VPSUBW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> Subtract(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sub_epi16 (__m256i a, __m256i b)
        ///   VPSUBW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> Subtract(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sub_epi32 (__m256i a, __m256i b)
        ///   VPSUBD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> Subtract(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sub_epi32 (__m256i a, __m256i b)
        ///   VPSUBD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> Subtract(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sub_epi64 (__m256i a, __m256i b)
        ///   VPSUBQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<long> Subtract(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_sub_epi64 (__m256i a, __m256i b)
        ///   VPSUBQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ulong> Subtract(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_subs_epi8 (__m256i a, __m256i b)
        ///   VPSUBSB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> SubtractSaturate(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_subs_epi16 (__m256i a, __m256i b)
        ///   VPSUBSW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> SubtractSaturate(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_subs_epu8 (__m256i a, __m256i b)
        ///   VPSUBUSB ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> SubtractSaturate(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_subs_epu16 (__m256i a, __m256i b)
        ///   VPSUBUSW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> SubtractSaturate(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_sad_epu8 (__m256i a, __m256i b)
        ///   VPSADBW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> SumAbsoluteDifferences(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_unpackhi_epi8 (__m256i a, __m256i b)
        ///   VPUNPCKHBW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> UnpackHigh(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpackhi_epi8 (__m256i a, __m256i b)
        ///   VPUNPCKHBW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> UnpackHigh(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpackhi_epi16 (__m256i a, __m256i b)
        ///   VPUNPCKHWD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> UnpackHigh(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpackhi_epi16 (__m256i a, __m256i b)
        ///   VPUNPCKHWD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> UnpackHigh(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpackhi_epi32 (__m256i a, __m256i b)
        ///   VPUNPCKHDQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> UnpackHigh(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpackhi_epi32 (__m256i a, __m256i b)
        ///   VPUNPCKHDQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> UnpackHigh(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpackhi_epi64 (__m256i a, __m256i b)
        ///   VPUNPCKHQDQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<long> UnpackHigh(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpackhi_epi64 (__m256i a, __m256i b)
        ///   VPUNPCKHQDQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ulong> UnpackHigh(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_unpacklo_epi8 (__m256i a, __m256i b)
        ///   VPUNPCKLBW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> UnpackLow(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpacklo_epi8 (__m256i a, __m256i b)
        ///   VPUNPCKLBW ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> UnpackLow(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpacklo_epi16 (__m256i a, __m256i b)
        ///   VPUNPCKLWD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> UnpackLow(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpacklo_epi16 (__m256i a, __m256i b)
        ///   VPUNPCKLWD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> UnpackLow(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpacklo_epi32 (__m256i a, __m256i b)
        ///   VPUNPCKLDQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> UnpackLow(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpacklo_epi32 (__m256i a, __m256i b)
        ///   VPUNPCKLDQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> UnpackLow(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpacklo_epi64 (__m256i a, __m256i b)
        ///   VPUNPCKLQDQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<long> UnpackLow(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_unpacklo_epi64 (__m256i a, __m256i b)
        ///   VPUNPCKLQDQ ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ulong> UnpackLow(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        ///   VPXOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<sbyte> Xor(Vector256<sbyte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        ///   VPXOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<byte> Xor(Vector256<byte> left, Vector256<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        ///   VPXOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<short> Xor(Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        ///   VPXOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ushort> Xor(Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        ///   VPXOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> Xor(Vector256<int> left, Vector256<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        ///   VPXOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<uint> Xor(Vector256<uint> left, Vector256<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        ///   VPXOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<long> Xor(Vector256<long> left, Vector256<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m256i _mm256_xor_si256 (__m256i a, __m256i b)
        ///   VPXOR ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<ulong> Xor(Vector256<ulong> left, Vector256<ulong> right) { throw new PlatformNotSupportedException(); }
    }
}
