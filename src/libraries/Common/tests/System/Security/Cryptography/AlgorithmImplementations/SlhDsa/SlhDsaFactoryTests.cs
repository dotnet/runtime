// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public static class SlhDsaFactoryTests
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
        public static void ArgumentValidation_WrongKeySizeForAlgorithm(SlhDsaAlgorithm algorithm)
        {
            int publicKeySize = algorithm.PublicKeySizeInBytes;
            int secretKeySize = algorithm.SecretKeySizeInBytes;

            // SLH-DSA key size is wrong when importing algorithm key. Throw an argument exception.
            Action<Func<SlhDsa>> assertDirectImport = import => AssertExtensions.Throws<ArgumentException>("source", import);

            // SLH-DSA key size is wrong when importing SPKI/PKCS8/PEM. Throw a cryptographic exception unless platform is not supported.
            // Note: this is the algorithm key size, not the PKCS#8 key size.
            Action<Func<SlhDsa>> assertEmbeddedImport = import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import()));

            AssertImportPublicKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[publicKeySize + 1]);
            AssertImportPublicKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[publicKeySize - 1]);
            AssertImportPublicKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[0]);

            AssertImportSecretKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[secretKeySize + 1]);
            AssertImportSecretKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[secretKeySize - 1]);
            AssertImportSecretKey(assertDirectImport, assertEmbeddedImport, algorithm, new byte[0]);
        }

        [ConditionalTheory(typeof(SlhDsaTestHelpers), nameof(SlhDsaTestHelpers.IsNotSupported))]
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

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void AlgorithmMatches_GenerateKey(SlhDsaAlgorithm algorithm)
        {
            AssertThrowIfNotSupported(() =>
            {
                using SlhDsa slhDsa = SlhDsa.GenerateKey(algorithm);
                Assert.Equal(algorithm, slhDsa.Algorithm);
            });

            AssertImportPublicKey(import =>
                AssertThrowIfNotSupported(() =>
                    Assert.Equal(algorithm, import().Algorithm)), algorithm, new byte[algorithm.PublicKeySizeInBytes]);

            AssertImportSecretKey(import =>
                AssertThrowIfNotSupported(() =>
                    Assert.Equal(algorithm, import().Algorithm)), algorithm, new byte[algorithm.SecretKeySizeInBytes]);
        }

        private static void AssertImportPublicKey(Action<Func<SlhDsa>> test, SlhDsaAlgorithm algorithm, ReadOnlyMemory<byte> publicKey) =>
            AssertImportPublicKey(test, test, algorithm, publicKey);

        private static void AssertImportPublicKey(Action<Func<SlhDsa>> testDirectCall, Action<Func<SlhDsa>> testEmbeddedCall, SlhDsaAlgorithm algorithm, ReadOnlyMemory<byte> publicKey)
        {
            if (publicKey.Length == 0)
            {
                testDirectCall(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, Array.Empty<byte>()));
                testDirectCall(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, ReadOnlySpan<byte>.Empty));
            }
            else
            {
                testDirectCall(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, publicKey.Span));
            }
        }

        private static void AssertImportSecretKey(Action<Func<SlhDsa>> test, SlhDsaAlgorithm algorithm, ReadOnlyMemory<byte> secretKey) =>
            AssertImportSecretKey(test, test, algorithm, secretKey);

        private static void AssertImportSecretKey(Action<Func<SlhDsa>> testDirectCall, Action<Func<SlhDsa>> testEmbeddedCall, SlhDsaAlgorithm algorithm, ReadOnlyMemory<byte> secretKey)
        {
            if (secretKey.Length == 0)
            {
                testDirectCall(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, Array.Empty<byte>()));
                testDirectCall(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, ReadOnlySpan<byte>.Empty));
            }
            else
            {
                testDirectCall(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, secretKey.Span));
            }
        }

        /// <summary>
        /// Asserts that on platforms that do not support SLH-DSA, the input test throws PlatformNotSupportedException.
        /// If the test does pass, it implies that the test is validating code after the platform check.
        /// </summary>
        /// <param name="test">The test to run.</param>
        private static void AssertThrowIfNotSupported(Action test)
        {
            if (SlhDsa.IsSupported)
            {
                test();
            }
            else
            {
                try
                {
                    test();
                }
                catch (PlatformNotSupportedException pnse)
                {
                    Assert.Contains("SlhDsa", pnse.Message);
                }
                catch (ThrowsException te) when (te.InnerException is PlatformNotSupportedException pnse)
                {
                    Assert.Contains("SlhDsa", pnse.Message);
                }
            }
        }
    }
}
