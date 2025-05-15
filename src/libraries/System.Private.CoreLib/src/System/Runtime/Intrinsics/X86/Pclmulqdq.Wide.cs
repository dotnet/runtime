// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    public abstract partial class Pclmulqdq : Sse2
    {
        [Intrinsic]
        public abstract class V256
        {
            internal V256() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>__m256i _mm256_clmulepi64_epi128 (__m256i a, __m256i b, const int imm8)</para>
            ///   <para>  VPCLMULQDQ ymm1, ymm2, ymm3/m256, imm8</para>
            /// </summary>
            public static Vector256<long> CarrylessMultiply(Vector256<long> left, Vector256<long> right, [ConstantExpected] byte control) => CarrylessMultiply(left, right, control);
            /// <summary>
            ///   <para>__m256i _mm256_clmulepi64_epi128 (__m256i a, __m256i b, const int imm8)</para>
            ///   <para>  VPCLMULQDQ ymm1, ymm2, ymm3/m256, imm8</para>
            /// </summary>
            public static Vector256<ulong> CarrylessMultiply(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected] byte control) => CarrylessMultiply(left, right, control);
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
            ///   <para>__m512i _mm512_clmulepi64_epi128 (__m512i a, __m512i b, const int imm8)</para>
            ///   <para>  VPCLMULQDQ zmm1, zmm2, zmm3/m512, imm8</para>
            /// </summary>
            public static Vector512<long> CarrylessMultiply(Vector512<long> left, Vector512<long> right, [ConstantExpected] byte control) => CarrylessMultiply(left, right, control);
            /// <summary>
            ///   <para>__m512i _mm512_clmulepi64_epi128 (__m512i a, __m512i b, const int imm8)</para>
            ///   <para>  VPCLMULQDQ zmm1, zmm2, zmm3/m512, imm8</para>
            /// </summary>
            public static Vector512<ulong> CarrylessMultiply(Vector512<ulong> left, Vector512<ulong> right, [ConstantExpected] byte control) => CarrylessMultiply(left, right, control);
        }
    }
}
