// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class op_xorTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunXorTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Xor Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");
            }

            // Xor Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");
            }

            // Xor Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");

                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");
            }

            // Xor Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");
            }

            // Xor Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");
            }

            // Xor Method - One large BigIntegers and -1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0xFF };
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");

                tempByteArray1 = new byte[] { 0xFF };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");
            }

            // Xor Method - One small BigIntegers and -1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0xFF };
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");

                tempByteArray1 = new byte[] { 0xFF };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");
            }

            // Xor Method - One large BigIntegers and Int.MaxValue+1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0x00, 0x00, 0x00, 0x80, 0x00 };
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");

                tempByteArray1 = new byte[] { 0x00, 0x00, 0x00, 0x80, 0x00 };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");
            }

            // Xor Method - One small BigIntegers and Int.MaxValue+1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0x00, 0x00, 0x00, 0x80, 0x00 };
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");

                tempByteArray1 = new byte[] { 0x00, 0x00, 0x00, 0x80, 0x00 };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyXorString(Print(tempByteArray1) + Print(tempByteArray2) + "b^");
            }
        }

        private static void VerifyXorString(string opstring)
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
