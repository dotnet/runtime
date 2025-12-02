// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 SSE4.1 hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Sse41 : Ssse3
    {
        internal Sse41() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the x86 SSE4.1 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : Ssse3.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>__int64 _mm_extract_epi64 (__m128i a, const int imm8)</para>
            ///   <para>   PEXTRQ r/m64, xmm1, imm8</para>
            ///   <para>  VPEXTRQ r/m64, xmm1, imm8</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static long Extract(Vector128<long> value, [ConstantExpected] byte index) => Extract(value, index);
            /// <summary>
            ///   <para>__int64 _mm_extract_epi64 (__m128i a, const int imm8)</para>
            ///   <para>   PEXTRQ r/m64, xmm1, imm8</para>
            ///   <para>  VPEXTRQ r/m64, xmm1, imm8</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong Extract(Vector128<ulong> value, [ConstantExpected] byte index) => Extract(value, index);

            /// <summary>
            ///   <para>__m128i _mm_insert_epi64 (__m128i a, __int64 i, const int imm8)</para>
            ///   <para>   PINSRQ xmm1,       r/m64, imm8</para>
            ///   <para>  VPINSRQ xmm1, xmm2, r/m64, imm8</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<long> Insert(Vector128<long> value, long data, [ConstantExpected] byte index) => Insert(value, data, index);
            /// <summary>
            ///   <para>__m128i _mm_insert_epi64 (__m128i a, __int64 i, const int imm8)</para>
            ///   <para>   PINSRQ xmm1,       r/m64, imm8</para>
            ///   <para>  VPINSRQ xmm1, xmm2, r/m64, imm8</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<ulong> Insert(Vector128<ulong> value, ulong data, [ConstantExpected] byte index) => Insert(value, data, index);
        }

        /// <summary>
        ///   <para>__m128i _mm_blend_epi16 (__m128i a, __m128i b, const int imm8)</para>
        ///   <para>   PBLENDW xmm1,       xmm2/m128 imm8</para>
        ///   <para>  VPBLENDW xmm1, xmm2, xmm3/m128 imm8</para>
        /// </summary>
        public static Vector128<short> Blend(Vector128<short> left, Vector128<short> right, [ConstantExpected] byte control) => Blend(left, right, control);
        /// <summary>
        ///   <para>__m128i _mm_blend_epi16 (__m128i a, __m128i b, const int imm8)</para>
        ///   <para>   PBLENDW xmm1,       xmm2/m128 imm8</para>
        ///   <para>  VPBLENDW xmm1, xmm2, xmm3/m128 imm8</para>
        /// </summary>
        public static Vector128<ushort> Blend(Vector128<ushort> left, Vector128<ushort> right, [ConstantExpected] byte control) => Blend(left, right, control);
        /// <summary>
        ///   <para>__m128 _mm_blend_ps (__m128 a, __m128 b, const int imm8)</para>
        ///   <para>   BLENDPS xmm1,       xmm2/m128, imm8</para>
        ///   <para>  VBLENDPS xmm1, xmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector128<float> Blend(Vector128<float> left, Vector128<float> right, [ConstantExpected] byte control) => Blend(left, right, control);
        /// <summary>
        ///   <para>__m128d _mm_blend_pd (__m128d a, __m128d b, const int imm8)</para>
        ///   <para>   BLENDPD xmm1,       xmm2/m128, imm8</para>
        ///   <para>  VBLENDPD xmm1, xmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector128<double> Blend(Vector128<double> left, Vector128<double> right, [ConstantExpected] byte control) => Blend(left, right, control);

        /// <summary>
        ///   <para>__m128i _mm_blendv_epi8 (__m128i a, __m128i b, __m128i mask)</para>
        ///   <para>   PBLENDVB xmm1,       xmm2/m128, &lt;XMM0&gt;</para>
        ///   <para>  VPBLENDVB xmm1, xmm2, xmm3/m128, xmm4</para>
        /// </summary>
        public static Vector128<sbyte> BlendVariable(Vector128<sbyte> left, Vector128<sbyte> right, Vector128<sbyte> mask) => BlendVariable(left, right, mask);
        /// <summary>
        ///   <para>__m128i _mm_blendv_epi8 (__m128i a, __m128i b, __m128i mask)</para>
        ///   <para>   PBLENDVB xmm1,       xmm2/m128, &lt;XMM0&gt;</para>
        ///   <para>  VPBLENDVB xmm1, xmm2, xmm3/m128, xmm4</para>
        /// </summary>
        public static Vector128<byte> BlendVariable(Vector128<byte> left, Vector128<byte> right, Vector128<byte> mask) => BlendVariable(left, right, mask);
        /// <summary>
        ///   <para>__m128i _mm_blendv_epi8 (__m128i a, __m128i b, __m128i mask)</para>
        ///   <para>   PBLENDVB xmm1,       xmm2/m128, &lt;XMM0&gt;</para>
        ///   <para>  VPBLENDVB xmm1, xmm2, xmm3/m128, xmm4</para>
        ///   <para>This intrinsic generates PBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.</para>
        /// </summary>
        public static Vector128<short> BlendVariable(Vector128<short> left, Vector128<short> right, Vector128<short> mask) => BlendVariable(left, right, mask);
        /// <summary>
        ///   <para>__m128i _mm_blendv_epi8 (__m128i a, __m128i b, __m128i mask)</para>
        ///   <para>   PBLENDVB xmm1,       xmm2/m128, &lt;XMM0&gt;</para>
        ///   <para>  VPBLENDVB xmm1, xmm2, xmm3/m128, xmm4</para>
        ///   <para>This intrinsic generates PBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.</para>
        /// </summary>
        public static Vector128<ushort> BlendVariable(Vector128<ushort> left, Vector128<ushort> right, Vector128<ushort> mask) => BlendVariable(left, right, mask);
        /// <summary>
        ///   <para>__m128i _mm_blendv_epi8 (__m128i a, __m128i b, __m128i mask)</para>
        ///   <para>   PBLENDVB xmm1,       xmm2/m128, &lt;XMM0&gt;</para>
        ///   <para>  VPBLENDVB xmm1, xmm2, xmm3/m128, xmm4</para>
        ///   <para>This intrinsic generates PBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.</para>
        /// </summary>
        public static Vector128<int> BlendVariable(Vector128<int> left, Vector128<int> right, Vector128<int> mask) => BlendVariable(left, right, mask);
        /// <summary>
        ///   <para>__m128i _mm_blendv_epi8 (__m128i a, __m128i b, __m128i mask)</para>
        ///   <para>   PBLENDVB xmm1,       xmm2/m128, &lt;XMM0&gt;</para>
        ///   <para>  VPBLENDVB xmm1, xmm2, xmm3/m128, xmm4</para>
        ///   <para>This intrinsic generates PBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.</para>
        /// </summary>
        public static Vector128<uint> BlendVariable(Vector128<uint> left, Vector128<uint> right, Vector128<uint> mask) => BlendVariable(left, right, mask);
        /// <summary>
        ///   <para>__m128i _mm_blendv_epi8 (__m128i a, __m128i b, __m128i mask)</para>
        ///   <para>   PBLENDVB xmm1,       xmm2/m128, &lt;XMM0&gt;</para>
        ///   <para>  VPBLENDVB xmm1, xmm2, xmm3/m128, xmm4</para>
        ///   <para>This intrinsic generates PBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.</para>
        /// </summary>
        public static Vector128<long> BlendVariable(Vector128<long> left, Vector128<long> right, Vector128<long> mask) => BlendVariable(left, right, mask);
        /// <summary>
        ///   <para>__m128i _mm_blendv_epi8 (__m128i a, __m128i b, __m128i mask)</para>
        ///   <para>   PBLENDVB xmm1,       xmm2/m128, &lt;XMM0&gt;</para>
        ///   <para>  VPBLENDVB xmm1, xmm2, xmm3/m128, xmm4</para>
        ///   <para>This intrinsic generates PBLENDVB that needs a BYTE mask-vector, so users should correctly set each mask byte for the selected elements.</para>
        /// </summary>
        public static Vector128<ulong> BlendVariable(Vector128<ulong> left, Vector128<ulong> right, Vector128<ulong> mask) => BlendVariable(left, right, mask);
        /// <summary>
        ///   <para>__m128 _mm_blendv_ps (__m128 a, __m128 b, __m128 mask)</para>
        ///   <para>   BLENDVPS xmm1,       xmm2/m128, &lt;XMM0&gt;</para>
        ///   <para>  VBLENDVPS xmm1, xmm2, xmm3/m128, xmm4</para>
        /// </summary>
        public static Vector128<float> BlendVariable(Vector128<float> left, Vector128<float> right, Vector128<float> mask) => BlendVariable(left, right, mask);
        /// <summary>
        ///   <para>__m128d _mm_blendv_pd (__m128d a, __m128d b, __m128d mask)</para>
        ///   <para>   BLENDVPD xmm1,       xmm2/m128, &lt;XMM0&gt;</para>
        ///   <para>  VBLENDVPD xmm1, xmm2, xmm3/m128, xmm4</para>
        /// </summary>
        public static Vector128<double> BlendVariable(Vector128<double> left, Vector128<double> right, Vector128<double> mask) => BlendVariable(left, right, mask);

        /// <summary>
        ///   <para>__m128 _mm_ceil_ps (__m128 a)</para>
        ///   <para>   ROUNDPS xmm1, xmm2/m128, imm8(10)</para>
        ///   <para>  VROUNDPS xmm1, xmm2/m128, imm8(10)</para>
        /// </summary>
        public static Vector128<float> Ceiling(Vector128<float> value) => Ceiling(value);
        /// <summary>
        ///   <para>__m128d _mm_ceil_pd (__m128d a)</para>
        ///   <para>   ROUNDPD xmm1, xmm2/m128, imm8(10)</para>
        ///   <para>  VROUNDPD xmm1, xmm2/m128, imm8(10)</para>
        /// </summary>
        public static Vector128<double> Ceiling(Vector128<double> value) => Ceiling(value);

        /// <summary>
        ///   <para>__m128 _mm_ceil_ss (__m128 a)</para>
        ///   <para>   ROUNDSS xmm1,       xmm2/m128, imm8(10)</para>
        ///   <para>  VROUNDSS xmm1, xmm2, xmm3/m128, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> CeilingScalar(Vector128<float> value) => CeilingScalar(value);
        /// <summary>
        ///   <para>__m128 _mm_ceil_ss (__m128 a, __m128 b)</para>
        ///   <para>   ROUNDSS xmm1,       xmm2/m128, imm8(10)</para>
        ///   <para>  VROUNDSS xmm1, xmm2, xmm3/m128, imm8(10)</para>
        /// </summary>
        public static Vector128<float> CeilingScalar(Vector128<float> upper, Vector128<float> value) => CeilingScalar(upper, value);
        /// <summary>
        ///   <para>__m128d _mm_ceil_sd (__m128d a)</para>
        ///   <para>   ROUNDSD xmm1,       xmm2/m128, imm8(10)</para>
        ///   <para>  VROUNDSD xmm1, xmm2, xmm3/m128, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> CeilingScalar(Vector128<double> value) => CeilingScalar(value);
        /// <summary>
        ///   <para>__m128d _mm_ceil_sd (__m128d a, __m128d b)</para>
        ///   <para>   ROUNDSD xmm1,       xmm2/m128, imm8(10)</para>
        ///   <para>  VROUNDSD xmm1, xmm2, xmm3/m128, imm8(10)</para>
        /// </summary>
        public static Vector128<double> CeilingScalar(Vector128<double> upper, Vector128<double> value) => CeilingScalar(upper, value);

        /// <summary>
        ///   <para>__m128i _mm_cmpeq_epi64 (__m128i a, __m128i b)</para>
        ///   <para>   PCMPEQQ xmm1,       xmm2/m128</para>
        ///   <para>  VPCMPEQQ xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<long> CompareEqual(Vector128<long> left, Vector128<long> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>__m128i _mm_cmpeq_epi64 (__m128i a, __m128i b)</para>
        ///   <para>   PCMPEQQ xmm1,       xmm2/m128</para>
        ///   <para>  VPCMPEQQ xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ulong> CompareEqual(Vector128<ulong> left, Vector128<ulong> right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>__m128i _mm_cvtepi8_epi16 (__m128i a)</para>
        ///   <para>   PMOVSXBW xmm1,         xmm2/m64</para>
        ///   <para>  VPMOVSXBW xmm1,         xmm2/m64</para>
        ///   <para>  VPMOVSXBW xmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector128<sbyte> value) => ConvertToVector128Int16(value);
        /// <summary>
        ///   <para>__m128i _mm_cvtepu8_epi16 (__m128i a)</para>
        ///   <para>   PMOVZXBW xmm1,         xmm2/m64</para>
        ///   <para>  VPMOVZXBW xmm1,         xmm2/m64</para>
        ///   <para>  VPMOVZXBW xmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector128<short> ConvertToVector128Int16(Vector128<byte> value) => ConvertToVector128Int16(value);
        /// <summary>
        ///   <para>__m128i _mm_cvtepi8_epi32 (__m128i a)</para>
        ///   <para>   PMOVSXBD xmm1,         xmm2/m32</para>
        ///   <para>  VPMOVSXBD xmm1,         xmm2/m32</para>
        ///   <para>  VPMOVSXBD xmm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<sbyte> value) => ConvertToVector128Int32(value);
        /// <summary>
        ///   <para>__m128i _mm_cvtepu8_epi32 (__m128i a)</para>
        ///   <para>   PMOVZXBD xmm1,         xmm2/m32</para>
        ///   <para>  VPMOVZXBD xmm1,         xmm2/m32</para>
        ///   <para>  VPMOVZXBD xmm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<byte> value) => ConvertToVector128Int32(value);
        /// <summary>
        ///   <para>__m128i _mm_cvtepi16_epi32 (__m128i a)</para>
        ///   <para>   PMOVSXWD xmm1,         xmm2/m64</para>
        ///   <para>  VPMOVSXWD xmm1,         xmm2/m64</para>
        ///   <para>  VPMOVSXWD xmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<short> value) => ConvertToVector128Int32(value);
        /// <summary>
        ///   <para>__m128i _mm_cvtepu16_epi32 (__m128i a)</para>
        ///   <para>   PMOVZXWD xmm1,         xmm2/m64</para>
        ///   <para>  VPMOVZXWD xmm1,         xmm2/m64</para>
        ///   <para>  VPMOVZXWD xmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<ushort> value) => ConvertToVector128Int32(value);
        /// <summary>
        ///   <para>__m128i _mm_cvtepi8_epi64 (__m128i a)</para>
        ///   <para>   PMOVSXBQ xmm1,         xmm2/m16</para>
        ///   <para>  VPMOVSXBQ xmm1,         xmm2/m16</para>
        ///   <para>  VPMOVSXBQ xmm1 {k1}{z}, xmm2/m16</para>
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<sbyte> value) => ConvertToVector128Int64(value);
        /// <summary>
        ///   <para>__m128i _mm_cvtepu8_epi64 (__m128i a)</para>
        ///   <para>   PMOVZXBQ xmm1,         xmm2/m16</para>
        ///   <para>  VPMOVZXBQ xmm1,         xmm2/m16</para>
        ///   <para>  VPMOVZXBQ xmm1 {k1}{z}, xmm2/m16</para>
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<byte> value) => ConvertToVector128Int64(value);
        /// <summary>
        ///   <para>__m128i _mm_cvtepi16_epi64 (__m128i a)</para>
        ///   <para>   PMOVSXWQ xmm1,         xmm2/m32</para>
        ///   <para>  VPMOVSXWQ xmm1,         xmm2/m32</para>
        ///   <para>  VPMOVSXWQ xmm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<short> value) => ConvertToVector128Int64(value);
        /// <summary>
        ///   <para>__m128i _mm_cvtepu16_epi64 (__m128i a)</para>
        ///   <para>   PMOVZXWQ xmm1,         xmm2/m32</para>
        ///   <para>  VPMOVZXWQ xmm1,         xmm2/m32</para>
        ///   <para>  VPMOVZXWQ xmm1 {k1}{z}, xmm2/m32</para>
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<ushort> value) => ConvertToVector128Int64(value);
        /// <summary>
        ///   <para>__m128i _mm_cvtepi32_epi64 (__m128i a)</para>
        ///   <para>   PMOVSXDQ xmm1,         xmm2/m64</para>
        ///   <para>  VPMOVSXDQ xmm1,         xmm2/m64</para>
        ///   <para>  VPMOVSXDQ xmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<int> value) => ConvertToVector128Int64(value);
        /// <summary>
        ///   <para>__m128i _mm_cvtepu32_epi64 (__m128i a)</para>
        ///   <para>   PMOVZXDQ xmm1,         xmm2/m64</para>
        ///   <para>  VPMOVZXDQ xmm1,         xmm2/m64</para>
        ///   <para>  VPMOVZXDQ xmm1 {k1}{z}, xmm2/m64</para>
        /// </summary>
        public static Vector128<long> ConvertToVector128Int64(Vector128<uint> value) => ConvertToVector128Int64(value);

        /// <summary>
        ///   <para>   PMOVSXBW xmm1,         m64</para>
        ///   <para>  VPMOVSXBW xmm1,         m64</para>
        ///   <para>  VPMOVSXBW xmm1 {k1}{z}, m64</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector128<short> ConvertToVector128Int16(sbyte* address) => ConvertToVector128Int16(address);
        /// <summary>
        ///   <para>   PMOVZXBW xmm1,         m64</para>
        ///   <para>  VPMOVZXBW xmm1,         m64</para>
        ///   <para>  VPMOVZXBW xmm1 {k1}{z}, m64</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector128<short> ConvertToVector128Int16(byte* address) => ConvertToVector128Int16(address);
        /// <summary>
        ///   <para>   PMOVSXBD xmm1,         m32</para>
        ///   <para>  VPMOVSXBD xmm1,         m32</para>
        ///   <para>  VPMOVSXBD xmm1 {k1}{z}, m32</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector128<int> ConvertToVector128Int32(sbyte* address) => ConvertToVector128Int32(address);
        /// <summary>
        ///   <para>   PMOVZXBD xmm1,         m32</para>
        ///   <para>  VPMOVZXBD xmm1,         m32</para>
        ///   <para>  VPMOVZXBD xmm1 {k1}{z}, m32</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector128<int> ConvertToVector128Int32(byte* address) => ConvertToVector128Int32(address);
        /// <summary>
        ///   <para>   PMOVSXWD xmm1,         m64</para>
        ///   <para>  VPMOVSXWD xmm1,         m64</para>
        ///   <para>  VPMOVSXWD xmm1 {k1}{z}, m64</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector128<int> ConvertToVector128Int32(short* address) => ConvertToVector128Int32(address);
        /// <summary>
        ///   <para>   PMOVZXWD xmm1,         m64</para>
        ///   <para>  VPMOVZXWD xmm1,         m64</para>
        ///   <para>  VPMOVZXWD xmm1 {k1}{z}, m64</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector128<int> ConvertToVector128Int32(ushort* address) => ConvertToVector128Int32(address);
        /// <summary>
        ///   <para>   PMOVSXBQ xmm1,         m16</para>
        ///   <para>  VPMOVSXBQ xmm1,         m16</para>
        ///   <para>  VPMOVSXBQ xmm1 {k1}{z}, m16</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector128<long> ConvertToVector128Int64(sbyte* address) => ConvertToVector128Int64(address);
        /// <summary>
        ///   <para>   PMOVZXBQ xmm1,         m16</para>
        ///   <para>  VPMOVZXBQ xmm1,         m16</para>
        ///   <para>  VPMOVZXBQ xmm1 {k1}{z}, m16</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector128<long> ConvertToVector128Int64(byte* address) => ConvertToVector128Int64(address);
        /// <summary>
        ///   <para>   PMOVSXWQ xmm1,         m32</para>
        ///   <para>  VPMOVSXWQ xmm1,         m32</para>
        ///   <para>  VPMOVSXWQ xmm1 {k1}{z}, m32</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector128<long> ConvertToVector128Int64(short* address) => ConvertToVector128Int64(address);
        /// <summary>
        ///   <para>   PMOVZXWQ xmm1,         m32</para>
        ///   <para>  VPMOVZXWQ xmm1,         m32</para>
        ///   <para>  VPMOVZXWQ xmm1 {k1}{z}, m32</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector128<long> ConvertToVector128Int64(ushort* address) => ConvertToVector128Int64(address);
        /// <summary>
        ///   <para>   PMOVSXDQ xmm1,         m64</para>
        ///   <para>  VPMOVSXDQ xmm1,         m64</para>
        ///   <para>  VPMOVSXDQ xmm1 {k1}{z}, m64</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector128<long> ConvertToVector128Int64(int* address) => ConvertToVector128Int64(address);
        /// <summary>
        ///   <para>   PMOVZXDQ xmm1,         m64</para>
        ///   <para>  VPMOVZXDQ xmm1,         m64</para>
        ///   <para>  VPMOVZXDQ xmm1 {k1}{z}, m64</para>
        ///   <para>The native signature does not exist. We provide this additional overload for completeness.</para>
        /// </summary>
        public static unsafe Vector128<long> ConvertToVector128Int64(uint* address) => ConvertToVector128Int64(address);

        /// <summary>
        ///   <para>__m128 _mm_dp_ps (__m128 a, __m128 b, const int imm8)</para>
        ///   <para>   DPPS xmm1,       xmm2/m128, imm8</para>
        ///   <para>  VDPPS xmm1, xmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector128<float> DotProduct(Vector128<float> left, Vector128<float> right, [ConstantExpected] byte control) => DotProduct(left, right, control);
        /// <summary>
        ///   <para>__m128d _mm_dp_pd (__m128d a, __m128d b, const int imm8)</para>
        ///   <para>   DPPD xmm1,       xmm2/m128, imm8</para>
        ///   <para>  VDPPD xmm1, xmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector128<double> DotProduct(Vector128<double> left, Vector128<double> right, [ConstantExpected] byte control) => DotProduct(left, right, control);

        /// <summary>
        ///   <para>int _mm_extract_epi8 (__m128i a, const int imm8)</para>
        ///   <para>   PEXTRB r/m8, xmm1, imm8</para>
        ///   <para>  VPEXTRB r/m8, xmm1, imm8</para>
        /// </summary>
        public static byte Extract(Vector128<byte> value, [ConstantExpected] byte index) => Extract(value, index);
        /// <summary>
        ///   <para>int _mm_extract_epi32 (__m128i a, const int imm8)</para>
        ///   <para>   PEXTRD r/m32, xmm1, imm8</para>
        ///   <para>  VPEXTRD r/m32, xmm1, imm8</para>
        /// </summary>
        public static int Extract(Vector128<int> value, [ConstantExpected] byte index) => Extract(value, index);
        /// <summary>
        ///   <para>int _mm_extract_epi32 (__m128i a, const int imm8)</para>
        ///   <para>   PEXTRD r/m32, xmm1, imm8</para>
        ///   <para>  VPEXTRD r/m32, xmm1, imm8</para>
        /// </summary>
        public static uint Extract(Vector128<uint> value, [ConstantExpected] byte index) => Extract(value, index);
        /// <summary>
        ///   <para>int _mm_extract_ps (__m128 a, const int imm8)</para>
        ///   <para>   EXTRACTPS r/m32, xmm1, imm8</para>
        ///   <para>  VEXTRACTPS r/m32, xmm1, imm8</para>
        /// </summary>
        public static float Extract(Vector128<float> value, [ConstantExpected] byte index) => Extract(value, index);

        /// <summary>
        ///   <para>__m128 _mm_floor_ps (__m128 a)</para>
        ///   <para>   ROUNDPS xmm1, xmm2/m128, imm8(9)</para>
        ///   <para>  VROUNDPS xmm1, xmm2/m128, imm8(9)</para>
        /// </summary>
        public static Vector128<float> Floor(Vector128<float> value) => Floor(value);
        /// <summary>
        ///   <para>__m128d _mm_floor_pd (__m128d a)</para>
        ///   <para>   ROUNDPD xmm1, xmm2/m128, imm8(9)</para>
        ///   <para>  VROUNDPD xmm1, xmm2/m128, imm8(9)</para>
        /// </summary>
        public static Vector128<double> Floor(Vector128<double> value) => Floor(value);

        /// <summary>
        ///   <para>__m128 _mm_floor_ss (__m128 a)</para>
        ///   <para>   ROUNDSS xmm1,       xmm2/m128, imm8(9)</para>
        ///   <para>  VROUNDSS xmm1, xmm2, xmm3/m128, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> FloorScalar(Vector128<float> value) => FloorScalar(value);
        /// <summary>
        ///   <para>__m128 _mm_floor_ss (__m128 a, __m128 b)</para>
        ///   <para>   ROUNDSS xmm1,       xmm2/m128, imm8(9)</para>
        ///   <para>  VROUNDSS xmm1, xmm2, xmm3/m128, imm8(9)</para>
        /// </summary>
        public static Vector128<float> FloorScalar(Vector128<float> upper, Vector128<float> value) => FloorScalar(upper, value);
        /// <summary>
        ///   <para>__m128d _mm_floor_sd (__m128d a)</para>
        ///   <para>   ROUNDSD xmm1,       xmm2/m128, imm8(9)</para>
        ///   <para>  VROUNDSD xmm1, xmm2, xmm3/m128, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> FloorScalar(Vector128<double> value) => FloorScalar(value);
        /// <summary>
        ///   <para>__m128d _mm_floor_sd (__m128d a, __m128d b)</para>
        ///   <para>   ROUNDSD xmm1,       xmm2/m128, imm8(9)</para>
        ///   <para>  VROUNDSD xmm1, xmm2, xmm3/m128, imm8(9)</para>
        /// </summary>
        public static Vector128<double> FloorScalar(Vector128<double> upper, Vector128<double> value) => FloorScalar(upper, value);

        /// <summary>
        ///   <para>__m128i _mm_insert_epi8 (__m128i a, int i, const int imm8)</para>
        ///   <para>   PINSRB xmm1,       r/m8, imm8</para>
        ///   <para>  VPINSRB xmm1, xmm2, r/m8, imm8</para>
        /// </summary>
        public static Vector128<sbyte> Insert(Vector128<sbyte> value, sbyte data, [ConstantExpected] byte index) => Insert(value, data, index);
        /// <summary>
        ///   <para>__m128i _mm_insert_epi8 (__m128i a, int i, const int imm8)</para>
        ///   <para>   PINSRB xmm1,       r/m8, imm8</para>
        ///   <para>  VPINSRB xmm1, xmm2, r/m8, imm8</para>
        /// </summary>
        public static Vector128<byte> Insert(Vector128<byte> value, byte data, [ConstantExpected] byte index) => Insert(value, data, index);
        /// <summary>
        ///   <para>__m128i _mm_insert_epi32 (__m128i a, int i, const int imm8)</para>
        ///   <para>   PINSRD xmm1,       r/m32, imm8</para>
        ///   <para>  VPINSRD xmm1, xmm2, r/m32, imm8</para>
        /// </summary>
        public static Vector128<int> Insert(Vector128<int> value, int data, [ConstantExpected] byte index) => Insert(value, data, index);
        /// <summary>
        ///   <para>__m128i _mm_insert_epi32 (__m128i a, int i, const int imm8)</para>
        ///   <para>   PINSRD xmm1,       r/m32, imm8</para>
        ///   <para>  VPINSRD xmm1, xmm2, r/m32, imm8</para>
        /// </summary>
        public static Vector128<uint> Insert(Vector128<uint> value, uint data, [ConstantExpected] byte index) => Insert(value, data, index);
        /// <summary>
        ///   <para>__m128 _mm_insert_ps (__m128 a, __m128 b, const int imm8)</para>
        ///   <para>   INSERTPS xmm1,       xmm2/m32, imm8</para>
        ///   <para>  VINSERTPS xmm1, xmm2, xmm3/m32, imm8</para>
        /// </summary>
        public static Vector128<float> Insert(Vector128<float> value, Vector128<float> data, [ConstantExpected] byte index) => Insert(value, data, index);

        /// <summary>
        ///   <para>__m128i _mm_stream_load_si128 (const __m128i* mem_addr)</para>
        ///   <para>   MOVNTDQA xmm1, m128</para>
        ///   <para>  VMOVNTDQA xmm1, m128</para>
        /// </summary>
        public static unsafe Vector128<sbyte> LoadAlignedVector128NonTemporal(sbyte* address) => LoadAlignedVector128NonTemporal(address);
        /// <summary>
        ///   <para>__m128i _mm_stream_load_si128 (const __m128i* mem_addr)</para>
        ///   <para>   MOVNTDQA xmm1, m128</para>
        ///   <para>  VMOVNTDQA xmm1, m128</para>
        /// </summary>
        public static unsafe Vector128<byte> LoadAlignedVector128NonTemporal(byte* address) => LoadAlignedVector128NonTemporal(address);
        /// <summary>
        ///   <para>__m128i _mm_stream_load_si128 (const __m128i* mem_addr)</para>
        ///   <para>   MOVNTDQA xmm1, m128</para>
        ///   <para>  VMOVNTDQA xmm1, m128</para>
        /// </summary>
        public static unsafe Vector128<short> LoadAlignedVector128NonTemporal(short* address) => LoadAlignedVector128NonTemporal(address);
        /// <summary>
        ///   <para>__m128i _mm_stream_load_si128 (const __m128i* mem_addr)</para>
        ///   <para>   MOVNTDQA xmm1, m128</para>
        ///   <para>  VMOVNTDQA xmm1, m128</para>
        /// </summary>
        public static unsafe Vector128<ushort> LoadAlignedVector128NonTemporal(ushort* address) => LoadAlignedVector128NonTemporal(address);
        /// <summary>
        ///   <para>__m128i _mm_stream_load_si128 (const __m128i* mem_addr)</para>
        ///   <para>   MOVNTDQA xmm1, m128</para>
        ///   <para>  VMOVNTDQA xmm1, m128</para>
        /// </summary>
        public static unsafe Vector128<int> LoadAlignedVector128NonTemporal(int* address) => LoadAlignedVector128NonTemporal(address);
        /// <summary>
        ///   <para>__m128i _mm_stream_load_si128 (const __m128i* mem_addr)</para>
        ///   <para>   MOVNTDQA xmm1, m128</para>
        ///   <para>  VMOVNTDQA xmm1, m128</para>
        /// </summary>
        public static unsafe Vector128<uint> LoadAlignedVector128NonTemporal(uint* address) => LoadAlignedVector128NonTemporal(address);
        /// <summary>
        ///   <para>__m128i _mm_stream_load_si128 (const __m128i* mem_addr)</para>
        ///   <para>   MOVNTDQA xmm1, m128</para>
        ///   <para>  VMOVNTDQA xmm1, m128</para>
        /// </summary>
        public static unsafe Vector128<long> LoadAlignedVector128NonTemporal(long* address) => LoadAlignedVector128NonTemporal(address);
        /// <summary>
        ///   <para>__m128i _mm_stream_load_si128 (const __m128i* mem_addr)</para>
        ///   <para>   MOVNTDQA xmm1, m128</para>
        ///   <para>  VMOVNTDQA xmm1, m128</para>
        /// </summary>
        public static unsafe Vector128<ulong> LoadAlignedVector128NonTemporal(ulong* address) => LoadAlignedVector128NonTemporal(address);

        /// <summary>
        ///   <para>__m128i _mm_max_epi8 (__m128i a, __m128i b)</para>
        ///   <para>   PMAXSB xmm1,               xmm2/m128</para>
        ///   <para>  VPMAXSB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMAXSB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> Max(Vector128<sbyte> left, Vector128<sbyte> right) => Max(left, right);
        /// <summary>
        ///   <para>__m128i _mm_max_epu16 (__m128i a, __m128i b)</para>
        ///   <para>   PMAXUW xmm1,               xmm2/m128</para>
        ///   <para>  VPMAXUW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMAXUW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> Max(Vector128<ushort> left, Vector128<ushort> right) => Max(left, right);
        /// <summary>
        ///   <para>__m128i _mm_max_epi32 (__m128i a, __m128i b)</para>
        ///   <para>   PMAXSD xmm1,               xmm2/m128</para>
        ///   <para>  VPMAXSD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMAXSD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> Max(Vector128<int> left, Vector128<int> right) => Max(left, right);
        /// <summary>
        ///   <para>__m128i _mm_max_epu32 (__m128i a, __m128i b)</para>
        ///   <para>   PMAXUD xmm1,               xmm2/m128</para>
        ///   <para>  VPMAXUD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMAXUD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> Max(Vector128<uint> left, Vector128<uint> right) => Max(left, right);

        /// <summary>
        ///   <para>__m128i _mm_min_epi8 (__m128i a, __m128i b)</para>
        ///   <para>   PMINSB xmm1,               xmm2/m128</para>
        ///   <para>  VPMINSB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMINSB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> Min(Vector128<sbyte> left, Vector128<sbyte> right) => Min(left, right);
        /// <summary>
        ///   <para>__m128i _mm_min_epu16 (__m128i a, __m128i b)</para>
        ///   <para>   PMINUW xmm1,               xmm2/m128</para>
        ///   <para>  VPMINUW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMINUW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> Min(Vector128<ushort> left, Vector128<ushort> right) => Min(left, right);
        /// <summary>
        ///   <para>__m128i _mm_min_epi32 (__m128i a, __m128i b)</para>
        ///   <para>   PMINSD xmm1,               xmm2/m128</para>
        ///   <para>  VPMINSD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMINSD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> Min(Vector128<int> left, Vector128<int> right) => Min(left, right);
        /// <summary>
        ///   <para>__m128i _mm_min_epu32 (__m128i a, __m128i b)</para>
        ///   <para>   PMINUD xmm1,               xmm2/m128</para>
        ///   <para>  VPMINUD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMINUD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> Min(Vector128<uint> left, Vector128<uint> right) => Min(left, right);

        /// <summary>
        ///   <para>__m128i _mm_minpos_epu16 (__m128i a)</para>
        ///   <para>   PHMINPOSUW xmm1, xmm2/m128</para>
        ///   <para>  VPHMINPOSUW xmm1, xmm2/m128</para>
        /// </summary>
        public static Vector128<ushort> MinHorizontal(Vector128<ushort> value) => MinHorizontal(value);

        /// <summary>
        ///   <para>__m128i _mm_mpsadbw_epu8 (__m128i a, __m128i b, const int imm8)</para>
        ///   <para>   MPSADBW xmm1,       xmm2/m128, imm8</para>
        ///   <para>  VMPSADBW xmm1, xmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector128<ushort> MultipleSumAbsoluteDifferences(Vector128<byte> left, Vector128<byte> right, [ConstantExpected] byte mask) => MultipleSumAbsoluteDifferences(left, right, mask);

        /// <summary>
        ///   <para>__m128i _mm_mul_epi32 (__m128i a, __m128i b)</para>
        ///   <para>   PMULDQ xmm1,               xmm2/m128</para>
        ///   <para>  VPMULDQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMULDQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> Multiply(Vector128<int> left, Vector128<int> right) => Multiply(left, right);

        /// <summary>
        ///   <para>__m128i _mm_mullo_epi32 (__m128i a, __m128i b)</para>
        ///   <para>   PMULLD xmm1,               xmm2/m128</para>
        ///   <para>  VPMULLD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMULLD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> MultiplyLow(Vector128<int> left, Vector128<int> right) => MultiplyLow(left, right);
        /// <summary>
        ///   <para>__m128i _mm_mullo_epi32 (__m128i a, __m128i b)</para>
        ///   <para>   PMULLD xmm1,               xmm2/m128</para>
        ///   <para>  VPMULLD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMULLD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> MultiplyLow(Vector128<uint> left, Vector128<uint> right) => MultiplyLow(left, right);

        /// <summary>
        ///   <para>__m128i _mm_packus_epi32 (__m128i a, __m128i b)</para>
        ///   <para>   PACKUSDW xmm1,               xmm2/m128</para>
        ///   <para>  VPACKUSDW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPACKUSDW xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<ushort> PackUnsignedSaturate(Vector128<int> left, Vector128<int> right) => PackUnsignedSaturate(left, right);

        /// <summary>
        ///   <para>__m128 _mm_round_ps (__m128 a, _MM_FROUND_CUR_DIRECTION)</para>
        ///   <para>   ROUNDPS xmm1, xmm2/m128, imm8(4)</para>
        ///   <para>  VROUNDPS xmm1, xmm2/m128, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundCurrentDirection(Vector128<float> value) => RoundCurrentDirection(value);
        /// <summary>
        ///   <para>__m128d _mm_round_pd (__m128d a, _MM_FROUND_CUR_DIRECTION)</para>
        ///   <para>   ROUNDPD xmm1, xmm2/m128, imm8(4)</para>
        ///   <para>  VROUNDPD xmm1, xmm2/m128, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundCurrentDirection(Vector128<double> value) => RoundCurrentDirection(value);

        /// <summary>
        ///   <para>__m128 _mm_round_ss (__m128 a, _MM_FROUND_CUR_DIRECTION)</para>
        ///   <para>   ROUNDSS xmm1,       xmm2/m128, imm8(4)</para>
        ///   <para>  VROUNDSS xmm1, xmm2, xmm3/m128, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundCurrentDirectionScalar(Vector128<float> value) => RoundCurrentDirectionScalar(value);
        /// <summary>
        ///   <para>__m128 _mm_round_ss (__m128 a, __m128 b, _MM_FROUND_CUR_DIRECTION)</para>
        ///   <para>   ROUNDSS xmm1,       xmm2/m128, imm8(4)</para>
        ///   <para>  VROUNDSS xmm1, xmm2, xmm3/m128, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundCurrentDirectionScalar(Vector128<float> upper, Vector128<float> value) => RoundCurrentDirectionScalar(upper, value);
        /// <summary>
        ///   <para>__m128d _mm_round_sd (__m128d a, _MM_FROUND_CUR_DIRECTION)</para>
        ///   <para>   ROUNDSD xmm1,       xmm2/m128, imm8(4)</para>
        ///   <para>  VROUNDSD xmm1, xmm2, xmm3/m128, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundCurrentDirectionScalar(Vector128<double> value) => RoundCurrentDirectionScalar(value);
        /// <summary>
        ///   <para>__m128d _mm_round_sd (__m128d a, __m128d b, _MM_FROUND_CUR_DIRECTION)</para>
        ///   <para>   ROUNDSD xmm1,       xmm2/m128, imm8(4)</para>
        ///   <para>  VROUNDSD xmm1, xmm2, xmm3/m128, imm8(4)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundCurrentDirectionScalar(Vector128<double> upper, Vector128<double> value) => RoundCurrentDirectionScalar(upper, value);

        /// <summary>
        ///   <para>__m128 _mm_round_ps (__m128 a, _MM_FROUND_TO_NEAREST_INT |_MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDPS xmm1, xmm2/m128, imm8(8)</para>
        ///   <para>  VROUNDPS xmm1, xmm2/m128, imm8(8)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundToNearestInteger(Vector128<float> value) => RoundToNearestInteger(value);
        /// <summary>
        ///   <para>__m128 _mm_round_pd (__m128 a, _MM_FROUND_TO_NEAREST_INT |_MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDPD xmm1, xmm2/m128, imm8(8)</para>
        ///   <para>  VROUNDPD xmm1, xmm2/m128, imm8(8)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundToNearestInteger(Vector128<double> value) => RoundToNearestInteger(value);

        /// <summary>
        ///   <para>__m128 _mm_round_ss (__m128 a, _MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSS xmm1,       xmm2/m128, imm8(8)</para>
        ///   <para>  VROUNDSS xmm1, xmm2, xmm3/m128, imm8(8)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundToNearestIntegerScalar(Vector128<float> value) => RoundToNearestIntegerScalar(value);
        /// <summary>
        ///   <para>__m128 _mm_round_ss (__m128 a, __m128 b, _MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSS xmm1,       xmm2/m128, imm8(8)</para>
        ///   <para>  VROUNDSS xmm1, xmm2, xmm3/m128, imm8(8)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundToNearestIntegerScalar(Vector128<float> upper, Vector128<float> value) => RoundToNearestIntegerScalar(upper, value);
        /// <summary>
        ///   <para>__m128d _mm_round_sd (__m128d a, _MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSD xmm1,       xmm2/m128, imm8(8)</para>
        ///   <para>  VROUNDSD xmm1, xmm2, xmm3/m128, imm8(8)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundToNearestIntegerScalar(Vector128<double> value) => RoundToNearestIntegerScalar(value);
        /// <summary>
        ///   <para>__m128d _mm_round_sd (__m128d a, __m128d b, _MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSD xmm1,       xmm2/m128, imm8(8)</para>
        ///   <para>  VROUNDSD xmm1, xmm2, xmm3/m128, imm8(8)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundToNearestIntegerScalar(Vector128<double> upper, Vector128<double> value) => RoundToNearestIntegerScalar(upper, value);

        /// <summary>
        ///   <para>__m128 _mm_round_ps (__m128 a, _MM_FROUND_TO_NEG_INF |_MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDPS xmm1, xmm2/m128, imm8(9)</para>
        ///   <para>  VROUNDPS xmm1, xmm2/m128, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundToNegativeInfinity(Vector128<float> value) => RoundToNegativeInfinity(value);
        /// <summary>
        ///   <para>__m128 _mm_round_pd (__m128 a, _MM_FROUND_TO_NEG_INF |_MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDPD xmm1, xmm2/m128, imm8(9)</para>
        ///   <para>  VROUNDPD xmm1, xmm2/m128, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundToNegativeInfinity(Vector128<double> value) => RoundToNegativeInfinity(value);

        /// <summary>
        ///   <para>__m128 _mm_round_ss (__m128 a, _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSS xmm1,       xmm2/m128, imm8(9)</para>
        ///   <para>  VROUNDSS xmm1, xmm2, xmm3/m128, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundToNegativeInfinityScalar(Vector128<float> value) => RoundToNegativeInfinityScalar(value);
        /// <summary>
        ///   <para>__m128 _mm_round_ss (__m128 a, __m128 b, _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSS xmm1,       xmm2/m128, imm8(9)</para>
        ///   <para>  VROUNDSS xmm1, xmm2, xmm3/m128, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundToNegativeInfinityScalar(Vector128<float> upper, Vector128<float> value) => RoundToNegativeInfinityScalar(upper, value);
        /// <summary>
        ///   <para>__m128d _mm_round_sd (__m128d a, _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSD xmm1,       xmm2/m128, imm8(9)</para>
        ///   <para>  VROUNDSD xmm1, xmm2, xmm3/m128, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundToNegativeInfinityScalar(Vector128<double> value) => RoundToNegativeInfinityScalar(value);
        /// <summary>
        ///   <para>__m128d _mm_round_sd (__m128d a, __m128d b, _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSD xmm1,       xmm2/m128, imm8(9)</para>
        ///   <para>  VROUNDSD xmm1, xmm2, xmm3/m128, imm8(9)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundToNegativeInfinityScalar(Vector128<double> upper, Vector128<double> value) => RoundToNegativeInfinityScalar(upper, value);

        /// <summary>
        ///   <para>__m128 _mm_round_ps (__m128 a, _MM_FROUND_TO_POS_INF |_MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDPS xmm1, xmm2/m128, imm8(10)</para>
        ///   <para>  VROUNDPS xmm1, xmm2/m128, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundToPositiveInfinity(Vector128<float> value) => RoundToPositiveInfinity(value);
        /// <summary>
        ///   <para>__m128 _mm_round_pd (__m128 a, _MM_FROUND_TO_POS_INF |_MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDPD xmm1, xmm2/m128, imm8(10)</para>
        ///   <para>  VROUNDPD xmm1, xmm2/m128, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundToPositiveInfinity(Vector128<double> value) => RoundToPositiveInfinity(value);

        /// <summary>
        ///   <para>__m128 _mm_round_ss (__m128 a, _MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSS xmm1,       xmm2/m128, imm8(10)</para>
        ///   <para>  VROUNDSS xmm1, xmm2, xmm3/m128, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundToPositiveInfinityScalar(Vector128<float> value) => RoundToPositiveInfinityScalar(value);
        /// <summary>
        ///   <para>__m128 _mm_round_ss (__m128 a, __m128 b, _MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSS xmm1,       xmm2/m128, imm8(10)</para>
        ///   <para>  VROUNDSS xmm1, xmm2, xmm3/m128, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundToPositiveInfinityScalar(Vector128<float> upper, Vector128<float> value) => RoundToPositiveInfinityScalar(upper, value);
        /// <summary>
        ///   <para>__m128d _mm_round_sd (__m128d a, _MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSD xmm1,       xmm2/m128, imm8(10)</para>
        ///   <para>  VROUNDSD xmm1, xmm2, xmm3/m128, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundToPositiveInfinityScalar(Vector128<double> value) => RoundToPositiveInfinityScalar(value);
        /// <summary>
        ///   <para>__m128d _mm_round_sd (__m128d a, __m128d b, _MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSD xmm1,       xmm2/m128, imm8(10)</para>
        ///   <para>  VROUNDSD xmm1, xmm2, xmm3/m128, imm8(10)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundToPositiveInfinityScalar(Vector128<double> upper, Vector128<double> value) => RoundToPositiveInfinityScalar(upper, value);

        /// <summary>
        ///   <para>__m128 _mm_round_ps (__m128 a, _MM_FROUND_TO_ZERO |_MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDPS xmm1, xmm2/m128, imm8(11)</para>
        ///   <para>  VROUNDPS xmm1, xmm2/m128, imm8(11)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundToZero(Vector128<float> value) => RoundToZero(value);
        /// <summary>
        ///   <para>__m128 _mm_round_pd (__m128 a, _MM_FROUND_TO_ZERO |_MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDPD xmm1, xmm2/m128, imm8(11)</para>
        ///   <para>  VROUNDPD xmm1, xmm2/m128, imm8(11)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundToZero(Vector128<double> value) => RoundToZero(value);

        /// <summary>
        ///   <para>__m128 _mm_round_ss (__m128 a, _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSS xmm1,       xmm2/m128, imm8(11)</para>
        ///   <para>  VROUNDSS xmm1, xmm2, xmm3/m128, imm8(11)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundToZeroScalar(Vector128<float> value) => RoundToZeroScalar(value);
        /// <summary>
        ///   <para>__m128 _mm_round_ss (__m128 a, __m128 b, _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSS xmm1,       xmm2/m128, imm8(11)</para>
        ///   <para>  VROUNDSS xmm1, xmm2, xmm3/m128, imm8(11)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<float> RoundToZeroScalar(Vector128<float> upper, Vector128<float> value) => RoundToZeroScalar(upper, value);
        /// <summary>
        ///   <para>__m128d _mm_round_sd (__m128d a, _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSD xmm1,       xmm2/m128, imm8(11)</para>
        ///   <para>  VROUNDSD xmm1, xmm2, xmm3/m128, imm8(11)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundToZeroScalar(Vector128<double> value) => RoundToZeroScalar(value);
        /// <summary>
        ///   <para>__m128d _mm_round_sd (__m128d a, __m128 b, _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC)</para>
        ///   <para>   ROUNDSD xmm1,       xmm2/m128, imm8(11)</para>
        ///   <para>  VROUNDSD xmm1, xmm2, xmm3/m128, imm8(11)</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> RoundToZeroScalar(Vector128<double> upper, Vector128<double> value) => RoundToZeroScalar(upper, value);

        /// <summary>
        ///   <para>int _mm_testc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; CF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector128<sbyte> left, Vector128<sbyte> right) => TestC(left, right);
        /// <summary>
        ///   <para>int _mm_testc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; CF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector128<byte> left, Vector128<byte> right) => TestC(left, right);
        /// <summary>
        ///   <para>int _mm_testc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; CF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector128<short> left, Vector128<short> right) => TestC(left, right);
        /// <summary>
        ///   <para>int _mm_testc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; CF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector128<ushort> left, Vector128<ushort> right) => TestC(left, right);
        /// <summary>
        ///   <para>int _mm_testc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; CF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector128<int> left, Vector128<int> right) => TestC(left, right);
        /// <summary>
        ///   <para>int _mm_testc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; CF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector128<uint> left, Vector128<uint> right) => TestC(left, right);
        /// <summary>
        ///   <para>int _mm_testc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; CF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector128<long> left, Vector128<long> right) => TestC(left, right);
        /// <summary>
        ///   <para>int _mm_testc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; CF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; CF=1</para>
        /// </summary>
        public static bool TestC(Vector128<ulong> left, Vector128<ulong> right) => TestC(left, right);

        /// <summary>
        ///   <para>int _mm_testnzc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector128<sbyte> left, Vector128<sbyte> right) => TestNotZAndNotC(left, right);
        /// <summary>
        ///   <para>int _mm_testnzc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector128<byte> left, Vector128<byte> right) => TestNotZAndNotC(left, right);
        /// <summary>
        ///   <para>int _mm_testnzc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector128<short> left, Vector128<short> right) => TestNotZAndNotC(left, right);
        /// <summary>
        ///   <para>int _mm_testnzc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector128<ushort> left, Vector128<ushort> right) => TestNotZAndNotC(left, right);
        /// <summary>
        ///   <para>int _mm_testnzc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector128<int> left, Vector128<int> right) => TestNotZAndNotC(left, right);
        /// <summary>
        ///   <para>int _mm_testnzc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector128<uint> left, Vector128<uint> right) => TestNotZAndNotC(left, right);
        /// <summary>
        ///   <para>int _mm_testnzc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector128<long> left, Vector128<long> right) => TestNotZAndNotC(left, right);
        /// <summary>
        ///   <para>int _mm_testnzc_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool TestNotZAndNotC(Vector128<ulong> left, Vector128<ulong> right) => TestNotZAndNotC(left, right);

        /// <summary>
        ///   <para>int _mm_testz_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector128<sbyte> left, Vector128<sbyte> right) => TestZ(left, right);
        /// <summary>
        ///   <para>int _mm_testz_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector128<byte> left, Vector128<byte> right) => TestZ(left, right);
        /// <summary>
        ///   <para>int _mm_testz_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector128<short> left, Vector128<short> right) => TestZ(left, right);
        /// <summary>
        ///   <para>int _mm_testz_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector128<ushort> left, Vector128<ushort> right) => TestZ(left, right);
        /// <summary>
        ///   <para>int _mm_testz_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector128<int> left, Vector128<int> right) => TestZ(left, right);
        /// <summary>
        ///   <para>int _mm_testz_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector128<uint> left, Vector128<uint> right) => TestZ(left, right);
        /// <summary>
        ///   <para>int _mm_testz_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector128<long> left, Vector128<long> right) => TestZ(left, right);
        /// <summary>
        ///   <para>int _mm_testz_si128 (__m128i a, __m128i b)</para>
        ///   <para>   PTEST xmm1, xmm2/m128    ; ZF=1</para>
        ///   <para>  VPTEST xmm1, xmm2/m128    ; ZF=1</para>
        /// </summary>
        public static bool TestZ(Vector128<ulong> left, Vector128<ulong> right) => TestZ(left, right);
    }
}
