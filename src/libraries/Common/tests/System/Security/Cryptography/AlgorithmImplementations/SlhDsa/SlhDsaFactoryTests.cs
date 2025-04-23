// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Xunit;
using Xunit.Sdk;

using static System.Security.Cryptography.SLHDsa.Tests.SlhDsaTestHelpers;

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
                testDirectCall(() => SlhDsa.ImportSlhDsaPublicKey(algorithm, []));
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
                testDirectCall(() => SlhDsa.ImportSlhDsaSecretKey(algorithm, []));
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
                catch (PlatformNotSupportedException)
                {
                    // Expected exception
                }
                catch (ThrowsException te) when (te.InnerException is PlatformNotSupportedException)
                {
                    // Expected exception
                }
            }
        }
    }
}
