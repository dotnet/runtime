// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>This class provides access to X86 CLMUL hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Pclmulqdq : Sse2
    {
        internal Pclmulqdq() { }

        /// <summary>Gets <c>true</c> if the APIs in this class are supported; otherwise, <c>false</c> which indicates they will throw <see cref="PlatformNotSupportedException" />.</summary>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>This class provides access to the x86 CLMUL hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : Sse2.X64
        {
            internal X64() { }

            /// <summary>Gets <c>true</c> if the APIs in this class are supported; otherwise, <c>false</c> which indicates they will throw <see cref="PlatformNotSupportedException" />.</summary>
            public static new bool IsSupported { get => IsSupported; }
        }

        /// <summary>
        /// __m128i _mm_clmulepi64_si128 (__m128i a, __m128i b, const int imm8)
        ///    PCLMULQDQ xmm1,       xmm2/m128, imm8
        ///   VPCLMULQDQ xmm1, xmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector128<long> CarrylessMultiply(Vector128<long> left, Vector128<long> right, [ConstantExpected] byte control) => CarrylessMultiply(left, right, control);
        /// <summary>
        /// __m128i _mm_clmulepi64_si128 (__m128i a, __m128i b, const int imm8)
        ///    PCLMULQDQ xmm1,       xmm2/m128, imm8
        ///   VPCLMULQDQ xmm1, xmm2, xmm3/m128, imm8
        /// </summary>
        public static Vector128<ulong> CarrylessMultiply(Vector128<ulong> left, Vector128<ulong> right, [ConstantExpected] byte control) => CarrylessMultiply(left, right, control);
    }
}
