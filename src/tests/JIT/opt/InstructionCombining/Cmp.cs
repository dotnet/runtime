// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestCompare
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckCompare()
        {
            bool fail = false;

            if (!Cmp(12, 12))
            {
                fail = true;
            }

            if (!CmpLSL(12, 3))
            {
                fail = true;
            }

            if (!CmpLSLSwap(12, 3))
            {
                fail = true;
            }

            if (!CmpLSR(5, 0xa00))
            {
                fail = true;
            }

            if (!CmpASR(7, 0x380))
            {
                fail = true;
            }

            if (!CmpLargeShift(0x500000, 0xA))
            {
                fail = true;
            }

            if (!CmpLargeShift64Bit(0x580000000000000, 0xB))
            {
                fail = true;
            }

            if (!CmpOptimizeBoolsReturn(5, 3, 10))
            {
                fail = true;
            }

            if (!CmpOptimizeBoolsReturnOr(2, 3, 4))
            {
                fail = true;
            }

            if (CmpOptimizeBoolsIf(5, 3, 10) != 1)
            {
                fail = true;
            }

            if (CmpOptimizeBoolsIfOr(2, 3, 4) != 1)
            {
                fail = true;
            }

            if (!CmpMultipleOptimizeBools(1, 2, 3, 3, 5, 4))
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
        static bool Cmp(int a, int b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}
            if (a == b)
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmpLSL(int a, int b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, LSL #2
            if (a == (b<<2))
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmpLSLSwap(int a, int b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, LSL #2
            if ((b<<2) == a)
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmpLSR(uint a, uint b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, LSR #9
            if (a == (b>>9))
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmpASR(int a, int b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, ASR #7
            if (a == (b>>7))
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmpLargeShift(int a, int b)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}, LSL #19
            if (a == (b<<115))
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmpLargeShift64Bit(long a, long b)
        {
            //ARM64-FULL-LINE: lsl {{x[0-9]+}}, {{x[0-9]+}}, #55
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, {{x[0-9]+}}
            if (a == (b<<119))
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmpOptimizeBoolsReturn(int a, int lower, int upper)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: ccmp {{w[0-9]+}}, {{w[0-9]+}}, 0, ge
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return a >= lower && a <= upper;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmpOptimizeBoolsReturnOr(int a, int lower, int upper)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: ccmp {{w[0-9]+}}, {{w[0-9]+}}, nzc, lt
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return a >= lower || a <= upper;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CmpOptimizeBoolsIf(int a, int lower, int upper)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: ccmp {{w[0-9]+}}, {{w[0-9]+}}, 0, ge
            //ARM64-FULL-LINE: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, gt
            if (a >= lower && a <= upper)
            {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CmpOptimizeBoolsIfOr(int a, int lower, int upper)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: ccmp {{w[0-9]+}}, {{w[0-9]+}}, nzc, lt
            //ARM64-FULL-LINE: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, gt
            if (a >= lower || a <= upper)
            {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CmpMultipleOptimizeBools(int a, int b, int c, int d, int e, int f)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: ccmp {{w[0-9]+}}, {{w[0-9]+}}, 0, lt
            //ARM64-FULL-LINE: ccmp {{w[0-9]+}}, {{w[0-9]+}}, nzc, le
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (a < b) && (c <= d) && (e > f);
        }
    }
}
