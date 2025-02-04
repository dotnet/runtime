// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestSub
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckSub()
        {
            bool fail = false;

            if (Sub(5, 2) != 3)
            {
                fail = true;
            }

            if (SubLSL(100, 2) != 84)
            {
                fail = true;
            }

            if (SubLSR(10, 64) != 6)
            {
                fail = true;
            }

            if (SubASR(10, 320) != 5)
            {
                fail = true;
            }

            if (SubLargeShift(0x40000F, 1) != 0xF)
            {
                fail = true;
            }

            if (SubLargeShift64Bit(0x40000000000ACE, 1) != 0xACE)
            {
                fail = true;
            }

            if (Subs(15, 15) != 1)
            {
                fail = true;
            }

            if (SubsLSL(14, 7) != 1)
            {
                fail = true;
            }
            
            if (SubsLSR(1, 0x80000000) != 1)
            {
                fail = true;
            }

            if (SubsASR(27, 0x1B00000) != 1)
            {
                fail = true;
            }

            if (SubsLargeShift(4, 2) != 1)
            {
                fail = true;
            }

            if (SubsLargeShift64Bit(0x300000000, 3) != 1)
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
        static int Sub(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return a - b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SubLSL(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #3
            return a - (b<<3);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint SubLSR(uint a, uint b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSR #4
            return a - (b>>4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SubASR(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ASR #6
            return a - (b>>6);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SubLargeShift(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #22
            return a - (b<<118);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long SubLargeShift64Bit(long a, long b)
        {
            //ARM64-FULL-LINE: sub {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #54
            return a - (b<<118);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Subs(int a, int b)
        {
            //ARM64-FULL-LINE: subs {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            if (a - b == 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SubsLSL(int a, int b)
        {
            //ARM64-FULL-LINE: subs {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #1
            if (a - (b<<1) == 0) {
                return 1;
            }
            return -1;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SubsLSR(uint a, uint b)
        {
            //ARM64-FULL-LINE: subs {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSR #31
            if (a - (b>>31) == 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SubsASR(int a, int b)
        {
            //ARM64-FULL-LINE: subs {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ASR #20
            if (a - (b>>20) == 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SubsLargeShift(int a, int b)
        {
            //ARM64-FULL-LINE: subs {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #1
            if (a - (b<<33) == 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SubsLargeShift64Bit(long a, long b)
        {
            //ARM64-FULL-LINE: subs {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #32
            if (a - (b<<96) == 0) {
                return 1;
            }
            return -1;
        }
    }
}
