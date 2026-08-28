// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    public sealed class CompositeMLKemImplementationTests : CompositeMLKemTestsBase
    {
        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void GenerateKey_TypeIsInternal(CompositeMLKemAlgorithm algorithm)
        {
            AssertCompositeMLKemIsOnlyPublicAncestor(() => CompositeMLKem.GenerateKey(algorithm));
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ImportEncapsulationKey_TypeIsInternal(CompositeMLKemTestVector vector)
        {
            AssertCompositeMLKemIsOnlyPublicAncestor(
                () => CompositeMLKem.ImportEncapsulationKey(vector.Algorithm, vector.EncapsulationKey));
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ImportDecapsulationKey_TypeIsInternal(CompositeMLKemTestVector vector)
        {
            AssertCompositeMLKemIsOnlyPublicAncestor(
                () => CompositeMLKem.ImportDecapsulationKey(vector.Algorithm, vector.DecapsulationKey));
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void Import_EncapsulationKey_Ietf(CompositeMLKemTestVector vector)
        {
            CompositeMLKemTestHelpers.AssertImportEncapsulationKey(
                import =>
                {
                    using (CompositeMLKem encapsKey = import())
                    {
                        Assert.Equal(vector.Algorithm, encapsKey.Algorithm);

                        CompositeMLKemTestHelpers.AssertExportEncapsulationKey(
                            export => AssertExtensions.SequenceEqual(vector.EncapsulationKey, export(encapsKey)));

                        CompositeMLKemTestHelpers.AssertExportDecapsulationKey(
                            export => Assert.Throws<CryptographicException>(() => export(encapsKey)));
                    }
                },
                vector.Algorithm,
                vector.EncapsulationKey.ToArray());
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void Import_SubjectPublicKeyInfo_Ietf(CompositeMLKemTestVector vector)
        {
            CompositeMLKemTestHelpers.AssertImportSubjectPublicKeyInfo(
                import =>
                {
                    using (CompositeMLKem encapsKey = import(vector.Spki.ToArray()))
                    {
                        Assert.Equal(vector.Algorithm, encapsKey.Algorithm);

                        CompositeMLKemTestHelpers.AssertExportEncapsulationKey(
                            export => AssertExtensions.SequenceEqual(vector.EncapsulationKey, export(encapsKey)));

                        CompositeMLKemTestHelpers.AssertExportDecapsulationKey(
                            export => Assert.Throws<CryptographicException>(() => export(encapsKey)));
                    }
                });
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void Import_DecapsulationKey_Ietf(CompositeMLKemTestVector vector)
        {
            CompositeMLKemTestHelpers.AssertImportDecapsulationKey(
                import =>
                {
                    using (CompositeMLKem decapsKey = import())
                    {
                        Assert.Equal(vector.Algorithm, decapsKey.Algorithm);

                        CompositeMLKemTestHelpers.AssertExportEncapsulationKey(
                            export => AssertExtensions.SequenceEqual(vector.EncapsulationKey, export(decapsKey)));

                        CompositeMLKemTestHelpers.AssertExportDecapsulationKey(
                            export => AssertExtensions.SequenceEqual(vector.DecapsulationKey, export(decapsKey)));
                    }
                },
                vector.Algorithm,
                vector.DecapsulationKey.ToArray());
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public void Import_Pkcs8_Ietf(CompositeMLKemTestVector vector)
        {
            CompositeMLKemTestHelpers.AssertImportPkcs8PrivateKey(
                import =>
                {
                    using (CompositeMLKem decapsKey = import(vector.Pkcs8.ToArray()))
                    {
                        Assert.Equal(vector.Algorithm, decapsKey.Algorithm);

                        CompositeMLKemTestHelpers.AssertExportEncapsulationKey(
                            export => AssertExtensions.SequenceEqual(vector.EncapsulationKey, export(decapsKey)));

                        CompositeMLKemTestHelpers.AssertExportDecapsulationKey(
                            export => AssertExtensions.SequenceEqual(vector.DecapsulationKey, export(decapsKey)));
                    }
                });
        }

        private static void AssertCompositeMLKemIsOnlyPublicAncestor(Func<CompositeMLKem> createKey)
        {
            Type? keyType;

            using (CompositeMLKem kem = createKey())
            {
                keyType = kem.GetType();
            }

            while (keyType is not null && keyType != typeof(CompositeMLKem))
            {
                AssertExtensions.FalseExpression(keyType.IsPublic);
                keyType = keyType.BaseType;
            }

            Assert.Equal(typeof(CompositeMLKem), keyType);
        }

        protected override CompositeMLKem GenerateKey(CompositeMLKemAlgorithm algorithm) =>
            CompositeMLKem.GenerateKey(algorithm);

        protected override CompositeMLKem ImportDecapsulationKey(CompositeMLKemAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            CompositeMLKem.ImportDecapsulationKey(algorithm, source);

        protected override CompositeMLKem ImportEncapsulationKey(CompositeMLKemAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            CompositeMLKem.ImportEncapsulationKey(algorithm, source);
    }
}
