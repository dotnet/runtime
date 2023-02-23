// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class ECDiffieHellmanTests
    {
        public static bool DoesNotSupportRawDerivation => !ECDiffieHellmanFactory.SupportsRawDerivation;

        [ConditionalFact(typeof(ECDiffieHellmanFactory), nameof(ECDiffieHellmanFactory.SupportsRawDerivation))]
        public static void RawDerivation_OtherKeyRequired()
        {
            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "otherPartyPublicKey",
                    () => ecdh.DeriveRawSecretAgreement(null));
            }
        }

        [ConditionalTheory(typeof(ECDiffieHellmanFactory), nameof(ECDiffieHellmanFactory.SupportsRawDerivation))]
        [MemberData(nameof(MismatchedKeysizes))]
        public static void RawDerivation_SameSizeOtherKeyRequired(int aliceSize, int bobSize)
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(aliceSize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(bobSize))
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                AssertExtensions.Throws<ArgumentException>(
                    "otherPartyPublicKey",
                    () => alice.DeriveRawSecretAgreement(bobPublic));
            }
        }

        [ConditionalTheory(typeof(ECDiffieHellmanFactory), nameof(ECDiffieHellmanFactory.SupportsRawDerivation))]
        [MemberData(nameof(EveryKeysize))]
        public static void RawDerivation_DeriveSharedSecret_Agree(int keySize)
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(keySize))
            using (ECDiffieHellmanPublicKey alicePublic = alice.PublicKey)
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveRawSecretAgreement(bobPublic);
                byte[] bobDerived = bob.DeriveRawSecretAgreement(alicePublic);
                Assert.Equal(aliceDerived, bobDerived);
            }
        }

        [ConditionalFact(typeof(ECDiffieHellmanFactory), nameof(ECDiffieHellmanFactory.SupportsRawDerivation))]
        public static void RawDerivation_DeriveSharedSecret_Disagree()
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellman eve = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            using (ECDiffieHellmanPublicKey evePublic = eve.PublicKey)
            {
                byte[] aliceDerived = alice.DeriveRawSecretAgreement(bobPublic);
                byte[] eveDerived = alice.DeriveRawSecretAgreement(evePublic);

                Assert.NotEqual(aliceDerived, eveDerived);
            }
        }

        [ConditionalFact(typeof(ECDiffieHellmanFactory), nameof(ECDiffieHellmanFactory.SupportsRawDerivation))]
        public static void RawDerivation_DeriveIsStable()
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived1 = alice.DeriveRawSecretAgreement(bobPublic);
                byte[] aliceDerived2 = alice.DeriveRawSecretAgreement(bobPublic);
                Assert.Equal(aliceDerived1, aliceDerived2);
            }
        }

        [ConditionalFact(nameof(DoesNotSupportRawDerivation))]
        public static void RawDerivation_NotSupported()
        {
            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                Assert.Throws<PlatformNotSupportedException>(() => alice.DeriveRawSecretAgreement(bobPublic));
            }
        }
    }
}
