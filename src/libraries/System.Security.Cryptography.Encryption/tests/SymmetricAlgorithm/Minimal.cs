// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

                Object ignored;
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