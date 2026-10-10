// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Numerics.Tests
{
    public class ModInverseTests
    {
        private const int Samples = 16;

        private static readonly Random s_random = new Random(100);

        // Mersenne exponents: 2^p - 1 is prime for each of these. Used to cross-check
        // against ModPow via Fermat's little theorem, and to cover a range of limb counts.
        private static readonly int[] s_mersenneExponents = new int[] { 31, 61, 89, 127, 521, 607, 1279 };

        [Theory]
        [InlineData(3, 7, 5)]
        [InlineData(2, 7, 4)]
        [InlineData(6, 7, 6)]
        [InlineData(1, 7, 1)]
        [InlineData(10, 7, 5)]          // |value| > modulus
        [InlineData(-3, 7, 2)]          // negative value is reduced first
        [InlineData(-1, 7, 6)]
        [InlineData(-11, 13, 7)]
        [InlineData(7, 26, 15)]
        [InlineData(2, 9, 5)]
        [InlineData(5, 12, 5)]
        [InlineData(17, 3120, 2753)]    // textbook RSA exponent
        [InlineData(65537, 1000000007, 743534192)]
        [InlineData(-2147483648, 3, 1)] // int.MinValue: negation must not overflow
        [InlineData(1, 2, 1)]           // smallest non-degenerate modulus
        public static void SmallValues(long value, long modulus, long expected)
        {
            Assert.Equal((BigInteger)expected, BigInteger.ModInverse(value, modulus));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(5)]
        [InlineData(-5)]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        public static void ModulusOne_ReturnsZero(long value)
        {
            Assert.Equal(BigInteger.Zero, BigInteger.ModInverse(value, BigInteger.One));
        }

        [Theory]
        [InlineData(6, 9)]      // gcd == 3
        [InlineData(0, 5)]      // gcd == 5
        [InlineData(5, 5)]      // value == modulus
        [InlineData(-5, 5)]
        [InlineData(10, 5)]     // value is a multiple of modulus
        [InlineData(4, 8)]
        [InlineData(0, 2)]
        [InlineData(2, 2)]
        public static void NotCoprime_ThrowsArithmeticException(long value, long modulus)
        {
            Assert.Throws<ArithmeticException>(() => BigInteger.ModInverse(value, modulus));
        }

        [Theory]
        [InlineData(3, 0)]
        [InlineData(3, -1)]
        [InlineData(3, -7)]
        [InlineData(0, 0)]
        [InlineData(0, -1)]
        [InlineData(-3, long.MinValue)]
        public static void NonPositiveModulus_ThrowsArgumentOutOfRangeException(long value, long modulus)
        {
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => BigInteger.ModInverse(value, modulus));

            Assert.Equal("modulus", exception.ParamName);
        }

        [Fact]
        public static void KnownIdentities()
        {
            foreach (BigInteger modulus in new BigInteger[] { 2, 3, 7, 26, 3120, 1000000007, BigInteger.Pow(2, 521) - 1 })
            {
                // 1 is always its own inverse.
                Assert.Equal(BigInteger.One, BigInteger.ModInverse(BigInteger.One, modulus));

                // (modulus - 1)^2 == modulus^2 - 2*modulus + 1 == 1 (mod modulus)
                Assert.Equal(modulus - 1, BigInteger.ModInverse(modulus - 1, modulus));

                // -1 reduces to modulus - 1.
                Assert.Equal(modulus - 1, BigInteger.ModInverse(BigInteger.MinusOne, modulus));
            }
        }

        [Fact]
        public static void ResultIsCanonicalRepresentative()
        {
            foreach ((BigInteger value, BigInteger modulus) in CoprimePairs())
            {
                BigInteger inverse = BigInteger.ModInverse(value, modulus);

                Assert.True(inverse.Sign >= 0, $"Inverse was negative for value={value}, modulus={modulus}.");
                Assert.True(inverse < modulus, $"Inverse was not below the modulus for value={value}, modulus={modulus}.");
            }
        }

        [Fact]
        public static void SatisfiesDefiningCongruence()
        {
            foreach ((BigInteger value, BigInteger modulus) in CoprimePairs())
            {
                BigInteger inverse = BigInteger.ModInverse(value, modulus);

                Assert.Equal(BigInteger.One % modulus, Mod(value * inverse, modulus));
            }
        }

        [Fact]
        public static void InverseOfInverseIsReducedValue()
        {
            foreach ((BigInteger value, BigInteger modulus) in CoprimePairs())
            {
                if (modulus.IsOne)
                {
                    continue;
                }

                BigInteger inverse = BigInteger.ModInverse(value, modulus);

                Assert.Equal(Mod(value, modulus), BigInteger.ModInverse(inverse, modulus));
            }
        }

        [Fact]
        public static void ValueIsReducedModuloTheModulus()
        {
            foreach ((BigInteger value, BigInteger modulus) in CoprimePairs())
            {
                BigInteger expected = BigInteger.ModInverse(value, modulus);

                // Adding or subtracting any multiple of the modulus leaves the inverse unchanged.
                Assert.Equal(expected, BigInteger.ModInverse(value + modulus, modulus));
                Assert.Equal(expected, BigInteger.ModInverse(value - modulus, modulus));
                Assert.Equal(expected, BigInteger.ModInverse(value + (modulus * 1000), modulus));

                // Negating the value negates the inverse.
                Assert.Equal(Mod(-expected, modulus), BigInteger.ModInverse(-value, modulus));
            }
        }

        [Fact]
        public static void MatchesModPowForPrimeModulus()
        {
            // For prime p and value not divisible by p, Fermat's little theorem gives
            // value^(p-2) == value^-1 (mod p).
            foreach (int exponent in s_mersenneExponents)
            {
                BigInteger prime = BigInteger.Pow(2, exponent) - 1;

                foreach (BigInteger value in new BigInteger[] { 2, 3, 65537, prime - 2, RandomNonNegative((exponent / 8) + 1) })
                {
                    BigInteger reduced = Mod(value, prime);
                    if (reduced.IsZero)
                    {
                        continue;
                    }

                    Assert.Equal(
                        BigInteger.ModPow(reduced, prime - 2, prime),
                        BigInteger.ModInverse(value, prime));
                }
            }
        }

        [Fact]
        public static void PowerOfTwoModulus()
        {
            // Exercises the power-of-two fast path. Only odd values are invertible mod 2^k.
            foreach (int bitLength in new int[] { 1, 2, 8, 31, 32, 33, 63, 64, 65, 127, 128, 255, 1024, 2048 })
            {
                BigInteger modulus = BigInteger.One << bitLength;

                for (int i = 0; i < 4; i++)
                {
                    BigInteger value = RandomNonNegative((bitLength / 8) + 2) | BigInteger.One;

                    BigInteger inverse = BigInteger.ModInverse(value, modulus);

                    Assert.True(inverse.Sign >= 0 && inverse < modulus);
                    Assert.Equal(BigInteger.One, Mod(value * inverse, modulus));

                    // The negated value must invert to the negated inverse.
                    Assert.Equal(Mod(-inverse, modulus), BigInteger.ModInverse(-value, modulus));
                }

                // Even values share the factor 2 with the modulus, so no inverse exists.
                Assert.Throws<ArithmeticException>(
                    () => BigInteger.ModInverse((RandomNonNegative(4) | BigInteger.One) << 1, modulus));
            }
        }

        [Fact]
        public static void PowerOfTwoModulusExhaustiveBitLengths()
        {
            // Every bit length through 300, plus the neighbourhoods of the larger whole-limb
            // multiples, so the power-of-two fast path is covered at, just below and just
            // above each limb boundary on both 32-bit and 64-bit limb layouts.
            List<int> bitLengths = new List<int>();

            for (int bitLength = 1; bitLength <= 300; bitLength++)
            {
                bitLengths.Add(bitLength);
            }

            foreach (int bitLength in new int[] { 511, 512, 513, 1023, 1024, 1025, 2047, 2048, 2049, 4096 })
            {
                bitLengths.Add(bitLength);
            }

            foreach (int bitLength in bitLengths)
            {
                BigInteger modulus = BigInteger.One << bitLength;

                // Small odd values stress the low limbs the lifting starts from, and
                // modulus - 1 is its own inverse for every power-of-two modulus.
                foreach (BigInteger candidate in new BigInteger[] { 1, 3, 5, 7, modulus - 1 })
                {
                    BigInteger value = candidate | BigInteger.One;

                    Assert.Equal(BigInteger.One, Mod(value * BigInteger.ModInverse(value, modulus), modulus));
                    Assert.Equal(BigInteger.One, Mod(-value * BigInteger.ModInverse(-value, modulus), modulus));
                }

                for (int i = 0; i < 4; i++)
                {
                    BigInteger value = RandomNonNegative((bitLength / 8) + 2) | BigInteger.One;

                    BigInteger inverse = BigInteger.ModInverse(value, modulus);

                    Assert.True(inverse.Sign > 0 && inverse < modulus);
                    Assert.Equal(BigInteger.One, Mod(value * inverse, modulus));

                    // Even values share the factor 2 with the modulus, so no inverse exists.
                    Assert.Throws<ArithmeticException>(() => BigInteger.ModInverse(value << 1, modulus));
                }
            }
        }

        [Fact]
        public static void PowerOfTwoModulusResultIsUniqueAndOdd()
        {
            // The inverse modulo a power of two is the unique representative in
            // [0, 2^bitLength), is always odd because the value it inverts is, and inverting
            // it again returns the reduced value.
            foreach (int bitLength in new int[] { 1, 2, 3, 7, 8, 16, 31, 32, 33, 64, 65, 127, 128, 129, 192, 256, 320, 512, 1024, 2048 })
            {
                BigInteger modulus = BigInteger.One << bitLength;

                for (int i = 0; i < Samples; i++)
                {
                    BigInteger value = RandomNonNegative((bitLength / 8) + 3) | BigInteger.One;

                    BigInteger inverse = BigInteger.ModInverse(value, modulus);

                    Assert.True(inverse.Sign > 0 && inverse < modulus);
                    Assert.False(inverse.IsEven);
                    Assert.Equal(BigInteger.One, Mod(value * inverse, modulus));
                    Assert.Equal(Mod(value, modulus), BigInteger.ModInverse(inverse, modulus));
                }
            }
        }

        [Fact]
        public static void LargeOperands()
        {
            // Sizes chosen to straddle the small/large thresholds inside BigIntegerCalculator
            // and to cover realistic cryptographic operand widths.
            foreach (int byteCount in new int[] { 8, 9, 16, 17, 32, 64, 128, 256, 512 })
            {
                for (int i = 0; i < 4; i++)
                {
                    BigInteger modulus = RandomNonNegative(byteCount) | BigInteger.One;
                    if (modulus <= BigInteger.One)
                    {
                        continue;
                    }

                    BigInteger value = RandomNonNegative(byteCount);
                    if (!BigInteger.GreatestCommonDivisor(value, modulus).IsOne)
                    {
                        continue;
                    }

                    BigInteger inverse = BigInteger.ModInverse(value, modulus);

                    Assert.True(inverse.Sign >= 0 && inverse < modulus);
                    Assert.Equal(BigInteger.One, Mod(value * inverse, modulus));

                    // The same must hold when the value is presented in negative form.
                    BigInteger negativeInverse = BigInteger.ModInverse(-value, modulus);
                    Assert.Equal(BigInteger.One, Mod(-value * negativeInverse, modulus));
                }
            }
        }

        [Fact]
        public static void AsymmetricOperandSizes()
        {
            // A small value against a large modulus and the reverse exercise different
            // short-circuit paths in the calculator.
            BigInteger largeModulus = BigInteger.Pow(2, 1279) - 1;

            Assert.Equal(BigInteger.One, Mod(3 * BigInteger.ModInverse(3, largeModulus), largeModulus));
            Assert.Equal(BigInteger.One, Mod(65537 * BigInteger.ModInverse(65537, largeModulus), largeModulus));

            // 2^1024 + 1 == 2 (mod 65537), so the two are coprime.
            BigInteger largeValue = BigInteger.Pow(2, 1024) + 1;
            Assert.Equal(BigInteger.One, Mod(largeValue * BigInteger.ModInverse(largeValue, 65537), 65537));
        }

        [Fact]
        public static void NonCoprimeLargeOperands()
        {
            BigInteger shared = BigInteger.Pow(2, 521) - 1;

            Assert.Throws<ArithmeticException>(() => BigInteger.ModInverse(shared * 3, shared * 5));
            Assert.Throws<ArithmeticException>(() => BigInteger.ModInverse(-shared * 3, shared * 5));
            Assert.Throws<ArithmeticException>(() => BigInteger.ModInverse(BigInteger.Zero, shared));
        }

        [Fact]
        public static void ChineseRemainderTheoremReconstruction()
        {
            // x == r1 (mod m1), x == r2 (mod m2), with m1 and m2 coprime.
            BigInteger m1 = 3, m2 = 5, r1 = 2, r2 = 3;
            BigInteger product = m1 * m2;

            BigInteger x = Mod(r1 + (m1 * Mod(BigInteger.ModInverse(m1, m2) * (r2 - r1), m2)), product);

            Assert.Equal((BigInteger)8, x);
            Assert.Equal(r1, Mod(x, m1));
            Assert.Equal(r2, Mod(x, m2));
        }

        private static IEnumerable<(BigInteger Value, BigInteger Modulus)> CoprimePairs()
        {
            yield return (3, 7);
            yield return (-3, 7);
            yield return (1, 1);
            yield return (0, 1);
            yield return (65537, 1000000007);
            yield return (BigInteger.Pow(2, 127) - 1, BigInteger.Pow(2, 89) - 1);

            for (int i = 0; i < Samples; i++)
            {
                foreach (int byteCount in new int[] { 1, 2, 4, 8, 16, 48, 100 })
                {
                    BigInteger modulus = RandomNonNegative(byteCount);
                    if (modulus <= BigInteger.One)
                    {
                        continue;
                    }

                    BigInteger value = RandomNonNegative(byteCount);
                    if ((i & 1) != 0)
                    {
                        value = -value;
                    }

                    if (BigInteger.GreatestCommonDivisor(value, modulus).IsOne)
                    {
                        yield return (value, modulus);
                    }
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

        private static BigInteger Mod(BigInteger value, BigInteger modulus)
        {
            BigInteger remainder = value % modulus;
            return remainder.Sign < 0 ? remainder + modulus : remainder;
        }
    }
}
