// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 GFNI hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Gfni : Sse41
    {
        internal Gfni() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the X86 GFNI hardware instructions that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : Sse41.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }
        }

        [Intrinsic]
        public abstract class V256
        {
            internal V256() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>__m256i _mm256_gf2p8affineinv_epi64_epi8 (__m256i x, __m256i A, int b)</para>
            ///   <para>   GF2P8AFFINEINVQB ymm1, ymm2/m256, imm8</para>
            ///   <para>  VGF2P8AFFINEINVQB ymm1, ymm2, ymm3/m256, imm8</para>
            ///   <para>  VGF2P8AFFINEINVQB ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<byte> GaloisFieldAffineTransformInverse(Vector256<byte> x, Vector256<byte> a, [ConstantExpected] byte b) => GaloisFieldAffineTransformInverse(x, a, b);
            /// <summary>
            ///   <para>__m256i _mm256_gf2p8affine_epi64_epi8 (__m256i x, __m256i A, int b)</para>
            ///   <para>   GF2P8AFFINEQB ymm1, ymm2/m256, imm8</para>
            ///   <para>  VGF2P8AFFINEQB ymm1, ymm2, ymm3/m256, imm8</para>
            ///   <para>  VGF2P8AFFINEQB ymm1{k1}{z}, ymm2, ymm3/m256/m64bcst, imm8</para>
            /// </summary>
            public static Vector256<byte> GaloisFieldAffineTransform(Vector256<byte> x, Vector256<byte> a, [ConstantExpected] byte b) => GaloisFieldAffineTransform(x, a, b);
            /// <summary>
            ///   <para>__m256i _mm256_gf2p8mul_epi8 (__m256i a, __m256i b)</para>
            ///   <para>   GF2P8MULB ymm1, ymm2/m256</para>
            ///   <para>  VGF2P8MULB ymm1, ymm2, ymm3/m256</para>
            ///   <para>  VGF2P8MULB ymm1{k1}{z}, ymm2, ymm3/m256</para>
            /// </summary>
            public static Vector256<byte> GaloisFieldMultiply(Vector256<byte> left, Vector256<byte> right) => GaloisFieldMultiply(left, right);
        }

        [Intrinsic]
        public abstract class V512
        {
            internal V512() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>__m512i _mm512_gf2p8affineinv_epi64_epi8 (__m512i x, __m512i A, int b)</para>
            ///   <para>   GF2P8AFFINEINVQB zmm1, zmm2/m512, imm8</para>
            ///   <para>  VGF2P8AFFINEINVQB zmm1, zmm2, zmm3/m512, imm8</para>
            ///   <para>  VGF2P8AFFINEINVQB zmm1{k1}{z}, zmm2, zmm3/m512/m64bcst, imm8</para>
            /// </summary>
            public static Vector512<byte> GaloisFieldAffineTransformInverse(Vector512<byte> x, Vector512<byte> a, [ConstantExpected] byte b) => GaloisFieldAffineTransformInverse(x, a, b);
            /// <summary>
            ///   <para>__m512i _mm512_gf2p8affine_epi64_epi8 (__m512i x, __m512i A, int b)</para>
            ///   <para>   GF2P8AFFINEQB zmm1, zmm2/m512, imm8</para>
            ///   <para>  VGF2P8AFFINEQB zmm1, zmm2, zmm3/m512, imm8</para>
            ///   <para>  VGF2P8AFFINEQB zmm1{k1}{z}, zmm2, zmm3/m512/m64bcst, imm8</para>
            /// </summary>
            public static Vector512<byte> GaloisFieldAffineTransform(Vector512<byte> x, Vector512<byte> a, [ConstantExpected] byte b) => GaloisFieldAffineTransform(x, a, b);
            /// <summary>
            ///   <para>__m512i _mm512_gf2p8mul_epi8 (__m512i a, __m512i b)</para>
            ///   <para>   GF2P8MULB zmm1, zmm2/m512</para>
            ///   <para>  VGF2P8MULB zmm1, zmm2, zmm3/m512</para>
            ///   <para>  VGF2P8MULB zmm1{k1}{z}, zmm2, zmm3/m512</para>
            /// </summary>
            public static Vector512<byte> GaloisFieldMultiply(Vector512<byte> left, Vector512<byte> right) => GaloisFieldMultiply(left, right);
        }

        /// <summary>
        ///   <para>__m128i _mm_gf2p8affineinv_epi64_epi8 (__m128i x, __m128i A, int b)</para>
        ///   <para>   GF2P8AFFINEINVQB xmm1, xmm2/m128, imm8</para>
        ///   <para>  VGF2P8AFFINEINVQB xmm1, xmm2, xmm3/m128, imm8</para>
        ///   <para>  VGF2P8AFFINEINVQB xmm1{k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<byte> GaloisFieldAffineTransformInverse(Vector128<byte> x, Vector128<byte> a, [ConstantExpected] byte b) => GaloisFieldAffineTransformInverse(x, a, b);
        /// <summary>
        ///   <para>__m128i _mm_gf2p8affine_epi64_epi8 (__m128i x, __m128i A, int b)</para>
        ///   <para>   GF2P8AFFINEQB xmm1, xmm2/m128, imm8</para>
        ///   <para>  VGF2P8AFFINEQB xmm1, xmm2, xmm3/m128, imm8</para>
        ///   <para>  VGF2P8AFFINEQB xmm1{k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<byte> GaloisFieldAffineTransform(Vector128<byte> x, Vector128<byte> a, [ConstantExpected] byte b) => GaloisFieldAffineTransform(x, a, b);
        /// <summary>
        ///   <para>__m128i _mm_gf2p8mul_epi8 (__m128i a, __m128i b)</para>
        ///   <para>   GF2P8MULB xmm1, xmm2/m128</para>
        ///   <para>  VGF2P8MULB xmm1, xmm2, xmm3/m128</para>
        ///   <para>  VGF2P8MULB xmm1{k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> GaloisFieldMultiply(Vector128<byte> left, Vector128<byte> right) => GaloisFieldMultiply(left, right);
    }
}
