// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public class minTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunMinTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Min Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");
            }

            // Min Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");
            }

            // Min Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");

                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");
            }

            // Min Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");
            }

            // Min Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");
            }


            // Min Method - One large BigIntegers and -1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0xFF };
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");

                tempByteArray1 = new byte[] { 0xFF };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");
            }

            // Min Method - One small BigIntegers and -1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0xFF };
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");

                tempByteArray1 = new byte[] { 0xFF };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");
            }


            // Min Method - One large BigIntegers and 1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 1 };
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");

                tempByteArray1 = new byte[] { 1 };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");
            }

            // Min Method - One small BigIntegers and 1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 1 };
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");

                tempByteArray1 = new byte[] { 1 };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyMinString(Print(tempByteArray1) + Print(tempByteArray2) + "bMin");
            }
        }

        private static void VerifyMinString(string opstring)
        {
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                Assert.Equal(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString());
            }
        }

        private static Byte[] GetRandomByteArray(Random random)
        {
            return GetRandomByteArray(random, random.Next(0, 100));
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
