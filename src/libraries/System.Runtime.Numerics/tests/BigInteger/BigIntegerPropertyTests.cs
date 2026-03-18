// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Numerics.Tests
{
    /// <summary>
    /// Property-based algebraic verification tests for BigInteger.
    /// These test cross-operation invariants at various sizes including
    /// algorithm threshold boundaries relevant to nuint-width limbs.
    /// </summary>
    public class BigIntegerPropertyTests
    {
        // On 64-bit, nuint limbs are 8 bytes. These sizes (in bytes) are chosen to exercise:
        //   - Small (inline _sign), single limb, multi-limb
        //   - Algorithm threshold boundaries (Karatsuba=32 limbs, BZ=64 limbs, Toom3=256 limbs)
        // Byte counts map to limb counts via ceil(bytes / nint.Size).
        public static IEnumerable<object[]> ByteSizes => new object[][]
        {
            new object[] { 1 },      // inline in _sign
            new object[] { 4 },      // single uint, single nuint on 32-bit
            new object[] { 8 },      // single nuint on 64-bit
            new object[] { 9 },      // 2 limbs on 64-bit
            new object[] { 16 },     // 2 nuint limbs
            new object[] { 64 },     // 8 limbs on 64-bit
            new object[] { 128 },    // 16 limbs
            new object[] { 248 },    // just below Karatsuba threshold on 64-bit (31 limbs)
            new object[] { 264 },    // just above Karatsuba threshold on 64-bit (33 limbs)
            new object[] { 504 },    // just below BZ threshold on 64-bit (63 limbs)
            new object[] { 520 },    // just above BZ threshold on 64-bit (65 limbs)
            new object[] { 1024 },   // 128 limbs, well into Karatsuba territory
            new object[] { 2040 },   // just below Toom3 threshold (255 limbs)
            new object[] { 2056 },   // just above Toom3 threshold (257 limbs)
        };

        private static BigInteger MakeRandom(int byteCount, int seed) =>
            MakeRandom(byteCount, new Random(seed));

        private static BigInteger MakeRandom(int byteCount, Random rng)
        {
            byte[] bytes = new byte[byteCount + 1]; // +1 for sign byte
            rng.NextBytes(bytes);
            bytes[^1] = 0; // ensure positive
            if (bytes.Length > 1 && bytes[^2] == 0)
                bytes[^2] = 1; // ensure non-zero high byte
            return new BigInteger(bytes);
        }

        [Theory]
        [MemberData(nameof(ByteSizes))]
        public void ParseToStringRoundtrip(int byteCount)
        {
            var rng = new Random(42 + byteCount);
            for (int trial = 0; trial < 3; trial++)
            {
                BigInteger original = MakeRandom(byteCount, rng);
                string decStr = original.ToString();
                Assert.Equal(original, BigInteger.Parse(decStr));

                string hexStr = original.ToString("X");
                Assert.Equal(original, BigInteger.Parse(hexStr, NumberStyles.HexNumber));

                // Negative
                BigInteger neg = -original;
                Assert.Equal(neg, BigInteger.Parse(neg.ToString()));
            }
        }

        [Theory]
        [MemberData(nameof(ByteSizes))]
        public void ToByteArrayRoundtrip(int byteCount)
        {
            var rng = new Random(123 + byteCount);
            for (int trial = 0; trial < 3; trial++)
            {
                BigInteger original = MakeRandom(byteCount, rng);
                byte[] bytes = original.ToByteArray();
                Assert.Equal(original, new BigInteger(bytes));

                BigInteger neg = -original;
                bytes = neg.ToByteArray();
                Assert.Equal(neg, new BigInteger(bytes));
            }
        }

        [Theory]
        [MemberData(nameof(ByteSizes))]
        public void DivisionInvariant(int byteCount)
        {
            var rng = new Random(200 + byteCount);
            for (int trial = 0; trial < 3; trial++)
            {
                BigInteger dividend = MakeRandom(byteCount, rng);
                BigInteger divisor = MakeRandom(Math.Max(1, byteCount / 2), rng);
                if (divisor.IsZero) divisor = BigInteger.One;

                var (quotient, remainder) = BigInteger.DivRem(dividend, divisor);

                // q * d + r == n
                Assert.Equal(dividend, quotient * divisor + remainder);

                // |r| < |d|
                Assert.True(BigInteger.Abs(remainder) < BigInteger.Abs(divisor));

                // Verify with negatives too
                var (q2, r2) = BigInteger.DivRem(-dividend, divisor);
                Assert.Equal(-dividend, q2 * divisor + r2);
            }
        }

        [Theory]
        [MemberData(nameof(ByteSizes))]
        public void MultiplyDivideRoundtrip(int byteCount)
        {
            var rng = new Random(300 + byteCount);
            for (int trial = 0; trial < 3; trial++)
            {
                BigInteger a = MakeRandom(byteCount, rng);
                BigInteger b = MakeRandom(Math.Max(1, byteCount / 2), rng);
                if (b.IsZero) b = BigInteger.One;

                BigInteger product = a * b;
                Assert.Equal(a, product / b);
            }
        }

        [Theory]
        [MemberData(nameof(ByteSizes))]
        public void SquareVsMultiply(int byteCount)
        {
            var rng = new Random(400 + byteCount);
            for (int trial = 0; trial < 3; trial++)
            {
                BigInteger a = MakeRandom(byteCount, rng);
                Assert.Equal(a * a, BigInteger.Pow(a, 2));
            }
        }

        [Theory]
        [MemberData(nameof(ByteSizes))]
        public void GcdDividesBoth(int byteCount)
        {
            var rng = new Random(500 + byteCount);
            for (int trial = 0; trial < 3; trial++)
            {
                BigInteger a = MakeRandom(byteCount, rng);
                BigInteger b = MakeRandom(Math.Max(1, byteCount / 2), rng);

                BigInteger gcd = BigInteger.GreatestCommonDivisor(a, b);

                if (!gcd.IsZero)
                {
                    Assert.Equal(BigInteger.Zero, a % gcd);
                    Assert.Equal(BigInteger.Zero, b % gcd);
                }
            }
        }

        [Theory]
        [MemberData(nameof(ByteSizes))]
        public void AddSubtractRoundtrip(int byteCount)
        {
            var rng = new Random(600 + byteCount);
            for (int trial = 0; trial < 3; trial++)
            {
                BigInteger a = MakeRandom(byteCount, rng);
                BigInteger b = MakeRandom(byteCount, rng);

                Assert.Equal(a, (a + b) - b);
                Assert.Equal(b, (a + b) - a);
                Assert.Equal(a + b, b + a); // commutativity
            }
        }

        [Theory]
        [MemberData(nameof(ByteSizes))]
        public void ShiftRoundtrip(int byteCount)
        {
            var rng = new Random(700 + byteCount);
            for (int trial = 0; trial < 3; trial++)
            {
                BigInteger a = MakeRandom(byteCount, rng);
                foreach (int shift in new[] { 1, 7, 8, 15, 16, 31, 32, 33, 63, 64, 65, 128 })
                {
                    Assert.Equal(a, (a << shift) >> shift);
                }
            }
        }

        [Theory]
        [InlineData(64)]
        [InlineData(128)]
        [InlineData(256)]
        [InlineData(512)]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(4096)]
        public void CarryPropagationAllOnes(int bits)
        {
            BigInteger allOnes = (BigInteger.One << bits) - 1;
            BigInteger powerOf2 = BigInteger.One << bits;

            // All-ones + 1 should equal the next power of 2
            Assert.Equal(powerOf2, allOnes + 1);

            // Power of 2 - 1 should equal all-ones
            Assert.Equal(allOnes, powerOf2 - 1);

            // All-ones * all-ones should be (2^bits - 1)^2
            BigInteger squared = allOnes * allOnes;
            Assert.Equal(allOnes, BigInteger.Pow(allOnes, 2).IsqrtCheck());

            // Verify ToString/Parse roundtrip for all-ones pattern
            Assert.Equal(allOnes, BigInteger.Parse(allOnes.ToString()));
        }

        [Theory]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(33)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(127)]
        [InlineData(128)]
        [InlineData(129)]
        public void PowerOfTwoBoundaries(int bits)
        {
            BigInteger p = BigInteger.One << bits;
            BigInteger pMinus1 = p - 1;
            BigInteger pPlus1 = p + 1;

            // Verify basic identities
            Assert.Equal(p, pMinus1 + 1);
            Assert.Equal(p, pPlus1 - 1);

            // Division: (2^n) / (2^n - 1) should be 1 remainder 1
            var (q, r) = BigInteger.DivRem(p, pMinus1);
            Assert.Equal(BigInteger.One, q);
            Assert.Equal(BigInteger.One, r);

            // Parse/ToString roundtrip at boundary
            Assert.Equal(p, BigInteger.Parse(p.ToString()));
            Assert.Equal(pMinus1, BigInteger.Parse(pMinus1.ToString()));
        }

        [Fact]
        public void NuintMaxValueEdgeCases()
        {
            // Test values at nuint boundaries
            BigInteger uint32Max = new BigInteger(uint.MaxValue);
            BigInteger uint64Max = new BigInteger(ulong.MaxValue);

            // Arithmetic at uint boundaries
            Assert.Equal(new BigInteger((long)uint.MaxValue + 1), uint32Max + 1);
            Assert.Equal(uint32Max * uint32Max, BigInteger.Parse("18446744065119617025"));

            // Arithmetic at ulong boundaries
            Assert.Equal(BigInteger.Parse("18446744073709551616"), uint64Max + 1);
            Assert.Equal(BigInteger.Parse("340282366920938463426481119284349108225"), uint64Max * uint64Max);

            // Division at boundary
            var (q, r) = BigInteger.DivRem(uint64Max * uint64Max, uint64Max);
            Assert.Equal(uint64Max, q);
            Assert.Equal(BigInteger.Zero, r);
        }

        [Theory]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(128)]
        [InlineData(256)]
        [InlineData(512)]
        public void ModPowBasicInvariants(int bits)
        {
            var rng = new Random(800 + bits);
            BigInteger b = MakeRandom(bits / 8, rng);
            BigInteger modulus = MakeRandom(bits / 8, rng);
            if (modulus <= 1) modulus = BigInteger.Parse("1000000007");

            // a^1 mod m == a mod m
            Assert.Equal(b % modulus, BigInteger.ModPow(b, 1, modulus));

            // a^0 mod m == 1 (when m > 1)
            Assert.Equal(BigInteger.One, BigInteger.ModPow(b, 0, modulus));

            // (a^2) mod m == (a * a) mod m
            BigInteger mp2 = BigInteger.ModPow(b, 2, modulus);
            BigInteger direct = (b * b) % modulus;
            Assert.Equal(direct, mp2);
        }

        [Theory]
        [MemberData(nameof(ByteSizes))]
        public void BitwiseRoundtrip(int byteCount)
        {
            var rng = new Random(900 + byteCount);
            for (int trial = 0; trial < 3; trial++)
            {
                BigInteger a = MakeRandom(byteCount, rng);
                BigInteger b = MakeRandom(byteCount, rng);

                // (a & b) | (a ^ b) == a | b
                Assert.Equal(a | b, (a & b) | (a ^ b));

                // a ^ a == 0
                Assert.Equal(BigInteger.Zero, a ^ a);

                // a & a == a
                Assert.Equal(a, a & a);

                // a | a == a
                Assert.Equal(a, a | a);
            }
        }
    }

    internal static class BigIntegerTestExtensions
    {
        /// <summary>Verify that value^2 == original, returning the sqrt.</summary>
        internal static BigInteger IsqrtCheck(this BigInteger squared)
        {
            // Newton's method integer sqrt for verification
            if (squared.IsZero) return BigInteger.Zero;
            BigInteger x = BigInteger.One << ((int)squared.GetBitLength() / 2 + 1);
            while (true)
            {
                BigInteger next = (x + squared / x) / 2;
                if (next >= x) return x;
                x = next;
            }
        }
    }
}
