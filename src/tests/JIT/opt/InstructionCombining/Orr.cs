// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestOrr
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckOrr()
        {
            bool fail = false;

            if (Orr(2, 4) != 6)
            {
                fail = true;
            }

            if (OrrLSL(0x180, 5) != 0x1C0)
            {
                fail = true;
            }

            if (OrrLSLSwap(0x180, 5) != 0x1C0)
            {
                fail = true;
            }

            if (OrrLSR(0x7, 0x1234) != 0x27)
            {
                fail = true;
            }

            if (OrrASR(0x13, 0xA98765) != 0x3B)
            {
                fail = true;
            }

            if (OrrLargeShift(0x1A40, 0x8E) != 0x1BC0)
            {
                fail = true;
            }

            if (OrrLargeShift64Bit(0x2468, 0xDEF1234567) != 0x246F)
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
        static int Orr(int a, int b)
        {
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return a | b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int OrrLSL(int a, int b)
        {
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #6
            return a | (b<<6);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int OrrLSLSwap(int a, int b)
        {
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #6
            return (b<<6) | a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint OrrLSR(uint a, uint b)
        {
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSR #7
            return a | (b>>7);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int OrrASR(int a, int b)
        {
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ASR #18
            return a | (b>>18);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int OrrLargeShift(int a, int b)
        {
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #5
            return a | (b<<101);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong OrrLargeShift64Bit(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSR #33
            return a | (b>>289);
        }
    }
}
