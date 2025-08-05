// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestEon
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckEon()
        {
            bool fail = false;

            if (Eon(5, 1) != 0xFFFFFFFB)
            {
                fail = true;
            }

            if (EonLSL(0x12345678, 0xA) != 0xEDC92987)
            {
                fail = true;
            }

            if (EonLSLSwapInt(0x12345678, 0xA) != -0x1236d679)
            {
                fail = true;
            }

            if (EonLSLSwapUint(0xFDFDFDFD, 0xB) != 0x200C202)
            {
                fail = true;
            }

            if (EonLSR(0x87654321, 0xFEDCBA) != 0x789D4A3B)
            {
                fail = true;
            }

            if (EonASR(0x2468, 0xFEDCBA) != -0x246C)
            {
                fail = true;
            }

            if (EonLargeShift(0x87654321, 0x12345678) != 0xB89ABCDE)
            {
                fail = true;
            }

            if (EonLargeShift64Bit(0x1357135713571357, 0x123456789ABCDEF0) != 0xECA8ECA8ECE03DF1)
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
        static uint Eon(uint a, uint b)
        {
            //ARM64-FULL-LINE: eon {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return a ^ ~b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint EonLSL(uint a, uint b)
        {
            //ARM64-FULL-LINE: eon {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #14
            return a ^ ~(b<<14);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int EonLSLSwapInt(int a, int b)
        {
            //ARM64-FULL-LINE: eon {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #14
            return ~(b<<14) ^ a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint EonLSLSwapUint(uint a, uint b)
        {
            //ARM64-FULL-LINE: eon {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #14
            return ~(b<<14) ^ a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint EonLSR(uint a, uint b)
        {
            //ARM64-FULL-LINE: eon {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSR #5
            return a ^ ~(b>>5);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int EonASR(int a, int b)
        {
            //ARM64-FULL-LINE: eon {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ASR #22
            return a ^ ~(b>>22);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint EonLargeShift(uint a, uint b)
        {
            //ARM64-FULL-LINE: eon {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #27
            return a ^ ~(b<<123);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong EonLargeShift64Bit(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: eon {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSR #38
            return a ^ ~(b>>166);
        }
    }
}
