// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class ECDiffieHellmanTests
    {
        private static readonly byte[] s_sampleHmacKey = { 0, 1, 2, 3, 4, 5 };

        [Fact]
        public static void HmacDerivation_OtherKeyRequired()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            {
                Assert.Throws<ArgumentNullException>(
                    () => ecdh.DeriveKeyFromHmac(null, HashAlgorithmName.SHA512, null));
            }
        }

        [Theory]
        [MemberData(nameof(MismatchedKeysizes))]
        public static void HmacDerivation_SameSizeOtherKeyRequired(int aliceSize, int bobSize)
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(aliceSize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(bobSize))
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                Assert.ThrowsAny<ArgumentException>(
                    () => alice.DeriveKeyFromHmac(bobPublic, HashAlgorithmName.SHA512, null));
            }
        }

        [Theory]
        [MemberData(nameof(EveryKeysize))]
        public static void SymmetricDerivation_Hmac(int keySize)
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHmac(bobPublic, HashAlgorithmName.SHA512, s_sampleHmacKey);
                byte[] bobDerived = bob.DeriveKeyFromHmac(alicePublic, HashAlgorithmName.SHA512, s_sampleHmacKey);

                Assert.Equal(aliceDerived, bobDerived);
            }
        }

        [Fact]
        public static void HmacDerivationVariesOnPublicKey()
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHmac(bobPublic, HashAlgorithmName.SHA512, s_sampleHmacKey);
                byte[] aliceSelfDerived = alice.DeriveKeyFromHmac(alicePublic, HashAlgorithmName.SHA512, s_sampleHmacKey);

                // Alice and Alice is HASH(aaG) != HASH(abG)
                // (Except for the fantastically small chance that Alice == Bob)
                Assert.NotEqual(aliceDerived, aliceSelfDerived);
            }
        }

        [Fact]
        public static void HmacDerivationVariesOnAlgorithm()
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHmac(bobPublic, HashAlgorithmName.SHA512, s_sampleHmacKey);
                byte[] bobDerived = bob.DeriveKeyFromHmac(alicePublic, HashAlgorithmName.SHA384, s_sampleHmacKey);

                Assert.NotEqual(aliceDerived, bobDerived);
            }
        }

        [Fact]
        public static void HmacDerivationVariesOnKey()
        {
            byte[] hmacKeyAlice = { 0, 1, 2, 3, 4, 5 };
            byte[] hmacKeyBob = { 10, 1, 2, 3, 4, 5 };

            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHmac(bobPublic, HashAlgorithmName.SHA512, hmacKeyAlice);
                byte[] bobDerived = bob.DeriveKeyFromHmac(alicePublic, HashAlgorithmName.SHA512, hmacKeyBob);

                Assert.NotEqual(aliceDerived, bobDerived);
            }
        }

        [Theory]
        [MemberData(nameof(EveryKeysize))]
        public static void SymmetricDerivation_HmacPrepend(int keySize)
        {
            byte[] prefix = new byte[10];

            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHmac(bobPublic, HashAlgorithmName.SHA512, s_sampleHmacKey, prefix, null);
                byte[] bobDerived = bob.DeriveKeyFromHmac(alicePublic, HashAlgorithmName.SHA512, s_sampleHmacKey, prefix, null);

                Assert.Equal(aliceDerived, bobDerived);
            }
        }

        [Fact]
        public static void HmacDerivationVariesOnPrepend()
        {
            byte[] alicePrefix = new byte[10];
            byte[] bobPrefix = new byte[alicePrefix.Length];
            bobPrefix[0] = 0xFF;

            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHmac(bobPublic, HashAlgorithmName.SHA512, s_sampleHmacKey, alicePrefix, null);
                byte[] bobDerived = bob.DeriveKeyFromHmac(alicePublic, HashAlgorithmName.SHA512, s_sampleHmacKey, bobPrefix, null);

                Assert.NotEqual(aliceDerived, bobDerived);
            }
        }

        [Theory]
        [MemberData(nameof(EveryKeysize))]
        public static void SymmetricDerivation_HmacAppend(int keySize)
        {
            byte[] suffix = new byte[10];

            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHmac(bobPublic, HashAlgorithmName.SHA512, s_sampleHmacKey, null, suffix);
                byte[] bobDerived = bob.DeriveKeyFromHmac(alicePublic, HashAlgorithmName.SHA512, s_sampleHmacKey, null, suffix);

                Assert.Equal(aliceDerived, bobDerived);
            }
        }

        [Fact]
        public static void HmacDerivationVariesOnAppend()
        {
            byte[] aliceSuffix = new byte[10];
            byte[] bobSuffix = new byte[aliceSuffix.Length];
            bobSuffix[0] = 0xFF;

            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHmac(bobPublic, HashAlgorithmName.SHA512, s_sampleHmacKey, null, aliceSuffix);
                byte[] bobDerived = bob.DeriveKeyFromHmac(alicePublic, HashAlgorithmName.SHA512, s_sampleHmacKey, null, bobSuffix);

                Assert.NotEqual(aliceDerived, bobDerived);
            }
        }

        [Fact]
        public static void HmacDerivationIsStable()
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHmac(bobPublic, HashAlgorithmName.SHA512, s_sampleHmacKey);
                byte[] aliceDerivedAgain = alice.DeriveKeyFromHmac(bobPublic, HashAlgorithmName.SHA512, s_sampleHmacKey);

                Assert.Equal(aliceDerived, aliceDerivedAgain);
            }
        }

        [Theory]
        [MemberData(nameof(EveryKeysize))]
        public static void SymmetricDerivation_HmacNullKey(int keySize)
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHmac(bobPublic, HashAlgorithmName.SHA512, null);
                byte[] bobDerived = bob.DeriveKeyFromHmac(alicePublic, HashAlgorithmName.SHA512, null);

                Assert.Equal(aliceDerived, bobDerived);
            }
        }

        [Fact]
        public static void HmacNullKeyDerivationIsStable()
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHmac(bobPublic, HashAlgorithmName.SHA512, null);
                byte[] aliceDerivedAgain = alice.DeriveKeyFromHmac(bobPublic, HashAlgorithmName.SHA512, null);

                Assert.Equal(aliceDerived, aliceDerivedAgain);
            }
        }

        [Fact]
        public static void SimpleHmacMethodForwardsNull()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                byte[] simple = ecdh.DeriveKeyFromHmac(publicKey, HashAlgorithmName.SHA512, s_sampleHmacKey);
                byte[] nulls = ecdh.DeriveKeyFromHmac(publicKey, HashAlgorithmName.SHA512, s_sampleHmacKey, null, null);

                Assert.Equal(simple, nulls);
            }
        }

        [Fact]
        public static void SimpleHmacNullKeyForwardsNull()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                byte[] simple = ecdh.DeriveKeyFromHmac(publicKey, HashAlgorithmName.SHA512, null);
                byte[] nulls = ecdh.DeriveKeyFromHmac(publicKey, HashAlgorithmName.SHA512, null, null, null);

                Assert.Equal(simple, nulls);
            }
        }

        public static IEnumerable<object[]> HmacDerivationTestCases()
        {
            return new object[][]
            {
                new object[]
                {
                    HashAlgorithmName.SHA256,
                    null,
                    null,
                    null,
                    "6D7D15C9A08FD47DFDABD3541BE3BBAF93B15FC65D30E6012CCC0B23ED5C43FF",
                },

                new object[]
                {
                    HashAlgorithmName.SHA1,
                    null,
                    null,
                    null,
                    "39D4B035BC1A1E4108B965689E27BA98ACED8449",
                },

                new object[]
                {
                    HashAlgorithmName.SHA256,
                    "030609",
                    null,
                    null,
                    "7A4F81BF065CC521AFB162DB4A45CEFC78227178A58632EA53D3E367AB7D1979",
                },

                new object[]
                {
                    HashAlgorithmName.SHA256,
                    null,
                    "02040608",
                    "010305",
                    "DB39A6AC9334701D2DCD508C401C65BC69348F684C85EDDE506950F049668842",
                },

                new object[]
                {
                    HashAlgorithmName.SHA256,
                    null,
                    "010305",
                    "02040608",
                    "66471DE2655DF9404636F9076F845F0B71A04DDA2BA6F1469EB0F2E9EF57DC33",
                },

                new object[]
                {
                    HashAlgorithmName.SHA256,
                    "030609",
                    "02040608",
                    "010305",
                    "2F7A31FF9118A6BBF92E268568C634A9F1E244CA8C1A74C864DECC50727B7DEE",
                },

                new object[]
                {
                    HashAlgorithmName.SHA256,
                    "030609",
                    "010305",
                    "02040608",
                    "AE3CD974F262B199B0859D9F933207D2F6E3E04434D60089FE0BE801ED38D370",
                },
            };
        }

#if netcoreapp
        [Theory]
        [MemberData(nameof(HmacDerivationTestCases))]
        public static void HmacDerivation_KnownResults(
            HashAlgorithmName hashAlgorithm,
            string hmacKeyBytes,
            string prependBytes,
            string appendBytes,
            string answerBytes)
        {
            byte[] hmacKey = hmacKeyBytes?.HexToByteArray();
            byte[] prepend = prependBytes?.HexToByteArray();
            byte[] append = appendBytes?.HexToByteArray();
            byte[] answer = answerBytes.HexToByteArray();
            byte[] output;

            using (ECDiffieHellman ecdh = OpenKnownKey())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                output = ecdh.DeriveKeyFromHmac(publicKey, hashAlgorithm, hmacKey, prepend, append);
            }

            Assert.Equal(answer, output);
        }
#endif
    }
}
