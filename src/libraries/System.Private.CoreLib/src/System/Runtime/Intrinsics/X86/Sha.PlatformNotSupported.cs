// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;


namespace System.Runtime.Intrinsics.X86
{
    [CLSCompliant(false)]
    public abstract class Sha : X86Base
    {
        internal Sha() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>
        /// This performs an intermediate calculation for the next four SHA1 message values.
        /// Intrinsic: __m128i _mm_sha1msg1_epu32 (__m128i a, __m128i b)
        /// Instruction: sha1msg1 xmm, xmm
        /// CPUID Flags: SHA
        /// </summary>
        /// <param name="a">Initial SHA1 state</param>
        /// <param name="b">State</param>
        /// <returns>The updated SHA1 state</returns>
        public static Vector128<byte> Sha1MessageSchedule1(Vector128<byte> a, Vector128<byte> b)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// This performs the final calculation using the intermediate result from <paramref name="a"/> and the previous message in <paramref name="b"/>
        /// Intrinsic: __m128i _mm_sha1msg2_epu32 (__m128i a, __m128i b)
        /// Instruction: sha1msg2 xmm, xmm
        /// CPUID Flags: SHA
        /// </summary>
        /// <param name="a">Intermediate result from <see cref="MessageSchedule1"/></param>
        /// <param name="b">The previous message</param>
        /// <returns>The SHA1 result</returns>
        public static Vector128<byte> Sha1MessageSchedule2(Vector128<byte> a, Vector128<byte> b)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Calculate SHA1 state E with four rounds operating the current SHA1 state variable <paramref name="a"/> and add this to the scheduled values in <paramref name="b"/>
        /// Intrinsic: __m128i _mm_sha1nexte_epu32 (__m128i a, __m128i b)
        /// Instruction: sha1nexte xmm, xmm
        /// CPUID Flags: SHA
        /// </summary>
        /// <param name="a">The current SHA1 state</param>
        /// <param name="b">Scheduled values</param>
        /// <returns>E</returns>
        public static Vector128<byte> Sha1NextE(Vector128<byte> a, Vector128<byte> b)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// This performs four rounds of SHA1 using the initial SHA 1 state from <paramref name="a"/> and pre-processed sum of the next 4 round message values and return the updated SHA1 state
        /// Intrinsic: __m128i _mm_sha1rnds4_epu32 (__m128i a, __m128i b, const int func)
        /// Instruction: sha1rnds4 xmm, xmm, imm8
        /// CPUID Flags: SHA
        /// </summary>
        /// <param name="a">The initial SHA1 state</param>
        /// <param name="b">Value from <see cref="NextE"/></param>
        /// <param name="func"></param>
        /// <returns>The updated SHA1 state</returns>
        public static Vector128<byte> Sha1FourRounds(Vector128<byte> a, Vector128<byte> b, byte func)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// This performs an intermediate calculation for the next four SHA256 message values.
        /// Intrinsic: __m128i _mm_sha256msg1_epu32 (__m128i a, __m128i b)
        /// Instruction: sha256msg1 xmm, xmm
        /// CPUID Flags: SHA
        /// </summary>
        /// <param name="a">Initial SHA256 state</param>
        /// <param name="b">State</param>
        /// <returns>The updated SHA256 state</returns>
        public static Vector128<byte> Sha256MessageSchedule1(Vector128<byte> a, Vector128<byte> b)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// This performs the final calculation using the intermediate result from <paramref name="a"/> and the previous message in <paramref name="b"/>
        /// Intrinsic: __m128i _mm_sha256msg2_epu32 (__m128i a, __m128i b)
        /// Instruction: sha256msg2 xmm, xmm
        /// CPUID Flags: SHA
        /// </summary>
        /// <param name="a">Intermediate result from <see cref="MessageSchedule1"/></param>
        /// <param name="b">The previous message</param>
        /// <returns>The SHA256 result</returns>
        public static Vector128<byte> Sha256MessageSchedule2(Vector128<byte> a, Vector128<byte> b)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// This performs four rounds of SHA256 using the initial SHA256 state from <paramref name="a"/> and pre-processed sum of the next 4 round message values and return the updated SHA256 state
        /// Intrinsic: __m128i _mm_sha256rnds2_epu32 (__m128i a, __m128i b, __m128i k)
        /// Instruction: sha256rnds2 xmm, xmm
        /// CPUID Flags: SHA
        /// </summary>
        /// <param name="a">The initial SHA256 state</param>
        /// <param name="b">The initial SHA256 state</param>
        /// <param name="k">The corresponding round constants</param>
        /// <returns>The updated SHA256 state</returns>
        public static Vector128<byte> Sha256TwoRounds(Vector128<byte> a, Vector128<byte> b, Vector128<byte> k)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
