// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public class op_minusTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunMinusTests()
        {
            byte[] tempByteArray1 = new byte[0];

            // Minus Method - Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                VerifyMinusString(Print(tempByteArray1) + "u-");
            }

            // Minus Method - Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                VerifyMinusString(Print(tempByteArray1) + "u-");
            }

            // Minus Method - zero
            VerifyMinusString("0 u-");

            // Minus Method - -1
            VerifyMinusString("-1 u-");

            // Minus Method - 1
            VerifyMinusString("1 u-");

            // Minus Method - Int32.MinValue
            VerifyMinusString(Int32.MinValue.ToString() + " u-");

            // Minus Method - Int32.MinValue-1
            VerifyMinusString(Int32.MinValue.ToString() + " -1 b+ u-");

            // Minus Method - Int32.MinValue+1
            VerifyMinusString(Int32.MinValue.ToString() + " 1 b+ u-");

            // Minus Method - Int32.MaxValue
            VerifyMinusString(Int32.MaxValue.ToString() + " u-");

            // Minus Method - Int32.MaxValue-1
            VerifyMinusString(Int32.MaxValue.ToString() + " -1 b+ u-");

            // Minus Method - Int32.MaxValue+1
            VerifyMinusString(Int32.MaxValue.ToString() + " 1 b+ u-");

            // Minus Method - Int64.MinValue
            VerifyMinusString(Int64.MinValue.ToString() + " u-");

            // Minus Method - Int64.MinValue-1
            VerifyMinusString(Int64.MinValue.ToString() + " -1 b+ u-");

            // Minus Method - Int64.MinValue+1
            VerifyMinusString(Int64.MinValue.ToString() + " 1 b+ u-");

            // Minus Method - Int64.MaxValue
            VerifyMinusString(Int64.MaxValue.ToString() + " u-");

            // Minus Method - Int64.MaxValue-1
            VerifyMinusString(Int64.MaxValue.ToString() + " -1 b+ u-");

            // Minus Method - Int64.MaxValue+1
            VerifyMinusString(Int64.MaxValue.ToString() + " 1 b+ u-");
        }

        private static void VerifyMinusString(string opstring)
        {
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                Assert.Equal(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString());
            }
        }

        private static byte[] GetRandomByteArray(Random random)
        {
            return GetRandomByteArray(random, random.Next(0, 1024));
        }

        private static byte[] GetRandomByteArray(Random random, int size)
        {
            return MyBigIntImp.GetRandomByteArray(random, size);
        }

        private static String Print(byte[] bytes)
        {
            return MyBigIntImp.Print(bytes);
        }
    }
}
