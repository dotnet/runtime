// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class ImportExport
    {
        [Fact]
        public static void ExportAutoKey()
        {
            RSAParameters privateParams;
            RSAParameters publicParams;
            int keySize;

            using (RSA rsa = new RSACryptoServiceProvider())
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
            RSAParameters diminishedDPParamaters = TestData.DiminishedDPParamaters;
            RSAParameters exported;

            using (RSA rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(diminishedDPParamaters);
                exported = rsa.ExportParameters(true);
            }

            // DP is the most likely to fail, the rest just otherwise ensure that Export
            // isn't losing data.
            AssertKeyEquals(ref diminishedDPParamaters, ref exported);
        }

        [Fact]
        public static void LargeKeyImportExport()
        {
            RSAParameters imported = TestData.RSA16384Params;

            using (RSA rsa = new RSACryptoServiceProvider())
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

                AssertKeyEquals(ref imported, ref exported);
            }
        }

        [Fact]
        public static void ImportReset()
        {
            using (RSA rsa = new RSACryptoServiceProvider())
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
                AssertKeyEquals(ref imported, ref exported);
            }
        }

        [Fact]
        public static void MultiExport()
        {
            RSAParameters imported = TestData.RSA1024Params;

            using (RSA rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(imported);

                RSAParameters exportedPrivate = rsa.ExportParameters(true);
                RSAParameters exportedPrivate2 = rsa.ExportParameters(true);
                RSAParameters exportedPublic = rsa.ExportParameters(false);
                RSAParameters exportedPublic2 = rsa.ExportParameters(false);
                RSAParameters exportedPrivate3 = rsa.ExportParameters(true);
                RSAParameters exportedPublic3 = rsa.ExportParameters(false);

                AssertKeyEquals(ref imported, ref exportedPrivate);

                Assert.Equal(imported.Modulus, exportedPublic.Modulus);
                Assert.Equal(imported.Exponent, exportedPublic.Exponent);
                Assert.Null(exportedPublic.D);
                ValidateParameters(ref exportedPublic);

                AssertKeyEquals(ref exportedPrivate, ref exportedPrivate2);
                AssertKeyEquals(ref exportedPrivate, ref exportedPrivate3);

                AssertKeyEquals(ref exportedPublic, ref exportedPublic2);
                AssertKeyEquals(ref exportedPublic, ref exportedPublic3);
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

            using (RSA rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(imported);
                Assert.Throws<CryptographicException>(() => rsa.ExportParameters(true));
            }
        }

        [Fact]
        public static void ImportNoExponent()
        {
            RSAParameters imported = new RSAParameters
            {
                Modulus = TestData.RSA1024Params.Modulus,
            };

            using (RSA rsa = new RSACryptoServiceProvider())
            {
                Assert.Throws<CryptographicException>(() => rsa.ImportParameters(imported));
            }
        }

        [Fact]
        public static void ImportNoModulus()
        {
            RSAParameters imported = new RSAParameters
            {
                Exponent = TestData.RSA1024Params.Exponent,
            };

            using (RSA rsa = new RSACryptoServiceProvider())
            {
                Assert.Throws<CryptographicException>(() => rsa.ImportParameters(imported));
            }
        }

        [Fact]
        public static void ImportNoDP()
        {
            // Because RSAParameters is a struct, this is a copy,
            // so assigning DP is not destructive to other tests.
            RSAParameters imported = TestData.RSA1024Params;
            imported.DP = null;

            using (RSA rsa = new RSACryptoServiceProvider())
            {
                Assert.Throws<CryptographicException>(() => rsa.ImportParameters(imported));
            }
        }

        internal static void AssertKeyEquals(ref RSAParameters expected, ref RSAParameters actual)
        {
            Assert.Equal(expected.Modulus, actual.Modulus);
            Assert.Equal(expected.Exponent, actual.Exponent);
            Assert.Equal(expected.D, actual.D);
            Assert.Equal(expected.P, actual.P);
            Assert.Equal(expected.DP, actual.DP);
            Assert.Equal(expected.Q, actual.Q);
            Assert.Equal(expected.DQ, actual.DQ);
            Assert.Equal(expected.InverseQ, actual.InverseQ);
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
    }
}
