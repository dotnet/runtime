// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class op_divideTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);
        
        [Fact]
        public static void RunDividePositivenonZero()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Divide Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyDivideString(Print(tempByteArray1) + Print(tempByteArray2) + "b/");
            }

            // Divide Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyDivideString(Print(tempByteArray1) + Print(tempByteArray2) + "b/");
            }

            // Divide Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyDivideString(Print(tempByteArray1) + Print(tempByteArray2) + "b/");

                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyDivideString(Print(tempByteArray1) + Print(tempByteArray2) + "b/");
            }
        }

        [Fact]
        public static void RunDivideNegative()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];
            
            // Divide Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifyDivideString(Print(tempByteArray1) + Print(tempByteArray2) + "b/");

                Assert.Throws<DivideByZeroException>(() =>
                {
                    VerifyDivideString(Print(tempByteArray2) + Print(tempByteArray1) + "b/");
                });
            }

            // Divide Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifyDivideString(Print(tempByteArray1) + Print(tempByteArray2) + "b/");

                Assert.Throws<DivideByZeroException>(() =>
                {
                    VerifyDivideString(Print(tempByteArray2) + Print(tempByteArray1) + "b/");
                });
            }
        }

        [Fact]
        public static void RunDivideAxiomX1()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];
            
            // Axiom: X/1 = X
            VerifyIdentityString(BigInteger.One + " " + int.MaxValue + " b/", Int32.MaxValue.ToString());
            VerifyIdentityString(BigInteger.One + " " + long.MaxValue + " b/", Int64.MaxValue.ToString());

            for (int i = 0; i < s_samples; i++)
            {
                string randBigInt = Print(GetRandomByteArray(s_random));
                VerifyIdentityString(BigInteger.One + " " + randBigInt + "b/", randBigInt.Substring(0, randBigInt.Length - 1));
            }
        }


        [Fact]
        public static void RunDivideAxiomX0()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];
            
            // Axiom: 0/X = 0
            VerifyIdentityString(int.MaxValue + " " + BigInteger.Zero + " b/", BigInteger.Zero.ToString());
            VerifyIdentityString(long.MaxValue + " " + BigInteger.Zero + " b/", BigInteger.Zero.ToString());

            for (int i = 0; i < s_samples; i++)
            {
                string randBigInt = Print(GetRandomByteArray(s_random));
                VerifyIdentityString(randBigInt + BigInteger.Zero + " b/", BigInteger.Zero.ToString());
            }
        }

        [Fact]
        public static void RunBoundary()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];
            
            // Check interesting cases for boundary conditions
            // You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            VerifyDivideString(Math.Pow(2, 32) + " 2 b/");

            // 32 bit boundary  n1=0 n2=1
            VerifyDivideString(Math.Pow(2, 33) + " 2 b/");
        }

        [Fact]
        public static void RunOverflow()
        {
            // these values lead to an "overflow", if dividing digit by digit
            // we need to ensure that this case is being handled accordingly...
            var x = new BigInteger(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0 });
            var y = new BigInteger(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0 });
            var z = new BigInteger(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0 });

            Assert.Equal(z, x / y);
        }

        private static void VerifyDivideString(string opstring)
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
            return GetRandomByteArray(random, random.Next(1, 100));
        }

        private static byte[] GetRandomByteArray(Random random, int size)
        {
            return MyBigIntImp.GetNonZeroRandomByteArray(random, size);
        }
        
        private static string Print(byte[] bytes)
        {
            return MyBigIntImp.Print(bytes);
        }
    }
}
