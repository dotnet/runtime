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

namespace TestIntLimits
{
    class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CheckMulNeg()
        {
            bool fail = false;
            if (Mul1_1(3) != -7 * 3)
            {
                fail = true;
            }

            if (Mul1_2(100) != 0)
            {
                fail = true;
            }

            try
            {
                Mul1_3(2);
            }
            catch
            {
                fail = true;
            }


            try
            {
                Mul1_4(1);
                fail = true;
            }
            catch
            { }

            try
            {
                Mul1_4(0);

            }
            catch
            {
                fail = true;
            }

            if (Mul1_5(1) != -int.MaxValue)
            {
                fail = true;
            }
            if (Mul1_5(0) != 0)
            {
                fail = true;
            }
            if (Mul1_5(-1) != int.MaxValue)
            {
                fail = true;
            }

            try
            {
                Mul1_6(int.MinValue);
                fail = true;
            }
            catch
            { }


            if (fail)
            {
                Console.WriteLine("CheckMulNeg failed");
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Mul1_1(int a) => -a * 7;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Mul1_2(int a) => -a * 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Mul1_3(int a) => -a * int.MinValue;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Mul1_4(int a)
        {
            checked
            {
                return -a * int.MinValue;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Mul1_5(int a) => -(a * int.MaxValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Mul1_6(int a)
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
            if (Mul2_1(3) != -7 * 3)
            {
                fail = true;
            }

            try
            {
                Mul2_2(100);
            }
            catch
            {
                fail = true;
            }


            try
            {
                Mul2_3(1);
                fail = true;
            }
            catch
            { }

            try
            {
                Mul2_3(0);

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
        static int Mul2_1(int a) => -(a * 7);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Mul2_2(int a) => -(a * int.MinValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Mul2_3(int a)
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

            if (Div1_1(110) != -10)
            {
                fail = true;
            }

            if (Div1_2(100000000000) != -100000000)
            {
                fail = true;
            }

            try
            {
                Div1_3(1);
                Div1_3(int.MinValue);
                Div1_4(1);
                Div1_4(long.MinValue);
                Div1_5(int.MinValue);
                Div1_6(long.MinValue);
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
        static int Div1_1(int a) => -a / 11;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Div1_2(long a) => -a / 1000;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Div1_3(int a) => -a / int.MinValue;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Div1_4(long a) => -a / long.MinValue;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Div1_5(int a) => -a / 1;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Div1_6(long a) => -a / 1;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CheckNegDiv()
        {
            bool fail = false;

            if (Div2_1(110) != -10)
            {
                fail = true;
            }

            if (Div2_2(100000000000) != -100000000)
            {
                fail = true;
            }

            try
            {
                Div2_3(1);
                Div2_3(int.MinValue);
                Div2_4(1);
                Div2_4(long.MinValue);
                Div2_5(int.MinValue);
                Div2_6(long.MinValue);
            }
            catch
            {
                fail = true;
            }

            try
            {
                Div2_7(int.MinValue);
                fail = true;
            }
            catch
            { }

            try
            {
                Div2_8(long.MinValue);
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
        static int Div2_1(int a) => -(a / 11);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Div2_2(long a) => -(a / 1000);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Div2_3(int a) => -(a / int.MinValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Div2_4(long a) => -(a / long.MinValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Div2_5(int a) => -(a / 1);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Div2_6(long a) => -(a / 1);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Div2_7(int a) => -(a / -1);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Div2_8(long a) => -(a / -1);

        static int Main()
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