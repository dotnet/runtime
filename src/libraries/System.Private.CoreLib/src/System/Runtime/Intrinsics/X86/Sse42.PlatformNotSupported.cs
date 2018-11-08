// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

        public new static bool IsSupported { get { return false; } }

        public new abstract class X64 : Sse41.X64
        {
            internal X64() { }

            public new static bool IsSupported { get { return false; } }

            /// <summary>
            /// unsigned __int64 _mm_crc32_u64 (unsigned __int64 crc, unsigned __int64 v)
            ///   CRC32 reg, reg/m64
            /// This intrinisc is only available on 64-bit processes
            /// </summary>
            public static ulong Crc32(ulong crc, ulong data) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        /// int _mm_cmpistra (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistrc (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistro (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistrs (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistrz (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// </summary>
        public static bool CompareImplicitLength(Vector128<sbyte> left, Vector128<sbyte> right, ResultsFlag flag, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cmpistra (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistrc (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistro (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistrs (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistrz (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// </summary>
        public static bool CompareImplicitLength(Vector128<byte> left, Vector128<byte> right, ResultsFlag flag, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cmpistra (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistrc (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistro (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistrs (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistrz (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// </summary>
        public static bool CompareImplicitLength(Vector128<short> left, Vector128<short> right, ResultsFlag flag, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }


        /// <summary>
        /// int _mm_cmpistra (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistrc (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistro (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistrs (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// int _mm_cmpistrz (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// </summary>
        public static bool CompareImplicitLength(Vector128<ushort> left, Vector128<ushort> right, ResultsFlag flag, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cmpestra (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestrc (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestro (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestrs (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestrz (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// </summary>
        public static bool CompareExplicitLength(Vector128<sbyte> left, byte leftLength, Vector128<sbyte> right, byte rightLength, ResultsFlag flag, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cmpestra (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestrc (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestro (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestrs (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestrz (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// </summary>
        public static bool CompareExplicitLength(Vector128<byte> left, byte leftLength, Vector128<byte> right, byte rightLength, ResultsFlag flag, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cmpestra (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestrc (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestro (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestrs (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestrz (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// </summary>
        public static bool CompareExplicitLength(Vector128<short> left, byte leftLength, Vector128<short> right, byte rightLength, ResultsFlag flag, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cmpestra (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestrc (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestro (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestrs (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// int _mm_cmpestrz (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// </summary>
        public static bool CompareExplicitLength(Vector128<ushort> left, byte leftLength, Vector128<ushort> right, byte rightLength, ResultsFlag flag, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cmpistri (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// </summary>
        public static int CompareImplicitLengthIndex(Vector128<sbyte> left, Vector128<sbyte> right, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cmpistri (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// </summary>
        public static int CompareImplicitLengthIndex(Vector128<byte> left, Vector128<byte> right, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cmpistri (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// </summary>
        public static int CompareImplicitLengthIndex(Vector128<short> left, Vector128<short> right, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cmpistri (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRI xmm, xmm/m128, imm8
        /// </summary>
        public static int CompareImplicitLengthIndex(Vector128<ushort> left, Vector128<ushort> right, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cmpestri (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// </summary>
        public static int CompareExplicitLengthIndex(Vector128<sbyte> left, byte leftLength, Vector128<sbyte> right, byte rightLength, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cmpestri (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// </summary>
        public static int CompareExplicitLengthIndex(Vector128<byte> left, byte leftLength, Vector128<byte> right, byte rightLength, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cmpestri (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// </summary>
        public static int CompareExplicitLengthIndex(Vector128<short> left, byte leftLength, Vector128<short> right, byte rightLength, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cmpestri (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRI xmm, xmm/m128, imm8
        /// </summary>
        public static int CompareExplicitLengthIndex(Vector128<ushort> left, byte leftLength, Vector128<ushort> right, byte rightLength, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpistrm (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<ushort> CompareImplicitLengthBitMask(Vector128<sbyte> left, Vector128<sbyte> right, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpistrm (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<ushort> CompareImplicitLengthBitMask(Vector128<byte> left, Vector128<byte> right, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpistrm (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<byte> CompareImplicitLengthBitMask(Vector128<short> left, Vector128<short> right, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpistrm (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<byte> CompareImplicitLengthBitMask(Vector128<ushort> left, Vector128<ushort> right, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpistrm (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<byte> CompareImplicitLengthUnitMask(Vector128<sbyte> left, Vector128<sbyte> right, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpistrm (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<byte> CompareImplicitLengthUnitMask(Vector128<byte> left, Vector128<byte> right, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpistrm (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<ushort> CompareImplicitLengthUnitMask(Vector128<short> left, Vector128<short> right, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpistrm (__m128i a, __m128i b, const int imm8)
        ///   PCMPISTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<ushort> CompareImplicitLengthUnitMask(Vector128<ushort> left, Vector128<ushort> right, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpestrm (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<ushort> CompareExplicitLengthBitMask(Vector128<sbyte> left, byte leftLength, Vector128<sbyte> right, byte rightLength, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpestrm (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<ushort> CompareExplicitLengthBitMask(Vector128<byte> left, byte leftLength, Vector128<byte> right, byte rightLength, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpestrm (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<byte> CompareExplicitLengthBitMask(Vector128<short> left, byte leftLength, Vector128<short> right, byte rightLength, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpestrm (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<byte> CompareExplicitLengthBitMask(Vector128<ushort> left, byte leftLength, Vector128<ushort> right, byte rightLength, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpestrm (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<byte> CompareExplicitLengthUnitMask(Vector128<sbyte> left, byte leftLength, Vector128<sbyte> right, byte rightLength, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpestrm (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<byte> CompareExplicitLengthUnitMask(Vector128<byte> left, byte leftLength, Vector128<byte> right, byte rightLength, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpestrm (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<ushort> CompareExplicitLengthUnitMask(Vector128<short> left, byte leftLength, Vector128<short> right, byte rightLength, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpestrm (__m128i a, int la, __m128i b, int lb, const int imm8)
        ///   PCMPESTRM xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<ushort> CompareExplicitLengthUnitMask(Vector128<ushort> left, byte leftLength, Vector128<ushort> right, byte rightLength, StringComparisonMode mode) { throw new PlatformNotSupportedException(); }

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
        /// <summary>
        /// unsigned __int64 _mm_crc32_u64 (unsigned __int64 crc, unsigned __int64 v)
        ///   CRC32 reg, reg/m64
        /// </summary>
        public static ulong Crc32(ulong crc, ulong data) { throw new PlatformNotSupportedException(); }
    }
}
