// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class log10Test
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunLogTests()
        {
            byte[] tempByteArray1 = new byte[0];

            // Log Method - Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                VerifyLogString(Print(tempByteArray1) + "uLog10");
            }

            // Log Method - Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                VerifyLogString(Print(tempByteArray1) + "uLog10");
            }

            // Log Method - zero
            VerifyLogString("0 uLog10");

            // Log Method - -1
            VerifyLogString("-1 uLog10");

            // Log Method - 1
            VerifyLogString("1 uLog10");

            // Log Method - Int32.MinValue
            VerifyLogString(Int32.MinValue.ToString() + " uLog10");

            // Log Method - Int32.MinValue-1
            VerifyLogString(Int32.MinValue.ToString() + " -1 b+ uLog10");

            // Log Method - Int32.MinValue+1
            VerifyLogString(Int32.MinValue.ToString() + " 1 b+ uLog10");

            // Log Method - Int32.MaxValue
            VerifyLogString(Int32.MaxValue.ToString() + " uLog10");

            // Log Method - Int32.MaxValue-1
            VerifyLogString(Int32.MaxValue.ToString() + " -1 b+ uLog10");

            // Log Method - Int32.MaxValue+1
            VerifyLogString(Int32.MaxValue.ToString() + " 1 b+ uLog10");

            // Log Method - Int64.MinValue
            VerifyLogString(Int64.MinValue.ToString() + " uLog10");

            // Log Method - Int64.MinValue-1
            VerifyLogString(Int64.MinValue.ToString() + " -1 b+ uLog10");

            // Log Method - Int64.MinValue+1
            VerifyLogString(Int64.MinValue.ToString() + " 1 b+ uLog10");

            // Log Method - Int64.MaxValue
            VerifyLogString(Int64.MaxValue.ToString() + " uLog10");

            // Log Method - Int64.MaxValue-1
            VerifyLogString(Int64.MaxValue.ToString() + " -1 b+ uLog10");

            // Log Method - Int64.MaxValue+1
            VerifyLogString(Int64.MaxValue.ToString() + " 1 b+ uLog10");
        }

        private static void VerifyLogString(string opstring)
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
