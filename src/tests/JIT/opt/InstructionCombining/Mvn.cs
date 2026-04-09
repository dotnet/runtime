// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestMvn
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckMvn()
        {
            bool fail = false;

            if (Mvn(5) != 0xFFFFFFFA)
            {
                fail = true;
            }

            if (MvnLSL(10) != 0xFFFAFFFF)
            {
                fail = true;
            }

            if (MvnLSR(0x76543210) != 0xFFFFFF89)
            {
                fail = true;
            }

            if (MvnASR(0xACE1234) != -0x5670A)
            {
                fail = true;
            }

            if (MvnLargeShift(0x1A1A) != 0x5FFFFFFf)
            {
                fail = true;
            }

            if (MvnLargeShift64Bit(0x2B3C2B3C2B3C2B3C) != 0xFFFFFFFFD4C3D4C3)
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
        static uint Mvn(uint a)
        {
            //ARM64-FULL-LINE: mvn {{w[0-9]+}}, {{w[0-9]+}}
            return ~a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint MvnLSL(uint a)
        {
            //ARM64-FULL-LINE: mvn {{w[0-9]+}}, {{w[0-9]+}}, LSL #15
            return ~(a<<15);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint MvnLSR(uint a)
        {
            //ARM64-FULL-LINE: mvn {{w[0-9]+}}, {{w[0-9]+}}, LSR #24
            return ~(a>>24);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int MvnASR(int a)
        {
            //ARM64-FULL-LINE: mvn {{w[0-9]+}}, {{w[0-9]+}}, ASR #9
            return ~(a>>9);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint MvnLargeShift(uint a)
        {
            //ARM64-FULL-LINE: mvn {{w[0-9]+}}, {{w[0-9]+}}, LSL #28
            return ~(a<<60);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong MvnLargeShift64Bit(ulong a)
        {
            //ARM64-FULL-LINE: mvn {{x[0-9]+}}, {{x[0-9]+}}, LSR #32
            return ~(a>>160);
        }
    }
}
