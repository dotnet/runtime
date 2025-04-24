// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    [ConditionalClass(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
    public sealed class SlhDsaImplementationTests : SlhDsaTests
    {
        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void SlhDsaIsOnlyPublicAncestor_GenerateKey(SlhDsaAlgorithm algorithm)
        {
            AssertSlhDsaIsOnlyPublicAncestor(() => SlhDsa.GenerateKey(algorithm));
        }

        [Theory]
        [MemberData(nameof(NistKeyGenTestVectorsData))]
        public static void SlhDsaIsOnlyPublicAncestor_Import(SlhDsaTestData.SlhDsaKeyGenTestVector vector)
        {
            AssertSlhDsaIsOnlyPublicAncestor(() => SlhDsa.ImportSlhDsaSecretKey(vector.Algorithm, vector.SecretKey));
            AssertSlhDsaIsOnlyPublicAncestor(() => SlhDsa.ImportSlhDsaPublicKey(vector.Algorithm, vector.PublicKey));
        }

        private static void AssertSlhDsaIsOnlyPublicAncestor(Func<SlhDsa> createKey)
        {
            using SlhDsa key = createKey();
            Type keyType = key.GetType();
            while (keyType != null && keyType != typeof(SlhDsa))
            {
                AssertExtensions.FalseExpression(keyType.IsPublic);
                keyType = keyType.BaseType;
            }

            Assert.Equal(typeof(SlhDsa), keyType);
        }

        public static IEnumerable<object[]> NistKeyGenTestVectorsData =>
            from vector in SlhDsaTestData.NistKeyGenTestVectors
            select new object[] { vector };

        [ConditionalTheory(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        [MemberData(nameof(NistKeyGenTestVectorsData))]
        public void NistKeyGenerationTest(SlhDsaTestData.SlhDsaKeyGenTestVector vector)
        {
            byte[] skSeed = vector.SecretKeySeed;
            byte[] skPrf = vector.SecretKeyPrf;
            byte[] pkSeed = vector.PublicKeySeed;

            byte[] sk = vector.SecretKey;
            byte[] pk = vector.PublicKey;

            // Sanity test for input vectors: SLH-DSA keys are composed of skSeed, skPrf and pkSeed
            AssertExtensions.SequenceEqual(skSeed.AsSpan(), sk.AsSpan(0, skSeed.Length));
            AssertExtensions.SequenceEqual(skPrf.AsSpan(), sk.AsSpan(skSeed.Length, skPrf.Length));
            AssertExtensions.SequenceEqual(pkSeed.AsSpan(), sk.AsSpan(skSeed.Length + skPrf.Length, pkSeed.Length));
            AssertExtensions.SequenceEqual(pkSeed.AsSpan(), pk.AsSpan(0, pkSeed.Length));

            // Import secret key and verify exports
            using (SlhDsa secretSlhDsa = ImportSlhDsaSecretKey(vector.Algorithm, sk))
            {
                byte[] pubKey = new byte[vector.Algorithm.PublicKeySizeInBytes];
                Assert.Equal(pk.Length, secretSlhDsa.ExportSlhDsaPublicKey(pubKey));
                AssertExtensions.SequenceEqual(pk, pubKey);

                byte[] secretKey = new byte[vector.Algorithm.SecretKeySizeInBytes];
                Assert.Equal(sk.Length, secretSlhDsa.ExportSlhDsaSecretKey(secretKey));
                AssertExtensions.SequenceEqual(sk, secretKey);
            }

            // Import public key and verify exports
            using (SlhDsa publicSlhDsa = ImportSlhDsaPublicKey(vector.Algorithm, pk))
            {
                byte[] pubKey = new byte[vector.Algorithm.PublicKeySizeInBytes];
                Assert.Equal(pk.Length, publicSlhDsa.ExportSlhDsaPublicKey(pubKey));
                AssertExtensions.SequenceEqual(pk, pubKey);

                byte[] secretKey = new byte[vector.Algorithm.SecretKeySizeInBytes];
                Assert.Throws<CryptographicException>(() => publicSlhDsa.ExportSlhDsaSecretKey(secretKey));
            }
        }

        protected override SlhDsa GenerateKey(SlhDsaAlgorithm algorithm) => SlhDsa.GenerateKey(algorithm);
        protected override SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => SlhDsa.ImportSlhDsaPublicKey(algorithm, source);
        protected override SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) => SlhDsa.ImportSlhDsaSecretKey(algorithm, source);
    }
}
