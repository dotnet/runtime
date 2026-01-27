// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace TestMultipleCasts
{
    public class Program
    {
        // Cast int -> x

        [Theory]
        [InlineData(0x11223344, 0x44)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastIntSbyte(int x, sbyte expected)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            sbyte result = (sbyte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(-0x11223344, -0x3344)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastIntShort(int x, short expected)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            short result = (short)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x11223344, 0x11223344)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastIntLong(int x, long expected)
        {
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            long result = (long)x;
            Assert.Equal(expected, result);
        }

        // Cast long -> x

        [Theory]
        [InlineData(0xFFEEDDCCBBAAL, -0x56)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastLongSbyte(long x, sbyte expected)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            sbyte result = (sbyte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0xFFEEDDCCBBAAL, -0x4456)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastLongShort(long x, short expected)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            short result = (short)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0xFFEEDDCCBBAAL, -0x22334456)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastLongInt(long x, int expected)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            int result = (int)x;
            Assert.Equal(expected, result);
        }

        // Cast uint -> x

        [Theory]
        [InlineData(0x11223344u, 0x44)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastUIntByte(uint x, byte expected)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            byte result = (byte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x11223344u, 0x3344)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastUIntUShort(uint x, ushort expected)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            ushort result = (ushort)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x11223344u, 0x11223344ul)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastUIntULong(uint x, ulong expected)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            ulong result = (ulong)x;
            Assert.Equal(expected, result);
        }

        // Cast ulong -> x

        [Theory]
        [InlineData(0xFFEEDDCCBBAAul, 0xAA)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastULongByte(ulong x, byte expected)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            byte result = (byte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0xFFEEDDCCBBAAul, 0xBBAA)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastULongUShort(ulong x, ushort expected)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            ushort result = (ushort)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0xFFEEDDCCBBAAul, 0xDDCCBBAAu)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastULongUInt(ulong x, uint expected)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            uint result = (uint)x;
            Assert.Equal(expected, result);
        }

        // Cast int -> x -> int

        [Theory]
        [InlineData(0xF0, -0x10)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastIntSbyteInt(int x, int expected)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            int result = (sbyte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0xFF8001, -0x7FFF)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastIntShortInt(int x, int expected)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            int result = (short)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x11223344, 0x11223344)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastIntLongInt(int x, int expected)
        {
            //ARM64-NOT: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            int result = (int)(long)x;
            Assert.Equal(expected, result);
        }

        // Cast int -> x -> long

        [Theory]
        [InlineData(0x12345678, 0x78)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastIntSbyteLong(int x, long expected)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            long result = (sbyte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x12345678, 0x5678)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastIntShortLong(int x, long expected)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            long result = (short)x;
            Assert.Equal(expected, result);
        }

        // Cast long -> x -> int

        [Theory]
        [InlineData(0xA7L, -0x59)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastLongSbyteInt(long x, int expected)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            int result = (sbyte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0xFFFFFFFF8003L, -0x7FFD)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastLongShortInt(long x, int expected)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            int result = (short)x;
            Assert.Equal(expected, result);
        }

        // Cast long -> x -> long

        [Theory]
        [InlineData(0xFEL, -0x2L)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastLongSbyteLong(long x, long expected)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            long result = (sbyte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0xDEADL, -0x2153L)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastLongShortLong(long x, long expected)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            long result = (short)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x1ABCDEF00L, -0x54321100L)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastLongIntLong(long x, long expected)
        {
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            long result = (int)x;
            Assert.Equal(expected, result);
        }

        // Cast uint -> x -> uint

        [Theory]
        [InlineData(0xF0u, 0xF0u)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastUIntByteUInt(uint x, uint expected)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            uint result = (byte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0xFF8001u, 0x8001u)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastUIntUShortUInt(uint x, uint expected)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            uint result = (ushort)x;
            Assert.Equal(expected, result);
        }

        // Cast uint -> x -> ulong

        [Theory]
        [InlineData(0x12345678u, 0x78ul)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastUIntByteULong(uint x, ulong expected)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            ulong result = (byte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x12345678u, 0x5678ul)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastUIntUShortULong(uint x, ulong expected)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            ulong result = (ushort)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x11223344u, 0x11223344u)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastUIntULongUInt(uint x, uint expected)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            uint result = (uint)(ulong)x;
            Assert.Equal(expected, result);
        }

        // Cast ulong -> x -> uint

        [Theory]
        [InlineData(0xA7ul, 0xA7u)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastULongByteUInt(ulong x, uint expected)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            uint result = (byte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0xFFFFFFFF8003ul, 0x8003u)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastULongUShortUInt(ulong x, uint expected)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            uint result = (ushort)x;
            Assert.Equal(expected, result);
        }

        // Cast ulong -> x -> ulong

        [Theory]
        [InlineData(0xFEul, 0xFEul)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastULongByteULong(ulong x, ulong expected)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            ulong result = (byte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0xDEADul, 0xDEADul)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastULongUShortULong(ulong x, ulong expected)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            ulong result = (ushort)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x1ABCDEF00ul, 0xABCDEF00ul)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastULongUIntULong(ulong x, ulong expected)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            ulong result = (uint)x;
            Assert.Equal(expected, result);
        }

        // Cast int -> long -> x

        [Theory]
        [InlineData(0x11223344, 0x44)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastIntLongSbyte(int x, sbyte expected)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            sbyte result = (sbyte)(long)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x11223344, 0x3344)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastIntLongShort(int x, short expected)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            short result = (short)(long)x;
            Assert.Equal(expected, result);
        }

        // Cast uint -> ulong -> x

        [Theory]
        [InlineData(0x11223344u, 0x44)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastUIntULongByte(uint x, byte expected)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            byte result = (byte)(ulong)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x11223344u, 0x3344)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastUIntULongUShort(uint x, ushort expected)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            ushort result = (ushort)(ulong)x;
            Assert.Equal(expected, result);
        }

        // Cast long -> int -> short -> sbyte

        [Theory]
        [InlineData(0x11223344L, 0x44)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastLongIntShortSByte(long x, sbyte expected)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            sbyte result = (sbyte)(short)(int)x;
            Assert.Equal(expected, result);
        }

        // Cast ulong -> uint -> ushort -> byte

        [Theory]
        [InlineData(0x11223344ul, 0x44)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastULongUIntUShortByte(ulong x, byte expected)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            byte result = (byte)(ushort)(uint)x;
            Assert.Equal(expected, result);
        }

        // Cast sbyte -> short -> int -> long

        [Theory]
        [InlineData(-0x59, -0x59L)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastSByteShortIntLong(sbyte x, long expected)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            long result = (long)(int)(short)x;
            Assert.Equal(expected, result);
        }

        // Cast byte -> ushort -> uint -> ulong

        [Theory]
        [InlineData(0xA7, 0xA7ul)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastByteUShortUIntULong(byte x, ulong expected)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            ulong result = (ulong)(uint)(ushort)x;
            Assert.Equal(expected, result);
        }

        // Cast int -> x -> int -> x

        [Theory]
        [InlineData(-0x15263748, -0x48)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastIntSByteIntSByte(int x, sbyte expected)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            sbyte result = (sbyte)(int)(sbyte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(-0x15263748, -0x3748)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastIntShortIntShort(int x, short expected)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            short result = (short)(int)(short)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(-0x15263748, -0x15263748L)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastIntLongIntLong(int x, long expected)
        {
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            long result = (long)(int)(long)x;
            Assert.Equal(expected, result);
        }

        // Cast uint -> x -> uint -> x

        [Theory]
        [InlineData(0xF0u, 0xF0)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastUIntByteUIntByte(uint x, byte expected)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            byte result = (byte)(uint)(byte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0xFF8001u, 0x8001)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastUIntUShortUIntUShort(uint x, ushort expected)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            ushort result = (ushort)(uint)(ushort)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x11223344u, 0x11223344ul)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastUIntULongUIntULong(uint x, ulong expected)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            ulong result = (ulong)(uint)(ulong)x;
            Assert.Equal(expected, result);
        }

        // Cast long -> x -> long -> x

        [Theory]
        [InlineData(0xA7L, -0x59)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastLongSByteLongSByte(long x, sbyte expected)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            sbyte result = (sbyte)(long)(sbyte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0xFFFFFFFF8003L, -0x7FFD)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastLongShortLongShort(long x, short expected)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            short result = (short)(long)(short)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x1ABCDEF00L, -0x54321100)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastLongIntLongInt(long x, int expected)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            int result = (int)(long)(int)x;
            Assert.Equal(expected, result);
        }

        // Cast ulong -> x -> ulong -> x

        [Theory]
        [InlineData(0xA7ul, 0xA7)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastULongByteULongByte(ulong x, byte expected)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            byte result = (byte)(ulong)(byte)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0xFFFFFFFF8003ul, 0x8003)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastULongUShortULongUShort(ulong x, ushort expected)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            ushort result = (ushort)(ulong)(ushort)x;
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0x1ABCDEF00ul, 0xABCDEF00u)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CastULongUIntULongUInt(ulong x, uint expected)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            uint result = (uint)(ulong)(uint)x;
            Assert.Equal(expected, result);
        }
    }
}
