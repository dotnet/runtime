// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            // TODO add remaining imports
        }

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

        protected override SlhDsa GenerateKey(SlhDsaAlgorithm algorithm) =>
            SlhDsa.GenerateKey(algorithm);

        protected override SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            SlhDsa.ImportSlhDsaPublicKey(algorithm, source);

        protected override SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            SlhDsa.ImportSlhDsaSecretKey(algorithm, source);
    }
}
