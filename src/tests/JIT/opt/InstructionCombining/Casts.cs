// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
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
            if (CastIntSbyte(0x11223344) != 0x44)
            {
                fail = true;
            }
            if (CastIntShort(-0x11223344) != -0x3344)
            {
                fail = true;
            }
            if (CastIntLong(0x11223344) != 0x11223344)
            {
                fail = true;
            }

            // Cast long -> x
            if (CastLongSbyte(0xFFEEDDCCBBAAL) != -0x56)
            {
                fail = true;
            }
            if (CastLongShort(0xFFEEDDCCBBAAL) != -0x4456)
            {
                fail = true;
            }
            if (CastLongInt(0xFFEEDDCCBBAAL) != -0x22334456)
            {
                fail = true;
            }

            // Cast uint -> x
            if (CastUIntByte(0x11223344u) != 0x44)
            {
                fail = true;
            }
            if (CastUIntUShort(0x11223344u) != 0x3344)
            {
                fail = true;
            }
            if (CastUIntULong(0x11223344u) != 0x11223344ul)
            {
                fail = true;
            }

            // Cast ulong -> x
            if (CastULongByte(0xFFEEDDCCBBAAul) != 0xAA)
            {
                fail = true;
            }
            if (CastULongUShort(0xFFEEDDCCBBAAul) != 0xBBAA)
            {
                fail = true;
            }
            if (CastULongUInt(0xFFEEDDCCBBAAul) != 0xDDCCBBAAu)
            {
                fail = true;
            }

            // Cast int -> x -> int
            if (CastIntSbyteInt(0xF0) != -0x10)
            {
                fail = true;
            }
            if (CastIntShortInt(0xFF8001) != -0x7FFF)
            {
                fail = true;
            }
            if (CastIntLongInt(0x11223344) != 0x11223344)
            {
                fail = true;
            }

            // Cast int -> x -> long
            if (CastIntSbyteLong(0x12345678) != 0x78)
            {
                fail = true;
            }
            if (CastIntShortLong(0x12345678) != 0x5678)
            {
                fail = true;
            }

            // Cast long -> x -> int
            if (CastLongSbyteInt(0xA7L) != -0x59)
            {
                fail = true;
            }
            if (CastLongShortInt(0xFFFFFFFF8003L) != -0x7FFD)
            {
                fail = true;
            }

            // Cast long -> x -> long
            if (CastLongSbyteLong(0xFEL) != -0x2L)
            {
                fail = true;
            }
            if (CastLongShortLong(0xDEADL) != -0x2153L)
            {
                fail = true;
            }
            if (CastLongIntLong(0x1ABCDEF00L) != -0x54321100L)
            {
                fail = true;
            }

            // Cast uint -> x -> uint
            if (CastUIntByteUInt(0xF0u) != 0xF0u)
            {
                fail = true;
            }
            if (CastUIntUShortUInt(0xFF8001u) != 0x8001u)
            {
                fail = true;
            }

            // Cast uint -> x -> ulong
            if (CastUIntByteULong(0x12345678u) != 0x78ul)
            {
                fail = true;
            }
            if (CastUIntUShortULong(0x12345678u) != 0x5678ul)
            {
                fail = true;
            }
            if (CastUIntULongUInt(0x11223344u) != 0x11223344u)
            {
                fail = true;
            }

            // Cast ulong -> x -> uint
            if (CastULongByteUInt(0xA7ul) != 0xA7u)
            {
                fail = true;
            }
            if (CastULongUShortUInt(0xFFFFFFFF8003ul) != 0x8003u)
            {
                fail = true;
            }

            // Cast ulong -> x -> ulong
            if (CastULongByteULong(0xFEul) != 0xFEul)
            {
                fail = true;
            }
            if (CastULongUShortULong(0xDEADul) != 0xDEADul)
            {
                fail = true;
            }
            if (CastULongUIntULong(0x1ABCDEF00ul) != 0xABCDEF00ul)
            {
                fail = true;
            }

            // Cast int -> long -> x
            if (CastIntLongSbyte(0x11223344) != 0x44)
            {
                fail = true;
            }
            if (CastIntLongShort(0x11223344) != 0x3344)
            {
                fail = true;
            }

            // Cast uint -> ulong -> x
            if (CastUIntULongByte(0x11223344u) != 0x44)
            {
                fail = true;
            }
            if (CastUIntULongUShort(0x11223344u) != 0x3344)
            {
                fail = true;
            }

            // Cast long -> int -> short -> sbyte
            if (CastLongIntShortSByte(0x11223344) != 0x44)
            {
                fail = true;
            }

            // Cast ulong -> uint -> ushort -> byte
            if (CastULongUIntUShortByte(0x11223344u) != 0x44)
            {
                fail = true;
            }

            // Cast sbyte -> short -> int -> long
            if (CastSByteShortIntLong(-0x59) != -0x59L)
            {
                fail = true;
            }

            // Cast byte -> ushort -> uint -> ulong
            if (CastByteUShortUIntULong(0xA7) != 0xA7ul)
            {
                fail = true;
            }

            // Cast int -> long -> int -> long
            if (CastIntSByteIntSByte(-0x15263748) != -0x48)
            {
                fail = true;
            }
            if (CastIntShortIntShort(-0x15263748) != -0x3748)
            {
                fail = true;
            }
            if (CastIntLongIntLong(-0x15263748) != -0x15263748l)
            {
                fail = true;
            }

            // Cast uint -> x -> uint -> x
            if (CastUIntByteUIntByte(0xF0u) != 0xF0)
            {
                fail = true;
            }
            if (CastUIntUShortUIntUShort(0xFF8001u) != 0x8001)
            {
                fail = true;
            }
            if (CastUIntULongUIntULong(0x11223344u) != 0x11223344ul)
            {
                fail = true;
            }

            // Cast long -> x -> long -> x
            if (CastLongSByteLongSByte(0xA7L) != -0x59)
            {
                fail = true;
            }
            if (CastLongShortLongShort(0xFFFFFFFF8003L) != -0x7FFD)
            {
                fail = true;
            }
            if (CastLongIntLongInt(0x1ABCDEF00L) != -0x54321100)
            {
                fail = true;
            }

            // Cast ulong -> x -> ulong -> x
            if (CastULongByteULongByte(0xA7ul) != 0xA7)
            {
                fail = true;
            }
            if (CastULongUShortULongUShort(0xFFFFFFFF8003ul) != 0x8003)
            {
                fail = true;
            }
            if (CastULongUIntULongUInt(0x1ABCDEF00ul) != 0xABCDEF00u)
            {
                fail = true;
            }

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
