// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.RNG.Tests
{
    public partial class RandomNumberGeneratorTests
    {
        [Theory]
        [InlineData(10)]
        [InlineData(256)]
        [InlineData(65536)]
        public static void DifferentSequential_Span(int arraySize)
        {
            // Ensure that the RNG doesn't produce a stable set of data.
            var first = new byte[arraySize];
            var second = new byte[arraySize];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes((Span<byte>)first);
                rng.GetBytes((Span<byte>)second);
            }

            // Random being random, there is a chance that it could produce the same sequence.
            // The smallest test case that we have is 10 bytes.
            // The probability that they are the same, given a Truly Random Number Generator is:
            // Pmatch(byte0) * Pmatch(byte1) * Pmatch(byte2) * ... * Pmatch(byte9)
            // = 1/256 * 1/256 * ... * 1/256
            // = 1/(256^10)
            // = 1/1,208,925,819,614,629,174,706,176
            Assert.NotEqual(first, second);
        }

        [Fact]
        public static void GetBytes_Span_ZeroCount()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                var rand = new byte[1] { 1 };
                rng.GetBytes(new Span<byte>(rand, 0, 0));
                Assert.Equal(1, rand[0]);
            }
        }

        [Fact]
        public static void GetNonZeroBytes_Span()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                var rand = new byte[65536];
                rng.GetNonZeroBytes(new Span<byte>(rand));
                Assert.Equal(-1, Array.IndexOf<byte>(rand, 0));
            }
        }

        [Fact]
        public static void Fill_ZeroLengthSpan()
        {
            byte[] rand = { 1 };
            RandomNumberGenerator.Fill(new Span<byte>(rand, 0, 0));
            Assert.Equal(1, rand[0]);
        }

        [Fact]
        public static void Fill_SpanLength1()
        {
            byte[] rand = { 1 };
            bool replacedValue = false;

            for (int i = 0; i < 10; i++)
            {
                RandomNumberGenerator.Fill(rand);

                if (rand[0] != 1)
                {
                    replacedValue = true;
                    break;
                }
            }

            Assert.True(replacedValue, "Fill eventually wrote a different byte");
        }

        [Fact]
        public static void Fill_RandomDistribution()
        {
            byte[] random = new byte[2048];
            RandomNumberGenerator.Fill(random);

            RandomDataGenerator.VerifyRandomDistribution(random);
        }

        [Theory]
        [InlineData(10, 10)]
        [InlineData(10, 9)]
        [InlineData(-10, -10)]
        [InlineData(-10, -11)]
        public static void GetInt32_LowerAndUpper_InvalidRange(int fromInclusive, int toExclusive)
        {
            Assert.Throws<ArgumentException>(() => RandomNumberGenerator.GetInt32(fromInclusive, toExclusive));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public static void GetInt32_Upper_InvalidRange(int toExclusive)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RandomNumberGenerator.GetInt32(toExclusive));
        }

        [Theory]
        [InlineData(1 << 1)]
        [InlineData(1 << 4)]
        [InlineData(1 << 16)]
        [InlineData(1 << 24)]
        public static void GetInt32_PowersOfTwo(int toExclusive)
        {
            for (int i = 0; i < 10; i++)
            {
                int result = RandomNumberGenerator.GetInt32(toExclusive);
                Assert.InRange(result, 0, toExclusive - 1);
            }
        }

        [Theory]
        [InlineData((1 << 1) + 1)]
        [InlineData((1 << 4) + 1)]
        [InlineData((1 << 16) + 1)]
        [InlineData((1 << 24) + 1)]
        public static void GetInt32_PowersOfTwoPlusOne(int toExclusive)
        {
            for (int i = 0; i < 10; i++)
            {
                int result = RandomNumberGenerator.GetInt32(toExclusive);
                Assert.InRange(result, 0, toExclusive - 1);
            }
        }

        [Fact]
        public static void GetInt32_FullRange()
        {
            int result = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
            Assert.NotEqual(int.MaxValue, result);
        }

        [Fact]
        public static void GetInt32_DoesNotProduceSameNumbers()
        {
            int result1 = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
            int result2 = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
            int result3 = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);

            // The changes of this happening are (2^32 - 1) * 3.
            Assert.False(result1 == result2 && result2 == result3, "Generated the same number three times in a row.");
        }

        [Fact]
        public static void GetInt32_FullRange_DistributesBitsEvenly()
        {
            // This test should work since we are selecting random numbers that are a
            // Power of two minus one so no bit should favored.
            int numberToGenerate = 256;
            byte[] bytes = new byte[numberToGenerate * 4];
            Span<byte> bytesSpan = bytes.AsSpan();
            for (int i = 0, j = 0; i < numberToGenerate; i++, j += 4)
            {
                int result = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
                Span<byte> slice = bytesSpan.Slice(j, 4);
                BinaryPrimitives.WriteInt32LittleEndian(slice, result);
            }
            RandomDataGenerator.VerifyRandomDistribution(bytes);
        }

        [Fact]
        public static void GetInt32_CoinFlipLowByte()
        {
            int numberToGenerate = 1024;
            Span<int> generated = stackalloc int[numberToGenerate];

            for (int i = 0; i < numberToGenerate; i++)
            {
                generated[i] = RandomNumberGenerator.GetInt32(0, 2);
            }
            VerifyAllInRange(generated, 0, 2);
            VerifyDistribution(generated, 0.5);
        }


        [Fact]
        public static void GetInt32_CoinFlipOverByteBoundary()
        {
            int numberToGenerate = 1024;
            Span<int> generated = stackalloc int[numberToGenerate];

            for (int i = 0; i < numberToGenerate; i++)
            {
                generated[i] = RandomNumberGenerator.GetInt32(255, 257);
            }
            VerifyAllInRange(generated, 255, 257);
            VerifyDistribution(generated, 0.5);
        }

        [Fact]
        public static void GetInt32_NegativeBounds1000d20()
        {
            int numberToGenerate = 1000;
            Span<int> generated = stackalloc int[numberToGenerate];

            for (int i = 0; i < numberToGenerate; i++)
            {
                generated[i] = RandomNumberGenerator.GetInt32(-4000, -3979);
            }
            VerifyAllInRange(generated, -4000, -3979);
            VerifyDistribution(generated, 0.05);
        }

        [Fact]
        public static void GetInt32_1000d6()
        {
            int numberToGenerate = 1000;
            Span<int> generated = stackalloc int[numberToGenerate];

            for (int i = 0; i < numberToGenerate; i++)
            {
                generated[i] = RandomNumberGenerator.GetInt32(1, 7);
            }
            VerifyAllInRange(generated, 1, 7);
            VerifyDistribution(generated, 0.16);
        }

        [Theory]
        [InlineData(int.MinValue, int.MinValue + 3)]
        [InlineData(-257, -129)]
        [InlineData(-100, 5)]
        [InlineData(254, 512)]
        [InlineData(-1_073_741_909, - 1_073_741_825)]
        [InlineData(65_534, 65_539)]
        [InlineData(16_777_214, 16_777_217)]
        public static void GetInt32_MaskRangeCorrect(int fromInclusive, int toExclusive)
        {
            int numberToGenerate = 1000;
            Span<int> generated = stackalloc int[numberToGenerate];

            for (int i = 0; i < numberToGenerate; i++)
            {
                generated[i] = RandomNumberGenerator.GetInt32(fromInclusive, toExclusive);
            }

            double expectedDistribution = 1d / (toExclusive - fromInclusive);
            VerifyAllInRange(generated, fromInclusive, toExclusive);
            VerifyDistribution(generated, expectedDistribution);
        }

        private static void VerifyAllInRange(ReadOnlySpan<int> numbers, int fromInclusive, int toExclusive)
        {
            for (int i = 0; i < numbers.Length; i++)
            {
                Assert.InRange(numbers[i], fromInclusive, toExclusive - 1);
            }
        }

        private static void VerifyDistribution(ReadOnlySpan<int> numbers, double expected)
        {
            var observedNumbers = new Dictionary<int, int>(numbers.Length);
            for (int i = 0; i < numbers.Length; i++)
            {
                int number = numbers[i];
                if (!observedNumbers.TryAdd(number, 1))
                {
                    observedNumbers[number]++;
                }
            }
            const double tolerance = 0.07;
            foreach ((_, int occurences) in observedNumbers)
            {
                double percentage = occurences / (double)numbers.Length;
                Assert.True(Math.Abs(expected - percentage) < tolerance, "Occurred number of times within threshold.");
            }
        }
    }
}
