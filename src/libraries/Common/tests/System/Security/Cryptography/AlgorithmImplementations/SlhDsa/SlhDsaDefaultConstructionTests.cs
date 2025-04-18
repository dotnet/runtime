// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Asn1;
using System.Text;
using Microsoft.Diagnostics.Runtime.Interop;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public sealed class SlhDsaDefaultConstructionTests : SlhDsaConstructionTestsBase
    {
        [Fact]
        public static void NullArgumentValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => SlhDsa.GenerateKey(null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => SlhDsa.ImportSlhDsaPublicKey(null, []));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => SlhDsa.ImportSlhDsaSecretKey(null, []));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void ArgumentValidation(SlhDsaAlgorithm algorithm)
        {
            int publicKeySize = algorithm.PublicKeySizeInBytes;
            int secretKeySize = algorithm.SecretKeySizeInBytes;

            AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportSlhDsaPublicKey(algorithm, new byte[publicKeySize - 1]));
            AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportSlhDsaPublicKey(algorithm, new byte[publicKeySize + 1]));
            AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportSlhDsaPublicKey(algorithm, []));
            AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportSlhDsaSecretKey(algorithm, new byte[secretKeySize - 1]));
            AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportSlhDsaSecretKey(algorithm, new byte[secretKeySize + 1]));
            AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportSlhDsaSecretKey(algorithm, []));
            AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportFromPem([]));

            // TODO add remaining imports
        }

        // TODO Test following cases:
        // <exception cref="ArgumentException">
        //   <para><paramref name="source" /> contains an encrypted PEM-encoded key.</para>
        //   <para>-or-</para>
        //   <para><paramref name="source" /> contains multiple PEM-encoded SLH-DSA keys.</para>
        //   <para>-or-</para>
        //   <para><paramref name="source" /> contains no PEM-encoded SLH-DSA keys.</para>
        // </exception>

        [ConditionalTheory(typeof(SlhDsaTestData), nameof(SlhDsaTestData.IsNotSupported))]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void ThrowIfNotSupported_NonNullArguments(SlhDsaAlgorithm algorithm)
        {
            // The public key size is smaller so this can be used for both:
            byte[] input = new byte[algorithm.SecretKeySizeInBytes];

            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.GenerateKey(algorithm));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportFromEncryptedPem(ReadOnlySpan<char>.Empty, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportFromEncryptedPem(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportFromPem(ReadOnlySpan<char>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportPkcs8PrivateKey(ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, input.AsSpan(0, algorithm.PublicKeySizeInBytes)));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, input.AsSpan(0, algorithm.SecretKeySizeInBytes)));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSubjectPublicKeyInfo(ReadOnlySpan<byte>.Empty));
        }

        [ConditionalTheory(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void SlhDsaIsOnlyPublicAncestor_GenerateKey(SlhDsaAlgorithm algorithm)
        {
            AssertSlhDsaIsOnlyPublicAncestor(() => SlhDsa.GenerateKey(algorithm));
        }

        [ConditionalTheory(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        [MemberData(nameof(NistKeyGenTestVectorsData))]
        public void SlhDsaIsOnlyPublicAncestor_Import(SlhDsaTestData.SlhDsaKeyGenTestVector vector)
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

        [Fact]
        public void ImportPkcs8PrivateKeyIetf()
        {
            using SlhDsa slhDsa = SlhDsa.ImportPkcs8PrivateKey(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyPkcs8);
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s, slhDsa.Algorithm);

            byte[] secretKey = new byte[SlhDsaAlgorithm.SlhDsaSha2_128s.SecretKeySizeInBytes];
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s.SecretKeySizeInBytes, slhDsa.ExportSlhDsaSecretKey(secretKey));
            AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue, secretKey);
        }

        [Fact]
        public void ImportPkcs8PublicKeyIetf()
        {
            using SlhDsa slhDsa = SlhDsa.ImportSubjectPublicKeyInfo(SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyPkcs8);
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s, slhDsa.Algorithm);

            byte[] secretKey = new byte[SlhDsaAlgorithm.SlhDsaSha2_128s.PublicKeySizeInBytes];
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s.PublicKeySizeInBytes, slhDsa.ExportSlhDsaPublicKey(secretKey));
            AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyValue, secretKey);
        }

        [Fact]
        public void ImportPemPrivateKeyIetf()
        {
            string pem = WritePem("PRIVATE KEY", SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyPkcs8);
            using SlhDsa slhDsa = SlhDsa.ImportFromPem(pem);
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s, slhDsa.Algorithm);

            byte[] secretKey = new byte[SlhDsaAlgorithm.SlhDsaSha2_128s.SecretKeySizeInBytes];
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s.SecretKeySizeInBytes, slhDsa.ExportSlhDsaSecretKey(secretKey));
            AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPrivateKeyValue, secretKey);
        }

        [Fact]
        public void ImportPemPublicKeyIetf()
        {
            string pem = WritePem("PUBLIC KEY", SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyPkcs8);
            using SlhDsa slhDsa = SlhDsa.ImportFromPem(pem);
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s, slhDsa.Algorithm);

            byte[] secretKey = new byte[SlhDsaAlgorithm.SlhDsaSha2_128s.PublicKeySizeInBytes];
            Assert.Equal(SlhDsaAlgorithm.SlhDsaSha2_128s.PublicKeySizeInBytes, slhDsa.ExportSlhDsaPublicKey(secretKey));
            AssertExtensions.SequenceEqual(SlhDsaTestData.IetfSlhDsaSha2_128sPublicKeyValue, secretKey);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ImportPkcs8PublicKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = SlhDsa.ImportSubjectPublicKeyInfo(info.Pkcs8PublicKey);
            Assert.Equal(info.Algorithm, slhDsa.Algorithm);

            byte[] publicKey = new byte[info.Algorithm.PublicKeySizeInBytes];
            Assert.Equal(info.Algorithm.PublicKeySizeInBytes, slhDsa.ExportSlhDsaPublicKey(publicKey));
            AssertExtensions.SequenceEqual(info.PublicKey, publicKey);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ImportPkcs8PrivateKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
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
        public void ImportEncryptedPkcs8PrivateKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
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
        public void ImportEncryptedPem(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
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
                // also do with unencrypted pkcs8
            });
        }

        [Fact]
        public void ImportFromPem_UnknownLabel()
        {
            AssertImportFromPem(import =>
            {
                string pem = WritePem("UNKNOWN LABEL", []);
                AssertExtensions.Throws<ArgumentException>("source", () => SlhDsa.ImportFromPem(pem));

                pem = WritePem("CERTIFICATE", []);
                AssertExtensions.Throws<ArgumentException>("source", () => import(pem));
            });
        }

        [Fact]
        public void ImportFromEncryptedPem_UnknownLabel()
        {
            AssertImportFromEncryptedPem(import =>
            {
                string pem = WritePem("UNKNOWN LABEL", []);
                AssertExtensions.Throws<ArgumentException>("source", () => import(pem, "password"));

                pem = WritePem("CERTIFICATE", []);
                AssertExtensions.Throws<ArgumentException>("source", () => import(pem, "password"));
            });
        }

        [Fact]
        public void ImportFromPem_ContainsNoKeys()
        {
            AssertImportFromPem(import =>
            {
                AssertExtensions.Throws<ArgumentException>("source", () => import(string.Empty));
            });
        }

        [Fact]
        public void ImportFromEncryptedPem_ContainsNoKeys()
        {
            AssertImportFromEncryptedPem(import => 
            {
                AssertExtensions.Throws<ArgumentException>("source", () => import(string.Empty, "password"));
            });
        }

        [Fact]
        public void ImportFromPem_ContainsEncryptedKey()
        {
            AssertImportFromPem(import =>
            {
                string pem = WritePem("ENCRYPTED PRIVATE KEY", []);
                AssertExtensions.Throws<ArgumentException>("source", () => import(pem));
            });
        }

        [Fact]
        public void ImportFromPem_MultipleKeys()
        {
            AssertImportFromPem(import =>
            {
                string pem = WritePem("PUBLIC KEY", []) + '\n' + WritePem("PUBLIC KEY", []);
                AssertExtensions.Throws<ArgumentException>("source", () => import(pem));
            });
        }

        [Fact]
        public void ImportFromEncryptedPem_MultipleKeys()
        {
            AssertImportFromEncryptedPem(import =>
            {
                string pem = WritePem("PUBLIC KEY", []) + '\n' + WritePem("PUBLIC KEY", []);
                AssertExtensions.Throws<ArgumentException>("source", () => import(pem, "password"));
            });
        }

        private static void AssertImportFromPem(Action<Func<string, SlhDsa>> callback)
        {
            callback(static (string pem) => SlhDsa.ImportFromPem(pem));
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

        private static void AssertImportFromEncryptedPem(Action<Func<string, string, SlhDsa>> callback)
        {
            callback(static (string pem, string password) => SlhDsa.ImportFromEncryptedPem(pem, password));
            callback(static (string pem, string password) => SlhDsa.ImportFromEncryptedPem(pem, Encoding.UTF8.GetBytes(password)));
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

        protected override SlhDsa GenerateKey(SlhDsaAlgorithm algorithm) =>
            SlhDsa.GenerateKey(algorithm);

        protected override SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            SlhDsa.ImportSlhDsaPublicKey(algorithm, source);

        protected override SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            SlhDsa.ImportSlhDsaSecretKey(algorithm, source);
    }
}
