// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{

    public static partial class TripleDesTests
    {
        [Fact]
        public static void TripleDesDefaultCtor()
        {
            using (TripleDES tdes = new TripleDESMinimal())
            {
                Assert.Equal(192, tdes.KeySize);
                Assert.Equal(64, tdes.BlockSize);
                Assert.Equal(CipherMode.CBC, tdes.Mode);
                Assert.Equal(PaddingMode.PKCS7, tdes.Padding);
            }
        }

        [Fact]
        public static void TripleDesIsWeakPositive()
        {
            foreach (byte[] key in BadKeys())
            {
                bool isWeak = TripleDES.IsWeakKey(key);
                Assert.True(isWeak);
            }
        }

        [Fact]
        public static void TripleDesCannotSetWeakKey()
        {
            using (TripleDESMinimal d = new TripleDESMinimal())
            {
                foreach (byte[] key in BadKeys())
                {
                    Assert.Throws<CryptographicException>(() => d.Key = key);
                }
            }
        }

        [Fact]
        public static void TripleDesCreate()
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes("This is a secret message and is a sentence that is longer than a block, it ensures that multi-block functions work.");
            TripleDES tripleDes = TripleDES.Create();

            byte[] encryptedBytes;
            using (MemoryStream input = new MemoryStream(inputBytes))
            using (CryptoStream cryptoStream = new CryptoStream(input, tripleDes.CreateEncryptor(), CryptoStreamMode.Read))
            using (MemoryStream output = new MemoryStream())
            {
                cryptoStream.CopyTo(output);
                encryptedBytes = output.ToArray();
            }

            Assert.NotEqual(inputBytes, encryptedBytes);

            byte[] decryptedBytes;
            using (MemoryStream input = new MemoryStream(encryptedBytes))
            using (CryptoStream cryptoStream = new CryptoStream(input, tripleDes.CreateDecryptor(), CryptoStreamMode.Read))
            using (MemoryStream output = new MemoryStream())
            {
                cryptoStream.CopyTo(output);
                decryptedBytes = output.ToArray();
            }

            Assert.Equal(inputBytes, decryptedBytes);
        }

        [Fact]
        public static void EnsureLegalSizesValuesIsolated()
        {
            new TripleDESLegalSizesBreaker().Dispose();

            using (TripleDES tripleDes = TripleDES.Create())
            {
                Assert.Equal(3 * 64, tripleDes.LegalKeySizes[0].MaxSize);
                Assert.Equal(64, tripleDes.LegalBlockSizes[0].MaxSize);

                tripleDes.Key = new byte[]
                {
                    /* k1 */ 0, 1, 2, 3, 4, 5, 6, 7,
                    /* k2 */ 0, 0, 0, 2, 4, 6, 0, 1,
                    /* k3 */ 0, 1, 2, 3, 4, 5, 6, 7,
                };
            }
        }

        private static IEnumerable<byte[]> BadKeys()
        {
            foreach (byte[] key in _weakKeys)
            {
                yield return key;
                yield return key.RemoveDesParityBits();
            }
        }

        private static byte[] RemoveDesParityBits(this byte[] key)
        {
            byte[] noParityKey = new byte[key.Length];
            for (int i = 0; i < key.Length; i++)
            {
                noParityKey[i] = (byte)(key[i] & 0xfe);
            }
            return noParityKey;
        }

        private static byte[][] _weakKeys =
        {
            "00000000000000000000000000000000".HexToByteArray(),
            "bbbbbbbbbbbbbbbb00000000000000000000000000000000".HexToByteArray(),
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb0000000000000000".HexToByteArray(),
        };

        private class TripleDESLegalSizesBreaker : TripleDESMinimal
        {
            public TripleDESLegalSizesBreaker()
            {
                LegalKeySizesValue[0] = new KeySizes(1, 1, 0);
                LegalBlockSizesValue[0] = new KeySizes(1, 1, 0);
            }
        }

        private class TripleDESMinimal : TripleDES
        {
            // If the constructor uses a virtual call to any of the property setters
            // they will fail.
            private readonly bool _ready;

            public TripleDESMinimal()
            {
                // Don't set this as a field initializer, otherwise it runs before the base ctor.
                _ready = true;
            }

            public override int KeySize
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.KeySize = value;
                }
            }

            public override int BlockSize
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.BlockSize = value;
                }
            }

            public override byte[] IV
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.IV = value;
                }
            }

            public override byte[] Key
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.Key = value;
                }
            }

            public override CipherMode Mode
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.Mode = value;
                }
            }

            public override PaddingMode Padding
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.Padding = value;
                }
            }

            public sealed override void GenerateIV()
            {
                throw new NotSupportedException();
            }

            public sealed override void GenerateKey()
            {
                throw new NotSupportedException();
            }

            public sealed override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotSupportedException();
            }

            public sealed override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotSupportedException();
            }
        }
    }
}
