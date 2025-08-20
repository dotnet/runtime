// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestNeg
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckNeg()
        {
            bool fail = false;

            if (Neg(3) != -3)
            {
                fail = true;
            }

            if (NegLSL(4) != -16)
            {
                fail = true;
            }

            if (NegLSR(0x300000) != 0xFFFFFFFD)
            {
                fail = true;
            }

            if (NegASR(0xA000) != -5)
            {
                fail = true;
            }

            if (NegLargeShift(0xC) != -0x180000)
            {
                fail = true;
            }
            
            if (NegLargeShift64Bit(0xD) != 0x6000000000000000)
            {
                fail = true;
            }

            if (Negs(12) != 1)
            {
                fail = true;
            }

            if (NegsLSL(2) != 1)
            {
                fail = true;
            }

            if (NegsLSR(8) != 1)
            {
                fail = true;
            }

            if (NegsASR(-5) != 1)
            {
                fail = true;
            }

            if (NegsLargeShift(20) != 1)
            {
                fail = true;
            }

            if (NegsLargeShift64Bits(0x400000000) != 1)
            {
                fail = true;
            }

            if (!NegsSingleLine(1))
            {
                fail = true;
            }

            if (!NegsSingleLineLSL(0xF))
            {
                fail = true;
            }

            if (NegsBinOp(-4, 0x3F) != 1)
            {
                fail = true;
            }

            if (!NegsBinOpSingleLine(1, -1))
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
        static int Neg(int a)
        {
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            return -a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegLSL(int a)
        {
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}, LSL #2
            return -(a<<2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint NegLSR(uint a)
        {
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}},  #20
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            return (uint)-(a>>20);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegASR(int a)
        {
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}, ASR #13
            return -(a>>13);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegLargeShift(int a)
        {
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}, LSL #17
            return -(a<<81);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long NegLargeShift64Bit(long a)
        {
            //ARM64-FULL-LINE: neg {{x[0-9]+}}, {{x[0-9]+}}, LSL #61
            return -(a<<189);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Negs(int a)
        {
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}
            if (-a != 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegsLSL(int a)
        {
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}, LSL #14
            if (-(a<<14) != 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegsLSR(uint a)
        {
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: negs {{x[0-9]+}}, {{x[0-9]+}}
            if (-(a>>3) != 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegsASR(int a)
        {
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}, ASR #2
            if (-(a>>2) != 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegsLargeShift(uint a)
        {
            //ARM64-FULL-LINE: lsl  {{w[0-9]+}}, {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: negs {{x[0-9]+}}, {{x[0-9]+}}
            if (-(a<<100) != 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegsLargeShift64Bits(long a)
        {
            //ARM64-FULL-LINE: negs {{x[0-9]+}}, {{x[0-9]+}}, ASR #34
            if (-(a>>98) != 0) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegsSingleLine(int a)
        {
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}
            return -a != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegsSingleLineLSL(int a)
        {
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}, LSL #13
            return -(a<<13) != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegsBinOp(int a, int b)
        {
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}, ASR #5
            if ((-a != 0) == (-(b>>5) != 0)) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegsBinOpSingleLine(int a, int b)
        {
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}, ASR #1
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}, LSL #1
            return (-(a>>1) != 0) | (-(b<<1) != 0);
        }
    }
}
