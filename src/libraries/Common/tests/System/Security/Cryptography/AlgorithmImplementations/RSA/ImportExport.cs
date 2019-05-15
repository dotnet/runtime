// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Numerics;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    public partial class ImportExport
    {
        public static bool Supports16384 { get; } = TestRsa16384();

        [Fact]
        public static void ExportAutoKey()
        {
            RSAParameters privateParams;
            RSAParameters publicParams;
            int keySize;

            using (RSA rsa = RSAFactory.Create())
            {
                keySize = rsa.KeySize;

                // We've not done anything with this instance yet, but it should automatically
                // create the key, because we'll now asked about it.
                privateParams = rsa.ExportParameters(true);
                publicParams = rsa.ExportParameters(false);

                // It shouldn't be changing things when it generated the key.
                Assert.Equal(keySize, rsa.KeySize);
            }

            Assert.Null(publicParams.D);
            Assert.NotNull(privateParams.D);

            ValidateParameters(ref publicParams);
            ValidateParameters(ref privateParams);

            Assert.Equal(privateParams.Modulus, publicParams.Modulus);
            Assert.Equal(privateParams.Exponent, publicParams.Exponent);
        }

        [Fact]
        public static void PaddedExport()
        {
            // OpenSSL's numeric type for the storage of RSA key parts disregards zero-valued
            // prefix bytes.
            //
            // The .NET 4.5 RSACryptoServiceProvider type verifies that all of the D breakdown
            // values (P, DP, Q, DQ, InverseQ) are exactly half the size of D (which is itself
            // the same size as Modulus).
            //
            // These two things, in combination, suggest that we ensure that all .NET
            // implementations of RSA export their keys to the fixed array size suggested by their
            // KeySize property.
            RSAParameters diminishedDPParameters = TestData.DiminishedDPParameters;
            RSAParameters exported;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(diminishedDPParameters);
                exported = rsa.ExportParameters(true);
            }

            // DP is the most likely to fail, the rest just otherwise ensure that Export
            // isn't losing data.
            AssertKeyEquals(diminishedDPParameters, exported);
        }

        [Fact]
        public static void LargeKeyImportExport()
        {
            RSAParameters imported = TestData.RSA16384Params;

            using (RSA rsa = RSAFactory.Create())
            {
                try
                {
                    rsa.ImportParameters(imported);
                }
                catch (CryptographicException)
                {
                    // The key is pretty big, perhaps it was refused.
                    return;
                }

                RSAParameters exported = rsa.ExportParameters(false);

                Assert.Equal(exported.Modulus, imported.Modulus);
                Assert.Equal(exported.Exponent, imported.Exponent);
                Assert.Null(exported.D);

                exported = rsa.ExportParameters(true);

                AssertKeyEquals(imported, exported);
            }
        }

        [Fact]
        public static void UnusualExponentImportExport()
        {
            // Most choices for the Exponent value in an RSA key use a Fermat prime.
            // Since a Fermat prime is 2^(2^m) + 1, it always only has two bits set, and
            // frequently has the form { 0x01, [some number of 0x00s], 0x01 }, which has the same
            // representation in both big- and little-endian.
            //
            // The only real requirement for an Exponent value is that it be coprime to (p-1)(q-1).
            // So here we'll use the (non-Fermat) prime value 433 (0x01B1) to ensure big-endian export.
            RSAParameters unusualExponentParameters = TestData.UnusualExponentParameters;
            RSAParameters exported;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(unusualExponentParameters);
                exported = rsa.ExportParameters(true);
            }

            // Exponent is the most likely to fail, the rest just otherwise ensure that Export
            // isn't losing data.
            AssertKeyEquals(unusualExponentParameters, exported);
        }

        [Fact]
        public static void ImportExport1032()
        {
            RSAParameters imported = TestData.RSA1032Parameters;
            RSAParameters exported;
            RSAParameters exportedPublic;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(imported);
                exported = rsa.ExportParameters(true);
                exportedPublic = rsa.ExportParameters(false);
            }

            AssertKeyEquals(imported, exported);

            Assert.Equal(exportedPublic.Modulus, imported.Modulus);
            Assert.Equal(exportedPublic.Exponent, imported.Exponent);
            Assert.Null(exportedPublic.D);
        }

        [Fact]
        public static void ImportReset()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                RSAParameters exported = rsa.ExportParameters(true);
                RSAParameters imported;

                // Ensure that we cause the KeySize value to change.
                if (rsa.KeySize == 1024)
                {
                    imported = TestData.RSA2048Params;
                }
                else
                {
                    imported = TestData.RSA1024Params;
                }

                Assert.NotEqual(imported.Modulus.Length * 8, rsa.KeySize);
                Assert.NotEqual(imported.Modulus, exported.Modulus);

                rsa.ImportParameters(imported);

                Assert.Equal(imported.Modulus.Length * 8, rsa.KeySize);

                exported = rsa.ExportParameters(true);
                AssertKeyEquals(imported, exported);
            }
        }

        [Fact]
        public static void ImportPrivateExportPublic()
        {
            RSAParameters imported = TestData.RSA1024Params;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(imported);

                RSAParameters exportedPublic = rsa.ExportParameters(false);

                Assert.Equal(imported.Modulus, exportedPublic.Modulus);
                Assert.Equal(imported.Exponent, exportedPublic.Exponent);
                Assert.Null(exportedPublic.D);
                ValidateParameters(ref exportedPublic);
            }
        }

        [Fact]
        public static void MultiExport()
        {
            RSAParameters imported = TestData.RSA1024Params;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(imported);

                RSAParameters exportedPrivate = rsa.ExportParameters(true);
                RSAParameters exportedPrivate2 = rsa.ExportParameters(true);
                RSAParameters exportedPublic = rsa.ExportParameters(false);
                RSAParameters exportedPublic2 = rsa.ExportParameters(false);
                RSAParameters exportedPrivate3 = rsa.ExportParameters(true);
                RSAParameters exportedPublic3 = rsa.ExportParameters(false);

                AssertKeyEquals(imported, exportedPrivate);

                Assert.Equal(imported.Modulus, exportedPublic.Modulus);
                Assert.Equal(imported.Exponent, exportedPublic.Exponent);
                Assert.Null(exportedPublic.D);
                ValidateParameters(ref exportedPublic);

                AssertKeyEquals(exportedPrivate, exportedPrivate2);
                AssertKeyEquals(exportedPrivate, exportedPrivate3);

                AssertKeyEquals(exportedPublic, exportedPublic2);
                AssertKeyEquals(exportedPublic, exportedPublic3);
            }
        }

        [Fact]
        public static void PublicOnlyPrivateExport()
        {
            RSAParameters imported = new RSAParameters
            {
                Modulus = TestData.RSA1024Params.Modulus,
                Exponent = TestData.RSA1024Params.Exponent,
            };

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(imported);
                Assert.ThrowsAny<CryptographicException>(() => rsa.ExportParameters(true));
            }
        }

        [Fact]
        public static void ImportNoExponent()
        {
            RSAParameters imported = new RSAParameters
            {
                Modulus = TestData.RSA1024Params.Modulus,
            };

            using (RSA rsa = RSAFactory.Create())
            {
                if (rsa is RSACng && PlatformDetection.IsFullFramework)
                    AssertExtensions.Throws<ArgumentException>(null, () => rsa.ImportParameters(imported));
                else
                    Assert.ThrowsAny<CryptographicException>(() => rsa.ImportParameters(imported));
            }
        }

        [Fact]
        public static void ImportNoModulus()
        {
            RSAParameters imported = new RSAParameters
            {
                Exponent = TestData.RSA1024Params.Exponent,
            };

            using (RSA rsa = RSAFactory.Create())
            {
                if (rsa is RSACng && PlatformDetection.IsFullFramework)
                    AssertExtensions.Throws<ArgumentException>(null, () => rsa.ImportParameters(imported));
                else
                    Assert.ThrowsAny<CryptographicException>(() => rsa.ImportParameters(imported));
            }
        }

        [Fact]
