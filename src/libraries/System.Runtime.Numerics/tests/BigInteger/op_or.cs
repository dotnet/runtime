// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class op_orTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunOrTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Or Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");
            }

            // Or Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");
            }

            // Or Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");

                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");
            }

            // Or Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");
            }

            // Or Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");
            }

            // Or Method - One large BigIntegers and -1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0xFF };
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");

                tempByteArray1 = new byte[] { 0xFF };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");
            }

            // Or Method - One small BigIntegers and -1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0xFF };
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");

                tempByteArray1 = new byte[] { 0xFF };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");
            }

            // Or Method - One large BigIntegers and Int.MaxValue+1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0x00, 0x00, 0x00, 0x80, 0x00 };
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");

                tempByteArray1 = new byte[] { 0x00, 0x00, 0x00, 0x80, 0x00 };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");
            }

            // Or Method - One small BigIntegers and Int.MaxValue+1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0x00, 0x00, 0x00, 0x80, 0x00 };
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");

                tempByteArray1 = new byte[] { 0x00, 0x00, 0x00, 0x80, 0x00 };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyOrString(Print(tempByteArray1) + Print(tempByteArray2) + "b|");
            }
        }

        private static void VerifyOrString(string opstring)
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
