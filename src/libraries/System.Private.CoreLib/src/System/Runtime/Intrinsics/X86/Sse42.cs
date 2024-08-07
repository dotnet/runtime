// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>This class provides access to X86 SSE4.2 hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Sse42 : Sse41
    {
        internal Sse42() { }

        /// <summary>Gets <c>true</c> if the APIs in this class are supported; otherwise, <c>false</c> which indicates they will throw <see cref="PlatformNotSupportedException" />.</summary>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>This class provides access to the x86 SSE4.2 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : Sse41.X64
        {
            internal X64() { }

            /// <summary>Gets <c>true</c> if the APIs in this class are supported; otherwise, <c>false</c> which indicates they will throw <see cref="PlatformNotSupportedException" />.</summary>
            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            /// unsigned __int64 _mm_crc32_u64 (unsigned __int64 crc, unsigned __int64 v)
            ///   CRC32 r64, r/m64
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong Crc32(ulong crc, ulong data) => Crc32(crc, data);
        }

        /// <summary>
        /// __m128i _mm_cmpgt_epi64 (__m128i a, __m128i b)
        ///    PCMPGTQ xmm1,       xmm2/m128
        ///   VPCMPGTQ xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<long> CompareGreaterThan(Vector128<long> left, Vector128<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// unsigned int _mm_crc32_u8 (unsigned int crc, unsigned char v)
        ///   CRC32 r32, r/m8
        /// </summary>
        public static uint Crc32(uint crc, byte data) => Crc32(crc, data);
        /// <summary>
        /// unsigned int _mm_crc32_u16 (unsigned int crc, unsigned short v)
        ///   CRC32 r32, r/m16
        /// </summary>
        public static uint Crc32(uint crc, ushort data) => Crc32(crc, data);
        /// <summary>
        /// unsigned int _mm_crc32_u32 (unsigned int crc, unsigned int v)
        ///   CRC32 r32, r/m32
        /// </summary>
        public static uint Crc32(uint crc, uint data) => Crc32(crc, data);
    }
}
