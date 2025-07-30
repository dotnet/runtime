// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestCompareNegative
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckCompareNegative()
        {
            bool fail = false;

            if (!Cmn(-4, 4))
            {
                fail = true;
            }

            if (!CmnLSL(-16, 4))
            {
                fail = true;
            }

            if (!CmnLSLSwap(-16, 4))
            {
                fail = true;
            }

            if (!CmnLSR(0xFFFFFFFC, 0x10))
            {
                fail = true;
            }

            if (!CmnASR(-0xA, 0x2800))
            {
                fail = true;
            }

            if (!CmnLargeShift(-0x18, 0x3))
            {
                fail = true;
            }

            if (!CmnLargeShift64Bit(-0x300000000000000, 0x6))
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
        static bool Cmn(int a, int b)
        {
            //ARM64-FULL-LINE: cmn {{w[0-9]+}}, {{w[0-9]+}}
            if (a == -b)
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmnLSL(int a, int b)
        {
            //ARM64-FULL-LINE: cmn {{w[0-9]+}}, {{w[0-9]+}},  LSL #2
            if (a == -(b<<2))
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmnLSLSwap(int a, int b)
        {
            //ARM64-FULL-LINE: cmn {{w[0-9]+}}, {{w[0-9]+}},  LSL #2
            if (-(b<<2) == a)
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmnLSR(uint a, uint b)
        {
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}},  #2
            //ARM64-FULL-LINE: cmn {{w[0-9]+}}, {{w[0-9]+}}
            if (a == (uint)-(b>>2))
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmnASR(int a, int b)
        {
            //ARM64-FULL-LINE: cmn {{w[0-9]+}}, {{w[0-9]+}},  ASR #10
            if (a == -(b>>10))
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmnLargeShift(int a, int b)
        {
            //ARM64-FULL-LINE: cmn {{w[0-9]+}}, {{w[0-9]+}},  LSL #3
            if (a == -(b<<35))
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmnLargeShift64Bit(long a, long b)
        {
            //ARM64-FULL-LINE: cmn {{x[0-9]+}}, {{x[0-9]+}},  LSL #55
            if (a == -(b<<247))
            {
                return true;
            }
            return false;
        }
    }
}
