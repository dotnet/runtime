// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test the following optimizations:
//  -(v * const) => v * -const;
//  -v * const => v * -const;
//  -(v / const) => v / -const;
//  -v / const => v / -const;
//
// Note that C# spec tells that `int.MinValue / -1` result is implementation specific, but
// ecma-335 requires it to throw `System.ArithmeticException` in such case, so we should not
// change 1 and -1 sign.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestIntLimits
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CheckMulNeg()
        {
            bool fail = false;
            if (MulNeg7(3) != -7 * 3)
            {
                fail = true;
            }

            if (MulNeg0(100) != 0)
            {
                fail = true;
            }

            try
            {
                MulNegIntMin(2);
            }
            catch
            {
                fail = true;
            }

            try
            {
                CheckedMulNegIntMin(1);
                fail = true;
            }
            catch
            { }

            try
            {
                CheckedMulNegIntMin(0);

            }
            catch
            {
                fail = true;
            }

            if (NegMulIntMaxValue(1) != -int.MaxValue)
            {
                fail = true;
            }
            if (NegMulIntMaxValue(0) != 0)
            {
                fail = true;
            }
            if (NegMulIntMaxValue(-1) != int.MaxValue)
            {
                fail = true;
            }

            try
            {
                CheckedMulNeg0(int.MinValue);
                fail = true;
            }
            catch
            { }

            if (MulNegCombined(3, 4) != -12)
            {
                fail = true;
            }

            if (fail)
            {
                Console.WriteLine("CheckMulNeg failed");
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int MulNeg7(int a) => -a * 7;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int MulNeg0(int a) => -a * 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int MulNegIntMin(int a) => -a * int.MinValue;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CheckedMulNegIntMin(int a)
        {
            checked
            {
                return -a * int.MinValue;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegMulIntMaxValue(int a) => -(a * int.MaxValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CheckedMulNeg0(int a)
        {
            checked
            {
                return -a * 0;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CheckNegMul()
        {
            bool fail = false;
            if (NegMul7(3) != -7 * 3)
            {
                fail = true;
            }

            try
            {
                NegMulIntMinValue(100);
            }
            catch
            {
                fail = true;
            }


            try
            {
                CheckedNegMulIntMinValue(1);
                fail = true;
            }
            catch
            { }

            try
            {
                CheckedNegMulIntMinValue(0);

            }
            catch
            {
                fail = true;
            }

            if (fail)
            {
                Console.WriteLine("CheckNegMul failed");
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegMul7(int a)
        {
            //ARM64-FULL-LINE: movn {{w[0-9]+}}, #6
            //ARM64-FULL-LINE-NEXT: mul {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return -(a * 7);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegMulIntMinValue(int a) => -(a * int.MinValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CheckedNegMulIntMinValue(int a)
        {
            checked
            {
                return -(a * int.MinValue);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CheckDivNeg()
        {
            bool fail = false;

            if (DivNeg11(110) != -10)
            {
                fail = true;
            }

            if (LongDivNeg1000(100000000000) != -100000000)
            {
                fail = true;
            }

            try
            {
                DivNegIntMinValue(1);
                DivNegIntMinValue(int.MinValue);
                LongDivNegLongMinValue(1);
                LongDivNegLongMinValue(long.MinValue);
                DivNeg1(int.MinValue);
                LongDivNeg1(long.MinValue);
            }
            catch
            {
                fail = true;
            }

            if (fail)
            {
                Console.WriteLine("CheckDivNeg failed");
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int MulNegCombined(int a, int b)
        {
            //ARM64-FULL-LINE: mneg {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return a * b * -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int DivNeg11(int a)
        {
            //ARM64-FULL-LINE: smull {{x[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE-NEXT: asr {{x[0-9]+}}, {{x[0-9]+}}, #32
            //ARM64-FULL-LINE-NEXT: asr {{w[0-9]+}}, {{w[0-9]+}}, #1
            return -a / 11;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long LongDivNeg1000(long a)
        {
            //ARM64-FULL-LINE: smulh {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}
            //ARM64-FULL-LINE-NEXT: asr {{x[0-9]+}}, {{x[0-9]+}}, #7
            //ARM64-FULL-LINE-NEXT: add {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSR #63
            return -a / 1000;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int DivNegIntMinValue(int a) => -a / int.MinValue;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long LongDivNegLongMinValue(long a) => -a / long.MinValue;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int DivNeg1(int a) => -a / 1;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long LongDivNeg1(long a) => -a / 1;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CheckNegDiv()
        {
            bool fail = false;

            if (NegDiv11(110) != -10)
            {
                fail = true;
            }

            if (LongNegDiv1000(100000000000) != -100000000)
            {
                fail = true;
            }

            try
            {
                NegDivIntMinValue(1);
                NegDivIntMinValue(int.MinValue);
                LongNegDivLongMinValue(1);
                LongNegDivLongMinValue(long.MinValue);
                NegDiv1(int.MinValue);
                LongNegDiv1(long.MinValue);
            }
            catch
            {
                fail = true;
            }

            try
            {
                NegDivMinus1(int.MinValue);
                fail = true;
            }
            catch
            { }

            try
            {
                LongNegDivMinus1(long.MinValue);
                fail = true;
            }
            catch
            { }

            if (fail)
            {
                Console.WriteLine("CheckNegDiv failed");
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegDiv11(int a) => -(a / 11);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long LongNegDiv1000(long a) => -(a / 1000);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegDivIntMinValue(int a) => -(a / int.MinValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long LongNegDivLongMinValue(long a) => -(a / long.MinValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegDiv1(int a) => -(a / 1);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long LongNegDiv1(long a) => -(a / 1);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NegDivMinus1(int a) => -(a / -1);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long LongNegDivMinus1(long a) => -(a / -1);

        [Fact]
        public static int TestEntryPoint()
        {
            if (CheckMulNeg() != 100)
            {
                return 101;
            }
            if (CheckNegMul() != 100)
            {
                return 101;
            }
            if (CheckDivNeg() != 100)
            {
                return 101;
            }
            if (CheckNegDiv() != 100)
            {
                return 101;
            }

            return 100;
        }
    }
}
