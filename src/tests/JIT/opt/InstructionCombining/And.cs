// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestAnd
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckAnd()
        {
            bool fail = false;

            if (And(3, 2) != 2)
            {
                fail = true;
            }

            if (AndLSL(255, 1) != 16)
            {
                fail = true;
            }

            if (AndLSLSwap(255, 1) != 16)
            {
                fail = true;
            }

            if (AndLSR(255, 0x10000000) != 8)
            {
                fail = true;
            }

            if (AndASR(-5, 0x3C00) != 0xB)
            {
                fail = true;
            }

            if (AndLargeShift(9, 1) != 8)
            {
                fail = true;
            }

            if (AndLargeShift64Bit(0xF000000000, 7) != 0xE000000000)
            {
                fail = true;
            }

            if (Ands(4, 4) != 1)
            {
                fail = true;
            }

            if (AndsLSL(8, 2) != 1)
            {
                fail = true;
            }

            if (AndsLSLSwap(8, 2) != 1)
            {
                fail = true;
            }

            if (AndsLSR(3, 0xa0000) != 1)
            {
                fail = true;
            }

            if (AndsASR(6, 0x6000) != 1)
            {
                fail = true;
            }

            if (AndsLargeShift(0x80000000, 1) != 1)
            {
                fail = true;
            }

            if (AndsLargeShift64Bit(0xD, 0x34000000000) != 1)
            {
                fail = true;
            }

            if (!AndsSingleLine(6, 10))
            {
                fail = true;
            }

            if (!AndsSingleLineLSR(0xB00, 8))
            {
                fail = true;
            }

            if (AndsBinOp(4, 1, 0x3000, 4) != 1)
            {
                fail = true;
            }

            if (!AndsBinOpSingleLine(1, 2, 4, 12))
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
        static int And(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return a & b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AndLSL(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return a & (b<<4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AndLSLSwap(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return (b<<4) & a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint AndLSR(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSR #25
            return a & (b>>25);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AndASR(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ASR #10
            return a & (b>>10);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AndLargeShift(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #3
            return a & (b<<67);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long AndLargeShift64Bit(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #37
            return a & (b<<101);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Ands(int a, int b)
        {
            //ARM64-FULL-LINE: tst {{w[0-9]+}}, {{w[0-9]+}}
            if ((a & b) != 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AndsLSL(int a, int b)
        {
            //ARM64-FULL-LINE: tst {{w[0-9]+}}, {{w[0-9]+}}, LSL #2
            if ((a & (b<<2)) != 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AndsLSLSwap(int a, int b)
        {
            //ARM64-FULL-LINE: tst {{w[0-9]+}}, {{w[0-9]+}}, LSL #2
            if (((b<<2) & a) != 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AndsLSR(uint a, uint b)
        {
            //ARM64-FULL-LINE: tst {{w[0-9]+}}, {{w[0-9]+}}, LSR #17
            if ((a & (b>>17)) != 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AndsASR(int a, int b)
        {
            //ARM64-FULL-LINE: tst {{w[0-9]+}}, {{w[0-9]+}}, ASR #12
            if ((a & (b>>12)) != 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AndsLargeShift(uint a, uint b)
        {
            //ARM64-FULL-LINE: tst {{w[0-9]+}}, {{w[0-9]+}}, LSL #31
            if ((a & (b<<255)) != 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AndsLargeShift64Bit(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: lsr {{x[0-9]+}}, {{x[0-9]+}}, #38
            //ARM64-FULL-LINE: tst {{x[0-9]+}}, {{x[0-9]+}}
            if ((a & (b>>230)) != 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool AndsSingleLine(uint a, uint b)
        {
            //ARM64-FULL-LINE: tst {{w[0-9]+}}, {{w[0-9]+}}
            return (a & b) != 0;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool AndsSingleLineLSR(uint a, uint b)
        {
            //ARM64-FULL-LINE: tst {{w[0-9]+}}, {{w[0-9]+}}, LSR #8
            return ((a>>8) & b) != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AndsBinOp(uint a, uint b, uint c, uint d)
        {
            //ARM64-FULL-LINE: tst {{w[0-9]+}}, {{w[0-9]+}}, LSL #2
            //ARM64-FULL-LINE: tst {{w[0-9]+}}, {{w[0-9]+}}, LSR #10
            if (((a & (b<<2)) == 0) == (((c>>10) & d) == 0)) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool AndsBinOpSingleLine(uint a, uint b, uint c, uint d)
        {
            //ARM64-FULL-LINE: tst {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: tst {{w[0-9]+}}, {{w[0-9]+}}
            return ((a & b) == 0) | ((c & d) == 0);
        }
    }
}
