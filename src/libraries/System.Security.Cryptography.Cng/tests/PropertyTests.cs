// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.EcDsa.Tests;
using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public static class PropertyTests
    {
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
                Assert.Equal(null, p2.GetValue());
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
    }
}
