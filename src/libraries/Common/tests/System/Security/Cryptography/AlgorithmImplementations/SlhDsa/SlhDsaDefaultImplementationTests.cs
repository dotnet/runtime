// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    [ConditionalClass(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
    public sealed class SlhDsaDefaultImplementationTests : SlhDsaImplementationTestsBase
    {
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
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);
            AssertEncryptedExportPkcs8PrivateKey(slhDsa, info.EncryptionPassword, info.EncryptionParameters, pkcs8 =>
            {
                using SlhDsa importedSlhDsa = SlhDsa.ImportEncryptedPkcs8PrivateKey(info.EncryptionPassword, pkcs8);

                byte[] secretKey = new byte[info.Algorithm.SecretKeySizeInBytes];
                importedSlhDsa.ExportSlhDsaSecretKey(secretKey);
                AssertExtensions.SequenceEqual(info.SecretKey, secretKey);

                AssertEncryptedPkcs8PrivateKeyContents(info.EncryptionParameters, pkcs8);
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

        // TODO add method to test that PKCS#8 keys don't have attributes

        protected override SlhDsa GenerateKey(SlhDsaAlgorithm algorithm) => SlhDsa.GenerateKey(algorithm);
        protected override SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => SlhDsa.ImportSlhDsaPublicKey(algorithm, source);
        protected override SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => SlhDsa.ImportSlhDsaSecretKey(algorithm, source);
    }
}
