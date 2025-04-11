// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public abstract class SlhDsaConstructionTestsBase : SlhDsaTestsBase
    {
        public static IEnumerable<object[]> NistKeyGenTestVectorsData =>
            from vector in SlhDsaTestData.NistKeyGenTestVectors
            select new object[] { vector };

        [ConditionalTheory(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void AlgorithmMatches_GenerateKey(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = SlhDsa.GenerateKey(algorithm);
            Assert.Equal(algorithm, slhDsa.Algorithm);
        }

        [ConditionalTheory(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        [MemberData(nameof(NistKeyGenTestVectorsData))]
        public void AlgorithmMatches_Import(SlhDsaTestData.SlhDsaKeyGenTestVector vector)
        {
            using (SlhDsa slhDsa = SlhDsa.ImportSlhDsaSecretKey(vector.Algorithm, vector.SecretKey))
            {
               Assert.Equal(vector.Algorithm, slhDsa.Algorithm);
            }

            using (SlhDsa slhDsa = SlhDsa.ImportSlhDsaPublicKey(vector.Algorithm, vector.PublicKey))
            {
                Assert.Equal(vector.Algorithm, slhDsa.Algorithm);
            }
        }

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
    }
}
