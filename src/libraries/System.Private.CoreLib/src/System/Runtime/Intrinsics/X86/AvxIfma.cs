// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to the x86 AVX-IFMA hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class AvxIfma : Avx2
    {
        internal AvxIfma() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the x86 AVX-IFMA hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : Avx2.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }
        }

        /// <summary>
        ///   <para>__m128i _mm_madd52lo_epu64 (__m128i a, __m128i b, __m128i c)</para>
        ///   <para>VPMADD52LUQ xmm, xmm, xmm/m128</para>
        /// </summary>
        public static Vector128<ulong> MultiplyAdd52Low(Vector128<ulong> addend, Vector128<ulong> left, Vector128<ulong> right) => MultiplyAdd52Low(addend, left, right);

        /// <summary>
        ///   <para>__m256i _mm256_madd52lo_epu64 (__m256i a, __m256i b, __m256i c)</para>
        ///   <para>VPMADD52LUQ ymm, ymm, ymm/m256</para>
        /// </summary>
        public static Vector256<ulong> MultiplyAdd52Low(Vector256<ulong> addend, Vector256<ulong> left, Vector256<ulong> right) => MultiplyAdd52Low(addend, left, right);

        /// <summary>
        ///   <para>__m128i _mm_madd52hi_epu64 (__m128i a, __m128i b, __m128i c)</para>
        ///   <para>VPMADD52HUQ xmm, xmm, xmm/m128</para>
        /// </summary>
        public static Vector128<ulong> MultiplyAdd52High(Vector128<ulong> addend, Vector128<ulong> left, Vector128<ulong> right) => MultiplyAdd52High(addend, left, right);

        /// <summary>
        ///   <para>__m256i _mm256_madd52hi_epu64 (__m256i a, __m256i b, __m256i c)</para>
        ///   <para>VPMADD52HUQ ymm, ymm, ymm/m256</para>
        /// </summary>
        public static Vector256<ulong> MultiplyAdd52High(Vector256<ulong> addend, Vector256<ulong> left, Vector256<ulong> right) => MultiplyAdd52High(addend, left, right);

        /// <summary>Provides access to the x86 AVX-512-IFMA hardware instructions, that operate on 512-bit vectors, via intrinsics.</summary>
        [Intrinsic]
        public abstract class V512
        {
            internal V512() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static bool IsSupported { [Intrinsic] get => IsSupported; }

            /// <summary>
            ///   <para>__m512i _mm512_madd52lo_epu64 (__m512i a, __m512i b, __m512i c)</para>
            ///   <para>VPMADD52LUQ zmm, zmm, zmm/m512</para>
            /// </summary>
            [Intrinsic]
            public static Vector512<ulong> MultiplyAdd52Low(Vector512<ulong> addend, Vector512<ulong> left, Vector512<ulong> right) => MultiplyAdd52Low(addend, left, right);

            /// <summary>
            ///   <para>__m512i _mm512_madd52hi_epu64 (__m512i a, __m512i b, __m512i c)</para>
            ///   <para>VPMADD52HUQ zmm, zmm, zmm/m512</para>
            /// </summary>
            [Intrinsic]
            public static Vector512<ulong> MultiplyAdd52High(Vector512<ulong> addend, Vector512<ulong> left, Vector512<ulong> right) => MultiplyAdd52High(addend, left, right);
        }
    }
}
