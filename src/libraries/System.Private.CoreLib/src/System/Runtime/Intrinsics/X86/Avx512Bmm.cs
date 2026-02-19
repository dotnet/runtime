// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Avx512Bmm : Avx512F
    {
        internal Avx512Bmm() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>
        ///   <para>__m128i _mm_bitrev_epi8 (__m128i values)</para>
        ///   <para>  VBITREV  xmm1{k1}{z}, xmm2/m128</para>
        /// </summary>
        public static Vector128<byte> ReverseBits(Vector128<byte> values) => ReverseBits(values);

        /// <summary>
        ///   <para>__m256i _mm256_bitrev_epi8 (__m256i values)</para>
        ///   <para>  VBITREV  ymm1{k1}{z}, ymm2/m256</para>
        /// </summary>
        public static Vector256<byte> ReverseBits(Vector256<byte> values) => ReverseBits(values);

        /// <summary>
        ///   <para>__m512i _mm512_bitrev_epi8 (__m512i values)</para>
        ///   <para>  VBITREV  zmm1{k1}{z}, zmm2/m512</para>
        /// </summary>
        public static Vector512<byte> ReverseBits(Vector512<byte> values) => ReverseBits(values);

        /// <summary>
        ///   <para>__m256i _mm256_bmacor16x16x16 (__m256i left, __m256i right, __m256i addend)</para>
        ///   <para>  VBMACOR16x16x16  ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> BitMultiplyMatrix16x16WithOrReduction(Vector256<ushort> left, Vector256<ushort> right, Vector256<ushort> addend) => BitMultiplyMatrix16x16WithOrReduction(left, right, addend);

        /// <summary>
        ///   <para>__m512i _mm512_bmacor16x16x16 (__m512i left, __m512i right, __m512i addend)</para>
        ///   <para>  VBMACOR16x16x16  zmm1, zmm2, zmm3/m256</para>
        /// </summary>
        public static Vector512<ushort> BitMultiplyMatrix16x16WithOrReduction(Vector512<ushort> left, Vector512<ushort> right, Vector512<ushort> addend) => BitMultiplyMatrix16x16WithOrReduction(left, right, addend);

        /// <summary>
        ///   <para>__m256i _mm256_bmacxor16x16x16 (__m256i left, __m256i right, __m256i addend)</para>
        ///   <para>  VBMACXOR16x16x16  ymm1, ymm2, ymm3/m256</para>
        /// </summary>
        public static Vector256<ushort> BitMultiplyMatrix16x16WithXorReduction(Vector256<ushort> left, Vector256<ushort> right, Vector256<ushort> addend) => BitMultiplyMatrix16x16WithXorReduction(left, right, addend);

        /// <summary>
        ///   <para>__m512i _mm512_bmacxor16x16x16 (__m512i left, __m512i right, __m512i addend)</para>
        ///   <para>  VBMACXOR16x16x16  zmm1, zmm2, zmm3/m256</para>
        /// </summary>
        public static Vector512<ushort> BitMultiplyMatrix16x16WithXorReduction(Vector512<ushort> left, Vector512<ushort> right, Vector512<ushort> addend) => BitMultiplyMatrix16x16WithXorReduction(left, right, addend);

        [Intrinsic]
        public new abstract class X64 : Avx512F.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }
        }
    }
}
