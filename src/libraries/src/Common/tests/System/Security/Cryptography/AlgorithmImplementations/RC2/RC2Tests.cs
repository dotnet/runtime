// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    using RC2 = System.Security.Cryptography.RC2;

    public static partial class RC2Tests
    {
        [Fact]
        public static void RC2DefaultCtor()
        {
            using (RC2 rc2 = RC2Factory.Create())
            {
                Assert.Equal(128, rc2.KeySize);
                Assert.Equal(64, rc2.BlockSize);
                Assert.Equal(CipherMode.CBC, rc2.Mode);
                Assert.Equal(PaddingMode.PKCS7, rc2.Padding);
            }
        }

        [Fact]
        public static void RC2Blockize()
        {
            using (RC2 rc2 = RC2Factory.Create())
            {
                rc2.BlockSize = 64;
                Assert.Equal(64, rc2.BlockSize);

                Assert.Throws<CryptographicException>(() => rc2.BlockSize = 64 - 1);
                Assert.Throws<CryptographicException>(() => rc2.BlockSize = 64 + 1);
            }
        }

        [Fact]
        public static void RC2EffectiveKeySize()
        {
            using (RC2 rc2 = RC2Factory.Create())
            {
                rc2.KeySize = 40;
                Assert.Equal(40, rc2.EffectiveKeySize);
                rc2.EffectiveKeySize = 40;

                // KeySize must equal EffectiveKeySize
                rc2.KeySize = 48;
                Assert.Throws<CryptographicUnexpectedOperationException>(() => rc2.EffectiveKeySize = 48 + 8);
                Assert.Throws<CryptographicUnexpectedOperationException>(() => rc2.EffectiveKeySize = 48 - 8);
                Assert.Throws<CryptographicUnexpectedOperationException>(() => rc2.EffectiveKeySize = 0);
            }
        }
    }
}
