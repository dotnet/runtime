// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class ECDiffieHellmanTests
    {
        private static readonly byte[] s_fourByteLabel = new byte[4];
        private static readonly byte[] s_emptySeed = new byte[64];

        [Fact]
        public static void TlsDerivation_OtherKeyRequired()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            {
                Assert.Throws<ArgumentNullException>(
                    () => ecdh.DeriveKeyTls(null, s_fourByteLabel, s_emptySeed));
            }
        }

        [Theory]
        [MemberData(nameof(MismatchedKeysizes))]
        public static void TlsDerivation_SameSizeOtherKeyRequired(int aliceSize, int bobSize)
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(aliceSize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(bobSize))
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                Assert.ThrowsAny<ArgumentException>(
                    () => alice.DeriveKeyTls(bobPublic, s_fourByteLabel, s_emptySeed));
            }
        }

        [Fact]
        public static void TlsRequiresLabel()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                Assert.Throws<ArgumentNullException>(
                    () => ecdh.DeriveKeyTls(publicKey, null, s_emptySeed));
            }
        }

        [Fact]
        public static void TlsRequiresSeed()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                Assert.Throws<ArgumentNullException>(
                    () => ecdh.DeriveKeyTls(publicKey, s_fourByteLabel, null));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(63)]
        [InlineData(65)]
        public static void TlsRequiresSeed64(int seedSize)
        {
            byte[] seed = new byte[seedSize];

            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => ecdh.DeriveKeyTls(publicKey, s_fourByteLabel, seed));
            }
        }

        [Theory]
        [MemberData(nameof(EveryKeysize))]
        public static void SymmetricDerivation_TlsPrf(int keySize)
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyTls(bobPublic, s_fourByteLabel, s_emptySeed);
                byte[] bobDerived = bob.DeriveKeyTls(alicePublic, s_fourByteLabel, s_emptySeed);

                Assert.Equal(aliceDerived, bobDerived);
            }
        }

        [Fact]
        public static void TlsPrfDerivationIsStable()
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyTls(bobPublic, s_fourByteLabel, s_emptySeed);
                byte[] aliceDerivedAgain = alice.DeriveKeyTls(bobPublic, s_fourByteLabel, s_emptySeed);

                Assert.Equal(aliceDerived, aliceDerivedAgain);
            }
        }

        [Theory]
        [MemberData(nameof(EveryKeysize))]
        public static void TlsPrfOutputIs48Bytes(int keySize)
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
            {
                byte[] derived = ecdh.DeriveKeyTls(publicKey, s_fourByteLabel, s_emptySeed);

                Assert.Equal(48, derived.Length);
            }
        }

        [Fact]
        public static void TlsPrfVariesOnOtherKey()
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyTls(bobPublic, s_fourByteLabel, s_emptySeed);
                byte[] aliceSelfDerived = alice.DeriveKeyTls(alicePublic, s_fourByteLabel, s_emptySeed);

                // Alice and Alice is HASH(aaG) != HASH(abG)
                // (Except for the fantastically small chance that Alice == Bob)
                Assert.NotEqual(aliceDerived, aliceSelfDerived);
            }
        }

        [Fact]
        public static void TlsPrfVariesOnLabel()
        {
            byte[] aliceLabel = s_fourByteLabel;
            byte[] bobLabel = new byte[5];

            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyTls(bobPublic, aliceLabel, s_emptySeed);
                byte[] bobDerived = bob.DeriveKeyTls(alicePublic, bobLabel, s_emptySeed);

                Assert.NotEqual(aliceDerived, bobDerived);
            }
        }

        [Fact]
        public static void TlsPrfVariesOnSeed()
        {
            byte[] aliceSeed = s_emptySeed;
            byte[] bobSeed = new byte[64];
            bobSeed[0] = 0x81;

            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create())
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveKeyTls(bobPublic, s_fourByteLabel, aliceSeed);
                byte[] bobDerived = bob.DeriveKeyTls(alicePublic, s_fourByteLabel, bobSeed);

                Assert.NotEqual(aliceDerived, bobDerived);
            }
        }

        public static IEnumerable<object[]> TlsDerivationTestCases()
        {
            return new object[][]
            {
                 new object[]
                 {
                     "slithy toves",
                     "3D5DCEF2B35E73523C34802175875CC241A966D2DEB89041540650478D300A70F822AF7F9D70A31BA4B67D100F4A1620",
                 },

                 new object[]
                 {
                     "Hello, World!",
                     "ED83A7CF14C6F1577FE7AD90F1D78D36AFF5D2612B78A70E5FD000660E4A3B00DFF9B7C118C29A3D32536A89A5481C00",
                 },
            };
        }

#if netcoreapp
        [Theory]
        [MemberData(nameof(TlsDerivationTestCases))]
        public static void TlsDerivation_KnownResults(string labelText, string answerHex)
        {
            byte[] label = Encoding.ASCII.GetBytes(labelText);
            byte[] output;

            using (ECDiffieHellman ecdh = OpenKnownKey())
            {
                using (ECDiffieHellmanPublicKey publicKey = ecdh.PublicKey)
                {
                    output = ecdh.DeriveKeyTls(publicKey, label, s_emptySeed);
                }
            }

            Assert.Equal(answerHex, output.ByteArrayToHex());
        }
#endif
    }
}
