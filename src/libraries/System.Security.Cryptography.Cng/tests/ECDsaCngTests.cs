// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Cryptography.EcDsa.Tests;
using System.Security.Cryptography.Tests;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public class ECDsaCngTests : ECDsaTestsBase
    {
        [Fact]
        public static void TestNegativeVerify256()
        {
            CngKey key = TestData.s_ECDsa256Key;
            ECDsaCng e = new ECDsaCng(key);

            byte[] tamperedSig = ("998791331eb2e1f4259297f5d9cb82fa20dec98e1cb0900e6b8f014a406c3d02cbdbf5238bde471c3155fc25565524301429"
                                + "e8713dad9a67eb0a5c355e9e23dc").HexToByteArray();
            bool verified = e.VerifyHash(EccTestData.s_hashSha512, tamperedSig);
            Assert.False(verified);
        }

        [Fact]
        public static void TestPositiveVerify384()
        {
            CngKey key = TestData.s_ECDsa384Key;
            ECDsaCng e = new ECDsaCng(key);

            byte[] sig = ("7805c494b17bba8cba09d3e5cdd16d69ce785e56c4f2d9d9061d549fce0a6860cca1cb9326bd534da21ad4ff326a1e0810d8"
                        + "2366eb6afc66ede0d1ffe345f6b37ac622ed77838b42825ceb96cd3996d3d77fd6a248357ae1ae6cb85f048b1b04").HexToByteArray();
            bool verified = e.VerifyHash(EccTestData.s_hashSha512, sig);
            Assert.True(verified);
        }

        [Fact]
        public static void TestNegativeVerify384()
        {
            CngKey key = TestData.s_ECDsa384Key;
            ECDsaCng e = new ECDsaCng(key);

            byte[] tamperedSig = ("7805c494b17bba8cba09d3e5cdd16d69ce785e56c4f2d9d9061d549fce0a6860cca1cb9326bd534da21ad4ff326a1e0810d8"
                                + "f366eb6afc66ede0d1ffe345f6b37ac622ed77838b42825ceb96cd3996d3d77fd6a248357ae1ae6cb85f048b1b04").HexToByteArray();
            bool verified = e.VerifyHash(EccTestData.s_hashSha512, tamperedSig);
            Assert.False(verified);
        }

        [Fact]
        public static void TestPositiveVerify521()
        {
            CngKey key = TestData.s_ECDsa521Key;
            ECDsaCng e = new ECDsaCng(key);

            byte[] sig = ("0084461450745672df85735fbf89f2dccef804d6b56e86ca45ea5c366a05a5de96327eddb75582821c6315c8bb823c875845"
                        + "b6f25963ddab70461b786261507f971401fdc300697824129e0a84e0ba1ab4820ac7b29e7f8248bc2e29d152a9190eb3fcb7"
                        + "6e8ebf1aa5dd28ffd582a24cbfebb3426a5f933ce1d995b31c951103d24f6256").HexToByteArray();
            bool verified = e.VerifyHash(EccTestData.s_hashSha512, sig);
            Assert.True(verified);
        }

        [Fact]
        public static void TestNegativeVerify521()
        {
            CngKey key = TestData.s_ECDsa521Key;
            ECDsaCng e = new ECDsaCng(key);

            byte[] tamperedSig = ("0084461450745672df85735fbf89f2dccef804d6b56e86ca45ea5c366a05a5de96327eddb75582821c6315c8bb823c875845"
                                + "a6f25963ddab70461b786261507f971401fdc300697824129e0a84e0ba1ab4820ac7b29e7f8248bc2e29d152a9190eb3fcb7"
                                + "6e8ebf1aa5dd28ffd582a24cbfebb3426a5f933ce1d995b31c951103d24f6256").HexToByteArray();
            bool verified = e.VerifyHash(EccTestData.s_hashSha512, tamperedSig);
            Assert.False(verified);
        }

        [Fact]
        public static void TestVerify521_EcdhKey()
        {
            byte[] keyBlob = (byte[])TestData.s_ECDsa521KeyBlob.Clone();

            // Rewrite the dwMagic value to be ECDH
            // ECDSA prefix: 45 43 53 36
            // ECDH prefix : 45 43 4b 36
            keyBlob[2] = 0x4b;

            using (CngKey ecdh521 = CngKey.Import(keyBlob, CngKeyBlobFormat.EccPrivateBlob))
            {
                // Preconditions:
                Assert.Equal(CngAlgorithmGroup.ECDiffieHellman, ecdh521.AlgorithmGroup);
                Assert.Equal(CngAlgorithm.ECDiffieHellmanP521, ecdh521.Algorithm);

                using (ECDsa ecdsaFromEcdsaKey = new ECDsaCng(TestData.s_ECDsa521Key))
                using (ECDsa ecdsaFromEcdhKey = new ECDsaCng(ecdh521))
                {
                    byte[] ecdhKeySignature = ecdsaFromEcdhKey.SignData(keyBlob, HashAlgorithmName.SHA512);
                    byte[] ecdsaKeySignature = ecdsaFromEcdsaKey.SignData(keyBlob, HashAlgorithmName.SHA512);

                    Assert.True(
                        ecdsaFromEcdhKey.VerifyData(keyBlob, ecdsaKeySignature, HashAlgorithmName.SHA512),
                        "ECDsaCng(ECDHKey) validates ECDsaCng(ECDsaKey)");

                    Assert.True(
                        ecdsaFromEcdsaKey.VerifyData(keyBlob, ecdhKeySignature, HashAlgorithmName.SHA512),
                        "ECDsaCng(ECDsaKey) validates ECDsaCng(ECDHKey)");
                }
            }
        }

        [Fact]
        public static void CreateEcdsaFromRsaKey_Fails()
        {
            using (RSACng rsaCng = new RSACng())
            {
                AssertExtensions.Throws<ArgumentException>("key", () => new ECDsaCng(rsaCng.Key));
            }
        }

        [Fact]
        public static void TestCreateKeyFromCngAlgorithmNistP256()
        {
            CngAlgorithm alg = CngAlgorithm.ECDsaP256;

            using (CngKey key = CngKey.Create(alg))
            {
                VerifyKey(key);
                using (ECDsaCng e = new ECDsaCng(key))
                {
                    Assert.Equal(CngAlgorithmGroup.ECDsa, e.Key.AlgorithmGroup);
                    Assert.Equal(CngAlgorithm.ECDsaP256, e.Key.Algorithm);
                    VerifyKey(e.Key);
                    e.Exercise();
                }
            }
        }

        [Fact]
        public static void TestCreateByKeySizeNistP256()
        {
            using (ECDsaCng cng = new ECDsaCng(256))
            {
                CngKey key1 = cng.Key;
                Assert.Equal(CngAlgorithmGroup.ECDsa, key1.AlgorithmGroup);

                // The three legacy nist curves are not treated as generic named curves
                Assert.Equal(CngAlgorithm.ECDsaP256, key1.Algorithm);

                Assert.Equal(256, key1.KeySize);
                VerifyKey(key1);
            }
        }

        [Fact]
        public static void HashAlgorithm_DefaultsToSha256()
        {
            using (var cng = new ECDsaCng())
               Assert.Equal(CngAlgorithm.Sha256, cng.HashAlgorithm);
        }

#if netcoreapp
        [Fact]
        public static void TestPositive256WithBlob()
        {
            CngKey key = TestData.s_ECDsa256Key;
            ECDsaCng e = new ECDsaCng(key);
            Verify256(e, true);
        }

        [Theory, MemberData(nameof(TestCurves))]
        public static void TestKeyPropertyFromNamedCurve(CurveDef curveDef)
        {
            ECDsaCng e = new ECDsaCng(curveDef.Curve);
            CngKey key1 = e.Key;
            VerifyKey(key1);
            e.Exercise();

            CngKey key2 = e.Key;
            Assert.Same(key1, key2);
        }

        [Fact]
        public static void TestCreateByNameNistP521()
        {
            using (ECDsaCng cng = new ECDsaCng(ECCurve.NamedCurves.nistP521))
            {
                CngKey key1 = cng.Key;
                Assert.Equal(CngAlgorithmGroup.ECDsa, key1.AlgorithmGroup);

                // The three legacy nist curves are not treated as generic named curves
                Assert.Equal(CngAlgorithm.ECDsaP521, key1.Algorithm);

                Assert.Equal(521, key1.KeySize);
                VerifyKey(key1);
            }
        }

        [Theory, MemberData(nameof(TestInvalidCurves))]
        public static void TestCreateKeyFromCngAlgorithmNegative(CurveDef curveDef)
        {
            CngAlgorithm alg = CngAlgorithm.ECDsa;
            Assert.ThrowsAny<Exception>(() => CngKey.Create(alg));
        }

        [Theory, MemberData(nameof(SpecialNistKeys))]
        public static void TestSpecialNistKeys(int keySize, string curveName, CngAlgorithm algorithm)
        {
            using (ECDsaCng cng = (ECDsaCng)ECDsaFactory.Create(keySize))
            {
                Assert.Equal(keySize, cng.KeySize);
                ECParameters param = cng.ExportParameters(false);
                Assert.Equal(curveName, param.Curve.Oid.FriendlyName);
                Assert.Equal(algorithm, cng.Key.Algorithm);
            }
        }
#endif // netcoreapp

        public static IEnumerable<object[]> SpecialNistKeys
        {
            get
            {
                yield return new object[] { 256, "nistP256", CngAlgorithm.ECDsaP256};
                yield return new object[] { 384, "nistP384", CngAlgorithm.ECDsaP384};
                yield return new object[] { 521, "nistP521", CngAlgorithm.ECDsaP521};
            }
        }

        private static void VerifyKey(CngKey key)
        {
            Assert.Equal("ECDSA", key.AlgorithmGroup.AlgorithmGroup);
            Assert.False(string.IsNullOrEmpty(key.Algorithm.Algorithm));
            Assert.True(key.KeySize > 0);
        }
    }
}
