// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class logTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunLogTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];
            BigInteger bi;

            // Log Method - Log(1,+Infinity)
            Assert.Equal(0, BigInteger.Log(1, double.PositiveInfinity));

            // Log Method - Log(1,0)
            VerifyLogString("0 1 bLog");

            // Log Method - Log(0, >1)
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomPosByteArray(s_random, 10);
                VerifyLogString(Print(tempByteArray1) + "0 bLog");
            }

            // Log Method - Log(0, 0>x>1)
            for (int i = 0; i < s_samples; i++)
            {
                Assert.Equal(double.PositiveInfinity, BigInteger.Log(0, s_random.NextDouble()));
            }

            // Log Method - base = 0
            for (int i = 0; i < s_samples; i++)
            {
                bi = 1;
                while (bi == 1)
                {
                    bi = new BigInteger(GetRandomPosByteArray(s_random, 8));
                }
                Assert.True((double.IsNaN(BigInteger.Log(bi, 0))));
            }

            // Log Method - base = 1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                VerifyLogString("1 " + Print(tempByteArray1) + "bLog");
            }

            // Log Method - base = NaN
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(double.IsNaN(BigInteger.Log(new BigInteger(GetRandomByteArray(s_random, 10)), double.NaN)));
            }

            // Log Method - base = +Infinity
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(double.IsNaN(BigInteger.Log(new BigInteger(GetRandomByteArray(s_random, 10)), double.PositiveInfinity)));
            }

            // Log Method - Log(0,1)
            VerifyLogString("1 0 bLog");

            // Log Method - base < 0
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 10);
                tempByteArray2 = GetRandomNegByteArray(s_random, 1);
                VerifyLogString(Print(tempByteArray2) + Print(tempByteArray1) + "bLog");
                Assert.True(double.IsNaN(BigInteger.Log(new BigInteger(GetRandomByteArray(s_random, 10)), -s_random.NextDouble())));
            }

            // Log Method - value < 0
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomNegByteArray(s_random, 10);
                tempByteArray2 = GetRandomPosByteArray(s_random, 1);
                VerifyLogString(Print(tempByteArray2) + Print(tempByteArray1) + "bLog");
            }

            // Log Method - Small BigInteger and 0<base<0.5 
            for (int i = 0; i < s_samples; i++)
            {
                BigInteger temp = new BigInteger(GetRandomPosByteArray(s_random, 10));
                double newbase = Math.Min(s_random.NextDouble(), 0.5);
                Assert.True(ApproxEqual(BigInteger.Log(temp, newbase), Math.Log((double)temp, newbase)));
            }

            // Log Method - Large BigInteger and 0<base<0.5 
            for (int i = 0; i < s_samples; i++)
            {
                BigInteger temp = new BigInteger(GetRandomPosByteArray(s_random, s_random.Next(1, 100)));
                double newbase = Math.Min(s_random.NextDouble(), 0.5);
                Assert.True(ApproxEqual(BigInteger.Log(temp, newbase), Math.Log((double)temp, newbase)));
            }

            // Log Method - two small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomPosByteArray(s_random, 2);
                tempByteArray2 = GetRandomPosByteArray(s_random, 3);
                VerifyLogString(Print(tempByteArray1) + Print(tempByteArray2) + "bLog");
            }

            // Log Method - one small and one large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomPosByteArray(s_random, 1);
                tempByteArray2 = GetRandomPosByteArray(s_random, s_random.Next(1, 100));
                VerifyLogString(Print(tempByteArray1) + Print(tempByteArray2) + "bLog");
            }

            // Log Method - two large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomPosByteArray(s_random, s_random.Next(1, 100));
                tempByteArray2 = GetRandomPosByteArray(s_random, s_random.Next(1, 100));
                VerifyLogString(Print(tempByteArray1) + Print(tempByteArray2) + "bLog");
            }

            // Log Method - Very Large BigInteger 1 << 128 << Int.MaxValue and 2
            LargeValueLogTests(128, 1);

        }

        [Fact]
        [OuterLoop]
        public static void RunLargeValueLogTests()
        {
            LargeValueLogTests(0, 4, 64, 3);
        }

        /// <summary>
        /// Test Log Method on Very Large BigInteger more than (1 &lt;&lt; Int.MaxValue) by base 2
        /// Tested BigInteger are: pow(2, startShift + smallLoopShift * [1..smallLoopLimit] + Int32.MaxValue * [1..bigLoopLimit])
        /// Note: 
        /// ToString() can not operate such large values
        /// VerifyLogString() can not operate such large values, 
        /// Math.Log() can not operate such large values
        /// </summary>
        private static void LargeValueLogTests(int startShift, int bigShiftLoopLimit, int smallShift = 0, int smallShiftLoopLimit = 1)
        {
            BigInteger init = BigInteger.One << startShift;
            double logbase = 2D;

            for (int i = 0; i < smallShiftLoopLimit; i++)
            {
                BigInteger temp = init << ((i + 1) * smallShift);

                for (int j = 0; j<bigShiftLoopLimit; j++)
                {
                    temp = temp << (int.MaxValue / 10);
                    double expected =
                        (double)startShift +
                        smallShift * (double)(i + 1) +
                        (int.MaxValue / 10) * (double)(j + 1);
                    Assert.True(ApproxEqual(BigInteger.Log(temp, logbase), expected));
                }
                
            }
        }

        private static void VerifyLogString(string opstring)
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

        private static byte[] GetRandomPosByteArray(Random random, int size)
        {
            byte[] value = new byte[size];

            for (int i = 0; i < value.Length; i++)
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

        private static bool ApproxEqual(double value1, double value2)
        {
            //Special case values;
            if (double.IsNaN(value1))
            {
                return double.IsNaN(value2);
            }
            if (double.IsNegativeInfinity(value1))
            {
                return double.IsNegativeInfinity(value2);
            }
            if (double.IsPositiveInfinity(value1))
            {
                return double.IsPositiveInfinity(value2);
            }
            if (value2 == 0)
            {
                return (value1 == 0);
            }

            double result = Math.Abs((value1 / value2) - 1);
            return (result <= double.Parse("1e-15"));
        }
    }
}
