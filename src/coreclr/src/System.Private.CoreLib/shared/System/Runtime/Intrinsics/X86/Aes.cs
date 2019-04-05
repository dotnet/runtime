// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel AES hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Aes : Sse2
    {
        internal Aes() { }

        public new static bool IsSupported { get => IsSupported; }

        /// <summary>
        /// __m128i _mm_aesdec_si128 (__m128i a, __m128i RoundKey)
        ///   AESDEC xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> Decrypt(Vector128<byte> value, Vector128<byte> roundKey) => Decrypt(value, roundKey);

        /// <summary>
        /// __m128i _mm_aesdeclast_si128 (__m128i a, __m128i RoundKey)
        ///   AESDECLAST xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> DecryptLast(Vector128<byte> value, Vector128<byte> roundKey) => DecryptLast(value, roundKey);

        /// <summary>
        /// __m128i _mm_aesenc_si128 (__m128i a, __m128i RoundKey)
        ///   AESENC xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> Encrypt(Vector128<byte> value, Vector128<byte> roundKey) => Encrypt(value, roundKey);

        /// <summary>
        /// __m128i _mm_aesenclast_si128 (__m128i a, __m128i RoundKey)
        ///   AESENCLAST xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> EncryptLast(Vector128<byte> value, Vector128<byte> roundKey) => EncryptLast(value, roundKey);

        /// <summary>
        /// __m128i _mm_aesimc_si128 (__m128i a)
        ///   AESIMC xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> InverseMixColumns(Vector128<byte> value) => InverseMixColumns(value);

        /// <summary>
        /// __m128i _mm_aeskeygenassist_si128 (__m128i a, const int imm8)
        ///   AESKEYGENASSIST xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<byte> KeygenAssist(Vector128<byte> value, byte control) => KeygenAssist(value, control);

    }

}
