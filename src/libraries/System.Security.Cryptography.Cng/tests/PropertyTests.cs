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

            using (CngKeyWrapper platformKey = CngKeyWrapper.CreateMicrosoftPlatformCryptoProvider(cngAlgorithm))
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

            using (CngKeyWrapper platformKey = CngKeyWrapper.CreateMicrosoftPlatformCryptoProvider(cngAlgorithm))
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
            CngKeyWrapper platformKey = CngKeyWrapper.CreateMicrosoftPlatformCryptoProvider(
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

        [Fact]
        public static void NullArrayAndNullSpanDiffer()
        {
            CngProperty fromNullArray = new CngProperty("test", (byte[]?)null, CngPropertyOptions.CustomProperty);
            CngProperty fromNullSpan = new CngProperty("test", (ReadOnlySpan<byte>)(byte[]?)null, CngPropertyOptions.CustomProperty);

            Assert.Null(fromNullArray.GetValue());
            Assert.Empty(fromNullSpan.GetValue());
            Assert.NotEqual(fromNullArray, fromNullSpan);
        }

        [Fact]
        public static void SpanCtorDefaultAndEmptyAreEquivalent()
        {
            CngProperty fromDefault = new CngProperty("test", default(ReadOnlySpan<byte>), CngPropertyOptions.CustomProperty);
            CngProperty fromEmpty = new CngProperty("test", ReadOnlySpan<byte>.Empty, CngPropertyOptions.CustomProperty);
            CngProperty fromArrayEmpty = new CngProperty("test", Array.Empty<byte>(), CngPropertyOptions.CustomProperty);
            CngProperty fromArrayAsSpan = new CngProperty("test", Array.Empty<byte>().AsSpan(), CngPropertyOptions.CustomProperty);

            Assert.Equal(fromDefault, fromEmpty);
            Assert.Equal(fromDefault, fromArrayEmpty);
            Assert.Equal(fromDefault, fromArrayAsSpan);
            Assert.Empty(fromDefault.GetValue());
            Assert.Empty(fromEmpty.GetValue());
        }

        [Fact]
        public static void SpanCtorValueEquivalence()
        {
            byte[] value = [1, 2, 3];
            CngProperty fromArray = new CngProperty("test", value, CngPropertyOptions.CustomProperty);
            CngProperty fromSpan = new CngProperty("test", value.AsSpan(), CngPropertyOptions.CustomProperty);

            Assert.Equal(fromArray, fromSpan);
            Assert.Equal<byte>(value, fromArray.GetValue());
            Assert.Equal<byte>(value, fromSpan.GetValue());
        }

        [Fact]
        public static void SpanCtorEmptyHasAndGetPropertyEquivalence()
        {
            using (CngKey key = CngKey.Import(TestData.Key_ECDiffieHellmanP256, CngKeyBlobFormat.GenericPublicBlob))
            {
                // HasProperty gives the same answer when the key doesn't already have the property.
                AssertExtensions.FalseExpression(key.HasProperty("DefaultSpanProp", CngPropertyOptions.CustomProperty));
                AssertExtensions.FalseExpression(key.HasProperty("EmptyArrayProp", CngPropertyOptions.CustomProperty));

                // default(ReadOnlySpan<byte>) and Array.Empty<byte>() both succeed on SetProperty.
                key.SetProperty(new CngProperty("DefaultSpanProp", default(ReadOnlySpan<byte>), CngPropertyOptions.CustomProperty));
                key.SetProperty(new CngProperty("EmptyArrayProp", Array.Empty<byte>(), CngPropertyOptions.CustomProperty));

                // HasProperty gives the same answer after setting.
                AssertExtensions.TrueExpression(key.HasProperty("DefaultSpanProp", CngPropertyOptions.CustomProperty));
                AssertExtensions.TrueExpression(key.HasProperty("EmptyArrayProp", CngPropertyOptions.CustomProperty));

                // GetProperty gives identical answers.
                // CNG transforms zero-length values to null on retrieval.
                Assert.Null(key.GetProperty("DefaultSpanProp", CngPropertyOptions.CustomProperty).GetValue());
                Assert.Null(key.GetProperty("EmptyArrayProp", CngPropertyOptions.CustomProperty).GetValue());
            }
        }

        [Fact]
        public static void SpanCtorValueHasAndGetPropertyEquivalence()
        {
            using (CngKey key = CngKey.Import(TestData.Key_ECDiffieHellmanP256, CngKeyBlobFormat.GenericPublicBlob))
            {
                byte[] value = [1, 2, 3];

                // HasProperty gives the same answer when the key doesn't already have the property.
                AssertExtensions.FalseExpression(key.HasProperty("SpanProp", CngPropertyOptions.CustomProperty));
                AssertExtensions.FalseExpression(key.HasProperty("ArrayProp", CngPropertyOptions.CustomProperty));

                key.SetProperty(new CngProperty("SpanProp", value.AsSpan(), CngPropertyOptions.CustomProperty));
                key.SetProperty(new CngProperty("ArrayProp", value, CngPropertyOptions.CustomProperty));

                // HasProperty gives the same answer for the same value in both ctors.
                AssertExtensions.TrueExpression(key.HasProperty("SpanProp", CngPropertyOptions.CustomProperty));
                AssertExtensions.TrueExpression(key.HasProperty("ArrayProp", CngPropertyOptions.CustomProperty));

                // GetProperty gives identical answers.
                Assert.Equal<byte>(value, key.GetProperty("SpanProp", CngPropertyOptions.CustomProperty).GetValue());
                Assert.Equal<byte>(value, key.GetProperty("ArrayProp", CngPropertyOptions.CustomProperty).GetValue());

                // Overwrite existing non-empty value.
                byte[] value2 = [5, 6, 7];
                key.SetProperty(new CngProperty("SpanProp", value2.AsSpan(), CngPropertyOptions.CustomProperty));
                key.SetProperty(new CngProperty("ArrayProp", value2, CngPropertyOptions.CustomProperty));

                // HasProperty gives the same answer when overwriting an existing non-empty value.
                AssertExtensions.TrueExpression(key.HasProperty("SpanProp", CngPropertyOptions.CustomProperty));
                AssertExtensions.TrueExpression(key.HasProperty("ArrayProp", CngPropertyOptions.CustomProperty));

                // GetProperty gives identical answers after overwrite.
                Assert.Equal<byte>(value2, key.GetProperty("SpanProp", CngPropertyOptions.CustomProperty).GetValue());
                Assert.Equal<byte>(value2, key.GetProperty("ArrayProp", CngPropertyOptions.CustomProperty).GetValue());
            }
        }
    }
}
