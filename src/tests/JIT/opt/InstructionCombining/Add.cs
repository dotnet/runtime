// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestAdd
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckAdd()
        {
            bool fail = false;

            if (Add(1, 2) != 3)
            {
                fail = true;
            }

            if (AddLSL(5, 5) != 85)
            {
                fail = true;
            }

            if (AddLSLSwap(5, 5) != 85)
            {
                fail = true;
            }

            if (AddLSR(1, 0x20000000) != 2)
            {
                fail = true;
            }

            if (AddASR(-2, 0x4000) != -1)
            {
                fail = true;
            }

            if (AddLargeShift(0x100000, 1) != 0x900000)
            {
                fail = true;
            }

            if (AddLargeShift64Bit(0xAB, 0x19a0000000000) != 0x178)
            {
                fail = true;
            }

            if (Adds(-5, 5) != 1)
            {
                fail = true;
            }

            if (AddsLSL(-0x78000, 0xF) != 1)
            {
                fail = true;
            }

            if (AddsLSLSwap(-0x78000, 0xF) != 1)
            {
                fail = true;
            }

            if (AddsLSR(0, 0x3c0) != 1)
            {
                fail = true;
            }

            if (AddsASR(-1, 0x800) != 1)
            {
                fail = true;
            }

            if (AddsLargeShift(-0xFF, 0x1fe0) != 1)
            {
                fail = true;
            }

            if (AddsLargeShift64Bit(-0x40000000000, 1) != 1)
            {
                fail = true;
            }

            if (!AddsSingleLine(-5, 5))
            {
                fail = true;
            }

            if (!AddsSingleLineLSL(-0x70, 0x7))
            {
                fail = true;
            }

            if (AddsBinOp(-5, 5, 2, -2) != 1)
            {
                fail = true;
            }

            if (!AddsBinOpSingleLine(-5, 5, 4, -2))
            {
                fail = true;
            }

            if (AddExtendedB(0, 0x101) != 1)
            {
                fail = true;
            }

            if (AddExtendedH(0, 0x10001) != 1)
            {
                fail = true;
            }

            if (AddExtendedW(0, 0x100000001) != 1)
            {
                fail = true;
            }

            if (AddExtendedUB(0, 0x101) != 1)
            {
                fail = true;
            }

            if (AddExtendedUH(0, 0x10001) != 1)
            {
                fail = true;
            }

            if (AddExtendedUW(0, 0x100000001) != 1)
            {
                fail = true;
            }

            if (fail)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Add(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return a + b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AddLSL(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return a + (b<<4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AddLSLSwap(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return (b<<4) + a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint AddLSR(uint a, uint b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSR #29
            return a + (b>>29);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AddASR(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ASR #14
            return a + (b>>14);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AddLargeShift(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #23
            return a + (b<<183);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long AddLargeShift64Bit(long a, long b)
        {
            //ARM64-FULL-LINE: add {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, ASR #41
            return a + (b>>169);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long AddExtendedB(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, SXTB
            return a + (sbyte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long AddExtendedH(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, SXTH
            return a + (short)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long AddExtendedW(long a, long b)
        {
            //ARM64-FULL-LINE: add {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, SXTW
            return a + (int)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long AddExtendedUB(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, UXTB
            return a + (byte)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long AddExtendedUH(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, UXTH
            return a + (ushort)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long AddExtendedUW(long a, long b)
        {
            //ARM64-FULL-LINE: add {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, UXTW
            return a + (uint)b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Adds(int a, int b)
        {
            //ARM64-FULL-LINE: adds {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            if (a + b == 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AddsLSL(int a, int b)
        {
            //ARM64-FULL-LINE: adds {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #15
            if (a + (b<<15) == 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AddsLSLSwap(int a, int b)
        {
            //ARM64-FULL-LINE: adds {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #15
            if ((b<<15) + a == 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AddsLSR(uint a, uint b)
        {
            //ARM64-FULL-LINE: adds {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSR #6
            if (a + (b>>6) != 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AddsASR(int a, int b)
        {
            //ARM64-FULL-LINE: adds {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ASR #11
            if (a + (b>>11) == 0) {
                return 1;
            }
            return -1;
        }

        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AddsLargeShift(int a, int b)
        {
            //ARM64-FULL-LINE: adds {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ASR #5
            if (a + (b>>133) == 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long AddsLargeShift64Bit(long a, long b)
        {
            //ARM64-FULL-LINE: adds {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #42
            if (a + (b<<106) == 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool AddsSingleLine(int a, int b)
        {
            //ARM64-FULL-LINE: adds {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return a + b == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool AddsSingleLineLSL(int a, int b)
        {
            //ARM64-FULL-LINE: adds {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return a + (b<<4) == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AddsBinOp(int a, int b, int c, int d)
        {
            //ARM64-FULL-LINE: adds {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: adds {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            if ((a + b == 0) == (c + d == 0)) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool AddsBinOpSingleLine(int a, int b, int c, int d)
        {
            //ARM64-FULL-LINE: adds {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: adds {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return (a + b == 0) | (c + d == 0);
        }
    }
}
