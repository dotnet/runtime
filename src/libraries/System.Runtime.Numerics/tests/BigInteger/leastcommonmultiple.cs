// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Numerics.Tests
{
    public class LeastCommonMultipleTests
    {
        private const int Samples = 16;

        private static readonly Random s_random = new Random(100);

        [Theory]
        [InlineData(4, 6, 12)]
        [InlineData(6, 4, 12)]
        [InlineData(21, 6, 42)]
        [InlineData(12, 18, 36)]
        [InlineData(3, 7, 21)]              // coprime: product
        [InlineData(4, 8, 8)]               // one divides the other
        [InlineData(8, 4, 8)]
        [InlineData(5, 5, 5)]
        [InlineData(1, 7, 7)]
        [InlineData(7, 1, 7)]
        [InlineData(-4, 6, 12)]             // result is always non-negative
        [InlineData(4, -6, 12)]
        [InlineData(-4, -6, 12)]
        [InlineData(-2147483648, 1, 2147483648)]
        [InlineData(-2147483648, -2147483648, 2147483648)]
        public static void SmallValues(long left, long right, long expected)
        {
            Assert.Equal((BigInteger)expected, BigInteger.LeastCommonMultiple(left, right));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 5)]
        [InlineData(5, 0)]
        [InlineData(0, -5)]
        [InlineData(-5, 0)]
        [InlineData(0, long.MaxValue)]
        [InlineData(0, long.MinValue)]
        public static void ZeroOperand_ReturnsZero(long left, long right)
        {
            Assert.Equal(BigInteger.Zero, BigInteger.LeastCommonMultiple(left, right));
        }

        [Fact]
        public static void ResultIsNeverNegative()
        {
            foreach ((BigInteger left, BigInteger right) in Pairs())
            {
                BigInteger result = BigInteger.LeastCommonMultiple(left, right);

                Assert.True(result.Sign >= 0, $"Result was negative for left={left}, right={right}.");
            }
        }

        [Fact]
        public static void IsCommutative()
        {
            foreach ((BigInteger left, BigInteger right) in Pairs())
            {
                Assert.Equal(
                    BigInteger.LeastCommonMultiple(left, right),
                    BigInteger.LeastCommonMultiple(right, left));
            }
        }

        [Fact]
        public static void SignOfOperandsDoesNotAffectResult()
        {
            foreach ((BigInteger left, BigInteger right) in Pairs())
            {
                BigInteger expected = BigInteger.LeastCommonMultiple(left, right);

                Assert.Equal(expected, BigInteger.LeastCommonMultiple(-left, right));
                Assert.Equal(expected, BigInteger.LeastCommonMultiple(left, -right));
                Assert.Equal(expected, BigInteger.LeastCommonMultiple(-left, -right));
            }
        }

        [Fact]
        public static void ResultIsAMultipleOfBothOperands()
        {
            foreach ((BigInteger left, BigInteger right) in Pairs())
            {
                BigInteger result = BigInteger.LeastCommonMultiple(left, right);

                if (result.IsZero)
                {
                    Assert.True(left.IsZero || right.IsZero);
                    continue;
                }

                Assert.Equal(BigInteger.Zero, result % left);
                Assert.Equal(BigInteger.Zero, result % right);
            }
        }

        [Fact]
        public static void ProductOfLcmAndGcdEqualsProductOfMagnitudes()
        {
            // lcm(a, b) * gcd(a, b) == |a * b| for all a, b (including zero, where both sides are zero).
            foreach ((BigInteger left, BigInteger right) in Pairs())
            {
                BigInteger lcm = BigInteger.LeastCommonMultiple(left, right);
                BigInteger gcd = BigInteger.GreatestCommonDivisor(left, right);

                Assert.Equal(BigInteger.Abs(left * right), lcm * gcd);
            }
        }

        [Fact]
        public static void IsTheLeastCommonMultiple()
        {
            // No proper divisor of the result that is still a common multiple should exist;
            // equivalently, result / |left| and result / |right| must be coprime cofactors.
            foreach ((BigInteger left, BigInteger right) in Pairs())
            {
                if (left.IsZero || right.IsZero)
                {
                    continue;
                }

                BigInteger result = BigInteger.LeastCommonMultiple(left, right);

                BigInteger leftCofactor = result / BigInteger.Abs(left);
                BigInteger rightCofactor = result / BigInteger.Abs(right);

                Assert.Equal(BigInteger.One, BigInteger.GreatestCommonDivisor(leftCofactor, rightCofactor));
            }
        }

        [Fact]
        public static void IsIdempotent()
        {
            foreach ((BigInteger left, BigInteger _) in Pairs())
            {
                Assert.Equal(BigInteger.Abs(left), BigInteger.LeastCommonMultiple(left, left));
            }
        }

        [Fact]
        public static void OneIsTheIdentity()
        {
            foreach ((BigInteger left, BigInteger _) in Pairs())
            {
                Assert.Equal(BigInteger.Abs(left), BigInteger.LeastCommonMultiple(left, BigInteger.One));
                Assert.Equal(BigInteger.Abs(left), BigInteger.LeastCommonMultiple(BigInteger.One, left));
            }
        }

        [Fact]
        public static void IsAssociative()
        {
            BigInteger[] values = new BigInteger[]
            {
                6,
                10,
                15,
                BigInteger.Pow(2, 61) - 1,
                BigInteger.Pow(3, 40),
                RandomNonNegative(32),
            };

            foreach (BigInteger x in values)
            {
                foreach (BigInteger y in values)
                {
                    foreach (BigInteger z in values)
                    {
                        Assert.Equal(
                            BigInteger.LeastCommonMultiple(BigInteger.LeastCommonMultiple(x, y), z),
                            BigInteger.LeastCommonMultiple(x, BigInteger.LeastCommonMultiple(y, z)));
                    }
                }
            }
        }

        [Fact]
        public static void PowersOfTwo()
        {
            for (int i = 0; i < 256; i += 17)
            {
                for (int j = 0; j < 256; j += 23)
                {
                    BigInteger left = BigInteger.One << i;
                    BigInteger right = BigInteger.One << j;

                    Assert.Equal(BigInteger.One << Math.Max(i, j), BigInteger.LeastCommonMultiple(left, right));
                    Assert.Equal(BigInteger.One << Math.Max(i, j), BigInteger.LeastCommonMultiple(-left, right));
                }
            }
        }

        [Fact]
        public static void SharedTrailingZeroLimbs()
        {
            // Both operands are shifted by whole limb counts, which exercises the common
            // limb offset handling shared with GreatestCommonDivisor.
            foreach (int shift in new int[] { 32, 64, 96, 128, 256 })
            {
                BigInteger left = RandomPositive(16) << shift;
                BigInteger right = RandomPositive(16) << shift;

                BigInteger lcm = BigInteger.LeastCommonMultiple(left, right);
                BigInteger gcd = BigInteger.GreatestCommonDivisor(left, right);

                Assert.Equal(BigInteger.Abs(left * right), lcm * gcd);
                Assert.Equal(BigInteger.Zero, lcm % left);
                Assert.Equal(BigInteger.Zero, lcm % right);
            }
        }

        [Fact]
        public static void LargeOperands()
        {
            foreach (int byteCount in new int[] { 8, 9, 16, 17, 32, 64, 128, 256, 512 })
            {
                for (int i = 0; i < 4; i++)
                {
                    BigInteger left = RandomPositive(byteCount);
                    BigInteger right = RandomPositive(byteCount);

                    if ((i & 1) != 0)
                    {
                        left = -left;
                    }

                    BigInteger lcm = BigInteger.LeastCommonMultiple(left, right);

                    Assert.True(lcm.Sign > 0);
                    Assert.Equal(BigInteger.Zero, lcm % left);
                    Assert.Equal(BigInteger.Zero, lcm % right);
                    Assert.Equal(
                        BigInteger.Abs(left * right),
                        lcm * BigInteger.GreatestCommonDivisor(left, right));
                }
            }
        }

        [Fact]
        public static void CarmichaelFunctionUsage()
        {
            // The motivating scenario from the API proposal: lambda(n) = lcm(p - 1, q - 1).
            BigInteger p = BigInteger.Parse("32416190071");
            BigInteger q = BigInteger.Parse("32416187567");

            BigInteger lambda = BigInteger.LeastCommonMultiple(p - 1, q - 1);

            Assert.Equal(BigInteger.Zero, lambda % (p - 1));
            Assert.Equal(BigInteger.Zero, lambda % (q - 1));
            Assert.Equal(
                BigInteger.Abs((p - 1) * (q - 1)),
                lambda * BigInteger.GreatestCommonDivisor(p - 1, q - 1));

            BigInteger e = 65537;
            BigInteger d = BigInteger.ModInverse(e, lambda);

            Assert.Equal(BigInteger.One, (e * d) % lambda);
        }

        private static IEnumerable<(BigInteger Left, BigInteger Right)> Pairs()
        {
            yield return (0, 0);
            yield return (0, 7);
            yield return (7, 0);
            yield return (1, 1);
            yield return (4, 6);
            yield return (-4, 6);
            yield return (long.MaxValue, long.MaxValue);
            yield return (BigInteger.Pow(2, 127) - 1, BigInteger.Pow(2, 89) - 1);
            yield return (BigInteger.Pow(2, 128), BigInteger.Pow(2, 64));

            for (int i = 0; i < Samples; i++)
            {
                foreach (int byteCount in new int[] { 1, 2, 4, 8, 16, 48, 100 })
                {
                    BigInteger left = RandomPositive(byteCount);
                    BigInteger right = RandomPositive(byteCount);

                    if ((i & 1) != 0)
                    {
                        left = -left;
                    }

                    if ((i & 2) != 0)
                    {
                        right = -right;
                    }

                    yield return (left, right);
                }
            }
        }

        private static BigInteger RandomNonNegative(int byteCount)
        {
            byte[] bytes = new byte[byteCount + 1];
            s_random.NextBytes(bytes);

            // Clear the most significant byte so the two's complement value is non-negative.
            bytes[byteCount] = 0;

            return new BigInteger(bytes);
        }

        private static BigInteger RandomPositive(int byteCount)
        {
            BigInteger result = RandomNonNegative(byteCount);
            return result.IsZero ? BigInteger.One : result;
        }
    }
}
