// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.EcDsa.Tests;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public static class PropertyTests
    {
        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.PlatformCryptoProviderFunctionalP256))]
        [InlineData("ECDH_P256")]
        [InlineData("ECDSA_P256")]
        [OuterLoop("Hardware backed key generation takes several seconds.")]
        public static void CreatePersisted_PlatformEccKeyHasKeySize_P256(string algorithm)
        {
            CngAlgorithm cngAlgorithm = new CngAlgorithm(algorithm);

            using (CngPlatformProviderKey platformKey = new CngPlatformProviderKey(cngAlgorithm))
            {
                Assert.Equal(256, platformKey.Key.KeySize);
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.PlatformCryptoProviderFunctionalP384))]
        [InlineData("ECDH_P384")]
        [InlineData("ECDSA_P384")]
        [OuterLoop("Hardware backed key generation takes several seconds.")]
        public static void CreatePersisted_PlatformEccKeyHasKeySize_P384(string algorithm)
        {
            CngAlgorithm cngAlgorithm = new CngAlgorithm(algorithm);

            using (CngPlatformProviderKey platformKey = new CngPlatformProviderKey(cngAlgorithm))
            {
                Assert.Equal(384, platformKey.Key.KeySize);
            }
        }

        [ConditionalTheory(typeof(PlatformSupport), nameof(PlatformSupport.PlatformCryptoProviderFunctionalRsa))]
        [InlineData(1024)]
        [InlineData(2048)]
        [OuterLoop("Hardware backed key generation takes several seconds.")]
        public static void CreatePersisted_PlatformRsaKeyHasKeySize(int keySize)
        {
            CngProperty keyLengthProperty = new CngProperty("Length", BitConverter.GetBytes(keySize), CngPropertyOptions.None);
            CngPlatformProviderKey platformKey = new CngPlatformProviderKey(
                CngAlgorithm.Rsa,
                keySuffix: keySize.ToString(),
                additionalParameters: keyLengthProperty);

            using (platformKey)
            {
                Assert.Equal(keySize, platformKey.Key.KeySize);
            }
        }

        [Fact]
        public static void GetProperty_NoSuchProperty()
        {
            using (CngKey key = CngKey.Import(TestData.Key_ECDiffieHellmanP256, CngKeyBlobFormat.GenericPublicBlob))
            {
                Assert.ThrowsAny<CryptographicException>(() => key.GetProperty("DOES NOT EXIST", CngPropertyOptions.CustomProperty));
            }
        }

        [Fact]
        public static void SetPropertyZeroLengthCornerCase()
        {
            using (CngKey key = CngKey.Import(TestData.Key_ECDiffieHellmanP256, CngKeyBlobFormat.GenericPublicBlob))
            {
                const string propertyName = "CustomZeroLengthProperty";
                CngProperty p = new CngProperty(propertyName, new byte[0], CngPropertyOptions.CustomProperty);
                key.SetProperty(p);

                CngProperty p2 = key.GetProperty(propertyName, CngPropertyOptions.CustomProperty);
                Assert.Equal(propertyName, p2.Name);
                Assert.Equal(CngPropertyOptions.CustomProperty, p2.Options);

                // This one is odd. CNG keys can have properties with zero length but CngKey.GetProperty() transforms this into null.
                Assert.Null(p2.GetValue());
            }
        }

        [Fact]
        public static void SetPropertyNullCornerCase()
        {
            using (CngKey key = CngKey.Import(TestData.Key_ECDiffieHellmanP256, CngKeyBlobFormat.GenericPublicBlob))
            {
                const string propertyName = "CustomNullProperty";
                CngProperty p = new CngProperty(propertyName, null, CngPropertyOptions.CustomProperty);
                Assert.ThrowsAny<CryptographicException>(() => key.SetProperty(p));
            }
        }

        [Fact]
        public static void HasProperty()
        {
            using (CngKey key = CngKey.Import(TestData.Key_ECDiffieHellmanP256, CngKeyBlobFormat.GenericPublicBlob))
            {
                const string propertyName = "CustomProperty";
                bool hasProperty;

                hasProperty = key.HasProperty(propertyName, CngPropertyOptions.CustomProperty);
                Assert.False(hasProperty);

                key.SetProperty(new CngProperty(propertyName, new byte[0], CngPropertyOptions.CustomProperty));
                hasProperty = key.HasProperty(propertyName, CngPropertyOptions.CustomProperty);
                Assert.True(hasProperty);
            }
        }

        [Fact]
        public static void GetAndSetProperties()
        {
            using (CngKey key = CngKey.Import(TestData.Key_ECDiffieHellmanP256, CngKeyBlobFormat.GenericPublicBlob))
            {
                string propertyName = "Are you there";
                bool hasProperty = key.HasProperty(propertyName, CngPropertyOptions.CustomProperty);
                Assert.False(hasProperty);

                byte[] propertyValue = { 1, 2, 3 };
                CngProperty property = new CngProperty(propertyName, propertyValue, CngPropertyOptions.CustomProperty);
                key.SetProperty(property);

                byte[] actualValue = key.GetProperty(propertyName, CngPropertyOptions.CustomProperty).GetValue();
                Assert.Equal<byte>(propertyValue, actualValue);
            }
        }

        [Fact]
        public static void OverwriteProperties()
        {
            using (CngKey key = CngKey.Import(TestData.Key_ECDiffieHellmanP256, CngKeyBlobFormat.GenericPublicBlob))
            {
                string propertyName = "Are you there";
                bool hasProperty = key.HasProperty(propertyName, CngPropertyOptions.CustomProperty);
                Assert.False(hasProperty);

                // Set it once.
                byte[] propertyValue = { 1, 2, 3 };
                CngProperty property = new CngProperty(propertyName, propertyValue, CngPropertyOptions.CustomProperty);
                key.SetProperty(property);

                // Set it again.
                propertyValue = new byte[] { 5, 6, 7 };
                property = new CngProperty(propertyName, propertyValue, CngPropertyOptions.CustomProperty);
                key.SetProperty(property);

                CngProperty retrievedProperty = key.GetProperty(propertyName, CngPropertyOptions.CustomProperty);
                Assert.Equal(propertyName, retrievedProperty.Name);
                Assert.Equal<byte>(propertyValue, retrievedProperty.GetValue());
                Assert.Equal(CngPropertyOptions.CustomProperty, retrievedProperty.Options);
            }
        }

        [Fact]
        public static void NullValueRoundtrip()
        {
            CngProperty property = new CngProperty("banana", null, CngPropertyOptions.CustomProperty);
            Assert.Null(property.GetValue());
        }
    }
}
