// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class op_notTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunNotTests()
        {
            byte[] tempByteArray1 = new byte[0];

            // Not Method - Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                VerifyNotString(Print(tempByteArray1) + "u~");
            }

            // Not Method - Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                VerifyNotString(Print(tempByteArray1) + "u~");
            }

            // Not Method - zero
            VerifyNotString("0 u~");

            // Not Method - -1
            VerifyNotString("-1 u~");

            // Not Method - 1
            VerifyNotString("1 u~");

            // Not Method - Int32.MinValue
            VerifyNotString(Int32.MinValue.ToString() + " u~");

            // Not Method - Int32.MinValue-1
            VerifyNotString(Int32.MinValue.ToString() + " -1 b+ u~");

            // Not Method - Int32.MinValue+1
            VerifyNotString(Int32.MinValue.ToString() + " 1 b+ u~");

            // Not Method - Int32.MaxValue
            VerifyNotString(Int32.MaxValue.ToString() + " u~");

            // Not Method - Int32.MaxValue-1
            VerifyNotString(Int32.MaxValue.ToString() + " -1 b+ u~");

            // Not Method - Int32.MaxValue+1
            VerifyNotString(Int32.MaxValue.ToString() + " 1 b+ u~");

            // Not Method - Int64.MinValue
            VerifyNotString(Int64.MinValue.ToString() + " u~");

            // Not Method - Int64.MinValue-1
            VerifyNotString(Int64.MinValue.ToString() + " -1 b+ u~");

            // Not Method - Int64.MinValue+1
            VerifyNotString(Int64.MinValue.ToString() + " 1 b+ u~");

            // Not Method - Int64.MaxValue
            VerifyNotString(Int64.MaxValue.ToString() + " u~");

            // Not Method - Int64.MaxValue-1
            VerifyNotString(Int64.MaxValue.ToString() + " -1 b+ u~");

            // Not Method - Int64.MaxValue+1
            VerifyNotString(Int64.MaxValue.ToString() + " 1 b+ u~");
        }

        private static void VerifyNotString(string opstring)
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
