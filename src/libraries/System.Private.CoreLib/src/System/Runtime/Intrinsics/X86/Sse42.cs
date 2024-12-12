// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 SSE4.2 hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Sse42 : Sse41
    {
        internal Sse42() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the x86 SSE4.2 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : Sse41.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>unsigned __int64 _mm_crc32_u64 (unsigned __int64 crc, unsigned __int64 v)</para>
            ///   <para>  CRC32 r64, r/m64</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong Crc32(ulong crc, ulong data) => Crc32(crc, data);
        }

        /// <summary>
        ///   <para>__m128i _mm_cmpgt_epi64 (__m128i a, __m128i b)</para>
        ///   <para>   PCMPGTQ xmm1,       xmm2/m128</para>
        ///   <para>  VPCMPGTQ xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<long> CompareGreaterThan(Vector128<long> left, Vector128<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>unsigned int _mm_crc32_u8 (unsigned int crc, unsigned char v)</para>
        ///   <para>  CRC32 r32, r/m8</para>
        /// </summary>
        public static uint Crc32(uint crc, byte data) => Crc32(crc, data);
        /// <summary>
        ///   <para>unsigned int _mm_crc32_u16 (unsigned int crc, unsigned short v)</para>
        ///   <para>  CRC32 r32, r/m16</para>
        /// </summary>
        public static uint Crc32(uint crc, ushort data) => Crc32(crc, data);
        /// <summary>
        ///   <para>unsigned int _mm_crc32_u32 (unsigned int crc, unsigned int v)</para>
        ///   <para>  CRC32 r32, r/m32</para>
        /// </summary>
        public static uint Crc32(uint crc, uint data) => Crc32(crc, data);
    }
}
