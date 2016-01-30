// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class subtractTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunSubtractTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract");
            }

            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract");
            }

            for (int i = 0; i < s_samples; i++)
            {
                try
                {
                    tempByteArray1 = GetRandomByteArray(s_random);
                    tempByteArray2 = GetRandomByteArray(s_random, 2);
                    VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract");

                    tempByteArray1 = GetRandomByteArray(s_random, 2);
                    tempByteArray2 = GetRandomByteArray(s_random);
                    VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract");
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine("Array1: " + Print(tempByteArray1));
                    Console.WriteLine("Array2: " + Print(tempByteArray2));
                    throw;
                }
            }

            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract");
            }

            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifySubtractString(Print(tempByteArray1) + Print(tempByteArray2) + "bSubtract");
            }

            // 32 bit boundary c=0 n1=0 n2=0
            VerifySubtractString(Math.Pow(2, 33) + " " + Math.Pow(2, 34) + " bSubtract");

            // 32 bit boundary c=0 n1=0 n2=1
            VerifySubtractString(Math.Pow(2, 32) + " " + Math.Pow(2, 34) + " bSubtract");

            // 32 bit boundary c=0 n1=1 n2=0
            VerifySubtractString(Math.Pow(2, 31) + " " + Math.Pow(2, 32) + " bSubtract");

            // 32 bit boundary c=0 n1=1 n2=1
            VerifySubtractString(Math.Pow(2, 32) + " " + Math.Pow(2, 32) + " bSubtract");

            // 32 bit boundary c=1 n1=0 n2=0
            VerifySubtractString(Math.Pow(2, 31) + " " + Math.Pow(2, 33) + " bSubtract");

            // 32 bit boundary c=1 n1=0 n2=1
            VerifySubtractString(Math.Pow(2, 33) + " " + Math.Pow(2, 32) + " bSubtract");

            // 32 bit boundary c=1 n1=1 n2=0
            VerifySubtractString(Math.Pow(2, 31) + " " + (Math.Pow(2, 32) + Math.Pow(2, 33)) + " bSubtract");

            // 32 bit boundary c=1 n1=1 n2=1
            VerifySubtractString((Math.Pow(2, 33) + Math.Pow(2, 32) + Math.Pow(2, 31)) + " " + (Math.Pow(2, 34) + Math.Pow(2, 33) + Math.Pow(2, 31)) + " bSubtract");
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
            return MyBigIntImp.GetNonZeroRandomByteArray(random, size);
        }

        private static String Print(byte[] bytes)
        {
            return MyBigIntImp.Print(bytes);
        }
    }
}
