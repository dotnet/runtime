// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 AES hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Aes : Sse2
    {
        internal Aes() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>Provides access to the x86 AES hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class X64 : Sse2.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        /// __m128i _mm_aesdec_si128 (__m128i a, __m128i RoundKey)
        ///    AESDEC xmm1,       xmm2/m128
        ///   VAESDEC xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<byte> Decrypt(Vector128<byte> value, Vector128<byte> roundKey) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_aesdeclast_si128 (__m128i a, __m128i RoundKey)
        ///    AESDECLAST xmm1,       xmm2/m128
        ///   VAESDECLAST xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<byte> DecryptLast(Vector128<byte> value, Vector128<byte> roundKey) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_aesenc_si128 (__m128i a, __m128i RoundKey)
        ///    AESENC xmm1,       xmm2/m128
        ///   VAESENC xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<byte> Encrypt(Vector128<byte> value, Vector128<byte> roundKey) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_aesenclast_si128 (__m128i a, __m128i RoundKey)
        ///    AESENCLAST xmm1,       xmm2/m128
        ///   VAESENCLAST xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<byte> EncryptLast(Vector128<byte> value, Vector128<byte> roundKey) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_aesimc_si128 (__m128i a)
        ///    AESIMC xmm1, xmm2/m128
        ///   VAESIMC xmm1, xmm2/m128
        /// </summary>
        public static Vector128<byte> InverseMixColumns(Vector128<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_aeskeygenassist_si128 (__m128i a, const int imm8)
        ///    AESKEYGENASSIST xmm1, xmm2/m128, imm8
        ///   VAESKEYGENASSIST xmm1, xmm2/m128, imm8
        /// </summary>
        public static Vector128<byte> KeygenAssist(Vector128<byte> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }
    }
}
