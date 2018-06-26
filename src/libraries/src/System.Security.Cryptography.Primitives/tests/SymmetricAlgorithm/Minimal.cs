// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Encryption.Tests.Symmetric
{
    public class MinimalTests
    {
        [Fact]
        public static void TestMinimalProperties()
        {
            using (Minimal s = new Minimal())
            {
                Assert.Equal(0, s.KeySize);
                Assert.Equal(0, s.BlockSize);

                object ignored;
                Assert.Throws<GenerateKeyNotImplementedException>(() => ignored = s.Key);
                Assert.Throws<GenerateIvNotImplementedException>(() => ignored = s.IV);

                Assert.Equal(CipherMode.CBC, s.Mode);
                Assert.Equal(PaddingMode.PKCS7, s.Padding);

                // Desktop compat: LegalBlockSizes and LegalKeySizes have to be overridden for these
                // properties (and the class as a whole) to be of any use.
                Assert.Throws<NullReferenceException>(() => ignored = s.LegalBlockSizes);
                Assert.Throws<NullReferenceException>(() => ignored = s.LegalKeySizes);
                Assert.Throws<NullReferenceException>(() => ignored = s.KeySize = 5);
                Assert.Throws<NullReferenceException>(() => s.Key = new byte[5]);

                s.Mode = CipherMode.CBC;
                Assert.Equal(CipherMode.CBC, s.Mode);
                s.Mode = CipherMode.ECB;
                Assert.Equal(CipherMode.ECB, s.Mode);
                Assert.Throws<CryptographicException>(() => s.Mode = CipherMode.CTS);
                Assert.Throws<CryptographicException>(() => s.Mode = (CipherMode)12345);
                Assert.Equal(CipherMode.ECB, s.Mode);

                s.Padding = PaddingMode.None;
                Assert.Equal(PaddingMode.None, s.Padding);
                s.Padding = PaddingMode.Zeros;
                Assert.Equal(PaddingMode.Zeros, s.Padding);
                s.Padding = PaddingMode.PKCS7;
                Assert.Equal(PaddingMode.PKCS7, s.Padding);
                Assert.Throws<CryptographicException>(() => s.Padding = (PaddingMode)12345);
                Assert.Equal(PaddingMode.PKCS7, s.Padding);
            }
        }

        private class Minimal : SymmetricAlgorithm
        {
            public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new CreateDecryptorNotImplementedException();
            }

            public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new CreateEncryptorNotImplementedException();
            }

            public override void GenerateIV()
            {
                throw new GenerateIvNotImplementedException();
            }

            public override void GenerateKey()
            {
                throw new GenerateKeyNotImplementedException();
            }
        }

        private class GenerateIvNotImplementedException : Exception { }
        private class GenerateKeyNotImplementedException : Exception { }
        private class CreateDecryptorNotImplementedException : Exception { }
        private class CreateEncryptorNotImplementedException : Exception { }
    }
}
