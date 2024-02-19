// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Tests
{
    public class divremTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunDivRem_TwoLargeBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");
            }
        }

        [Fact]
        public static void RunDivRem_TwoSmallBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");
            }
        }

        [Fact]
        public static void RunDivRem_OneSmallOneLargeBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - One Large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");

                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");
            }
        }

        [Fact]
        public static void RunDivRem_OneLargeOneHalfBI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Divide Method - Two Large BigIntegers
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    tempByteArray1 = GetRandomByteArray(s_random, 512 + i);
                    tempByteArray2 = GetRandomByteArray(s_random, 256 + j);
                    VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");
                }
        }

        [Fact]
        public static void RunDivRem_OneLargeOne0BI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - One Large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");

                Assert.Throws<DivideByZeroException>(() =>
                {
                    VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem");
                });
            }
        }

        [Fact]
        public static void RunDivRem_OneSmallOne0BI()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");

                Assert.Throws<DivideByZeroException>(() =>
                {
                    VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem");
                });
            }
        }

        [Fact]
        public static void Boundary()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Check interesting cases for boundary conditions
            // You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            VerifyDivRemString(Math.Pow(2, 32) + " 2 bDivRem");

            // 32 bit boundary  n1=0 n2=1
            VerifyDivRemString(Math.Pow(2, 33) + " 2 bDivRem");
        }

        [Fact]
        public static void RunDivRemMedium()
        {
            Test();
            BigIntTools.Utils.RunWithFakeThreshold(BigIntegerCalculator.DivideThreshold, 5, Test);

            void Test()
            {
                byte[] tempByteArray1 = new byte[0];
                byte[] tempByteArray2 = new byte[0];

                for (int i = 1; i < 8; i++)
                {
                    var num = 1 << i;
                    tempByteArray1 = GetRandomByteArray(s_random, num);
                    tempByteArray2 = GetRandomByteArray(s_random, num / 2);
                    VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem");
                }

                // Divide Method - Two Large BigIntegers
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                    {
                        int num = (1 << 7) + i;
                        tempByteArray1 = GetRandomByteArray(s_random, num);
                        tempByteArray2 = GetRandomByteArray(s_random, num / 3 + j);
                        VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem");
                    }
            }
        }

        [Fact]
        [OuterLoop]
        public static void RunDivRemLarge()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            for (int i = 8; i < 10; i++)
            {
                var num = 1 << i;
                tempByteArray1 = GetRandomByteArray(s_random, num);
                tempByteArray2 = GetRandomByteArray(s_random, num / 2);
                VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem");
            }

            // Divide Method - Two Large BigIntegers
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    int num = (1 << 10) + i;
                    tempByteArray1 = GetRandomByteArray(s_random, num);
                    tempByteArray2 = GetRandomByteArray(s_random, num / 3 + j);
                    VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem");
                }
        }

        [Fact]
        public void D3n2nBound()
        {
            var right = (BigInteger.One << (BigIntegerCalculator.DivideThreshold * 4 * 32 - 1))
                + (BigInteger.One << (BigIntegerCalculator.DivideThreshold * 2 * 32)) - 1;
            var rem = right - 1;

            var qi = BigIntegerCalculator.DivideThreshold * 8 * 32 * 4 - 1;
            var q = (BigInteger.One << qi) - 1;
            var left = q * right + rem;

            var (q2, r2) = BigInteger.DivRem(left, right);
            Assert.Equal(q, q2);
            Assert.Equal(rem, r2);
        }

        [Fact]
        public static void RunDivRemTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // DivRem Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");
            }

            // DivRem Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");
            }

            // DivRem Method - One Large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");

                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");
            }

            // DivRem Method - One Large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");

                Assert.Throws<DivideByZeroException>(() => { VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem"); });
            }

            // DivRem Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifyDivRemString(Print(tempByteArray1) + Print(tempByteArray2) + "bDivRem");

                Assert.Throws<DivideByZeroException>(() => { VerifyDivRemString(Print(tempByteArray2) + Print(tempByteArray1) + "bDivRem"); });
            }


            // Check interesting cases for boundary conditions
            // You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            VerifyDivRemString(Math.Pow(2, 32) + " 2 bDivRem");

            // 32 bit boundary  n1=0 n2=1
            VerifyDivRemString(Math.Pow(2, 33) + " 2 bDivRem");
        }

        private static void VerifyDivRemString(string opstring)
        {
            try
            {
                StackCalc sc = new StackCalc(opstring);
                while (sc.DoNextOperation())
                {
                    Assert.Equal(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString());
                    sc.VerifyOutParameter();
                }
            }
            catch (Exception e) when (!(e is DivideByZeroException))
            {
                // Log the original parameters, so we can reproduce any failure given the log
                throw new Exception($"VerifyDivRemString failed: {opstring} {e.ToString()}", e);
            }
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
