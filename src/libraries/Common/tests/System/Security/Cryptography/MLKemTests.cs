// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Text;
using Microsoft.DotNet.XUnitExtensions;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    public static class MLKemTests
    {
        private static readonly byte[] s_asnNull = new byte[] { 0x05, 0x00 };

        [Fact]
        public static void Generate_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLKem.GenerateKey(null));
        }

        [Fact]
        public static void ImportPrivateSeed_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportPrivateSeed(null, new byte[MLKemAlgorithm.MLKem512.PrivateSeedSizeInBytes]));

            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportPrivateSeed(null, new ReadOnlySpan<byte>(new byte[MLKemAlgorithm.MLKem512.PrivateSeedSizeInBytes])));
        }

        [Fact]
        public static void ImportPrivateSeed_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                MLKem.ImportPrivateSeed(MLKemAlgorithm.MLKem512, null));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportPrivateSeed_WrongSize_Array(MLKemAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, new byte[algorithm.PrivateSeedSizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, new byte[algorithm.PrivateSeedSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, Array.Empty<byte>()));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportPrivateSeed_WrongSize_Span(MLKemAlgorithm algorithm)
        {
            byte[] seed = new byte[algorithm.PrivateSeedSizeInBytes + 1];

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, seed.AsSpan()));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, seed.AsSpan(0, seed.Length - 2)));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void ImportDecapsulationKey_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportDecapsulationKey(null, new byte[MLKemAlgorithm.MLKem512.DecapsulationKeySizeInBytes]));
        }

        [Fact]
        public static void ImportDecapsulationKey_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                MLKem.ImportDecapsulationKey(MLKemAlgorithm.MLKem512, null));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportDecapsulationKey_WrongSize_Array(MLKemAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, new byte[algorithm.DecapsulationKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, new byte[algorithm.DecapsulationKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, Array.Empty<byte>()));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportDecapsulationKey_WrongSize_Span(MLKemAlgorithm algorithm)
        {
            byte[] destination = new byte[algorithm.DecapsulationKeySizeInBytes + 1];

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, destination.AsSpan()));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, destination.AsSpan(0, destination.Length - 2)));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void ImportEncapsulationKey_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportEncapsulationKey(null, new byte[MLKemAlgorithm.MLKem512.EncapsulationKeySizeInBytes]));
        }

        [Fact]
        public static void ImportEncapsulationKey_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                MLKem.ImportEncapsulationKey(MLKemAlgorithm.MLKem512, null));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportEncapsulationKey_WrongSize_Array(MLKemAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, new byte[algorithm.EncapsulationKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, new byte[algorithm.EncapsulationKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, []));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportEncapsulationKey_WrongSize_Span(MLKemAlgorithm algorithm)
        {
            byte[] destination = new byte[algorithm.EncapsulationKeySizeInBytes + 1];

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, destination.AsSpan()));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, destination.AsSpan(0, destination.Length - 2)));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                MLKem.ImportSubjectPublicKeyInfo((byte[])null));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                MLKem.ImportPkcs8PrivateKey((byte[])null));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_WrongAlgorithm()
        {
            byte[] ecP256Key = Convert.FromBase64String(@"
                MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgZg/vYKeaTgco6dGx
                6KCMw5/L7/Xu7j7idYWNSCBcod6hRANCAASc/jV6ZojlesoM+qNnSYZdc7Fkd4+E
                2raDwlFPZGucEHDUmdCwaDx/hglDZaLimpD/67F5k5jUe+I3CkijLST7");

            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(ecP256Key)));
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(ecP256Key));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void ImportPkcs8PrivateKey_BogusAsnChoice(bool useSpanImport)
        {
            // SEQUENCE {
            //   INTEGER 0
            //   SEQUENCE {
            //     OBJECT IDENTIFIER 2.16.840.1.101.3.4.4.1 #id-alg-ml-kem-512
            //   }
            //   PRINTABLE STRING "Potato"
            // }
            byte[] pkcs8 = "3018020100300B06096086480165030404011306506F7461746F".HexToByteArray();

            if (useSpanImport)
            {
                Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(pkcs8)));
            }
            else
            {
                Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(pkcs8));
            }
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_Seed_Array()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8) in Pkcs8PrivateKeySeedTestData)
            {
                using MLKem kem = MLKem.ImportPkcs8PrivateKey(pkcs8);
                Assert.Equal(algorithm, kem.Algorithm);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, kem.ExportPrivateSeed());
            }
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_Seed_Span()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8) in Pkcs8PrivateKeySeedTestData)
            {
                using MLKem kem = MLKem.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(pkcs8));
                Assert.Equal(algorithm, kem.Algorithm);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, kem.ExportPrivateSeed());
            }
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportPkcs8PrivateKey_Seed_BadLength(MLKemAlgorithm algorithm)
        {
            byte[] encoded = Pkcs8Encode(algorithm.GetOid(), seed: new byte[algorithm.PrivateSeedSizeInBytes - 1]);
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(encoded));

            encoded = Pkcs8Encode(algorithm.GetOid(), seed: new byte[algorithm.PrivateSeedSizeInBytes + 1]);
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(encoded));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_Seed_TrailingData()
        {
            foreach ((_, byte[] pkcs8) in Pkcs8PrivateKeySeedTestData)
            {
                byte[] oversized = new byte[pkcs8.Length + 1];
                pkcs8.AsSpan().CopyTo(oversized);

                Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(oversized.AsSpan()));
                Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(oversized));
            }
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportPkcs8PrivateKey_Seed_BadAlgorithmIdentifier(MLKemAlgorithm algorithm)
        {
            byte[] encoded = Pkcs8Encode(
                algorithm.GetOid(),
                seed: MLKemTestData.IncrementalSeed.ToArray(),
                algorithmParameters: s_asnNull);
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(encoded.AsSpan()));
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(encoded));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_ExpandedKey_Array()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8PrivateKeyExpandedKeyTestData)
            {
                using MLKem kem = MLKem.ImportPkcs8PrivateKey(pkcs8);
                Assert.Equal(algorithm, kem.Algorithm);
                byte[] decapsulationKey = kem.ExportDecapsulationKey();
                AssertExtensions.SequenceEqual(decapKey, decapsulationKey);
            }
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_ExpandedKey_Span()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8PrivateKeyExpandedKeyTestData)
            {
                using MLKem kem = MLKem.ImportPkcs8PrivateKey(pkcs8.AsSpan());
                Assert.Equal(algorithm, kem.Algorithm);
                byte[] decapsulationKey = kem.ExportDecapsulationKey();
                AssertExtensions.SequenceEqual(decapKey, decapsulationKey);
            }
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_ExpandedKey_WrongAlgorithm()
        {
            byte[] pkcs8 = Pkcs8Encode(
                MLKemTestData.MlKem768Oid,
                expandedKey: MLKemTestData.IetfMlKem512PrivateKeyDecapsulationKey);

            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(pkcs8)));
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(pkcs8));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportPkcs8PrivateKey_ExpandedKey_BadAlgorithmIdentifier(MLKemAlgorithm algorithm)
        {
            byte[] pkcs8 = Pkcs8Encode(
                algorithm.GetOid(),
                expandedKey: MLKemTestData.IetfMlKem512PrivateKeyDecapsulationKey,
                algorithmParameters: s_asnNull);
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(pkcs8.AsSpan()));
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(pkcs8));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_ExpandedKey_TrailingData()
        {
            foreach ((_, byte[] pkcs8, _) in Pkcs8PrivateKeyExpandedKeyTestData)
            {
                byte[] oversized = new byte[pkcs8.Length + 1];
                pkcs8.AsSpan().CopyTo(oversized);

                Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(oversized.AsSpan()));
                Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(oversized));
            }
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_Both_Array()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8PrivateKeyBothTestData)
            {
                using MLKem kem = MLKem.ImportPkcs8PrivateKey(pkcs8);
                byte[] seed = kem.ExportPrivateSeed();
                byte[] decapsulationKey = kem.ExportDecapsulationKey();
                Assert.Equal(algorithm, kem.Algorithm);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, seed);
                AssertExtensions.SequenceEqual(decapKey, decapsulationKey);
            }
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_Both_Span()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8PrivateKeyBothTestData)
            {
                using MLKem kem = MLKem.ImportPkcs8PrivateKey(pkcs8.AsSpan());
                byte[] seed = kem.ExportPrivateSeed();
                byte[] decapsulationKey = kem.ExportDecapsulationKey();
                Assert.Equal(algorithm, kem.Algorithm);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, seed);
                AssertExtensions.SequenceEqual(decapKey, decapsulationKey);
            }
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportPkcs8PrivateKey_Both_Array_MismatchedKeys(MLKemAlgorithm algorithm)
        {
            using MLKem kem1 = MLKem.GenerateKey(algorithm);
            using MLKem kem2 = MLKem.GenerateKey(algorithm);
            byte[] seed = kem1.ExportPrivateSeed();
            byte[] decapKey = kem2.ExportDecapsulationKey();
            byte[] encoded = Pkcs8Encode(algorithm.GetOid(), seed: seed, expandedKey: decapKey);
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(encoded));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportPkcs8PrivateKey_Both_Span_MismatchedKeys(MLKemAlgorithm algorithm)
        {
            using MLKem kem1 = MLKem.GenerateKey(algorithm);
            using MLKem kem2 = MLKem.GenerateKey(algorithm);
            byte[] seed = kem1.ExportPrivateSeed();
            byte[] decapKey = kem2.ExportDecapsulationKey();
            byte[] encoded = Pkcs8Encode(algorithm.GetOid(), seed: seed, expandedKey: decapKey);
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(encoded.AsSpan()));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportPkcs8PrivateKey_Both_BadAlgorithmIdentifier(MLKemAlgorithm algorithm)
        {
            byte[] pkcs8 = Pkcs8Encode(
                algorithm.GetOid(),
                expandedKey: MLKemTestData.IetfMlKem512PrivateKeyDecapsulationKey,
                seed: MLKemTestData.IncrementalSeed.ToArray(),
                algorithmParameters: s_asnNull);
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(pkcs8.AsSpan()));
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(pkcs8));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_Both_TrailingData()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8PrivateKeyBothTestData)
            {
                byte[] oversized = new byte[pkcs8.Length + 1];
                pkcs8.AsSpan().CopyTo(oversized);

                Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(oversized.AsSpan()));
                Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(oversized));
            }
        }

       [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_WrongAlgorithm()
        {
            byte[] ecP256Key = Convert.FromBase64String(@"
                MIHrMFYGCSqGSIb3DQEFDTBJMDEGCSqGSIb3DQEFDDAkBBCr0ipJGBOnThng8uXT
                iyZWAgIIADAMBggqhkiG9w0CCQUAMBQGCCqGSIb3DQMHBAgNPETMQWxeYgSBkN4J
                tW/1aNLGpRCBPvz2aHMulF/bBRRy3G8hwidysLR/mc0CaFWeltzZUpSGJgMSDJE4
                /zQJXhyXcEApuChzg0H0o8cPK1SCyi4wScMokiUHskOhcxhyr1VQ7cFAT+qS+66C
                gJoH9z0+/Z9WzLU8ix8F7B+HWwRhib5Cd6si+AX6DsNelMq2zP1NO7Un416dkg==");

            Assert.Throws<CryptographicException>(() =>
                MLKem.ImportEncryptedPkcs8PrivateKey("PLACEHOLDER", new ReadOnlySpan<byte>(ecP256Key)));

            Assert.Throws<CryptographicException>(() =>
                MLKem.ImportEncryptedPkcs8PrivateKey("PLACEHOLDER".AsSpan(), new ReadOnlySpan<byte>(ecP256Key)));

            Assert.Throws<CryptographicException>(() =>
                MLKem.ImportEncryptedPkcs8PrivateKey("PLACEHOLDER"u8, new ReadOnlySpan<byte>(ecP256Key)));
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_TrailingData()
        {
            foreach ((_, byte[] pkcs8) in Pkcs8EncryptedPrivateKeySeedTestData)
            {
                byte[] oversized = new byte[pkcs8.Length + 1];
                pkcs8.AsSpan().CopyTo(oversized);

                Assert.Throws<CryptographicException>(() =>
                    MLKem.ImportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPassword, oversized));

                Assert.Throws<CryptographicException>(() =>
                    MLKem.ImportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(), oversized));

                Assert.Throws<CryptographicException>(() =>
                    MLKem.ImportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPasswordBytes, oversized));
            }
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_DoesNotProcessUnencryptedData()
        {
            foreach ((_, byte[] pkcs8) in Pkcs8PrivateKeySeedTestData)
            {
                Assert.Throws<CryptographicException>(() =>
                    MLKem.ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, pkcs8));

                Assert.Throws<CryptographicException>(() =>
                    MLKem.ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, pkcs8));

                Assert.Throws<CryptographicException>(() =>
                    MLKem.ImportEncryptedPkcs8PrivateKey(string.Empty, pkcs8));
            }
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_Seed_CharPassword()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8) in Pkcs8EncryptedPrivateKeySeedTestData)
            {
                using MLKem kem = MLKem.ImportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(), pkcs8);

                Assert.Equal(algorithm, kem.Algorithm);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, kem.ExportPrivateSeed());
            }
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_Seed_StringPassword()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8) in Pkcs8EncryptedPrivateKeySeedTestData)
            {
                using MLKem kem = MLKem.ImportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPassword, pkcs8);
                Assert.Equal(algorithm, kem.Algorithm);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, kem.ExportPrivateSeed());
            }
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_Seed_BytePassword()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8) in Pkcs8EncryptedPrivateKeySeedTestData)
            {
                using MLKem kem = MLKem.ImportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPasswordBytes, pkcs8);
                Assert.Equal(algorithm, kem.Algorithm);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, kem.ExportPrivateSeed());
            }
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_ExpandedKey_CharPassword()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8EncryptedPrivateKeyExpandedKeyTestData)
            {
                using MLKem kem = MLKem.ImportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(), pkcs8);
                Assert.Equal(algorithm, kem.Algorithm);
                byte[] decapsulationKey = kem.ExportDecapsulationKey();
                AssertExtensions.SequenceEqual(decapKey, decapsulationKey);
            }
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_ExpandedKey_StringPassword()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8EncryptedPrivateKeyExpandedKeyTestData)
            {
                using MLKem kem = MLKem.ImportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPassword, pkcs8);
                Assert.Equal(algorithm, kem.Algorithm);
                byte[] decapsulationKey = kem.ExportDecapsulationKey();
                AssertExtensions.SequenceEqual(decapKey, decapsulationKey);
            }
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_ExpandedKey_BytePassword()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8EncryptedPrivateKeyExpandedKeyTestData)
            {
                using MLKem kem = MLKem.ImportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPasswordBytes, pkcs8);
                Assert.Equal(algorithm, kem.Algorithm);
                byte[] decapsulationKey = kem.ExportDecapsulationKey();
                AssertExtensions.SequenceEqual(decapKey, decapsulationKey);
            }
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_Both_CharPassword()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8EncryptedPrivateKeyBothTestData)
            {
                using MLKem kem = MLKem.ImportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(), pkcs8);
                Assert.Equal(algorithm, kem.Algorithm);
                byte[] decapsulationKey = kem.ExportDecapsulationKey();
                byte[] seed = kem.ExportPrivateSeed();
                AssertExtensions.SequenceEqual(decapKey, decapsulationKey);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, seed);
            }
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_Both_StringPassword()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8EncryptedPrivateKeyBothTestData)
            {
                using MLKem kem = MLKem.ImportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPassword, pkcs8);
                Assert.Equal(algorithm, kem.Algorithm);
                byte[] decapsulationKey = kem.ExportDecapsulationKey();
                byte[] seed = kem.ExportPrivateSeed();
                AssertExtensions.SequenceEqual(decapKey, decapsulationKey);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, seed);
            }
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_Both_BytePassword()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8EncryptedPrivateKeyBothTestData)
            {
                using MLKem kem = MLKem.ImportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPasswordBytes, pkcs8);
                Assert.Equal(algorithm, kem.Algorithm);
                byte[] decapsulationKey = kem.ExportDecapsulationKey();
                byte[] seed = kem.ExportPrivateSeed();
                AssertExtensions.SequenceEqual(decapKey, decapsulationKey);
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, seed);
            }
        }

        [Fact]
        public static void ImportFromPem_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () => MLKem.ImportFromPem((string)null));
        }

        [Fact]
        public static void ImportFromPem_PublicKey_Roundtrip()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] spki) in SubjectPublicKeyInfoTestData)
            {
                string pem = WritePem("PUBLIC KEY", spki);
                AssertImportFromPem(importer =>
                {
                    using MLKem kem = importer(pem);
                    byte[] exportedSpki = kem.ExportSubjectPublicKeyInfo();
                    AssertExtensions.SequenceEqual(spki, exportedSpki);
                });
            }
        }

        [Fact]
        public static void ImportFromPem_PublicKey_IgnoresNotUnderstoodPems()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] spki) in SubjectPublicKeyInfoTestData)
            {
                string pem = $"""
                -----BEGIN POTATO-----
                dmluY2U=
                -----END POTATO-----
                {WritePem("PUBLIC KEY", spki)}
                """;

                AssertImportFromPem(importer =>
                {
                    using MLKem kem = importer(pem);
                    byte[] exportedSpki = kem.ExportSubjectPublicKeyInfo();
                    AssertExtensions.SequenceEqual(spki, exportedSpki);
                });
            }
        }

        [Fact]
        public static void ImportFromPem_PrivateKey_Seed_Roundtrip()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8) in Pkcs8PrivateKeySeedTestData)
            {
                string pem = WritePem("PRIVATE KEY", pkcs8);
                AssertImportFromPem(importer =>
                {
                    using MLKem kem = importer(pem);
                    byte[] exportedSeed = kem.ExportPrivateSeed();
                    AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, exportedSeed);
                });
            }
        }

        [Fact]
        public static void ImportFromPem_PrivateKey_ExpandedKey_Roundtrip()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8PrivateKeyExpandedKeyTestData)
            {
                string pem = WritePem("PRIVATE KEY", pkcs8);
                AssertImportFromPem(importer =>
                {
                    using MLKem kem = importer(pem);
                    byte[] exportedDecapKey = kem.ExportDecapsulationKey();
                    AssertExtensions.SequenceEqual(decapKey, exportedDecapKey);
                });
            }
        }

        [Fact]
        public static void ImportFromPem_PrivateKey_Both_Roundtrip()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8PrivateKeyBothTestData)
            {
                string pem = WritePem("PRIVATE KEY", pkcs8);
                AssertImportFromPem(importer =>
                {
                    using MLKem kem = importer(pem);
                    byte[] exportedSeed = kem.ExportPrivateSeed();
                    AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, exportedSeed);
                });
            }
        }

        [Fact]
        public static void ImportFromPem_AmbiguousImportWithPublicKey_Throws()
        {
            string pem = $"""
            {WritePem("PUBLIC KEY", MLKemTestData.IetfMlKem512Spki)}
            {WritePem("PUBLIC KEY", MLKemTestData.IetfMlKem768Spki)}
            """;

            AssertImportFromPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source", () => importer(pem));
            });
        }

        [Fact]
        public static void ImportFromPem_AmbiguousImportWithPrivateKey_Throws()
        {
            string pem = $"""
            {WritePem("PUBLIC KEY", MLKemTestData.IetfMlKem512Spki)}
            {WritePem("PRIVATE KEY", MLKemTestData.IetfMlKem512PrivateKeySeed)}
            """;

            AssertImportFromPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source", () => importer(pem));
            });
        }

        [Fact]
        public static void ImportFromPem_AmbiguousImportWithEncryptedPrivateKey_Throws()
        {
            string pem = $"""
            {WritePem("PUBLIC KEY", MLKemTestData.IetfMlKem512Spki)}
            {WritePem("ENCRYPTED PRIVATE KEY", MLKemTestData.IetfMlKem512EncryptedPrivateKeySeed)}
            """;

            AssertImportFromPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source", () => importer(pem));
            });
        }

        [Fact]
        public static void ImportFromPem_EncryptedPrivateKey_Throws()
        {
            string pem = WritePem("ENCRYPTED PRIVATE KEY", MLKemTestData.IetfMlKem512EncryptedPrivateKeySeed);
            AssertImportFromPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source", () => importer(pem));
            });
        }

        [Fact]
        public static void ImportFromPem_NoUnderstoodPem_Throws()
        {
            string pem = """
            -----BEGIN UNKNOWN-----
            cGNq
            -----END UNKNOWN-----
            """;

            AssertImportFromPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source", () => importer(pem));
            });
        }

        [Fact]
        public static void ImportFromPem_PrivateKey_IgnoresNotUnderstoodPems()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8) in Pkcs8PrivateKeySeedTestData)
            {
                string pem = $"""
                -----BEGIN UNKNOWN-----
                cGNq
                -----END UNKNOWN-----
                {WritePem("PRIVATE KEY", pkcs8)}
                """;

                AssertImportFromPem(importer =>
                {
                    using MLKem kem = importer(pem);
                    byte[] exportedSeed = kem.ExportPrivateSeed();
                    AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, exportedSeed);
                });
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source",
                static () => MLKem.ImportFromEncryptedPem((string)null, "PLACEHOLDER"));

            AssertExtensions.Throws<ArgumentNullException>("source",
                static () => MLKem.ImportFromEncryptedPem((string)null, "PLACEHOLDER"u8.ToArray()));
        }

        [Fact]
        public static void ImportFromEncryptedPem_NullPassword()
        {
            AssertExtensions.Throws<ArgumentNullException>("password",
                static () => MLKem.ImportFromEncryptedPem("the pem", (string)null));

            AssertExtensions.Throws<ArgumentNullException>("passwordBytes",
                static () => MLKem.ImportFromEncryptedPem("the pem", (byte[])null));
        }

        [Fact]
        public static void ImportFromEncryptedPem_PrivateKey_Seed_Roundtrip()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8) in Pkcs8EncryptedPrivateKeySeedTestData)
            {
                string pem = WritePem("ENCRYPTED PRIVATE KEY", pkcs8);
                AssertImportFromEncryptedPem(importer =>
                {
                    using MLKem kem = importer(pem, MLKemTestData.EncryptedPrivateKeyPassword);
                    byte[] exportedSeed = kem.ExportPrivateSeed();
                    AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, exportedSeed);
                });
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_PrivateKey_ExpandedKey_Roundtrip()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8EncryptedPrivateKeyExpandedKeyTestData)
            {
                string pem = WritePem("ENCRYPTED PRIVATE KEY", pkcs8);
                AssertImportFromEncryptedPem(importer =>
                {
                    using MLKem kem = importer(pem, MLKemTestData.EncryptedPrivateKeyPassword);
                    byte[] exportedDecapKey = kem.ExportDecapsulationKey();
                    AssertExtensions.SequenceEqual(decapKey, exportedDecapKey);
                });
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_PrivateKey_Both_Roundtrip()
        {
            foreach ((MLKemAlgorithm algorithm, byte[] pkcs8, byte[] decapKey) in Pkcs8EncryptedPrivateKeyBothTestData)
            {
                string pem = WritePem("ENCRYPTED PRIVATE KEY", pkcs8);
                AssertImportFromEncryptedPem(importer =>
                {
                    using MLKem kem = importer(pem, MLKemTestData.EncryptedPrivateKeyPassword);
                    byte[] exportedSeed = kem.ExportPrivateSeed();
                    AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, exportedSeed);
                });
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_PrivateKey_Ambiguous_Throws()
        {
            string pem = $"""
            {WritePem("ENCRYPTED PRIVATE KEY", MLKemTestData.IetfMlKem512EncryptedPrivateKeyBoth)}
            {WritePem("ENCRYPTED PRIVATE KEY", MLKemTestData.IetfMlKem768EncryptedPrivateKeyBoth)}
            """;
            AssertImportFromEncryptedPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source",
                    () => importer(pem, MLKemTestData.EncryptedPrivateKeyPassword));
            });
        }

        [Fact]
        public static void ImportFromEncryptedPem_PrivateKey_DoesNotImportNonEncrypted()
        {
            string pem = WritePem("PRIVATE KEY", MLKemTestData.IetfMlKem512PrivateKeyBoth);
            AssertImportFromEncryptedPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source",
                    () => importer(pem, ""));
            });
        }

        [Fact]
        public static void ImportFromEncryptedPem_NoUnderstoodPem_Throws()
        {
            string pem = """
            -----BEGIN UNKNOWN-----
            cGNq
            -----END UNKNOWN-----
            """;
            AssertImportFromEncryptedPem(importer =>
            {
                AssertExtensions.Throws<ArgumentException>("source",
                    () => importer(pem, ""));
            });
        }

        [Fact]
        public static void ImportFromEncryptedPem_PrivateKey_IgnoresNotUnderstoodPems()
        {
            string pem = $"""
            -----BEGIN UNKNOWN-----
            cGNq
            -----END UNKNOWN-----
            {WritePem("ENCRYPTED PRIVATE KEY", MLKemTestData.IetfMlKem768EncryptedPrivateKeyBoth)}
            """;
            AssertImportFromEncryptedPem(importer =>
            {
                using MLKem kem = importer(pem, MLKemTestData.EncryptedPrivateKeyPassword);
                byte[] exportedSeed = kem.ExportPrivateSeed();
                AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, exportedSeed);
            });
        }

        [Fact]
        public static void ImportFromEncryptedPem_PrivateKey_WrongPassword()
        {
            string pem = WritePem("ENCRYPTED PRIVATE KEY", MLKemTestData.IetfMlKem768EncryptedPrivateKeyBoth);
            AssertImportFromEncryptedPem(importer =>
            {
                Assert.Throws<CryptographicException>(
                    () => importer(pem, "WRONG"));
            });
        }

        private static void AssertImportFromPem(Action<Func<string, MLKem>> callback)
        {
            callback(static (string pem) => MLKem.ImportFromPem(pem));
            callback(static (string pem) => MLKem.ImportFromPem(pem.AsSpan()));
        }

        private static void AssertImportFromEncryptedPem(Action<Func<string, string, MLKem>> callback)
        {
            callback(static (string pem, string password) => MLKem.ImportFromEncryptedPem(pem, password));

            callback(static (string pem, string password) => MLKem.ImportFromEncryptedPem(
                pem.AsSpan(),
                password.AsSpan()));

            callback(static (string pem, string password) => MLKem.ImportFromEncryptedPem(
                pem,
                Encoding.UTF8.GetBytes(password)));

            callback(static (string pem, string password) => MLKem.ImportFromEncryptedPem(
                pem.AsSpan(),
                new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(password))));
        }

        public static IEnumerable<(MLKemAlgorithm Algorithm, byte[] Pkcs8Seed)> Pkcs8PrivateKeySeedTestData
        {
            get
            {
                yield return (MLKemAlgorithm.MLKem512, MLKemTestData.IetfMlKem512PrivateKeySeed);
                yield return (MLKemAlgorithm.MLKem768, MLKemTestData.IetfMlKem768PrivateKeySeed);
                yield return (MLKemAlgorithm.MLKem1024, MLKemTestData.IetfMlKem1024PrivateKeySeed);
            }
        }

        public static IEnumerable<(MLKemAlgorithm Algorithm, byte[] Pkcs8Seed)> Pkcs8EncryptedPrivateKeySeedTestData
        {
            get
            {
                yield return (MLKemAlgorithm.MLKem512, MLKemTestData.IetfMlKem512EncryptedPrivateKeySeed);
                yield return (MLKemAlgorithm.MLKem768, MLKemTestData.IetfMlKem768EncryptedPrivateKeySeed);
                yield return (MLKemAlgorithm.MLKem1024, MLKemTestData.IetfMlKem1024EncryptedPrivateKeySeed);
            }
        }

        public static IEnumerable<(MLKemAlgorithm Algorithm, byte[] Pkcs8ExpandedKey, byte[] DecapsulationKey)> Pkcs8PrivateKeyExpandedKeyTestData
        {
            get
            {
                yield return
                (
                    MLKemAlgorithm.MLKem512,
                    MLKemTestData.IetfMlKem512PrivateKeyExpandedKey,
                    MLKemTestData.IetfMlKem512PrivateKeyDecapsulationKey
                );
                yield return
                (
                    MLKemAlgorithm.MLKem768,
                    MLKemTestData.IetfMlKem768PrivateKeyExpandedKey,
                    MLKemTestData.IetfMlKem768PrivateKeyDecapsulationKey
                );
                yield return
                (
                    MLKemAlgorithm.MLKem1024,
                    MLKemTestData.IetfMlKem1024PrivateKeyExpandedKey,
                    MLKemTestData.IetfMlKem1024PrivateKeyDecapsulationKey
                );
            }
        }

        public static IEnumerable<(MLKemAlgorithm Algorithm, byte[] Pkcs8ExpandedKey, byte[] DecapsulationKey)> Pkcs8EncryptedPrivateKeyExpandedKeyTestData
        {
            get
            {
                yield return
                (
                    MLKemAlgorithm.MLKem512,
                    MLKemTestData.IetfMlKem512EncryptedPrivateKeyExpandedKey,
                    MLKemTestData.IetfMlKem512PrivateKeyDecapsulationKey
                );
                yield return
                (
                    MLKemAlgorithm.MLKem768,
                    MLKemTestData.IetfMlKem768EncryptedPrivateKeyExpandedKey,
                    MLKemTestData.IetfMlKem768PrivateKeyDecapsulationKey
                );
                yield return
                (
                    MLKemAlgorithm.MLKem1024,
                    MLKemTestData.IetfMlKem1024EncryptedPrivateKeyExpandedKey,
                    MLKemTestData.IetfMlKem1024PrivateKeyDecapsulationKey
                );
            }
        }

        public static IEnumerable<(MLKemAlgorithm Algorithm, byte[] Pkcs8Both, byte[] DecapsulationKey)> Pkcs8PrivateKeyBothTestData
        {
            get
            {
                yield return
                (
                    MLKemAlgorithm.MLKem512,
                    MLKemTestData.IetfMlKem512PrivateKeyBoth,
                    MLKemTestData.IetfMlKem512PrivateKeyDecapsulationKey
                );
                yield return
                (
                    MLKemAlgorithm.MLKem768,
                    MLKemTestData.IetfMlKem768PrivateKeyBoth,
                    MLKemTestData.IetfMlKem768PrivateKeyDecapsulationKey
                );
                yield return
                (
                    MLKemAlgorithm.MLKem1024,
                    MLKemTestData.IetfMlKem1024PrivateKeyBoth,
                    MLKemTestData.IetfMlKem1024PrivateKeyDecapsulationKey
                );
            }
        }

        public static IEnumerable<(MLKemAlgorithm Algorithm, byte[] Pkcs8Both, byte[] DecapsulationKey)> Pkcs8EncryptedPrivateKeyBothTestData
        {
            get
            {
                yield return
                (
                    MLKemAlgorithm.MLKem512,
                    MLKemTestData.IetfMlKem512EncryptedPrivateKeyBoth,
                    MLKemTestData.IetfMlKem512PrivateKeyDecapsulationKey
                );
                yield return
                (
                    MLKemAlgorithm.MLKem768,
                    MLKemTestData.IetfMlKem768EncryptedPrivateKeyBoth,
                    MLKemTestData.IetfMlKem768PrivateKeyDecapsulationKey
                );
                yield return
                (
                    MLKemAlgorithm.MLKem1024,
                    MLKemTestData.IetfMlKem1024EncryptedPrivateKeyBoth,
                    MLKemTestData.IetfMlKem1024PrivateKeyDecapsulationKey
                );
            }
        }

        public static IEnumerable<(MLKemAlgorithm Algorithm, byte[] spki)> SubjectPublicKeyInfoTestData
        {
            get
            {
                yield return (MLKemAlgorithm.MLKem512, MLKemTestData.IetfMlKem512Spki);
                yield return (MLKemAlgorithm.MLKem768, MLKemTestData.IetfMlKem768Spki);
                yield return (MLKemAlgorithm.MLKem1024, MLKemTestData.IetfMlKem1024Spki);
            }
        }

        private static string WritePem(string label, byte[] contents)
        {
            string base64 = Convert.ToBase64String(contents, Base64FormattingOptions.InsertLineBreaks);
            return $"-----BEGIN {label}-----\n{base64}\n-----END {label}-----";
        }

        private static byte[] Pkcs8Encode(
            string oid,
            ReadOnlyMemory<byte>? seed = default,
            ReadOnlyMemory<byte>? expandedKey = default,
            ReadOnlyMemory<byte>? algorithmParameters = default)
        {
            byte[] EncodePrivateKey()
            {
                AsnWriter writer = new(AsnEncodingRules.BER);

                if (seed.HasValue && expandedKey.HasValue)
                {
                    using (writer.PushSequence())
                    {
                        writer.WriteOctetString(seed.Value.Span);
                        writer.WriteOctetString(expandedKey.Value.Span);
                    }
                }
                else if (seed.HasValue)
                {
                    writer.WriteOctetString(seed.Value.Span, new Asn1Tag(TagClass.ContextSpecific, 0));
                }
                else if (expandedKey.HasValue)
                {
                    writer.WriteOctetString(expandedKey.Value.Span);
                }
                else
                {
                    Assert.Fail("Seed or ExpandedKey must be provided.");
                    return null;
                }

                return writer.Encode();
            }

            AsnWriter writer = new(AsnEncodingRules.DER);
            using (writer.PushSequence())
            {
                writer.WriteInteger(0); //Version

                using (writer.PushSequence()) // AlgorithmIdentifier
                {
                    writer.WriteObjectIdentifier(oid);

                    if (algorithmParameters.HasValue)
                    {
                        writer.WriteEncodedValue(algorithmParameters.Value.Span);
                    }
                }

                writer.WriteOctetString(EncodePrivateKey());
            }

            return writer.Encode();
        }

        private static string GetOid(this MLKemAlgorithm algorithm)
        {
            if (algorithm == MLKemAlgorithm.MLKem512)
            {
                return MLKemTestData.MlKem512Oid;
            }
            if (algorithm == MLKemAlgorithm.MLKem768)
            {
                return MLKemTestData.MlKem768Oid;
            }
            if (algorithm == MLKemAlgorithm.MLKem1024)
            {
                return MLKemTestData.MlKem1024Oid;
            }

            Assert.Fail("$Unexpected algorithm identifier {algorithm.Name}.");
            return null;
        }
    }
}
