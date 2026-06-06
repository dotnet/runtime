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

            if (NegCmpZero(12) != 1)
            {
                fail = true;
            }

            if (NegLslCmpZero(2) != 1)
            {
                fail = true;
            }

            if (NegLsrCmpZero(8) != 1)
            {
                fail = true;
            }

            if (NegAsrCmpZero(-5) != 1)
            {
                fail = true;
            }

            if (NegLargeShiftCmpZero(20) != 1)
            {
                fail = true;
            }

            if (NegLargeShift64BitsCmpZero(0x400000000) != 1)
            {
                fail = true;
            }

            if (!NegNeZeroSingleLine(1))
            {
                fail = true;
            }

            if (!NegNeZeroSingleLineLsl(0xF))
            {
                fail = true;
            }

            if (NegNeZeroBinOp(-4, 0x3F) != 1)
            {
                fail = true;
            }

            if (!NegNeZeroBinOpSingleLine(1, -1))
            {
                fail = true;
            }

            if (!NegGtZero(-1))
            {
                fail = true;
            }

            if (!NegGeZero(0))
            {
                fail = true;
            }

            if (!NegLtZero(5))
            {
                fail = true;
            }

            if (!NegLeZero(20))
            {
                fail = true;
            }

            if (NegGtIntMinValue())
            {
                fail = true;
            }

            if (NegGtLongMinValue())
            {
                fail = true;
            }

            if (!NegGtZeroShort(-1))
            {
                fail = true;
            }

            if (!NegLtZeroShort(1))
            {
                fail = true;
            }

            if (!NegLtConstShort(5))
            {
                fail = true;
            }

            if (!NegLtConstInt(5))
            {
                fail = true;
            }

            if (!NegGeZeroShort(0))
            {
                fail = true;
            }

            if (!NegLeZeroShort(1))
            {
                fail = true;
            }

            if (!NegEqZeroShort(0))
            {
                fail = true;
            }

            if (!NegNeZeroShort(1))
            {
                fail = true;
            }

            if (!NegEqConstShort(-20))
            {
                fail = true;
            }

            if (!NegEqConstInt(-20))
            {
                fail = true;
            }

            if (!NegNeConstShort(20))
            {
                fail = true;
            }

            if (!NegNeConstInt(20))
            {
                fail = true;
            }

            if (NegAddNotEqualZero(5, -5))
            {
                fail = true;
            }

            if (!NegAddNotEqual1(5, -5))
            {
                fail = true;
            }

            if (!NegAddNotEqual4(5, -5))
            {
                fail = true;
            }

            if (!NegAddEquaLZero(5, -5))
            {
                fail = true;
            }

            if (!NegAddEquaL1(0, -1))
            {
                fail = true;
            }

            if (!NegAddEquaL4(0, -4))
            {
                fail = true;
            }


            if (!NegAddGtZero(-5, 0))
            {
                fail = true;
            }

            if (!NegAddGt1(-5, 0))
            {
                fail = true;
            }

            if (!NegAddGt4(-5, 0))
            {
                fail = true;
            }

            if (!NegAddGeZero(-5, 0))
            {
                fail = true;
            }

            if (!NegAddGe1(-5, 0))
            {
                fail = true;
            }

            if (!NegAddGe4(-5, 0))
            {
                fail = true;
            }

            if (!NegAddLtZero(1, 0))
            {
                fail = true;
            }

            if (!NegAddLt1(1, 0))
            {
                fail = true;
            }

            if (!NegAddLt4(1, 0))
            {
                fail = true;
            }

            if (!NegAddLeZero(1, 0))
            {
                fail = true;
            }

            if (!NegAddLe1(1, 0))
            {
                fail = true;
            }

            if (!NegAddLe4(1, 0))
            {
                fail = true;
            }

            if (NegSubNotEqualZero(5, 5))
            {
                fail = true;
            }

            if (NegSubNotEqual1(0, 1))
            {
                fail = true;
            }

            if (NegSubNotEqual4(1, 5))
            {
                fail = true;
            }

            if (!NegSubEquaLZero(5, 5))
            {
                fail = true;
            }

            if (!NegSubEquaL1(0, 1))
            {
                fail = true;
            }

            if (!NegSubEquaL4(1, 5))
            {
                fail = true;
            }


            if (!NegSubGtZero(-5, 0))
            {
                fail = true;
            }

            if (!NegSubGt1(-5, 0))
            {
                fail = true;
            }

            if (!NegSubGt4(-5, 0))
            {
                fail = true;
            }

            if (!NegSubGeZero(-5, 0))
            {
                fail = true;
            }

            if (!NegSubGe1(-5, 0))
            {
                fail = true;
            }

            if (!NegSubGe4(-5, 0))
            {
                fail = true;
            }

            if (!NegSubLtZero(1, 0))
            {
                fail = true;
            }

            if (!NegSubLt1(1, 0))
            {
                fail = true;
            }

            if (!NegSubLt4(1, 0))
            {
                fail = true;
            }

            if (!NegSubLeZero(1, 0))
            {
                fail = true;
            }

            if (!NegSubLe1(1, 0))
            {
                fail = true;
            }

            if (!NegSubLe4(1, 0))
            {
                fail = true;
            }

            if (NegAndNotEqualZero(5, 2))
            {
                fail = true;
            }

            if (NegAndNotEqual1(-1, -1))
            {
                fail = true;
            }

            if (NegAndNotEqual4(-4, -4))
            {
                fail = true;
            }

            if (!NegAndEquaLZero(5, 2))
            {
                fail = true;
            }

            if (!NegAndEquaL1(-1, -1))
            {
                fail = true;
            }

            if (!NegAndEquaL4(-4, -4))
            {
                fail = true;
            }


            if (!NegBicEquaLZero(0, 0))
            {
                fail = true;
            }

            if (!NegBicEquaL1(-1, 0))
            {
                fail = true;
            }

            if (!NegBicEquaL4(-4, 0))
            {
                fail = true;
            }

            if (!NegBicNotEqualZero(1, 0))
            {
                fail = true;
            }

            if (!NegBicNotEqual1(0, 0))
            {
                fail = true;
            }

            if (!NegBicNotEqual4(0, 0))
            {
                fail = true;
            }

            if (!NegBicGtZero(-5, 0))
            {
                fail = true;
            }

            if (!NegBicGt1(-5, 0))
            {
                fail = true;
            }

            if (!NegBicGt4(-5, 0))
            {
                fail = true;
            }

            if (!NegBicGeZero(-5, 0))
            {
                fail = true;
            }

            if (!NegBicGe1(-5, 0))
            {
                fail = true;
            }

            if (!NegBicGe4(-5, 0))
            {
                fail = true;
            }

            if (!NegBicLtZero(1, 0))
            {
                fail = true;
            }

            if (!NegBicLt1(1, 0))
            {
                fail = true;
            }

            if (!NegBicLt4(1, 0))
            {
                fail = true;
            }

            if (!NegBicLeZero(1, 0))
            {
                fail = true;
            }

            if (!NegBicLe1(1, 0))
            {
                fail = true;
            }

            if (!NegBicLe4(1, 0))
            {
                fail = true;
            }


            if (!NegAndGtZero(-5, -5))
            {
                fail = true;
            }

            if (!NegAndGt1(-5, -5))
            {
                fail = true;
            }

            if (!NegAndGt4(-5, -5))
            {
                fail = true;
            }

            if (!NegAndGeZero(-5, -5))
            {
                fail = true;
            }

            if (!NegAndGe1(-5, -5))
            {
                fail = true;
            }

            if (!NegAndGe4(-5, -5))
            {
                fail = true;
            }

            if (!NegAndLtZero(1, 1))
            {
                fail = true;
            }

            if (!NegAndLt1(1, 1))
            {
                fail = true;
            }

            if (!NegAndLt4(1, 1))
            {
                fail = true;
            }

            if (!NegAndLeZero(1, 1))
            {
                fail = true;
            }

            if (!NegAndLe1(1, 1))
            {
                fail = true;
            }

            if (!NegAndLe4(1, 1))
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
            return -(a << 2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint NegLSR(uint a)
        {
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}},  #20
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            return (uint)-(a >> 20);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegASR(int a)
        {
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}, ASR #13
            return -(a >> 13);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegLargeShift(int a)
        {
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}, LSL #17
            return -(a << 81);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long NegLargeShift64Bit(long a)
        {
            //ARM64-FULL-LINE: neg {{x[0-9]+}}, {{x[0-9]+}}, LSL #61
            return -(a << 189);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegCmpZero(int a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            if (-a != 0)
            {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegLslCmpZero(int a)
        {
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}, LSL #14
            if (-(a << 14) != 0)
            {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegLsrCmpZero(uint a)
        {
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            if (-(a >> 3) != 0)
            {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegAsrCmpZero(int a)
        {
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}, ASR #2
            if (-(a >> 2) != 0)
            {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegLargeShiftCmpZero(uint a)
        {
            //ARM64-FULL-LINE: lsl  {{w[0-9]+}}, {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            if (-(a << 100) != 0)
            {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegLargeShift64BitsCmpZero(long a)
        {
            //ARM64-FULL-LINE: negs {{x[0-9]+}}, {{x[0-9]+}}, ASR #34
            if (-(a >> 98) != 0)
            {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegNeZeroSingleLine(int a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            return -a != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegNeZeroSingleLineLsl(int a)
        {
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}, LSL #13
            return -(a << 13) != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegNeZeroBinOp(int a, int b)
        {
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}, ASR #5
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ne
            if ((-a != 0) == (-(b >> 5) != 0))
            {
                return 1;
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegNeZeroBinOpSingleLine(int a, int b)
        {
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}, ASR #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            //ARM64-FULL-LINE: negs {{w[0-9]+}}, {{w[0-9]+}}, LSL #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return (-(a >> 1) != 0) | (-(b << 1) != 0);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegGtZero(int a)
        {
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            return -a > 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegGeZero(int a)
        {
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            return -a >= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegLtZero(int a)
        {
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #31
            return -a < 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegLeZero(int a)
        {
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            return -a <= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegGtIntMinValue()
        {
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            return -IntMinValue() > 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegGtLongMinValue()
        {
            //ARM64-FULL-LINE: neg {{x[0-9]+}}, {{x[0-9]+}}
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #0
            return -LongMinValue() > 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int IntMinValue()
        {
            return int.MinValue;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long LongMinValue()
        {
            return long.MinValue;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegGtZeroShort(short x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return -x > 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegLtZeroShort(short x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #31
            return -x < 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegLtConstShort(short x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #5
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, lt
            return -x < 5;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegLtConstInt(int x)
        {
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #5
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, lt
            return -x < 5;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegGeZeroShort(short x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ge
            return -x >= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegLeZeroShort(short x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return -x <= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegEqZeroShort(short x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return -x == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegNeZeroShort(short x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return -x != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegEqConstShort(short x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #20
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return -x == 20;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegEqConstInt(int x)
        {
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #20
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return -x == 20;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegNeConstShort(short x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #20
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return -x != 20;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegNeConstInt(int x)
        {
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #20
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return -x != 20;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddEquaLZero(int a, int b)
        {
            //ARM64-FULL-LINE: adds {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return (-(a + b)) == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddEquaL1(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return (-(a + b)) == 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddEquaL4(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return (-(a + b)) == 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddNotEqualZero(int a, int b)
        {
            //ARM64-FULL-LINE: adds {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return (-(a + b)) != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddNotEqual1(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return (-(a + b)) != 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddNotEqual4(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return (-(a + b)) != 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddGtZero(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a + b)) > 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddGt1(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a + b)) > 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddGt4(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a + b)) > 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddGeZero(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ge
            return (-(a + b)) >= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddGe1(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a + b)) >= 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddGe4(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ge
            return (-(a + b)) >= 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddLtZero(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #31
            return (-(a + b)) < 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddLt1(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a + b)) < 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddLt4(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, lt
            return (-(a + b)) < 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddLeZero(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a + b)) <= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddLe1(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a + b)) <= 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAddLe4(int a, int b)
        {
            //ARM64-FULL-LINE: add {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a + b)) <= 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubEquaLZero(int a, int b)
        {
            //ARM64-FULL-LINE: subs {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return (-(a - b)) == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubEquaL1(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return (-(a - b)) == 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubEquaL4(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return (-(a - b)) == 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubNotEqualZero(int a, int b)
        {
            //ARM64-FULL-LINE: subs {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return (-(a - b)) != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubNotEqual1(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return (-(a - b)) != 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubNotEqual4(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return (-(a - b)) != 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubGtZero(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a - b)) > 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubGt1(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a - b)) > 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubGt4(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a - b)) > 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubGeZero(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ge
            return (-(a - b)) >= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubGe1(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a - b)) >= 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubGe4(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ge
            return (-(a - b)) >= 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubLtZero(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #31
            return (-(a - b)) < 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubLt1(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a - b)) < 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubLt4(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, lt
            return (-(a - b)) < 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubLeZero(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a - b)) <= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubLe1(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a - b)) <= 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegSubLe4(int a, int b)
        {
            //ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a - b)) <= 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndEquaLZero(int a, int b)
        {
            //ARM64-FULL-LINE: tst {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return (-(a & b)) == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndEquaL1(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return (-(a & b)) == 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndEquaL4(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return (-(a & b)) == 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndNotEqualZero(int a, int b)
        {
            //ARM64-FULL-LINE: tst {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return (-(a & b)) != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndNotEqual1(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return (-(a & b)) != 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndNotEqual4(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return (-(a & b)) != 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndGtZero(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a & b)) > 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndGt1(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a & b)) > 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndGt4(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a & b)) > 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndGeZero(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ge
            return (-(a & b)) >= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndGe1(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a & b)) >= 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndGe4(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ge
            return (-(a & b)) >= 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndLtZero(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #31
            return (-(a & b)) < 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndLt1(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a & b)) < 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndLt4(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, lt
            return (-(a & b)) < 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndLeZero(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a & b)) <= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndLe1(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a & b)) <= 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegAndLe4(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a & b)) <= 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicEquaLZero(int a, int b)
        {
            //ARM64-FULL-LINE: bics {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return (-(a & ~b)) == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicEquaL1(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return (-(a & ~b)) == 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicEquaL4(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, eq
            return (-(a & ~b)) == 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicNotEqualZero(int a, int b)
        {
            //ARM64-FULL-LINE: bics {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return (-(a & ~b)) != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicNotEqual1(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return (-(a & ~b)) != 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicNotEqual4(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ne
            return (-(a & ~b)) != 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicGtZero(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a & ~b)) > 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicGt1(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a & ~b)) > 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicGt4(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a & ~b)) > 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicGeZero(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ge
            return (-(a & ~b)) >= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicGe1(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return (-(a & ~b)) >= 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicGe4(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ge
            return (-(a & ~b)) >= 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicLtZero(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #31
            return (-(a & ~b)) < 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicLt1(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a & ~b)) < 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicLt4(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, lt
            return (-(a & ~b)) < 4;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicLeZero(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a & ~b)) <= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicLe1(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a & ~b)) <= 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool NegBicLe4(int a, int b)
        {
            //ARM64-FULL-LINE: bic {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: neg {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return (-(a & ~b)) <= 4;
        }
    }
}
