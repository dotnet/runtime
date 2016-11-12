// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    using RC2 = System.Security.Cryptography.RC2;

    public static partial class RC2CryptoServiceProviderTests
    {
        [Fact]
        public static void RC2KeySize()
        {
            using (RC2 rc2 = new RC2CryptoServiceProvider())
            {
                rc2.KeySize = 40;
                Assert.Equal(40 / 8, rc2.Key.Length);
                Assert.Equal(40, rc2.KeySize);

                rc2.KeySize = 128;
                Assert.Equal(128 / 8, rc2.Key.Length);
                Assert.Equal(128, rc2.KeySize);

                Assert.Throws<CryptographicException>(() => rc2.KeySize = 40 - 8);
                Assert.Throws<CryptographicException>(() => rc2.KeySize = 128 + 8);
            }
        }
    }
}
