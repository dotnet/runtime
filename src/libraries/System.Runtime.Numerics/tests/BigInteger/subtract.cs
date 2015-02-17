// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public class subtractTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunSubtractTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                Assert.True(VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract"), " Verification Failed");
            }

            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                Assert.True(VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract"), " Verification Failed");
            }

            for (int i = 0; i < s_samples; i++)
            {
                try
                {
                    tempByteArray1 = GetRandomByteArray(s_random);
                    tempByteArray2 = GetRandomByteArray(s_random, 2);
                    Assert.True(VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract"), " Verification Failed");

                    tempByteArray1 = GetRandomByteArray(s_random, 2);
                    tempByteArray2 = GetRandomByteArray(s_random);
                    Assert.True(VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract"), " Verification Failed");
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine("Array1: " + Print(tempByteArray1));
                    Console.WriteLine("Array2: " + Print(tempByteArray2));
                    throw;
                }
            }

            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                Assert.True(VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract"), " Verification Failed");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random);
                Assert.True(VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract"), " Verification Failed");
            }

            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                Assert.True(VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract"), " Verification Failed");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                Assert.True(VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract"), " Verification Failed");
            }

            // 32 bit boundary c=0 n1=0 n2=0
            Assert.True(VerifySubtractString(Math.Pow(2, 33) + " " + Math.Pow(2, 34) + " bSubtract"), " Verification Failed");

            // 32 bit boundary c=0 n1=0 n2=1
            Assert.True(VerifySubtractString(Math.Pow(2, 32) + " " + Math.Pow(2, 34) + " bSubtract"), " Verification Failed");

            // 32 bit boundary c=0 n1=1 n2=0
            Assert.True(VerifySubtractString(Math.Pow(2, 31) + " " + Math.Pow(2, 32) + " bSubtract"), " Verification Failed");

            // 32 bit boundary c=0 n1=1 n2=1
            Assert.True(VerifySubtractString(Math.Pow(2, 32) + " " + Math.Pow(2, 32) + " bSubtract"), " Verification Failed");

            // 32 bit boundary c=1 n1=0 n2=0
            Assert.True(VerifySubtractString(Math.Pow(2, 31) + " " + Math.Pow(2, 33) + " bSubtract"), " Verification Failed");

            // 32 bit boundary c=1 n1=0 n2=1
            Assert.True(VerifySubtractString(Math.Pow(2, 33) + " " + Math.Pow(2, 32) + " bSubtract"), " Verification Failed");

            // 32 bit boundary c=1 n1=1 n2=0
            Assert.True(VerifySubtractString(Math.Pow(2, 31) + " " + (Math.Pow(2, 32) + Math.Pow(2, 33)) + " bSubtract"), " Verification Failed");

            // 32 bit boundary c=1 n1=1 n2=1
            Assert.True(VerifySubtractString((Math.Pow(2, 33) + Math.Pow(2, 32) + Math.Pow(2, 31)) + " " + (Math.Pow(2, 34) + Math.Pow(2, 33) + Math.Pow(2, 31)) + " bSubtract"), " Verification Failed");
        }

        private static bool VerifySubtractString(string opstring)
        {
            bool ret = true;
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                ret &= Eval(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString(), String.Format("Out of Sync stacks found.  BigInteger {0} Mine {1}", sc.snCalc.Peek(), sc.myCalc.Peek()));
            }
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
