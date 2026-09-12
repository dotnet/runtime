// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class HpkeTests
    {
        [Fact]
        public static void KeyFactories_NotSupported()
        {
            foreach (HpkeKem kem in Enum.GetValues<HpkeKem>())
            foreach (HpkeKdf kdf in Enum.GetValues<HpkeKdf>())
            foreach (HpkeAead aead in Enum.GetValues<HpkeAead>())
            {
                HpkeSuite suite = new(kem, kdf, aead);

                if (!Hpke.IsSupported(suite))
                {
                    byte[] privateKey = new byte[suite.DecapsulationKeySizeInBytes];
                    byte[] publicKey = new byte[suite.EncapsulationKeySizeInBytes];
                    Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                    Assert.Throws<PlatformNotSupportedException>(() => Hpke.DeriveKey(suite, privateKey));
                    Assert.Throws<PlatformNotSupportedException>(() => Hpke.DeriveKey(suite, privateKey.AsSpan()));
                    Assert.Throws<PlatformNotSupportedException>(() => Hpke.ImportDecapsulationKey(suite, privateKey));
                    Assert.Throws<PlatformNotSupportedException>(
                        () => Hpke.ImportDecapsulationKey(suite, privateKey.AsSpan()));
                    Assert.Throws<PlatformNotSupportedException>(() => Hpke.ImportEncapsulationKey(suite, publicKey));
                    Assert.Throws<PlatformNotSupportedException>(
                        () => Hpke.ImportEncapsulationKey(suite, publicKey.AsSpan()));
                }
            }
        }
    }
}
