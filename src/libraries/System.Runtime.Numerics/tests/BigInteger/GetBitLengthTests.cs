// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Tests
{
    public class GetBitLengthTests
    {
        [Fact]
        public static void RunGetBitLengthTests()
        {
            Random random = new Random();

            // Trivial cases
            //                     sign bit|shortest two's complement
            //                              string w/o sign bit
            VerifyGetBitLength(0, 0);  // 0|
            VerifyGetBitLength(1, 1);  // 0|1
            VerifyGetBitLength(-1, 0); // 1|
            VerifyGetBitLength(2, 2);  // 0|10
            VerifyGetBitLength(-2, 1); // 1|0
            VerifyGetBitLength(3, 2);  // 0|11
            VerifyGetBitLength(-3, 2); // 1|01
            VerifyGetBitLength(4, 3);  // 0|100
            VerifyGetBitLength(-4, 2); // 1|00
            VerifyGetBitLength(5, 3);  // 0|101
            VerifyGetBitLength(-5, 3); // 1|011
            VerifyGetBitLength(6, 3);  // 0|110
            VerifyGetBitLength(-6, 3); // 1|010
            VerifyGetBitLength(7, 3);  // 0|111
            VerifyGetBitLength(-7, 3); // 1|001
            VerifyGetBitLength(8, 4);  // 0|1000
            VerifyGetBitLength(-8, 3); // 1|000

            // Random cases
            VerifyLoopGetBitLength(random, true);
            VerifyLoopGetBitLength(random, false);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))] // OOM on 32 bit
        [SkipOnPlatform(TestPlatforms.Browser, "OOM on browser due to large array allocations")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37093", TestPlatforms.Android)]
        public static void RunGetBitLengthTestsLarge()
        {
            // Very large cases
            VerifyGetBitLength(BigInteger.One << 32 << int.MaxValue, int.MaxValue + 32L + 1, 1);
            VerifyGetBitLength(BigInteger.One << 64 << int.MaxValue, int.MaxValue + 64L + 1, 1);
        }

        private static void VerifyLoopGetBitLength(Random random, bool isSmall)
        {
            for (uint i = 0; i < 1000; i++)
            {
                byte[] byteArray = GetRandomByteArray(random, isSmall);

                BigInteger bigInteger = new BigInteger(byteArray);

                VerifyGetBitLength(bigInteger);
            }
        }

        private static byte[] GetRandomByteArray(Random random, bool isSmall)
        {
            byte[] value;
            int byteValue;

            if (isSmall)
            {
                byteValue = random.Next(0, 32);
                value = new byte[byteValue];
            }
            else
            {
                byteValue = random.Next(32, 128);
                value = new byte[byteValue];
            }

            for (int i = 0; i < byteValue; i++)
            {
                value[i] = (byte)random.Next(0, 256);
            }

            return value;
        }

        private static long Log2BitLength(BigInteger integer)
        {
            return (long)Math.Ceiling(BigInteger.Log(integer.Sign < 0 ? -integer : integer + 1, 2.0));
        }

        private static bool TryIterativeBitLength(BigInteger integer, out long bitLength)
        {
            long value;

            try
            {
                value = (long) integer;
            }
            catch (OverflowException)
            {
                bitLength = 0;
                return false;
            }

            const long signMask = unchecked((long)0x8000_0000_0000_0000);

            long signBit = value < 0 ? signMask : 0;
            long tmp = value;
            long j;
            for (j = 0; j < 64 && (tmp & signMask) == signBit; j++)
                tmp <<= 1;

            bitLength = 64 - j;
            return true;
        }

        private static void VerifyGetBitLength(BigInteger bigInt)
        {
            long actualBitLength = GetBitLength(bigInt);
            long expectedBitLength = Log2BitLength(bigInt);

            // self-consistency check
            if (TryIterativeBitLength(bigInt, out long expectedBitLengthIterative))
                Assert.Equal(expectedBitLength, expectedBitLengthIterative);

            Assert.Equal(expectedBitLength, actualBitLength);
        }

        private static void VerifyGetBitLength(BigInteger bigInt, long expectedResult, uint epsilon = 0)
        {
            long actualBitLength = GetBitLength(bigInt);

            // Log is imprecise on large inputs
            Assert.InRange(Log2BitLength(bigInt), expectedResult - epsilon, expectedResult + epsilon);

            // self-consistency check
            if (TryIterativeBitLength(bigInt, out long expectedBitLengthIterative))
                Assert.Equal(expectedResult, expectedBitLengthIterative);

            Assert.Equal(expectedResult, actualBitLength);
        }

        private static long GetBitLength(BigInteger bigInt)
        {
            long actualBitLength = bigInt.GetBitLength();
            Assert.InRange(actualBitLength, 0, long.MaxValue);
            return actualBitLength;
        }
    }
}
