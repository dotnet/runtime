// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class ECDiffieHellmanTests
    {
        private static ECDiffieHellmanCng NewDefaultECDHCng()
        {
            return new ECDiffieHellmanCng();
        }

        [Fact]
        public static void ECCurve_ctor()
        {
            using (ECDiffieHellman ecdh = new ECDiffieHellmanCng(ECCurve.NamedCurves.nistP256))
            {
                Assert.Equal(256, ecdh.KeySize);
                ecdh.Exercise();
            }

            using (ECDiffieHellman ecdh = new ECDiffieHellmanCng(ECCurve.NamedCurves.nistP384))
            {
                Assert.Equal(384, ecdh.KeySize);
                ecdh.Exercise();
            }

            using (ECDiffieHellman ecdh = new ECDiffieHellmanCng(ECCurve.NamedCurves.nistP521))
            {
                Assert.Equal(521, ecdh.KeySize);
                ecdh.Exercise();
            }
        }

        [Fact]
        public static void CngKey_ReusesObject()
        {
            using (ECDiffieHellmanCng ecdh = NewDefaultECDHCng())
            {
                CngKey key1 = ecdh.Key;
                CngKey key2 = ecdh.Key;

                Assert.Same(key1, key2);
            }
        }

        public static IEnumerable<object[]> HashEquivalenceData()
        {
            return new object[][]
            {
                new object[] { HashAlgorithmName.SHA256, false, false },
                new object[] { HashAlgorithmName.SHA256, true, false },
                new object[] { HashAlgorithmName.SHA256, false, true },
                new object[] { HashAlgorithmName.SHA256, true, true },

                new object[] { HashAlgorithmName.SHA384, false, false },
                new object[] { HashAlgorithmName.SHA384, true, false },
                new object[] { HashAlgorithmName.SHA384, false, true },
                new object[] { HashAlgorithmName.SHA384, true, true },
            };
        }

        [Theory]
        [MemberData(nameof(HashEquivalenceData))]
        public static void Equivalence_Hash(HashAlgorithmName algorithm, bool prepend, bool append)
        {
            using (ECDiffieHellmanCng ecdh = NewDefaultECDHCng())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                byte[] secretPrepend = prepend ? new byte[3] : null;
                byte[] secretAppend = append ? new byte[4] : null;

                byte[] newWay = ecdh.DeriveKeyFromHash(publicKey, algorithm, secretPrepend, secretAppend);

                ecdh.HashAlgorithm = new CngAlgorithm(algorithm.Name);
                ecdh.SecretPrepend = secretPrepend;
                ecdh.SecretAppend = secretAppend;
                ecdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;

                byte[] oldWay = ecdh.DeriveKeyMaterial(publicKey);

                Assert.Equal(newWay, oldWay);
            }
        }

        public static IEnumerable<object[]> HmacEquivalenceData()
        {
            return new object[][]
            {
                new object[] { HashAlgorithmName.SHA256, false, false, false },
                new object[] { HashAlgorithmName.SHA256, true, false, false },
                new object[] { HashAlgorithmName.SHA256, false, false, true },
                new object[] { HashAlgorithmName.SHA256, true, false, true },
                new object[] { HashAlgorithmName.SHA256, false, true, false },
                new object[] { HashAlgorithmName.SHA256, true, true, false },
                new object[] { HashAlgorithmName.SHA256, false, true, true },
                new object[] { HashAlgorithmName.SHA256, true, true, true },

                new object[] { HashAlgorithmName.SHA384, false, false, false },
                new object[] { HashAlgorithmName.SHA384, true, false, false },
                new object[] { HashAlgorithmName.SHA384, false, false, true },
                new object[] { HashAlgorithmName.SHA384, true, false, true },
                new object[] { HashAlgorithmName.SHA384, false, true, false },
                new object[] { HashAlgorithmName.SHA384, true, true, false },
                new object[] { HashAlgorithmName.SHA384, false, true, true },
                new object[] { HashAlgorithmName.SHA384, true, true, true },
            };
        }

        [Theory]
        [MemberData(nameof(HmacEquivalenceData))]
        public static void Equivalence_Hmac(HashAlgorithmName algorithm, bool useSecretAgreementAsHmac, bool prepend, bool append)
        {
            using (ECDiffieHellmanCng ecdh = NewDefaultECDHCng())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                byte[] secretPrepend = prepend ? new byte[3] : null;
                byte[] secretAppend = append ? new byte[4] : null;
                byte[] hmacKey = useSecretAgreementAsHmac ? null : new byte[12];

                byte[] newWay = ecdh.DeriveKeyFromHmac(publicKey, algorithm, hmacKey, secretPrepend, secretAppend);

                ecdh.HashAlgorithm = new CngAlgorithm(algorithm.Name);
                ecdh.HmacKey = hmacKey;
                ecdh.SecretPrepend = secretPrepend;
                ecdh.SecretAppend = secretAppend;
                ecdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hmac;

                byte[] oldWay = ecdh.DeriveKeyMaterial(publicKey);

                Assert.Equal(newWay, oldWay);
            }
        }

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        public static void Equivalence_TlsPrf(int labelSize)
        {
            using (ECDiffieHellmanCng ecdh = NewDefaultECDHCng())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                byte[] label = new byte[labelSize];
                byte[] seed = new byte[64];

                byte[] newWay = ecdh.DeriveKeyTls(publicKey, label, seed);

                ecdh.Label = label;
                ecdh.Seed = seed;
                ecdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Tls;

                byte[] oldWay = ecdh.DeriveKeyMaterial(publicKey);

                Assert.Equal(newWay, oldWay);
            }
        }

        [Fact]
        public static void HashDerivation_AlgorithmsCreateECDH()
        {
            using (ECDiffieHellman ecdhCng = new ECDiffieHellmanCng())
            using (ECDiffieHellman ecdhAlgorithms = ECDiffieHellman.Create())
            {
                Assert.NotNull(ecdhAlgorithms.PublicKey);
                byte[] outputCng = ecdhCng.DeriveKeyMaterial(ecdhAlgorithms.PublicKey);
                byte[] outputAlgorithms = ecdhAlgorithms.DeriveKeyMaterial(ecdhCng.PublicKey);
                Assert.Equal(outputCng, outputAlgorithms);
            }
        }

        [Fact]
        public static void HashAlgorithm_DefaultsToSha256()
        {
            using (var ecdhCng = new ECDiffieHellmanCng())
                Assert.Equal(CngAlgorithm.Sha256, ecdhCng.HashAlgorithm);
        }

        [Fact]
        public static void HashAlgorithm_SupportsOtherECDHImplementations()
        {
            using ECDiffieHellman ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using ECDiffieHellmanCng ecdhCng = new ECDiffieHellmanCng(ECCurve.NamedCurves.nistP256);
            byte[] key1 = ecdhCng.DeriveKeyFromHash(ecdh.PublicKey, HashAlgorithmName.SHA256);
            byte[] key2 = ecdh.DeriveKeyFromHash(ecdhCng.PublicKey, HashAlgorithmName.SHA256);
            Assert.Equal(key1, key2);
        }
    }
}
