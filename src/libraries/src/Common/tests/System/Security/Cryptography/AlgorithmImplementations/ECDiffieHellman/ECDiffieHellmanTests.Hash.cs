// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class ECDiffieHellmanTests
    {
        [Fact]
        public static void HashDerivation_OtherKeyRequired()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            {
                Assert.Throws<ArgumentNullException>(
                    () => ecdh.DeriveKeyFromHash(null, HashAlgorithmName.SHA512));
            }
        }

        [Theory]
        [MemberData(nameof(MismatchedKeysizes))]
        public static void HashDerivation_SameSizeOtherKeyRequired(int aliceSize, int bobSize)
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(aliceSize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(bobSize))
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                Assert.ThrowsAny<ArgumentException>(
                    () => alice.DeriveKeyFromHash(bobPublic, HashAlgorithmName.SHA512));
            }
        }

        [Fact]
        public static void HashDerivation_AlgorithmRequired()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                Assert.Throws<ArgumentException>(
                    () => ecdh.DeriveKeyFromHash(publicKey, new HashAlgorithmName("")));
            }
        }

        [Theory]
        [MemberData(nameof(EveryKeysize))]
        public static void HashDerivation(int keySize)
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHash(bobPublic, HashAlgorithmName.SHA512);
                byte[] bobDerived = bob.DeriveKeyFromHash(alicePublic, HashAlgorithmName.SHA512);

                Assert.Equal(aliceDerived, bobDerived);
            }
        }

        [Fact]
        public static void HashDerivationVariesOnPublicKey()
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHash(bobPublic, HashAlgorithmName.SHA512);
                byte[] aliceSelfDerived = alice.DeriveKeyFromHash(alicePublic, HashAlgorithmName.SHA512);

                // Alice and Alice is HASH(aaG) != HASH(abG)
                // (Except for the fantastically small chance that Alice == Bob)
                Assert.NotEqual(aliceDerived, aliceSelfDerived);
            }
        }

        [Fact]
        public static void HashDerivationVariesOnAlgorithm()
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHash(bobPublic, HashAlgorithmName.SHA512);
                byte[] bobDerived = bob.DeriveKeyFromHash(alicePublic, HashAlgorithmName.SHA384);

                Assert.NotEqual(aliceDerived, bobDerived);
            }
        }

        [Theory]
        [MemberData(nameof(EveryKeysize))]
        public static void SymmetricDerivation_HashPrepend(int keySize)
        {
            byte[] prefix = new byte[10];

            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHash(bobPublic, HashAlgorithmName.SHA512, prefix, null);
                byte[] bobDerived = bob.DeriveKeyFromHash(alicePublic, HashAlgorithmName.SHA512, prefix, null);

                Assert.Equal(aliceDerived, bobDerived);
            }
        }

        [Fact]
        public static void HashDerivationVariesOnPrepend()
        {
            byte[] alicePrefix = new byte[10];
            byte[] bobPrefix = new byte[alicePrefix.Length];
            bobPrefix[0] = 0xFF;

            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHash(bobPublic, HashAlgorithmName.SHA512, alicePrefix, null);
                byte[] bobDerived = alice.DeriveKeyFromHash(alicePublic, HashAlgorithmName.SHA512, bobPrefix, null);

                Assert.NotEqual(aliceDerived, bobDerived);
            }
        }

        [Theory]
        [MemberData(nameof(EveryKeysize))]
        public static void SymmetricDerivation_HashAppend(int keySize)
        {
            byte[] suffix = new byte[10];

            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHash(bobPublic, HashAlgorithmName.SHA512, null, suffix);
                byte[] bobDerived = bob.DeriveKeyFromHash(alicePublic, HashAlgorithmName.SHA512, null, suffix);

                Assert.Equal(aliceDerived, bobDerived);
            }
        }

        [Fact]
        public static void HashDerivationVariesOnAppend()
        {
            byte[] aliceSuffix = new byte[10];
            byte[] bobSuffix = new byte[aliceSuffix.Length];
            bobSuffix[0] = 0xFF;

            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHash(bobPublic, HashAlgorithmName.SHA512, null, aliceSuffix);
                byte[] bobDerived = alice.DeriveKeyFromHash(alicePublic, HashAlgorithmName.SHA512, null, bobSuffix);

                Assert.NotEqual(aliceDerived, bobDerived);
            }
        }

        [Fact]
        public static void HashDerivationIsStable()
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyFromHash(bobPublic, HashAlgorithmName.SHA512);
                byte[] aliceDerivedAgain = alice.DeriveKeyFromHash(bobPublic, HashAlgorithmName.SHA512);

                Assert.Equal(aliceDerived, aliceDerivedAgain);
            }
        }

        [Fact]
        public static void SimpleHashMethodForwardsNull()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                byte[] simple = ecdh.DeriveKeyFromHash(publicKey, HashAlgorithmName.SHA512);
                byte[] nulls = ecdh.DeriveKeyFromHash(publicKey, HashAlgorithmName.SHA512, null, null);

                Assert.Equal(simple, nulls);
            }
        }

        [Fact]
        public static void DeriveKeyMaterialEquivalentToDeriveKeyFromHash()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                byte[] simple = ecdh.DeriveKeyMaterial(publicKey);
                byte[] nulls = ecdh.DeriveKeyFromHash(publicKey, HashAlgorithmName.SHA256, null, null);

                Assert.Equal(simple, nulls);
            }
        }

        public static IEnumerable<object[]> HashDerivationTestCases()
        {
            return new object[][]
            {
                new object[]
                {
                    HashAlgorithmName.SHA256,
                    null,
                    null,
                    "595B71C33D9D40ACD9CA847C47267DAEE7498EEF0B553482FAA45791418AC679",
                },

                new object[]
                {
                    HashAlgorithmName.SHA1,
                    null,
                    null,
                    "25E464FAC33F4A5F8786627FB3685F4C31B26327",
                },

                new object[]
                {
                    HashAlgorithmName.SHA256,
                    "02040608",
                    null,
                    "D0F4C42D61E794E508A079822F3069C9F89D9E3385C8E090425FF38927798017",
                },

                new object[]
                {
                    HashAlgorithmName.SHA256,
                    null,
                    "010305",
                    "20DCB58E2AC4E70B1BF47362B0D1C8B728E27D6575EA9B85106CBE05E1F7D6DB",
                },

                new object[]
                {
                    HashAlgorithmName.SHA256,
                    "02040608",
                    "010305",
                    "EFC758D39896E9DE96C120B0A74FB751F140BD7F3F4FC3777DC2A530145E01EC",
                },

                new object[]
                {
                    HashAlgorithmName.SHA256,
                    "010305",
                    "02040608",
                    "7DB5520A5D6351595FC286CD53509D964FBB152C289F072581CB5E16EBF319E8",
                },
            };
        }

#if netcoreapp
        [Theory]
        [MemberData(nameof(HashDerivationTestCases))]
        public static void HashDerivation_KnownResults(
            HashAlgorithmName hashAlgorithm,
            string prependBytes,
            string appendBytes,
            string answerBytes)
        {
            byte[] prepend = prependBytes?.HexToByteArray();
            byte[] append = appendBytes?.HexToByteArray();
            byte[] answer = answerBytes.HexToByteArray();
            byte[] output;

            using (ECDiffieHellman ecdh = OpenKnownKey())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                output = ecdh.DeriveKeyFromHash(publicKey, hashAlgorithm, prepend, append);
            }

            Assert.Equal(answer, output);
        }
#endif
    }
}
