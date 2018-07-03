// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            byte[] tempByteArray1 = new byte[0];

            // Increment Method - Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                VerifyIncrementString(Print(tempByteArray1) + "u++");
            }

            // Increment Method - Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                VerifyIncrementString(Print(tempByteArray1) + "u++");
            }

            // Increment Method - zero
            VerifyIncrementString("0 u++");

            // Increment Method - -1
            VerifyIncrementString("-1 u++");

            // Increment Method - 1
            VerifyIncrementString("1 u++");

            // Increment Method - Int32.MinValue
            VerifyIncrementString(Int32.MinValue.ToString() + " u++");

            // Increment Method - Int32.MinValue-1
            VerifyIncrementString(Int32.MinValue.ToString() + " -1 b+ u++");

            // Increment Method - Int32.MinValue+1
            VerifyIncrementString(Int32.MinValue.ToString() + " 1 b+ u++");

            // Increment Method - Int32.MaxValue
            VerifyIncrementString(Int32.MaxValue.ToString() + " u++");

            // Increment Method - Int32.MaxValue-1
            VerifyIncrementString(Int32.MaxValue.ToString() + " -1 b+ u++");

            // Increment Method - Int32.MaxValue+1
            VerifyIncrementString(Int32.MaxValue.ToString() + " 1 b+ u++");

            // Increment Method - Int64.MinValue
            VerifyIncrementString(Int64.MinValue.ToString() + " u++");

            // Increment Method - Int64.MinValue-1
            VerifyIncrementString(Int64.MinValue.ToString() + " -1 b+ u++");

            // Increment Method - Int64.MinValue+1
            VerifyIncrementString(Int64.MinValue.ToString() + " 1 b+ u++");

            // Increment Method - Int64.MaxValue
            VerifyIncrementString(Int64.MaxValue.ToString() + " u++");

            // Increment Method - Int64.MaxValue-1
            VerifyIncrementString(Int64.MaxValue.ToString() + " -1 b+ u++");

            // Increment Method - Int64.MaxValue+1
            VerifyIncrementString(Int64.MaxValue.ToString() + " 1 b+ u++");
        }

        private static void VerifyIncrementString(string opstring)
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

        private static string Print(byte[] bytes)
        {
            return MyBigIntImp.Print(bytes);
        }
    }
}
