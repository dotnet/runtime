// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        protected override SlhDsa GenerateKey(SlhDsaAlgorithm algorithm) =>
            SlhDsa.GenerateKey(algorithm);

        protected override SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            SlhDsa.ImportSlhDsaPublicKey(algorithm, source);

        protected override SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            SlhDsa.ImportSlhDsaSecretKey(algorithm, source);
    }
}
