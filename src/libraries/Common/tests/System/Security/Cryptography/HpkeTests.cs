// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(PlatformDetection),
        nameof(PlatformDetection.IsNotBrowser),
        nameof(PlatformDetection.IsNotWasi),
        nameof(PlatformDetection.IsNotNetFramework))]
    public static class HpkeTests
    {
        private static readonly HpkeSuite s_suite = new(HpkeKem.MLKEM_768, HpkeKdf.SHAKE256, HpkeAead.AES_128_GCM);

        [Fact]
        public static void IsSupported_NullSuite()
        {
            AssertExtensions.Throws<ArgumentNullException>("suite", () => Hpke.IsSupported(null));
        }

        [Fact]
        public static void GenerateKey_NullSuite()
        {
            AssertExtensions.Throws<ArgumentNullException>("suite", () => Hpke.GenerateKey(null));
        }

        [Fact]
        public static void DeriveKey_NullSuite()
        {
            AssertExtensions.Throws<ArgumentNullException>("suite", () => Hpke.DeriveKey(null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("suite",
                () => Hpke.DeriveKey(null, ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void DeriveKey_NullIkm()
        {
            AssertExtensions.Throws<ArgumentNullException>("ikm", () => Hpke.DeriveKey(s_suite, (byte[])null));
        }

        [Fact]
        public static void ImportDecapsulationKey_NullSuite()
        {
            AssertExtensions.Throws<ArgumentNullException>("suite",
                () => Hpke.ImportDecapsulationKey(null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("suite",
                () => Hpke.ImportDecapsulationKey(null, ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void ImportDecapsulationKey_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source",
                () => Hpke.ImportDecapsulationKey(s_suite, (byte[])null));
        }

        [Fact]
        public static void ImportEncapsulationKey_NullSuite()
        {
            AssertExtensions.Throws<ArgumentNullException>("suite",
                () => Hpke.ImportEncapsulationKey(null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>("suite",
                () => Hpke.ImportEncapsulationKey(null, ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void ImportEncapsulationKey_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source",
                () => Hpke.ImportEncapsulationKey(s_suite, (byte[])null));
        }

        [Theory]
        [MemberData(nameof(HpkeTestData.KemAlgorithms), MemberType = typeof(HpkeTestData))]
        public static void ImportDecapsulationKey_InvalidSize(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.SHAKE256, HpkeAead.AES_128_GCM);

            byte[] shortPrivateKey = new byte[suite.DecapsulationKeySizeInBytes - 1];
            byte[] longPrivateKey = new byte[suite.DecapsulationKeySizeInBytes + 1];
            AssertExtensions.Throws<ArgumentException>("source",
                () => Hpke.ImportDecapsulationKey(suite, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentException>("source",
                () => Hpke.ImportDecapsulationKey(suite, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentException>("source",
                () => Hpke.ImportDecapsulationKey(suite, shortPrivateKey));
            AssertExtensions.Throws<ArgumentException>("source",
                () => Hpke.ImportDecapsulationKey(suite, shortPrivateKey.AsSpan()));
            AssertExtensions.Throws<ArgumentException>("source",
                () => Hpke.ImportDecapsulationKey(suite, longPrivateKey));
            AssertExtensions.Throws<ArgumentException>("source",
                () => Hpke.ImportDecapsulationKey(suite, longPrivateKey.AsSpan()));
        }

        [Theory]
        [MemberData(nameof(HpkeTestData.KemAlgorithms), MemberType = typeof(HpkeTestData))]
        public static void ImportEncapsulationKey_InvalidSize(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.SHAKE256, HpkeAead.AES_128_GCM);

            byte[] shortPublicKey = new byte[suite.EncapsulationKeySizeInBytes - 1];
            byte[] longPublicKey = new byte[suite.EncapsulationKeySizeInBytes + 1];
            AssertExtensions.Throws<ArgumentException>("source",
                () => Hpke.ImportEncapsulationKey(suite, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentException>("source",
                () => Hpke.ImportEncapsulationKey(suite, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentException>("source",
                () => Hpke.ImportEncapsulationKey(suite, shortPublicKey));
            AssertExtensions.Throws<ArgumentException>("source",
                () => Hpke.ImportEncapsulationKey(suite, shortPublicKey.AsSpan()));
            AssertExtensions.Throws<ArgumentException>("source",
                () => Hpke.ImportEncapsulationKey(suite, longPublicKey));
            AssertExtensions.Throws<ArgumentException>("source",
                () => Hpke.ImportEncapsulationKey(suite, longPublicKey.AsSpan()));
        }
    }
}
