// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestBitwiseClearShift
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckBitwiseClearShift()
        {
            bool fail = false;

            if (!Bic(0xFFFFFFFF, 0x12345678, 0xEDCBA987))
            {
                fail = true;
            }

            if (!BicLSL(0xFFFFFFFF, 0x12345678, 0xDCBA987F))
            {
                fail = true;
            }

            if (!BicLSR(0xFFFFFFFF, 0x12345678, 0xFFFEDCBA))
            {
                fail = true;
            }

            if (!BicASR(0xFFFF, 0x8765, 0xFEF1))
            {
                fail = true;
            }

            if (!BicLargeShift(0xFEFEFEFE, 1, 0xFEFCFEFE))
            {
                fail = true;
            }

            if (!BicLargeShift64Bit(0xFEFEFEFEFEFEFE, 0xFEDCBA98765432, 0xFEFEFEFEF01234))
            {
                fail = true;
            }

            if (Bics(0xFFFFFFFF, 0x100) != 1)
            {
                fail = true;
            }

            if (BicsLSL(0xFFFFFFFF, 1) != 1)
            {
                fail = true;
            }

            if (BicsLSR(0xFFFFFFFF, 0x87654321) != 1)
            {
                fail = true;
            }

            if (BicsASR(0xFFFF, 0x8F6E) != 1)
            {
                fail = true;
            }

            if (BicsLargeShift(0xFFFFFFFF, 0x87654321) != 1)
            {
                fail = true;
            }

            if (BicsLargeShift64Bit(0xFFFFFFFFFFFFFFFF, 0xFEDCBA9876543210) != 1)
            {
                fail = true;
            }

            if (!BicsSingleLine(0x22446688, 0x22446688))
            {
                fail = true;
            }

            if (!BicsSingleLineLSL(0xABC, 0xFFFEA87F))
            {
                fail = true;
            }

            if (BicsBinOp(0xFF012FFF, 0xFED, 0xFFDDBB99, 0xFF002244) != 1)
            {
                fail = true;
            }

            if (!BicsBinOpSingleLine(0x66665555, 0x9999AAAA, 0xFFFFFFFD, 0x2))
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
        static bool Bic(uint a, uint b, uint c)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            if ((a & ~b) == c)
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool BicLSL(uint a, uint b, uint c)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            if ((a & ~(b<<4)) == c)
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool BicLSR(uint a, uint b, uint c)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSR #12
            if ((a & ~(b>>12)) == c)
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool BicASR(int a, int b, int c)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ASR #7
            if ((a & ~(b>>7)) == c)
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool BicLargeShift(uint a, uint b, uint c)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #17
            if ((a & ~(b<<145)) == c)
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool BicLargeShift64Bit(long a, long b, long c)
        {
            //ARM64-FULL-LINE: bic {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, ASR #36
            if ((a & ~(b>>292)) == c)
            {
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Bics(uint a, uint b)
        {
            //ARM64-FULL-LINE: bics {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            if ((a & ~b) == 0)
            {
                return -1;
            }
            return 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int BicsLSL(uint a, uint b)
        {
            //ARM64-FULL-LINE: bics {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #31
            if ((a & ~(b<<31)) == 0)
            {
                return -1;
            }
            return 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int BicsLSR(uint a, uint b)
        {
            //ARM64-FULL-LINE: bics {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSR #20
            if ((a & ~(b>>20)) == 0)
            {
                return -1;
            }
            return 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int BicsASR(int a, int b)
        {
            //ARM64-FULL-LINE: bics {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ASR #9
            if ((a & ~(b>>9)) == 0)
            {
                return -1;
            }
            return 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int BicsLargeShift(uint a, uint b)
        {
            //ARM64-FULL-LINE: bics {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSR #3
            if ((a & ~(b>>99)) == 0)
            {
                return -1;
            }
            return 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int BicsLargeShift64Bit(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: bics {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #51
            if ((a & ~(b<<179)) == 0)
            {
                return -1;
            }
            return 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool BicsSingleLine(uint a, uint b)
        {
            //ARM64-FULL-LINE: bics {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return (a & ~b) == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool BicsSingleLineLSL(uint a, uint b)
        {
            //ARM64-FULL-LINE: bics {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #5
            return (~(a<<5) & b) != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int BicsBinOp(uint a, uint b, uint c, uint d)
        {
            //ARM64-FULL-LINE: bics {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSR #8
            //ARM64-FULL-LINE: bics {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #12
            if (((a & ~(b<<12)) == 0) == ((~(c>>8) & d) == 0)) {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool BicsBinOpSingleLine(uint a, uint b, uint c, uint d)
        {
            //ARM64-FULL-LINE: bics {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: bics {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return ((~a & b) != 0) & ((c & ~d) != 0);
        }
    }
}
