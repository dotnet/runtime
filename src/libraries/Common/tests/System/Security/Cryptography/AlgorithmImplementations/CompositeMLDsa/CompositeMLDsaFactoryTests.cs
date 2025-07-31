// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    public static class CompositeMLDsaFactoryTests
    {
        [Fact]
        public static void NullArgumentValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLDsa.GenerateKey(null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLDsa.IsAlgorithmSupported(null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(null, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLDsa.ImportCompositeMLDsaPublicKey(null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLDsa.ImportCompositeMLDsaPublicKey(null, ReadOnlySpan<byte>.Empty));

            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256, null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLDsa.ImportCompositeMLDsaPublicKey(CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256, null));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPrivateKey_Empty(CompositeMLDsaAlgorithm algorithm)
        {
            AssertImportBadPrivateKey(algorithm, Array.Empty<byte>());
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPrivateKey_ShortMLDsaSeed(CompositeMLDsaAlgorithm algorithm)
        {
            MLDsaKeyInfo mldsaVector = CompositeMLDsaTestData.GetMLDsaIetfTestVector(algorithm);
            AssertImportBadPrivateKey(algorithm, new byte[mldsaVector.PrivateSeed.Length - 1]);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPrivateKey_OnlyMLDsaSeed(CompositeMLDsaAlgorithm algorithm)
        {
            MLDsaKeyInfo mldsaVector = CompositeMLDsaTestData.GetMLDsaIetfTestVector(algorithm);
            AssertImportBadPrivateKey(algorithm, mldsaVector.PrivateSeed.ToArray());
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPrivateKey_ShortTradKey(CompositeMLDsaAlgorithm algorithm)
        {
            MLDsaKeyInfo mldsaVector = CompositeMLDsaTestData.GetMLDsaIetfTestVector(algorithm);
            byte[] shortTradKey = mldsaVector.PrivateSeed;
            Array.Resize(ref shortTradKey, shortTradKey.Length + 1);

            AssertImportBadPrivateKey(algorithm, shortTradKey);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportPrivateKey_LowerBound(CompositeMLDsaAlgorithm algorithm)
        {
            int bound = CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].PrivateSeedSizeInBytes +
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa => rsa.KeySizeInBits / 8,
                    ecdsa => 1 + ((ecdsa.KeySizeInBits + 7) / 8),
                    eddsa => eddsa.KeySizeInBits / 8);

            AssertImportBadPrivateKey(algorithm, new byte[bound - 1]);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportPrivateKey_UpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            int? bound = CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].PrivateSeedSizeInBytes +
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa => default(int?),
                    ecdsa => default(int?),
                    eddsa => eddsa.KeySizeInBits / 8);

            if (bound.HasValue)
                AssertImportBadPrivateKey(algorithm, new byte[bound.Value + 1]);
        }

        private static void AssertImportBadPrivateKey(CompositeMLDsaAlgorithm algorithm, byte[] key)
        {
            CompositeMLDsaTestHelpers.AssertImportPrivateKey(
                import => AssertThrowIfNotSupported(
                    () => AssertExtensions.Throws<CryptographicException>(() => import()),
                    algorithm),
                algorithm,
                key);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPublicKey_Empty(CompositeMLDsaAlgorithm algorithm)
        {
            AssertImportBadPublicKey(algorithm, Array.Empty<byte>());
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPublicKey_ShortMLDsaKey(CompositeMLDsaAlgorithm algorithm)
        {
            MLDsaKeyInfo mldsaVector = CompositeMLDsaTestData.GetMLDsaIetfTestVector(algorithm);
            AssertImportBadPublicKey(algorithm, new byte[mldsaVector.PublicKey.Length - 1]);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPublicKey_OnlyMLDsaKey(CompositeMLDsaAlgorithm algorithm)
        {
            MLDsaKeyInfo mldsaVector = CompositeMLDsaTestData.GetMLDsaIetfTestVector(algorithm);
            AssertImportBadPublicKey(algorithm, mldsaVector.PublicKey.ToArray());
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPublicKey_ShortTradKey(CompositeMLDsaAlgorithm algorithm)
        {
            MLDsaKeyInfo mldsaVector = CompositeMLDsaTestData.GetMLDsaIetfTestVector(algorithm);
            byte[] shortTradKey = mldsaVector.PublicKey;
            Array.Resize(ref shortTradKey, shortTradKey.Length + 1);

            AssertImportBadPublicKey(algorithm, shortTradKey);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportPublicKey_LowerBound(CompositeMLDsaAlgorithm algorithm)
        {
            int bound = CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].PublicKeySizeInBytes +
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa => rsa.KeySizeInBits / 8,
                    ecdsa => 1 + 2 * ((ecdsa.KeySizeInBits + 7) / 8),
                    eddsa => eddsa.KeySizeInBits / 8);

            AssertImportBadPublicKey(algorithm, new byte[bound - 1]);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportPublicKey_UpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            int? bound = CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].PublicKeySizeInBytes +
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa => default(int?),
                    ecdsa => 1 + 2 * ((ecdsa.KeySizeInBits + 7) / 8),
                    eddsa => eddsa.KeySizeInBits / 8);

            if (bound.HasValue)
                AssertImportBadPublicKey(algorithm, new byte[bound.Value + 1]);
        }

        private static void AssertImportBadPublicKey(CompositeMLDsaAlgorithm algorithm, byte[] key)
        {
            CompositeMLDsaTestHelpers.AssertImportPublicKey(
                import => AssertThrowIfNotSupported(
                    () => AssertExtensions.Throws<CryptographicException>(() => import()),
                    algorithm),
                algorithm,
                key);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void AlgorithmMatches_GenerateKey(CompositeMLDsaAlgorithm algorithm)
        {
            AssertThrowIfNotSupported(() =>
            {
                using CompositeMLDsa dsa = CompositeMLDsa.GenerateKey(algorithm);
                Assert.Equal(algorithm, dsa.Algorithm);
            });
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void AlgorithmMatches_Import(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            CompositeMLDsaTestHelpers.AssertImportPublicKey(import =>
                AssertThrowIfNotSupported(() =>
                    Assert.Equal(vector.Algorithm, import().Algorithm)), vector.Algorithm, vector.PublicKey);

            CompositeMLDsaTestHelpers.AssertImportPrivateKey(import =>
                AssertThrowIfNotSupported(() =>
                    Assert.Equal(vector.Algorithm, import().Algorithm)), vector.Algorithm, vector.SecretKey);
        }

        [Fact]
        public static void IsSupported_AgreesWithPlatform()
        {
            // Composites are supported everywhere MLDsa is supported
            Assert.Equal(MLDsa.IsSupported, CompositeMLDsa.IsSupported);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void IsAlgorithmSupported_AgreesWithPlatform(CompositeMLDsaAlgorithm algorithm)
        {
            bool supported = CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                algorithm,
                _ => MLDsa.IsSupported,
                _ => false,
                _ => false);

            Assert.Equal(
                supported,
                CompositeMLDsa.IsAlgorithmSupported(algorithm));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void IsSupported_InitializesCrypto()
        {
            string arg = CompositeMLDsa.IsSupported ? "1" : "0";

            // This ensures that Composite ML-DSA is the first cryptographic algorithm touched in the process, which kicks off
            // the initialization of the crypto layer on some platforms. Running in a remote executor ensures no other
            // test has pre-initialized anything.
            RemoteExecutor.Invoke(static (string isSupportedStr) =>
            {
                bool isSupported = isSupportedStr == "1";
                return CompositeMLDsa.IsSupported == isSupported ? RemoteExecutor.SuccessExitCode : 0;
            }, arg).Dispose();
        }

        /// <summary>
        /// Asserts that on platforms that do not support Composite ML-DSA, the input test throws PlatformNotSupportedException.
        /// If the test does pass, it implies that the test is validating code after the platform check.
        /// </summary>
        /// <param name="test">The test to run.</param>
        private static void AssertThrowIfNotSupported(Action test, CompositeMLDsaAlgorithm? algorithm = null)
        {
            if (algorithm == null ? CompositeMLDsa.IsSupported : CompositeMLDsa.IsAlgorithmSupported(algorithm))
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
                    Assert.Contains("CompositeMLDsa", pnse.Message);
                }
                catch (ThrowsException te) when (te.InnerException is PlatformNotSupportedException pnse)
                {
                    Assert.Contains("CompositeMLDsa", pnse.Message);
                }
            }
        }
    }
}
