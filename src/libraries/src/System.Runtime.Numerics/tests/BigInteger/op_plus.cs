// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public class op_plusTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunPlusTests()
        {
            long temp;
            byte[] tempByteArray1 = new byte[0];

            // Plus Method - Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                VerifyPlusString(Print(tempByteArray1) + "u+");
            }

            // Plus Method - Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                VerifyPlusString(Print(tempByteArray1) + "u+");
            }

            // Plus Method - zero
            VerifyPlusString("0 u+");

            // Plus Method - -1
            VerifyPlusString("-1 u+");

            // Plus Method - 1
            VerifyPlusString("1 u+");

            temp = Int32.MinValue;
            // Plus Method - Int32.MinValue
            VerifyPlusString(temp.ToString() + " u+");

            // Plus Method - Int32.MinValue-1
            VerifyPlusString(temp.ToString() + " -1 b+ u+");

            // Plus Method - Int32.MinValue+1
            VerifyPlusString(temp.ToString() + " 1 b+ u+");

            temp = Int32.MaxValue;
            // Plus Method - Int32.MaxValue
            VerifyPlusString(temp.ToString() + " u+");

            // Plus Method - Int32.MaxValue-1
            VerifyPlusString(temp.ToString() + " -1 b+ u+");

            // Plus Method - Int32.MaxValue+1
            VerifyPlusString(temp.ToString() + " 1 b+ u+");

            temp = Int64.MinValue;
            // Plus Method - Int64.MinValue
            VerifyPlusString(temp.ToString() + " u+");

            // Plus Method - Int64.MinValue-1
            VerifyPlusString(temp.ToString() + " -1 b+ u+");

            // Plus Method - Int64.MinValue+1
            VerifyPlusString(temp.ToString() + " 1 b+ u+");

            temp = Int64.MaxValue;
            // Plus Method - Int64.MaxValue
            VerifyPlusString(temp.ToString() + " u+");

            // Plus Method - Int64.MaxValue-1
            VerifyPlusString(temp.ToString() + " -1 b+ u+");

            // Plus Method - Int64.MaxValue+1
            VerifyPlusString(temp.ToString() + " 1 b+ u+");
        }

        private static void VerifyPlusString(string opstring)
        {
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                Assert.Equal(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString());
            }
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
