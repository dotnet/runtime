// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.Intrinsics.X86
{
    [CLSCompliant(false)]
    [RequiresPreviewFeatures]
    public abstract class AvxVnni : Avx2
    {
        internal AvxVnni() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        public new abstract class X64 : Avx2.X64
        {
            internal X64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        /// __m128i _mm_dpbusd_epi32 (__m128i src, __m128i a, __m128i b)
        /// VPDPBUSD xmm, xmm, xmm/m128
        /// </summary>
        public static Vector128<int> MultiplyWideningAndAdd(Vector128<int> addend, Vector128<byte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_dpwssd_epi32 (__m128i src, __m128i a, __m128i b)
        /// VPDPWSSD xmm, xmm, xmm/m128
        /// </summary>
        public static Vector128<int> MultiplyWideningAndAdd(Vector128<int> addend, Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_dpbusd_epi32 (__m256i src, __m256i a, __m256i b)
        /// VPDPBUSD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> MultiplyWideningAndAdd(Vector256<int> addend, Vector256<byte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_dpwssd_epi32 (__m256i src, __m256i a, __m256i b)
        /// VPDPWSSD ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> MultiplyWideningAndAdd(Vector256<int> addend, Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_dpbusds_epi32 (__m128i src, __m128i a, __m128i b)
        /// VPDPBUSDS xmm, xmm, xmm/m128
        /// </summary>
        public static Vector128<int> MultiplyWideningAndAddSaturate(Vector128<int> addend, Vector128<byte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_dpwssds_epi32 (__m128i src, __m128i a, __m128i b)
        /// VPDPWSSDS xmm, xmm, xmm/m128
        /// </summary>
        public static Vector128<int> MultiplyWideningAndAddSaturate(Vector128<int> addend, Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_dpbusds_epi32 (__m256i src, __m256i a, __m256i b)
        /// VPDPBUSDS ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> MultiplyWideningAndAddSaturate(Vector256<int> addend, Vector256<byte> left, Vector256<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m256i _mm256_dpwssds_epi32 (__m256i src, __m256i a, __m256i b)
        /// VPDPWSSDS ymm, ymm, ymm/m256
        /// </summary>
        public static Vector256<int> MultiplyWideningAndAddSaturate(Vector256<int> addend, Vector256<short> left, Vector256<short> right) { throw new PlatformNotSupportedException(); }
    }
}
