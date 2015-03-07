// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                VerifyMinusString(Print(tempByteArray1) + "uNegate");
            }

            // Negate Method - Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                VerifyMinusString(Print(tempByteArray1) + "uNegate");
            }

            // Negate Method - zero
            VerifyMinusString("0 uNegate");

            // Negate Method - -1
            VerifyMinusString("-1 uNegate");

            // Negate Method - 1
            VerifyMinusString("1 uNegate");

            temp = Int32.MinValue;
            // Negate Method - Int32.MinValue
            VerifyMinusString(temp.ToString() + " uNegate");

            // Negate Method - Int32.MinValue-1
            VerifyMinusString(temp.ToString() + " -1 b+ uNegate");

            // Negate Method - Int32.MinValue+1
            VerifyMinusString(temp.ToString() + " 1 b+ uNegate");

            temp = Int32.MaxValue;
            // Negate Method - Int32.MaxValue
            VerifyMinusString(temp.ToString() + " uNegate");

            // Negate Method - Int32.MaxValue-1
            VerifyMinusString(temp.ToString() + " -1 b+ uNegate");

            // Negate Method - Int32.MaxValue+1
            VerifyMinusString(temp.ToString() + " 1 b+ uNegate");

            temp = Int64.MinValue;
            // Negate Method - Int64.MinValue
            VerifyMinusString(temp.ToString() + " uNegate");

            // Negate Method - Int64.MinValue-1
            VerifyMinusString(temp.ToString() + " -1 b+ uNegate");

            // Negate Method - Int64.MinValue+1
            VerifyMinusString(temp.ToString() + " 1 b+ uNegate");

            temp = Int64.MaxValue;
            // Negate Method - Int64.MaxValue
            VerifyMinusString(temp.ToString() + " uNegate");

            // Negate Method - Int64.MaxValue-1
            VerifyMinusString(temp.ToString() + " -1 b+ uNegate");

            // Negate Method - Int64.MaxValue+1
            VerifyMinusString(temp.ToString() + " 1 b+ uNegate");
        }

        private static void VerifyMinusString(string opstring)
        {
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                Assert.Equal(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString());
            }
        }

        private static void VerifyIdentityString(string opstring1, string opstring2)
        {
            StackCalc sc1 = new StackCalc(opstring1);
            while (sc1.DoNextOperation())
            {
                //Run the full calculation
                sc1.DoNextOperation();
            }

            StackCalc sc2 = new StackCalc(opstring2);
            while (sc2.DoNextOperation())
            {	
                //Run the full calculation
                sc2.DoNextOperation();
            }

            Assert.Equal(sc1.snCalc.Peek().ToString(), sc2.snCalc.Peek().ToString());
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
    }
}
