// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    [CLSCompliant(false)]
    public abstract class Sha256 : X86Base
    {
        internal Sha256() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>
        /// This performs an intermediate calculation for the next four SHA256 message values.
        /// Intrinsic: __m128i _mm_sha256msg1_epu32 (__m128i a, __m128i b)
        /// Instruction: sha256msg1 xmm, xmm
        /// CPUID Flags: SHA
        /// </summary>
        /// <param name="a">Initial SHA256 state</param>
        /// <param name="b">State</param>
        /// <returns>The updated SHA256 state</returns>
        public static Vector128<byte> MessageSchedule1(Vector128<byte> a, Vector128<byte> b)
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
        public static Vector128<byte> MessageSchedule2(Vector128<byte> a, Vector128<byte> b)
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
        public static Vector128<byte> TwoRounds(Vector128<byte> a, Vector128<byte> b, Vector128<byte> k)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
