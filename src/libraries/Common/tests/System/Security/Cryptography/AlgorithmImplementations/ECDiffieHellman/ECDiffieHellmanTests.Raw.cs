// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class ECDiffieHellmanTests
    {
        [Fact]
        public static void RawDerivation_OtherKeyRequired()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "otherPartyPublicKey",
                    () => ecdh.DeriveSecretAgreement(null));
            }
        }

        [Theory]
        [MemberData(nameof(MismatchedKeysizes))]
        public static void RawDerivation_SameSizeOtherKeyRequired(int aliceSize, int bobSize)
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(aliceSize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(bobSize))
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                AssertExtensions.Throws<ArgumentException>(
                    "otherPartyPublicKey",
                    () => alice.DeriveSecretAgreement(bobPublic));
            }
        }

        [Theory]
        [MemberData(nameof(EveryKeysize))]
        public static void RawDerivation_DeriveSharedSecret_Agree(int keySize)
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveSecretAgreement(bobPublic);
                byte[] bobDerived = bob.DeriveSecretAgreement(alicePublic);
                Assert.Equal(aliceDerived, bobDerived);
            }
        }

        [Fact]
        public static void RawDerivation_DeriveSharedSecret_Disagree()
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellman eve = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            using (ECDiffieHellmanPublicKey evePublic = eve.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveSecretAgreement(bobPublic);
                byte[] eveDerived = alice.DeriveSecretAgreement(evePublic);

                Assert.NotEqual(aliceDerived, eveDerived);
            }
        }

        [Fact]
        public static void RawDerivation_DeriveIsStable()
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived1 = alice.DeriveSecretAgreement(bobPublic);
                byte[] aliceDerived2 = alice.DeriveSecretAgreement(bobPublic);
                Assert.Equal(aliceDerived1, aliceDerived2);
            }
        }
    }
}
