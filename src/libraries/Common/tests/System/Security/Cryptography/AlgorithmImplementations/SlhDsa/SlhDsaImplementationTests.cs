// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    [ConditionalClass(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
    public sealed class SlhDsaImplementationTests : SlhDsaTests
    {
        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void SlhDsaIsOnlyPublicAncestor_GenerateKey(SlhDsaAlgorithm algorithm)
        {
            AssertSlhDsaIsOnlyPublicAncestor(() => SlhDsa.GenerateKey(algorithm));
        }

        [Theory]
        [MemberData(nameof(NistKeyGenTestVectorsData))]
        public static void SlhDsaIsOnlyPublicAncestor_Import(SlhDsaTestData.SlhDsaKeyGenTestVector vector)
        {
            AssertSlhDsaIsOnlyPublicAncestor(() => SlhDsa.ImportSlhDsaSecretKey(vector.Algorithm, vector.SecretKey));
            AssertSlhDsaIsOnlyPublicAncestor(() => SlhDsa.ImportSlhDsaPublicKey(vector.Algorithm, vector.PublicKey));
        }

        private static void AssertSlhDsaIsOnlyPublicAncestor(Func<SlhDsa> createKey)
        {
            using SlhDsa key = createKey();
            Type keyType = key.GetType();
            while (keyType != null && keyType != typeof(SlhDsa))
            {
                AssertExtensions.FalseExpression(keyType.IsPublic);
                keyType = keyType.BaseType;
            }

            Assert.Equal(typeof(SlhDsa), keyType);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void GenerateExportImportPkcs8_PrivateKey(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] key = new byte[slhDsa.Algorithm.SecretKeySizeInBytes];
            slhDsa.ExportSlhDsaSecretKey(key);

            // Export and import it using PKCS#8
            byte[] pkcs8 = slhDsa.ExportPkcs8PrivateKey();
            using SlhDsa importedSlhDsa = SlhDsa.ImportPkcs8PrivateKey(pkcs8);
            byte[] importedKey = new byte[slhDsa.Algorithm.SecretKeySizeInBytes];
            importedSlhDsa.ExportSlhDsaSecretKey(importedKey);

            // The keys should be the same
            Assert.Equal(algorithm, importedSlhDsa.Algorithm);
            AssertExtensions.SequenceEqual(key, importedKey);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void GenerateExportImportPkcs8_PublicKey(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] key = new byte[slhDsa.Algorithm.PublicKeySizeInBytes];
            slhDsa.ExportSlhDsaPublicKey(key);

            // Export and import it using PKCS#8
            byte[] pkcs8 = slhDsa.ExportSubjectPublicKeyInfo();
            using SlhDsa importedSlhDsa = SlhDsa.ImportSubjectPublicKeyInfo(pkcs8);
            byte[] importedKey = new byte[slhDsa.Algorithm.PublicKeySizeInBytes];
            importedSlhDsa.ExportSlhDsaPublicKey(importedKey);

            // The keys should be the same
            Assert.Equal(algorithm, importedSlhDsa.Algorithm);
            AssertExtensions.SequenceEqual(key, importedKey);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void GenerateExportImportPem_PrivateKey(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] key = new byte[slhDsa.Algorithm.SecretKeySizeInBytes];
            slhDsa.ExportSlhDsaSecretKey(key);

            // Export and import it using PKCS#8
            string pkcs8 = slhDsa.ExportPkcs8PrivateKeyPem();
            using SlhDsa importedSlhDsa = SlhDsa.ImportFromPem(pkcs8);
            byte[] importedKey = new byte[slhDsa.Algorithm.SecretKeySizeInBytes];
            importedSlhDsa.ExportSlhDsaSecretKey(importedKey);

            // The keys should be the same
            Assert.Equal(algorithm, importedSlhDsa.Algorithm);
            AssertExtensions.SequenceEqual(key, importedKey);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void GenerateExportImportPem_PublicKey(SlhDsaAlgorithm algorithm)
        {
            // Generate new key
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] key = new byte[slhDsa.Algorithm.PublicKeySizeInBytes];
            slhDsa.ExportSlhDsaPublicKey(key);

            // Export and import it using PKCS#8
            string pkcs8 = slhDsa.ExportSubjectPublicKeyInfoPem();
            using SlhDsa importedSlhDsa = SlhDsa.ImportFromPem(pkcs8);
            byte[] importedKey = new byte[slhDsa.Algorithm.PublicKeySizeInBytes];
            importedSlhDsa.ExportSlhDsaPublicKey(importedKey);

            // The keys should be the same
            Assert.Equal(algorithm, importedSlhDsa.Algorithm);
            AssertExtensions.SequenceEqual(key, importedKey);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void EncryptedPkcs8PrivateKey_RoundTrip(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = SlhDsa.ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);
            SlhDsaTestHelpers.AssertEncryptedExportPkcs8PrivateKey(slhDsa, info.EncryptionPassword, info.EncryptionParameters, pkcs8 =>
            {
                using SlhDsa importedSlhDsa = SlhDsa.ImportEncryptedPkcs8PrivateKey(info.EncryptionPassword, pkcs8);

                byte[] secretKey = new byte[info.Algorithm.SecretKeySizeInBytes];
                importedSlhDsa.ExportSlhDsaSecretKey(secretKey);
                AssertExtensions.SequenceEqual(info.SecretKey, secretKey);

                SlhDsaTestHelpers.AssertEncryptedPkcs8PrivateKeyContents(info.EncryptionParameters, pkcs8);
            });
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void ImportPkcs8WithTrailingData(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);

            byte[] privateKey = slhDsa.ExportPkcs8PrivateKey();
            Array.Resize(ref privateKey, privateKey.Length + 1);
            AssertExtensions.Throws<CryptographicException>(() => SlhDsa.ImportPkcs8PrivateKey(privateKey));

            byte[] publicKey = slhDsa.ExportSubjectPublicKeyInfo();
            Array.Resize(ref publicKey, publicKey.Length + 1);
            AssertExtensions.Throws<CryptographicException>(() => SlhDsa.ImportSubjectPublicKeyInfo(publicKey));

            PbeParameters pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA1, 42);
            byte[] encryptedPrivateKeyCharPassword = slhDsa.ExportEncryptedPkcs8PrivateKey("password", pbeParameters);
            Array.Resize(ref encryptedPrivateKeyCharPassword, encryptedPrivateKeyCharPassword.Length + 1);
            AssertExtensions.Throws<CryptographicException>(() => SlhDsa.ImportEncryptedPkcs8PrivateKey("password", encryptedPrivateKeyCharPassword));

            byte[] encryptedPrivateKeyBytePassword = slhDsa.ExportEncryptedPkcs8PrivateKey("password"u8, pbeParameters);
            Array.Resize(ref encryptedPrivateKeyBytePassword, encryptedPrivateKeyBytePassword.Length + 1);
            AssertExtensions.Throws<CryptographicException>(() => SlhDsa.ImportEncryptedPkcs8PrivateKey("password"u8, encryptedPrivateKeyBytePassword));
        }

        [Fact]
        public static void ImportPkcs8PrivateKeyIetf()
        {
            using SlhDsa slhDsa = SlhDsa.ImportPkcs8PrivateKey(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyPkcs8);
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s, slhDsa.Algorithm);

            byte[] secretKey = new byte[SlhDsaAlgorithm.SlhDsaSha2_128s.SecretKeySizeInBytes];
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s.SecretKeySizeInBytes, slhDsa.ExportSlhDsaSecretKey(secretKey));
            AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue, secretKey);
        }

        [Fact]
        public static void ImportPkcs8PublicKeyIetf()
        {
            using SlhDsa slhDsa = SlhDsa.ImportSubjectPublicKeyInfo(SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyPkcs8);
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s, slhDsa.Algorithm);

            byte[] secretKey = new byte[SlhDsaAlgorithm.SlhDsaSha2_128s.PublicKeySizeInBytes];
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s.PublicKeySizeInBytes, slhDsa.ExportSlhDsaPublicKey(secretKey));
            AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyValue, secretKey);
        }

        [Fact]
        public static void ImportPemPrivateKeyIetf()
        {
            string pem = PemEncoding.WriteString("PRIVATE KEY", SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyPkcs8);
            using SlhDsa slhDsa = SlhDsa.ImportFromPem(pem);
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s, slhDsa.Algorithm);

            byte[] secretKey = new byte[SlhDsaAlgorithm.SlhDsaSha2_128s.SecretKeySizeInBytes];
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s.SecretKeySizeInBytes, slhDsa.ExportSlhDsaSecretKey(secretKey));
            AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue, secretKey);
        }

        [Fact]
        public static void ImportPemPublicKeyIetf()
        {
            string pem = PemEncoding.WriteString("PUBLIC KEY", SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyPkcs8);
            using SlhDsa slhDsa = SlhDsa.ImportFromPem(pem);
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s, slhDsa.Algorithm);

            byte[] secretKey = new byte[SlhDsaAlgorithm.SlhDsaSha2_128s.PublicKeySizeInBytes];
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s.PublicKeySizeInBytes, slhDsa.ExportSlhDsaPublicKey(secretKey));
            AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyValue, secretKey);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public static void ImportPkcs8PublicKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = SlhDsa.ImportSubjectPublicKeyInfo(info.Pkcs8PublicKey);
            Assert.Equal(info.Algorithm, slhDsa.Algorithm);

            byte[] publicKey = new byte[info.Algorithm.PublicKeySizeInBytes];
            Assert.Equal(info.Algorithm.PublicKeySizeInBytes, slhDsa.ExportSlhDsaPublicKey(publicKey));
            AssertExtensions.SequenceEqual(info.PublicKey, publicKey);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public static void ImportPkcs8PrivateKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            AssertImportPkcs8PrivateKey(createSlhDsa =>
            {
                using SlhDsa slhDsa = createSlhDsa(info.Pkcs8PrivateKey);

                byte[] secretKey = new byte[info.Algorithm.SecretKeySizeInBytes];
                Assert.Equal(info.Algorithm.SecretKeySizeInBytes, slhDsa.ExportSlhDsaSecretKey(secretKey));
                AssertExtensions.SequenceEqual(info.SecretKey, secretKey);

                byte[] publicKey = new byte[info.Algorithm.PublicKeySizeInBytes];
                Assert.Equal(info.Algorithm.PublicKeySizeInBytes, slhDsa.ExportSlhDsaPublicKey(publicKey));
                AssertExtensions.SequenceEqual(info.PublicKey, publicKey);

                // TODO test what happens when malformed pkcs8 is passed in - write tests modifying it and ensuring errors are thrown
                // also do with unencrypted pkcs8 and public key
            });
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public static void ImportEncryptedPkcs8PrivateKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            AssertImportEncryptedPkcs8PrivateKey(createSlhDsa =>
            {
                using SlhDsa slhDsa = createSlhDsa(info.EncryptionPassword, info.Pkcs8EncryptedPrivateKey);

                byte[] secretKey = new byte[info.Algorithm.SecretKeySizeInBytes];
                Assert.Equal(info.Algorithm.SecretKeySizeInBytes, slhDsa.ExportSlhDsaSecretKey(secretKey));
                AssertExtensions.SequenceEqual(info.SecretKey, secretKey);

                byte[] publicKey = new byte[info.Algorithm.PublicKeySizeInBytes];
                Assert.Equal(info.Algorithm.PublicKeySizeInBytes, slhDsa.ExportSlhDsaPublicKey(publicKey));
                AssertExtensions.SequenceEqual(info.PublicKey, publicKey);

                // TODO test what happens when malformed pkcs8 is passed in - write tests modifying it and ensuring errors are thrown
                // also do with unencrypted pkcs8 and public key
            });
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public static void ImportEncryptedPem(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            AssertImportEncryptedPkcs8PrivateKey(createSlhDsa =>
            {
                using SlhDsa slhDsa = createSlhDsa(info.EncryptionPassword, info.Pkcs8EncryptedPrivateKey);

                byte[] secretKey = new byte[info.Algorithm.SecretKeySizeInBytes];
                Assert.Equal(info.Algorithm.SecretKeySizeInBytes, slhDsa.ExportSlhDsaSecretKey(secretKey));
                AssertExtensions.SequenceEqual(info.SecretKey, secretKey);

                byte[] publicKey = new byte[info.Algorithm.PublicKeySizeInBytes];
                Assert.Equal(info.Algorithm.PublicKeySizeInBytes, slhDsa.ExportSlhDsaPublicKey(publicKey));
                AssertExtensions.SequenceEqual(info.PublicKey, publicKey);
            });
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ImportSecretKey_ExportPkcs8PrivateKey_Pem(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);

            string pem = slhDsa.ExportPkcs8PrivateKeyPem();
            AssertPemsEqual(PemEncoding.WriteString("PRIVATE KEY", info.Pkcs8PrivateKey), pem);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ImportSecretKey_ExportPkcs8PublicKey_Pem(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);

            string pem = slhDsa.ExportSubjectPublicKeyInfoPem();
            AssertPemsEqual(PemEncoding.WriteString("PUBLIC KEY", info.Pkcs8PublicKey), pem);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ImportPublicKey_ExportPkcs8PublicKey_Pem(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaPublicKey(info.Algorithm, info.PublicKey);

            string pem = slhDsa.ExportSubjectPublicKeyInfoPem();
            AssertPemsEqual(PemEncoding.WriteString("PUBLIC KEY", info.Pkcs8PublicKey), pem);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ImportSecretKey_ExportPkcs8PrivateKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);
            SlhDsaTestHelpers.AssertExportPkcs8PrivateKey(slhDsa, pkcs8 => AssertExtensions.SequenceEqual(info.Pkcs8PrivateKey, pkcs8));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ImportSecretKey_ExportPkcs8PublicKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);
            SlhDsaTestHelpers.AssertExportSubjectPublicKeyInfo(slhDsa, spki => AssertExtensions.SequenceEqual(info.Pkcs8PublicKey, spki));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ImportPublicKey_ExportPkcs8PublicKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaPublicKey(info.Algorithm, info.PublicKey);
            SlhDsaTestHelpers.AssertExportSubjectPublicKeyInfo(slhDsa, spki => AssertExtensions.SequenceEqual(info.Pkcs8PublicKey, spki));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ExportEncryptedPkcs8PrivateKey_PbeParameters(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);
            SlhDsaTestHelpers.AssertEncryptedExportPkcs8PrivateKey(slhDsa, info.EncryptionPassword, info.EncryptionParameters, pkcs8 =>
            {
                SlhDsaTestHelpers.AssertEncryptedPkcs8PrivateKeyContents(info.EncryptionParameters, pkcs8);
            });
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ExportKey_DestinationTooSmall(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);
            byte[] pkcs8PrivateKey = slhDsa.ExportPkcs8PrivateKey();
            byte[] spki = slhDsa.ExportSubjectPublicKeyInfo();
            byte[] encryptedPkcs8 = slhDsa.ExportEncryptedPkcs8PrivateKey(info.EncryptionPassword, info.EncryptionParameters);
            byte[] largeBuffer = new byte[2 * Math.Max(pkcs8PrivateKey.Length, Math.Max(spki.Length, encryptedPkcs8.Length))];

            int bytesWritten = -1;

            // TryExportPkcs8PrivateKey
            AssertExtensions.FalseExpression(slhDsa.TryExportPkcs8PrivateKey(Span<byte>.Empty, out bytesWritten));               // Empty
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportPkcs8PrivateKey(largeBuffer, out bytesWritten));                     // Too large
            AssertExtensions.Equal(pkcs8PrivateKey.Length, bytesWritten);
            AssertExtensions.FalseExpression(slhDsa.TryExportPkcs8PrivateKey(pkcs8PrivateKey.AsSpan(0..^1), out bytesWritten));  // Too small
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportPkcs8PrivateKey(pkcs8PrivateKey, out bytesWritten));                 // Exact size
            AssertExtensions.Equal(pkcs8PrivateKey.Length, bytesWritten);

            // TryExportSubjectPublicKeyInfo
            AssertExtensions.FalseExpression(slhDsa.TryExportSubjectPublicKeyInfo(Span<byte>.Empty, out bytesWritten));
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportSubjectPublicKeyInfo(largeBuffer, out bytesWritten));
            AssertExtensions.Equal(spki.Length, bytesWritten);
            AssertExtensions.FalseExpression(slhDsa.TryExportSubjectPublicKeyInfo(spki.AsSpan(0..^1), out bytesWritten));
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportSubjectPublicKeyInfo(spki, out bytesWritten));
            AssertExtensions.Equal(spki.Length, bytesWritten);

            // TryExportEncryptedPkcs8PrivateKey (string password)
            AssertExtensions.FalseExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password", info.EncryptionParameters, Span<byte>.Empty, out bytesWritten));
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password", info.EncryptionParameters, largeBuffer, out bytesWritten));
            AssertExtensions.Equal(encryptedPkcs8.Length, bytesWritten);
            AssertExtensions.FalseExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password", info.EncryptionParameters, encryptedPkcs8.AsSpan(0..^1), out bytesWritten));
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password", info.EncryptionParameters, encryptedPkcs8, out bytesWritten));
            AssertExtensions.Equal(encryptedPkcs8.Length, bytesWritten);

            if (info.EncryptionParameters.EncryptionAlgorithm is not PbeEncryptionAlgorithm.TripleDes3KeyPkcs12)
            {
                // TryExportEncryptedPkcs8PrivateKey (byte[] password)
                AssertExtensions.FalseExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password"u8, info.EncryptionParameters, Span<byte>.Empty, out bytesWritten));
                AssertExtensions.Equal(0, bytesWritten);
                AssertExtensions.TrueExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password"u8, info.EncryptionParameters, largeBuffer, out bytesWritten));
                AssertExtensions.Equal(encryptedPkcs8.Length, bytesWritten);
                AssertExtensions.FalseExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password"u8, info.EncryptionParameters, encryptedPkcs8.AsSpan(0..^1), out bytesWritten));
                AssertExtensions.Equal(0, bytesWritten);
                AssertExtensions.TrueExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password"u8, info.EncryptionParameters, encryptedPkcs8, out bytesWritten));
                AssertExtensions.Equal(encryptedPkcs8.Length, bytesWritten);
            }
        }

        public static IEnumerable<object[]> NistKeyGenTestVectorsData =>
            from vector in SlhDsaTestData.NistKeyGenTestVectors
            select new object[] { vector };

        [ConditionalTheory(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        [MemberData(nameof(NistKeyGenTestVectorsData))]
        public void NistKeyGenerationTest(SlhDsaTestData.SlhDsaKeyGenTestVector vector)
        {
            byte[] skSeed = vector.SecretKeySeed;
            byte[] skPrf = vector.SecretKeyPrf;
            byte[] pkSeed = vector.PublicKeySeed;

            byte[] sk = vector.SecretKey;
            byte[] pk = vector.PublicKey;

            // Sanity test for input vectors: SLH-DSA keys are composed of skSeed, skPrf and pkSeed
            AssertExtensions.SequenceEqual(skSeed.AsSpan(), sk.AsSpan(0, skSeed.Length));
            AssertExtensions.SequenceEqual(skPrf.AsSpan(), sk.AsSpan(skSeed.Length, skPrf.Length));
            AssertExtensions.SequenceEqual(pkSeed.AsSpan(), sk.AsSpan(skSeed.Length + skPrf.Length, pkSeed.Length));
            AssertExtensions.SequenceEqual(pkSeed.AsSpan(), pk.AsSpan(0, pkSeed.Length));

            // Import secret key and verify exports
            using (SlhDsa secretSlhDsa = ImportSlhDsaSecretKey(vector.Algorithm, sk))
            {
                byte[] pubKey = new byte[vector.Algorithm.PublicKeySizeInBytes];
                Assert.Equal(pk.Length, secretSlhDsa.ExportSlhDsaPublicKey(pubKey));
                AssertExtensions.SequenceEqual(pk, pubKey);

                byte[] secretKey = new byte[vector.Algorithm.SecretKeySizeInBytes];
                Assert.Equal(sk.Length, secretSlhDsa.ExportSlhDsaSecretKey(secretKey));
                AssertExtensions.SequenceEqual(sk, secretKey);
            }

            // Import public key and verify exports
            using (SlhDsa publicSlhDsa = ImportSlhDsaPublicKey(vector.Algorithm, pk))
            {
                byte[] pubKey = new byte[vector.Algorithm.PublicKeySizeInBytes];
                Assert.Equal(pk.Length, publicSlhDsa.ExportSlhDsaPublicKey(pubKey));
                AssertExtensions.SequenceEqual(pk, pubKey);

                byte[] secretKey = new byte[vector.Algorithm.SecretKeySizeInBytes];
                Assert.Throws<CryptographicException>(() => publicSlhDsa.ExportSlhDsaSecretKey(secretKey));
            }
        }

        internal delegate SlhDsa ImportEncryptedPkcs8PrivateKeyCallback(string password, ReadOnlySpan<byte> pkcs8);
        private static void AssertImportEncryptedPkcs8PrivateKey(Action<ImportEncryptedPkcs8PrivateKeyCallback> callback)
        {
            callback(static (string password, ReadOnlySpan<byte> pkcs8) => SlhDsa.ImportEncryptedPkcs8PrivateKey(password, pkcs8));
            callback(static (string password, ReadOnlySpan<byte> pkcs8) => SlhDsa.ImportEncryptedPkcs8PrivateKey(Encoding.UTF8.GetBytes(password), pkcs8));

            AssertImportFromEncryptedPem(importPem =>
            {
                callback((string password, ReadOnlySpan<byte> pkcs8) =>
                {
                    string pem = PemEncoding.WriteString("ENCRYPTED PRIVATE KEY", pkcs8);
                    return importPem(pem, password);
                });
            });
        }

        internal delegate SlhDsa ImportPkcs8PrivateKeyCallback(ReadOnlySpan<byte> pkcs8);
        private static void AssertImportPkcs8PrivateKey(Action<ImportPkcs8PrivateKeyCallback> callback)
        {
            callback(SlhDsa.ImportPkcs8PrivateKey);

            AssertImportFromPem(importPem =>
            {
                callback(pkcs8 => importPem(PemEncoding.WriteString("PRIVATE KEY", pkcs8)));
            });
        }

        private static void AssertImportFromPem(Action<Func<string, SlhDsa>> callback)
        {
            callback(static (string pem) => SlhDsa.ImportFromPem(pem));
        }

        private static void AssertImportFromEncryptedPem(Action<Func<string, string, SlhDsa>> callback)
        {
            callback(static (string pem, string password) => SlhDsa.ImportFromEncryptedPem(pem, password));
            callback(static (string pem, string password) => SlhDsa.ImportFromEncryptedPem(pem, Encoding.UTF8.GetBytes(password)));
        }

        private static void AssertPemsEqual(string expectedPem, string actualPem)
        {
            (string Label, byte[] Base64Data)[] expectedPemObjects = EnumeratePem(expectedPem).ToArray();
            (string Label, byte[] Base64Data)[] actualPemObjects = EnumeratePem(actualPem).ToArray();

            AssertExtensions.TrueExpression(expectedPemObjects.Length == actualPemObjects.Length);

            for (int i = 0; i < expectedPemObjects.Length; i++)
            {
                Assert.Equal(expectedPemObjects[i].Label, actualPemObjects[i].Label);
                AssertExtensions.SequenceEqual(expectedPemObjects[i].Base64Data, actualPemObjects[i].Base64Data);
            }
        }

        private static IEnumerable<(string Label, byte[] Base64Data)> EnumeratePem(string pem)
        {
            ReadOnlyMemory<char> pemMemory = pem.AsMemory();
            while (PemEncoding.TryFind(pemMemory.Span, out PemFields fields))
            {
                byte[] data = new byte[fields.DecodedDataLength];
                AssertExtensions.TrueExpression(Convert.TryFromBase64Chars(pemMemory.Span[fields.Base64Data], data, out int bytesWritten));
                AssertExtensions.TrueExpression(bytesWritten == fields.DecodedDataLength);
                yield return (pemMemory[fields.Label].ToString(), data.AsSpan(0, bytesWritten).ToArray());
                pemMemory = pemMemory[fields.Location.End..];
            }
        }

        protected override SlhDsa GenerateKey(SlhDsaAlgorithm algorithm) => SlhDsa.GenerateKey(algorithm);
        protected override SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => SlhDsa.ImportSlhDsaPublicKey(algorithm, source);
        protected override SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => SlhDsa.ImportSlhDsaSecretKey(algorithm, source);
    }
}
