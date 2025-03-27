
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
                MLKem.ImportSubjectPublicKeyInfo(Array.Empty<byte>()));

            Assert.Throws<PlatformNotSupportedException>(() =>
                MLKem.ImportSubjectPublicKeyInfo(ReadOnlySpan<byte>.Empty));
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
    }
}
