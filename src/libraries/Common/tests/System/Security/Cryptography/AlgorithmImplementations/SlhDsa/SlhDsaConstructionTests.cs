// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public class SlhDsaConstructionTests : SlhDsaTestsBase
    {
        [Fact]
        public static void NullArgumentValidation()
        {
            Assert.Throws<ArgumentNullException>(() => SlhDsa.GenerateKey(null));
            Assert.Throws<ArgumentNullException>(() => SlhDsa.ImportSlhDsaPublicKey(null, ReadOnlySpan<byte>.Empty));
            Assert.Throws<ArgumentNullException>(() => SlhDsa.ImportSlhDsaSecretKey(null, ReadOnlySpan<byte>.Empty));
            Assert.Throws<ArgumentNullException>(() => SlhDsa.ImportSlhDsaPrivateSeed(null, ReadOnlySpan<byte>.Empty));
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData))]
        public static void ArgumentValidation(SlhDsaAlgorithm algorithm)
        {
            int publicKeySize = algorithm.PublicKeySizeInBytes;
            int secretKeySize = algorithm.SecretKeySizeInBytes;
            int privateSeedSize = algorithm.PrivateSeedSizeInBytes;

            Assert.Throws<ArgumentException>(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, new byte[publicKeySize - 1]));
            Assert.Throws<ArgumentException>(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, new byte[publicKeySize + 1]));
            Assert.Throws<ArgumentException>(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, new byte[secretKeySize - 1]));
            Assert.Throws<ArgumentException>(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, new byte[secretKeySize + 1]));
            Assert.Throws<ArgumentException>(() => SlhDsa.ImportSlhDsaPrivateSeed(algorithm, new byte[privateSeedSize - 1]));
            Assert.Throws<ArgumentException>(() => SlhDsa.ImportSlhDsaPrivateSeed(algorithm, new byte[privateSeedSize + 1]));

            // TODO add remaining imports
        }

        [ConditionalTheory(nameof(NotSupportedOnPlatform))]
        [MemberData(nameof(AlgorithmsData))]
        public static void ThrowIfNotSupported_NonNullArguments(SlhDsaAlgorithm algorithm)
        {
            // The private seed and public key sizes are both smaller so this can be used for all three:
            byte[] input = new byte[algorithm.SecretKeySizeInBytes];

            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.GenerateKey(algorithm));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportFromEncryptedPem(ReadOnlySpan<char>.Empty, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportFromEncryptedPem(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportFromPem(ReadOnlySpan<char>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportPkcs8PrivateKey(ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSlhDsaPrivateSeed(algorithm, input.AsSpan(0, algorithm.PrivateSeedSizeInBytes)));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, input.AsSpan(0, algorithm.PublicKeySizeInBytes)));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, input.AsSpan(0, algorithm.SecretKeySizeInBytes)));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSubjectPublicKeyInfo(ReadOnlySpan<byte>.Empty));
        }

        [ConditionalTheory(nameof(SupportedOnPlatform))]
        [MemberData(nameof(AlgorithmsData))]
        public static void AlgorithmMatches(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = SlhDsa.GenerateKey(algorithm);
            Assert.Equal(algorithm, slhDsa.Algorithm);

            // TODO add remaining imports
        }

        [ConditionalTheory(nameof(SupportedOnPlatform))]
        [MemberData(nameof(AlgorithmsData))]
        public static void SlhDsaIsOnlyPublicAncestor(SlhDsaAlgorithm algorithm)
        {
            AssertSlhDsaIsOnlyPublicAncestor(() => SlhDsa.GenerateKey(algorithm));

            // TODO add remaining imports

            void AssertSlhDsaIsOnlyPublicAncestor(Func<SlhDsa> createKey)
            {
                using SlhDsa key = createKey();
                Type keyType = key.GetType();
                while (keyType != null && keyType != typeof(SlhDsa))
                {
                    Assert.False(keyType.IsPublic);
                    keyType = keyType.BaseType;
                }

                Assert.Equal(typeof(SlhDsa), keyType);
            }
        }
    }
}
