// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class op_multiplyTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunMultiplyPositive()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Multiply Method - One Large BigInteger
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                VerifyMultiplyString(Print(tempByteArray1) + "u*");
            }

            // Multiply Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");
            }

            // Multiply Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");
            }

            // Multiply Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                try
                {
                    tempByteArray1 = GetRandomByteArray(s_random);
                    tempByteArray2 = GetRandomByteArray(s_random, 2);
                    VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");

                    tempByteArray1 = GetRandomByteArray(s_random, 2);
                    tempByteArray2 = GetRandomByteArray(s_random);
                    VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");
                }
                catch (IndexOutOfRangeException)
                {
                    // TODO: Refactor this
                    Console.WriteLine("Array1: " + Print(tempByteArray1));
                    Console.WriteLine("Array2: " + Print(tempByteArray2));
                    throw;
                }
            }
        }

        [Fact]
        public static void RunMultiplyPositiveWith0()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];
            
            // Multiply Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");
            }

            // Multiply Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");
            }
        }

        [Fact]
        public static void RunMultiplyAxiomXmult1()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Axiom: X*1 = X
            VerifyIdentityString(int.MaxValue + " " + BigInteger.One + " b*", Int32.MaxValue.ToString());
            VerifyIdentityString(long.MaxValue + " " + BigInteger.One + " b*", Int64.MaxValue.ToString());

            for (int i = 0; i < s_samples; i++)
            {
                string randBigInt = Print(GetRandomByteArray(s_random));
                VerifyIdentityString(randBigInt + BigInteger.One + " b*", randBigInt + "u+");
            }
        }

        [Fact]
        public static void RunMultiplyAxiomXmult0()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];


            // Axiom: X*0 = 0
            VerifyIdentityString(int.MaxValue + " " + BigInteger.Zero + " b*", BigInteger.Zero.ToString());
            VerifyIdentityString(long.MaxValue + " " + BigInteger.Zero + " b*", BigInteger.Zero.ToString());

            for (int i = 0; i < s_samples; i++)
            {
                string randBigInt = Print(GetRandomByteArray(s_random));
                VerifyIdentityString(randBigInt + BigInteger.Zero + " b*", BigInteger.Zero.ToString());
            }
        }

        [Fact]
        public static void RunMultiplyAxiomComm()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Check interesting cases for boundary conditions
            // You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            VerifyMultiplyString(Math.Pow(2, 32) + " 2 b*");

            // 32 bit boundary  n1=0 n2=1
            VerifyMultiplyString(Math.Pow(2, 33) + " 2 b*");
        }

        [Fact]
        public static void RunMultiplyBoundary()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Check interesting cases for boundary conditions
            // You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            VerifyMultiplyString(Math.Pow(2, 32) + " 2 b*");

            // 32 bit boundary  n1=0 n2=1
            VerifyMultiplyString(Math.Pow(2, 33) + " 2 b*");
        }

        [Fact]
        public static void RunMultiplyTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Multiply Method - One Large BigInteger
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                VerifyMultiplyString(Print(tempByteArray1) + "u*");
            }

            // Multiply Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");
            }

            // Multiply Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");
            }

            // Multiply Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                try
                {
                    tempByteArray1 = GetRandomByteArray(s_random);
                    tempByteArray2 = GetRandomByteArray(s_random, 2);
                    VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");

                    tempByteArray1 = GetRandomByteArray(s_random, 2);
                    tempByteArray2 = GetRandomByteArray(s_random);
                    VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");
                }
                catch (IndexOutOfRangeException)
                {
                    // TODO: Refactor this
                    Console.WriteLine("Array1: " + Print(tempByteArray1));
                    Console.WriteLine("Array2: " + Print(tempByteArray2));
                    throw;
                }
            }

            // Multiply Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");
            }

            // Multiply Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "b*");
            }

            // Axiom: X*1 = X
            VerifyIdentityString(int.MaxValue + " " + BigInteger.One + " b*", Int32.MaxValue.ToString());
            VerifyIdentityString(long.MaxValue + " " + BigInteger.One + " b*", Int64.MaxValue.ToString());

            for (int i = 0; i < s_samples; i++)
            {
                string randBigInt = Print(GetRandomByteArray(s_random));
                VerifyIdentityString(randBigInt + BigInteger.One + " b*", randBigInt + "u+");
            }

            // Axiom: X*0 = 0
            VerifyIdentityString(int.MaxValue + " " + BigInteger.Zero + " b*", BigInteger.Zero.ToString());
            VerifyIdentityString(long.MaxValue + " " + BigInteger.Zero + " b*", BigInteger.Zero.ToString());

            for (int i = 0; i < s_samples; i++)
            {
                string randBigInt = Print(GetRandomByteArray(s_random));
                VerifyIdentityString(randBigInt + BigInteger.Zero + " b*", BigInteger.Zero.ToString());
            }

            // Axiom: a*b = b*a
            VerifyIdentityString(int.MaxValue + " " + long.MaxValue + " b*", long.MaxValue + " " + int.MaxValue + " b*");

            for (int i = 0; i < s_samples; i++)
            {
                string randBigInt1 = Print(GetRandomByteArray(s_random));
                string randBigInt2 = Print(GetRandomByteArray(s_random));
                VerifyIdentityString(randBigInt1 + randBigInt2 + "b*", randBigInt2 + randBigInt1 + "b*");
            }
            
            // Check interesting cases for boundary conditions
            // You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            VerifyMultiplyString(Math.Pow(2, 32) + " 2 b*");

            // 32 bit boundary  n1=0 n2=1
            VerifyMultiplyString(Math.Pow(2, 33) + " 2 b*");
        }

        private static void VerifyMultiplyString(string opstring)
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
            return GetRandomByteArray(random, random.Next(0, 100));
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
