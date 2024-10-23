// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 CLMUL hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Pclmulqdq : Sse2
    {
        internal Pclmulqdq() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>Provides access to the x86 CLMUL hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class X64 : Sse2.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        public abstract class V256
        {
            internal V256() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            ///   <para>__m256i _mm256_clmulepi64_epi128 (__m256i a, __m256i b, const int imm8)</para>
            ///   <para>  VPCLMULQDQ ymm1, ymm2, ymm3/m256, imm8</para>
            /// </summary>
            public static Vector256<long> CarrylessMultiply(Vector256<long> left, Vector256<long> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m256i _mm256_clmulepi64_epi128 (__m256i a, __m256i b, const int imm8)</para>
            ///   <para>  VPCLMULQDQ ymm1, ymm2, ymm3/m256, imm8</para>
            /// </summary>
            public static Vector256<ulong> CarrylessMultiply(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        }

        public abstract class V512
        {
            internal V512() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            ///   <para>__m512i _mm512_clmulepi64_epi128 (__m512i a, __m512i b, const int imm8)</para>
            ///   <para>  VPCLMULQDQ zmm1, zmm2, zmm3/m512, imm8</para>
            /// </summary>
            public static Vector512<long> CarrylessMultiply(Vector512<long> left, Vector512<long> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
            /// <summary>
            ///   <para>__m512i _mm512_clmulepi64_epi128 (__m512i a, __m512i b, const int imm8)</para>
            ///   <para>  VPCLMULQDQ zmm1, zmm2, zmm3/m512, imm8</para>
            /// </summary>
            public static Vector512<ulong> CarrylessMultiply(Vector512<ulong> left, Vector512<ulong> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        ///   <para>__m128i _mm_clmulepi64_si128 (__m128i a, __m128i b, const int imm8)</para>
        ///   <para>   PCLMULQDQ xmm1,       xmm2/m128, imm8</para>
        ///   <para>  VPCLMULQDQ xmm1, xmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector128<long> CarrylessMultiply(Vector128<long> left, Vector128<long> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        ///   <para>__m128i _mm_clmulepi64_si128 (__m128i a, __m128i b, const int imm8)</para>
        ///   <para>   PCLMULQDQ xmm1,       xmm2/m128, imm8</para>
        ///   <para>  VPCLMULQDQ xmm1, xmm2, xmm3/m128, imm8</para>
        /// </summary>
        public static Vector128<ulong> CarrylessMultiply(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
    }
}
