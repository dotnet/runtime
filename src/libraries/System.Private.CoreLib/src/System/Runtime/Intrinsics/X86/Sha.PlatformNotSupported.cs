// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 SHA hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Sha : Sse2
    {
        internal Sha() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>Provides access to the x86 SHA hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class X64 : Sse2.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        ///   <para>__m128i _mm_sha1rnds4_epu32 (__m128i a, __m128i b, const int func)</para>
        ///   <para>  SHA1RNDS4 xmm1, xmm2/m128, imm8</para>
        /// </summary>
        public static Vector128<byte> Sha1FourRounds(Vector128<byte> a, Vector128<byte> b, [ConstantExpected] byte func) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_sha1msg1_epu32 (__m128i a, __m128i b)</para>
        ///   <para>  SHA1MSG1 xmm1, xmm2/m128</para>
        /// </summary>
        public static Vector128<byte> Sha1MessageSchedule1(Vector128<byte> a, Vector128<byte> b) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_sha1msg2_epu32 (__m128i a, __m128i b)</para>
        ///   <para>  SHA1MSG2 xmm1, xmm2/m128</para>
        /// </summary>
        public static Vector128<byte> Sha1MessageSchedule2(Vector128<byte> a, Vector128<byte> b) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_sha1nexte_epu32 (__m128i a, __m128i b)</para>
        ///   <para>  SHA1NEXTE xmm1, xmm2/m128</para>
        /// </summary>
        public static Vector128<byte> Sha1NextE(Vector128<byte> a, Vector128<byte> b) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_sha256msg1_epu32 (__m128i a, __m128i b)</para>
        ///   <para>  SHA256MSG1 xmm1, xmm2/m128</para>
        /// </summary>
        public static Vector128<byte> Sha256MessageSchedule1(Vector128<byte> a, Vector128<byte> b) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_sha256msg2_epu32 (__m128i a, __m128i b)</para>
        ///   <para>  SHA256MSG2 xmm1, xmm2/m128</para>
        /// </summary>
        public static Vector128<byte> Sha256MessageSchedule2(Vector128<byte> a, Vector128<byte> b) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>__m128i _mm_sha256rnds2_epu32 (__m128i a, __m128i b, __m128i k)</para>
        ///   <para>  SHA256RNDS2 xmm1, xmm2/m128, &lt;XMM0&gt;</para>
        /// </summary>
        public static Vector128<byte> Sha256TwoRounds(Vector128<byte> a, Vector128<byte> b, Vector128<byte> k) { throw new PlatformNotSupportedException(); }
    }
}