#if TESTING_CNG_IMPLEMENTATION
        [ActiveIssue(18882, TargetFrameworkMonikers.NetFramework)]
#endif
        public static void ImportNoDP()
        {
            // Because RSAParameters is a struct, this is a copy,
            // so assigning DP is not destructive to other tests.
            RSAParameters imported = TestData.RSA1024Params;
            imported.DP = null;

            using (RSA rsa = RSAFactory.Create())
            {
                Assert.ThrowsAny<CryptographicException>(() => rsa.ImportParameters(imported));
            }
        }

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

        internal static void ValidateParameters(ref RSAParameters rsaParams)
        {
            Assert.NotNull(rsaParams.Modulus);
            Assert.NotNull(rsaParams.Exponent);

            // Key compatibility: RSA as an algorithm is achievable using just N (Modulus),
            // E (public Exponent) and D (private exponent).  Having all of the breakdowns
            // of D make the algorithm faster, and shipped versions of RSACryptoServiceProvider
            // have thrown if D is provided and the rest of the private key values are not.
            // So, here we're going to assert that none of them were null for private keys.

            if (rsaParams.D == null)
            {
                Assert.Null(rsaParams.P);
                Assert.Null(rsaParams.DP);
                Assert.Null(rsaParams.Q);
                Assert.Null(rsaParams.DQ);
                Assert.Null(rsaParams.InverseQ);
            }
            else
            {
                Assert.NotNull(rsaParams.P);
                Assert.NotNull(rsaParams.DP);
                Assert.NotNull(rsaParams.Q);
                Assert.NotNull(rsaParams.DQ);
                Assert.NotNull(rsaParams.InverseQ);
            }
        }

        internal static RSAParameters MakePublic(in RSAParameters rsaParams)
        {
            return new RSAParameters
            {
                Modulus = rsaParams.Modulus,
                Exponent = rsaParams.Exponent,
            };
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

        private static bool TestRsa16384()
        {
            try
            {
                using (RSA rsa = RSAFactory.Create())
                {
                    rsa.ImportParameters(TestData.RSA16384Params);
                }

                return true;
            }
            catch (CryptographicException)
            {
                // The key is too big for this platform.
                return false;
            }
        }
    }
}
