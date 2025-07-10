// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class CompositeMLDsaFactoryTests
    {
        // TODO test doesn't belong here, move to different class
        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportExportVerify(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            using (CompositeMLDsa key = CompositeMLDsa.ImportCompositeMLDsaPrivateKey(vector.Algorithm, vector.SecretKey))
            {
                byte[] exportedSecretKey = key.ExportCompositeMLDsaPrivateKey();
                // TODO 'D' values differ, so we cannot compare keys directly
                //Assert.Equal(vector.SecretKey, exportedSecretKey);

                byte[] exportedPublicKey = key.ExportCompositeMLDsaPublicKey();
                Assert.Equal(vector.PublicKey, exportedPublicKey);

                AssertExtensions.TrueExpression(key.VerifyData(vector.Message, vector.Signature));
            }

            using (CompositeMLDsa key = CompositeMLDsa.ImportCompositeMLDsaPublicKey(vector.Algorithm, vector.PublicKey))
            {
                Assert.Throws<CryptographicException>(key.ExportCompositeMLDsaPrivateKey);

                byte[] exportedPublicKey = key.ExportCompositeMLDsaPublicKey();
                Assert.Equal(vector.PublicKey, exportedPublicKey);

                AssertExtensions.TrueExpression(key.VerifyData(vector.Message, vector.Signature));
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportSignVerify(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            byte[] signature;

            using (CompositeMLDsa key = CompositeMLDsa.ImportCompositeMLDsaPrivateKey(vector.Algorithm, vector.SecretKey))
            {
                signature = key.SignData(vector.Message, null);

                Assert.Equal(vector.Signature.Length, signature.Length);

                AssertExtensions.TrueExpression(key.VerifyData(vector.Message, signature));
            }

            using (CompositeMLDsa key = CompositeMLDsa.ImportCompositeMLDsaPublicKey(vector.Algorithm, vector.PublicKey))
            {
                AssertExtensions.TrueExpression(key.VerifyData(vector.Message, signature));
            }
        }
    }
}
