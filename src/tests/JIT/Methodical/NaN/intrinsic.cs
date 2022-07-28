// Licensed to the .NET Foundation under one or more agreements.
using Xunit;
namespace Test_intrinsic
{
// The .NET Foundation licenses this file to you under the MIT license.

namespace JitTest
{
    using System;

    public class Test
    {
        private static void Fail(String func, double arg, double exp, double res)
        {
            throw new Exception(func + "(" + arg.ToString() +
                ") failed: expected " + exp.ToString() + ", got " + res.ToString());
        }

        private static void Fail2(String func, double arg1, double arg2, double exp, double res)
        {
            throw new Exception(func + "(" + arg1.ToString() +
                ", " + arg2.ToString() +
                ") failed: expected " + exp.ToString() + ", got " + res.ToString());
        }

        private static void TestAbs(double arg, double exp)
        {
            double res = Math.Abs(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Abs(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Abs(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Abs", arg, exp, res);
        }

        private static void TestAcos(double arg, double exp)
        {
            double res = Math.Acos(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Acos(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Acos(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Acos", res, arg, exp);
        }

        private static void TestAsin(double arg, double exp)
        {
            double res = Math.Asin(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Asin(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Asin(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Asin", res, arg, exp);
        }

        private static void TestAtan(double arg, double exp)
        {
            double res = Math.Atan(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Atan(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Atan(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Atan", res, arg, exp);
        }

        private static void TestCeiling(double arg, double exp)
        {
            double res = Math.Ceiling(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Ceiling(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Ceiling(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Ceiling", res, arg, exp);
        }

        private static void TestCos(double arg, double exp)
        {
            double res = Math.Cos(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Cos(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Cos(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Cos", res, arg, exp);
        }

        private static void TestCosh(double arg, double exp)
        {
            double res = Math.Cosh(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Cosh(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Cosh(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Cosh", res, arg, exp);
        }

        private static void TestExp(double arg, double exp)
        {
            double res = Math.Exp(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Exp(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Exp(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Exp", res, arg, exp);
        }

        private static void TestFloor(double arg, double exp)
        {
            double res = Math.Floor(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Floor(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Floor(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Floor", res, arg, exp);
        }

        private static void TestLog(double arg, double exp)
        {
            double res = Math.Log(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Log(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Log(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Log", res, arg, exp);
        }

        private static void TestLog10(double arg, double exp)
        {
            double res = Math.Log10(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Log10(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Log10(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Log10", res, arg, exp);
        }

        private static void TestRound(double arg, double exp)
        {
            double res = Math.Round(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Round(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Round(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Round", res, arg, exp);
        }

        private static void TestSign(double arg, double exp)
        {
            double res = Math.Sign(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Sign(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Sign(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Sign", res, arg, exp);
        }

        private static void TestSin(double arg, double exp)
        {
            double res = Math.Sin(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Sin(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Sin(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Sin", res, arg, exp);
        }

        private static void TestSinh(double arg, double exp)
        {
            double res = Math.Sinh(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Sinh(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Sinh(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Sinh", res, arg, exp);
        }

        private static void TestSqrt(double arg, double exp)
        {
            double res = Math.Sqrt(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Sqrt(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Sqrt(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Sqrt", res, arg, exp);
        }

        private static void TestTan(double arg, double exp)
        {
            double res = Math.Tan(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Tan(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Tan(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Tan", res, arg, exp);
        }

        private static void TestTanh(double arg, double exp)
        {
            double res = Math.Tanh(arg);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Tanh(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Tanh(" + arg.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail("Tanh", res, arg, exp);
        }

        private static void TestLog2(double arg1, double arg2, double exp)
        {
            double res = Math.Log(arg1, arg2);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Log2(" + arg1.ToString() + ", " + arg2.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Log2(" + arg1.ToString() + ", " + arg2.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail2("Log2", arg1, arg2, exp, res);
        }

        private static void TestPow(double arg1, double arg2, double exp)
        {
            double res = Math.Pow(arg1, arg2);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Pow(" + arg1.ToString() + ", " + arg2.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Pow(" + arg1.ToString() + ", " + arg2.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail2("Pow", arg1, arg2, exp, res);
        }

        private static void TestAtan2(double arg1, double arg2, double exp)
        {
            double res = Math.Atan2(arg1, arg2);
            if (Double.IsNaN(exp) && Double.IsNaN(res) ||
                Double.IsNegativeInfinity(exp) && Double.IsNegativeInfinity(res) ||
                Double.IsPositiveInfinity(exp) && Double.IsPositiveInfinity(res))
            {
                Console.WriteLine(
                    "Atan2(" + arg1.ToString() + ", " + arg2.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            if (exp == res)
            {
                Console.WriteLine(
                    "Atan2(" + arg1.ToString() + ", " + arg2.ToString() + ") == " + res.ToString() + "  OK");

                return;
            }
            Fail2("Atan2", arg1, arg2, exp, res);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                TestAbs(Double.NaN, Double.NaN);
                TestAbs(Double.NegativeInfinity, Double.PositiveInfinity);
                TestAbs(Double.PositiveInfinity, Double.PositiveInfinity);

                TestAcos(Double.NaN, Double.NaN);
                TestAcos(Double.NegativeInfinity, Double.NaN);
                TestAcos(Double.PositiveInfinity, Double.NaN);

                TestAsin(Double.NaN, Double.NaN);
                TestAsin(Double.NegativeInfinity, Double.NaN);
                TestAsin(Double.PositiveInfinity, Double.NaN);

                TestAtan(Double.NaN, Double.NaN);
                TestAtan(Double.NegativeInfinity, -Math.PI / 2);
                TestAtan(Double.PositiveInfinity, Math.PI / 2);

                TestCeiling(Double.NaN, Double.NaN);
                TestCeiling(Double.NegativeInfinity, Double.NegativeInfinity);
                TestCeiling(Double.PositiveInfinity, Double.PositiveInfinity);

                TestCos(Double.NaN, Double.NaN);
                TestCos(Double.NegativeInfinity, Double.NaN);
                TestCos(Double.PositiveInfinity, Double.NaN);

                TestCosh(Double.NaN, Double.NaN);
                TestCosh(Double.NegativeInfinity, Double.PositiveInfinity);
                TestCosh(Double.PositiveInfinity, Double.PositiveInfinity);

                TestExp(Double.NaN, Double.NaN);
                TestExp(Double.NegativeInfinity, 0.0);
                TestExp(Double.PositiveInfinity, Double.PositiveInfinity);

                TestFloor(Double.NaN, Double.NaN);
                TestFloor(Double.NegativeInfinity, Double.NegativeInfinity);
                TestFloor(Double.PositiveInfinity, Double.PositiveInfinity);

                TestLog(Double.NaN, Double.NaN);
                TestLog(Double.NegativeInfinity, Double.NaN);
                TestLog(Double.PositiveInfinity, Double.PositiveInfinity);

                TestLog10(Double.NaN, Double.NaN);
                TestLog10(Double.NegativeInfinity, Double.NaN);
                TestLog10(Double.PositiveInfinity, Double.PositiveInfinity);

                TestRound(Double.NaN, Double.NaN);
                TestRound(Double.NegativeInfinity, Double.NegativeInfinity);
                TestRound(Double.PositiveInfinity, Double.PositiveInfinity);

                TestSign(Double.NegativeInfinity, -1);
                TestSign(Double.PositiveInfinity, 1);

                TestSin(Double.NaN, Double.NaN);
                TestSin(Double.NegativeInfinity, Double.NaN);
                TestSin(Double.PositiveInfinity, Double.NaN);

                TestSinh(Double.NaN, Double.NaN);
                TestSinh(Double.NegativeInfinity, Double.NegativeInfinity);
                TestSinh(Double.PositiveInfinity, Double.PositiveInfinity);

                TestSqrt(Double.NaN, Double.NaN);
                TestSqrt(Double.NegativeInfinity, Double.NaN);
                TestSqrt(Double.PositiveInfinity, Double.PositiveInfinity);

                TestTan(Double.NaN, Double.NaN);
                TestTan(Double.NegativeInfinity, Double.NaN);
                TestTan(Double.PositiveInfinity, Double.NaN);

                TestTanh(Double.NaN, Double.NaN);
                TestTanh(Double.NegativeInfinity, -1);
                TestTanh(Double.PositiveInfinity, 1);

                TestLog2(Double.NaN, Double.NaN, Double.NaN);
                TestLog2(Double.NaN, Double.PositiveInfinity, Double.NaN);
                TestLog2(Double.NaN, Double.NegativeInfinity, Double.NaN);
                TestLog2(Double.PositiveInfinity, Double.NaN, Double.NaN);
                TestLog2(Double.PositiveInfinity, Double.PositiveInfinity, Double.NaN);
                TestLog2(Double.PositiveInfinity, Double.NegativeInfinity, Double.NaN);
                TestLog2(Double.NegativeInfinity, Double.NaN, Double.NaN);
                TestLog2(Double.NegativeInfinity, Double.PositiveInfinity, Double.NaN);
                TestLog2(Double.NegativeInfinity, Double.NegativeInfinity, Double.NaN);

                TestPow(Double.NaN, Double.NaN, Double.NaN);
                TestPow(Double.NaN, Double.PositiveInfinity, Double.NaN);
                TestPow(Double.NaN, Double.NegativeInfinity, Double.NaN);
                TestPow(Double.PositiveInfinity, Double.NaN, Double.NaN);
                TestPow(Double.PositiveInfinity, Double.PositiveInfinity, Double.PositiveInfinity);
                TestPow(Double.PositiveInfinity, Double.NegativeInfinity, 0.0);
                TestPow(Double.NegativeInfinity, Double.NaN, Double.NaN);
                TestPow(Double.NegativeInfinity, Double.PositiveInfinity, Double.PositiveInfinity);
                TestPow(Double.NegativeInfinity, Double.NegativeInfinity, 0.0);

                TestAtan2(Double.NaN, Double.NaN, Double.NaN);
                TestAtan2(Double.NaN, Double.PositiveInfinity, Double.NaN);
                TestAtan2(Double.NaN, Double.NegativeInfinity, Double.NaN);
                TestAtan2(Double.PositiveInfinity, Double.NaN, Double.NaN);
                TestAtan2(Double.PositiveInfinity, Double.PositiveInfinity, Math.PI / 4);
                TestAtan2(Double.PositiveInfinity, Double.NegativeInfinity, 3 * Math.PI / 4);
                TestAtan2(Double.NegativeInfinity, Double.NaN, Double.NaN);
                TestAtan2(Double.NegativeInfinity, Double.PositiveInfinity, -Math.PI / 4);
                TestAtan2(Double.NegativeInfinity, Double.NegativeInfinity, -3 * Math.PI / 4);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("=== FAILED ===");
                return 101;
            }
            Console.WriteLine("=== PASSED ===");
            return 100;
        }
    }
}
}
