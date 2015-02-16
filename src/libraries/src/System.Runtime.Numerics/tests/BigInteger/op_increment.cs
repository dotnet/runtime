// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public class op_incrementTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunIncrementTests()
        {
            long temp;
            byte[] tempByteArray1 = new byte[0];

            // Increment Method - Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                Assert.True(VerifyIncrementString(Print(tempByteArray1) + "u++"), " Verification Failed");
            }

            // Increment Method - Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                Assert.True(VerifyIncrementString(Print(tempByteArray1) + "u++"), " Verification Failed");
            }

            // Increment Method - zero
            Assert.True(VerifyIncrementString("0 u++"), " Verification Failed");

            // Increment Method - -1
            Assert.True(VerifyIncrementString("-1 u++"), " Verification Failed");

            // Increment Method - 1
            Assert.True(VerifyIncrementString("1 u++"), " Verification Failed");

            temp = Int32.MinValue;
            // Increment Method - Int32.MinValue
            Assert.True(VerifyIncrementString(temp.ToString() + " u++"), " Verification Failed");

            // Increment Method - Int32.MinValue-1
            Assert.True(VerifyIncrementString(temp.ToString() + " -1 b+ u++"), " Verification Failed");

            // Increment Method - Int32.MinValue+1
            Assert.True(VerifyIncrementString(temp.ToString() + " 1 b+ u++"), " Verification Failed");

            temp = Int32.MaxValue;
            // Increment Method - Int32.MaxValue
            Assert.True(VerifyIncrementString(temp.ToString() + " u++"), " Verification Failed");

            // Increment Method - Int32.MaxValue-1
            Assert.True(VerifyIncrementString(temp.ToString() + " -1 b+ u++"), " Verification Failed");

            // Increment Method - Int32.MaxValue+1
            Assert.True(VerifyIncrementString(temp.ToString() + " 1 b+ u++"), " Verification Failed");

            temp = Int64.MinValue;
            // Increment Method - Int64.MinValue
            Assert.True(VerifyIncrementString(temp.ToString() + " u++"), " Verification Failed");

            // Increment Method - Int64.MinValue-1
            Assert.True(VerifyIncrementString(temp.ToString() + " -1 b+ u++"), " Verification Failed");

            // Increment Method - Int64.MinValue+1
            Assert.True(VerifyIncrementString(temp.ToString() + " 1 b+ u++"), " Verification Failed");

            temp = Int64.MaxValue;
            // Increment Method - Int64.MaxValue
            Assert.True(VerifyIncrementString(temp.ToString() + " u++"), " Verification Failed");

            // Increment Method - Int64.MaxValue-1
            Assert.True(VerifyIncrementString(temp.ToString() + " -1 b+ u++"), " Verification Failed");

            // Increment Method - Int64.MaxValue+1
            Assert.True(VerifyIncrementString(temp.ToString() + " 1 b+ u++"), " Verification Failed");
        }

        private static bool VerifyIncrementString(string opstring)
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
