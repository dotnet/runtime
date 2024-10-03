// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to the x86 AVXVNNI hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class AvxVnni : Avx2
    {
        internal AvxVnni() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>Provides access to the x86 AVXVNNI hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class X64 : Avx2.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        ///   <para>__m128i _mm_dpbusd_epi32 (__m128i src, __m128i a, __m128i b)</para>
        ///   <para>VPDPBUSD xmm, xmm, xmm/m128</para>
        /// </summary>
        public static Vector128<int> MultiplyWideningAndAdd(Vector128<int> addend, Vector128<byte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_dpwssd_epi32 (__m128i src, __m128i a, __m128i b)</para>
        ///   <para>VPDPWSSD xmm, xmm, xmm/m128</para>
        /// </summary>
        public static Vector128<int> MultiplyWideningAndAdd(Vector128<int> addend, Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_dpbusd_epi32 (__m256i src, __m256i a, __m256i b)</para>
        ///   <para>VPDPBUSD ymm, ymm, ymm/m256</para>
        /// </summary>
        public static Vector256<int> MultiplyWideningAndAdd(Vector256<int> addend, Vector256<byte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_dpwssd_epi32 (__m256i src, __m256i a, __m256i b)</para>
        ///   <para>VPDPWSSD ymm, ymm, ymm/m256</para>
        /// </summary>
        public static Vector256<int> MultiplyWideningAndAdd(Vector256<int> addend, Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_dpbusds_epi32 (__m128i src, __m128i a, __m128i b)</para>
        ///   <para>VPDPBUSDS xmm, xmm, xmm/m128</para>
        /// </summary>
        public static Vector128<int> MultiplyWideningAndAddSaturate(Vector128<int> addend, Vector128<byte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_dpwssds_epi32 (__m128i src, __m128i a, __m128i b)</para>
        ///   <para>VPDPWSSDS xmm, xmm, xmm/m128</para>
        /// </summary>
        public static Vector128<int> MultiplyWideningAndAddSaturate(Vector128<int> addend, Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_dpbusds_epi32 (__m256i src, __m256i a, __m256i b)</para>
        ///   <para>VPDPBUSDS ymm, ymm, ymm/m256</para>
        /// </summary>
        public static Vector256<int> MultiplyWideningAndAddSaturate(Vector256<int> addend, Vector256<byte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m256i _mm256_dpwssds_epi32 (__m256i src, __m256i a, __m256i b)</para>
        ///   <para>VPDPWSSDS ymm, ymm, ymm/m256</para>
        /// </summary>
        public static Vector256<int> MultiplyWideningAndAddSaturate(Vector256<int> addend, Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
    }
}
