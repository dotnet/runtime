// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
