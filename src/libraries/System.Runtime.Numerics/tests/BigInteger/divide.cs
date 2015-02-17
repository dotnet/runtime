// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public class divideTest
    {
        private static int s_samples = 10;

        private static Random s_random = new Random(100);


        [Fact]
        public static void RunDivideTwoLargeBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Divide Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                Assert.True(VerifyDivideString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivide"), " Verification Failed");
            }
        }

        [Fact]
        public static void RunDivideTwoSmallBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Divide Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                Assert.True(VerifyDivideString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivide"), " Verification Failed");
            }
        }

        [Fact]
        public static void RunDivideOneLargeOneSmallBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Divide Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                Assert.True(VerifyDivideString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivide"), " Verification Failed");

                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random);
                Assert.True(VerifyDivideString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivide"), " Verification Failed");
            }
        }


        [Fact]
        public static void RunDivideOneLargeOneZeroBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Divide Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };

                bool v = VerifyDivideString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivide");
                Assert.True(v, " Verification Failed");

                Assert.Throws<DivideByZeroException>(() =>
                {
                    VerifyDivideString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivide");
                });
            }
        }

        [Fact]
        public static void RunDivideOneSmallOneZeroBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Divide Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                Assert.True(VerifyDivideString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivide"), " Verification Failed");

                Assert.Throws<DivideByZeroException>(() =>
                {
                    VerifyDivideString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivide");
                });
            }
        }

        [Fact]
        public static void RunDivideOneOverOne()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Axiom: X/1 = X
            Assert.True(VerifyIdentityString(BigInteger.One + " " + Int32.MaxValue + " bDivide", Int32.MaxValue.ToString()), " Verification Failed");
            Assert.True(VerifyIdentityString(BigInteger.One + " " + Int64.MaxValue + " bDivide", Int64.MaxValue.ToString()), " Verification Failed");

            for (int i = 0; i < s_samples; i++)
            {
                String randBigInt = Print(GetRandomByteArray(s_random));
                Assert.True(VerifyIdentityString(BigInteger.One + " " + randBigInt + "bDivide", randBigInt.Substring(0, randBigInt.Length - 1)), " Verification Failed");
            }
        }


        [Fact]
        public static void RunDivideZeroOverBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Axiom: 0/X = 0
            Assert.True(VerifyIdentityString(Int32.MaxValue + " " + BigInteger.Zero + " bDivide", BigInteger.Zero.ToString()), " Verification Failed");
            Assert.True(VerifyIdentityString(Int64.MaxValue + " " + BigInteger.Zero + " bDivide", BigInteger.Zero.ToString()), " Verification Failed");

            for (int i = 0; i < s_samples; i++)
            {
                String randBigInt = Print(GetRandomByteArray(s_random));
                Assert.True(VerifyIdentityString(randBigInt + BigInteger.Zero + " bDivide", BigInteger.Zero.ToString()), " Verification Failed");
            }
        }

        [Fact]
        public static void RunDivideBoundary()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            //Check interesting cases for boundary conditions
            //You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            Assert.True(VerifyDivideString(Math.Pow(2, 32) + " 2 bDivide"), " Verification Failed");

            // 32 bit boundary  n1=0 n2=1
            Assert.True(VerifyDivideString(Math.Pow(2, 33) + " 2 bDivide"), " Verification Failed");
        }

        private static bool VerifyDivideString(string opstring)
        {
            bool ret = true;
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                ret &= Eval(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString(), String.Format("Out of Sync stacks found.  BigInteger {0} Mine {1}", sc.snCalc.Peek(), sc.myCalc.Peek()));
            }
            return ret;
        }

        private static bool VerifyIdentityString(string opstring1, string opstring2)
        {
            bool ret = true;

            StackCalc sc1 = new StackCalc(opstring1);
            while (sc1.DoNextOperation())
            {	//Run the full calculation
                sc1.DoNextOperation();
            }

            StackCalc sc2 = new StackCalc(opstring2);
            while (sc2.DoNextOperation())
            {	//Run the full calculation
                sc2.DoNextOperation();
            }

            Eval(sc1.snCalc.Peek().ToString(), sc2.snCalc.Peek().ToString(), String.Format("Out of Sync stacks found.  BigInteger1: {0} BigInteger2: {1}", sc1.snCalc.Peek(), sc2.snCalc.Peek()));

            return ret;
        }

        private static Byte[] GetRandomByteArray(Random random)
        {
            return GetRandomByteArray(random, random.Next(1, 100));
        }
        private static Byte[] GetRandomByteArray(Random random, int size)
        {
            byte[] value = new byte[size];

            while (IsZero(value))
            {
                for (int i = 0; i < value.Length; ++i)
                {
                    value[i] = (byte)random.Next(0, 256);
                }
            }

            return value;
        }
        private static bool IsZero(byte[] value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != 0)
                    return false;
            }
            return true;
        }

        private static String Print(byte[] bytes)
        {
            String ret = "make ";

            for (int i = 0; i < bytes.Length; i++)
            {
                ret += bytes[i] + " ";
            }
            ret += "endmake ";

            return ret;
        }

        public static bool Eval<T>(T expected, T actual, String errorMsg)
        {
            bool retValue = expected == null ? actual == null : expected.Equals(actual);

            if (!retValue)
                return Eval(retValue, errorMsg +
                " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                " Actual:" + (null == actual ? "<null>" : actual.ToString()));

            return true;
        }
        public static bool Eval(bool expression, string message)
        {
            if (!expression)
            {
                Console.WriteLine(message);
            }

            return expression;
        }
    }
}
