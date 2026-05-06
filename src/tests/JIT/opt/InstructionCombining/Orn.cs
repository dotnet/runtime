// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestOrn
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckOrn()
        {
            bool fail = false;

            if (Orn(0xAB, 0xCD) != 0xFFFFFFBB)
            {
                fail = true;
            }

            if (OrnLSL(0xAB, 0xCDE) != 0xCC87FFFF)
            {
                fail = true;
            }

            if (OrnLSLSwapInt(0xAB, -0xCDE) != 0x3377FFFF)
            {
                fail = true;
            }

            if (OrnLSLSwapUint(0xAB, 0xCDE) != 0xCC87FFFF)
            {
                fail = true;
            }

            if (OrnLSR(0x123456, 0xA9876543) != 0xFFFFFD5F)
            {
                fail = true;
            }

            if (OrnASR(0x141414, 0x13579) != -0x9AC)
            {
                fail = true;
            }

            if (OrnLargeShift(0x63636363, 0xE) != 0xFFFFFF7F)
            {
                fail = true;
            }

            if (OrnLargeShift64Bit(0xACEACEACEACEACE0, 0xFEDCFEDCFEDCFEDC) != 0xFFFFFFFFEACEECE8)
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
        static uint Orn(uint a, uint b)
        {
            //ARM64-FULL-LINE: orn {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return a | ~b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint OrnLSL(uint a, uint b)
        {
            //ARM64-FULL-LINE: orn {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #18
            return a | ~(b<<18);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int OrnLSLSwapInt(int a, int b)
        {
            //ARM64-FULL-LINE: orn {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #18
            return ~(b<<18) | a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint OrnLSLSwapUint(uint a, uint b)
        {
            //ARM64-FULL-LINE: orn {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #18
            return ~(b<<18) | a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint OrnLSR(uint a, uint b)
        {
            //ARM64-FULL-LINE: orn {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSR #22
            return a | ~(b>>22);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int OrnASR(int a, int b)
        {
            //ARM64-FULL-LINE: orn {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ASR #5
            return a | ~(b>>5);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint OrnLargeShift(uint a, uint b)
        {
            //ARM64-FULL-LINE: orn {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #6
            return a | ~(b<<102);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong OrnLargeShift64Bit(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: orn {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSR #34
            return a | ~(b>>162);
        }
    }
}
