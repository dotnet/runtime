// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Tests;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class ECDiffieHellmanTests
    {
        [ConditionalFact]
        public void RawDerivation_OtherKeyRequired()
        {
            SkipTestException.ThrowUnless(ECDiffieHellmanFactory.SupportsRawDerivation);

            using (ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create())
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "otherPartyPublicKey",
                    () => ecdh.DeriveRawSecretAgreement(null));
            }
        }

        [ConditionalFact]
        public void RawDerivation_SameSizeOtherKeyRequired()
        {
            SkipTestException.ThrowUnless(ECDiffieHellmanFactory.SupportsRawDerivation);

            ForEachMismatchedKeySize(RawDerivation_SameSizeOtherKeyRequiredImpl);
        }

        private void RawDerivation_SameSizeOtherKeyRequiredImpl(int aliceSize, int bobSize)
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

        [ConditionalFact]
        public void RawDerivation_DeriveSharedSecret_Agree()
        {
            SkipTestException.ThrowUnless(ECDiffieHellmanFactory.SupportsRawDerivation);

            ForEachKeySize(RawDerivation_DeriveSharedSecret_AgreeImpl);
        }

        private void RawDerivation_DeriveSharedSecret_AgreeImpl(int keySize)
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

        [ConditionalFact]
        public void RawDerivation_DeriveSharedSecret_Disagree()
        {
            SkipTestException.ThrowUnless(ECDiffieHellmanFactory.SupportsRawDerivation);

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

        [ConditionalFact]
        public void RawDerivation_DeriveIsStable()
        {
            SkipTestException.ThrowUnless(ECDiffieHellmanFactory.SupportsRawDerivation);

            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                byte[] aliceDerived1 = alice.DeriveRawSecretAgreement(bobPublic);
                byte[] aliceDerived2 = alice.DeriveRawSecretAgreement(bobPublic);
                Assert.Equal(aliceDerived1, aliceDerived2);
            }
        }

        [ConditionalFact]
        public void RawDerivation_NotSupported()
        {
            SkipTestException.ThrowWhen(ECDiffieHellmanFactory.SupportsRawDerivation);

            using (ECDiffieHellman alice = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellman bob = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256))
            using (ECDiffieHellmanPublicKey bobPublic = bob.PublicKey)
            {
                Assert.Throws<PlatformNotSupportedException>(() => alice.DeriveRawSecretAgreement(bobPublic));
            }
        }
    }
}
