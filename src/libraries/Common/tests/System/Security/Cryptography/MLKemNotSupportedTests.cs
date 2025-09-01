
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLKemNotSupportedTests), nameof(MLKemNotSupportedTests.IsNotSupported))]
    public static class MLKemNotSupportedTests
    {
        public static bool IsNotSupported => !MLKem.IsSupported;

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Generate_NotSupported(MLKemAlgorithm algorithm)
        {
            Assert.Throws<PlatformNotSupportedException>(() => MLKem.GenerateKey(algorithm));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportPrivateSeed_NotSupported(MLKemAlgorithm algorithm)
        {
            Assert.Throws<PlatformNotSupportedException>(() =>
                MLKem.ImportPrivateSeed(algorithm, new byte[algorithm.PrivateSeedSizeInBytes]));

            Assert.Throws<PlatformNotSupportedException>(() =>
                MLKem.ImportPrivateSeed(algorithm, new ReadOnlySpan<byte>(new byte[algorithm.PrivateSeedSizeInBytes])));
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() =>
                MLKem.ImportSubjectPublicKeyInfo(MLKemTestData.IetfMlKem512Spki));

            Assert.Throws<PlatformNotSupportedException>(() =>
                MLKem.ImportSubjectPublicKeyInfo(new ReadOnlySpan<byte>(MLKemTestData.IetfMlKem512Spki)));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportEncapsulationKey_NotSupported(MLKemAlgorithm algorithm)
        {
            Assert.Throws<PlatformNotSupportedException>(() => MLKem.ImportEncapsulationKey(
                algorithm,
                new byte[algorithm.EncapsulationKeySizeInBytes]));

            Assert.Throws<PlatformNotSupportedException>(() => MLKem.ImportEncapsulationKey(
                algorithm,
                new Span<byte>(new byte[algorithm.EncapsulationKeySizeInBytes])));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportDecapsulationKey_NotSupported(MLKemAlgorithm algorithm)
        {
            Assert.Throws<PlatformNotSupportedException>(() => MLKem.ImportDecapsulationKey(
                algorithm,
                new byte[algorithm.DecapsulationKeySizeInBytes]));

            Assert.Throws<PlatformNotSupportedException>(() => MLKem.ImportDecapsulationKey(
                algorithm,
                new Span<byte>(new byte[algorithm.DecapsulationKeySizeInBytes])));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() => MLKem.ImportPkcs8PrivateKey(
                MLKemTestData.IetfMlKem512PrivateKeySeed));

            Assert.Throws<PlatformNotSupportedException>(() => MLKem.ImportPkcs8PrivateKey(
                new ReadOnlySpan<byte>(MLKemTestData.IetfMlKem512PrivateKeySeed)));
        }

        [Fact]
        public static void ImportEncryptedPkcs8PrivateKey_NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() => MLKem.ImportEncryptedPkcs8PrivateKey(
                MLKemTestData.EncryptedPrivateKeyPassword, MLKemTestData.IetfMlKem512EncryptedPrivateKeySeed));

            Assert.Throws<PlatformNotSupportedException>(() => MLKem.ImportEncryptedPkcs8PrivateKey(
                MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(), MLKemTestData.IetfMlKem512EncryptedPrivateKeySeed));

            Assert.Throws<PlatformNotSupportedException>(() => MLKem.ImportEncryptedPkcs8PrivateKey(
                MLKemTestData.EncryptedPrivateKeyPasswordBytes, MLKemTestData.IetfMlKem512EncryptedPrivateKeySeed));
        }

        [Fact]
        public static void ImportFromPem_NotSupported()
        {
            string pem = """
            -----BEGIN THING-----
            Should throw before even attempting to read the PEM
            -----END THING-----
            """;
            Assert.Throws<PlatformNotSupportedException>(() => MLKem.ImportFromPem(pem));
            Assert.Throws<PlatformNotSupportedException>(() => MLKem.ImportFromPem(pem.AsSpan()));
        }
    }
}
