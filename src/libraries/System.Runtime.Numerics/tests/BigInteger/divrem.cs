// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public class divremTest
    {
        private static int s_samples = 10;

        private static Random s_random = new Random(100);

        [Fact]
        public static void RunDivRem_TwoLArgeBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                Assert.True(VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem"), " Verification Failed");
            }
        }

        [Fact]
        public static void RunDivRem_TwoSmallBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                Assert.True(VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem"), " Verification Failed");
            }
        }


        [Fact]
        public static void RunDivRem_OneSmallOneLargeBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                Assert.True(VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem"), " Verification Failed");

                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random);
                Assert.True(VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem"), " Verification Failed");
            }
        }

        [Fact]
        public static void RunDivRem_OneLargeOne0BI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                Assert.True(VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem"), " Verification Failed");

                Assert.Throws<DivideByZeroException>(() =>
                {
                    VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem");
                });
            }
        }

        [Fact]
        public static void RunDivRem_OneSmallOne0BI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                Assert.True(VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem"), " Verification Failed");

                Assert.Throws<DivideByZeroException>(() =>
                {
                    VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem");
                });
            }
        }

        [Fact]
        public static void Boundary()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];


            //Check interesting cases for boundary conditions
            //You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            Assert.True(VerifyDivRemString(Math.Pow(2, 32) + " 2 bDivRem"), " Verification Failed");

            // 32 bit boundary  n1=0 n2=1
            Assert.True(VerifyDivRemString(Math.Pow(2, 33) + " 2 bDivRem"), " Verification Failed");
        }

        public static bool RunDivRemTests(Random random)
        {
            bool ret = true;
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(random);
                tempByteArray2 = GetRandomByteArray(random);
                Assert.True(VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem"), " Verification Failed");
            }

            // DivRem Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(random, 2);
                tempByteArray2 = GetRandomByteArray(random, 2);
                Assert.True(VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem"), " Verification Failed");
            }

            // DivRem Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(random);
                tempByteArray2 = GetRandomByteArray(random, 2);
                Assert.True(VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem"), " Verification Failed");

                tempByteArray1 = GetRandomByteArray(random, 2);
                tempByteArray2 = GetRandomByteArray(random);
                Assert.True(VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem"), " Verification Failed");
            }

            // DivRem Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(random);
                tempByteArray2 = new byte[] { 0 };
                Assert.True(VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem"), " Verification Failed");

                try
                {
                    VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem");
                    ret = false;
                }
                catch (DivideByZeroException)
                {
                    //   caught expected exception.
                }
            }

            // DivRem Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(random, 2);
                tempByteArray2 = new byte[] { 0 };
                Assert.True(VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem"), " Verification Failed");

                try
                {
                    VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem");
                    ret = false;
                }
                catch (DivideByZeroException)
                {
                    //   caught expected exception.
                }
            }


            //Check interesting cases for boundary conditions
            //You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            Assert.True(VerifyDivRemString(Math.Pow(2, 32) + " 2 bDivRem"), " Verification Failed");

            // 32 bit boundary  n1=0 n2=1
            Assert.True(VerifyDivRemString(Math.Pow(2, 33) + " 2 bDivRem"), " Verification Failed");
            return ret;
        }

        private static bool VerifyDivRemString(string opstring)
        {
            bool ret = true;
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                ret &= Eval(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString(), String.Format("Out of Sync stacks found.  BigInteger {0} Mine {1}", sc.snCalc.Peek(), sc.myCalc.Peek()));
                ret &= Eval(sc.VerifyOut(), String.Format("Out parameters not matching"));
            }
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
