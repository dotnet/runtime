// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public class op_subtractTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunSubtractTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Subtract Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "b-");
            }

            // Subtract Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "b-");
            }

            // Subtract Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                try
                {
                    tempByteArray1 = GetRandomByteArray(s_random);
                    tempByteArray2 = GetRandomByteArray(s_random, 2);
                    VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "b-");

                    tempByteArray1 = GetRandomByteArray(s_random, 2);
                    tempByteArray2 = GetRandomByteArray(s_random);
                    VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "b-");
                }
                catch (IndexOutOfRangeException)
                {
                    // TODO: Refactor this
                    Console.WriteLine("Array1: " + Print(tempByteArray1));
                    Console.WriteLine("Array2: " + Print(tempByteArray2));
                    throw;
                }
            }

            // Subtract Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "b-");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "b-");
            }

            // Subtract Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "b-");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "b-");
            }

            // 32 bit boundary c=0 n1=0 n2=0
            VerifySubtractString(Math.Pow(2, 33) + " " + Math.Pow(2, 34) + " b-");

            // 32 bit boundary c=0 n1=0 n2=1
            VerifySubtractString(Math.Pow(2, 32) + " " + Math.Pow(2, 34) + " b-");

            // 32 bit boundary c=0 n1=1 n2=0
            VerifySubtractString(Math.Pow(2, 31) + " " + Math.Pow(2, 32) + " b-");

            // 32 bit boundary c=0 n1=1 n2=1
            VerifySubtractString(Math.Pow(2, 32) + " " + Math.Pow(2, 32) + " b-");

            // 32 bit boundary c=1 n1=0 n2=0
            VerifySubtractString(Math.Pow(2, 31) + " " + Math.Pow(2, 33) + " b-");

            // 32 bit boundary c=1 n1=0 n2=1
            VerifySubtractString(Math.Pow(2, 33) + " " + Math.Pow(2, 32) + " b-");

            // 32 bit boundary c=1 n1=1 n2=0
            VerifySubtractString(Math.Pow(2, 31) + " " + (Math.Pow(2, 32) + Math.Pow(2, 33)) + " b-");

            // 32 bit boundary c=1 n1=1 n2=1
            VerifySubtractString((Math.Pow(2, 33) + Math.Pow(2, 32) + Math.Pow(2, 31)) + " " + (Math.Pow(2, 34) + Math.Pow(2, 33) + Math.Pow(2, 31)) + " b-");
        }

        private static void VerifySubtractString(string opstring)
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
