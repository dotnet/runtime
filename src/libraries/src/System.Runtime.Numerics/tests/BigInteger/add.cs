// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public class addTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunAddTests()
        {
            byte[] byteArray1 = new byte[0];
            byte[] byteArray2 = new byte[0];

            // Add Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                byteArray1 = GetRandomByteArray(s_random);
                byteArray2 = GetRandomByteArray(s_random);
                VerifyAdditionString(Print(byteArray1) + Print(byteArray2) + "bAdd");
            }

            // Add Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                byteArray1 = GetRandomByteArray(s_random, 2);
                byteArray2 = GetRandomByteArray(s_random, 2);
                VerifyAdditionString(Print(byteArray1) + Print(byteArray2) + "bAdd");
            }

            // Add Method - One large and one small BigInteger
            for (int i = 0; i < s_samples; i++)
            {
                try
                {
                    byteArray1 = GetRandomByteArray(s_random);
                    byteArray2 = GetRandomByteArray(s_random, 2);
                    VerifyAdditionString(Print(byteArray1) + Print(byteArray2) + "bAdd");

                    byteArray1 = GetRandomByteArray(s_random, 2);
                    byteArray2 = GetRandomByteArray(s_random);
                    VerifyAdditionString(Print(byteArray1) + Print(byteArray2) + "bAdd");
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine("Array1: " + Print(byteArray1));
                    Console.WriteLine("Array2: " + Print(byteArray2));
                    throw;
                }
            }

            // Add Method - One large BigInteger and zero
            for (int i = 0; i < s_samples; i++)
            {
                byteArray1 = GetRandomByteArray(s_random);
                byteArray2 = new byte[] { 0 };
                VerifyAdditionString(Print(byteArray1) + Print(byteArray2) + "bAdd");

                byteArray1 = new byte[] { 0 };
                byteArray2 = GetRandomByteArray(s_random);
                VerifyAdditionString(Print(byteArray1) + Print(byteArray2) + "bAdd");
            }

            // Add Method - One small BigInteger and zero
            for (int i = 0; i < s_samples; i++)
            {
                byteArray1 = GetRandomByteArray(s_random, 2);
                byteArray2 = new byte[] { 0 };
                VerifyAdditionString(Print(byteArray1) + Print(byteArray2) + "bAdd");

                byteArray1 = new byte[] { 0 };
                byteArray2 = GetRandomByteArray(s_random, 2);
                VerifyAdditionString(Print(byteArray1) + Print(byteArray2) + "bAdd");
            }

            // 32 bit boundary n1=0 n2=0 c=0
            VerifyAdditionString("0 0 bAdd");

            // 32 bit boundary n1=0 n2=0 c=1
            VerifyAdditionString((Math.Pow(2, 31) + Math.Pow(2, 30)) + " " + (Math.Pow(2, 31) + Math.Pow(2, 30)) + " bAdd");

            // 32 bit boundary n1=0 n2=1 c=0
            VerifyAdditionString("0" + " " + Math.Pow(2, 32) + " bAdd");

            // 32 bit boundary n1=0 n2=1 c=1
            VerifyAdditionString(Math.Pow(2, 31) + " " + (Math.Pow(2, 32) + Math.Pow(2, 31)) + " bAdd");

            // 32 bit boundary n1=1 n2=0 c=0
            VerifyAdditionString(Math.Pow(2, 32) + " " + "0" + " bAdd");

            // 32 bit boundary n1=1 n2=0 c=1
            VerifyAdditionString((Math.Pow(2, 32) + Math.Pow(2, 31)) + " " + Math.Pow(2, 31) + " bAdd");

            // 32 bit boundary n1=0 n2=1 c=0
            VerifyAdditionString(Math.Pow(2, 32) + " " + Math.Pow(2, 32) + " bAdd");

            // 32 bit boundary n1=0 n2=1 c=1
            VerifyAdditionString((Math.Pow(2, 32) + Math.Pow(2, 31)) + " " + (Math.Pow(2, 32) + Math.Pow(2, 31)) + " bAdd");

            // Identity (x+y)+z == (y+z)+x
            VerifyIdentityString(
                    Int64.MaxValue.ToString() + " " + Int32.MaxValue.ToString() + " bAdd " + Int16.MaxValue.ToString() + " bAdd",
                    Int32.MaxValue.ToString() + " " + Int16.MaxValue.ToString() + " bAdd " + Int64.MaxValue.ToString() + " bAdd"
            );

            byte[] x = GetRandomByteArray(s_random);
            byte[] y = GetRandomByteArray(s_random);
            byte[] z = GetRandomByteArray(s_random);

            VerifyIdentityString(Print(x) + Print(y) + Print(z) + "bAdd bAdd", Print(y) + Print(z) + Print(x) + "bAdd bAdd");
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
