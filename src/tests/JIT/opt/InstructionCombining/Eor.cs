// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestEor
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckEor()
        {
            bool fail = false;

            if (Eor(5, 3) != 6)
            {
                fail = true;
            }

            if (EorLSL(32, 3) != 16)
            {
                fail = true;
            }

            if (EorLSLSwap(32, 3) != 16)
            {
                fail = true;
            }

            if (EorLSR(0xBA, 0xABCDE) != 0x11)
            {
                fail = true;
            }

            if (EorASR(0x8282, 0x1DA00000) != 0x82B9)
            {
                fail = true;
            }

            if (EorLargeShift(0xDCBA, 0x1F) != 0xc3ba)
            {
                fail = true;
            }

            if (EorLargeShift64Bit(0x2468, 0x26ae123456789ABC) != 0x373F)
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
        static int Eor(int a, int b)
        {
            //ARM64-FULL-LINE: eor {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return a ^ b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int EorLSL(int a, int b)
        {
            //ARM64-FULL-LINE: eor {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return a ^ (b<<4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int EorLSLSwap(int a, int b)
        {
            //ARM64-FULL-LINE: eor {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return (b<<4) ^ a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint EorLSR(uint a, uint b)
        {
            //ARM64-FULL-LINE: eor {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSR #12
            return a ^ (b>>12);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int EorASR(int a, int b)
        {
            //ARM64-FULL-LINE: eor {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ASR #23
            return a ^ (b>>23);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int EorLargeShift(int a, int b)
        {
            //ARM64-FULL-LINE: eor {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #8
            return a ^ (b<<136);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong EorLargeShift64Bit(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: eor {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSR #49
            return a ^ (b>>177);
        }
    }
}
