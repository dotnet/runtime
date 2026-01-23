// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using static TestLibrary.Expect;
using Xunit;

namespace TestMultipleCasts
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckMultipleCasts()
        {
            bool fail = false;

            // Cast int -> x
            ExpectEqual(() => CastIntSbyte(0x11223344), 0x44, ref fail);
            ExpectEqual(() => CastIntShort(-0x11223344), -0x3344, ref fail);
            ExpectEqual(() => CastIntLong(0x11223344), 0x11223344, ref fail);

            // Cast long -> x
            ExpectEqual(() => CastLongSbyte(0xFFEEDDCCBBAAL), -0x56, ref fail);
            ExpectEqual(() => CastLongShort(0xFFEEDDCCBBAAL), -0x4456, ref fail);
            ExpectEqual(() => CastLongInt(0xFFEEDDCCBBAAL), -0x22334456, ref fail);

            // Cast uint -> x
            ExpectEqual<byte>(() => CastUIntByte(0x11223344u), 0x44, ref fail);
            ExpectEqual<ushort>(() => CastUIntUShort(0x11223344u), 0x3344, ref fail);
            ExpectEqual(() => CastUIntULong(0x11223344u), 0x11223344ul, ref fail);

            // Cast ulong -> x
            ExpectEqual<byte>(() => CastULongByte(0xFFEEDDCCBBAAul), 0xAA, ref fail);
            ExpectEqual<ushort>(() => CastULongUShort(0xFFEEDDCCBBAAul), 0xBBAA, ref fail);
            ExpectEqual(() => CastULongUInt(0xFFEEDDCCBBAAul), 0xDDCCBBAAu, ref fail);

            // Cast int -> x -> int
            ExpectEqual(() => CastIntSbyteInt(0xF0), -0x10, ref fail);
            ExpectEqual(() => CastIntShortInt(0xFF8001), -0x7FFF, ref fail);
            ExpectEqual(() => CastIntLongInt(0x11223344), 0x11223344, ref fail);

            // Cast int -> x -> long
            ExpectEqual(() => CastIntSbyteLong(0x12345678), 0x78, ref fail);
            ExpectEqual(() => CastIntShortLong(0x12345678), 0x5678, ref fail);

            // Cast long -> x -> int
            ExpectEqual(() => CastLongSbyteInt(0xA7L), -0x59, ref fail);
            ExpectEqual(() => CastLongShortInt(0xFFFFFFFF8003L), -0x7FFD, ref fail);

            // Cast long -> x -> long
            ExpectEqual(() => CastLongSbyteLong(0xFEL), -0x2L, ref fail);
            ExpectEqual(() => CastLongShortLong(0xDEADL), -0x2153L, ref fail);
            ExpectEqual(() => CastLongIntLong(0x1ABCDEF00L), -0x54321100L, ref fail);

            // Cast uint -> x -> uint
            ExpectEqual(() => CastUIntByteUInt(0xF0u), 0xF0u, ref fail);
            ExpectEqual(() => CastUIntUShortUInt(0xFF8001u), 0x8001u, ref fail);

            // Cast uint -> x -> ulong
            ExpectEqual(() => CastUIntByteULong(0x12345678u), 0x78ul, ref fail);
            ExpectEqual(() => CastUIntUShortULong(0x12345678u), 0x5678ul, ref fail);
            ExpectEqual(() => CastUIntULongUInt(0x11223344u), 0x11223344u, ref fail);

            // Cast ulong -> x -> uint
            ExpectEqual(() => CastULongByteUInt(0xA7ul), 0xA7u, ref fail);
            ExpectEqual(() => CastULongUShortUInt(0xFFFFFFFF8003ul), 0x8003u, ref fail);

            // Cast ulong -> x -> ulong
            ExpectEqual(() => CastULongByteULong(0xFEul), 0xFEul, ref fail);
            ExpectEqual(() => CastULongUShortULong(0xDEADul), 0xDEADul, ref fail);
            ExpectEqual(() => CastULongUIntULong(0x1ABCDEF00ul), 0xABCDEF00ul, ref fail);

            // Cast int -> long -> x
            ExpectEqual<sbyte>(() => CastIntLongSbyte(0x11223344), 0x44, ref fail);
            ExpectEqual<short>(() => CastIntLongShort(0x11223344), 0x3344, ref fail);

            // Cast uint -> ulong -> x
            ExpectEqual<byte>(() => CastUIntULongByte(0x11223344u), 0x44, ref fail);
            ExpectEqual<ushort>(() => CastUIntULongUShort(0x11223344u), 0x3344, ref fail);

            // Cast long -> int -> short -> sbyte
            ExpectEqual<sbyte>(() => CastLongIntShortSByte(0x11223344), 0x44, ref fail);

            // Cast ulong -> uint -> ushort -> byte
            ExpectEqual<byte>(() => CastULongUIntUShortByte(0x11223344u), 0x44, ref fail);

            // Cast sbyte -> short -> int -> long
            ExpectEqual(() => CastSByteShortIntLong(-0x59), -0x59L, ref fail);

            // Cast byte -> ushort -> uint -> ulong
            ExpectEqual(() => CastByteUShortUIntULong(0xA7), 0xA7ul, ref fail);

            // Cast int -> long -> int -> long
            ExpectEqual<sbyte>(() => CastIntSByteIntSByte(-0x15263748), -0x48, ref fail);
            ExpectEqual<short>(() => CastIntShortIntShort(-0x15263748), -0x3748, ref fail);
            ExpectEqual(() => CastIntLongIntLong(-0x15263748), -0x15263748l, ref fail);

            // Cast uint -> x -> uint -> x
            ExpectEqual<byte>(() => CastUIntByteUIntByte(0xF0u), 0xF0, ref fail);
            ExpectEqual<ushort>(() => CastUIntUShortUIntUShort(0xFF8001u), 0x8001, ref fail);
            ExpectEqual(() => CastUIntULongUIntULong(0x11223344u), 0x11223344ul, ref fail);

            // Cast long -> x -> long -> x
            ExpectEqual<sbyte>(() => CastLongSByteLongSByte(0xA7L), -0x59, ref fail);
            ExpectEqual<short>(() => CastLongShortLongShort(0xFFFFFFFF8003L), -0x7FFD, ref fail);
            ExpectEqual(() => CastLongIntLongInt(0x1ABCDEF00L), -0x54321100, ref fail);

            // Cast ulong -> x -> ulong -> x
            ExpectEqual<byte>(() => CastULongByteULongByte(0xA7ul), 0xA7, ref fail);
            ExpectEqual<ushort>(() => CastULongUShortULongUShort(0xFFFFFFFF8003ul), 0x8003, ref fail);
            ExpectEqual(() => CastULongUIntULongUInt(0x1ABCDEF00ul), 0xABCDEF00u, ref fail);

            if (fail)
            {
                return 101;
            }
            return 100;
        }

        // Cast int -> x

        [MethodImpl(MethodImplOptions.NoInlining)]
        static sbyte CastIntSbyte(int x)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (sbyte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static short CastIntShort(int x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            return (short)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastIntLong(int x)
        {
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return (long)x;
        }

        // Cast long -> x

        [MethodImpl(MethodImplOptions.NoInlining)]
        static sbyte CastLongSbyte(long x)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (sbyte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static short CastLongShort(long x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            return (short)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastLongInt(long x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            return (int)x;
        }

        // Cast uint -> x

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte CastUIntByte(uint x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (byte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ushort CastUIntUShort(uint x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            return (ushort)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong CastUIntULong(uint x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            return (ulong)x;
        }

        // Cast ulong -> x

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte CastULongByte(ulong x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (byte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ushort CastULongUShort(ulong x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            return (ushort)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint CastULongUInt(ulong x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            return (uint)x;
        }

        // Cast int -> x -> int

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastIntSbyteInt(int x)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (sbyte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastIntShortInt(int x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            return (short)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastIntLongInt(int x)
        {
            //ARM64-NOT: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return (int)(long)x;
        }

        // Cast int -> x -> long

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastIntSbyteLong(int x)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return (sbyte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastIntShortLong(int x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return (short)x;
        }

        // Cast long -> x -> int

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastLongSbyteInt(long x)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (sbyte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastLongShortInt(long x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            return (short)x;
        }

        // Cast long -> x -> long

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastLongSbyteLong(long x)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return (sbyte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastLongShortLong(long x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return (short)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastLongIntLong(long x)
        {
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return (int)x;
        }

        // Cast uint -> x -> uint

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint CastUIntByteUInt(uint x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (byte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint CastUIntUShortUInt(uint x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            return (ushort)x;
        }

        // Cast uint -> x -> ulong

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong CastUIntByteULong(uint x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            return (byte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong CastUIntUShortULong(uint x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            return (ushort)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint CastUIntULongUInt(uint x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            return (uint)(ulong)x;
        }

        // Cast ulong -> x -> uint

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint CastULongByteUInt(ulong x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (byte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint CastULongUShortUInt(ulong x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            return (ushort)x;
        }

        // Cast ulong -> x -> ulong

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong CastULongByteULong(ulong x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            return (byte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong CastULongUShortULong(ulong x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            return (ushort)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong CastULongUIntULong(ulong x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            return (uint)x;
        }

        // Cast int -> long -> x

        [MethodImpl(MethodImplOptions.NoInlining)]
        static sbyte CastIntLongSbyte(int x)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (sbyte)(long)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static short CastIntLongShort(int x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            return (short)(long)x;
        }

        // Cast uint -> ulong -> x

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte CastUIntULongByte(uint x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (byte)(ulong)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ushort CastUIntULongUShort(uint x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            return (ushort)(ulong)x;
        }

        // Cast long -> int -> short -> sbyte

        [MethodImpl(MethodImplOptions.NoInlining)]
        static sbyte CastLongIntShortSByte(long x)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (sbyte)(short)(int)x;
        }

        // Cast ulong -> uint -> ushort -> byte

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte CastULongUIntUShortByte(ulong x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (byte)(ushort)(uint)x;
        }

        // Cast sbyte -> short -> int -> long

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastSByteShortIntLong(sbyte x)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return (long)(int)(short)x;
        }

        // Cast byte -> ushort -> uint -> ulong

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong CastByteUShortUIntULong(byte x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            return (ulong)(uint)(ushort)x;
        }

        // Cast int -> x -> int -> x

        [MethodImpl(MethodImplOptions.NoInlining)]
        static sbyte CastIntSByteIntSByte(int x)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (sbyte)(int)(sbyte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static short CastIntShortIntShort(int x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            return (short)(int)(short)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastIntLongIntLong(int x)
        {
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return (long)(int)(long)x;
        }

        // Cast uint -> x -> uint -> x

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte CastUIntByteUIntByte(uint x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (byte)(uint)(byte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ushort CastUIntUShortUIntUShort(uint x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            return (ushort)(uint)(ushort)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong CastUIntULongUIntULong(uint x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            return (ulong)(uint)(ulong)x;
        }

        // Cast long -> x -> long -> x

        [MethodImpl(MethodImplOptions.NoInlining)]
        static sbyte CastLongSByteLongSByte(long x)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (sbyte)(long)(sbyte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static short CastLongShortLongShort(long x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            return (short)(long)(short)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastLongIntLongInt(long x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            return (int)(long)(int)x;
        }

        // Cast ulong -> x -> ulong -> x

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte CastULongByteULongByte(ulong x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (byte)(ulong)(byte)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ushort CastULongUShortULongUShort(ulong x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            return (ushort)(ulong)(ushort)x;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint CastULongUIntULongUInt(ulong x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            return (uint)(ulong)(uint)x;
        }
    }
}
