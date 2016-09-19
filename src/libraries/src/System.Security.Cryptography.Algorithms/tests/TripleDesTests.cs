// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{

    public static partial class TripleDesTests
    {
        [Fact]
        public static void DesDefaultCtor()
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

        private sealed class TripleDESMinimal : TripleDES
        {
            public TripleDESMinimal()
            {
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
