// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel SSE4.2 hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public abstract class Sse42 : Sse41
    {
        internal Sse42() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        public new abstract class X64 : Sse41.X64
        {
            internal X64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            /// unsigned __int64 _mm_crc32_u64 (unsigned __int64 crc, unsigned __int64 v)
            ///   CRC32 reg, reg/m64
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong Crc32(ulong crc, ulong data) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        /// __m128i _mm_cmpgt_epi64 (__m128i a, __m128i b)
        ///   PCMPGTQ xmm, xmm/m128
        /// </summary>
        public static Vector128<long> CompareGreaterThan(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// unsigned int _mm_crc32_u8 (unsigned int crc, unsigned char v)
        ///   CRC32 reg, reg/m8
        /// </summary>
        public static uint Crc32(uint crc, byte data) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// unsigned int _mm_crc32_u16 (unsigned int crc, unsigned short v)
        ///   CRC32 reg, reg/m16
        /// </summary>
        public static uint Crc32(uint crc, ushort data) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// unsigned int _mm_crc32_u32 (unsigned int crc, unsigned int v)
        ///   CRC32 reg, reg/m32
        /// </summary>
        public static uint Crc32(uint crc, uint data) { throw new PlatformNotSupportedException(); }
    }
}
