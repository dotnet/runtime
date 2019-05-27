// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class powTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunPowPositive()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Pow Method - 0^(1)
            VerifyPowString(BigInteger.One.ToString() + " " + BigInteger.Zero.ToString() + " bPow");

            // Pow Method - 0^(0)
            VerifyPowString(BigInteger.Zero.ToString() + " " + BigInteger.Zero.ToString() + " bPow");

            // Pow Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomPosByteArray(s_random, 1);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyPowString(Print(tempByteArray1) + Print(tempByteArray2) + "bPow");
            }

            // Pow Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomPosByteArray(s_random, 1);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyPowString(Print(tempByteArray1) + Print(tempByteArray2) + "bPow");
            }

            // Pow Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifyPowString(Print(tempByteArray2) + Print(tempByteArray1) + "bPow");
            }

            // Pow Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomPosByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifyPowString(Print(tempByteArray1) + Print(tempByteArray2) + "bPow");
                VerifyPowString(Print(tempByteArray2) + Print(tempByteArray1) + "bPow");
            }
        }

        [Fact]
        public static void RunPowAxiomXPow1()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Axiom: X^1 = X
            VerifyIdentityString(BigInteger.One + " " + int.MaxValue + " bPow", Int32.MaxValue.ToString());
            VerifyIdentityString(BigInteger.One + " " + long.MaxValue + " bPow", Int64.MaxValue.ToString());

            for (int i = 0; i < s_samples; i++)
            {
                string randBigInt = Print(GetRandomByteArray(s_random));
                VerifyIdentityString(BigInteger.One + " " + randBigInt + "bPow", randBigInt.Substring(0, randBigInt.Length - 1));
            }
        }

        [Fact]
        public static void RunPowAxiomXPow0()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Axiom: X^0 = 1
            VerifyIdentityString(BigInteger.Zero + " " + int.MaxValue + " bPow", BigInteger.One.ToString());
            VerifyIdentityString(BigInteger.Zero + " " + long.MaxValue + " bPow", BigInteger.One.ToString());

            for (int i = 0; i < s_samples; i++)
            {
                string randBigInt = Print(GetRandomByteArray(s_random));
                VerifyIdentityString(BigInteger.Zero + " " + randBigInt + "bPow", BigInteger.One.ToString());
            }
        }

        [Fact]
        public static void RunPowAxiom0PowX()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Axiom: 0^X = 0
            VerifyIdentityString(int.MaxValue + " " + BigInteger.Zero + " bPow", BigInteger.Zero.ToString());

            for (int i = 0; i < s_samples; i++)
            {
                string randBigInt = Print(GetRandomPosByteArray(s_random, 4));
                VerifyIdentityString(randBigInt + BigInteger.Zero + " bPow", BigInteger.Zero.ToString());
            }
        }

        [Fact]
        public static void RunPowAxiom1PowX()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Axiom: 1^X = 1
            VerifyIdentityString(int.MaxValue + " " + BigInteger.One + " bPow", BigInteger.One.ToString());

            for (int i = 0; i < s_samples; i++)
            {
                string randBigInt = Print(GetRandomPosByteArray(s_random, 4));
                VerifyIdentityString(randBigInt + BigInteger.One + " bPow", BigInteger.One.ToString());
            }
        }

        [Fact]
        public static void RunPowBoundary()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Check interesting cases for boundary conditions
            // You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            VerifyPowString("2 " + Math.Pow(2, 32) + " bPow");

            // 32 bit boundary  n1=0 n2=1
            VerifyPowString("2 " + Math.Pow(2, 33) + " bPow");
        }

        [Fact]
        public static void RunPowNegative()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Pow Method - 1^(-1)
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                VerifyPowString(BigInteger.MinusOne.ToString() + " " + BigInteger.One.ToString() + " bPow");
            });

            // Pow Method - 0^(-1)
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                VerifyPowString(BigInteger.MinusOne.ToString() + " " + BigInteger.Zero.ToString() + " bPow");
            });

            // Pow Method - Negative Exponent
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomNegByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    VerifyPowString(Print(tempByteArray1) + Print(tempByteArray2) + "bPow");
                });
            }
        }

        [Fact]
        public static void RunOverflow()
        {
            var bytes = new byte[1000];
            bytes[bytes.Length - 1] = 1;

            Assert.Throws<OverflowException>(() =>
                BigInteger.Pow(new BigInteger(bytes), int.MaxValue));
        }

        private static void VerifyPowString(string opstring)
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
            return GetRandomByteArray(random, random.Next(0, 10));
        }

        private static byte[] GetRandomByteArray(Random random, int size)
        {
            return MyBigIntImp.GetRandomByteArray(random, size);
        }

        private static byte[] GetRandomPosByteArray(Random random, int size)
        {
            byte[] value = new byte[size];

            for (int i = 0; i < value.Length; ++i)
            {
                value[i] = (byte)random.Next(0, 256);
            }
            value[value.Length - 1] &= 0x7F;

            return value;
        }

        private static byte[] GetRandomNegByteArray(Random random, int size)
        {
            byte[] value = new byte[size];

            for (int i = 0; i < value.Length; ++i)
            {
                value[i] = (byte)random.Next(0, 256);
            }
            value[value.Length - 1] |= 0x80;

            return value;
        }

        private static string Print(byte[] bytes)
        {
            return MyBigIntImp.Print(bytes);
        }
    }
}
