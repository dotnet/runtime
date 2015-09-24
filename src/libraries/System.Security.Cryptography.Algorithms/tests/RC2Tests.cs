// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Rc2.Tests
{
    public static partial class RC2Tests
    {
        [Fact]
        public static void RC2DefaultCtor()
        {
            using (RC2 rc2 = new RC2Minimal())
            {
                Assert.Equal(128, rc2.KeySize);
                Assert.Equal(64, rc2.BlockSize);
                Assert.Equal(CipherMode.CBC, rc2.Mode);
                Assert.Equal(PaddingMode.PKCS7, rc2.Padding);
            }
        }

        [Fact]
        public static void RC2EffectiveKeySize()
        {
            using (RC2 rc2 = new RC2Minimal())
            {
                for (int keySize = 40; keySize <= 1024; keySize += 8)
                {
                    rc2.KeySize = keySize;
                    Assert.Equal(keySize, rc2.KeySize);

                    rc2.EffectiveKeySize = 0;
                    Assert.Equal(keySize, rc2.EffectiveKeySize);

                    for (int effectiveKeySize = 40; effectiveKeySize <= keySize; effectiveKeySize += 8)
                    {
                        rc2.EffectiveKeySize = effectiveKeySize;
                        Assert.Equal(effectiveKeySize, rc2.EffectiveKeySize);
                    }
                }
            }
        }

        private sealed class RC2Minimal : RC2
        {
            public sealed override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotImplementedException();
            }

            public sealed override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotImplementedException();
            }

            public sealed override void GenerateIV()
            {
                throw new NotImplementedException();
            }

            public sealed override void GenerateKey()
            {
                throw new NotImplementedException();
            }
        }
    }
}
