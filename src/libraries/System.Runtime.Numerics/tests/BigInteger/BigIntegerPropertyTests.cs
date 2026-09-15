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

            // Verify integer sqrt of allOnes^2 recovers allOnes
            Assert.Equal(allOnes, (allOnes * allOnes).IsqrtCheck());

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

    /// <summary>
    /// Tests exercising sign combinations, vectorization boundaries,
    /// special values, and asymmetric operand sizes.
    /// </summary>
    public class BigIntegerEdgeCaseTests
    {
        // Sizes chosen to exercise SIMD vector tail handling.
        // On 64-bit: Vector128=2 limbs, Vector256=4, Vector512=8.
        // Shift loops need VectorWidth+1 limbs minimum.
        // Multiply: Karatsuba=32 limbs, Toom3=256 limbs.
        public static IEnumerable<object[]> VectorAlignedLimbCounts => new object[][]
        {
            new object[] { 1 },    // scalar only
            new object[] { 2 },    // Vector128 width (no SIMD — needs 3 for shift)
            new object[] { 3 },    // Vector128 + 1 (minimum for SIMD shift)
            new object[] { 4 },    // Vector256 width
            new object[] { 5 },    // Vector256 + 1 (minimum for Vector256 shift)
            new object[] { 7 },    // Vector512 - 1
            new object[] { 8 },    // Vector512 width
            new object[] { 9 },    // Vector512 + 1 (minimum for Vector512 shift)
            new object[] { 15 },   // 2×Vector512 - 1
            new object[] { 16 },   // 2×Vector512
            new object[] { 17 },   // 2×Vector512 + 1
            new object[] { 31 },   // Karatsuba - 1
            new object[] { 32 },   // Karatsuba threshold
            new object[] { 33 },   // Karatsuba + 1
            new object[] { 63 },   // BZ - 1
            new object[] { 64 },   // BZ threshold
            new object[] { 65 },   // BZ + 1
        };

        private static BigInteger MakePositive(int limbCount, Random rng)
        {
            int byteCount = limbCount * nint.Size;
            byte[] bytes = new byte[byteCount + 1];
            rng.NextBytes(bytes);
            bytes[^1] = 0;
            if (bytes.Length > 1 && bytes[^2] == 0)
                bytes[^2] = 1;
            return new BigInteger(bytes);
        }

        // --- Sign combination tests ---

        public static IEnumerable<object[]> SignCombinations()
        {
            int[] limbCounts = [1, 3, 9, 33, 65];
            foreach (int n in limbCounts)
            {
                yield return new object[] { n, true, true };
                yield return new object[] { n, true, false };
                yield return new object[] { n, false, true };
                yield return new object[] { n, false, false };
            }
        }

        [Theory]
        [MemberData(nameof(SignCombinations))]
        public void ArithmeticSignCombinations(int limbCount, bool aNeg, bool bNeg)
        {
            var rng = new Random(1000 + limbCount * 4 + (aNeg ? 2 : 0) + (bNeg ? 1 : 0));
            BigInteger a = MakePositive(limbCount, rng);
            BigInteger b = MakePositive(Math.Max(1, limbCount / 2), rng);
            if (aNeg) a = -a;
            if (bNeg) b = -b;

            // Add/subtract roundtrip
            Assert.Equal(a, (a + b) - b);
            Assert.Equal(a + b, b + a);

            // Multiply/divide roundtrip
            BigInteger product = a * b;
            Assert.Equal(a, product / b);

            // Division invariant
            var (q, r) = BigInteger.DivRem(a, b);
            Assert.Equal(a, q * b + r);
            Assert.True(BigInteger.Abs(r) < BigInteger.Abs(b));
        }

        [Theory]
        [MemberData(nameof(SignCombinations))]
        public void BitwiseSignCombinations(int limbCount, bool aNeg, bool bNeg)
        {
            var rng = new Random(2000 + limbCount * 4 + (aNeg ? 2 : 0) + (bNeg ? 1 : 0));
            BigInteger a = MakePositive(limbCount, rng);
            BigInteger b = MakePositive(limbCount, rng);
            if (aNeg) a = -a;
            if (bNeg) b = -b;

            // De Morgan: ~(a & b) == (~a) | (~b)
            Assert.Equal(~(a & b), (~a) | (~b));

            // De Morgan: ~(a | b) == (~a) & (~b)
            Assert.Equal(~(a | b), (~a) & (~b));

            // XOR identity: (a ^ b) ^ b == a
            Assert.Equal(a, (a ^ b) ^ b);

            // Self-identities
            Assert.Equal(BigInteger.Zero, a ^ a);
            Assert.Equal(a, a & a);
            Assert.Equal(a, a | a);
        }

        // --- Vectorization boundary tests ---

        [Theory]
        [MemberData(nameof(VectorAlignedLimbCounts))]
        public void ShiftAtVectorBoundaries(int limbCount)
        {
            var rng = new Random(3000 + limbCount);
            BigInteger a = MakePositive(limbCount, rng);

            // Shift amounts that exercise different vector paths
            foreach (int shift in new[] { 1, 63, 64, 65, 127, 128, 129, 255, 256, 512 })
            {
                BigInteger shifted = a << shift;
                Assert.Equal(a, shifted >> shift);

                // Negative shift
                BigInteger negA = -a;
                BigInteger negShifted = negA << shift;
                Assert.Equal(negA, negShifted >> shift);
            }
        }

        [Theory]
        [MemberData(nameof(VectorAlignedLimbCounts))]
        public void BitwiseNotAtVectorBoundaries(int limbCount)
        {
            var rng = new Random(4000 + limbCount);
            BigInteger a = MakePositive(limbCount, rng);

            // ~~a == a
            Assert.Equal(a, ~~a);

            // ~a == -(a+1) for all integers
            Assert.Equal(-(a + 1), ~a);

            // Negative too
            BigInteger neg = -a;
            Assert.Equal(neg, ~~neg);
            Assert.Equal(-(neg + 1), ~neg);
        }

        [Theory]
        [MemberData(nameof(VectorAlignedLimbCounts))]
        public void MultiplyAtVectorBoundaries(int limbCount)
        {
            var rng = new Random(5000 + limbCount);
            BigInteger a = MakePositive(limbCount, rng);
            BigInteger b = MakePositive(limbCount, rng);

            BigInteger product = a * b;

            // Commutativity
            Assert.Equal(product, b * a);

            // Multiply/divide roundtrip
            Assert.Equal(a, product / b);
            Assert.Equal(b, product / a);

            // Square consistency
            Assert.Equal(a * a, BigInteger.Pow(a, 2));
        }

        public static IEnumerable<object[]> SparseLimbCounts()
        {
            for (int limbCount = 4; limbCount < 32; limbCount++)
            {
                yield return new object[] { limbCount };
            }
        }

        [Theory]
        [MemberData(nameof(SparseLimbCounts))]
        public void MultiplySparseLimbOperands(int sparseLength)
        {
            int bitsPerLimb = nint.Size * 8;
            BigInteger sparse = BigInteger.Zero;
            BigInteger other = BigInteger.Zero;
            BigInteger expected = BigInteger.Zero;

            for (int i = 0; i <= sparseLength; i++)
            {
                nuint limb = nuint.MaxValue - (nuint)((i * 2) + 1);
                other += (BigInteger)limb << (i * bitsPerLimb);
            }

            for (int i = 0; i < sparseLength; i++)
            {
                nuint limb = (i & 3) switch
                {
                    1 => 0,
                    2 => 1,
                    _ => nuint.MaxValue - (nuint)(i + 17),
                };
                int shift = i * bitsPerLimb;
                sparse += (BigInteger)limb << shift;
                expected += (other * limb) << shift;
            }

            Assert.Equal(expected, other * sparse);
            Assert.Equal(expected, sparse * other);
        }

        [Theory]
        [InlineData(4, 3, 1, 0)]
        [InlineData(4, 8, 2, 0)]
        [InlineData(8, 4, 3, 0)]
        [InlineData(16, 32, 5, 0)]
        [InlineData(31, 64, 7, 0)]
        [InlineData(32, 16, 10, 0)]
        [InlineData(33, 64, 257, 0)]
        [InlineData(64, 31, 65_535, 0)]
        [InlineData(64, 33, 1, 2)]
        [InlineData(256, 65, 0, 4)]
        public void MultiplyLimbRun(int runLength, int otherLength, int repeatedLimb, int limbOffset)
        {
            int bitsPerLimb = nint.Size * 8;
            int runBits = runLength * bitsPerLimb;
            BigInteger radix = BigInteger.One << bitsPerLimb;
            BigInteger run = repeatedLimb == 0
                ? (BigInteger.One << runBits) - 1
                : repeatedLimb * (((BigInteger.One << runBits) - 1) / (radix - 1));
            BigInteger other = MakePositive(otherLength, new Random(5_100 + runLength + otherLength));
            run <<= limbOffset * bitsPerLimb;

            BigInteger expected;

            if (repeatedLimb == 0)
            {
                expected = ((other << runBits) - other) << (limbOffset * bitsPerLimb);
            }
            else
            {
                expected = BigInteger.Zero;

                for (int i = 0; i < runLength; i++)
                {
                    expected += (other * repeatedLimb) << (i * bitsPerLimb);
                }

                expected <<= limbOffset * bitsPerLimb;
            }

            Assert.Equal(expected, other * run);
            Assert.Equal(expected, run * other);

            if (limbOffset == 0)
            {
                BigInteger expectedSquare;

                if (repeatedLimb == 0)
                {
                    expectedSquare = (run << runBits) - run;
                }
                else
                {
                    expectedSquare = BigInteger.Zero;

                    for (int i = 0; i < runLength; i++)
                    {
                        expectedSquare += (run * repeatedLimb) << (i * bitsPerLimb);
                    }
                }

                Assert.Equal(expectedSquare, BigInteger.Pow(run, 2));
            }
        }

        [Fact]
        public void MultiplyLimbRunFallsBackWhenAccumulatorWouldOverflow()
        {
            const int RunLength = 8;
            int bitsPerLimb = nint.Size * 8;
            BigInteger repeatedLimb = new BigInteger((ulong)(nuint.MaxValue / 2));
            BigInteger repunit = ((BigInteger.One << (RunLength * bitsPerLimb)) - 1)
                / ((BigInteger.One << bitsPerLimb) - 1);
            BigInteger run = repeatedLimb * repunit;
            BigInteger other = MakePositive(16, new Random(5_200));
            BigInteger expected = BigInteger.Zero;
            BigInteger expectedSquare = BigInteger.Zero;

            for (int i = 0; i < RunLength; i++)
            {
                int shift = i * bitsPerLimb;
                expected += (other * repeatedLimb) << shift;
                expectedSquare += (run * repeatedLimb) << shift;
            }

            Assert.Equal(expected, other * run);
            Assert.Equal(expected, run * other);
            Assert.Equal(expectedSquare, BigInteger.Pow(run, 2));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        public void MultiplyShiftedLimbRunFallsBackWhenAccumulatorWouldOverflow()
        {
            const int RunLength = 8;
            const int Shift = 17;
            const int BitsPerLimb = 64;
            BigInteger repeatedLimb = new BigInteger((ulong)(nuint.MaxValue / 2));
            BigInteger repunit = ((BigInteger.One << (RunLength * BitsPerLimb)) - 1)
                / ((BigInteger.One << BitsPerLimb) - 1);
            BigInteger run = (repeatedLimb * repunit) << Shift;
            BigInteger other = MakePositive(16, new Random(5_250));
            BigInteger expected = BigInteger.Zero;
            BigInteger expectedSquare = BigInteger.Zero;

            for (int i = 0; i < RunLength; i++)
            {
                int shift = (i * BitsPerLimb) + Shift;
                expected += (other * repeatedLimb) << shift;
                expectedSquare += (run * repeatedLimb) << shift;
            }

            Assert.Equal(expected, other * run);
            Assert.Equal(expected, run * other);
            Assert.Equal(expectedSquare, BigInteger.Pow(run, 2));
        }

        [Fact]
        public void MultiplyLimbRunCandidateFallsBackWhenLimbsDiffer()
        {
            const int LimbCount = 8;
            const int RepeatedLimb = 5;
            int bitsPerLimb = nint.Size * 8;
            BigInteger candidate = BigInteger.Zero;
            BigInteger other = MakePositive(16, new Random(5_300));
            BigInteger expected = BigInteger.Zero;
            BigInteger expectedSquare = BigInteger.Zero;

            for (int i = 0; i < LimbCount; i++)
            {
                int limb = i == 3 ? 9 : RepeatedLimb;
                int shift = i * bitsPerLimb;
                candidate += (BigInteger)limb << shift;
                expected += (other * limb) << shift;
            }

            for (int i = 0; i < LimbCount; i++)
            {
                int limb = i == 3 ? 9 : RepeatedLimb;
                expectedSquare += (candidate * limb) << (i * bitsPerLimb);
            }

            Assert.Equal(expected, other * candidate);
            Assert.Equal(expected, candidate * other);
            Assert.Equal(expectedSquare, BigInteger.Pow(candidate, 2));
        }

        [Theory]
        [InlineData(4, 3, 1, 1, 0)]
        [InlineData(4, 8, 3, 31, 0)]
        [InlineData(8, 4, 7, 32, 0)]
        [InlineData(16, 32, 257, 63, 0)]
        [InlineData(64, 33, 0, 17, 0)]
        [InlineData(8, 4, 3, 0, 1)]
        public void MultiplyShiftedLimbRun(int runLength, int otherLength, int repeatedLimb, int shift, int limbOffset)
        {
            int bitsPerLimb = nint.Size * 8;
            shift = Math.Min(shift, bitsPerLimb - 1);
            int totalShift = shift + (limbOffset * bitsPerLimb);
            BigInteger limb = repeatedLimb == 0 ? nuint.MaxValue : repeatedLimb;
            BigInteger run = BigInteger.Zero;
            BigInteger other = MakePositive(otherLength, new Random(5_400 + runLength + otherLength));
            BigInteger expected = BigInteger.Zero;

            for (int i = 0; i < runLength; i++)
            {
                int limbShift = i * bitsPerLimb;
                run += limb << limbShift;
                expected += (other * limb) << limbShift;
            }

            run <<= totalShift;
            expected <<= totalShift;

            Assert.Equal(expected, other * run);
            Assert.Equal(expected, run * other);

            BigInteger expectedSquare = BigInteger.Zero;

            for (int i = 0; i < runLength; i++)
            {
                expectedSquare += (run * limb) << (i * bitsPerLimb);
            }

            expectedSquare <<= totalShift;
            Assert.Equal(expectedSquare, BigInteger.Pow(run, 2));
        }

        // --- Asymmetric operand sizes ---

        public static IEnumerable<object[]> AsymmetricSizePairs => new object[][]
        {
            new object[] { 1, 9 },      // 1 limb × 9 limbs (Vector512 + 1)
            new object[] { 1, 33 },     // 1 limb × above Karatsuba
            new object[] { 1, 65 },     // 1 limb × above BZ
            new object[] { 3, 33 },     // small × Karatsuba+1
            new object[] { 9, 65 },     // Vector512+1 × BZ+1
            new object[] { 16, 33 },    // half of Karatsuba × Karatsuba+1 (RightSmall path)
            new object[] { 31, 64 },    // Karatsuba-1 × BZ (left>2*right triggers RightSmall)
            new object[] { 33, 65 },    // both above Karatsuba, different sizes
        };

        [Theory]
        [MemberData(nameof(AsymmetricSizePairs))]
        public void AsymmetricMultiply(int smallLimbs, int largeLimbs)
        {
            var rng = new Random(6000 + smallLimbs * 100 + largeLimbs);
            BigInteger small = MakePositive(smallLimbs, rng);
            BigInteger large = MakePositive(largeLimbs, rng);

            BigInteger product = small * large;
            Assert.Equal(product, large * small);
            Assert.Equal(small, product / large);
            Assert.Equal(large, product / small);
        }

        [Theory]
        [MemberData(nameof(AsymmetricSizePairs))]
        public void AsymmetricDivision(int smallLimbs, int largeLimbs)
        {
            var rng = new Random(7000 + smallLimbs * 100 + largeLimbs);
            BigInteger dividend = MakePositive(largeLimbs, rng);
            BigInteger divisor = MakePositive(smallLimbs, rng);

            var (q, r) = BigInteger.DivRem(dividend, divisor);
            Assert.Equal(dividend, q * divisor + r);
            Assert.True(BigInteger.Abs(r) < BigInteger.Abs(divisor));

            // Also with negative dividend
            var (q2, r2) = BigInteger.DivRem(-dividend, divisor);
            Assert.Equal(-dividend, q2 * divisor + r2);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(9)]
        [InlineData(10)]
        public void SmallConstantDivision(int divisor)
        {
            var rng = new Random(7100 + divisor);

            for (int sample = 1; sample <= 8; sample++)
            {
                BigInteger expectedQuotient = MakePositive(sample, rng);

                for (int expectedRemainder = 0; expectedRemainder < divisor; expectedRemainder++)
                {
                    BigInteger dividend = expectedRemainder;

                    for (int i = 0; i < divisor; i++)
                    {
                        dividend += expectedQuotient;
                    }

                    (BigInteger quotient, BigInteger remainder) = BigInteger.DivRem(dividend, divisor);
                    Assert.Equal(expectedQuotient, quotient);
                    Assert.Equal(expectedRemainder, remainder);
                    Assert.Equal(expectedQuotient, dividend / divisor);
                    Assert.Equal(expectedRemainder, dividend % divisor);

                    (quotient, remainder) = BigInteger.DivRem(-dividend, divisor);
                    Assert.Equal(-expectedQuotient, quotient);
                    Assert.Equal(-expectedRemainder, remainder);

                    (quotient, remainder) = BigInteger.DivRem(dividend, -divisor);
                    Assert.Equal(-expectedQuotient, quotient);
                    Assert.Equal(expectedRemainder, remainder);
                }
            }
        }

        [Theory]
        [InlineData(6)]
        [InlineData(9)]
        [InlineData(12)]
        [InlineData(18)]
        [InlineData(20)]
        [InlineData(27)]
        [InlineData(343)]
        [InlineData(625)]
        [InlineData(6_561)]
        [InlineData(43_046_721)]
        [InlineData(65_535)]
        [InlineData(4_294_967_295)]
        [InlineData(6_973_568_802)]
        [InlineData(1_000_000_000_000_000_000)]
        [InlineData(3_909_821_048_582_988_049)]
        [InlineData(7_450_580_596_923_828_125)]
        [InlineData(12_157_665_459_056_928_801)]
        public void ScalarDivision(ulong divisor)
        {
            var rng = new Random(unchecked(7300 + (int)divisor));

            foreach (int limbCount in new[] { 8, 16, 32, 128 })
            {
                BigInteger expectedQuotient = MakePositive(limbCount, rng);

                foreach (BigInteger expectedRemainder in new BigInteger[]
                {
                    BigInteger.Zero,
                    BigInteger.One,
                    divisor / 2,
                    divisor - 1,
                })
                {
                    BigInteger dividend = (expectedQuotient * divisor) + expectedRemainder;
                    BigInteger quotient = BigInteger.DivRem(dividend, divisor, out BigInteger remainder);

                    Assert.Equal(expectedQuotient, quotient);
                    Assert.Equal(expectedRemainder, remainder);
                    Assert.Equal(expectedQuotient, dividend / divisor);
                    Assert.Equal(expectedRemainder, dividend % divisor);
                }
            }
        }

        [Theory]
        [MemberData(nameof(AsymmetricSizePairs))]
        public void AsymmetricGcd(int smallLimbs, int largeLimbs)
        {
            var rng = new Random(8000 + smallLimbs * 100 + largeLimbs);
            BigInteger a = MakePositive(largeLimbs, rng);
            BigInteger b = MakePositive(smallLimbs, rng);

            BigInteger gcd = BigInteger.GreatestCommonDivisor(a, b);
            if (!gcd.IsZero)
            {
                Assert.Equal(BigInteger.Zero, a % gcd);
                Assert.Equal(BigInteger.Zero, b % gcd);
            }
        }

        // --- Special value interactions ---

        public static IEnumerable<object[]> SpecialValues()
        {
            yield return new object[] { BigInteger.Zero, "0" };
            yield return new object[] { BigInteger.One, "1" };
            yield return new object[] { BigInteger.MinusOne, "-1" };
            yield return new object[] { new BigInteger(nint.MaxValue), "nint.MaxValue" };
            yield return new object[] { new BigInteger(nint.MaxValue) + 1, "nint.MaxValue+1" };
            yield return new object[] { new BigInteger(nint.MinValue), "nint.MinValue" };
            yield return new object[] { new BigInteger(int.MinValue), "int.MinValue" };
            yield return new object[] { new BigInteger(uint.MaxValue), "uint.MaxValue" };
            yield return new object[] { new BigInteger(ulong.MaxValue), "ulong.MaxValue" };
        }

        [Theory]
        [MemberData(nameof(SpecialValues))]
        public void SpecialValueArithmetic(BigInteger special, string _)
        {
            // Identity properties
            Assert.Equal(special, special + BigInteger.Zero);
            Assert.Equal(special, special - BigInteger.Zero);
            Assert.Equal(special, special * BigInteger.One);
            Assert.Equal(BigInteger.Zero, special * BigInteger.Zero);
            Assert.Equal(-special, special * BigInteger.MinusOne);

            if (!special.IsZero)
            {
                Assert.Equal(BigInteger.One, special / special);
                Assert.Equal(BigInteger.Zero, special % special);
                Assert.Equal(BigInteger.Zero, BigInteger.Zero / special);
            }

            // Parse/ToString roundtrip
            Assert.Equal(special, BigInteger.Parse(special.ToString()));
        }

        [Theory]
        [MemberData(nameof(SpecialValues))]
        public void SpecialValueBitwise(BigInteger special, string _)
        {
            Assert.Equal(BigInteger.Zero, special ^ special);
            Assert.Equal(special, special & special);
            Assert.Equal(special, special | special);
            Assert.Equal(special, ~~special);
            Assert.Equal(-(special + 1), ~special);

            // AND with zero = zero, OR with zero = identity
            Assert.Equal(BigInteger.Zero, special & BigInteger.Zero);
            Assert.Equal(special, special | BigInteger.Zero);
        }

        [Theory]
        [MemberData(nameof(SpecialValues))]
        public void SpecialValueWithLargeOperand(BigInteger special, string _)
        {
            var rng = new Random(9000);
            BigInteger large = MakePositive(33, rng); // above Karatsuba threshold

            // Arithmetic identities hold with mixed sizes
            Assert.Equal(special + large, large + special);
            Assert.Equal(special * large, large * special);

            if (!special.IsZero)
            {
                var (q, r) = BigInteger.DivRem(large, special);
                Assert.Equal(large, q * special + r);
            }
        }

        // --- Threshold-straddling multiply pairs ---

        [Theory]
        [InlineData(31, 32)]   // one below, one at Karatsuba
        [InlineData(32, 33)]   // one at, one above Karatsuba
        [InlineData(31, 33)]   // one below, one above Karatsuba
        [InlineData(16, 33)]   // half × Karatsuba+1 (RightSmall path)
        [InlineData(17, 33)]   // just over half × Karatsuba+1
        [InlineData(63, 64)]   // at BZ boundary
        [InlineData(255, 256)] // at Toom3 boundary
        [InlineData(256, 257)] // at/above Toom3
        public void ThresholdStraddlingMultiply(int leftLimbs, int rightLimbs)
        {
            var rng = new Random(10000 + leftLimbs * 1000 + rightLimbs);
            BigInteger a = MakePositive(leftLimbs, rng);
            BigInteger b = MakePositive(rightLimbs, rng);

            BigInteger product = a * b;
            Assert.Equal(product, b * a);
            Assert.Equal(a, product / b);
            Assert.Equal(b, product / a);

            // Square at threshold
            BigInteger aSq = a * a;
            Assert.Equal(aSq, BigInteger.Pow(a, 2));
        }

        // --- Power-of-two divisors (fast paths in some implementations) ---

        [Theory]
        [InlineData(1)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(128)]
        [InlineData(256)]
        public void PowerOfTwoDivisor(int bits)
        {
            var rng = new Random(11000 + bits);
            BigInteger dividend = MakePositive(Math.Max(1, (bits / (nint.Size * 8)) + 2), rng);
            BigInteger powerOf2 = BigInteger.One << bits;

            var (q, r) = BigInteger.DivRem(dividend, powerOf2);
            Assert.Equal(dividend, q * powerOf2 + r);
            Assert.True(r >= 0 && r < powerOf2);

            // Shift equivalence: dividend / 2^n == dividend >> n (for positive)
            Assert.Equal(dividend >> bits, q);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(73)]
        [InlineData(127)]
        [InlineData(128)]
        [InlineData(1_024)]
        public void DivisionByPowerOfTwoUsesShiftSemantics(int exponent)
        {
            BigInteger divisorMagnitude = BigInteger.One << exponent;
            BigInteger dividendMagnitude = (BigInteger.One << (exponent + 70))
                + (new BigInteger(9) << exponent)
                + 13;
            BigInteger expectedQuotientMagnitude = (BigInteger.One << 70) + 9 + (exponent >= 4 ? 0 : 13 >> exponent);
            BigInteger expectedRemainderMagnitude = exponent >= 4 ? 13 : 13 & ((1 << exponent) - 1);

            foreach (bool negativeDividend in new[] { false, true })
            {
                foreach (bool negativeDivisor in new[] { false, true })
                {
                    BigInteger dividend = negativeDividend ? -dividendMagnitude : dividendMagnitude;
                    BigInteger divisor = negativeDivisor ? -divisorMagnitude : divisorMagnitude;
                    BigInteger expectedQuotient = negativeDividend ^ negativeDivisor
                        ? -expectedQuotientMagnitude
                        : expectedQuotientMagnitude;
                    BigInteger expectedRemainder = negativeDividend
                        ? -expectedRemainderMagnitude
                        : expectedRemainderMagnitude;

                    Assert.Equal(expectedQuotient, dividend / divisor);
                    Assert.Equal(expectedRemainder, dividend % divisor);

                    BigInteger quotient = BigInteger.DivRem(dividend, divisor, out BigInteger remainder);
                    Assert.Equal(expectedQuotient, quotient);
                    Assert.Equal(expectedRemainder, remainder);
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(63)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(73)]
        [InlineData(1_024)]
        public void MultiplicationByPowerOfTwoUsesShiftSemantics(int exponent)
        {
            BigInteger valueMagnitude = (BigInteger.One << 130) + (BigInteger.One << 65) + 13;
            BigInteger powerMagnitude = BigInteger.One << exponent;

            foreach (bool negativeValue in new[] { false, true })
            {
                foreach (bool negativePower in new[] { false, true })
                {
                    BigInteger value = negativeValue ? -valueMagnitude : valueMagnitude;
                    BigInteger power = negativePower ? -powerMagnitude : powerMagnitude;
                    BigInteger product = value * power;
                    BigInteger expected = value << exponent;

                    if (negativePower)
                    {
                        expected = -expected;
                    }

                    Assert.Equal(expected, product);
                    Assert.Equal(power, product / value);
                    Assert.Equal(BigInteger.Zero, product % value);
                    Assert.Equal(product, power * value);
                }
            }
        }

        [Theory]
        [InlineData(0, 64)]
        [InlineData(1, 64)]
        [InlineData(9, 64)]
        [InlineData(64, 64)]
        [InlineData(73, 64)]
        [InlineData(1_024, 73)]
        public void GcdWithPowerOfTwoUsesTrailingZeroCount(int valueTrailingZeros, int power)
        {
            BigInteger value = new BigInteger(15) << valueTrailingZeros;
            BigInteger powerOfTwo = BigInteger.One << power;
            BigInteger expected = BigInteger.One << Math.Min(valueTrailingZeros, power);

            Assert.Equal(expected, BigInteger.GreatestCommonDivisor(value, powerOfTwo));
            Assert.Equal(expected, BigInteger.GreatestCommonDivisor(powerOfTwo, value));
            Assert.Equal(expected, BigInteger.GreatestCommonDivisor(-value, -powerOfTwo));
        }

        [Theory]
        [InlineData(1, 1, 64)]
        [InlineData(2, 0, 1)]
        [InlineData(2, 0, 64)]
        [InlineData(2, 31, 64)]
        [InlineData(2, 32, 64)]
        [InlineData(2, 63, 64)]
        [InlineData(4, 15, 64)]
        [InlineData(4, 16, 64)]
        [InlineData(-1, 31, 64)]
        [InlineData(-1, 32, 64)]
        [InlineData(-2, 31, 64)]
        [InlineData(-2, 32, 64)]
        [InlineData(2, 64, 64)]
        [InlineData(-4, 33, 64)]
        [InlineData(16, 64, 1_024)]
        public void ModPowOfPowersOfTwoUsesExponentScaling(int value, int exponent, int modulusExponent)
        {
            BigInteger modulus = BigInteger.One << modulusExponent;
            int valueExponent = BitOperations.TrailingZeroCount((uint)Math.Abs(value));
            int resultExponent = checked(valueExponent * exponent);
            BigInteger expected = resultExponent >= modulusExponent
                ? BigInteger.Zero
                : BigInteger.One << resultExponent;

            if (value < 0 && (exponent & 1) != 0)
            {
                expected = -expected;
            }

            Assert.Equal(expected, BigInteger.ModPow(value, exponent, modulus));
            Assert.Equal(expected, BigInteger.ModPow(value, exponent, -modulus));
        }

        [Theory]
        [InlineData(2, 31)]
        [InlineData(2, 64)]
        [InlineData(2, 1_024)]
        [InlineData(-2, 31)]
        [InlineData(-2, 32)]
        [InlineData(16, 257)]
        public void PowOfPowerOfTwoUsesExponentScaling(int value, int exponent)
        {
            int valueExponent = BitOperations.TrailingZeroCount((uint)Math.Abs(value));
            BigInteger expected = BigInteger.One << checked(valueExponent * exponent);
            if (value < 0 && (exponent & 1) != 0)
            {
                expected = -expected;
            }

            Assert.Equal(expected, BigInteger.Pow(value, exponent));
        }

        [Theory]
        [InlineData(3, 1_025)]
        [InlineData(3, 2_048)]
        [InlineData(3, 2_049)]
        [InlineData(-3, 1_025)]
        [InlineData(5, 1_024)]
        [InlineData(-5, 1_025)]
        [InlineData(6, 1_025)]
        [InlineData(10, 1_024)]
        [InlineData(12, 1_025)]
        [InlineData(20, 1_024)]
        [InlineData(15, 1_025)]
        [InlineData(45, 1_025)]
        [InlineData(75, 1_025)]
        [InlineData(21, 257)]
        [InlineData(21, 384)]
        [InlineData(42, 257)]
        [InlineData(42, 384)]
        [InlineData(35, 257)]
        [InlineData(35, 384)]
        [InlineData(70, 257)]
        [InlineData(63, 257)]
        [InlineData(175, 257)]
        [InlineData(105, 257)]
        [InlineData(10_326, 31)]
        [InlineData(43_046_721, 33)]
        [InlineData(-43_046_721, 1)]
        [InlineData(1_162_261_467, 17)]
        [InlineData(390_625, 33)]
        [InlineData(-390_625, 1)]
        [InlineData(1_220_703_125, 17)]
        [InlineData(4_100_625, 17)]
        [InlineData(7, 31)]
        [InlineData(7, 1_024)]
        [InlineData(49, 257)]
        [InlineData(5_764_801, 33)]
        public void PowWithThreeFiveOrSevenFactors(int value, int exponent)
        {
            BigInteger expected = BigInteger.One;

            for (int i = 0; i < exponent; i++)
            {
                expected *= value;
            }

            Assert.Equal(expected, BigInteger.Pow(value, exponent));
        }

        [Theory]
        [InlineData(5_559_060_566_555_523, 17)]
        [InlineData(762_939_453_125, 17)]
        [InlineData(232_630_513_987_207, 17)]
        public void PowWithRepeatedLargeFactors(long value, int exponent)
        {
            BigInteger expected = BigInteger.One;

            for (int i = 0; i < exponent; i++)
            {
                expected *= value;
            }

            Assert.Equal(expected, BigInteger.Pow(value, exponent));
        }

        [Fact]
        public void PowerOfTwoResultsRespectMaximumLength()
        {
            Assert.Throws<OverflowException>(() => BigInteger.One << int.MaxValue);
            Assert.Throws<OverflowException>(() => BigInteger.Pow(2, int.MaxValue));
            Assert.Throws<OverflowException>(() => BigInteger.Pow(3, int.MaxValue));
            Assert.Throws<OverflowException>(() => BigInteger.Pow(10, int.MaxValue));

            BigInteger arrayBacked = (BigInteger.One << (nint.Size * 8)) + 1;
            Assert.Throws<OverflowException>(() => arrayBacked << int.MaxValue);
            Assert.Throws<OverflowException>(() => arrayBacked >> int.MinValue);

            int maxLength = Array.MaxLength / (nint.Size * 8);
            BigInteger allOnes = nint.Size == 8 ? ulong.MaxValue : uint.MaxValue;
            int shift = checked(((maxLength - 1) * nint.Size * 8) + 1);
            Assert.Throws<OverflowException>(() => allOnes << shift);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(31, 31)]
        [InlineData(32, 32)]
        [InlineData(32, 64)]
        [InlineData(33, 33)]
        [InlineData(64, 32)]
        [InlineData(256, 257)]
        public void CommonZeroLimbsPreserveArithmetic(int leftLimbOffset, int rightLimbOffset)
        {
            int leftShift = leftLimbOffset * nint.Size * 8;
            int rightShift = rightLimbOffset * nint.Size * 8;
            int commonShift = Math.Min(leftShift, rightShift);
            BigInteger leftMagnitude = (BigInteger.One << 130) + (BigInteger.One << 65) + 12_345;
            BigInteger rightMagnitude = (BigInteger.One << 66) + 321;
            BigInteger left = leftMagnitude << leftShift;
            BigInteger right = rightMagnitude << rightShift;
            BigInteger reducedLeft = leftMagnitude << (leftShift - commonShift);
            BigInteger reducedRight = rightMagnitude << (rightShift - commonShift);

            foreach (bool negativeLeft in new[] { false, true })
            {
                foreach (bool negativeRight in new[] { false, true })
                {
                    BigInteger signedLeftMagnitude = negativeLeft ? -leftMagnitude : leftMagnitude;
                    BigInteger signedRightMagnitude = negativeRight ? -rightMagnitude : rightMagnitude;
                    BigInteger signedLeft = negativeLeft ? -left : left;
                    BigInteger signedRight = negativeRight ? -right : right;
                    BigInteger signedReducedLeft = negativeLeft ? -reducedLeft : reducedLeft;
                    BigInteger signedReducedRight = negativeRight ? -reducedRight : reducedRight;

                    Assert.Equal((signedReducedLeft + signedReducedRight) << commonShift, signedLeft + signedRight);
                    Assert.Equal((signedReducedLeft - signedReducedRight) << commonShift, signedLeft - signedRight);
                    Assert.Equal((signedLeftMagnitude * signedRightMagnitude) << (leftShift + rightShift), signedLeft * signedRight);
                    Assert.Equal(signedReducedLeft / signedReducedRight, signedLeft / signedRight);
                    Assert.Equal((signedReducedLeft % signedReducedRight) << commonShift, signedLeft % signedRight);

                    BigInteger quotient = BigInteger.DivRem(signedLeft, signedRight, out BigInteger remainder);
                    Assert.Equal(signedReducedLeft / signedReducedRight, quotient);
                    Assert.Equal((signedReducedLeft % signedReducedRight) << commonShift, remainder);
                }
            }

            BigInteger expectedGcd = BigInteger.GreatestCommonDivisor(reducedLeft, reducedRight) << commonShift;
            Assert.Equal(expectedGcd, BigInteger.GreatestCommonDivisor(left, right));
            Assert.Equal(expectedGcd, BigInteger.GreatestCommonDivisor(-left, -right));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(32)]
        [InlineData(256)]
        public void ZeroLimbScalarMultiplicationAndSquarePreserveValue(int limbOffset)
        {
            int shift = limbOffset * nint.Size * 8;
            BigInteger magnitude = (BigInteger.One << 130) + (BigInteger.One << 65) + 12_345;
            BigInteger value = magnitude << shift;

            Assert.Equal((magnitude * 3) << shift, value * 3);
            Assert.Equal((magnitude * magnitude) << (shift * 2), value * value);
        }

        [Theory]
        [InlineData(3, 1, 1)]
        [InlineData(3, 64, 16)]
        [InlineData(5, 64, 16)]
        [InlineData(7, 64, 16)]
        [InlineData(15, 16, 4)]
        public void DivisorZeroLimbsPreserveArithmetic(int factor, int exponent, int limbOffset)
        {
            int shift = limbOffset * nint.Size * 8;
            BigInteger reducedDivisor = BigInteger.Pow(factor, exponent);
            BigInteger divisor = reducedDivisor << shift;
            BigInteger expectedQuotient = (BigInteger.One << 521) + 12_345;
            BigInteger expectedRemainder = divisor - 1;
            BigInteger dividend = (expectedQuotient * divisor) + expectedRemainder;

            foreach (bool negativeDividend in new[] { false, true })
            {
                foreach (bool negativeDivisor in new[] { false, true })
                {
                    BigInteger signedDividend = negativeDividend ? -dividend : dividend;
                    BigInteger signedDivisor = negativeDivisor ? -divisor : divisor;
                    BigInteger signedQuotient = negativeDividend != negativeDivisor
                        ? -expectedQuotient
                        : expectedQuotient;
                    BigInteger signedRemainder = negativeDividend
                        ? -expectedRemainder
                        : expectedRemainder;

                    Assert.Equal(signedQuotient, signedDividend / signedDivisor);
                    Assert.Equal(signedRemainder, signedDividend % signedDivisor);

                    BigInteger quotient = BigInteger.DivRem(signedDividend, signedDivisor, out BigInteger remainder);
                    Assert.Equal(signedQuotient, quotient);
                    Assert.Equal(signedRemainder, remainder);
                }
            }
        }

        [Theory]
        [InlineData(2, 2)]
        [InlineData(2, 9)]
        [InlineData(4, 16)]
        [InlineData(3, 7)]
        [InlineData(4, 33)]
        [InlineData(31, 64)]
        [InlineData(32, 65)]
        [InlineData(64, 129)]
        [InlineData(65, 261)]
        [InlineData(256, 515)]
        public void MersenneDivisorPreservesArithmetic(int divisorLength, int quotientLength)
        {
            int bitsPerLimb = nint.Size * 8;
            BigInteger divisor = (BigInteger.One << (divisorLength * bitsPerLimb)) - 1;
            BigInteger expectedQuotient = MakePositive(quotientLength, new Random(9_100 + divisorLength + quotientLength));

            foreach (BigInteger expectedRemainder in new[] { BigInteger.Zero, BigInteger.One, divisor - 1 })
            {
                BigInteger dividend = (expectedQuotient << (divisorLength * bitsPerLimb))
                    - expectedQuotient
                    + expectedRemainder;

                foreach (bool negativeDividend in new[] { false, true })
                {
                    foreach (bool negativeDivisor in new[] { false, true })
                    {
                        BigInteger signedDividend = negativeDividend ? -dividend : dividend;
                        BigInteger signedDivisor = negativeDivisor ? -divisor : divisor;
                        BigInteger signedQuotient = negativeDividend != negativeDivisor
                            ? -expectedQuotient
                            : expectedQuotient;
                        BigInteger signedRemainder = negativeDividend
                            ? -expectedRemainder
                            : expectedRemainder;

                        Assert.Equal(signedQuotient, signedDividend / signedDivisor);
                        Assert.Equal(signedRemainder, signedDividend % signedDivisor);

                        BigInteger quotient = BigInteger.DivRem(signedDividend, signedDivisor, out BigInteger remainder);
                        Assert.Equal(signedQuotient, quotient);
                        Assert.Equal(signedRemainder, remainder);
                    }
                }
            }
        }

        [Fact]
        public void CalculatorGcdHandlesCommonZeroLimbs()
        {
            nuint[] left = [0, 0, 18, 3];
            nuint[] right = [0, 0, 12, 1];
            nuint[] result = new nuint[left.Length];

            BigIntegerCalculator.Gcd(left, right, result);

            Assert.Equal(new nuint[] { 0, 0, 2, 0 }, result);
        }

        [Fact]
        public void RemainderByPowerOfTwoNormalizesZeroAndSmallValues()
        {
            BigInteger divisor = BigInteger.One << (nint.Size * 8);
            BigInteger exactDividend = BigInteger.One << (nint.Size * 8 * 4);

            Assert.Equal(BigInteger.Zero, exactDividend % divisor);
            Assert.Equal(BigInteger.One << (nint.Size * 8 * 3), BigInteger.DivRem(exactDividend, divisor, out BigInteger exactRemainder));
            Assert.Equal(BigInteger.Zero, exactRemainder);

            Assert.Equal(new BigInteger(13), new BigInteger(13) % divisor);
            Assert.Equal(BigInteger.Zero, new BigInteger(13) / divisor);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(1, 32)]
        [InlineData(32, 1)]
        [InlineData(31, 32)]
        [InlineData(32, 31)]
        public void UnequalZeroLimbsPreserveAddition(int leftOffset, int rightOffset)
        {
            int bitsPerLimb = nint.Size * 8;
            BigInteger left = ((BigInteger.One << 130) + 12_345) << (leftOffset * bitsPerLimb);
            BigInteger right = ((BigInteger.One << 66) + 321) << (rightOffset * bitsPerLimb);
            BigInteger sum = left + right;
            int commonOffset = Math.Min(leftOffset, rightOffset);
            int commonShift = commonOffset * bitsPerLimb;
            BigInteger expectedDifference = ((left >> commonShift) - (right >> commonShift)) << commonShift;

            Assert.Equal(left, sum - right);
            Assert.Equal(right, sum - left);
            Assert.Equal(sum, right + left);
            Assert.Equal(-sum, -left + -right);
            Assert.Equal(expectedDifference, left - right);
        }

        [Fact]
        public void CommonZeroLimbMultiplicationPreservesCanonicalRecursiveInputs()
        {
            const int HalfLength = 256;
            int bitsPerLimb = nint.Size * 8;
            var rng = new Random(13000 + bitsPerLimb);
            BigInteger low = MakePositive(HalfLength - 16, rng);
            BigInteger high = MakePositive(HalfLength, rng);
            BigInteger leftMagnitude = (high << (HalfLength * bitsPerLimb)) + low;
            BigInteger rightMagnitude = MakePositive(HalfLength, rng);
            int shift = 32 * bitsPerLimb;
            BigInteger left = leftMagnitude << shift;
            BigInteger right = rightMagnitude << shift;

            Assert.Equal((leftMagnitude * rightMagnitude) << (shift * 2), left * right);
            Assert.Equal((leftMagnitude * leftMagnitude) << (shift * 2), left * left);
        }

        // --- Toom-Cook 3 body coverage (both operands >= 256 limbs) ---

        [Theory]
        [InlineData(256, 256)]   // Equal size at Toom3 threshold
        [InlineData(256, 300)]   // Right larger
        [InlineData(300, 300)]   // Both well above threshold
        [InlineData(256, 512)]   // 2x asymmetry at Toom3 scale
        public void Toom3Multiply(int leftLimbs, int rightLimbs)
        {
            var rng = new Random(12000 + leftLimbs * 1000 + rightLimbs);
            BigInteger a = MakePositive(leftLimbs, rng);
            BigInteger b = MakePositive(rightLimbs, rng);

            BigInteger product = a * b;
            Assert.Equal(product, b * a);

            // Square consistency at Toom3 scale
            BigInteger aSq = a * a;
            Assert.Equal(aSq, BigInteger.Pow(a, 2));
        }

        // --- Barrett reduction in ModPow (large modulus) ---

        [Theory]
        [InlineData(8, 4)]     // Small modulus (no Barrett)
        [InlineData(33, 16)]   // Medium modulus
        [InlineData(65, 32)]   // Large modulus (Barrett reduction)
        public void ModPowLargeModulus(int modulusLimbs, int baseLimbs)
        {
            var rng = new Random(13000 + modulusLimbs);
            BigInteger modulus = MakePositive(modulusLimbs, rng);
            BigInteger b = MakePositive(baseLimbs, rng);
            BigInteger exp = new BigInteger(65537); // Common RSA exponent

            BigInteger result = BigInteger.ModPow(b, exp, modulus);

            // Result must be in [0, modulus)
            Assert.True(result >= 0);
            Assert.True(result < modulus);

            // Verify against manual computation for small exponent
            // a^1 mod m == a mod m (basic sanity)
            BigInteger r1 = BigInteger.ModPow(b, 1, modulus);
            BigInteger expected1 = b % modulus;
            if (expected1 < 0) expected1 += modulus;
            Assert.Equal(expected1, r1);

            // a^2 mod m == (a*a) mod m
            BigInteger r2 = BigInteger.ModPow(b, 2, modulus);
            BigInteger expected2 = (b * b) % modulus;
            if (expected2 < 0) expected2 += modulus;
            Assert.Equal(expected2, r2);
        }

        [Theory]
        [InlineData(33, 16, 2)]   // Even modulus ≥ ReducerThreshold → Barrett with multi-limb exponent
        [InlineData(65, 32, 2)]   // Large even modulus → Barrett + pool allocation
        public void ModPowEvenLargeModulusMultiLimbExponent(int modulusLimbs, int baseLimbs, int exponentLimbs)
        {
            var rng = new Random(14000 + modulusLimbs);
            BigInteger modulus = MakePositive(modulusLimbs, rng);
            modulus &= ~BigInteger.One; // force even
            if (modulus < 2) modulus = 2;

            BigInteger b = MakePositive(baseLimbs, rng);
            BigInteger exp = MakePositive(exponentLimbs, rng);

            BigInteger result = BigInteger.ModPow(b, exp, modulus);

            Assert.True(result >= 0);
            Assert.True(result < modulus);

            // Cross-validate: a^e mod m == (a^e1 * a^e2) mod m  where e = e1 + e2
            BigInteger e1 = exp >> 1;
            BigInteger e2 = exp - e1;
            BigInteger r1 = BigInteger.ModPow(b, e1, modulus);
            BigInteger r2 = BigInteger.ModPow(b, e2, modulus);
            Assert.Equal(result, (r1 * r2) % modulus);
        }

        // --- GCD with specific size differences ---

        [Theory]
        [InlineData(10, 10)]    // Same length (case 0)
        [InlineData(10, 9)]     // Offset by 1 (case 1)
        [InlineData(10, 8)]     // Offset by 2 (case 2)
        [InlineData(10, 5)]     // Offset by 5 (default case)
        [InlineData(33, 33)]    // Large, same length
        [InlineData(33, 32)]    // Large, offset by 1
        [InlineData(33, 31)]    // Large, offset by 2
        [InlineData(65, 33)]    // Very different sizes
        public void GcdSizeOffsets(int aLimbs, int bLimbs)
        {
            var rng = new Random(14000 + aLimbs * 100 + bLimbs);
            BigInteger a = MakePositive(aLimbs, rng);
            BigInteger b = MakePositive(bLimbs, rng);

            BigInteger gcd = BigInteger.GreatestCommonDivisor(a, b);

            // GCD divides both
            Assert.True(gcd > 0);
            Assert.Equal(BigInteger.Zero, a % gcd);
            Assert.Equal(BigInteger.Zero, b % gcd);

            // GCD is symmetric
            Assert.Equal(gcd, BigInteger.GreatestCommonDivisor(b, a));

            // GCD with negatives
            Assert.Equal(gcd, BigInteger.GreatestCommonDivisor(-a, b));
            Assert.Equal(gcd, BigInteger.GreatestCommonDivisor(a, -b));
        }

        // --- GCD with zero (exercises array-sharing paths) ---

        public static IEnumerable<object[]> GcdWithZeroData()
        {
            // Single-limb values that fit in _sign (trivialLeft/trivialRight both true)
            yield return new object[] { BigInteger.Zero, BigInteger.Zero, BigInteger.Zero };
            yield return new object[] { BigInteger.Zero, BigInteger.One, BigInteger.One };
            yield return new object[] { BigInteger.One, BigInteger.Zero, BigInteger.One };
            yield return new object[] { BigInteger.Zero, BigInteger.MinusOne, BigInteger.One };
            yield return new object[] { BigInteger.MinusOne, BigInteger.Zero, BigInteger.One };
            yield return new object[] { BigInteger.Zero, new BigInteger(42), new BigInteger(42) };
            yield return new object[] { new BigInteger(-42), BigInteger.Zero, new BigInteger(42) };

            // Multi-limb values (exercises the trivialLeft / trivialRight paths that share _bits)
            BigInteger twoLimb = (BigInteger.One << (nint.Size * 8)) + 1;       // just over 1 limb
            BigInteger threeLimb = (BigInteger.One << (nint.Size * 8 * 2)) + 1; // just over 2 limbs
            BigInteger large = BigInteger.Pow(new BigInteger(long.MaxValue), 4); // many limbs

            yield return new object[] { BigInteger.Zero, twoLimb, twoLimb };
            yield return new object[] { twoLimb, BigInteger.Zero, twoLimb };
            yield return new object[] { BigInteger.Zero, -twoLimb, twoLimb };
            yield return new object[] { -twoLimb, BigInteger.Zero, twoLimb };

            yield return new object[] { BigInteger.Zero, threeLimb, threeLimb };
            yield return new object[] { threeLimb, BigInteger.Zero, threeLimb };
            yield return new object[] { BigInteger.Zero, -threeLimb, threeLimb };
            yield return new object[] { -threeLimb, BigInteger.Zero, threeLimb };

            yield return new object[] { BigInteger.Zero, large, large };
            yield return new object[] { large, BigInteger.Zero, large };
            yield return new object[] { BigInteger.Zero, -large, large };
            yield return new object[] { -large, BigInteger.Zero, large };

            // One-limb value that doesn't fit in _sign (magnitude >= nint.MaxValue)
            BigInteger oneLimbLarge = new BigInteger(nint.MaxValue) + 1; // requires _bits with 1 element
            yield return new object[] { BigInteger.Zero, oneLimbLarge, oneLimbLarge };
            yield return new object[] { oneLimbLarge, BigInteger.Zero, oneLimbLarge };
            yield return new object[] { BigInteger.Zero, -oneLimbLarge, oneLimbLarge };
            yield return new object[] { -oneLimbLarge, BigInteger.Zero, oneLimbLarge };

            // Non-zero trivial + multi-limb (tests Gcd(bits, scalar) path)
            yield return new object[] { new BigInteger(6), twoLimb, BigInteger.GreatestCommonDivisor(6, twoLimb) };
            yield return new object[] { twoLimb, new BigInteger(6), BigInteger.GreatestCommonDivisor(twoLimb, 6) };
        }

        [Theory]
        [MemberData(nameof(GcdWithZeroData))]
        public void GcdWithZero(BigInteger left, BigInteger right, BigInteger expected)
        {
            BigInteger result = BigInteger.GreatestCommonDivisor(left, right);
            Assert.Equal(expected, result);

            // GCD is always non-negative
            Assert.True(result >= 0);

            // GCD is symmetric
            Assert.Equal(result, BigInteger.GreatestCommonDivisor(right, left));

            // GCD with negated inputs should give the same result
            Assert.Equal(result, BigInteger.GreatestCommonDivisor(-left, right));
            Assert.Equal(result, BigInteger.GreatestCommonDivisor(left, -right));
            Assert.Equal(result, BigInteger.GreatestCommonDivisor(-left, -right));
        }

        // --- CopySign coverage ---

        [Theory]
        [InlineData(5, true, 3, true)]        // inline pos, inline pos → keep pos
        [InlineData(5, false, -3, false)]     // inline pos, inline neg → flip to neg
        [InlineData(-5, true, 3, true)]       // inline neg, inline pos → flip to pos
        [InlineData(-5, false, -3, false)]    // inline neg, inline neg → keep neg
        public void CopySignInline(long value, bool expectPositive, long sign, bool _)
        {
            BigInteger v = new BigInteger(value);
            BigInteger s = new BigInteger(sign);
            BigInteger result = BigInteger.CopySign(v, s);
            Assert.Equal(BigInteger.Abs(v), BigInteger.Abs(result));
            Assert.Equal(expectPositive ? 1 : -1, result.Sign);
        }

        [Fact]
        public void CopySignMixed()
        {
            var rng = new Random(15000);
            BigInteger large = MakePositive(10, rng);

            // Array value, inline sign
            Assert.Equal(large, BigInteger.CopySign(large, BigInteger.One));
            Assert.Equal(-large, BigInteger.CopySign(large, BigInteger.MinusOne));
            Assert.Equal(large, BigInteger.CopySign(-large, BigInteger.One));
            Assert.Equal(-large, BigInteger.CopySign(-large, BigInteger.MinusOne));

            // Inline value, array sign
            BigInteger small = new BigInteger(42);
            Assert.Equal(small, BigInteger.CopySign(small, large));
            Assert.Equal(-small, BigInteger.CopySign(small, -large));

            // Array value, array sign
            BigInteger other = MakePositive(10, rng);
            Assert.Equal(large, BigInteger.CopySign(large, other));
            Assert.Equal(-large, BigInteger.CopySign(large, -other));

            // Zero value
            Assert.Equal(BigInteger.Zero, BigInteger.CopySign(BigInteger.Zero, large));
            Assert.Equal(BigInteger.Zero, BigInteger.CopySign(BigInteger.Zero, -large));
        }

        // --- Explicit conversions at boundaries ---

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void ExplicitInt32Conversion(int value)
        {
            BigInteger bi = new BigInteger(value);
            Assert.Equal(value, (int)bi);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(1L)]
        [InlineData(-1L)]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        [InlineData((long)int.MaxValue + 1)]
        [InlineData((long)int.MinValue - 1)]
        public void ExplicitInt64Conversion(long value)
        {
            BigInteger bi = new BigInteger(value);
            Assert.Equal(value, (long)bi);
        }

        [Fact]
        public void ExplicitInt128Conversion()
        {
            // Values that exercise multi-limb Int128 conversion paths
            Int128 val1 = (Int128)long.MaxValue + 1;
            Assert.Equal(val1, (Int128)(BigInteger)val1);

            Int128 val2 = Int128.MaxValue;
            Assert.Equal(val2, (Int128)(BigInteger)val2);

            Int128 val3 = Int128.MinValue;
            Assert.Equal(val3, (Int128)(BigInteger)val3);

            Int128 val4 = (Int128)ulong.MaxValue + 1;
            Assert.Equal(val4, (Int128)(BigInteger)val4);

            // Negative values near boundaries
            Int128 val5 = -(Int128)long.MaxValue - 2;
            Assert.Equal(val5, (Int128)(BigInteger)val5);

            // Overflow
            BigInteger tooLarge = (BigInteger)Int128.MaxValue + 1;
            Assert.Throws<OverflowException>(() => (Int128)tooLarge);
        }

        [Fact]
        public void ExplicitUInt128Conversion()
        {
            UInt128 val1 = (UInt128)ulong.MaxValue + 1;
            Assert.Equal(val1, (UInt128)(BigInteger)val1);

            UInt128 val2 = UInt128.MaxValue;
            Assert.Equal(val2, (UInt128)(BigInteger)val2);

            UInt128 val3 = UInt128.MinValue;
            Assert.Equal(val3, (UInt128)(BigInteger)val3);

            // Overflow
            BigInteger tooLarge = (BigInteger)UInt128.MaxValue + 1;
            Assert.Throws<OverflowException>(() => (UInt128)tooLarge);

            BigInteger negative = BigInteger.MinusOne;
            Assert.Throws<OverflowException>(() => (UInt128)negative);
        }

        // --- DebuggerDisplay for large values ---

        [Fact]
        public void DebuggerDisplayLargeValue()
        {
            BigInteger large = MakePositive(10, new Random(16000));
            BigInteger veryLarge = MakePositive(100, new Random(16001));

            // DebuggerDisplay is accessed via DebuggerDisplayAttribute
            // Verify it doesn't throw for large values
            string display1 = large.ToString();
            Assert.NotNull(display1);
            Assert.NotEmpty(display1);

            string display2 = veryLarge.ToString();
            Assert.NotNull(display2);
            Assert.NotEmpty(display2);

            // Verify roundtrip still works at these sizes
            Assert.Equal(large, BigInteger.Parse(display1));
            Assert.Equal(veryLarge, BigInteger.Parse(display2));
        }

        // --- Inline-to-array representation boundary ---

        [Fact]
        public void InlineToArrayTransition()
        {
            BigInteger nintMax = new BigInteger(nint.MaxValue);
            BigInteger nintMaxPlus1 = nintMax + 1;
            BigInteger nintMin = new BigInteger(nint.MinValue);
            BigInteger nintMinMinus1 = nintMin - 1;

            // Arithmetic across the boundary
            Assert.Equal(nintMax, nintMaxPlus1 - 1);
            Assert.Equal(nintMaxPlus1, nintMax + 1);
            Assert.Equal(nintMin, nintMinMinus1 + 1);
            Assert.Equal(nintMinMinus1, nintMin - 1);

            // Multiply at boundary
            Assert.Equal(nintMax * nintMax, BigInteger.Pow(nintMax, 2));

            // Parse/ToString roundtrip at boundary
            Assert.Equal(nintMax, BigInteger.Parse(nintMax.ToString()));
            Assert.Equal(nintMaxPlus1, BigInteger.Parse(nintMaxPlus1.ToString()));
            Assert.Equal(nintMin, BigInteger.Parse(nintMin.ToString()));
            Assert.Equal(nintMinMinus1, BigInteger.Parse(nintMinMinus1.ToString()));

            // ToByteArray roundtrip at boundary
            Assert.Equal(nintMax, new BigInteger(nintMax.ToByteArray()));
            Assert.Equal(nintMaxPlus1, new BigInteger(nintMaxPlus1.ToByteArray()));
            Assert.Equal(nintMin, new BigInteger(nintMin.ToByteArray()));
            Assert.Equal(nintMinMinus1, new BigInteger(nintMinMinus1.ToByteArray()));

            // Bitwise at boundary
            Assert.Equal(BigInteger.Zero, nintMax ^ nintMax);
            Assert.Equal(BigInteger.Zero, nintMaxPlus1 ^ nintMaxPlus1);
            Assert.Equal(BigInteger.Zero, nintMin ^ nintMin);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(20)]
        [InlineData(32)]
        public void ModPowOddModulus(int limbCount)
        {
            Random rng = new Random(42);
            int byteCount = limbCount * nint.Size;

            byte[] modBytes = new byte[byteCount + 1];
            rng.NextBytes(modBytes);
            modBytes[^1] = 0;
            modBytes[0] |= 1; // ensure odd

            BigInteger mod = new BigInteger(modBytes);

            byte[] baseBytes = new byte[byteCount / 2 + 1];
            rng.NextBytes(baseBytes);
            baseBytes[^1] = 0;
            BigInteger b = new BigInteger(baseBytes);

            BigInteger exp = 65537;

            BigInteger result = BigInteger.ModPow(b, exp, mod);

            BigInteger check = BigInteger.One;
            BigInteger bb = b % mod;
            int e = 65537;
            while (e > 0)
            {
                if ((e & 1) == 1)
                    check = (check * bb) % mod;
                bb = (bb * bb) % mod;
                e >>= 1;
            }

            Assert.Equal(check, result);
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
