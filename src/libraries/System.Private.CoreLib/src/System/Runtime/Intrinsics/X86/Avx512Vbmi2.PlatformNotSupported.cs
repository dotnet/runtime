// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 AVX512VBMI2 hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Avx512Vbmi2 : Avx512Vbmi
    {
        internal Avx512Vbmi2() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>Provides access to the x86 AVX512VBMI2+VL hardware instructions via intrinsics.</summary>
        public new abstract class VL : Avx512Vbmi.VL
        {
            internal VL() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            ///   <para>__m128i _mm_mask_compress_epi8 (__m128i s, __mmask16 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSB xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<byte> Compress(Vector128<byte> merge, Vector128<byte> mask, Vector128<byte> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_compress_epi16 (__m128i s, __mmask8 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSW xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<short> Compress(Vector128<short> merge, Vector128<short> mask, Vector128<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_compress_epi8 (__m128i s, __mmask16 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSB xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<sbyte> Compress(Vector128<sbyte> merge, Vector128<sbyte> mask, Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_compress_epi16 (__m128i s, __mmask8 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSW xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<ushort> Compress(Vector128<ushort> merge, Vector128<ushort> mask, Vector128<ushort> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m256i _mm256_mask_compress_epi8 (__m256i s, __mmask32 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSB ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<byte> Compress(Vector256<byte> merge, Vector256<byte> mask, Vector256<byte> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_compress_epi16 (__m256i s, __mmask16 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSW ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<short> Compress(Vector256<short> merge, Vector256<short> mask, Vector256<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_compress_epi8 (__m256i s, __mmask32 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSB ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<sbyte> Compress(Vector256<sbyte> merge, Vector256<sbyte> mask, Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_compress_epi16 (__m256i s, __mmask16 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSW ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<ushort> Compress(Vector256<ushort> merge, Vector256<ushort> mask, Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m128i _mm_mask_compressstoreu_epi8 (void * s, __mmask16 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSB m128 {k1}{z}, xmm2</para>
            /// </summary>
            public static unsafe void CompressStore(byte* address, Vector128<byte> mask, Vector128<byte> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_compressstoreu_epi16 (void * s, __mmask8 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSW m128 {k1}{z}, xmm2</para>
            /// </summary>
            public static unsafe void CompressStore(short* address, Vector128<short> mask, Vector128<short> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_compressstoreu_epi8 (void * s, __mmask16 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSB m128 {k1}{z}, xmm2</para>
            /// </summary>
            public static unsafe void CompressStore(sbyte* address, Vector128<sbyte> mask, Vector128<sbyte> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_compressstoreu_epi16 (void * s, __mmask8 k, __m128i a)</para>
            ///   <para>  VPCOMPRESSW m128 {k1}{z}, xmm2</para>
            /// </summary>
            public static unsafe void CompressStore(ushort* address, Vector128<ushort> mask, Vector128<ushort> source) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>void _mm256_mask_compressstoreu_epi8 (void * s, __mmask32 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSB m256 {k1}{z}, ymm2</para>
            /// </summary>
            public static unsafe void CompressStore(byte* address, Vector256<byte> mask, Vector256<byte> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>void _mm256_mask_compressstoreu_epi16 (void * s, __mmask16 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSW m256 {k1}{z}, ymm2</para>
            /// </summary>
            public static unsafe void CompressStore(short* address, Vector256<short> mask, Vector256<short> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>void _mm256_mask_compressstoreu_epi8 (void * s, __mmask32 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSB m256 {k1}{z}, ymm2</para>
            /// </summary>
            public static unsafe void CompressStore(sbyte* address, Vector256<sbyte> mask, Vector256<sbyte> source) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>void _mm256_mask_compressstoreu_epi16 (void * s, __mmask16 k, __m256i a)</para>
            ///   <para>  VPCOMPRESSW m256 {k1}{z}, ymm2</para>
            /// </summary>
            public static unsafe void CompressStore(ushort* address, Vector256<ushort> mask, Vector256<ushort> source) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m128i _mm_mask_expand_epi8 (__m128i s, __mmask16 k, __m128i a)</para>
            ///   <para>  VPEXPANDB xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<byte> Expand(Vector128<byte> merge, Vector128<byte> mask, Vector128<byte> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_expand_epi16 (__m128i s, __mmask8 k, __m128i a)</para>
            ///   <para>  VPEXPANDW xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<short> Expand(Vector128<short> merge, Vector128<short> mask, Vector128<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_expand_epi8 (__m128i s, __mmask16 k, __m128i a)</para>
            ///   <para>  VPEXPANDB xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<sbyte> Expand(Vector128<sbyte> merge, Vector128<sbyte> mask, Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_expand_epi16 (__m128i s, __mmask8 k, __m128i a)</para>
            ///   <para>  VPEXPANDW xmm1 {k1}{z}, xmm2</para>
            /// </summary>
            public static Vector128<ushort> Expand(Vector128<ushort> merge, Vector128<ushort> mask, Vector128<ushort> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m256i _mm256_mask_expand_epi8 (__m256i s, __mmask32 k, __m256i a)</para>
            ///   <para>  VPEXPANDB ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<byte> Expand(Vector256<byte> merge, Vector256<byte> mask, Vector256<byte> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_expand_epi16 (__m256i s, __mmask16 k, __m256i a)</para>
            ///   <para>  VPEXPANDW ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<short> Expand(Vector256<short> merge, Vector256<short> mask, Vector256<short> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_expand_epi8 (__m256i s, __mmask32 k, __m256i a)</para>
            ///   <para>  VPEXPANDB ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<sbyte> Expand(Vector256<sbyte> merge, Vector256<sbyte> mask, Vector256<sbyte> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_expand_epi16 (__m256i s, __mmask16 k, __m256i a)</para>
            ///   <para>  VPEXPANDW ymm1 {k1}{z}, ymm2</para>
            /// </summary>
            public static Vector256<ushort> Expand(Vector256<ushort> merge, Vector256<ushort> mask, Vector256<ushort> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m128i _mm_mask_expandloadu_epi8 (__m128i s, __mmask16 k, void const * a)</para>
            ///   <para>  VPEXPANDB xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<byte> ExpandLoad(byte* address, Vector128<byte> mask, Vector128<byte> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_expandloadu_epi16 (__m128i s, __mmask8 k, void const * a)</para>
            ///   <para>  VPEXPANDW xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<short> ExpandLoad(short* address, Vector128<short> mask, Vector128<short> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_expandloadu_epi8 (__m128i s, __mmask16 k, void const * a)</para>
            ///   <para>  VPEXPANDB xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<sbyte> ExpandLoad(sbyte* address, Vector128<sbyte> mask, Vector128<sbyte> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m128i _mm_mask_expandloadu_epi16 (__m128i s, __mmask8 k, void const * a)</para>
            ///   <para>  VPEXPANDW xmm1 {k1}{z}, m128</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector128<ushort> ExpandLoad(ushort* address, Vector128<ushort> mask, Vector128<ushort> merge) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__m256i _mm256_mask_expandloadu_epi8 (__m256i s, __mmask32 k, void const * a)</para>
            ///   <para>  VPEXPANDB ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<byte> ExpandLoad(byte* address, Vector256<byte> mask, Vector256<byte> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_expandloadu_epi16 (__m256i s, __mmask16 k, void const * a)</para>
            ///   <para>  VPEXPANDW ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<short> ExpandLoad(short* address, Vector256<short> mask, Vector256<short> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_expandloadu_epi8 (__m256i s, __mmask32 k, void const * a)</para>
            ///   <para>  VPEXPANDB ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<sbyte> ExpandLoad(sbyte* address, Vector256<sbyte> mask, Vector256<sbyte> merge) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_mask_expandloadu_epi16 (__m256i s, __mmask16 k, void const * a)</para>
            ///   <para>  VPEXPANDW ymm1 {k1}{z}, m256</para>
            /// </summary>
            /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
            public static unsafe Vector256<ushort> ExpandLoad(ushort* address, Vector256<ushort> mask, Vector256<ushort> merge) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>Provides access to the x86 AVX512VBMI2 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class X64 : Avx512Vbmi.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        ///   <para>__m512i _mm512_mask_compress_epi8 (__m512i s, __mmask64 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSB zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<byte> Compress(Vector512<byte> merge, Vector512<byte> mask, Vector512<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_compress_epi16 (__m512i s, __mmask32 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSW zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<short> Compress(Vector512<short> merge, Vector512<short> mask, Vector512<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_compress_epi8 (__m512i s, __mmask64 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSB zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<sbyte> Compress(Vector512<sbyte> merge, Vector512<sbyte> mask, Vector512<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_compress_epi16 (__m512i s, __mmask32 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSW zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<ushort> Compress(Vector512<ushort> merge, Vector512<ushort> mask, Vector512<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_mask_compresstoreu_epi8 (void * s, __mmask64 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSB m512 {k1}{z}, zmm2</para>
        /// </summary>
        public static unsafe void CompressStore(byte* address, Vector512<byte> mask, Vector512<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_compresstoreu_epi16 (void * s, __mmask32 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSW m512 {k1}{z}, zmm2</para>
        /// </summary>
        public static unsafe void CompressStore(short* address, Vector512<short> mask, Vector512<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_compresstoreu_epi8 (void * s, __mmask64 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSB m512 {k1}{z}, zmm2</para>
        /// </summary>
        public static unsafe void CompressStore(sbyte* address, Vector512<sbyte> mask, Vector512<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_compresstoreu_epi16 (void * s, __mmask32 k, __m512i a)</para>
        ///   <para>  VPCOMPRESSW m512 {k1}{z}, zmm2</para>
        /// </summary>
        public static unsafe void CompressStore(ushort* address, Vector512<ushort> mask, Vector512<ushort> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_mask_expand_epi8 (__m512i s, __mmask64 k, __m512i a)</para>
        ///   <para>  VPEXPANDB zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<byte> Expand(Vector512<byte> merge, Vector512<byte> mask, Vector512<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_expand_epi16 (__m512i s, __mmask32 k, __m512i a)</para>
        ///   <para>  VPEXPANDW zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<short> Expand(Vector512<short> merge, Vector512<short> mask, Vector512<short> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_expand_epi8 (__m512i s, __mmask64 k, __m512i a)</para>
        ///   <para>  VPEXPANDB zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<sbyte> Expand(Vector512<sbyte> merge, Vector512<sbyte> mask, Vector512<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_expand_epi16 (__m512i s, __mmask32 k, __m512i a)</para>
        ///   <para>  VPEXPANDW zmm1 {k1}{z}, zmm2</para>
        /// </summary>
        public static Vector512<ushort> Expand(Vector512<ushort> merge, Vector512<ushort> mask, Vector512<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m512i _mm512_mask_expandloadu_epi8 (__m512i s, __mmask64 k, void * const a)</para>
        ///   <para>  VPEXPANDB zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<byte> ExpandLoad(byte* address, Vector512<byte> mask, Vector512<byte> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_expandloadu_epi16 (__m512i s, __mmask32 k, void * const a)</para>
        ///   <para>  VPEXPANDW zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<short> ExpandLoad(short* address, Vector512<short> mask, Vector512<short> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_expandloadu_epi8 (__m512i s, __mmask64 k, void * const a)</para>
        ///   <para>  VPEXPANDB zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<sbyte> ExpandLoad(sbyte* address, Vector512<sbyte> mask, Vector512<sbyte> merge) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m512i _mm512_mask_expandloadu_epi16 (__m512i s, __mmask32 k, void * const a)</para>
        ///   <para>  VPEXPANDW zmm1 {k1}{z}, m512</para>
        /// </summary>
        /// <remarks>The native and managed intrinsics have different order of parameters.</remarks>
        public static unsafe Vector512<ushort> ExpandLoad(ushort* address, Vector512<ushort> mask, Vector512<ushort> merge) { throw new PlatformNotSupportedException(); }
    }
}
