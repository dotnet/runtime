// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Encryption.Tests.Symmetric
{
    public static class TrivialTests
    {
        [Fact]
        public static void TestKey()
        {
            using (Trivial s = new Trivial())
            {
                Assert.Equal(0, s.KeySize);

                Assert.Throws<ArgumentNullException>(() => s.Key = null);

                {
                    // Testing automatic generation of Key.

                    Trivial t = new Trivial();
                    byte[] generatedKey = t.Key;
                    Assert.Equal(generatedKey, Trivial.GeneratedKey);
                    Assert.False(Object.ReferenceEquals(generatedKey, Trivial.GeneratedKey));
                }

                // Testing KeySize and Key setter.
                int[] validKeySizes = { 40, 104, 152, 808, 816, 824, 832 };
                for (int keySize = -10; keySize < 200 * 8; keySize++)
                {
                    if (validKeySizes.Contains(keySize))
                    {
                        s.KeySize = keySize;
                        Assert.Equal(keySize, s.KeySize);
                    }
                    else
                    {
                        Assert.Throws<CryptographicException>(() => s.KeySize = keySize);
                    }

                    if (keySize >= 0)
                    {
                        int keySizeInBytes = keySize / 8;
                        byte[] key = GenerateRandom(keySizeInBytes);
                        if (validKeySizes.Contains(keySizeInBytes * 8))
                        {
                            s.Key = key;
                            byte[] copyOfKey = s.Key;
                            Assert.Equal(key, copyOfKey);
                            Assert.False(Object.ReferenceEquals(key, copyOfKey));
                        }
                        else
                        {
                            Assert.Throws<CryptographicException>(() => s.Key = key);
                        }
                    }
                }

                // Test overflow
                try
                {
                    byte[] hugeKey = new byte[536870917]; // value chosen so that when multiplied by 8 (bits) it overflows to the value 40
                    Assert.Throws<CryptographicException>(() => s.Key = hugeKey);
                }
                catch (OutOfMemoryException) { } // in case there isn't enough memory at test-time to allocate the large array
            }
        }

        [Fact]
        public static void TestIv()
        {
            using (Trivial s = new Trivial())
            {
                Assert.Throws<ArgumentNullException>(() => s.IV = null);

                {
                    // Testing automatic generation of Iv.

                    Trivial t = new Trivial();
                    t.BlockSize = 5 * 8;
                    byte[] generatedIv = t.IV;
                    Assert.Equal(generatedIv, Trivial.GeneratedIV);
                    Assert.False(Object.ReferenceEquals(generatedIv, Trivial.GeneratedIV));
                }

                // Testing IV property setter
                {
                    s.BlockSize = 5 * 8;
                    {
                        byte[] iv = GenerateRandom(5);
                        s.IV = iv;
                        byte[] copyOfIv = s.IV;
                        Assert.Equal(iv, copyOfIv);
                        Assert.False(Object.ReferenceEquals(iv, copyOfIv));
                    }

                    {
                        byte[] iv = GenerateRandom(6);
                        Assert.Throws<CryptographicException>(() => s.IV = iv);
                    }
                }
            }
            return;
        }

        [Fact]
        public static void TestBlockSize()
        {
            using (Trivial s = new Trivial())
            {
                Assert.Equal(0, s.BlockSize);

                // Testing BlockSizeSetter.
                int[] validBlockSizes = { 40, 104, 152, 808, 816, 824, 832 };
                for (int blockSize = -10; blockSize < 200 * 8; blockSize++)
                {
                    if (validBlockSizes.Contains(blockSize))
                    {
                        s.BlockSize = blockSize;
                        Assert.Equal(blockSize, s.BlockSize);
                    }
                    else
                    {
                        Assert.Throws<CryptographicException>(() => s.BlockSize = blockSize);
                    }
                }
            }
            return;
        }

        private static byte[] GenerateRandom(int size)
        {
            byte[] data = new byte[size];
            Random r = new Random();
            for (int i = 0; i < size; i++)
            {
                data[i] = unchecked((byte)(r.Next()));
            }
            return data;
        }

        private class Trivial : SymmetricAlgorithm
        {
            public Trivial()
            {
                //
                // Although the desktop CLR allows overriding the LegalKeySizes property, 
                // the BlockSize setter does not invoke the overriding method when validating
                // the blockSize. Instead, it accesses the underlying field (LegalKeySizesValue) directly.
                //
                // We've since removed this field from the public surface area (and fixed the BlockSize property
                // to call LegalKeySizes rather than the underlying field.) To make this test also run on the desktop, however,
                // we will also set the LegalKeySizesValue field if present.
                //
                FieldInfo legalBlockSizesValue = typeof(SymmetricAlgorithm).GetTypeInfo().GetDeclaredField("LegalBlockSizesValue");
                if (legalBlockSizesValue != null && legalBlockSizesValue.IsFamily)
                {
                    legalBlockSizesValue.SetValue(this, LegalBlockSizes);
                }
            }

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
                IV = GeneratedIV;
            }

            public override void GenerateKey()
            {
                Key = GeneratedKey;
            }

            public override KeySizes[] LegalBlockSizes
            {
                get
                {
                    return new KeySizes[]
                    {
                        new KeySizes(5*8, -99*8, 0*8),
                        new KeySizes(13*8, 22*8, 6*8),
                        new KeySizes(101*8, 104*8, 1*8),
                    };
                }
            }

            public override KeySizes[] LegalKeySizes
            {
                get
                {
                    return new KeySizes[]
                    {
                        new KeySizes(5*8, -99*8, 0*8),
                        new KeySizes(13*8, 22*8, 6*8),
                        new KeySizes(101*8, 104*8, 1*8),
                    };
                }
            }

            public static readonly byte[] GeneratedKey = GenerateRandom(13);
            public static readonly byte[] GeneratedIV = GenerateRandom(5);
        }

        private class GenerateIvNotImplementedException : Exception { }
        private class GenerateKeyNotImplementedException : Exception { }
        private class CreateDecryptorNotImplementedException : Exception { }
        private class CreateEncryptorNotImplementedException : Exception { }
    }
}