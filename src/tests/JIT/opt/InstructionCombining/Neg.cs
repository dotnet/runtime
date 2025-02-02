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
    }
}
