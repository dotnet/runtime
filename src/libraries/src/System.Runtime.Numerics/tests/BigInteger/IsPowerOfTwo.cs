// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Xunit;

namespace System.Numerics.Tests
{
    public class IsPowerOfTwoTest
    {
        private const int MaxDigits = 400;
        private const int MaxExponent = 2;
        private const int Reps = 500;
        private static int s_seed = 0;
        public static int onePosition = 0;

        [Fact]
        public static void RunIsPowerOdTwoTests()
        {
            Random random = new Random(s_seed);
            BigInteger bigIntegerSmall = 0;
            BigInteger bigIntegerLarge = 0;
            BigInteger bigIntegerPowerOfTwo;

            byte[] byteArray1;
            byte[] byteArray2;
            byte[] byteArray3;

            //random cases

            byteArray1 = GetRandomByteArray(random, true);
            byteArray2 = GetRandomByteArray(random, false);
            byteArray3 = GetPowerOfTwoByteArray(random);

            bigIntegerSmall = new BigInteger(byteArray1);
            bigIntegerLarge = new BigInteger(byteArray2);
            bigIntegerPowerOfTwo = new BigInteger(byteArray3);

            // Just basic tests
            // Large Power Of Two
            VerifyIsPowerOfTwo((BigInteger)int.MaxValue + 1, true);

            // Large Non Power Of Two
            VerifyIsPowerOfTwo((BigInteger)int.MaxValue + 2, false);


            // Small Power Of Two
            VerifyIsPowerOfTwo((BigInteger)short.MaxValue + 1, true);

            // Small Non Power Of Two
            VerifyIsPowerOfTwo((BigInteger)int.MaxValue - 2, false);

            // Zero Case, 1, -1
            // Zero
            VerifyIsPowerOfTwo(BigInteger.Zero, false);

            // One
            VerifyIsPowerOfTwo(BigInteger.One, true);

            // Negative One
            VerifyIsPowerOfTwo(BigInteger.MinusOne, false);

            // Random Small BigInteger
            VerifyIsPowerOfTwo(bigIntegerSmall, CheckExpected(byteArray1));

            // Random Large BigInteger
            VerifyIsPowerOfTwo(bigIntegerLarge, CheckExpected(byteArray2));

            // Random BigInteger Power of Two
            VerifyIsPowerOfTwo(bigIntegerPowerOfTwo, true);
        }

        private static bool CheckExpected(byte[] value)
        {
            int valueOne = 0;
            bool expected = false;
            BitArray value2 = new BitArray(value);

            // Count the number of 1's in the value
            for (int i = 0; i < value2.Length; i++)
            {
                if (value2[i])
                {
                    valueOne++;
                }
            }

            // If only one 1 and value is positive. 
            if ((1 == valueOne) && (!value2[value2.Length - 1]))
            {
                expected = true;
            }
            else
            {
                expected = false;
            }
            return expected;
        }

        private static byte[] GetRandomByteArray(Random random, bool isSmall)
        {
            byte[] value;
            int byteValue;
            if (isSmall == true)
            {
                byteValue = random.Next(0, 32);
                value = new byte[byteValue];
            }
            else
            {
                byteValue = random.Next(32, 128);
                value = new byte[byteValue];
            }

            for (int i = 0; i < value.Length; i++)
            {
                value[i] = (byte)random.Next(0, 256);
            }
            return value;
        }

        private static byte[] GetPowerOfTwoByteArray(Random random)
        {
            double exactOnePosition;

            byte[] value = new byte[32];
            for (int m = 0; m < value.Length; m++)
            {
                value[m] = 0x00;
            }

            onePosition = random.Next(0, 32);
            exactOnePosition = random.Next(0, 8);
            //ensure it isn't the sign bit that we would be flipping to 1.
            if (onePosition == 31)
            {
                exactOnePosition = random.Next(0, 7);
            }
            value[onePosition] = (byte)Math.Pow(2, exactOnePosition);

            return value;
        }
        
        private static void VerifyIsPowerOfTwo(BigInteger bigInt, bool expectedAnswer)
        {
            Assert.Equal(expectedAnswer, bigInt.IsPowerOfTwo);
        }
    }
}
