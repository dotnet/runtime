// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public class op_addTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunAddTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Add Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyAdditionString(Print(tempByteArray1) + Print(tempByteArray2) + "b+");
            }

            // Add Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyAdditionString(Print(tempByteArray1) + Print(tempByteArray2) + "b+");
            }

            // Add Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                try
                {
                    tempByteArray1 = GetRandomByteArray(s_random);
                    tempByteArray2 = GetRandomByteArray(s_random, 2);
                    VerifyAdditionString(Print(tempByteArray1) + Print(tempByteArray2) + "b+");

                    tempByteArray1 = GetRandomByteArray(s_random, 2);
                    tempByteArray2 = GetRandomByteArray(s_random);
                    VerifyAdditionString(Print(tempByteArray1) + Print(tempByteArray2) + "b+");
                }
                catch (IndexOutOfRangeException)
                {
                    // TODO: Refactor this
                    Console.WriteLine("Array1: " + Print(tempByteArray1));
                    Console.WriteLine("Array2: " + Print(tempByteArray2));
                    throw;
                }
            }

            // Add Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifyAdditionString(Print(tempByteArray1) + Print(tempByteArray2) + "b+");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyAdditionString(Print(tempByteArray1) + Print(tempByteArray2) + "b+");
            }

            // Add Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifyAdditionString(Print(tempByteArray1) + Print(tempByteArray2) + "b+");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyAdditionString(Print(tempByteArray1) + Print(tempByteArray2) + "b+");
            }

            // 32 bit boundary n1=0 n2=0 c=0
            VerifyAdditionString("0 0 bAdd");

            // 32 bit boundary n1=0 n2=0 c=1
            VerifyAdditionString((Math.Pow(2, 31) + Math.Pow(2, 30)) + " " + (Math.Pow(2, 31) + Math.Pow(2, 30)) + " b+");

            // 32 bit boundary n1=0 n2=1 c=0
            VerifyAdditionString("0" + " " + Math.Pow(2, 32) + " b+");

            // 32 bit boundary n1=0 n2=1 c=1
            VerifyAdditionString(Math.Pow(2, 31) + " " + (Math.Pow(2, 32) + Math.Pow(2, 31)) + " b+");

            // 32 bit boundary n1=1 n2=0 c=0
            VerifyAdditionString(Math.Pow(2, 32) + " " + "0" + " b+");

            // 32 bit boundary n1=1 n2=0 c=1
            VerifyAdditionString((Math.Pow(2, 32) + Math.Pow(2, 31)) + " " + Math.Pow(2, 31) + " b+");

            // 32 bit boundary n1=0 n2=1 c=0
            VerifyAdditionString(Math.Pow(2, 32) + " " + Math.Pow(2, 32) + " b+");

            // 32 bit boundary n1=0 n2=1 c=1
            VerifyAdditionString((Math.Pow(2, 32) + Math.Pow(2, 31)) + " " + (Math.Pow(2, 32) + Math.Pow(2, 31)) + " b+");

            // Identity (x+y)+z == (y+z)+x
            // Check some identities
            // (x+y)+z = (y+z)+x

            VerifyIdentityString(
                Int64.MaxValue.ToString() + " " + Int32.MaxValue.ToString() + " b+ " + Int16.MaxValue.ToString() + " b+",
                Int32.MaxValue.ToString() + " " + Int16.MaxValue.ToString() + " b+ " + Int64.MaxValue.ToString() + " b+"
            );

            byte[] x = GetRandomByteArray(s_random);
            byte[] y = GetRandomByteArray(s_random);
            byte[] z = GetRandomByteArray(s_random);

            VerifyIdentityString(Print(x) + Print(y) + Print(z) + "b+ b+", Print(y) + Print(z) + Print(x) + "b+ b+");
        }

        private static void VerifyAdditionString(string opstring)
        {
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                Assert.Equal(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString());
            }
        }

        private static void VerifyIdentityString(string opstring1, string opstring2)
        {
            StackCalc sc1 = new StackCalc(opstring1);
            while (sc1.DoNextOperation())
            {
                //Run the full calculation
                sc1.DoNextOperation();
            }

            StackCalc sc2 = new StackCalc(opstring2);
            while (sc2.DoNextOperation())
            {
                //Run the full calculation
                sc2.DoNextOperation();
            }

            Assert.Equal(sc1.snCalc.Peek().ToString(), sc2.snCalc.Peek().ToString());
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
