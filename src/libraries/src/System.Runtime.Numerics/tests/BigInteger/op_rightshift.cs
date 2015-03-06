// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public class op_rightshiftTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void BigShiftsTest()
        {
            BigInteger a = new BigInteger(1);
            BigInteger b = new BigInteger(Math.Pow(2, 31));

            for (int i = 0; i < 100; i++)
            {
                BigInteger a1 = (a << (i + 31));
                BigInteger a2 = a1 >> i;

                if (a2 != b)
                {
                    Console.WriteLine("Unexpected Results for i={0}: Expected: {1} Got: {2}", i, b.ToString("X"), a2.ToString("X"));
                    Console.WriteLine("Intermediate: {0}", a1.ToString("X"));
                    Assert.True(false, "Incorrect verification");
                }
            }
        }

        [Fact]
        public static void RunRightShiftTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // RightShift Method - Large BigIntegers - large + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomPosByteArray(s_random, 2);
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }

            // RightShift Method - Large BigIntegers - small + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)s_random.Next(1, 32) };
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }

            // RightShift Method - Large BigIntegers - 32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)32 };
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }
            // RightShift Method - Large BigIntegers - large - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomNegByteArray(s_random, 2);
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }

            // RightShift Method - Large BigIntegers - small - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)s_random.Next(-31, 0) };
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }

            // RightShift Method - Large BigIntegers - -32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)0xe0 };
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }

            // RightShift Method - Large BigIntegers - 0 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)0 };
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }

            // RightShift Method - Small BigIntegers - large + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomPosByteArray(s_random, 2);
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }

            // RightShift Method - Small BigIntegers - small + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)s_random.Next(1, 32) };

                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }

            // RightShift Method - Small BigIntegers - 32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)32 };
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }
            // RightShift Method - Small BigIntegers - large - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomNegByteArray(s_random, 2);
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }

            // RightShift Method - Small BigIntegers - small - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)s_random.Next(-31, 0) };
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }

            // RightShift Method - Small BigIntegers - -32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)0xe0 };
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }

            // RightShift Method - Small BigIntegers - 0 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)0 };
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }

            // RightShift Method - Positive BigIntegers - Shift to 0
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomPosByteArray(s_random, 100);
                tempByteArray2 = BitConverter.GetBytes(s_random.Next(8 * tempByteArray1.Length, 1000));
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }

            // RightShift Method - Negative BigIntegers - Shift to -1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomNegByteArray(s_random, 100);
                tempByteArray2 = BitConverter.GetBytes(s_random.Next(8 * tempByteArray1.Length, 1000));
                Assert.True(VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>"), " Verification Failed");
            }
        }

        private static bool VerifyRightShiftString(string opstring)
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

            ret &= Eval(sc1.snCalc.Peek().ToString(), sc2.snCalc.Peek().ToString(), String.Format("Out of Sync stacks found.  BigInteger1: {0} BigInteger2: {1}", sc1.snCalc.Peek(), sc2.snCalc.Peek()));

            return ret;
        }

        private static Byte[] GetRandomByteArray(Random random)
        {
            return GetRandomByteArray(random, random.Next(0, 1024));
        }
        private static Byte[] GetRandomByteArray(Random random, int size)
        {
            byte[] value = new byte[size];

            for (int i = 0; i < value.Length; ++i)
            {
                value[i] = (byte)random.Next(0, 256);
            }

            return value;
        }
        private static Byte[] GetRandomPosByteArray(Random random, int size)
        {
            byte[] value = new byte[size];

            for (int i = 0; i < value.Length; ++i)
            {
                value[i] = (byte)random.Next(0, 256);
            }
            value[value.Length - 1] &= 0x7F;

            return value;
        }
        private static Byte[] GetRandomNegByteArray(Random random, int size)
        {
            byte[] value = new byte[size];

            for (int i = 0; i < value.Length; ++i)
            {
                value[i] = (byte)random.Next(0, 256);
            }
            value[value.Length - 1] |= 0x80;

            return value;
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
