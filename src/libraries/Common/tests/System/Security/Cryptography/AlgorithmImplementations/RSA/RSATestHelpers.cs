// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class RSATestHelpers
    {
        internal static void AssertKeyEquals(in RSAParameters expected, in RSAParameters actual)
        {
            Assert.Equal(expected.Modulus, actual.Modulus);
            Assert.Equal(expected.Exponent, actual.Exponent);

            Assert.Equal(expected.P, actual.P);
            Assert.Equal(expected.DP, actual.DP);
            Assert.Equal(expected.Q, actual.Q);
            Assert.Equal(expected.DQ, actual.DQ);
            Assert.Equal(expected.InverseQ, actual.InverseQ);

            if (expected.D == null)
            {
                Assert.Null(actual.D);
            }
            else
            {
                Assert.NotNull(actual.D);

                // If the value matched expected, take that as valid and shortcut the math.
                // If it didn't, we'll test that the value is at least legal.
                if (!expected.D.SequenceEqual(actual.D))
                {
                    VerifyDValue(actual);
                }
            }
        }

        private static void VerifyDValue(in RSAParameters rsaParams)
        {
            if (rsaParams.P == null)
            {
                return;
            }

            // Verify that the formula (D * E) % LCM(p - 1, q - 1) == 1
            // is true.
            //
            // This is NOT the same as saying D = ModInv(E, LCM(p - 1, q - 1)),
            // because D = ModInv(E, (p - 1) * (q - 1)) is a valid choice, but will
            // still work through this formula.
            BigInteger p = PositiveBigInteger(rsaParams.P);
            BigInteger q = PositiveBigInteger(rsaParams.Q);
            BigInteger e = PositiveBigInteger(rsaParams.Exponent);
            BigInteger d = PositiveBigInteger(rsaParams.D);

            BigInteger lambda = LeastCommonMultiple(p - 1, q - 1);

            BigInteger modProduct = (d * e) % lambda;
            Assert.Equal(BigInteger.One, modProduct);
        }

        private static BigInteger LeastCommonMultiple(BigInteger a, BigInteger b)
        {
            BigInteger gcd = BigInteger.GreatestCommonDivisor(a, b);
            return BigInteger.Abs(a) / gcd * BigInteger.Abs(b);
        }

        private static BigInteger PositiveBigInteger(byte[] bigEndianBytes)
        {
            byte[] littleEndianBytes;

            if (bigEndianBytes[0] >= 0x80)
            {
                // Insert a padding 00 byte so the number is treated as positive.
                littleEndianBytes = new byte[bigEndianBytes.Length + 1];
                Buffer.BlockCopy(bigEndianBytes, 0, littleEndianBytes, 1, bigEndianBytes.Length);
            }
            else
            {
                littleEndianBytes = (byte[])bigEndianBytes.Clone();

            }

            Array.Reverse(littleEndianBytes);
            return new BigInteger(littleEndianBytes);
        }
    }
}
