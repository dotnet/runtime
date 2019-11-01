// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Numerics.Tests
{
    public class propertiesTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunZeroTests()
        {
            BigInteger bigInteger;
            byte[] tempByteArray;

            // BigInteger.Zero == 0
            Assert.Equal("0", BigInteger.Zero.ToString());
            Assert.Equal(new BigInteger((long)(0)), BigInteger.Zero);
            Assert.Equal(new BigInteger((double)(0)), BigInteger.Zero);
            Assert.Equal(new BigInteger(new byte[] { 0, 0, 0, 0 }), BigInteger.Zero);
            Assert.Equal(BigInteger.One + BigInteger.MinusOne, BigInteger.Zero);
            Assert.Equal(BigInteger.One - BigInteger.One, BigInteger.Zero);
            Assert.Equal(BigInteger.MinusOne - BigInteger.MinusOne, BigInteger.Zero);

            // Identities with BigInteger.Zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray = GetRandomByteArray(s_random);
                bigInteger = new BigInteger(tempByteArray);

                Assert.Equal(BigInteger.Zero, BigInteger.Zero * bigInteger);
                Assert.Equal(bigInteger, bigInteger - BigInteger.Zero);
                Assert.Equal(BigInteger.MinusOne * bigInteger, BigInteger.Zero - bigInteger);
                Assert.Equal(bigInteger, bigInteger + BigInteger.Zero);
                Assert.Equal(bigInteger, BigInteger.Zero + bigInteger);

                Assert.Throws<DivideByZeroException>(() =>
                {
                    BigInteger tempBigInteger = bigInteger / BigInteger.Zero;
                });

                if (!MyBigIntImp.IsZero(tempByteArray))
                {
                    Assert.Equal(BigInteger.Zero, BigInteger.Zero / bigInteger);
                }
            }
        }

        [Fact]
        public static void RunOneTests()
        {
            BigInteger bigInteger;
            byte[] tempByteArray;

            // BigInteger.One == 1
            Assert.Equal("1", BigInteger.One.ToString());
            Assert.Equal(new BigInteger((long)(1)), BigInteger.One);
            Assert.Equal(new BigInteger((double)(1)), BigInteger.One);
            Assert.Equal(new BigInteger(new byte[] { 1, 0, 0, 0 }), BigInteger.One);
            Assert.Equal(BigInteger.Zero - BigInteger.MinusOne, BigInteger.One);
            Assert.Equal((BigInteger)671832 / (BigInteger)671832, BigInteger.One);

            // Identities with BigInteger.One
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray = GetRandomByteArray(s_random);

                bigInteger = new BigInteger(tempByteArray);

                Assert.Equal(bigInteger, BigInteger.One * bigInteger);
                Assert.Equal(bigInteger, bigInteger / BigInteger.One);
            }
        }

        [Fact]
        public static void RunMinusOneTests()
        {
            BigInteger bigInteger;
            byte[] tempByteArray;

            // BigInteger.MinusOne == -1
            Assert.Equal(
                CultureInfo.CurrentCulture.NumberFormat.NegativeSign + "1",
                BigInteger.MinusOne.ToString()
            );
            Assert.Equal(new BigInteger((long)(-1)), BigInteger.MinusOne);
            Assert.Equal(new BigInteger((double)(-1)), BigInteger.MinusOne);
            Assert.Equal(new BigInteger(new byte[] { 0xff, 0xff, 0xff, 0xff }), BigInteger.MinusOne);
            Assert.Equal(BigInteger.Zero - BigInteger.One, BigInteger.MinusOne);
            Assert.Equal((BigInteger)671832 / (BigInteger)(-671832), BigInteger.MinusOne);

            // Identities with BigInteger.MinusOne
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray = GetRandomByteArray(s_random);

                bigInteger = new BigInteger(tempByteArray);

                Assert.Equal(
                     BigInteger.Negate(new BigInteger(tempByteArray)),
                     BigInteger.MinusOne * bigInteger
                );

                Assert.Equal(
                    BigInteger.Negate(new BigInteger(tempByteArray)),
                    bigInteger / BigInteger.MinusOne
                );
            }
        }

        private static byte[] GetRandomByteArray(Random random)
        {
            return MyBigIntImp.GetRandomByteArray(random, random.Next(0, 1024));
        }
    }
}
