// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.CryptoConfigTests
{
    public static class CryptoConfigTests
    {
        [Fact]
        public static void StaticCreateMethods()
        {
            // These are not supported because CryptoConfig exists in Algorithms assembly.
            // CryptoConfig exists in Algorithms partly because it requires the Oid class in Encoding assembly.
            Assert.Throws<PlatformNotSupportedException>(() => AsymmetricAlgorithm.Create());
            Assert.Throws<PlatformNotSupportedException>(() => AsymmetricAlgorithm.Create(null));
            Assert.Throws<PlatformNotSupportedException>(() => HashAlgorithm.Create());
            Assert.Throws<PlatformNotSupportedException>(() => HashAlgorithm.Create(null));
            Assert.Throws<PlatformNotSupportedException>(() => KeyedHashAlgorithm.Create());
            Assert.Throws<PlatformNotSupportedException>(() => KeyedHashAlgorithm.Create(null));
            Assert.Throws<PlatformNotSupportedException>(() => HMAC.Create());
            Assert.Throws<PlatformNotSupportedException>(() => HMAC.Create(null));
            Assert.Throws<PlatformNotSupportedException>(() => SymmetricAlgorithm.Create());
            Assert.Throws<PlatformNotSupportedException>(() => SymmetricAlgorithm.Create(null));
        }
    }
}
