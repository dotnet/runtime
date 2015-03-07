// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Tools;
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
