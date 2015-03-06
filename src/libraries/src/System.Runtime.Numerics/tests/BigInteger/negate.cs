// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public class negateTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunMinusTests()
        {
            long temp;
            byte[] tempByteArray1 = new byte[0];

            // Negate Method - Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                Assert.True(VerifyMinusString(Print(tempByteArray1) + "uNegate"), " Verification Failed");
            }

            // Negate Method - Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                Assert.True(VerifyMinusString(Print(tempByteArray1) + "uNegate"), " Verification Failed");
            }

            // Negate Method - zero
            Assert.True(VerifyMinusString("0 uNegate"), " Verification Failed");

            // Negate Method - -1
            Assert.True(VerifyMinusString("-1 uNegate"), " Verification Failed");

            // Negate Method - 1
            Assert.True(VerifyMinusString("1 uNegate"), " Verification Failed");

            temp = Int32.MinValue;
            // Negate Method - Int32.MinValue
            Assert.True(VerifyMinusString(temp.ToString() + " uNegate"), " Verification Failed");

            // Negate Method - Int32.MinValue-1
            Assert.True(VerifyMinusString(temp.ToString() + " -1 b+ uNegate"), " Verification Failed");

            // Negate Method - Int32.MinValue+1
            Assert.True(VerifyMinusString(temp.ToString() + " 1 b+ uNegate"), " Verification Failed");

            temp = Int32.MaxValue;
            // Negate Method - Int32.MaxValue
            Assert.True(VerifyMinusString(temp.ToString() + " uNegate"), " Verification Failed");

            // Negate Method - Int32.MaxValue-1
            Assert.True(VerifyMinusString(temp.ToString() + " -1 b+ uNegate"), " Verification Failed");

            // Negate Method - Int32.MaxValue+1
            Assert.True(VerifyMinusString(temp.ToString() + " 1 b+ uNegate"), " Verification Failed");

            temp = Int64.MinValue;
            // Negate Method - Int64.MinValue
            Assert.True(VerifyMinusString(temp.ToString() + " uNegate"), " Verification Failed");

            // Negate Method - Int64.MinValue-1
            Assert.True(VerifyMinusString(temp.ToString() + " -1 b+ uNegate"), " Verification Failed");

            // Negate Method - Int64.MinValue+1
            Assert.True(VerifyMinusString(temp.ToString() + " 1 b+ uNegate"), " Verification Failed");

            temp = Int64.MaxValue;
            // Negate Method - Int64.MaxValue
            Assert.True(VerifyMinusString(temp.ToString() + " uNegate"), " Verification Failed");

            // Negate Method - Int64.MaxValue-1
            Assert.True(VerifyMinusString(temp.ToString() + " -1 b+ uNegate"), " Verification Failed");

            // Negate Method - Int64.MaxValue+1
            Assert.True(VerifyMinusString(temp.ToString() + " 1 b+ uNegate"), " Verification Failed");
        }

        private static bool VerifyMinusString(string opstring)
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
